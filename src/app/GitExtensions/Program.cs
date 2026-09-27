using System.ComponentModel.Design;
using System.Configuration;
using System.Diagnostics;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI;
using GitUI;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Infrastructure.Telemetry;
using GitUI.NBugReports;
using GitUI.Theming;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;
using MessageBoxes = GitUI.MessageBoxes;

namespace GitExtensions;

internal static class Program
{
    private static readonly ServiceContainer _serviceContainer = new();

    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        // The askpass mode of ssh and git off Windows: the script of SSH_ASKPASS runs "GitExtensions askpass <prompt>".
        string[] commandLine = Environment.GetCommandLineArgs();
        if (!OperatingSystem.IsWindows() && commandLine.Length >= 2 && commandLine[1] == "askpass")
        {
            Environment.Exit(GitUI.AvaloniaHosting.AvaloniaStartupDialogs.RunAskPass(string.Join(" ", commandLine[2..])));
            return;
        }

        // If you want to suppress the BugReportInvoker when debugging and exit quickly, uncomment the condition:
        ////if (!Debugger.IsAttached)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => BugReportInvoker.Report((Exception)e.ExceptionObject, e.IsTerminating);

            // The exceptions of the background operations (those of the jobs of the UI thread are reported by AvaloniaUi).
            TaskManager.UnhandledExceptionHandler = exception => BugReportInvoker.Report(exception, isTerminating: false);
            AppDomain.CurrentDomain.ProcessExit += (s, e) => BugReportInvoker.IgnoreFailedToLoadAnAssembly = true;
        }

        // The message boxes and task dialogs off Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
        GitUI.AvaloniaHosting.AvaloniaDialogBoxHost.Register();

        ServiceContainerRegistry.RegisterServices(_serviceContainer);
        BugReportInvoker.ExecutorProvider = _serviceContainer.GetRequiredService<IGitExecutorProvider>();

        // If an error happens before we had a chance to init the environment information
        // the call to GetInformation() from BugReporter.ShowNBug() will fail.
        // There's no perf hit calling Initialise() multiple times.
        UserEnvironmentInformation.Initialise(ThisAssembly.Git.Sha, ThisAssembly.Git.IsDirty);

        AppSettings.SetDocumentationBaseUrl(AppSettings.ProductVersion);

        ThemeModule.Load();

        try
        {
            DiagnosticsClient.Initialize(ThisAssembly.Git.IsDirty);
        }
        catch (TypeInitializationException tie)
        {
            // is this exception caused by the configuration?
            if (tie.InnerException is not null
                && tie.InnerException.GetType()
                    .IsSubclassOf(typeof(ConfigurationException)))
            {
                HandleConfigurationException((ConfigurationException)tie.InnerException);
            }
        }

        AppTitleGenerator.Initialise(ThisAssembly.Git.Sha, ThisAssembly.Git.Branch);

        // NOTE we perform the rest of the application's startup in another method to defer
        // the JIT processing more types than required to configure NBug.
        // In this way, there's more chance we can handle startup exceptions correctly.
        RunApplication();
    }

    private static void RunApplication()
    {
        string[] args = Environment.GetCommandLineArgs();

        // Avalonia runs the UI thread; the shared JoinableTaskContext is created on its synchronization context.
        GitUI.AvaloniaHosting.AvaloniaStartupDialogs.InitializeUi();
        ThreadHelper.JoinableTaskContext = new JoinableTaskContext();

        ManagedExtensibility.Initialise(userPluginsPath: AppSettings.UserPluginsPath);

        AppSettings.LoadSettings();

        if (!OperatingSystem.IsWindows())
        {
            SetUpAskPass();
        }

        if (OperatingSystem.IsWindows())
        {
            GitUI.AvaloniaHosting.AvaloniaStartupDialogs.CheckHomePath();
        }

        if (string.IsNullOrEmpty(AppSettings.Translation))
        {
            GitUI.AvaloniaHosting.AvaloniaStartupDialogs.TryShowChooseTranslation();
        }

        // The first start does not ask about telemetry: it is off until the user allows it in the settings (General).
        AppSettings.TelemetryEnabled ??= false;

        // The checklist and settings dialog load independent copies from disk. Flush the startup choices before
        // creating them, rather than waiting for the settings cache's delayed save (which can lose the language).
        AppSettings.SaveSettings();

        try
        {
            // Ensure we can find the git command to execute,
            // unless we are being instructed to uninstall,
            // or AppSettings.CheckSettings is set to false.
            if (!(args.Length >= 2 && args[1] == "uninstall"))
            {
                if (!CheckSettingsLogic.SolveGitCommand())
                {
                    if (!LocateMissingGit())
                    {
                        Environment.Exit(-1);
                        return;
                    }
                }

                GitUICommands uiCommands = new(_serviceContainer, new GitModule(_serviceContainer.GetRequiredService<IGitExecutorProvider>(), ""));
                CommonLogic commonLogic = new(uiCommands.Module);
                if (AppSettings.CheckSettings)
                {
                    CheckSettingsLogic checkSettingsLogic = new(commonLogic);
                    if (!GitUI.AvaloniaHosting.AvaloniaStartupDialogs.CheckSettings(uiCommands, commonLogic))
                    {
                        if (!checkSettingsLogic.AutoSolveAllSettings() || !GitUI.AvaloniaHosting.AvaloniaStartupDialogs.CheckSettings(uiCommands, commonLogic))
                        {
                            uiCommands.StartSettingsDialog(owner: null);
                        }
                    }
                }
                else
                {
                    CheckSettingsLogic.SolveEditor(commonLogic);
                }
            }
        }
        catch
        {
            // TODO: remove catch-all
        }

        GitUICommands commands = new(_serviceContainer, new GitModule(_serviceContainer.GetRequiredService<IGitExecutorProvider>(), GetWorkingDir(args)));

        if (args.Length <= 1)
        {
            commands.StartBrowseDialog(owner: null);
        }
        else
        {
            // if we are here args.Length > 1

            // Avoid replacing the ExitCode eventually set while parsing arguments,
            // i.e. assume -1 and afterwards, only set it to 0 if no error is indicated.
            Environment.ExitCode = -1;
            try
            {
                if (commands.RunCommand(args))
                {
                    Environment.ExitCode = 0;
                }
            }
            catch (Exception ex)
            {
                BugReportInvoker.Report(new UserExternalOperationException(
                        context: "Invalid Gitanas command line",
                        new ExternalOperationException(command: args.Join(" ").Quote(), innerException: ex)),
                    isTerminating: false);
            }
        }

        AppSettings.SaveSettings();
    }

    private static string? GetWorkingDir(string[] args)
    {
        string? workingDir = null;

        if (args.Length >= 3)
        {
            // there is bug in .net
            // while parsing command line arguments, it unescapes " incorrectly
            // https://github.com/gitextensions/gitextensions/issues/3489
            string dirArg = args[2].TrimEnd('"');
            if (!string.IsNullOrWhiteSpace(dirArg))
            {
                if (!Directory.Exists(dirArg))
                {
                    dirArg = Path.GetDirectoryName(dirArg)!;
                }

                workingDir = GitModule.TryFindGitWorkingDir(dirArg);

                if (Directory.Exists(workingDir))
                {
                    workingDir = Path.GetFullPath(workingDir);
                }

                // Do not add this working directory to the recent repositories. It is a nice feature, but it
                // also increases the startup time
                ////if (Module.ValidWorkingDir())
                ////   Repositories.RepositoryHistory.AddMostRecentRepository(Module.WorkingDir);
            }
        }

        if (args.Length <= 1 && workingDir is null && AppSettings.StartWithRecentWorkingDir)
        {
            if (GitModule.IsValidGitWorkingDir(AppSettings.RecentWorkingDir))
            {
                workingDir = AppSettings.RecentWorkingDir;
            }
        }

        if (args.Length > 1 && workingDir is null)
        {
            // If no working dir is yet found, try to find one relative to the current working directory.
            // This allows the `fileeditor` command to discover repository configuration which is
            // required for core.commentchar support.
            workingDir = GitModule.TryFindGitWorkingDir(Environment.CurrentDirectory);
        }

        return workingDir;
    }

    /// <summary>
    /// Used in the rare event that the configuration file for the application is corrupted.
    /// </summary>
    private static void HandleConfigurationException(ConfigurationException ce)
    {
        bool exceptionHandled = false;
        try
        {
            // perhaps this should be checked for if it is null
            Exception? in3 = ce.InnerException?.InnerException;

            // saves having to have a reference to System.Xml just to check that we have an XmlException
            if (in3?.GetType().Name == "XmlException")
            {
                string localSettingsPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppSettings.ApplicationId);

                // assume that if we are having this error and the installation is not a portable one then the folder will exist.
                if (Directory.Exists(localSettingsPath))
                {
                    string messageContent = string.Format("There is a problem with the user.xml configuration file.{0}{0}The error message was: {1}{0}{0}The configuration file is usually found in: {2}{0}{0}Problems with configuration can usually be solved by deleting the configuration file. Would you like to delete the file?", Environment.NewLine, in3.Message, localSettingsPath);

                    if (MessageBoxes.Show(messageContent, "Configuration Error",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2) == DialogResult.Yes)
                    {
                        if (localSettingsPath.TryDeleteDirectory(out string? errorMessage))
                        {
                            // Restart Git Extensions with the same arguments after old config is deleted?
                            if (DialogResult.OK.Equals(MessageBoxes.Show(string.Format("Files have been deleted.{0}{0}Would you like to attempt to restart Gitanas?", Environment.NewLine), "Configuration Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Question)))
                            {
                                string[] args = Environment.GetCommandLineArgs();

                                // The executable of the process (the first argument is the dll of the application).
                                Process p = new() { StartInfo = { FileName = Environment.ProcessPath ?? args[0] } };
                                if (args.Length > 1)
                                {
                                    args[0] = "";
                                    p.StartInfo.Arguments = string.Join(" ", args);
                                }

                                p.Start();
                            }
                        }
                        else
                        {
                            MessageBoxes.Show(string.Format("Could not delete all files and folders in {0}!", localSettingsPath), "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }

                // assuming that there is no localSettingsPath directory in existence we probably have a portable installation.
                else
                {
                    string messageContent = string.Format("There is a problem with the application settings XML configuration file.{0}{0}The error message was: {1}{0}{0}Problems with configuration can usually be solved by deleting the configuration file.", Environment.NewLine, in3.Message);
                    MessageBoxes.Show(messageContent, "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                exceptionHandled = true;
            }
        }
        finally
        {
            // if we fail in this somehow at least this message might get somewhere
            if (!exceptionHandled)
            {
                MessageBoxes.Show(ce.ToString(), "Configuration Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Environment.Exit(1);
        }
    }

    /// <summary>The prompt of ssh and git off Windows (<see cref="AskPassScript"/>), in the local data folder of the application.</summary>
    private static void SetUpAskPass()
    {
        try
        {
            string folder = AppSettings.LocalApplicationDataPath.Value ?? Path.GetTempPath();
            EnvironmentConfiguration.AskPassCommand = AskPassScript.Write(folder, AppSettings.GetGitExtensionsFullPath());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.WriteLine($"The prompt of ssh could not be set up: {ex.Message}");
        }
    }

    private static bool LocateMissingGit()
    {
        TaskDialogPage page = new()
        {
            Heading = ResourceManager.TranslatedStrings.GitExecutableNotFound,
            Icon = TaskDialogIcon.Error,
            Buttons = { TaskDialogButton.Cancel },
            AllowCancel = true,
            SizeToContent = true
        };
        TaskDialogCommandLinkButton btnFindGitExecutable = new(ResourceManager.TranslatedStrings.FindGitExecutable);
        TaskDialogCommandLinkButton btnInstallGitInstructions = new(ResourceManager.TranslatedStrings.InstallGitInstructions);
        page.Buttons.Add(btnFindGitExecutable);
        page.Buttons.Add(btnInstallGitInstructions);

        TaskDialogButton result = TaskDialog.ShowDialog(page);
        if (result == btnFindGitExecutable)
        {
            using OpenFileDialog dialog = new() { Filter = OperatingSystem.IsWindows() ? @"git.exe|git.exe|git.cmd|git.cmd" : "git|git" };
            if (dialog.ShowDialog(null) == DialogResult.OK)
            {
                AppSettings.GitCommandValue = dialog.FileName;
                return CheckSettingsLogic.SolveGitCommand(dialog.FileName);
            }

            return CheckSettingsLogic.SolveGitCommand();
        }

        if (result == btnInstallGitInstructions)
        {
            OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/wiki/Application-Dependencies#git");
            return false;
        }

        return false;
    }
}

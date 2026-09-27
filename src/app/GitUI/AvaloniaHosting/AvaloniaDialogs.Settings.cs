using System.Diagnostics;
using Avalonia.Threading;
using GitCommands;
using GitCommands.Git;
using GitCommands.Settings;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.Translations;
using GitUI.ScriptsEngine;
using SettingsPageViewModel = GitUI.Presentation.CommandsDialogs.SettingsDialog.SettingsPageViewModel;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the settings dialog (docs/avalonia-port/PLAN.md, phase 6), with all its pages and the pages of the plugins.
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The Avalonia port of <c>FormSettings.ShowSettingsDialog</c>.</summary>
    /// <param name="saved">Whether settings were saved (as <c>DialogResult.OK</c>).</param>
    public static bool TryShowSettings(IWin32Window? owner, IGitUICommands commands, SettingsPageReference? initialPage, out bool saved)
    {
        saved = false;
        CommonLogic commonLogic = new(commands.Module);
        SettingsWindow? settingsWindow = null;
        SettingsDialogHost dialogHost = new(commonLogic, owner, () => settingsWindow is null ? owner : new NativeWindowOwner(settingsWindow));
        SettingsDialogViewModel viewModel = new(ViewStrings.Load<SettingsDialogStrings>(), dialogHost);
        GeneralSettingsPagesHost generalHost = new(commands, owner);
        GitSettingsPagesHost gitHost = new(commands, commonLogic);
        AddSettingsPages(
            viewModel,
            commonLogic,
            commands,
            commands.Module.IsValidGitWorkingDir(),
            () => settingsWindow is null ? owner : new NativeWindowOwner(settingsWindow),
            generalHost,
            gitHost);

        // As ShowSettingsDialog: the pages read and write AppSettings in the global settings of the dialog until saved.
        AppSettings.UsingContainer(commonLogic.DistributedSettingsSet.GlobalSettings, () =>
        {
            dialogHost.ThemeSettings = GetThemeSettings();
            ShowDialog(
                () =>
                {
                    SettingsWindow window = new() { DataContext = viewModel };
                    settingsWindow = window;
                    generalHost.Window = window;
                    gitHost.Window = window;
                    window.Opened += (_, _) => viewModel.Open(initialPage switch
                    {
                        SettingsPageReferenceByType byType => byType.SettingsPageType.Name,
                        SettingsPageReferenceByName byName => byName.PageName,
                        _ => null,
                    });
                    return window;
                },
                owner,
                positionName: "FormSettings");
        });

        // The pages holding controls (the settings controls of the build server plugins) release them.
        foreach (IDisposable page in viewModel.Pages.OfType<IDisposable>())
        {
            page.Dispose();
        }

        saved = viewModel.IsSaved;

        if (viewModel.IsRestartConfirmed)
        {
            string repository = commands.Module.IsValidGitWorkingDir() ? commands.Module.WorkingDir : Environment.CurrentDirectory;
            Dispatcher.UIThread.Post(() => RestartApplication(repository));
        }

        return true;
    }

    /// <summary>The settings applied at the start: the color theme and its variations.</summary>
    private static string GetThemeSettings()
        => $"{AppSettings.ThemeId.Name}|{AppSettings.ThemeId.IsBuiltin}|{string.Join(",", AppSettings.ThemeVariations)}";

    /// <summary>
    ///  Starts the application again on <paramref name="repository"/> (as the main window), then closes the main windows of
    ///  this one, as Quit does; the process ends with the last one.
    /// </summary>
    private static void RestartApplication(string repository)
    {
        ProcessStartInfo startInfo = new(Environment.ProcessPath!) { WorkingDirectory = repository, UseShellExecute = false };

        // Started as "dotnet Gitanas.dll": the host is given the application again.
        if (Path.GetFileNameWithoutExtension(startInfo.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
        }

        startInfo.ArgumentList.Add("browse");
        try
        {
            Process.Start(startInfo)?.Dispose();
        }
        catch (Exception exception)
        {
            MessageBoxes.ShowError(owner: null, exception.Message);
            return;
        }

        CloseBrowseWindows();
    }

    /// <summary>
    ///  As <c>OnRuntimeLoad</c> of <c>FormSettings</c>: the pages in the tree, with the levels of their base class
    ///  (<c>SettingsPageWithHeader</c>: global; <c>DistributedSettingsPage</c> and <c>GitConfigBaseSettingsPage</c>: the
    ///  levels of the repository too, in a repository). The pages not ported yet are left out.
    /// </summary>
    /// <param name="getOwner">The owner of the message boxes and dialogs of the pages: the settings window once shown.</param>
    /// <param name="generalHost">The host of the pages of the Git Extensions settings (General, Appearance, Advanced, Detailed).</param>
    /// <param name="gitHost">The host of the Git pages and of SSH, the build server integration, the revision links and the shell extension.</param>
    private static void AddSettingsPages(
        SettingsDialogViewModel viewModel,
        CommonLogic commonLogic,
        IGitUICommands commands,
        bool canSaveInsideRepo,
        Func<IWin32Window?> getOwner,
        GeneralSettingsPagesHost generalHost,
        GitSettingsPagesHost gitHost)
    {
        DistributedSettingsSet distributed = commonLogic.DistributedSettingsSet;
        GitConfigSettingsSet gitConfig = commonLogic.GitConfigSettingsSet;
        Dictionary<SettingsLevel, SettingsSource> none = [];
        Dictionary<SettingsLevel, SettingsSource> global = new() { [SettingsLevel.Global] = distributed.GlobalSettings };
        Dictionary<SettingsLevel, SettingsSource> distributedLevels = canSaveInsideRepo
            ? new()
            {
                [SettingsLevel.Effective] = distributed.EffectiveSettings,
                [SettingsLevel.Local] = distributed.LocalSettings,
                [SettingsLevel.Distributed] = distributed.DistributedSettings,
                [SettingsLevel.Global] = distributed.GlobalSettings,
            }
            : global;
        Dictionary<SettingsLevel, SettingsSource> gitConfigLevels = canSaveInsideRepo
            ? new()
            {
                [SettingsLevel.Effective] = gitConfig.EffectiveSettings,
                [SettingsLevel.Local] = gitConfig.LocalSettings,
                [SettingsLevel.Global] = gitConfig.GlobalSettings,
                [SettingsLevel.System] = gitConfig.SystemSettings,
            }
            : new() { [SettingsLevel.Global] = gitConfig.GlobalSettings };

        SettingsDialogStrings strings = viewModel.Strings;

        // Git Extensions settings
        const string gitExtensions = nameof(GitExtensionsSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitExtensionsGroup.Text, gitExtensions), null, "GitanasLogo16", none);
        ChecklistSettingsPageStrings checklistStrings = ViewStrings.Load<ChecklistSettingsPageStrings>();
        ChecklistSettingsPageViewModel checklist = new(checklistStrings, new ChecklistSettingsHost(checklistStrings, commonLogic, commands, getOwner));
        Add(checklist, gitExtensions, null, global, asRoot: true);

        Add(new GeneralSettingsPageViewModel(ViewStrings.Load<GeneralSettingsPageStrings>(), LoadRecentCloneDestinations(), generalHost, generalHost), gitExtensions, "GeneralSettings", global);

        // >> Appearance
        Add(new AppearanceSettingsPageViewModel(ViewStrings.Load<AppearanceSettingsPageStrings>(), generalHost), gitExtensions, "Appearance", global);
        const string appearance = "AppearanceSettingsPage";
        Add(new SortingSettingsPageViewModel(ViewStrings.Load<SortingSettingsPageStrings>(), generalHost), appearance, "SortBy", global);
        Add(new ColorsSettingsPageViewModel(ViewStrings.Load<ColorsSettingsPageStrings>(), generalHost), appearance, "Colors", global);
        Add(new AppearanceFontsSettingsPageViewModel(ViewStrings.Load<AppearanceFontsSettingsPageStrings>(), generalHost), appearance, "Font", global);
        Add(new ConsoleStyleSettingsPageViewModel(ViewStrings.Load<ConsoleStyleSettingsPageStrings>(), generalHost), appearance, "Console", global);
        Add(new RevisionLinksSettingsPageViewModel(ViewStrings.Load<RevisionLinksSettingsPageStrings>(), gitHost), gitExtensions, "Link", distributedLevels);

        Add(new BuildServerIntegrationSettingsPageViewModel(ViewStrings.Load<BuildServerIntegrationSettingsPageStrings>(), gitHost), gitExtensions, "Integration", distributedLevels);
        Add(new ScriptsSettingsPageViewModel(ViewStrings.Load<ScriptsSettingsPageStrings>(), new ScriptsSettingsHost(commands.GetRequiredService<IScriptsManager>())), gitExtensions, "Console", global);
        Add(new HotkeysSettingsPageViewModel(ViewStrings.Load<HotkeysSettingsPageStrings>(), new HotkeysSettingsHost(commands.GetRequiredService<IHotkeySettingsManager>())), gitExtensions, "Hotkey", global);

        if (OperatingSystem.IsWindows())
        {
            Add(new ShellExtensionSettingsPageViewModel(ViewStrings.Load<ShellExtensionSettingsPageStrings>(), gitHost), gitExtensions, "ShellExtensions", global);
        }

        // >> Advanced
        Add(new AdvancedSettingsPageViewModel(ViewStrings.Load<AdvancedSettingsPageStrings>(), generalHost), gitExtensions, "AdvancedSettings", global);
        const string advanced = "AdvancedSettingsPage";
        Add(new ConfirmationsSettingsPageViewModel(ViewStrings.Load<ConfirmationsSettingsPageStrings>()), advanced, "BisectGood", global);

        // >> Detailed
        Add(new DetailedSettingsPageViewModel(ViewStrings.Load<DetailedSettingsPageStrings>()), gitExtensions, "Settings", distributedLevels);
        const string detailed = "DetailedSettingsPage";
        Add(new FormBrowseRepoSettingsPageViewModel(ViewStrings.Load<FormBrowseRepoSettingsPageStrings>(), generalHost), detailed, "BranchFolder", global);
        Add(new CommitDialogSettingsPageViewModel(ViewStrings.Load<CommitDialogSettingsPageStrings>()), detailed, "CommitSummary", global);
        Add(new DiffViewerSettingsPageViewModel(ViewStrings.Load<DiffViewerSettingsPageStrings>(), generalHost), detailed, "Diff", global);
        Add(new BlameViewerSettingsPageViewModel(ViewStrings.Load<BlameViewerSettingsPageStrings>()), detailed, "Blame", global);

        // As checklistSettingsPage.SshSettingsPage: the checklist finds the paths of PuTTY with the SSH page.
        SshSettingsPageViewModel ssh = new(ViewStrings.Load<SshSettingsPageStrings>(), gitHost, gitHost);
        Add(ssh, gitExtensions, "Key", global);
        checklist.PuttyPathsFinder = ssh.AutoFindPuttyPaths;

        // Git settings
        const string git = nameof(GitSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.GitGroup.Text, git), null, "GitLogo16", none);
        Add(new GitSettingsPageViewModel(ViewStrings.Load<GitSettingsPageStrings>(), gitHost, gitHost), git, "FolderOpen", global);
        Add(new GitConfigSettingsPageViewModel(ViewStrings.Load<GitConfigSettingsPageStrings>(), gitHost, gitHost, commands.Module.WorkingDir), git, "GeneralSettings", gitConfigLevels);
        Add(new GitConfigAdvancedSettingsPageViewModel(ViewStrings.Load<GitConfigAdvancedSettingsPageStrings>(), gitHost, GitVersion.Current.SupportUpdateRefs), git, "AdvancedSettings", gitConfigLevels);
        Add(IntroductionSettingsPageViewModel.CreateGitRoot(), git, null, none, asRoot: true);

        // Plugins settings
        const string plugins = nameof(PluginsSettingsGroup);
        viewModel.AddPage(new GroupSettingsPageViewModel(strings.PluginsGroup.Text, plugins), null, "Plugin", none);
        Add(IntroductionSettingsPageViewModel.CreatePluginRoot(), plugins, null, none, asRoot: true);
        foreach ((PluginSettingsPageViewModel page, byte[]? icon) in CreatePluginSettingsPages(() => getOwner().ToWindowOwner()))
        {
            Add(page, plugins, icon, distributedLevels);
        }

        void Add(SettingsPageViewModel page, string parent, object? icon, Dictionary<SettingsLevel, SettingsSource> sources, bool asRoot = false)
            => viewModel.AddPage(page, parent, icon, sources, asRoot);
    }

    /// <summary>The end of <c>FormSettings.Save</c> and its error message.</summary>
    private sealed class SettingsDialogHost(CommonLogic commonLogic, IWin32Window? owner, Func<IWin32Window?> getDialogOwner) : ISettingsDialogHost
    {
        /// <summary>
        ///  The settings applied at the start (<see cref="GetThemeSettings"/>) when the dialog opened or when last asked, read in
        ///  the settings of the dialog: the settings of the application may see a save only later (their file watcher).
        /// </summary>
        public string? ThemeSettings { get; set; }

        // Another theme, variation or control theme saved: offer to restart the main windows (asked once per change).
        public bool ConfirmRestart()
        {
            string themeSettings = GetThemeSettings();
            if (themeSettings == ThemeSettings || !AvaloniaUi.IsMainLoopRunning)
            {
                return false;
            }

            ThemeSettings = themeSettings;
            return AvaloniaUi.RunInHostContext(() => MessageBoxes.ConfirmRestartToApplyTheme(getDialogOwner()));
        }

        public string? SaveSettingsSets() => AvaloniaUi.RunInHostContext(() =>
        {
            try
            {
                commonLogic.GitConfigSettingsSet.Save();
                commonLogic.DistributedSettingsSet.Save();
                if (OperatingSystem.IsWindows())
                {
                    HomeDirectoryCheck.CheckHomePath();
                }

                // Saves some specific settings only, despite its name.
                AppSettings.SaveSettings();
                return null;
            }
            catch (SaveSettingsException ex) when (ex.InnerException is not null)
            {
                return ex.InnerException.Message;
            }
        });

        public void ShowError(string heading, string text) => AvaloniaUi.RunInHostContext(() =>
        {
            TaskDialogPage page = new()
            {
                Text = text,
                Heading = heading,
                Caption = TranslatedStrings.Error,
                Buttons = { TaskDialogButton.OK },
                Icon = TaskDialogIcon.Error,
                AllowCancel = true,
                SizeToContent = true
            };
            TaskDialog.ShowDialog(owner?.Handle ?? 0, page);
        });
    }
}

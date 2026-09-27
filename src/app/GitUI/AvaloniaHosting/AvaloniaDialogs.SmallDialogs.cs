using System.ComponentModel;
using GitCommands;
using GitCommands.Remotes;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Editor;
using GitUI.Avalonia.Hosting;
using GitUI.Avalonia.ScriptsEngine;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Editor;
using GitUI.Presentation;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.ScriptsEngine;
using GitUI.Presentation.Translations;
using GitUI.Properties;
using GitUI.ScriptsEngine;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the first batch of small dialogs (docs/avalonia-port/PLAN.md, phase 2).
/// </summary>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowCommandlineHelp()
    {
        // The command list (not translated), as the resource of the WinForms FormCommandlineHelp.
        string commands = CommandlineHelpText;
        ShowDialog(() => new CommandlineHelpWindow { DataContext = new CommandlineHelpViewModel(ViewStrings.Load<CommandlineHelpStrings>(), commands) }, owner: null);
        return true;
    }

    public static bool TryShowAddFiles(IWin32Window? owner, IGitUICommands commands, string? filter)
    {
        ShowDialog(
            () =>
            {
                AddFilesWindow window = new();
                window.DataContext = new AddFilesViewModel(
                    ViewStrings.Load<AddFilesStrings>(),
                    filter,
                    arguments => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowProcess(
                        new NativeWindowOwner(window), commands, arguments, commands.Module.WorkingDir, input: null, useDialogSettings: false)));
                return window;
            },
            owner,
            positionName: "FormAddFiles");
        return true;
    }

    public static bool TryShowContributors(IWin32Window? owner)
    {
        ShowDialog(
            () => new ContributorsWindow { DataContext = new ContributorsViewModel(Resources.Team, Resources.Coders, Resources.Translators, Resources.Designers) },
            owner);
        return true;
    }

    /// <summary>The prompt of ssh and git off Windows (<c>GitExtensions askpass</c>): the answer, or <see langword="null"/> if cancelled.</summary>
    public static string? ShowAskPass(string prompt)
    {
        GitUI.Presentation.HelperDialogs.AskPassViewModel viewModel = new(prompt, ViewStrings.Load<GitUI.Presentation.HelperDialogs.DialogBoxStrings>());
        ShowDialog(() => new GitUI.Avalonia.HelperDialogs.AskPassWindow { DataContext = viewModel }, owner: null);
        return viewModel.Answer;
    }

    /// <summary>As <c>SearchWindow</c>: finds an item by the name typed, from the matches of <paramref name="getCandidates"/>.</summary>
    /// <returns>The chosen item, or <see langword="null"/> if cancelled.</returns>
    public static T? ShowSearch<T>(IWin32Window? owner, Func<string, IEnumerable<T>> getCandidates)
        where T : class
    {
        GitUI.Avalonia.HelperDialogs.SearchWindow? window = null;
        ShowDialog(
            () => window = new GitUI.Avalonia.HelperDialogs.SearchWindow(
                text => getCandidates(text).Cast<object>(),
                ViewStrings.Load<GitUI.Presentation.HelperDialogs.SearchWindowStrings>().EnterFileName.Text),
            owner);
        return window?.SelectedItem as T;
    }

    /// <summary>As <c>FormResetChanges.ShowResetDialog</c>: whether to reset, and to delete the new files too.</summary>
    public static ResetChangesAction ShowResetChanges(IWin32Window? owner, bool hasExistingFiles, bool hasNewFiles, string? confirmationMessage = null)
    {
        ResetChangesViewModel viewModel = new(ViewStrings.Load<ResetChangesStrings>(), hasExistingFiles, hasNewFiles, confirmationMessage);
        ShowDialog(() => new ResetChangesWindow { DataContext = viewModel }, owner);
        return viewModel.SelectedAction;
    }

    public static bool TryShowDeleteTag(IWin32Window? owner, IGitUICommands commands, string? tag, out bool deleted)
    {
        deleted = false;
        IGitModule module = commands.Module;
        IReadOnlyList<string> tags = [.. module.GetRefs(RefsFilter.Tags).Select(r => r.LocalName)];
        IReadOnlyList<string> remotes = [.. new ConfigFileRemoteSettingsManager(() => module).LoadRemotes(false).Select(r => r.Name!)];

        deleted = ShowDialog(
            () =>
            {
                DeleteTagWindow window = new();
                window.DataContext = new DeleteTagViewModel(
                    ViewStrings.Load<DeleteTagStrings>(),
                    tags,
                    tag,
                    remotes,
                    module.GetCurrentRemote(),
                    () => UserManual.UserManual.UrlFor("tag", "delete-tag"),
                    new DeleteTagHost(window, commands));
                return window;
            },
            owner,
            positionName: "FormDeleteTag");
        return true;
    }

    public static bool TryShowInit(IWin32Window? owner, IGitUICommands commands, string dir, EventHandler<GitModuleEventArgs>? gitModuleChanged)
    {
        IList<Repository> history = ThreadHelper.JoinableTaskFactory.Run(RepositoryHistoryManager.Locals.LoadRecentHistoryAsync);

        ShowDialog(
            () =>
            {
                InitWindow window = new();
                window.DataContext = new InitViewModel(
                    ViewStrings.Load<InitStrings>(),
                    [.. history.Select(r => r.Path)],
                    string.IsNullOrEmpty(dir) ? AppSettings.DefaultCloneDestinationPath : dir,
                    TranslatedStrings.Error,
                    new InitRepositoryHost(commands, gitModuleChanged, owner),
                    new MessageBoxService(window),
                    new AvaloniaFileDialogService(window));
                return window;
            },
            owner,
            positionName: "FormInit");
        return true;
    }

    public static bool TryShowGoToLine(IWin32Window? owner, int maxLineNumber, out int? lineNumber)
    {
        lineNumber = null;
        GoToLineViewModel viewModel = new(ViewStrings.Load<GoToLineStrings>(), maxLineNumber);
        if (ShowDialog(() => new GoToLineWindow { DataContext = viewModel }, owner))
        {
            lineNumber = viewModel.LineNumber;
        }

        return true;
    }

    /// <summary>The Avalonia port of <c>SimplePrompt</c>.</summary>
    public static IUserInputPrompt CreateSimplePrompt(string? title, string? label, string? defaultValue)
        => new UserInputPrompt(
            () => new SimplePromptViewModel(title, label, defaultValue),
            () => new SimplePromptWindow(),
            viewModel => ((SimplePromptViewModel)viewModel).UserInput);

    /// <summary>The Avalonia port of <c>FormFilePrompt</c>.</summary>
    public static IUserInputPrompt CreateFilePrompt()
        => new UserInputPrompt(
            window => new FilePromptViewModel(ViewStrings.Load<FilePromptStrings>(), new AvaloniaFileDialogService(window)),
            () => new FilePromptWindow(),
            viewModel => ((FilePromptViewModel)viewModel).UserInput);

    /// <summary>A script input prompt (<see cref="IUserInputPrompt"/>) shown as an Avalonia dialog.</summary>
    private sealed class UserInputPrompt : IUserInputPrompt
    {
        private readonly Func<DialogWindow, DialogViewModel> _createViewModel;
        private readonly Func<DialogWindow> _createWindow;
        private readonly Func<DialogViewModel, string> _getInput;

        public UserInputPrompt(Func<DialogViewModel> createViewModel, Func<DialogWindow> createWindow, Func<DialogViewModel, string> getInput)
            : this(_ => createViewModel(), createWindow, getInput)
        {
        }

        public UserInputPrompt(Func<DialogWindow, DialogViewModel> createViewModel, Func<DialogWindow> createWindow, Func<DialogViewModel, string> getInput)
        {
            _createViewModel = createViewModel;
            _createWindow = createWindow;
            _getInput = getInput;
        }

        public string UserInput { get; private set; } = "";

        public DialogResult ShowDialog(IWin32Window owner)
        {
            DialogViewModel? viewModel = null;
            bool accepted = AvaloniaDialogs.ShowDialog(
                () =>
                {
                    // The view model may need the window (e.g. for file pickers), so the window comes first.
                    DialogWindow window = _createWindow();
                    viewModel = _createViewModel(window);
                    window.DataContext = viewModel;
                    return window;
                },
                owner);

            if (!accepted)
            {
                return DialogResult.Cancel;
            }

            UserInput = _getInput(viewModel!);
            return DialogResult.OK;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>Lets an Avalonia dialog run event scripts, which need a host form.</summary>
    private sealed class ScriptHost(DialogWindow window, IGitUICommands commands) : IGitModuleForm, IScriptOptionsForm, IWin32Window
    {
        public IGitUICommands UICommands => commands;

        public nint Handle => window.OwnerHandle;

        public IScriptOptionsProvider GetScriptOptionsProvider() => ScriptOptionsProviderBase.Default;
    }

    private sealed class DeleteTagHost(DialogWindow window, IGitUICommands commands) : IDeleteTagHost
    {
        public void DeleteLocalTag(string tagName) => commands.Module.DeleteTag(tagName);

        public void DeleteRemoteTag(string remote, string tagName) => AvaloniaUi.RunInHostContext(() =>
        {
            // As in FormDeleteTag.RemoveRemoteTag.
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            ScriptHost scriptHost = new(window, commands);
            if (!scriptsRunner.RunEventScripts(ScriptEvent.BeforePush, scriptHost))
            {
                return;
            }

            RemoteProcessResult result = RunRemoteProcess(new NativeWindowOwner(window), commands, $"push \"{remote}\" :refs/tags/{tagName}");
            if (!commands.Module.InTheMiddleOfAction() && !result.ErrorOccurred)
            {
                scriptsRunner.RunEventScripts(ScriptEvent.AfterPush, scriptHost);
            }
        });

        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));
    }

    private sealed class InitRepositoryHost(IGitUICommands commands, EventHandler<GitModuleEventArgs>? gitModuleChanged, IWin32Window? owner) : IInitRepositoryHost
    {
        public bool FileExists(string path) => File.Exists(path);

        public string Init(string directory, bool central)
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), directory);
            if (!Directory.Exists(module.WorkingDir))
            {
                Directory.CreateDirectory(module.WorkingDir);
            }

            return module.Init(bare: central, shared: central);
        }

        public void OnRepositoryCreated(string directory) => AvaloniaUi.RunInHostContext(() =>
        {
            GitModule module = new(commands.GetRequiredService<IGitExecutorProvider>(), directory);
            gitModuleChanged?.Invoke(owner, new GitModuleEventArgs(module));
            ThreadHelper.JoinableTaskFactory.Run(() => RepositoryHistoryManager.Locals.AddAsMostRecentAsync(directory));
        });
    }

    /// <summary>The command line verbs (<c>_NO_TRANSLATE_commands</c> of <c>FormCommandlineHelp</c>); update it with the verbs of <c>GitUICommands.RunCommand</c>.</summary>
    private const string CommandlineHelpText = """
        [path]
        browse [path] [-filter=] [--pathFilter=<filepath>] [-commit=<selectedSha>[,<firstSha>]]
        about
        add [filename]
        addfiles [filename]
        apply [filename]
        applypatch [filename]
        blame filename
        branch
        checkout
        checkoutbranch
        checkoutrevision
        cherry
        cleanup
        clone [path]
        commit [--quiet] [--message commitmessage]
        difftool filename
        filehistory filename
        fileeditor filename
        formatpatch
        gitignore
        help (shows this dialog)
        init [path]
        merge [--branch name]
        mergeconflicts [--quiet]
        mergetool [--quiet]
        openrepo [path] [-filter=]
        pull [--rebase] [--merge] [--fetch] [--quiet] [--remotebranch name]
        push [--quiet]
        rebase [--branch name]
        remotes
        reset
        revert filename
        searchfile
        settings
        stash
        synchronize [--rebase] [--merge] [--fetch] [--quiet]
        tag
        viewdiff
        viewpatch [filename]
        """;
}

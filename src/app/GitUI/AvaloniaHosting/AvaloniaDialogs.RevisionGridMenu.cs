using GitCommands;
using GitCommands.Git;
using GitCommands.Utils;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Hotkey;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.ScriptsEngine;
using GitUI.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using ResourceManager;
using ResourceManager.Hotkey;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  The context menu of the Avalonia revision grid, as <c>RevisionGridControl.ContextMenuOpening</c> fills
    ///  <c>mainContextMenu</c> for the selected revisions, with the handlers of its items.
    /// </summary>
    internal sealed class RevisionGridMenuBuilder(GitUICommands commands, Func<IWin32Window> owner, RevisionGridViewModel grid, Action refresh)
    {
        private readonly RevisionGridMenuStrings _s = ViewStrings.Load<RevisionGridMenuStrings>();
        private GitRevision? _baseCommitToCompare;
        private IReadOnlyList<string> _customDiffTools = [];
        private bool _customDiffToolsRequested;

        /// <summary>Selects a reference in the left panel (<c>SelectInLeftPanel</c>), if there is one.</summary>
        public Action<string>? SelectInLeftPanel { get; set; }

        /// <summary>The filter of the grid (the main window's), which the branch and reflog items of the View menu change.</summary>
        public IRevisionGridFilterHost? Filter { get; init; }

        private IGitModule Module => commands.Module;

        public IReadOnlyList<MenuModelItem> Build()
        {
            // As RevisionGridControl.LoadCustomDifftools: the difftools of the submenu, read once (after a delay, not
            // while git log runs); shown from the next menu.
            if (!_customDiffToolsRequested)
            {
                _customDiffToolsRequested = true;
                ThreadHelper.FileAndForget(async () => _customDiffTools = await LoadCustomDiffToolsAsync(Module, CustomDiffToolsDelay, CancellationToken.None));
            }

            // As ContextMenuOpening: the menu of a right-clicked reference label is focused on it: its actions only, the
            // advanced items under "Other actions" (unless Shift or AlwaysShowAdvOpt; Ctrl overrides them).
            RevisionGridRefMenuRequest? request = grid.RefMenuRequest;
            grid.RefMenuRequest = null;
            IGitRef? clickedRef = request?.GitRef;
            bool focused = request is not null && (request.Control || !(request.Shift || AppSettings.AlwaysShowAdvOpt));
            Func<IEnumerable<IGitRef>, IEnumerable<IGitRef>> filterRefs = clickedRef is null
                ? refs => refs
                : refs => refs.Where(r => r == clickedRef);

            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            if (selected.Count == 0)
            {
                return [];
            }

            GitRevision revision = selected[0];
            GitRefListsForRevision refs = new(revision);
            IReadOnlyCollection<string> ambiguousRefs = GitRef.GetAmbiguousRefNames(Module.GetRefs(RefsFilter.NoFilter));
            string currentBranch = Module.GetSelectedBranch();
            string currentBranchRef = GitRefName.RefsHeadsPrefix + currentBranch;
            bool bare = Module.IsBareRepository();
            bool bareOrArtificial = bare || revision.IsArtificial;
            bool bisect = Module.InTheMiddleOfBisect();

            List<MenuModelItem> deleteTags = [];
            List<MenuModelItem> deleteBranches = [];
            List<MenuModelItem> checkoutBranches = [];
            List<MenuModelItem> mergeBranches = [];
            List<MenuModelItem> pushBranches = [];
            List<MenuModelItem> renameBranches = [];
            List<string> selectInLeftPanel = [];
            string? rebaseOnTopOf = null;

            foreach (IGitRef tag in filterRefs(refs.AllTags))
            {
                deleteTags.Add(RefItem(tag, () => commands.StartDeleteTagDialog(owner(), tag.Name)));
                selectInLeftPanel.Add(tag.Name);
                string name = UnambiguousName(tag);
                mergeBranches.Add(RefItem(tag, () => commands.StartMergeBranchDialog(owner(), name)));
            }

            bool currentBranchPointsToRevision = false;
            foreach (IGitRef head in filterRefs(refs.BranchesWithNoIdenticalRemotes))
            {
                if (head.CompleteName == currentBranchRef)
                {
                    currentBranchPointsToRevision = !revision.IsArtificial;
                }
                else
                {
                    string name = UnambiguousName(head);
                    mergeBranches.Add(RefItem(head, () => commands.StartMergeBranchDialog(owner(), name)));
                    rebaseOnTopOf ??= name;
                }
            }

            // Without a branch to rebase on or to merge, the selected commit itself.
            if (rebaseOnTopOf is null && !currentBranchPointsToRevision)
            {
                rebaseOnTopOf = revision.Guid;
            }

            if (mergeBranches.Count == 0 && !currentBranchPointsToRevision)
            {
                mergeBranches.Add(new MenuModelItem(Escape(revision.Guid), () => Run(() => commands.StartMergeBranchDialog(owner(), revision.Guid))));
            }

            bool isHeadOfCurrentBranch = false;
            bool separatorBeforeRemotes = false;
            IReadOnlyList<IGitRef> allBranches = [.. filterRefs(refs.AllBranches)];
            foreach (IGitRef head in allBranches)
            {
                selectInLeftPanel.Add(head.Name);
                if (!head.IsRemote)
                {
                    if (head.CompleteName == currentBranchRef)
                    {
                        isHeadOfCurrentBranch = true;
                    }
                    else
                    {
                        deleteBranches.Add(RefItem(head, () => commands.StartDeleteBranchDialog(owner(), head.Name)));
                    }

                    renameBranches.Add(RefItem(head, () => commands.StartRenameDialog(owner(), head.Name)));
                    pushBranches.Add(RefItem(head, () => commands.StartPushDialog(owner(), pushOnShow: false, forceWithLease: false, out _, head.Name)));
                }

                if (head.CompleteName != currentBranchRef)
                {
                    if (!head.IsRemote)
                    {
                        separatorBeforeRemotes = true;
                    }
                    else if (separatorBeforeRemotes)
                    {
                        checkoutBranches.Add(MenuModelItem.Separator);
                        separatorBeforeRemotes = false;
                    }

                    checkoutBranches.Add(RefItem(head, () =>
                    {
                        if (head.IsRemote)
                        {
                            commands.StartCheckoutRemoteBranch(owner(), head.Name);
                        }
                        else
                        {
                            commands.StartCheckoutBranch(owner(), head.Name);
                        }
                    }));
                }
            }

            bool firstRemote = true;
            foreach (IGitRef head in allBranches.Where(b => b.IsRemote))
            {
                if (firstRemote && deleteBranches.Count > 0)
                {
                    deleteBranches.Add(MenuModelItem.Separator);
                }

                firstRemote = false;
                deleteBranches.Add(RefItem(head, () => commands.StartDeleteRemoteBranchDialog(owner(), head.Name)));
            }

            bool isStash = !bareOrArtificial && revision.IsStash;
            const bool top = false;
            const bool advanced = true;

            // The items with whether they are advanced (_contextMenuItems).
            List<(MenuModelItem? Item, bool Advanced)> items =
            [
                (If(revision.IsArtificial, Item(_s.ResetChanges, "ResetWorkingDirChanges", ResetChanges)), top),
                (If(revision.IsArtificial, Item(_s.Commit, "RepoStateDirty", () => commands.StartCommitDialog(owner()))), top),
                (If(bisect, Item(_s.MarkRevisionAsBad, null, () => ContinueBisect(GitBisectOption.Bad, revision))), top),
                (If(bisect, Item(_s.MarkRevisionAsGood, null, () => ContinueBisect(GitBisectOption.Good, revision))), top),
                (If(bisect, Item(_s.BisectSkipRevision, null, () => ContinueBisect(GitBisectOption.Skip, revision))), top),
                (If(bisect, Item(_s.StopBisect, null, StopBisect)), top),
                (MenuModelItem.Separator, top),
                (If(!revision.IsArtificial, new MenuModelItem(_s.CopyToClipboard.AccessKeyText, Icon: "CopyToClipboard", Children: CreateCopyItems(selected, clickedRef?.Name))), top),
                (MenuModelItem.Separator, top),
                (If(isStash || (!bareOrArtificial && revision.IsAutostash), Item(_s.ApplyStash, "Stash", () => StashApply(revision))), top),
                (If(isStash, Item(_s.PopStash, "Stash", () => StashPop(revision))), top),
                (If(isStash, Item(_s.DropStash, "Stash", () => StashDrop(revision))), top),
                (MenuModelItem.Separator, top),
                (If(!bareOrArtificial && checkoutBranches.Any(i => !i.IsSeparator), SubMenu(_s.CheckoutBranch, "BranchCheckout", checkoutBranches)), top),
                (If(!bareOrArtificial && pushBranches.Count > 0, SubMenu(_s.TsmiPushBranch, "Push", pushBranches)), top),
                (If(!bareOrArtificial && mergeBranches.Count > 0, SubMenu(_s.MergeBranch, "Merge", mergeBranches)), top),
                (If(!bareOrArtificial, SubMenu(_s.RebaseOn, "Rebase", CreateRebaseItems(rebaseOnTopOf, grid.GetSelectedRevisionsLatestSelectedFirst()))), top),
                (If(!bareOrArtificial, Item(_s.ResetCurrentBranchToHere, "ResetCurrentBranchToHere", () => ResetCurrentBranch(revision))), top),
                (MenuModelItem.Separator, top),
                (If(SelectInLeftPanel is not null && selectInLeftPanel.Count > 0, SelectInLeftPanelItem(selectInLeftPanel)), top),
                (If(!bareOrArtificial, Item(_s.CreateNewBranch, "BranchCreate", () => CreateBranch(revision)) with { Gesture = GetBrowseGesture(BrowseHotkeyCommand.CreateBranch) }), top),
                (If(!bareOrArtificial, Item(_s.ResetAnotherBranchToHere, "ResetAnotherBranchToHere", () => ResetAnotherBranch(revision))), advanced),
                (If(renameBranches.Count > 0, SubMenu(_s.RenameBranch, "Renamed", renameBranches)), top),
                (If((deleteBranches.Count > 0 && !bare) || isHeadOfCurrentBranch, SubMenu(_s.DeleteBranch, "BranchDelete", deleteBranches, enabled: deleteBranches.Count > 0 && !bare)), top),
                (MenuModelItem.Separator, top),
                (If(!revision.IsArtificial, Item(_s.CreateTag, "TagCreate", () => CreateTag(revision)) with { Gesture = GetBrowseGesture(BrowseHotkeyCommand.CreateTag) }), advanced),
                (If(deleteTags.Count > 0, SubMenu(_s.DeleteTag, "TagDelete", deleteTags)), top),
                (MenuModelItem.Separator, advanced),
                (If(!bareOrArtificial, Item(_s.CheckoutRevision, "Checkout", () => commands.StartCheckoutRevisionDialog(owner(), revision.Guid))), advanced),
                (If(!bareOrArtificial, Item(_s.RevertCommit, "RevertCommit", RevertCommits)), advanced),
                (If(!bareOrArtificial, Item(_s.CherryPickCommit, "CherryPick", () => commands.StartCherryPickDialog(owner(), grid.GetSelectedRevisions(descending: true)))), advanced),
                (If(!revision.IsArtificial, Item(_s.ArchiveRevision, "ArchiveRevision", Archive)), advanced),
                (If(!bareOrArtificial, SubMenu(_s.ManipulateCommit, "Advanced", CreateManipulateItems(revision))), advanced),
                (MenuModelItem.Separator, top),
                (SubMenu(_s.Compare, "Diff", CreateCompareItems()), top),
                (MenuModelItem.Separator, advanced),
                (SubMenu(_s.Navigate, "GotoCommit", CreateNavigateItems()), advanced),
                (SubMenu(_s.View, "AdvancedSettings", CreateViewItems()), advanced),
                .. CreateScriptItems().Select(item => ((MenuModelItem?)item, top)),
                (If(!string.IsNullOrWhiteSpace(revision.BuildStatus?.Url), Item(_s.OpenBuildReport, "Integration", () => OsShellUtil.OpenUrlInDefaultBrowser(revision.BuildStatus!.Url!))), advanced),
                (If(!string.IsNullOrWhiteSpace(revision.BuildStatus?.PullRequestUrl), Item(_s.OpenPullRequestPage, "PullRequest", () => OsShellUtil.OpenUrlInDefaultBrowser(revision.BuildStatus!.PullRequestUrl!))), advanced),
            ];

            List<MenuModelItem> main = [];
            List<MenuModelItem> otherActions = [];
            foreach ((MenuModelItem? item, bool isAdvanced) in items)
            {
                if (item is not null)
                {
                    (focused && isAdvanced ? otherActions : main).Add(item);
                }
            }

            if (focused && MenuModelItem.TrimSeparators(otherActions) is { Count: > 0 } other)
            {
                main.Add(new MenuModelItem(_s.TsmiOtherActions.AccessKeyText, Children: other));
            }

            return MenuModelItem.TrimSeparators(main);

            string UnambiguousName(IGitRef gitRef) => ambiguousRefs.Contains(gitRef.Name) ? gitRef.CompleteName : gitRef.Name;
        }

        // As PerformFirstDropdownItemClick: a submenu of one item; its parent also runs it.
        private static MenuModelItem SubMenu(TranslatedText text, string icon, IReadOnlyList<MenuModelItem> children, bool enabled = true)
            => new(text.AccessKeyText, Icon: icon, Children: children, IsEnabled: enabled);

        private MenuModelItem Item(TranslatedText text, string? icon, Action action) => new(text.AccessKeyText, () => Run(action), icon);

        private MenuModelItem RefItem(IGitRef gitRef, Action action) => new(Escape(gitRef.Name), () => Run(action));

        private static MenuModelItem? If(bool condition, MenuModelItem item) => condition ? item : null;

        /// <summary>A text shown as is: an underscore is not an access key.</summary>
        private static string Escape(string text) => text.Replace("_", "__");

        private void Run(Action action)
        {
            AvaloniaUi.RunInHostContext(action);
            refresh();
        }

        private MenuModelItem SelectInLeftPanelItem(IReadOnlyList<string> refNames)
            => refNames.Count == 1
                ? new MenuModelItem(_s.TsmiSelectInLeftPanel.AccessKeyText, () => SelectInLeftPanel!(refNames[0]), "FileTree")
                : new MenuModelItem(_s.TsmiSelectInLeftPanel.AccessKeyText, Icon: "FileTree", Children: [.. refNames.Select(name => new MenuModelItem(Escape(name), () => SelectInLeftPanel!(name)))]);

        // As CopyContextMenuItem.OnDropDownOpening; only the name of the clicked reference label, if any (SetFilterRefsFunc).
        private static IReadOnlyList<MenuModelItem> CreateCopyItems(IReadOnlyList<GitRevision> revisions, string? clickedRefName = null)
        {
            List<MenuModelItem> items = [];
            List<string> branchNames = [];
            List<string> tagNames = [];
            foreach (GitRevision revision in revisions)
            {
                GitRefListsForRevision refLists = new(revision);
                branchNames.AddRange(refLists.GetAllBranchNames().Where(name => clickedRefName is null || name == clickedRefName));
                tagNames.AddRange(refLists.GetAllTagNames().Where(name => clickedRefName is null || name == clickedRefName));
            }

            int number = 0;
            AddNames(TranslatedStrings.Branches, branchNames, "Branch");
            AddNames(TranslatedStrings.Tags, tagNames, "Tag");

            int count = revisions.Count;
            Add(ResourceManager.TranslatedStrings.GetCommitHash(count), r => r.Guid, "CommitId", 'C');
            Add(ResourceManager.TranslatedStrings.GetMessage(count), r => r.Body ?? r.Subject, "Message", 'M');
            Add(ResourceManager.TranslatedStrings.GetAuthor(count), r => $"{r.Author} <{r.AuthorEmail}>", "Author", 'A');
            if (count == 1 && revisions[0].AuthorDate == revisions[0].CommitDate)
            {
                Add(ResourceManager.TranslatedStrings.Date, r => r.AuthorDate.ToString(), "Date", 'D');
            }
            else
            {
                Add(ResourceManager.TranslatedStrings.GetAuthorDate(count), r => r.AuthorDate.ToString(), "Date", 'T');
                Add(ResourceManager.TranslatedStrings.GetCommitDate(count), r => r.CommitDate.ToString(), "Date", 'D');
            }

            return items;

            void AddNames(string caption, List<string> names, string icon)
            {
                if (names.Count == 0)
                {
                    return;
                }

                items.Add(new MenuModelItem(caption, IsEnabled: false, IsBold: true));
                foreach (string name in names)
                {
                    // As PrependItemNumber: the first ten have their number as access key.
                    string header = ++number > 10 ? Escape(name) : $"_{number % 10}:   {Escape(name)}";
                    items.Add(new MenuModelItem(header, () => ClipboardUtil.TrySetText(name), icon));
                }

                items.Add(MenuModelItem.Separator);
            }

            void Add(string caption, Func<GitRevision, string> extract, string icon, char hotkey)
            {
                string[] texts = [.. revisions.Select(extract).Distinct()];
                string display = Escape(caption) + ":   " + Escape(texts.Select(t => t.SubstringUntil('\n')).Join(", ").ShortenTo(40));
                int position = display.IndexOf(hotkey.ToString(), StringComparison.InvariantCultureIgnoreCase);
                if (position >= 0)
                {
                    display = display.Insert(position, "_");
                }

                string text = texts.Join("\n");
                items.Add(new MenuModelItem(display.TrimEnd('\n', '\r'), () => ClipboardUtil.TrySetText(text), icon));
            }
        }

        // As RebaseOnToolStripMenuItem_DropDownOpening and its items.
        private IReadOnlyList<MenuModelItem> CreateRebaseItems(string? rebaseOnTopOf, IReadOnlyList<GitRevision> selected)
        {
            bool one = rebaseOnTopOf is not null && selected.Count == 1;
            bool advanced = rebaseOnTopOf is not null && (selected.Count == 1 || (selected.Count == 2 && selected.All(r => !r.IsArtificial)));
            return
            [
                new(_s.Rebase.AccessKeyText, () => Rebase(rebaseOnTopOf!, interactive: false), "Rebase", IsEnabled: one),
                new(_s.RebaseInteractively.AccessKeyText, () => Rebase(rebaseOnTopOf!, interactive: true), IsEnabled: one),
                new(_s.RebaseWithAdvOptions.AccessKeyText, () => Run(() => commands.StartRebaseDialogWithAdvOptions(owner(), rebaseOnTopOf!, selected.Count == 2 ? selected[1].ObjectId.ToShortString() : string.Empty)), IsEnabled: advanced),
            ];
        }

        private void Rebase(string onto, bool interactive) => Run(() =>
        {
            if (MessageBoxes.ConfirmSuppressible(owner(), _s.AreYouSureRebase.Text, _s.RebaseConfirmTitle.Text, AppSettings.DontConfirmRebase, heading: interactive ? _s.RebaseBranchInteractive.Text : _s.RebaseBranch.Text))
            {
                if (interactive)
                {
                    commands.StartInteractiveRebase(owner(), onto);
                }
                else
                {
                    commands.StartRebase(owner(), onto);
                }
            }
        });

        private IReadOnlyList<MenuModelItem> CreateManipulateItems(GitRevision revision)
            =>
            [
                new(_s.EditCommit.AccessKeyText, () => LaunchRebase("e", revision)),
                new(_s.RewordCommit.AccessKeyText, () => LaunchRebase("r", revision)),
                MenuModelItem.Separator,
                Item(_s.FixupCommit, null, () => commands.StartFixupCommitDialog(owner(), revision)),
                Item(_s.SquashCommit, null, () => commands.StartSquashCommitDialog(owner(), revision)),
                new(_s.AmendCommit.AccessKeyText, () => Run(() => commands.StartAmendCommitDialog(owner(), revision)), IsEnabled: Module.GitVersion.SupportAmendCommits),
                MenuModelItem.Separator,
                new(_s.GetHelpOnHowToUseTheseFeatures.AccessKeyText, () => OsShellUtil.OpenUrlInDefaultBrowser(UserManual.UserManual.UrlFor("modify_history", "using-autosquash-rebase-feature"))),
            ];

        private IReadOnlyList<MenuModelItem> CreateCompareItems()
            =>
            [
                Item(_s.OpenCommitsWithDiffTool, "Diff", () => DiffSelectedCommitsWithDifftool()) with
                {
                    Children = CreateCustomDiffToolItems(
                        _customDiffTools,
                        tool => Run(() => DiffSelectedCommitsWithDifftool(tool)),
                        () =>
                        {
                            AppSettings.ShowAvailableDiffTools = false;
                            _customDiffTools = [];
                        }),
                },
                MenuModelItem.Separator,
                Item(_s.CompareToBranch, null, CompareToBranch),
                Item(_s.CompareWithCurrentBranch, null, CompareWithCurrentBranch),
                MenuModelItem.Separator,
                new(_s.SelectAsBase.AccessKeyText, () => _baseCommitToCompare = grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault()),
                new(_s.CompareToBase.AccessKeyText, () => Run(CompareToBase), IsEnabled: _baseCommitToCompare is not null),
                Item(_s.CompareToWorkingDirectory, null, CompareToWorkingDirectory),
                Item(_s.CompareSelectedCommits, null, CompareSelectedCommits),
            ];

        // As RevisionGridMenuCommands.CreateNavigateMenuCommands.

        /// <summary>The Navigate submenu, also the Navigate menu of the main window (<c>NavigateMenuCommands</c>).</summary>
        public IReadOnlyList<MenuModelItem> CreateNavigateItems()
            =>
            [
                new(_s.ToggleBetweenArtificialAndHeadCommits.AccessKeyText, ToggleBetweenArtificialAndHead, "WorkingDirChanges", Gesture: GetGesture(RevisionGridCommand.ToggleBetweenArtificialAndHeadCommits)),
                new(_s.GotoCurrentRevision.AccessKeyText, SelectCurrentRevision, "GotoCurrentRevision", Gesture: GetGesture(RevisionGridCommand.SelectCurrentRevision)),
                new(_s.GotoCommit.AccessKeyText, GotoCommit, "GotoCommit", Gesture: GetGesture(RevisionGridCommand.GoToCommit)),
                MenuModelItem.Separator,
                new(_s.GotoChildCommit.AccessKeyText, grid.GoToChild, "GoToChildCommit", Gesture: GetGesture(RevisionGridCommand.GoToChild)),
                new(_s.GotoParentCommit.AccessKeyText, grid.GoToParent, "GoToParentCommit", Gesture: GetGesture(RevisionGridCommand.GoToParent)),
                new(_s.GotoFirstParentCommit.AccessKeyText, grid.GoToFirstParent, "GoToFirstParentCommit", Gesture: GetGesture(RevisionGridCommand.GoToFirstParent)),
                new(_s.GotoLastParentCommit.AccessKeyText, grid.GoToLastParent, "GoToLastParentCommit", Gesture: GetGesture(RevisionGridCommand.GoToLastParent)),
                MenuModelItem.Separator,
                new(_s.GotoMergeBaseCommit.AccessKeyText, () => AvaloniaUi.RunInHostContext(GoToMergeBase), "GoToMergeBaseCommit", Gesture: GetGesture(RevisionGridCommand.GoToMergeBase), ToolTip: _s.GotoMergeBaseCommitToolTip.Text),
                MenuModelItem.Separator,
                new(_s.NavigateBackward.AccessKeyText, grid.NavigateBackward, "NavigateBackward", IsEnabled: grid.CanNavigateBackward, Gesture: GetGesture(RevisionGridCommand.NavigateBackward)),
                new(_s.NavigateForward.AccessKeyText, grid.NavigateForward, "NavigateForward", IsEnabled: grid.CanNavigateForward, Gesture: GetGesture(RevisionGridCommand.NavigateForward)),
                MenuModelItem.Separator,
                new(_s.QuickSearch.AccessKeyText, () => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), _s.QuickSearchQuickHelp.Text, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)), ToolTip: _s.QuickSearchToolTip.Text),
                new(_s.PrevQuickSearch.AccessKeyText, () => grid.QuickSearchNext(down: false), Gesture: GetGesture(RevisionGridCommand.PrevQuickSearch)),
                new(_s.NextQuickSearch.AccessKeyText, () => grid.QuickSearchNext(down: true), Gesture: GetGesture(RevisionGridCommand.NextQuickSearch)),
            ];

        // As RevisionGridMenuCommands.CreateViewMenuCommands, for the settings the Avalonia grid shows.

        /// <summary>The View submenu, also the View menu of the main window (<c>ViewMenuCommands</c>).</summary>
        public IReadOnlyList<MenuModelItem> CreateViewItems()
        {
            FilterInfo filter = new();
            RevisionGridFilterState? state = Filter?.State;
            string? highlightGesture = GetGesture(RevisionGridCommand.ToggleHighlightSelectedBranch);
            return
            [
                new(_s.ShowAllBranches.AccessKeyText, () => SetBranchFilter(byBranchFilter: false, currentOnly: false), "BranchLocal", IsChecked: state?.ShowAllBranches ?? filter.IsShowAllBranchesChecked, Gesture: GetGesture(RevisionGridCommand.ShowAllBranches)),
                new(_s.ShowCurrentBranchOnly.AccessKeyText, () => SetBranchFilter(byBranchFilter: false, currentOnly: true), "BranchFilter", IsChecked: state?.ShowCurrentBranchOnly ?? filter.IsShowCurrentBranchOnlyChecked, Gesture: GetGesture(RevisionGridCommand.ShowCurrentBranchOnly)),
                new(_s.ShowFilteredBranches.AccessKeyText, () => SetBranchFilter(byBranchFilter: true, currentOnly: false), "BranchFilter", IsChecked: state?.ShowFilteredBranches ?? filter.IsShowFilteredBranchesChecked, Gesture: GetGesture(RevisionGridCommand.ShowFilteredBranches)),
                new(_s.ShowReflogReferences.AccessKeyText, ToggleShowReflogReferences, "Book", IsChecked: state?.ShowReflogReferences ?? AppSettings.ShowReflogReferences, Gesture: GetGesture(RevisionGridCommand.ShowReflogReferences)),
                .. Filter is { } advancedFilter ? (MenuModelItem[])[new(_s.Filter.AccessKeyText, advancedFilter.ShowRevisionFilterDialog, "EditFilter", Gesture: GetGesture(RevisionGridCommand.RevisionFilter))] : [],
                MenuModelItem.Separator,
                new(_s.DrawNonrelativesGray.AccessKeyText, ToggleDrawNonRelativesGray, IsChecked: AppSettings.RevisionGraphDrawNonRelativesGray, Gesture: GetGesture(RevisionGridCommand.ToggleDrawNonRelativesGray)),
                new(_s.HighlightSelectedBranch.AccessKeyText, grid.HighlightSelectedBranch, Gesture: highlightGesture is null ? "Alt+Click" : $"{highlightGesture}, Alt+Click"),
                MenuModelItem.Separator,
                new(_s.ShowArtificialCommits.AccessKeyText, () => ToggleAndReload(() => AppSettings.RevisionGraphShowArtificialCommits = !AppSettings.RevisionGraphShowArtificialCommits), IsChecked: AppSettings.RevisionGraphShowArtificialCommits),
                new(_s.ShowStashes.AccessKeyText, ToggleShowStashes, IsChecked: AppSettings.ShowStashes, Gesture: GetGesture(RevisionGridCommand.ShowStashes)),
                new(_s.ShowGitNotes.AccessKeyText, () => ToggleAndReload(() => AppSettings.ShowGitNotes = !AppSettings.ShowGitNotes), IsChecked: AppSettings.ShowGitNotes, Gesture: GetGesture(RevisionGridCommand.ToggleShowGitNotes)),
                new(_s.ShowSessionCheckpoints.AccessKeyText, () => ToggleAndReload(() => AppSettings.ShowSessionRefs = !AppSettings.ShowSessionRefs), IsChecked: AppSettings.ShowSessionRefs),
                MenuModelItem.Separator,
                new(_s.ShowRemoteBranches.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowRemoteBranches = !AppSettings.ShowRemoteBranches), IsChecked: AppSettings.ShowRemoteBranches, Gesture: GetGesture(RevisionGridCommand.ShowRemoteBranches)),
                new(_s.ShowTags.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowTags = !AppSettings.ShowTags), IsChecked: AppSettings.ShowTags, Gesture: GetGesture(RevisionGridCommand.ToggleShowTags)),
                new(_s.ShowSuperprojectTags.AccessKeyText, () => ToggleAndReload(() => AppSettings.ShowSuperprojectTags = !AppSettings.ShowSuperprojectTags), IsChecked: AppSettings.ShowSuperprojectTags),
                new(_s.ShowSuperprojectRemoteBranches.AccessKeyText, () => ToggleAndReload(() => AppSettings.ShowSuperprojectRemoteBranches = !AppSettings.ShowSuperprojectRemoteBranches), IsChecked: AppSettings.ShowSuperprojectRemoteBranches),
                new(_s.ShowSuperprojectBranches.AccessKeyText, () => ToggleAndReload(() => AppSettings.ShowSuperprojectBranches = !AppSettings.ShowSuperprojectBranches), IsChecked: AppSettings.ShowSuperprojectBranches),
                MenuModelItem.Separator,
                new(_s.ShowBuildStatusIcon.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowBuildStatusIconColumn = !AppSettings.ShowBuildStatusIconColumn), IsChecked: AppSettings.ShowBuildStatusIconColumn),
                new(_s.ShowBuildStatusText.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowBuildStatusTextColumn = !AppSettings.ShowBuildStatusTextColumn), IsChecked: AppSettings.ShowBuildStatusTextColumn),
                new(_s.ShowCommitMessageBody.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowCommitBodyInRevisionGrid = !AppSettings.ShowCommitBodyInRevisionGrid), IsChecked: AppSettings.ShowCommitBodyInRevisionGrid),
                new(_s.ShowAuthorDate.AccessKeyText, () => ToggleDisplay(() => AppSettings.ShowAuthorDate = !AppSettings.ShowAuthorDate), IsChecked: AppSettings.ShowAuthorDate, Gesture: GetGesture(RevisionGridCommand.ToggleAuthorDateCommitDate)),
                new(_s.ShowRelativeDate.AccessKeyText, () => ToggleDisplay(() => AppSettings.RelativeDate = !AppSettings.RelativeDate), IsChecked: AppSettings.RelativeDate, Gesture: GetGesture(RevisionGridCommand.ToggleShowRelativeDate)),
                MenuModelItem.Separator,
                new(_s.ShowRevisionGraphColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowRevisionGridGraphColumn = !AppSettings.ShowRevisionGridGraphColumn), IsChecked: AppSettings.ShowRevisionGridGraphColumn, Gesture: GetGesture(RevisionGridCommand.ToggleRevisionGraph)),
                new(_s.ShowGitNotesColumn.AccessKeyText, ToggleShowGitNotesColumn, IsChecked: AppSettings.ShowGitNotesColumn.Value, Gesture: GetGesture(RevisionGridCommand.ToggleShowGitNotesColumn)),
                new(_s.ShowAuthorAvatarColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowAuthorAvatarColumn = !AppSettings.ShowAuthorAvatarColumn), IsChecked: AppSettings.ShowAuthorAvatarColumn),
                new(_s.ShowAuthorNameColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowAuthorNameColumn = !AppSettings.ShowAuthorNameColumn), IsChecked: AppSettings.ShowAuthorNameColumn),
                new(_s.ShowDateColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowDateColumn = !AppSettings.ShowDateColumn), IsChecked: AppSettings.ShowDateColumn),
                new(_s.ShowIdColumn.AccessKeyText, () => ToggleColumns(() => AppSettings.ShowObjectIdColumn = !AppSettings.ShowObjectIdColumn), IsChecked: AppSettings.ShowObjectIdColumn),
                MenuModelItem.Separator,

                // As ToggleAuthorDateSort and ToggleTopoOrder: the order of git log (RevisionReader), or the default again.
                new(_s.AuthorDateSort.AccessKeyText, () => ToggleSortOrder(RevisionSortOrder.AuthorDate), IsChecked: AppSettings.RevisionSortOrder == RevisionSortOrder.AuthorDate),
                new(_s.TopoOrder.AccessKeyText, () => ToggleSortOrder(RevisionSortOrder.Topology), IsChecked: AppSettings.RevisionSortOrder == RevisionSortOrder.Topology),
                MenuModelItem.Separator,
                new(_s.SaveAsDefault.AccessKeyText, SaveCurrentViewSettingsAsDefault),
            ];

            void ToggleSortOrder(RevisionSortOrder order)
                => ToggleAndReload(() => AppSettings.RevisionSortOrder.Value = AppSettings.RevisionSortOrder != order ? order : RevisionSortOrder.GitDefault);

            void SetBranchFilter(bool byBranchFilter, bool currentOnly)
            {
                if (Filter is not null)
                {
                    // As the handlers of the menu: through the filter of the grid, which the toolbar shows.
                    (currentOnly ? (Action)Filter.ShowCurrentBranchOnly : byBranchFilter ? Filter.ShowFilteredBranches : Filter.ShowAllBranches)();
                    return;
                }

                // As ShowAllBranches, ShowCurrentBranchOnly and ShowFilteredBranches: the settings of the filter of the grid.
                FilterInfo current = new();
                current.ByBranchFilter = byBranchFilter;
                current.ShowCurrentBranchOnly = currentOnly;
                grid.Load(grid.SelectedRow?.ObjectId);
            }
        }

        // The handlers of the View menu and of the hotkeys (the Toggle* methods of RevisionGridControl).

        // As PerformRefreshRevisions after a change of a setting of the revisions shown.
        private void ToggleAndReload(Action change)
        {
            change();
            grid.Load(grid.SelectedRow?.ObjectId);
        }

        // As ToggleShowReflogReferences.
        private void ToggleShowReflogReferences()
        {
            if (Filter is not null)
            {
                Filter.ToggleShowReflogReferences();
                return;
            }

            ToggleAndReload(() => AppSettings.ShowReflogReferences.Value = !AppSettings.ShowReflogReferences);
        }

        // As ToggleShowStashes.
        private void ToggleShowStashes() => ToggleAndReload(() => AppSettings.ShowStashes = !AppSettings.ShowStashes);

        // As ToggleShowGitNotesColumn: the notes are read with the revisions.
        private void ToggleShowGitNotesColumn()
        {
            AppSettings.ShowGitNotesColumn.Value = !AppSettings.ShowGitNotesColumn.Value;
            ApplyColumns(grid);
            grid.Load(grid.SelectedRow?.ObjectId);
        }

        // As ToggleDrawNonRelativesGray.
        private void ToggleDrawNonRelativesGray() => ToggleColumns(() => AppSettings.RevisionGraphDrawNonRelativesGray = !AppSettings.RevisionGraphDrawNonRelativesGray);

        private void ToggleDisplay(Action change)
        {
            change();
            grid.DisplayOptions = GetDisplayOptions();
        }

        private void ToggleColumns(Action change)
        {
            change();
            ApplyColumns(grid);
        }

        /// <summary>
        ///  The text of a hotkey of the main window (<c>FormBrowse</c>) shown by an item of the grid, as the WinForms menu does
        ///  (Create new branch Ctrl+B, Create new tag Ctrl+T).
        /// </summary>
        private string? GetBrowseGesture(BrowseHotkeyCommand command)
            => LoadHotkeys(commands, HotkeyCommands.BrowseSettingsName).FirstOrDefault(hotkey => hotkey.CommandCode == (int)command) is { KeyData: not 0 } hotkey
                ? ((Keys)hotkey.KeyData).ToShortcutKeyDisplayString()
                : null;

        /// <summary>The text of the hotkey of a command of the grid (the <c>ShortcutKeyDisplayString</c> of its menu item).</summary>
        private string? GetGesture(RevisionGridCommand command)
            => grid.GetHotkey(command) is { KeyData: not 0 } hotkey ? ((Keys)hotkey.KeyData).ToShortcutKeyDisplayString() : null;

        /// <summary>
        ///  As <c>RevisionGridControl.ExecuteCommand</c>: the hotkeys of the grid that it does not execute itself (the
        ///  navigation), with the handlers of the menus.
        /// </summary>
        public bool ExecuteCommand(RevisionGridCommand command)
        {
            switch (command)
            {
                case RevisionGridCommand.ToggleRevisionGraph: ToggleColumns(() => AppSettings.ShowRevisionGridGraphColumn = !AppSettings.ShowRevisionGridGraphColumn); break;
                case RevisionGridCommand.RevisionFilter when Filter is not null: Filter.ShowRevisionFilterDialog(); break;
                case RevisionGridCommand.ResetRevisionFilter when Filter is not null: Filter.ResetAllFiltersAndRefresh(); break;
                case RevisionGridCommand.ResetRevisionPathFilter when Filter is not null: Filter.SetAndApplyPathFilter(""); break;
                case RevisionGridCommand.ToggleAuthorDateCommitDate: ToggleDisplay(() => AppSettings.ShowAuthorDate = !AppSettings.ShowAuthorDate); break;
                case RevisionGridCommand.ToggleShowRelativeDate: ToggleDisplay(() => AppSettings.RelativeDate = !AppSettings.RelativeDate); break;
                case RevisionGridCommand.ToggleDrawNonRelativesGray: ToggleDrawNonRelativesGray(); break;
                case RevisionGridCommand.ToggleShowGitNotes: ToggleAndReload(() => AppSettings.ShowGitNotes = !AppSettings.ShowGitNotes); break;
                case RevisionGridCommand.ToggleShowGitNotesColumn: ToggleShowGitNotesColumn(); break;
                case RevisionGridCommand.ToggleHideMergeCommits: ToggleAndReload(() => AppSettings.HideMergeCommits = !AppSettings.HideMergeCommits); break;
                case RevisionGridCommand.ToggleShowTags: ToggleDisplay(() => AppSettings.ShowTags = !AppSettings.ShowTags); break;
                case RevisionGridCommand.ShowAllBranches when Filter is not null: Filter.ShowAllBranches(); break;
                case RevisionGridCommand.ShowCurrentBranchOnly when Filter is not null: Filter.ShowCurrentBranchOnly(); break;
                case RevisionGridCommand.ShowFilteredBranches when Filter is not null: Filter.ShowFilteredBranches(); break;
                case RevisionGridCommand.ShowReflogReferences: ToggleShowReflogReferences(); break;
                case RevisionGridCommand.ShowRemoteBranches: ToggleDisplay(() => AppSettings.ShowRemoteBranches = !AppSettings.ShowRemoteBranches); break;
                case RevisionGridCommand.ShowFirstParent when Filter is not null: Filter.ToggleShowOnlyFirstParent(); break;
                case RevisionGridCommand.ShowStashes: ToggleShowStashes(); break;
                case RevisionGridCommand.ToggleBetweenArtificialAndHeadCommits: ToggleBetweenArtificialAndHead(); break;
                case RevisionGridCommand.SelectCurrentRevision: SelectCurrentRevision(); break;
                case RevisionGridCommand.GoToCommit: GotoCommit(); break;
                case RevisionGridCommand.GoToMergeBase: AvaloniaUi.RunInHostContext(GoToMergeBase); break;
                case RevisionGridCommand.SelectAsBaseToCompare: _baseCommitToCompare = grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault(); break;
                case RevisionGridCommand.CompareToBase: Run(CompareToBase); break;
                case RevisionGridCommand.CreateFixupCommit: RunOnLatest(r => commands.StartFixupCommitDialog(owner(), r)); break;
                case RevisionGridCommand.CreateSquashCommit: RunOnLatest(r => commands.StartSquashCommitDialog(owner(), r)); break;
                case RevisionGridCommand.CreateAmendCommit: RunOnLatest(r => commands.StartAmendCommitDialog(owner(), r)); break;
                case RevisionGridCommand.OpenCommitsWithDifftool: Run(() => DiffSelectedCommitsWithDifftool()); break;
                case RevisionGridCommand.CompareToWorkingDirectory: Run(CompareToWorkingDirectory); break;
                case RevisionGridCommand.CompareToCurrentBranch: Run(CompareWithCurrentBranch); break;
                case RevisionGridCommand.CompareToBranch: Run(CompareToBranch); break;
                case RevisionGridCommand.CompareSelectedCommits: Run(CompareSelectedCommits); break;
                case RevisionGridCommand.DeleteRef: Run(DeleteRef); break;
                case RevisionGridCommand.RenameRef: Run(RenameRef); break;
                default: return false;
            }

            return true;
        }

        private void RunOnLatest(Action<GitRevision> action)
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } revision)
            {
                Run(() => action(revision));
            }
        }

        // As the SelectCurrentRevision command.
        private void SelectCurrentRevision()
        {
            ObjectId currentCheckout = Module.GetCurrentCheckout();
            if (!grid.SelectRevision(currentCheckout))
            {
                AvaloniaUi.RunInHostContext(() => MessageBoxes.RevisionFilteredInGrid(owner(), currentCheckout));
            }
        }

        // As RenameRef.
        private void RenameRef()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } revision)
            {
                InitiateRefAction(
                    new GitRefListsForRevision(revision).GetRenameableLocalBranches(),
                    gitRef => commands.StartRenameDialog(owner(), gitRef.Name),
                    QuickRefAction.Rename);
            }
        }

        // As DeleteRef.
        private void DeleteRef()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is not { } revision)
            {
                return;
            }

            InitiateRefAction(
                new GitRefListsForRevision(revision).GetDeletableRefs(Module.GetSelectedBranch(emptyIfDetached: true)),
                gitRef =>
                {
                    if (gitRef.IsTag)
                    {
                        commands.StartDeleteTagDialog(owner(), gitRef.Name);
                    }
                    else if (gitRef.IsRemote)
                    {
                        commands.StartDeleteRemoteBranchDialog(owner(), gitRef.Name);
                    }
                    else
                    {
                        commands.StartDeleteBranchDialog(owner(), gitRef.Name);
                    }
                },
                QuickRefAction.Delete);
        }

        // As InitiateRefAction: the action on the only reference, else on the one chosen in the quick selector.
        private void InitiateRefAction(IReadOnlyList<IGitRef>? gitRefs, Action<IGitRef> action, QuickRefAction actionLabel)
        {
            if (gitRefs?.Count is not > 0)
            {
                return;
            }

            if (gitRefs.Count == 1)
            {
                action(gitRefs[0]);
                return;
            }

            // At the mouse pointer on Windows; elsewhere let Avalonia position the picker over its owner.
            IWin32Window window = owner();
            Point? location = GetCursorPosition();
            if (TryShowQuickRefSelector(window, actionLabel, gitRefs, location, out IGitRef? selectedRef) && selectedRef is not null)
            {
                action(selectedRef);
            }
        }

        // As AddUserScripts: the scripts shown in the grid, then the others under "Run script".
        private IEnumerable<MenuModelItem> CreateScriptItems()
        {
            IScriptsManager scriptsManager = commands.GetRequiredService<IScriptsManager>();
            List<ScriptInfo> scripts = [.. scriptsManager.GetScripts().Where(script => script.Enabled)];
            List<MenuModelItem> direct = [];
            List<MenuModelItem> others = [];
            foreach (ScriptInfo script in scripts)
            {
                MenuModelItem item = new(script.Name ?? "", () => RunScript(script), script.GetIcon());
                (script.AddToRevisionGridContextMenu ? direct : others).Add(item);
            }

            if (others.Count > 0)
            {
                direct.Insert(0, new MenuModelItem(_s.RunScript.AccessKeyText, Icon: "Console", Children: others));
            }

            return direct;
        }

        private void RunScript(ScriptInfo script) => Run(() =>
        {
            IScriptsRunner scriptsRunner = commands.GetRequiredService<IScriptsRunner>();
            scriptsRunner.RunScript(script, owner(), commands, new ScriptOptionsProvider(() => [], () => null, () => null));
        });

        private void ContinueBisect(GitBisectOption option, GitRevision revision)
            => ProcessDialogs.ShowProcess(owner(), commands, arguments: Commands.ContinueBisect(option, revision.ObjectId), Module.WorkingDir, input: null, useDialogSettings: false);

        private void StopBisect()
            => ProcessDialogs.ShowProcess(owner(), commands, arguments: Commands.StopBisect(), Module.WorkingDir, input: null, useDialogSettings: true);

        private void ResetChanges()
            => commands.StartResetChangesDialog(owner(), Module.GetWorkTreeFiles(), onlyWorkTree: grid.SelectedRow?.ObjectId == ObjectId.WorkTreeId);

        private void StashApply(GitRevision revision) => commands.StashApply(owner(), revision.ObjectId.ToString());

        private void StashPop(GitRevision revision)
        {
            if (!string.IsNullOrEmpty(revision.ReflogSelector))
            {
                commands.StashPop(owner(), revision.ReflogSelector);
            }
        }

        // As DropStashToolStripMenuItemClick.
        private void StashDrop(GitRevision revision)
        {
            if (string.IsNullOrEmpty(revision.ReflogSelector))
            {
                return;
            }

            TaskDialogButton result = TaskDialogButton.Yes;
            if (!AppSettings.DontConfirmStashDrop)
            {
                TaskDialogPage page = new()
                {
                    Text = TranslatedStrings.AreYouSure,
                    Caption = TranslatedStrings.StashDropConfirmTitle,
                    Heading = TranslatedStrings.CannotBeUndone,
                    Buttons = { TaskDialogButton.Yes, TaskDialogButton.No },
                    Icon = TaskDialogIcon.Information,
                    Verification = new TaskDialogVerificationCheckBox { Text = TranslatedStrings.DontShowAgain },
                    SizeToContent = true,
                };
                result = TaskDialog.ShowDialog(owner().Handle, page);
                if (page.Verification.Checked)
                {
                    AppSettings.DontConfirmStashDrop = true;
                }
            }

            if (result == TaskDialogButton.Yes)
            {
                commands.StashDrop(owner(), revision.ReflogSelector);
            }
        }

        private void ResetCurrentBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                return TryShowResetCurrentBranch(owner(), commands, revision, ResetCurrentBranchType.Soft, out bool reset) && reset;
            });

        private void ResetAnotherBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                return TryShowResetAnotherBranch(owner(), commands, revision, out bool reset) && reset;
            });

        private void CreateBranch(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                return TryShowCreateBranch(owner(), commands, revision.ObjectId, new(BranchName: null), out bool created) && created;
            });

        private void CreateTag(GitRevision revision)
            => commands.DoActionOnRepo(() =>
            {
                return TryShowCreateTag(owner(), commands, revision.ObjectId, out bool created) && created;
            });

        private void RevertCommits()
        {
            foreach (GitRevision revision in grid.GetSelectedRevisions(descending: false))
            {
                commands.StartRevertCommitDialog(owner(), revision);
            }
        }

        // As ArchiveRevisionToolStripMenuItemClick.
        private void Archive()
        {
            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            if (selected.Count > 2)
            {
                MessageBoxes.Show(owner(), "Select only one or two revisions. Abort.", "Archive revision", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            commands.StartArchiveDialog(owner(), selected.Count > 0 ? selected[0] : null, selected.Count == 2 ? selected[1] : null);
        }

        // As LaunchRebase: an interactive rebase editing (e) or rewording (r) the first commit.
        private void LaunchRebase(string command, GitRevision revision) => Run(() =>
        {
            string rebaseCmd = Commands.Rebase(new Commands.RebaseOptions
            {
                BranchName = revision.FirstParentId is { IsZero: false } parent ? parent.ToString() : null,
                Interactive = true,
                AutoStash = true,
                SupportRebaseMerges = Module.GitVersion.SupportRebaseMerges,
            });

            const string sequenceEditor = "GIT_SEQUENCE_EDITOR";
            Dictionary<string, string> environment = new() { [sequenceEditor] = string.Format("sed -i -re '0,/pick/s//{0}/'", command) };
            environment.ForwardEnvironmentVariableToWsl(Module.WorkingDir, sequenceEditor);
            TryShowProcess(owner(), commands, rebaseCmd, Module.WorkingDir, input: null, useDialogSettings: true, process: null, out _, out _, environment);
        });

        // As GetFirstAndSelected.
        private (ObjectId FirstId, GitRevision? Selected) GetFirstAndSelected()
        {
            IReadOnlyList<GitRevision> revisions = grid.GetSelectedRevisionsLatestSelectedFirst();
            return revisions.Count switch
            {
                0 => (default, null),
                1 => (revisions[0].FirstParentId, revisions[0]),
                _ => (revisions[^1].ObjectId, revisions[0]),
            };
        }

        private void DiffSelectedCommitsWithDifftool(string? customTool = null)
        {
            (ObjectId first, GitRevision? selected) = GetFirstAndSelected();
            if (selected is not null)
            {
                Module.OpenWithDifftoolDirDiff(first.IsZero ? null : first.ToString(), selected.ObjectId.ToString(), customTool: customTool);
            }
        }

        private void CompareToBranch()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is not { } head)
            {
                return;
            }

            TryShowCompareToBranch(owner(), commands, head.ObjectId, out string? branchName);

            if (branchName is not null)
            {
                ObjectId baseCommit = Module.RevParse(branchName);
                if (baseCommit.IsZero)
                {
                    MessageBoxes.ShowError(owner(), _s.NoRevisionFoundError.Text);
                    return;
                }

                ShowFormDiff(baseCommit, head.ObjectId, branchName, head.Subject);
            }
        }

        private void CompareWithCurrentBranch()
        {
            string branch = Module.GetSelectedBranch(emptyIfDetached: true);
            ObjectId checkout = Module.GetCurrentCheckout();
            if (string.IsNullOrWhiteSpace(branch) || checkout.IsZero)
            {
                MessageBoxes.Show(owner(), "No branch is currently selected", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } baseCommit)
            {
                ShowFormDiff(baseCommit.ObjectId, checkout, baseCommit.Subject, branch);
            }
        }

        private void CompareToBase()
        {
            if (_baseCommitToCompare is null)
            {
                MessageBoxes.Show(owner(), _s.BaseForCompareNotSelectedError.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is { } head)
            {
                ShowFormDiff(_baseCommitToCompare.ObjectId, head.ObjectId, _baseCommitToCompare.Subject, head.Subject);
            }
        }

        private void CompareToWorkingDirectory()
        {
            if (grid.GetSelectedRevisionsLatestSelectedFirst().FirstOrDefault() is not { } baseCommit)
            {
                return;
            }

            if (baseCommit.ObjectId == ObjectId.WorkTreeId)
            {
                MessageBoxes.Show(owner(), "Cannot diff working directory to itself", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ShowFormDiff(baseCommit.ObjectId, ObjectId.WorkTreeId, baseCommit.Subject, "Working directory");
        }

        private void CompareSelectedCommits()
        {
            (ObjectId firstId, GitRevision? selected) = GetFirstAndSelected();
            if (selected is not null && !firstId.IsZero)
            {
                ShowFormDiff(firstId, selected.ObjectId, grid.GetRevision(firstId)?.Subject ?? "", selected.Subject);
            }
            else
            {
                MessageBoxes.Show(owner(), "You must have two commits selected to compare", TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowFormDiff(ObjectId baseCommit, ObjectId headCommit, string baseDisplay, string headDisplay)
        {
            TryShowDiff(commands, baseCommit, headCommit, baseDisplay, headDisplay);
        }

        // As ToggleBetweenArtificialAndHeadCommits: working directory -> index -> HEAD.
        private void ToggleBetweenArtificialAndHead()
        {
            ObjectId current = grid.SelectedRow?.ObjectId ?? default;
            ObjectId next = current == ObjectId.WorkTreeId ? ObjectId.IndexId : current == ObjectId.IndexId ? Module.GetCurrentCheckout() : ObjectId.WorkTreeId;
            if (!grid.SelectRevision(next))
            {
                grid.SelectRevision(Module.GetCurrentCheckout());
            }
        }

        private void GotoCommit() => AvaloniaUi.RunInHostContext(() =>
        {
            if (TryShowGoToCommit(owner(), commands, out bool accepted, out ObjectId commitId) && accepted && !grid.SelectRevision(commitId))
            {
                MessageBoxes.RevisionFilteredInGrid(owner(), commitId);
            }
        });

        // As GoToMergeBase.
        private void GoToMergeBase()
        {
            IReadOnlyList<GitRevision> selected = grid.GetSelectedRevisionsLatestSelectedFirst();
            List<ObjectId> revisions = [.. selected.Select(r => r.ObjectId).Where(id => !id.IsArtificial)];
            bool hasArtificial = selected.Any(r => r.IsArtificial);
            ObjectId headId = Module.RevParse("HEAD");
            if (headId.IsZero || (revisions.Count == 0 && !hasArtificial))
            {
                return;
            }

            GitArgumentBuilder args = new("merge-base")
            {
                { revisions.Count > 2 || (revisions.Count == 2 && hasArtificial), "--octopus" },
                { revisions.Count < 1, headId.ToString() },
                { revisions.Count < 2, headId.ToString() },
                revisions
            };

            ExecutionResult result = Module.GitExecutable.Execute(args, throwOnErrorExit: false);
            const int noCommonAncestorsExitCode = 1;
            if (result.ExitCode == noCommonAncestorsExitCode || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                MessageBoxes.ShowError(owner(), _s.NoMergeBaseCommit.Text);
                return;
            }

            result.ThrowIfErrorExit();
            ObjectId commitId = ObjectId.Parse(result.StandardOutput.TrimEnd());
            if (!grid.SelectRevision(commitId))
            {
                MessageBoxes.RevisionFilteredInGrid(owner(), commitId);
            }
        }
    }

    /// <summary>The display options of the grid from the settings (as <c>RevisionGridControl</c> reads them).</summary>
    /// <summary>
    ///  As <c>RevisionGridMenuCommands.SaveCurrentViewSettingsAsDefault</c>: the settings changed for the session (the runtime
    ///  settings) are saved.
    /// </summary>
    internal static void SaveCurrentViewSettingsAsDefault()
    {
        foreach (System.Reflection.FieldInfo field in typeof(AppSettings).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static))
        {
            (field.GetValue(null) as GitCommands.Settings.IRuntimeSetting)?.Save();
        }

        AppSettings.SaveSettings();
    }

    internal static RevisionGridDisplayOptions GetDisplayOptions()
        => new(AppSettings.RelativeDate, AppSettings.ShowAuthorDate, TranslatedStrings.SearchingFor, AppSettings.RevisionGridQuickSearchTimeout, AppSettings.ShowRemoteBranches, AppSettings.ShowTags, AppSettings.ShowCommitBodyInRevisionGrid, AppSettings.ShowRevisionGridTooltips.Value);

    /// <summary>The columns of the grid from the settings.</summary>
    internal static void ApplyColumns(RevisionGridViewModel grid)
    {
        grid.ShowGraphColumn = AppSettings.ShowRevisionGridGraphColumn;
        grid.ShowNotesColumn = AppSettings.ShowGitNotesColumn.Value;
        grid.ShowAvatarColumn = AppSettings.ShowAuthorAvatarColumn;
        grid.ShowAuthorColumn = AppSettings.ShowAuthorNameColumn;
        grid.ShowDateColumn = AppSettings.ShowDateColumn;
        grid.ShowIdColumn = AppSettings.ShowObjectIdColumn;
        grid.ShowBuildStatusIcon = AppSettings.ShowBuildStatusIconColumn;
        grid.ShowBuildStatusText = AppSettings.ShowBuildStatusTextColumn;

        // As RevisionDataGridView.ApplySettings and PerformRefreshRevisions (RevisionGraphDrawStyle).
        grid.DrawNonRelativesGray = AppSettings.RevisionGraphDrawNonRelativesGray;
        grid.DrawNonRelativesTextGray = AppSettings.RevisionGraphDrawNonRelativesTextGray;
        grid.HighlightAuthoredRevisions = AppSettings.HighlightAuthoredRevisions;
    }

    /// <summary>The Windows mouse position, or no explicit position when global pointer coordinates are unavailable.</summary>
    private static Point? GetCursorPosition() => OperatingSystem.IsWindows() && GetCursorPos(out Point point) ? point : null;

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);
}

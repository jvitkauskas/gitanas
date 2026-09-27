using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the advanced settings; ids match <c>AdvancedSettingsPage</c>.</summary>
public sealed class AdvancedSettingsPageStrings : ViewStrings
{
    public AdvancedSettingsPageStrings()
        : base("AdvancedSettingsPage")
    {
        Title = Add("$this", "Text", "Advanced");
        Checkout = Add("CheckoutGB", "Text", "Checkout");
        AlwaysShowCheckoutDlg = Add("chkAlwaysShowCheckoutDlg", "Text", "Always show checkout dialog");
        UseLocalChangesAction = Add("chkUseLocalChangesAction", "Text", "Use last chosen \"local changes\" action as default action.\nThis action will be performed without warning while checking out branch.");
        General = Add("GeneralGB", "Text", "General");
        DontShowHelpImages = Add("chkDontSHowHelpImages", "Text", "Don't show help images");
        AlwaysShowAdvOpt = Add("chkAlwaysShowAdvOpt", "Text", "Always show advanced options");
        ConsoleEmulator = Add("chkConsoleEmulator", "Text", "Use Console Emulator for console output in command dialogs");
        ConsoleEmulatorTooltip = Add("chkConsoleEmulator", "ToolTipText", "Controls how console programs output is displayed in command progress dialogs, like Clone or Pull.\n\nIf yes, embeds the fully functional console emulator. This is experimental and might have side-effects.\nIf no, redirects select strings from stdout/stderr into an edit box. This is the classic mode.\n\nTurn off if you experience problems with the console emulator.");
        AutoNormaliseBranchName = Add("chkAutoNormaliseBranchName", "Text", "Auto normalise branch name");
        AutoNormaliseBranchNameTooltip = Add("chkAutoNormaliseBranchName", "ToolTipText", "Controls whether branch name should be automatically normalised as per git branch naming rules.\nIf enabled, any illegal symbols will be replaced with the replacement symbol of your choice.");
        SymbolToUse = Add("label1", "Text", "Symbol to use:");
        Commit = Add("grpCommit", "Text", "Commit");
        CommitAndPushForcedWhenAmend = Add("chkCommitAndPushForcedWhenAmend", "Text", "Push forced with lease when Commit && Push action is performed with Amend option checked");
    }

    public TranslatedText Title { get; }

    public TranslatedText Checkout { get; }

    public TranslatedText AlwaysShowCheckoutDlg { get; }

    public TranslatedText UseLocalChangesAction { get; }

    public TranslatedText General { get; }

    public TranslatedText DontShowHelpImages { get; }

    public TranslatedText AlwaysShowAdvOpt { get; }

    public TranslatedText ConsoleEmulator { get; }

    public TranslatedText ConsoleEmulatorTooltip { get; }

    public TranslatedText AutoNormaliseBranchName { get; }

    public TranslatedText AutoNormaliseBranchNameTooltip { get; }

    public TranslatedText SymbolToUse { get; }

    public TranslatedText Commit { get; }

    public TranslatedText CommitAndPushForcedWhenAmend { get; }
}

/// <summary>Port of <c>AdvancedSettingsPage</c> (global settings).</summary>
public sealed partial class AdvancedSettingsPageViewModel : SettingsPageWithServicesViewModel
{
    public AdvancedSettingsPageViewModel(AdvancedSettingsPageStrings strings, ISettingsPageServices services)
        : base(services)
    {
        Strings = strings;
        AutoNormaliseSymbol = AutoNormaliseSymbols[0];
    }

    public AdvancedSettingsPageStrings Strings { get; }

    public override string Title => Strings.Title.Text;

    public override string PageName => "AdvancedSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    /// <summary>The items of <c>cboAutoNormaliseSymbol</c>.</summary>
    public IReadOnlyList<SettingChoice<string>> AutoNormaliseSymbols { get; } = [new("_", "_"), new("-", "-"), new("", "(none)")];

    [ObservableProperty]
    public partial bool AlwaysShowCheckoutDlg { get; set; }

    [ObservableProperty]
    public partial bool UseLocalChangesAction { get; set; }

    [ObservableProperty]
    public partial bool DontShowHelpImages { get; set; }

    [ObservableProperty]
    public partial bool AlwaysShowAdvOpt { get; set; }

    [ObservableProperty]
    public partial bool UseConsoleEmulator { get; set; }

    /// <summary>As <c>chkAutoNormaliseBranchName</c>, enabling <c>cboAutoNormaliseSymbol</c>.</summary>
    [ObservableProperty]
    public partial bool AutoNormaliseBranchName { get; set; }

    [ObservableProperty]
    public partial SettingChoice<string>? AutoNormaliseSymbol { get; set; }

    [ObservableProperty]
    public partial bool CommitAndPushForcedWhenAmend { get; set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        AlwaysShowCheckoutDlg = AppSettings.AlwaysShowCheckoutBranchDlg;
        UseLocalChangesAction = AppSettings.UseDefaultCheckoutBranchAction;
        DontShowHelpImages = AppSettings.DontShowHelpImages;
        AlwaysShowAdvOpt = AppSettings.AlwaysShowAdvOpt;
        UseConsoleEmulator = AppSettings.UseConsoleEmulatorForCommands.Value;
        AutoNormaliseBranchName = AppSettings.AutoNormaliseBranchName;
        AutoNormaliseSymbol = AutoNormaliseSymbols.FirstOrDefault(s => s.Value == AppSettings.AutoNormaliseSymbol) ?? AutoNormaliseSymbol;
        CommitAndPushForcedWhenAmend = AppSettings.CommitAndPushForcedWhenAmend;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        AppSettings.AlwaysShowCheckoutBranchDlg = AlwaysShowCheckoutDlg;
        AppSettings.UseDefaultCheckoutBranchAction = UseLocalChangesAction;
        AppSettings.DontShowHelpImages = DontShowHelpImages;
        AppSettings.AlwaysShowAdvOpt = AlwaysShowAdvOpt;
        AppSettings.UseConsoleEmulatorForCommands.Value = UseConsoleEmulator;
        AppSettings.AutoNormaliseBranchName = AutoNormaliseBranchName;
        AppSettings.AutoNormaliseSymbol = (AutoNormaliseSymbol ?? AutoNormaliseSymbols[0]).Value;
        AppSettings.CommitAndPushForcedWhenAmend = CommitAndPushForcedWhenAmend;

        base.PageToSettings(settings);
    }
}

/// <summary>Strings of the confirmations settings; ids match <c>ConfirmationsSettingsPage</c>.</summary>
public sealed class ConfirmationsSettingsPageStrings : ViewStrings
{
    public ConfirmationsSettingsPageStrings()
        : base("ConfirmationsSettingsPage")
    {
        Title = Add("$this", "Text", "Confirmations");
        ConfirmActions = Add("gbConfirmations", "Text", "Confirm actions");
        GroupCommits = Add("lblGroupCommits", "Text", "Commits:");
        Amend = Add("chkAmend", "Text", "Amend last commit");
        UndoLastCommit = Add("chkUndoLastCommitConfirmation", "Text", "Undo last commit");
        CommitIfNoBranch = Add("chkCommitIfNoBranch", "Text", "Commit when no branch is currently checked out (headless state)");
        RebaseOnTopOfSelectedCommit = Add("chkRebaseOnTopOfSelectedCommit", "Text", "Rebase on top of selected commit");
        GroupBranches = Add("lblGroupBranches", "Text", "Branches:");
        FetchAndPruneAll = Add("chkFetchAndPruneAllConfirmation", "Text", "Fetch and prune branches");
        PushNewBranch = Add("chkPushNewBranch", "Text", "Push a new branch for the remote");
        AddTrackingRef = Add("chkAddTrackingRef", "Text", "Add a tracking reference for newly pushed branch");
        BranchDeleteUnmerged = Add("chkBranchDeleteUnmerged", "Text", "Delete unmerged branches");
        BranchCheckout = Add("chkBranchCheckoutConfirmation", "Text", "Checkout branch using left panel");
        GroupStashes = Add("lblGroupStashes", "Text", "Stash:");
        AutoPopStashAfterCheckout = Add("chkAutoPopStashAfterCheckout", "Text", "Apply stashed changes after successful checkout (else stash will be popped automatically)");
        AutoPopStashAfterPull = Add("chkAutoPopStashAfterPull", "Text", "Apply stashed changes after successful pull (else stash will be popped automatically)");
        ConfirmStashDrop = Add("chkConfirmStashDrop", "Text", "Drop stash");
        GroupConflictResolution = Add("lblGroupConflictResolution", "Text", "Rebase / conflict resolution:");
        ResolveConflicts = Add("chkResolveConflicts", "Text", "Resolve conflicts");
        CommitAfterConflictsResolved = Add("chkCommitAfterConflictsResolved", "Text", "Commit changes after conflicts have been resolved");
        SecondAbortConfirmation = Add("chkSecondAbortConfirmation", "Text", "Confirm for the second time to abort a merge");
        GroupSubmodules = Add("lblGroupSubmodules", "Text", "Submodules:");
        UpdateModules = Add("chkUpdateModules", "Text", "Update submodules on checkout");
        GroupWorktrees = Add("lblGroupWorktrees", "Text", "Worktrees:");
        SwitchWorktree = Add("chkSwitchWorktree", "Text", "Switch worktree");
    }

    public TranslatedText Title { get; }

    public TranslatedText ConfirmActions { get; }

    public TranslatedText GroupCommits { get; }

    public TranslatedText Amend { get; }

    public TranslatedText UndoLastCommit { get; }

    public TranslatedText CommitIfNoBranch { get; }

    public TranslatedText RebaseOnTopOfSelectedCommit { get; }

    public TranslatedText GroupBranches { get; }

    public TranslatedText FetchAndPruneAll { get; }

    public TranslatedText PushNewBranch { get; }

    public TranslatedText AddTrackingRef { get; }

    public TranslatedText BranchDeleteUnmerged { get; }

    public TranslatedText BranchCheckout { get; }

    public TranslatedText GroupStashes { get; }

    public TranslatedText AutoPopStashAfterCheckout { get; }

    public TranslatedText AutoPopStashAfterPull { get; }

    public TranslatedText ConfirmStashDrop { get; }

    public TranslatedText GroupConflictResolution { get; }

    public TranslatedText ResolveConflicts { get; }

    public TranslatedText CommitAfterConflictsResolved { get; }

    public TranslatedText SecondAbortConfirmation { get; }

    public TranslatedText GroupSubmodules { get; }

    public TranslatedText UpdateModules { get; }

    public TranslatedText GroupWorktrees { get; }

    public TranslatedText SwitchWorktree { get; }
}

/// <summary>
///  Port of <c>ConfirmationsSettingsPage</c> (global settings): a checked box asks for a confirmation; the three-state
///  boxes of a setting that can be unset ask when indeterminate.
/// </summary>
public sealed partial class ConfirmationsSettingsPageViewModel(ConfirmationsSettingsPageStrings strings) : SettingsPageViewModel
{
    public ConfirmationsSettingsPageStrings Strings { get; } = strings;

    public override string Title => Strings.Title.Text;

    public override string PageName => "ConfirmationsSettingsPage";

    public override IEnumerable<string> SearchKeywords => Strings.Texts;

    [ObservableProperty]
    public partial bool Amend { get; set; }

    [ObservableProperty]
    public partial bool UndoLastCommit { get; set; }

    [ObservableProperty]
    public partial bool CommitIfNoBranch { get; set; }

    [ObservableProperty]
    public partial bool RebaseOnTopOfSelectedCommit { get; set; }

    [ObservableProperty]
    public partial bool FetchAndPruneAll { get; set; }

    [ObservableProperty]
    public partial bool PushNewBranch { get; set; }

    [ObservableProperty]
    public partial bool AddTrackingRef { get; set; }

    [ObservableProperty]
    public partial bool BranchDeleteUnmerged { get; set; }

    [ObservableProperty]
    public partial bool BranchCheckout { get; set; }

    [ObservableProperty]
    public partial bool? AutoPopStashAfterCheckout { get; set; }

    [ObservableProperty]
    public partial bool? AutoPopStashAfterPull { get; set; }

    [ObservableProperty]
    public partial bool ConfirmStashDrop { get; set; }

    [ObservableProperty]
    public partial bool ResolveConflicts { get; set; }

    [ObservableProperty]
    public partial bool CommitAfterConflictsResolved { get; set; }

    [ObservableProperty]
    public partial bool SecondAbortConfirmation { get; set; }

    [ObservableProperty]
    public partial bool? UpdateModules { get; set; }

    [ObservableProperty]
    public partial bool SwitchWorktree { get; set; }

    protected override void SettingsToPage(SettingsSource? settings)
    {
        // Commits:
        Amend = !AppSettings.DontConfirmAmend.Value;
        UndoLastCommit = !AppSettings.DontConfirmUndoLastCommit.Value;
        CommitIfNoBranch = !AppSettings.DontConfirmCommitIfNoBranch;
        RebaseOnTopOfSelectedCommit = !AppSettings.DontConfirmRebase.Value;

        // Branches:
        FetchAndPruneAll = !AppSettings.DontConfirmFetchAndPruneAll.Value;
        PushNewBranch = !AppSettings.DontConfirmPushNewBranch.Value;
        AddTrackingRef = !AppSettings.DontConfirmAddTrackingRef;
        BranchDeleteUnmerged = !AppSettings.DontConfirmDeleteUnmergedBranch.Value;
        BranchCheckout = AppSettings.ConfirmBranchCheckout.Value;

        // Stashes:
        AutoPopStashAfterPull = Invert(AppSettings.AutoPopStashAfterPull);
        AutoPopStashAfterCheckout = Invert(AppSettings.AutoPopStashAfterCheckoutBranch);
        ConfirmStashDrop = !AppSettings.DontConfirmStashDrop;

        // Conflict resolution:
        ResolveConflicts = !AppSettings.DontConfirmResolveConflicts.Value;
        CommitAfterConflictsResolved = !AppSettings.DontConfirmCommitAfterConflictsResolved.Value;
        SecondAbortConfirmation = !AppSettings.DontConfirmSecondAbortConfirmation.Value;

        // Submodules:
        UpdateModules = Invert(AppSettings.DontConfirmUpdateSubmodulesOnCheckout);

        // Worktrees:
        SwitchWorktree = !AppSettings.DontConfirmSwitchWorktree.Value;

        base.SettingsToPage(settings);
    }

    protected override void PageToSettings(SettingsSource? settings)
    {
        // Commits:
        AppSettings.DontConfirmAmend.Value = !Amend;
        AppSettings.DontConfirmUndoLastCommit.Value = !UndoLastCommit;
        AppSettings.DontConfirmCommitIfNoBranch = !CommitIfNoBranch;
        AppSettings.DontConfirmRebase.Value = !RebaseOnTopOfSelectedCommit;

        // Branches:
        AppSettings.DontConfirmFetchAndPruneAll.Value = !FetchAndPruneAll;
        AppSettings.DontConfirmPushNewBranch.Value = !PushNewBranch;
        AppSettings.DontConfirmAddTrackingRef = !AddTrackingRef;
        AppSettings.DontConfirmDeleteUnmergedBranch.Value = !BranchDeleteUnmerged;
        AppSettings.ConfirmBranchCheckout.Value = BranchCheckout;

        // Stashes:
        AppSettings.AutoPopStashAfterPull = Invert(AutoPopStashAfterPull);
        AppSettings.AutoPopStashAfterCheckoutBranch = Invert(AutoPopStashAfterCheckout);
        AppSettings.DontConfirmStashDrop = !ConfirmStashDrop;

        // Conflict resolution:
        AppSettings.DontConfirmResolveConflicts.Value = !ResolveConflicts;
        AppSettings.DontConfirmCommitAfterConflictsResolved.Value = !CommitAfterConflictsResolved;
        AppSettings.DontConfirmSecondAbortConfirmation.Value = !SecondAbortConfirmation;

        // Submodules:
        AppSettings.DontConfirmUpdateSubmodulesOnCheckout = Invert(UpdateModules);

        // Worktrees:
        AppSettings.DontConfirmSwitchWorktree.Value = !SwitchWorktree;

        base.PageToSettings(settings);
    }

    /// <summary>As <c>ToCheckboxStateInverted</c> and <c>ToBooleanInverted</c>: unset stays unset (indeterminate).</summary>
    private static bool? Invert(bool? value) => value is { } flag ? !flag : null;
}

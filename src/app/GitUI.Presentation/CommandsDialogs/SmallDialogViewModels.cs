using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>View model of the command line help (port of <c>FormCommandlineHelp</c>).</summary>
public sealed partial class CommandlineHelpViewModel(CommandlineHelpStrings strings, string commands) : DialogViewModel
{
    public CommandlineHelpStrings Strings { get; } = strings;

    /// <summary>The list of supported commands (not translated, as in the WinForms form).</summary>
    public string Commands { get; } = commands;
}

/// <summary>View model of the add files dialog (port of <c>FormAddFiles</c>).</summary>
public sealed partial class AddFilesViewModel : DialogViewModel
{
    private readonly Func<string, bool> _runGit;

    /// <param name="runGit">Runs git with the given arguments in the progress dialog; returns <see langword="true"/> on success.</param>
    public AddFilesViewModel(AddFilesStrings strings, string? filter, Func<string, bool> runGit)
    {
        Strings = strings;
        Filter = filter ?? ".";
        _runGit = runGit;
    }

    public AddFilesStrings Strings { get; }

    [ObservableProperty]
    public partial string Filter { get; set; }

    [ObservableProperty]
    public partial bool Force { get; set; }

    [RelayCommand]
    private void ShowFiles() => _runGit($"add --dry-run{ForceArgument} {Filter}");

    [RelayCommand]
    private void AddFiles()
    {
        if (_runGit($"add{ForceArgument} {Filter}"))
        {
            Close(accepted: true);
        }
    }

    private string ForceArgument => Force ? " -f" : "";
}

/// <summary>
///  View model of the contributors dialog (port of <c>FormContributors</c>). Its strings are not in the translations
///  (as for the WinForms form), so they are constants.
/// </summary>
public sealed partial class ContributorsViewModel : DialogViewModel
{
    public ContributorsViewModel(string team, string coders, string translators, string designers)
    {
        DevelopersText = $"Team:\r\n{JoinLines(team)}\r\n\r\nContributors:\r\n{JoinLines(coders)}";
        TranslatorsText = JoinLines(translators);
        DesignersText = JoinLines(designers);
    }

    public string Title => "The application would not be possible without...";

    public string DevelopersTab => "Developers";

    public string TranslatorsTab => "Translators";

    public string DesignersTab => "Designers";

    public string DevelopersText { get; }

    public string TranslatorsText { get; }

    public string DesignersText { get; }

    [GeneratedRegex(@"\r\n?|\n", RegexOptions.ExplicitCapture)]
    private static partial Regex NewlineRegex { get; }

    private static string JoinLines(string text) => NewlineRegex.Replace(text, " ");
}

/// <summary>The choice made in the reset changes confirmation (as <c>FormResetChanges.ActionEnum</c>).</summary>
public enum ResetChangesAction
{
    Cancel,
    Reset,
    ResetAndDelete,
}

/// <summary>View model of the reset changes confirmation (port of <c>FormResetChanges</c>).</summary>
public sealed partial class ResetChangesViewModel : DialogViewModel
{
    public ResetChangesViewModel(ResetChangesStrings strings, bool hasExistingFiles, bool hasNewFiles, string? confirmationMessage)
    {
        Strings = strings;
        Message = confirmationMessage?.ReplaceLineEndings() ?? strings.Message.Text;

        // No existing files: new files only, so they are deleted. No new files: nothing to delete. Otherwise the user decides.
        CanChooseDeleteNewFiles = hasExistingFiles && hasNewFiles;
        DeleteNewFiles = !hasExistingFiles;
    }

    public ResetChangesStrings Strings { get; }

    public string Message { get; }

    public bool CanChooseDeleteNewFiles { get; }

    [ObservableProperty]
    public partial bool DeleteNewFiles { get; set; }

    /// <summary>The choice; <see cref="ResetChangesAction.Cancel"/> also when the dialog is closed otherwise.</summary>
    public ResetChangesAction SelectedAction { get; private set; }

    [RelayCommand]
    private void Reset()
    {
        SelectedAction = DeleteNewFiles ? ResetChangesAction.ResetAndDelete : ResetChangesAction.Reset;
        Close(accepted: true);
    }

    [RelayCommand]
    private void Cancel()
    {
        SelectedAction = ResetChangesAction.Cancel;
        Close(accepted: false);
    }
}

/// <summary>Git operations of the delete tag dialog.</summary>
public interface IDeleteTagHost
{
    void DeleteLocalTag(string tagName);

    /// <summary>Deletes the tag from the remote (runs the push with its event scripts).</summary>
    void DeleteRemoteTag(string remote, string tagName);

    void OpenUrl(string url);
}

/// <summary>View model of the delete tag dialog (port of <c>FormDeleteTag</c>).</summary>
public sealed partial class DeleteTagViewModel : DialogViewModel
{
    private readonly IDeleteTagHost _host;
    private readonly Func<string> _manualUrl;

    public DeleteTagViewModel(
        DeleteTagStrings strings,
        IReadOnlyList<string> tags,
        string? tag,
        IReadOnlyList<string> remotes,
        string currentRemote,
        Func<string> manualUrl,
        IDeleteTagHost host)
    {
        Strings = strings;
        Tags = tags;
        TagName = tag ?? "";
        Remotes = remotes;
        SelectedRemote = currentRemote;
        _manualUrl = manualUrl;
        _host = host;
    }

    public DeleteTagStrings Strings { get; }

    public IReadOnlyList<string> Tags { get; }

    public IReadOnlyList<string> Remotes { get; }

    /// <remarks>The URL is only built when needed, as in <c>GotoUserManualControl</c>.</remarks>
    public string HelpTooltip => string.Format(Strings.HelpTooltip.Text, _manualUrl());

    [ObservableProperty]
    public partial string TagName { get; set; }

    [ObservableProperty]
    public partial bool DeleteFromRemote { get; set; }

    [ObservableProperty]
    public partial string SelectedRemote { get; set; }

    [RelayCommand]
    private void Delete()
    {
        try
        {
            _host.DeleteLocalTag(TagName);

            if (DeleteFromRemote && !string.IsNullOrEmpty(TagName))
            {
                _host.DeleteRemoteTag(SelectedRemote, TagName);
            }

            Close(accepted: true);
        }
        catch (Exception ex)
        {
            // As in FormDeleteTag: git reports its own errors; the dialog stays open.
            System.Diagnostics.Trace.WriteLine(ex.Message);
        }
    }

    [RelayCommand]
    private void OpenHelp() => _host.OpenUrl(_manualUrl());
}

/// <summary>View model of the go to line dialog (port of <c>FormGoToLine</c>).</summary>
public sealed partial class GoToLineViewModel : DialogViewModel
{
    public GoToLineViewModel(GoToLineStrings strings, int maxLineNumber)
    {
        Strings = strings;
        MaxLineNumber = Math.Max(1, maxLineNumber);
        LineNumber = 1;
    }

    public GoToLineStrings Strings { get; }

    public int MaxLineNumber { get; }

    public string Label => $"{Strings.LineNumber.Text} (1 - {MaxLineNumber}):";

    [ObservableProperty]
    public partial int LineNumber { get; set; }

    // As WinForms NumericUpDown: the value stays within 1..MaxLineNumber, however it is set.
    partial void OnLineNumberChanged(int value)
    {
        int clamped = Math.Clamp(value, 1, MaxLineNumber);
        if (clamped != value)
        {
            LineNumber = clamped;
        }
    }

    [RelayCommand]
    private void Ok() => Close(accepted: true);

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

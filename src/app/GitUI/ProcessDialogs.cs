using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitUI.AvaloniaHosting;
using GitUI.Infrastructure;

namespace GitUI;

/// <summary>
///  The progress dialogs of the git commands: what the static methods of the WinForms <c>FormProcess</c>,
///  <c>FormRemoteProcess</c> and <c>FormStatus</c> did, with the Avalonia dialogs.
/// </summary>
public static class ProcessDialogs
{
    /// <summary>Runs the command in the progress dialog; returns whether it succeeded (<c>FormProcess.ShowDialog</c>).</summary>
    public static bool ShowProcess(IWin32Window? owner, IGitUICommands commands, ArgumentString arguments, string workingDirectory, string? input, bool useDialogSettings, string? process = null)
        => ShowProcess(owner, commands, arguments, workingDirectory, input, useDialogSettings, out _, process);

    /// <inheritdoc cref="ShowProcess(IWin32Window?, IGitUICommands, ArgumentString, string, string?, bool, string?)"/>
    public static bool ShowProcess(IWin32Window? owner, IGitUICommands commands, ArgumentString arguments, string workingDirectory, string? input, bool useDialogSettings, out string output, string? process = null)
    {
        AvaloniaDialogs.TryShowProcess(owner, commands, arguments, workingDirectory, input, useDialogSettings, process, out bool success, out output);
        return success;
    }

    /// <summary>Runs the command in the progress dialog; returns its output (<c>FormProcess.ReadDialog</c>).</summary>
    public static string ReadProcess(IWin32Window? owner, IGitUICommands commands, ArgumentString arguments, string workingDirectory, string? input, bool useDialogSettings)
    {
        AvaloniaDialogs.TryShowProcess(owner, commands, arguments, workingDirectory, input, useDialogSettings, process: null, out _, out string output);
        return output;
    }

    /// <summary>Whether the output of the dialog tells that the operation was aborted (<c>FormProcess.IsOperationAborted</c>).</summary>
    public static bool IsOperationAborted(string dialogResult)
        => dialogResult.Trim(Delimiters.LineFeedAndCarriageReturn) == "Aborted";

    /// <summary>Runs a command accessing a remote in the progress dialog (<c>FormRemoteProcess.ShowDialog</c>).</summary>
    public static bool ShowRemoteProcess(IWin32Window? owner, IGitUICommands commands, ArgumentString arguments)
    {
        AvaloniaDialogs.TryShowRemoteProcess(owner, commands, arguments, out bool success);
        return success;
    }

    /// <summary>Offers to cache the host key of the remote with PuTTY (<c>FormRemoteProcess.AskForCacheHostkey</c>).</summary>
    public static bool AskForCacheHostkey(IWin32Window owner, string remoteUrl)
        => OperatingSystem.IsWindows() && !string.IsNullOrEmpty(remoteUrl) && MessageBoxes.CacheHostkey(owner) && new Plink().Connect(remoteUrl);

    /// <summary>Reports an operation that failed with its output (<c>FormStatus.ShowErrorDialog</c>).</summary>
    public static void ShowErrorDialog(IWin32Window owner, IGitUICommands commands, string text, params string[] output)
        => AvaloniaDialogs.TryShowErrorDialog(owner, commands, text, output);
}

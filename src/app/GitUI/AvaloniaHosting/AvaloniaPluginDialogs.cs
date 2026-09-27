using GitExtensions.Extensibility;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Services;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  The hosting of the Avalonia ports of the plugins' forms (docs/avalonia-port/PLAN.md, phase 7). The plugins own their
///  view models and views (as they own their WinForms forms) and show them through these methods, which set Avalonia up
///  with the application's theme and fonts, share the WinForms window positions and own the windows by WinForms windows.
/// </summary>
/// <remarks>
///  <see cref="AvaloniaDialogs"/> is internal to GitUI; this is its public surface for the plugins, as
///  <see cref="AvaloniaStartupDialogs"/> is for <c>Gitanas.exe</c>.
/// </remarks>
public static class AvaloniaPluginDialogs
{
    /// <summary>
    ///  Shows the window created by <paramref name="createWindow"/> modally over <paramref name="owner"/>; returns whether it
    ///  was accepted.
    /// </summary>
    /// <param name="createWindow">Creates the window, once Avalonia is set up.</param>
    /// <param name="owner">The WinForms window (or control) that owns the dialog, or <see langword="null"/>.</param>
    /// <param name="positionName">
    ///  The name under which the WinForms form persisted its position (its type name), if it did; the Avalonia window shares it.
    /// </param>
    public static bool ShowDialog(Func<DialogWindow> createWindow, IWin32Window? owner, string? positionName = null)
        => AvaloniaDialogs.ShowDialog(createWindow, owner, positionName);

    /// <summary>
    ///  Shows the window created by <paramref name="createWindow"/> modally over <paramref name="owner"/> (plugin API v2, e.g.
    ///  <c>GitUIEventArgs.Owner</c>); returns whether it was accepted.
    /// </summary>
    /// <param name="createWindow">Creates the window, once Avalonia is set up.</param>
    /// <param name="owner">The window that owns the dialog (of any UI framework), or <see cref="WindowOwner.None"/>.</param>
    /// <param name="positionName">
    ///  The name under which the WinForms form persisted its position (its type name), if it did; the Avalonia window shares it.
    /// </param>
    public static bool ShowDialog(Func<DialogWindow> createWindow, WindowOwner owner, string? positionName = null)
        => ShowDialog(createWindow, owner.ToWin32Window(), positionName);

    /// <summary>
    ///  Shows the window created by <paramref name="createWindow"/> modelessly over <paramref name="owner"/> (as
    ///  <c>Form.Show(owner)</c>); it closes with the WinForms form that owns it.
    /// </summary>
    /// <param name="showInTaskbar">Whether the window has its own taskbar button although owned (as <c>ShowInTaskbar = true</c>).</param>
    public static DialogWindow Show(Func<DialogWindow> createWindow, IWin32Window? owner, string? positionName = null, bool showInTaskbar = false)
    {
        AvaloniaUi.EnsureInitialized(AvaloniaDialogs.GetOptions);
        DialogWindow window = createWindow();
        window.PositionName = positionName;
        window.PositionStore = AvaloniaDialogs.WindowPositionStore.Instance;

        // An owned window closes with its owner (AvaloniaDialogHost.Show).
        AvaloniaDialogHost.Show(window, owner?.Handle ?? 0);
        if (showInTaskbar)
        {
            window.ShowInTaskbar = true;
        }

        return window;
    }

    /// <summary>As <see cref="Show(Func{DialogWindow}, IWin32Window?, string?, bool)"/>, owned by a window of plugin API v2.</summary>
    public static DialogWindow Show(Func<DialogWindow> createWindow, WindowOwner owner, string? positionName = null, bool showInTaskbar = false)
        => Show(createWindow, owner.ToWin32Window(), positionName, showInTaskbar);

    /// <summary>The Avalonia window as the owner of WinForms dialogs and message boxes it opens.</summary>
    public static IWin32Window GetOwner(DialogWindow window) => new AvaloniaDialogs.NativeWindowOwner(window);

    /// <summary>The Avalonia window as the owner of the dialogs and message boxes it opens (plugin API v2).</summary>
    public static WindowOwner GetWindowOwner(DialogWindow window) => new(window.OwnerHandle);

    /// <summary>The application's message boxes, owned by the Avalonia window.</summary>
    public static IMessageBoxService CreateMessageBoxService(DialogWindow window) => new AvaloniaDialogs.MessageBoxService(window);

    /// <summary>The file and folder pickers of the Avalonia window.</summary>
    public static IFileDialogService CreateFileDialogService(DialogWindow window) => new AvaloniaFileDialogService(window);

    /// <summary>The caption of error messages (<c>TranslatedStrings.Error</c>, the default of <c>MessageBoxes.ShowError</c>).</summary>
    public static string ErrorCaption => TranslatedStrings.Error;

    /// <summary>Runs the background work of view models with the application's <c>JoinableTaskFactory</c>.</summary>
    public static IBackgroundRunner BackgroundRunner { get; } = new JoinableTaskBackgroundRunner();

    private sealed class JoinableTaskBackgroundRunner : IBackgroundRunner
    {
        public async Task<T> RunAsync<T>(Func<T> work, CancellationToken cancellationToken)
        {
            await TaskScheduler.Default;
            T result = work();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return result;
        }

        public void Post(Action action)
            => ThreadHelper.FileAndForget(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                action();
            });
    }
}

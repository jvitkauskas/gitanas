using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Shows Avalonia dialogs modally over their owner windows, given by native handle.
/// </summary>
/// <remarks>
///  <para>
///   On Windows the owner may be any native window (Avalonia's own <c>ShowDialog</c> only accepts an Avalonia owner), so
///   this reproduces what WinForms' <c>Form.ShowDialog</c> did: it disables the other windows of the UI thread, sets the
///   native owner (so the dialog stays above it and is minimised with it), and runs a nested message loop until the
///   dialog closes. Avalonia's loop dispatches all Win32 messages of the thread, so native windows keep painting.
///  </para>
///  <para>
///   Elsewhere every owner is an Avalonia window (<see cref="DialogWindow.OwnerHandle"/> identifies it even on Wayland,
///   which has no native handle), so the dialog is shown with Avalonia's <c>ShowDialog</c> over the identified window, in
///   a nested dispatcher frame too (docs/avalonia-port/CROSS-PLATFORM.md, phase 2): the callers still get the result when
///   the call returns.
///  </para>
/// </remarks>
public static class AvaloniaDialogHost
{
    /// <summary>
    ///  Raised on the UI thread just before a dialog is shown; lets integration tests drive the modal dialog.
    /// </summary>
    internal static Action<DialogWindow>? DialogShowingForTests { get; set; }

    /// <summary>The windows shown and not closed yet (as the WinForms <c>Application.OpenForms</c>).</summary>
    private static readonly List<DialogWindow> _openWindows = [];

    /// <summary>Whether a window is open (as <c>Application.OpenForms.Count &gt; 0</c>).</summary>
    public static bool HasOpenWindows => _openWindows.Count > 0;

    /// <summary>The open window of <paramref name="handle"/> (or of a window it contains), if any.</summary>
    public static DialogWindow? FindOpenWindow(nint handle)
    {
        nint root = handle == 0 ? 0 : OperatingSystem.IsWindows() ? NativeMethods.GetAncestor(handle, NativeMethods.GA_ROOT) : handle;
        return root == 0 ? null : _openWindows.FirstOrDefault(window => window.OwnerHandle == root);
    }

    /// <summary>
    ///  Enumerates the window containing <paramref name="handle"/>, then its owners, nearest first. Off Windows these
    ///  are Avalonia window identities, including synthetic handles on Wayland, rather than Win32 handles.
    /// </summary>
    public static IEnumerable<nint> GetWindowAndOwnerHandles(nint handle)
    {
        if (OperatingSystem.IsWindows())
        {
            handle = handle == 0 ? 0 : NativeMethods.GetAncestor(handle, NativeMethods.GA_ROOT);
            for (; handle != 0; handle = NativeMethods.GetWindow(handle, NativeMethods.GW_OWNER))
            {
                yield return handle;
            }
        }
        else
        {
            for (Window? window = FindOpenWindow(handle); window is not null; window = window.Owner as Window)
            {
                if (window is DialogWindow dialog)
                {
                    yield return dialog.OwnerHandle;
                }
            }
        }
    }

    /// <summary>
    ///  The handle of the active window of the application, else of the window opened last (the owner of a message box
    ///  shown without one, as the native message box takes the active window); 0 if no window is open.
    /// </summary>
    public static nint GetActiveWindowHandle() => GetActiveWindow()?.OwnerHandle ?? 0;

    /// <summary>The active window of the application, else the window opened last; none if no window is open.</summary>
    public static DialogWindow? GetActiveWindow() => _openWindows.LastOrDefault(window => window.IsActive) ?? _openWindows.LastOrDefault();

    /// <summary>Closes all the open windows (as the WinForms <c>Application.Exit</c>), which ends the main loop.</summary>
    public static void CloseAllWindows()
    {
        foreach (DialogWindow window in _openWindows.ToList())
        {
            window.Close();
        }
    }

    private static void TrackOpenWindow(DialogWindow window)
    {
        _openWindows.Add(window);
        window.Closed += (_, _) => _openWindows.Remove(window);
    }

    /// <summary>
    ///  Shows <paramref name="window"/> modally and returns whether it was accepted.
    /// </summary>
    /// <param name="window">The dialog to show.</param>
    /// <param name="ownerHandle">
    ///  Native handle of the owner (any window or control of it), or 0 for an unowned dialog such as
    ///  one started from the command line.
    /// </param>
    public static bool ShowDialog(DialogWindow window, nint ownerHandle)
    {
        AvaloniaUi.VerifyUiThread();
        AvaloniaUi.RememberHostContext();

        return OperatingSystem.IsWindows()
            ? ShowDialogOverNativeOwner(window, ownerHandle)
            : ShowDialogOverAvaloniaOwner(window, ownerHandle);
    }

    /// <summary>On Windows: a modal dialog over any native window (as WinForms' <c>Form.ShowDialog</c>).</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static bool ShowDialogOverNativeOwner(DialogWindow window, nint ownerHandle)
    {
        nint owner = ownerHandle == 0 ? 0 : NativeMethods.GetAncestor(ownerHandle, NativeMethods.GA_ROOT);
        nint dialog = window.NativeHandle;

        if (owner != 0 && dialog != 0)
        {
            NativeMethods.SetWindowLongPtr(dialog, NativeMethods.GWLP_HWNDPARENT, owner);
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.IsCenteredOnOwner = true;
            CenterOver(window, owner);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        List<nint> disabledWindows = NativeMethods.DisableEnabledThreadWindows(except: dialog);
        bool reenabled = false;
        void ReenableWindows()
        {
            if (!reenabled)
            {
                reenabled = true;
                NativeMethods.EnableWindows(disabledWindows);
            }
        }

        DispatcherFrame frame = new();

        // Re-enable the owner before the dialog is destroyed, otherwise Windows activates another application.
        window.Closing += (_, e) =>
        {
            if (!e.Cancel)
            {
                ReenableWindows();
            }
        };
        window.Closed += (_, _) =>
        {
            ReenableWindows();
            frame.Continue = false;
        };

        DialogShowingForTests?.Invoke(window);
        TrackOpenWindow(window);
        window.Show();
        Dispatcher.UIThread.PushFrame(frame);

        if (owner != 0)
        {
            NativeMethods.SetForegroundWindow(owner);
        }

        return window.DialogResult;
    }

    /// <summary>
    ///  Shows <paramref name="window"/> modelessly over a native owner, as WinForms' <c>Form.Show(owner)</c>:
    ///  it stays above the owner and is minimised with it, but the owner stays usable; it closes with an owner shown here
    ///  (Windows would otherwise destroy the owned window behind Avalonia's back).
    /// </summary>
    /// <param name="window">The window to show.</param>
    /// <param name="ownerHandle">Native handle of the owner (any window or control of it), or 0 for none.</param>
    public static void Show(DialogWindow window, nint ownerHandle)
    {
        AvaloniaUi.VerifyUiThread();
        AvaloniaUi.RememberHostContext();

        if (OperatingSystem.IsWindows())
        {
            ShowOverNativeOwner(window, ownerHandle);
        }
        else
        {
            ShowOverAvaloniaOwner(window, ownerHandle);
        }
    }

    /// <summary>On Windows: a modeless window over any native window (as WinForms' <c>Form.Show(owner)</c>).</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ShowOverNativeOwner(DialogWindow window, nint ownerHandle)
    {
        nint owner = ownerHandle == 0 ? 0 : NativeMethods.GetAncestor(ownerHandle, NativeMethods.GA_ROOT);
        nint handle = window.NativeHandle;
        if (owner != 0 && handle != 0)
        {
            NativeMethods.SetWindowLongPtr(handle, NativeMethods.GWLP_HWNDPARENT, owner);
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.IsCenteredOnOwner = true;
            CenterOver(window, owner);
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (owner != 0 && FindOpenWindow(owner) is { } ownerWindow)
        {
            void CloseWithOwner(object? sender, EventArgs e) => window.Close();
            ownerWindow.Closed += CloseWithOwner;
            window.Closed += (_, _) => ownerWindow.Closed -= CloseWithOwner;
        }

        DialogShowingForTests?.Invoke(window);
        TrackOpenWindow(window);
        window.Show();
    }

    /// <summary>Off Windows: a modal dialog over the open window of <paramref name="ownerHandle"/>, if any.</summary>
    private static bool ShowDialogOverAvaloniaOwner(DialogWindow window, nint ownerHandle)
    {
        DialogWindow? owner = FindOpenWindow(ownerHandle);
        PrepareForOwner(window, owner);
        window.IsModal = true;

        DispatcherFrame frame = new();
        window.Closed += (_, _) => frame.Continue = false;

        DialogShowingForTests?.Invoke(window);
        TrackOpenWindow(window);
        if (owner is not null)
        {
            // Avalonia disables the owner while the dialog is open; the task completes when the dialog closes, which the
            // frame waits for.
            _ = window.ShowDialog(owner);
        }
        else
        {
            window.Show();
        }

        Dispatcher.UIThread.PushFrame(frame);
        owner?.Activate();
        return window.DialogResult;
    }

    /// <summary>Off Windows: a modeless window over the open window of <paramref name="ownerHandle"/>, if any.</summary>
    private static void ShowOverAvaloniaOwner(DialogWindow window, nint ownerHandle)
    {
        DialogWindow? owner = FindOpenWindow(ownerHandle);
        PrepareForOwner(window, owner);

        DialogShowingForTests?.Invoke(window);
        TrackOpenWindow(window);
        if (owner is not null)
        {
            // An owned window stays above its owner and closes with it.
            window.Show(owner);
        }
        else
        {
            window.Show();
        }
    }

    private static void PrepareForOwner(DialogWindow window, DialogWindow? owner)
    {
        if (owner is not null)
        {
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.IsCenteredOnOwner = true;
        }
        else
        {
            window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void CenterOver(Window window, nint owner)
    {
        if (!NativeMethods.GetWindowRect(owner, out NativeMethods.RECT ownerRect))
        {
            return;
        }

        void Center()
        {
            // A size restored on opening is set but not laid out yet: use it rather than the current client size.
            Size clientSize = new(
                double.IsNaN(window.Width) || window.SizeToContent.HasFlag(SizeToContent.Width) ? window.ClientSize.Width : window.Width,
                double.IsNaN(window.Height) || window.SizeToContent.HasFlag(SizeToContent.Height) ? window.ClientSize.Height : window.Height);
            Size frame = window.FrameSize is { } frameSize ? new Size(frameSize.Width - window.ClientSize.Width, frameSize.Height - window.ClientSize.Height) : default;
            PixelSize pixels = PixelSize.FromSize(new Size(clientSize.Width + frame.Width, clientSize.Height + frame.Height), window.DesktopScaling);
            window.Position = new PixelPoint(
                ownerRect.Left + ((ownerRect.Right - ownerRect.Left - pixels.Width) / 2),
                ownerRect.Top + ((ownerRect.Bottom - ownerRect.Top - pixels.Height) / 2));
        }

        // The size is only final once the window has been laid out (e.g. SizeToContent); center again then.
        if (!double.IsNaN(window.Width) && !double.IsNaN(window.Height))
        {
            window.Position = new PixelPoint(
                ownerRect.Left + (int)((ownerRect.Right - ownerRect.Left - (window.Width * window.DesktopScaling)) / 2),
                ownerRect.Top + (int)((ownerRect.Bottom - ownerRect.Top - (window.Height * window.DesktopScaling)) / 2));
        }

        window.Opened += (_, _) => Center();
    }
}

using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitUI.Presentation;
using GitUI.Presentation.Services;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Base class of Avalonia dialogs; the counterpart of <c>GitExtensionsForm</c> / <c>GitExtensionsDialog</c>.
/// </summary>
/// <remarks>
///  <list type="bullet">
///   <item>Closes itself when its <see cref="DialogViewModel"/> requests it, and on Escape (like <c>GitExtensionsForm</c>).</item>
///   <item>Restores and saves its size and position under <see cref="PositionName"/> (like <c>WindowPositionManager</c>).</item>
///   <item>Executes the hotkeys of the focused <see cref="IHotkeyControl"/>s, then the configured <see cref="Hotkeys"/> through <see cref="ExecuteHotkeyCommand"/>, before the focused control gets the key.</item>
///  </list>
/// </remarks>
public class DialogWindow : Window
{
    private static int _nextOwnerId;
    private readonly nint _ownerId = Interlocked.Decrement(ref _nextOwnerId);

    private static readonly Lazy<WindowIcon> _applicationIcon = new(
        () => new WindowIcon(AssetLoader.Open(new Uri("avares://GitUI.Avalonia/Assets/git-extensions-logo-256px.png"))));

    private DialogViewModel? _viewModel;
    private PixelRect? _lastNormalBounds;

    /// <summary>Size of the window frame (title bar, borders): saved positions are outer bounds, Width/Height are client size.</summary>
    private Size _frameThickness;

    public DialogWindow()
    {
        CanMinimize = false;
        Icon = _applicationIcon.Value;

        if (OperatingSystem.IsWindows())
        {
            Win32Properties.AddWndProcHookCallback(this, WndProcHook);
            ActualThemeVariantChanged += (_, _) => UpdateWindows10TitleBar();
        }

        // As ProcessCmdKey: the hotkeys come before the focused control (which gets the key if the command is not executed).
        AddHandler(KeyDownEvent, OnPreviewKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);
        ScalingChanged += (_, _) => GitExtUtils.GitUI.DpiUtil.UseRenderScaling(RenderScaling);
    }

    /// <summary>
    ///  <see langword="true"/> if the dialog was accepted (the equivalent of <c>DialogResult.OK</c>).
    /// </summary>
    public bool DialogResult { get; private set; }

    /// <summary>
    ///  The native window handle, available once the window has been created; used to parent WinForms dialogs.
    /// </summary>
    public nint NativeHandle => TryGetPlatformHandle()?.Handle ?? 0;

    /// <summary>
    ///  Identifies this window to the dialog bridge. Wayland has no native handles, so it uses a unique process-local
    ///  ID instead; that ID must never be passed to native APIs. Windows keeps its HWND, including 0 before creation.
    /// </summary>
    public nint OwnerHandle => NativeHandle != 0 ? NativeHandle : OperatingSystem.IsWindows() ? 0 : _ownerId;

    /// <summary>Whether the window is shown modally (off Windows, by <see cref="AvaloniaDialogHost"/>).</summary>
    internal bool IsModal { get; set; }

    /// <summary>
    ///  The name under which the window position is persisted (the name of the WinForms form it replaces),
    ///  or <see langword="null"/> to not persist it.
    /// </summary>
    public string? PositionName { get; set; }

    /// <summary>The store for <see cref="PositionName"/>; set by the host.</summary>
    public IWindowPositionStore? PositionStore { get; set; }

    /// <summary>
    ///  The store of all the windows instead of their <see cref="PositionStore"/>, for the integration tests, which would
    ///  otherwise save the sizes of their windows in the user's <c>WindowPositions.xml</c>.
    /// </summary>
    internal static IWindowPositionStore? PositionStoreForTests { get; set; }

    private IWindowPositionStore? EffectivePositionStore => PositionStoreForTests ?? PositionStore;

    /// <summary>
    ///  Set by the host when the dialog is centered over an owner: only the size is restored then, not the location.
    /// </summary>
    public bool IsCenteredOnOwner { get; set; }

    /// <summary>
    ///  The screen position (in pixels) to open the window at instead of centering it, as a WinForms form with
    ///  <c>StartPosition.Manual</c> and a <c>Location</c> (e.g. a popup under a grid cell).
    /// </summary>
    public PixelPoint? StartupScreenPosition { get; set; }

    /// <summary>The configured hotkeys of this dialog.</summary>
    public IReadOnlyList<HotkeyBinding> Hotkeys { get; set; } = [];

    /// <summary>
    ///  The section of the user manual (as <c>GitExtensionsDialog.ManualSectionSubfolder</c> and
    ///  <c>ManualSectionAnchorName</c>); F1 opens it, instead of the help button of the WinForms title bar.
    /// </summary>
    public string? ManualSectionSubfolder { get; set; }

    /// <inheritdoc cref="ManualSectionSubfolder"/>
    public string? ManualSectionAnchorName { get; set; }

    /// <summary>Opens the section of the user manual (subfolder, anchor); set by the host.</summary>
    public static Action<string, string>? OpenManualSection { get; set; }

    protected override void OnDataContextChanged(EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.CloseRequested -= OnCloseRequested;
        }

        _viewModel = DataContext as DialogViewModel;

        if (_viewModel is not null)
        {
            _viewModel.CloseRequested += OnCloseRequested;
        }

        base.OnDataContextChanged(e);
    }

    private void UpdateWindows10TitleBar()
    {
        // Avalonia 12.1 applies this attribute only on Windows 11. Windows 10 20H1+ also supports it.
        // Keep the workaround local to Modern and leave Windows 11 to the backend. Never pass a synthetic
        // Wayland owner ID (or a headless test handle) to DWM.
        if (GitExtensionsAvaloniaApp.IsModern
            && OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041)
            && !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            && TryGetPlatformHandle() is { Handle: not 0, HandleDescriptor: "HWND" } handle)
        {
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            int dark = ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark ? 1 : 0;

            // Cosmetic best effort: an unsupported DWM attribute must not prevent a window from opening.
            if (NativeMethods.DwmSetWindowAttribute(handle.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref dark, sizeof(int)) == 0)
            {
                // Repaint both caption states without changing keyboard focus or activating the window.
                // SWP_FRAMECHANGED alone leaves a newly opened active dialog's caption light on Windows 10.
                _ = NativeMethods.SendMessage(handle.Handle, NativeMethods.WM_NCACTIVATE, IsActive ? 0 : 1, 0);
                _ = NativeMethods.SendMessage(handle.Handle, NativeMethods.WM_NCACTIVATE, IsActive ? 1 : 0, 0);
            }
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        // Cancel, OK on macOS (before the focus goes to the default button).
        DialogButtonOrder.Apply(this, DialogButtonOrder.IsReversed);

        // Off Windows the scaling of the screens is known from the windows only (e.g. Retina displays).
        GitExtUtils.GitUI.DpiUtil.UseRenderScaling(RenderScaling);

        // On macOS the menu bar keeps the menu of the main window (disabled while this window is modal).
        if (OperatingSystem.IsMacOS())
        {
            CommandsDialogs.BrowseDialog.BrowseWindow.AttachMacOSMenu(this, IsModal);
            Activated += (_, _) =>
            {
                if (OperatingSystem.IsMacOS())
                {
                    MacOSApplicationMenu.SetEnabled(!IsModal);
                }
            };
        }

        // Before the base raises Opened, where the host centers the dialog with its final size.
        _frameThickness = FrameSize is { } frameSize ? new Size(Math.Max(0, frameSize.Width - ClientSize.Width), Math.Max(0, frameSize.Height - ClientSize.Height)) : default;
        RestorePosition();
        AccessibleNames.Apply(this);
        base.OnOpened(e);

        // After the host centered the window (in the Opened event).
        if (StartupScreenPosition is { } position)
        {
            Position = position;
        }

        RememberNormalBounds();

        // Refresh after the host has finished opening/centering a modal window and native messages settle.
        if (OperatingSystem.IsWindows())
        {
            Dispatcher.UIThread.Post(UpdateWindows10TitleBar, DispatcherPriority.Loaded);
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (_viewModel?.CanClose() == false)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);

        if (!e.Cancel)
        {
            SavePosition();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ClientSizeProperty || change.Property == WindowStateProperty)
        {
            RememberNormalBounds();
        }
    }

    /// <summary>Escape: the dialog is cancelled (the <c>CancelButton</c> of <c>GitExtensionsForm</c>); a window may do otherwise.</summary>
    protected virtual void OnEscapePressed() => CloseDialog(accepted: false);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            e.Handled = true;
            OnEscapePressed();
            return;
        }

        if (e.Key == Key.F1 && e.KeyModifiers == KeyModifiers.None
            && !string.IsNullOrWhiteSpace(ManualSectionSubfolder) && !string.IsNullOrWhiteSpace(ManualSectionAnchorName) && OpenManualSection is { } openManual)
        {
            e.Handled = true;
            openManual(ManualSectionSubfolder, ManualSectionAnchorName);
            return;
        }
    }

    /// <summary>
    ///  Executes a hotkey command, by default through <see cref="DialogViewModel.ExecuteHotkeyCommand"/>; a dialog overrides
    ///  it for the commands of its view (e.g. moving the focus).
    /// </summary>
    /// <returns><see langword="true"/> if the command was executed (the key then does not reach the focused control).</returns>
    protected virtual bool ExecuteHotkeyCommand(int commandCode) => _viewModel?.ExecuteHotkeyCommand(commandCode) == true;

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        int keyData = KeyMapping.ToKeyData(e.Key, e.KeyModifiers);
        if (keyData == 0)
        {
            return;
        }

        // As IsTextEditKey in the hotkeys of the WinForms controls: a key typing or editing text is not a hotkey in an editable
        // text box (e.g. R in the filter of a file list, which would reset the selected files).
        object? focused = FocusManager?.GetFocusedElement();
        if (focused is TextBox { IsReadOnly: false } textBox && KeyMapping.IsTextEditKey(keyData, multiLine: textBox.AcceptsReturn))
        {
            return;
        }

        // As ProcessCmdKey from the focused control up: the controls with their own hotkeys come first.
        for (Visual? visual = focused as Visual; visual is not null && visual != this; visual = visual.GetVisualParent())
        {
            if (visual is IHotkeyControl control && control.ProcessHotkey(keyData))
            {
                e.Handled = true;
                return;
            }
        }

        HotkeyBinding? hotkey = Hotkeys.FirstOrDefault(h => h.KeyData == keyData);
        if (hotkey is not null && ExecuteHotkeyCommand(hotkey.CommandCode))
        {
            e.Handled = true;
        }
    }

    /// <summary>
    ///  A dialog whose height follows its content can only be resized horizontally (as the WinForms forms that fix
    ///  their height through <c>MinimumSize</c> / <c>MaximumSize</c>).
    /// </summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private nint WndProcHook(nint handle, uint msg, nint wordParameter, nint longParameter, ref bool handled)
    {
        if (!CanResize || SizeToContent != SizeToContent.Height)
        {
            return 0;
        }

        switch (msg)
        {
            case NativeMethods.WM_NCHITTEST:
                return HitTestHorizontalBordersOnly(handle, msg, wordParameter, longParameter, ref handled);
            case NativeMethods.WM_WINDOWPOSCHANGING:
                KeepContentHeight(handle, longParameter);
                return 0;
            default:
                return 0;
        }
    }

    /// <summary>The top and bottom borders do not resize the dialog.</summary>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static nint HitTestHorizontalBordersOnly(nint handle, uint msg, nint wordParameter, nint longParameter, ref bool handled)
    {
        nint hit = NativeMethods.DefWindowProc(handle, msg, wordParameter, longParameter);
        nint horizontalHit = hit switch
        {
            NativeMethods.HTTOP or NativeMethods.HTBOTTOM => NativeMethods.HTBORDER,
            NativeMethods.HTTOPLEFT or NativeMethods.HTBOTTOMLEFT => NativeMethods.HTLEFT,
            NativeMethods.HTTOPRIGHT or NativeMethods.HTBOTTOMRIGHT => NativeMethods.HTRIGHT,
            _ => hit
        };

        handled = horizontalHit != hit;
        return horizontalHit;
    }

    /// <summary>
    ///  Other resizes that are not the layout sizing the dialog to its content (e.g. Win+Shift+Up, or sizing
    ///  with the keyboard) keep its height, and then its vertical position.
    /// </summary>
    /// <remarks>
    ///  Avalonia would otherwise drop <see cref="SizeToContent.Height"/> on such a resize.
    /// </remarks>
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private void KeepContentHeight(nint handle, nint windowPosPointer)
    {
        NativeMethods.WINDOWPOS windowPos = Marshal.PtrToStructure<NativeMethods.WINDOWPOS>(windowPosPointer);
        if ((windowPos.Flags & NativeMethods.SWP_NOSIZE) != 0
            || DesiredSize.Height <= 0
            || !NativeMethods.GetWindowRect(handle, out NativeMethods.RECT windowRect)
            || !NativeMethods.GetClientRect(handle, out NativeMethods.RECT clientRect))
        {
            return;
        }

        int frameHeight = (windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top);
        int contentHeight = (int)Math.Round(DesiredSize.Height * RenderScaling) + frameHeight;

        // Allow for rounding: the layout resizes the window to its content, anything else is vetoed.
        if (Math.Abs(windowPos.Cy - contentHeight) <= 2)
        {
            return;
        }

        windowPos.Cy = windowRect.Bottom - windowRect.Top;
        if ((windowPos.Flags & NativeMethods.SWP_NOMOVE) == 0)
        {
            windowPos.Y = windowRect.Top;
        }

        Marshal.StructureToPtr(windowPos, windowPosPointer, fDeleteOld: false);
    }

    private void OnCloseRequested(object? sender, bool accepted) => CloseDialog(accepted);

    private void CloseDialog(bool accepted)
    {
        DialogResult = accepted;
        Close();
    }

    private int CurrentDpi => (int)Math.Round(DesktopScaling * 96);

    private void RestorePosition()
    {
        if (PositionName is null || EffectivePositionStore?.Load(PositionName) is not { } placement || placement.Dpi <= 0)
        {
            return;
        }

        // Dialogs restore only the dimensions that do not follow their content: a size saved by the WinForms form
        // (or for other content) would crop a layout sized to content.
        // Stored in pixels at the DPI of the time; device-independent units are pixels * 96 / DPI.
        if (CanResize && !SizeToContent.HasFlag(SizeToContent.Width))
        {
            Width = Math.Min(MaxWidth, Math.Max(MinWidth, (placement.Width * 96.0 / placement.Dpi) - _frameThickness.Width));
        }

        if (CanResize && !SizeToContent.HasFlag(SizeToContent.Height))
        {
            Height = Math.Min(MaxHeight, Math.Max(MinHeight, (placement.Height * 96.0 / placement.Dpi) - _frameThickness.Height));
        }

        if (!IsCenteredOnOwner)
        {
            double scale = (double)CurrentDpi / placement.Dpi;
            PixelRect bounds = new((int)(placement.X * scale), (int)(placement.Y * scale), (int)(placement.Width * scale), (int)(placement.Height * scale));
            if (IsVisibleOnAScreen(bounds))
            {
                Position = bounds.Position;
            }
        }

        if (placement.IsMaximized && CanResize)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void RememberNormalBounds()
    {
        if (WindowState == WindowState.Normal)
        {
            Size outerSize = new(ClientSize.Width + _frameThickness.Width, ClientSize.Height + _frameThickness.Height);
            _lastNormalBounds = new PixelRect(Position, PixelSize.FromSize(outerSize, DesktopScaling));
        }
    }

    private void SavePosition()
    {
        if (PositionName is null || EffectivePositionStore is not { } store)
        {
            return;
        }

        RememberNormalBounds();
        if (_lastNormalBounds is not { } bounds)
        {
            return;
        }

        store.Save(
            PositionName,
            new WindowPlacement(bounds.X, bounds.Y, bounds.Width, bounds.Height, CurrentDpi, WindowState == WindowState.Maximized));
    }

    /// <summary>
    ///  As <c>WindowPositionManager.FitWindowOnScreen</c>: a window counts as visible if at least 10% of a screen
    ///  (or the whole window, if smaller) is covered from its top-left corner.
    /// </summary>
    private bool IsVisibleOnAScreen(PixelRect window)
    {
        foreach (Screen screen in Screens.All)
        {
            PixelRect area = screen.WorkingArea;
            int requiredWidth = Math.Min(area.Width / 10, window.Width);
            int requiredHeight = Math.Min(area.Height / 10, window.Height);
            if (area.Contains(window.Position) && area.Contains(new PixelPoint(window.X + requiredWidth, window.Y + requiredHeight)))
            {
                return true;
            }
        }

        return false;
    }
}

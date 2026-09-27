using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Appearance settings the host passes to the Avalonia UI.
/// </summary>
/// <param name="IsDarkTheme">Whether the Git Extensions theme is dark.</param>
/// <param name="FontFamily">The application font family (<c>AppSettings.Font</c>).</param>
/// <param name="FontSize">The application font size in device-independent pixels (1/96 inch).</param>
/// <param name="Colors">
///  Theme colors as ARGB: the system colors <see cref="ThemeColors.Control"/>, <see cref="ThemeColors.Highlight"/> etc.
///  (as the Git Extensions theme defines them) and every <c>AppColor</c> under <c>"AppColor.&lt;name&gt;"</c>.
/// </param>
/// <param name="MonospaceFontFamily">The font family of hashes (<c>AppSettings.MonospaceFont</c>).</param>
/// <param name="EditorFontFamily">The font family of the file viewer and editor (<c>AppSettings.FixedWidthFont</c>).</param>
/// <param name="EditorFontSize">The font size of the file viewer and editor, in device-independent pixels.</param>
/// <param name="CommitFontFamily">The font family of the commit message (<c>AppSettings.CommitFont</c>).</param>
/// <param name="CommitFontSize">The font size of the commit message, in device-independent pixels.</param>
/// <param name="ControlTheme">
///  The control theme (<c>AppSettings.AvaloniaControlTheme</c>), used by the first options only: Fluent when missing.
/// </param>
public sealed record AvaloniaUiOptions(
    bool IsDarkTheme,
    string? FontFamily,
    double FontSize,
    IReadOnlyDictionary<string, uint>? Colors = null,
    string? MonospaceFontFamily = null,
    string? EditorFontFamily = null,
    double EditorFontSize = 0,
    string? ControlTheme = null,
    string? CommitFontFamily = null,
    double CommitFontSize = 0);

/// <summary>Keys of <see cref="AvaloniaUiOptions.Colors"/>.</summary>
public static class ThemeColors
{
    public const string Control = nameof(Control);
    public const string ControlText = nameof(ControlText);
    public const string Window = nameof(Window);
    public const string WindowText = nameof(WindowText);
    public const string Highlight = nameof(Highlight);
    public const string HighlightText = nameof(HighlightText);
    public const string GrayText = nameof(GrayText);
    public const string HotTrack = nameof(HotTrack);

    /// <summary>Prefix of the keys of <c>AppColor</c> values; they become brush resources with the same key.</summary>
    public const string AppColorPrefix = "AppColor.";
}

/// <summary>
///  Runs Avalonia inside the WinForms process, on the same UI thread (docs/avalonia-port/PLAN.md, "hybrid process").
/// </summary>
public static class AvaloniaUi
{
    /// <summary>The host's (WinForms) synchronization context, captured when Avalonia was set up.</summary>
    private static SynchronizationContext? _hostContext;

    public static bool IsInitialized => Application.Current is GitExtensionsAvaloniaApp;

    /// <summary>
    ///  Reports the exceptions that escape the jobs of the Avalonia dispatcher (the bug report of the application), instead
    ///  of ending the process. Set by the host before the first Avalonia window.
    /// </summary>
    public static Action<Exception>? UnhandledExceptionHandler { get; set; }

    /// <summary>
    ///  Called when the system asks the application to quit (on macOS: Quit of the application menu, Cmd+Q, the Dock) while
    ///  <see cref="RunMainLoop"/> runs, instead of letting the system end the process at once: e.g. closing the main windows,
    ///  which ends the loop as closing the last one does.
    /// </summary>
    public static Action? QuitRequested { get; set; }

    /// <summary>
    ///  On macOS, the lifetime that runs the first loop of <see cref="RunMainLoop"/>: the Quit of the application menu reaches a
    ///  lifetime only (without one, the system ends the process at once). It is set at the setup, as Avalonia requires.
    /// </summary>
    private static ClassicDesktopStyleApplicationLifetime? _macOSLifetime;

    /// <summary>Whether <see cref="RunMainLoop"/> is running, i.e. Avalonia runs the message loop of the process.</summary>
    public static bool IsMainLoopRunning { get; private set; }

    /// <summary>
    ///  Runs the message loop of the process with the Avalonia dispatcher until <paramref name="cancellationToken"/> is
    ///  cancelled (e.g. the last main window closed): Avalonia owns the lifetime of the application (docs/avalonia-port/PLAN.md,
    ///  phase 7, "switch the host"). WinForms dialogs shown meanwhile run their own modal loops as before.
    /// </summary>
    public static void RunMainLoop(CancellationToken cancellationToken)
    {
        IsMainLoopRunning = true;
        try
        {
            if (_macOSLifetime is { } lifetime)
            {
                _macOSLifetime = null;
                RunMainLoopWithLifetime(lifetime, cancellationToken);
            }
            else
            {
                Dispatcher.UIThread.MainLoop(cancellationToken);
            }
        }
        finally
        {
            IsMainLoopRunning = false;
        }
    }

    /// <summary>
    ///  The loop of <see cref="RunMainLoop"/> run by the lifetime of macOS, which asks <see cref="QuitRequested"/> when the system
    ///  asks to quit. It shuts down only when <paramref name="cancellationToken"/> is cancelled, as the loop without lifetime.
    /// </summary>
    private static void RunMainLoopWithLifetime(ClassicDesktopStyleApplicationLifetime lifetime, CancellationToken cancellationToken)
    {
        lifetime.ShutdownRequested += (_, e) =>
        {
            if (QuitRequested is { } quit)
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(quit);
            }
        };
        using CancellationTokenRegistration registration = cancellationToken.Register(() => Dispatcher.UIThread.Post(() => lifetime.Shutdown()));
        lifetime.Start([]);
    }

    /// <summary>
    ///  Runs host (WinForms) code from Avalonia code, e.g. a view model callback that opens a WinForms dialog.
    /// </summary>
    /// <remarks>
    ///  Inside Avalonia's message loop <see cref="SynchronizationContext.Current"/> is Avalonia's context, so
    ///  <c>await</c> continuations in WinForms code would be queued to Avalonia's dispatcher. Avalonia does not run
    ///  those while the dispatcher job that opened a WinForms modal dialog is still on the stack, so the WinForms
    ///  dialog would wait forever (e.g. FormProcess never learning that git has exited). WinForms code therefore
    ///  runs with the WinForms context it expects.
    /// </remarks>
    public static T RunInHostContext<T>(Func<T> hostCode)
    {
        SynchronizationContext? current = SynchronizationContext.Current;
        if (_hostContext is null || ReferenceEquals(current, _hostContext))
        {
            return hostCode();
        }

        SynchronizationContext.SetSynchronizationContext(_hostContext);
        try
        {
            return hostCode();
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(current);
        }
    }

    /// <inheritdoc cref="RunInHostContext{T}(Func{T})"/>
    public static void RunInHostContext(Action hostCode)
        => RunInHostContext(() =>
        {
            hostCode();
            return true;
        });

    /// <summary>
    ///  Sets up Avalonia on the current (UI) thread on first use. Initialising lazily keeps startup unchanged
    ///  while no ported dialog has been opened.
    /// </summary>
    public static void EnsureInitialized(Func<AvaloniaUiOptions> getOptions)
    {
        if (IsInitialized)
        {
            return;
        }

        // The control theme also chooses Wayland decorations, before the backend is initialized.
        AvaloniaUiOptions options = getOptions();

        // WinForms installs its SynchronizationContext on the UI thread, and JoinableTaskFactory captured it at
        // startup. Avalonia's setup replaces it, so restore it: WinForms stays the owner of the process.
        SynchronizationContext? winFormsContext = SynchronizationContext.Current;
        _hostContext = winFormsContext;
        try
        {
            AppBuilder builder = AppBuilder.Configure<GitExtensionsAvaloniaApp>()
                .UsePlatformDetect()
                .UseSkia()
                .UseHarfBuzz();
            if (ShouldUseWayland(OperatingSystem.IsLinux(), Environment.GetEnvironmentVariable("WAYLAND_DISPLAY"), Environment.GetEnvironmentVariable("GITEXTENSIONS_USE_WAYLAND")))
            {
                // KWin's server decorations do not follow the application's color theme. Modern uses the same
                // themed Avalonia decorations as GNOME; other control themes keep the compositor's preference.
#pragma warning disable AVALONIA_WAYLAND_FORCE_CSD // Needed for Modern's light/dark title bars on SSD compositors.
                builder.With(new WaylandPlatformOptions
                {
                    ForceDrawnDecorations = GitExtensionsAvaloniaApp.ResolveControlTheme(options.ControlTheme) == "modern",
                });
#pragma warning restore AVALONIA_WAYLAND_FORCE_CSD
                builder.UseWaylandWithFallback();
            }

            if (OperatingSystem.IsMacOS())
            {
                // Explicit shutdown: the main loop ends as without lifetime (RunMainLoop), not when windows close.
                _macOSLifetime = new ClassicDesktopStyleApplicationLifetime { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                builder.SetupWithLifetime(_macOSLifetime);
            }
            else
            {
                builder.SetupWithoutStarting();
            }
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(winFormsContext);
        }

        ((GitExtensionsAvaloniaApp)Application.Current!).ApplyOptions(options);
        Dispatcher.UIThread.UnhandledException += (_, e) =>
        {
            if (UnhandledExceptionHandler is { } report)
            {
                report(e.Exception);
                e.Handled = true;
            }
        };
    }

    // Prefer native scaling and input in Wayland sessions. Keep an explicit X11 escape hatch
    // while the backend is experimental; initialization failures also fall back to X11.
    internal static bool ShouldUseWayland(bool isLinux, string? waylandDisplay, string? useWayland)
        => isLinux && !string.IsNullOrWhiteSpace(waylandDisplay) && useWayland != "0";

    /// <summary>
    ///  Waits on the UI thread for <paramref name="task"/> of an asynchronous Avalonia API (e.g. the storage provider, the
    ///  clipboard) in a nested dispatcher frame, so that the UI keeps running, for the synchronous callers of the dialogs and
    ///  of the clipboard (docs/avalonia-port/CROSS-PLATFORM.md, phase 2).
    /// </summary>
    public static T WaitFor<T>(Task<T> task)
    {
        VerifyUiThread();
        if (!task.IsCompleted)
        {
            DispatcherFrame frame = new();
#pragma warning disable VSTHRD110, VSTHRD105 // The continuation only ends the frame, on the UI thread.
            task.ContinueWith(_ => Dispatcher.UIThread.Post(() => frame.Continue = false), TaskScheduler.Default);
#pragma warning restore VSTHRD110, VSTHRD105
            Dispatcher.UIThread.PushFrame(frame);
        }

#pragma warning disable VSTHRD002 // The task is complete.
        return task.GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
    }

    internal static void VerifyUiThread() => Dispatcher.UIThread.VerifyAccess();

    /// <summary>
    ///  Remembers the current context as the host's, unless called from Avalonia code.
    /// </summary>
    internal static void RememberHostContext()
    {
        if (SynchronizationContext.Current is { } current and not AvaloniaSynchronizationContext)
        {
            _hostContext = current;
        }
    }
}

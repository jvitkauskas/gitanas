using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.ConsoleEmulation;
using GitUI.ConsoleEmulation.PlainText;
using GitUI.Models;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Services;
using GitUI.Theming;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  The Git Extensions theme as seen by WinForms: the system colors (possibly overridden by the theme) and all
    ///  <see cref="AppColor"/> values.
    /// </summary>
    private static IReadOnlyDictionary<string, uint> GetThemeColors()
    {
        Theme theme = ThemeModule.Settings.Theme;
        Dictionary<string, uint> colors = new()
        {
            [ThemeColors.Control] = Argb(theme.GetNonEmptyColor(KnownColor.Control)),
            [ThemeColors.ControlText] = Argb(theme.GetNonEmptyColor(KnownColor.ControlText)),
            [ThemeColors.Window] = Argb(theme.GetNonEmptyColor(KnownColor.Window)),
            [ThemeColors.WindowText] = Argb(theme.GetNonEmptyColor(KnownColor.WindowText)),
            [ThemeColors.Highlight] = Argb(theme.GetNonEmptyColor(KnownColor.Highlight)),
            [ThemeColors.HighlightText] = Argb(theme.GetNonEmptyColor(KnownColor.HighlightText)),
            [ThemeColors.GrayText] = Argb(theme.GetNonEmptyColor(KnownColor.GrayText)),
            [ThemeColors.HotTrack] = Argb(theme.GetNonEmptyColor(KnownColor.HotTrack)),
        };

        foreach (AppColor name in Theme.AppColorNames)
        {
            colors[ThemeColors.AppColorPrefix + name] = Argb(name.GetThemeColor());
        }

        return colors;

        static uint Argb(Color color) => unchecked((uint)color.ToArgb());
    }

    /// <summary>Lets an Avalonia window own WinForms dialogs (e.g. progress or message boxes it opens).</summary>
    internal sealed class NativeWindowOwner(DialogWindow window) : IWin32Window
    {
        public nint Handle => window.OwnerHandle;
    }

    /// <summary>Stores Avalonia window positions next to the WinForms ones (<c>WindowPositions.xml</c>).</summary>
    internal sealed class WindowPositionStore : IWindowPositionStore
    {
        public static WindowPositionStore Instance { get; } = new();

        public WindowPlacement? Load(string name)
        {
            try
            {
                WindowPosition? position = WindowPositionList.Load()?.Get(name);
                return position is null || position.Rect.IsEmpty
                    ? null
                    : new WindowPlacement(position.Rect.X, position.Rect.Y, position.Rect.Width, position.Rect.Height, position.DeviceDpi, position.State == WindowPositionState.Maximized);
            }
            catch
            {
                // As in WindowPositionManager: a corrupted file must not break the dialog.
                return null;
            }
        }

        public void Save(string name, WindowPlacement placement)
        {
            try
            {
                WindowPositionList? list = WindowPositionList.Load();
                if (list is null)
                {
                    return;
                }

                list.AddOrUpdate(new WindowPosition(
                    new Rectangle(placement.X, placement.Y, placement.Width, placement.Height),
                    placement.Dpi,
                    placement.IsMaximized ? WindowPositionState.Maximized : WindowPositionState.Normal,
                    name));
                list.Save();
            }
            catch
            {
                // As in WindowPositionManager.
            }
        }
    }

    /// <summary>Shows the existing WinForms message boxes, owned by an Avalonia dialog.</summary>
    internal sealed class MessageBoxService(DialogWindow window) : IMessageBoxService
    {
        public void ShowError(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.ShowError(new NativeWindowOwner(window), text, caption));

        public void ShowInformation(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(new NativeWindowOwner(window), text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information));

        public void ShowWarning(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(new NativeWindowOwner(window), text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning));

        public bool Confirm(string text, string caption, bool defaultNo = false)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(
                new NativeWindowOwner(window), text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Question, defaultNo ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1) == DialogResult.Yes);

        public bool? ConfirmWithCancel(string text, string caption)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(new NativeWindowOwner(window), text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch
            {
                DialogResult.Yes => true,
                DialogResult.No => (bool?)false,
                _ => null,
            });
    }

    /// <summary>
    ///  Wraps the configured console runner (ConEmu, Mintty or plain text) for the Avalonia progress dialog, which
    ///  embeds its native window (ConEmu, Mintty) or shows its text (plain text).
    /// </summary>
    private sealed class ConsoleProcess : IConsoleProcess, IDisposable
    {
        private readonly IConsoleCommandRunner _runner;
        private readonly IGitUICommands _commands;
        private readonly string _process;
        private string _arguments;
        private readonly string _workingDirectory;
        private readonly Dictionary<string, string> _environment;

        /// <param name="environment">The environment variables of the process (as <c>FormProcess.ProcessEnvVariables</c>).</param>
        public ConsoleProcess(IConsoleCommandRunner runner, IGitUICommands commands, string process, string arguments, string workingDirectory, Dictionary<string, string>? environment = null)
        {
            _environment = environment ?? [];
            _runner = runner;
            _commands = commands;
            _process = process;
            _arguments = arguments;
            _workingDirectory = workingDirectory;

            runner.CommandOutputReceived += (_, e) => OutputReceived?.Invoke(this, e.Text);
            runner.CommandProcessExited += (_, e) => Exited?.Invoke(this, e.ExitCode);
            runner.ConsoleHostTerminated += (_, _) => HostTerminated?.Invoke(this, EventArgs.Empty);
            if (runner is IPlainTextConsoleCommandRunner plainText)
            {
                plainText.OutputTextWritten += (_, text) => PlainTextWritten?.Invoke(this, text);
            }
        }

        public event EventHandler<string>? OutputReceived;

        public event EventHandler<int>? Exited;

        public event EventHandler? HostTerminated;

        public event EventHandler<string>? PlainTextWritten;

        public bool IsPlainText => _runner is IPlainTextConsoleCommandRunner;

        public IEmbeddedView? View => _runner.View;

        public string Process => _process;

        /// <summary>The arguments of the process, which can be changed before it is retried (e.g. to force a push).</summary>
        public string Arguments
        {
            get => _arguments;
            set => _arguments = value;
        }

        /// <summary>Marshals to the UI thread, as <c>FormProcess</c> does with <c>InvokeAndForget</c>.</summary>
        public void PostToUiThread(Action action) => ThreadHelper.InvokeAndForget(action);

        public void Start() => _runner.StartCommand(_process, _arguments, _workingDirectory, _environment);

        public void Kill()
        {
            try
            {
                _runner.KillCommandProcess();

                GitModule module = new(_commands.GetRequiredService<IGitExecutorProvider>(), _workingDirectory);
                module.UnlockIndex(includeSubmodules: true);
            }
            catch
            {
                // As in FormProcess.KillProcess.
            }
        }

        public void Reset() => _runner.ResetConsole();

        public void WriteInput(string text) => _runner.WriteCommandProcessInput(text);

        public void WriteOutput(string text) => ((IPlainTextConsoleCommandRunner)_runner).WriteOutputText(text);

        public void Dispose() => _runner.Dispose();
    }

    private sealed class ProcessDialogHost(IGitUICommands commands, ConsoleProcess console) : IProcessDialogHost
    {
        public bool CloseProcessDialog
        {
            get => AppSettings.CloseProcessDialog;
            set => AppSettings.CloseProcessDialog = value;
        }

        public bool ShowPasswordInput
        {
            get => AppSettings.ShowProcessDialogPasswordInput.Value;
            set => AppSettings.ShowProcessDialogPasswordInput.Value = value;
        }

        public void RecordHistory(string output)
            => commands.GetRequiredService<IOutputHistoryRecorder>().RecordHistory(new RunProcessInfo(console.Process, console.Arguments, output, DateTime.Now));

        public void SetTaskbarProgress(TaskbarProgressState state, int percent)
        {
            switch (state)
            {
                case TaskbarProgressState.Indeterminate:
                    TaskbarProgress.SetState(TaskbarProgressBarState.Indeterminate);
                    break;
                case TaskbarProgressState.Normal:
                    TaskbarProgress.SetProgress(TaskbarProgressBarState.Normal, percent, 100);
                    break;
                case TaskbarProgressState.Error:
                    TaskbarProgress.SetProgress(TaskbarProgressBarState.Error, percent, 100);
                    break;
            }
        }

        public void ClearTaskbarProgress() => TaskbarProgress.Clear();
    }
}

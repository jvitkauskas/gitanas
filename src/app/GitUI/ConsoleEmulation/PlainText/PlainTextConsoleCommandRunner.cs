using System.Diagnostics;
using System.Text;
using GitCommands;
using GitCommands.Git.Extensions;
using GitCommands.Logging;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Presentation.Services;
using Microsoft;

namespace GitUI.ConsoleEmulation.PlainText;

/// <summary>
///  Runs a process with redirected output, which the progress dialog shows as plain text, when no embedded terminal is
///  being used.
/// </summary>
public sealed class PlainTextConsoleCommandRunner : IPlainTextConsoleCommandRunner
{
    private Process? _process;

    private Action? _logProcessKilled;

    private StreamWriter? _input;

    private bool _isDisposed;

    /// <summary>The progress dialog shows the output (<see cref="OutputTextWritten"/>), there is no window.</summary>
    public IEmbeddedView? View => null;

    public event EventHandler<string>? OutputTextWritten;

    public event EventHandler<ConsoleOutputEventArgs>? CommandOutputReceived;

    public event EventHandler<ConsoleProcessExitEventArgs>? CommandProcessExited;

    // The plain text output never terminates independently; event is required by the interface.
#pragma warning disable CS0067
    public event EventHandler? ConsoleHostTerminated;
#pragma warning restore CS0067

    public void WriteOutputText(string text)
    {
        if (!_isDisposed)
        {
            OutputTextWritten?.Invoke(this, text);
        }
    }

    public void WriteCommandProcessInput(string text)
    {
        Validates.NotNull(_input);
        _input.Write(text);
    }

    public void KillCommandProcess()
    {
        ThreadHelper.ThrowIfNotOnUIThread();

        if (_process is null)
        {
            return;
        }

        _logProcessKilled?.Invoke();

        try
        {
            _process.TerminateTree();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }

        _process.Dispose();
        _process = null;
        _input?.Dispose();
        _input = null;
        CommandProcessExited?.Invoke(this, new ConsoleProcessExitEventArgs(-1));
    }

    public void ResetConsole()
    {
        KillCommandProcess();
    }

    public void StartCommand(string command, string arguments, string workDir, Dictionary<string, string> envVariables)
    {
        ProcessOperation operation = CommandLog.LogProcessStart(command, arguments, workDir);

        WriteOutputText($"{command.Quote()} {arguments}{Environment.NewLine}");

        try
        {
            EnvironmentConfiguration.SetEnvironmentVariables();

            KillCommandProcess();

            _logProcessKilled = () => operation.LogProcessEnd(new Exception("Process killed"));

            // process used to execute external commands
            Encoding outputEncoding = GitModule.SystemEncoding;
            ProcessStartInfo startInfo = new()
            {
                UseShellExecute = false,
                ErrorDialog = false,
                CreateNoWindow = !AppSettings.ShowGitCommandLine,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = outputEncoding,
                StandardErrorEncoding = outputEncoding,
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workDir
            };

            foreach ((string name, string value) in envVariables)
            {
                startInfo.EnvironmentVariables.Add(name, value);
            }

            _process = new() { StartInfo = startInfo, EnableRaisingEvents = true };

            AsyncStreamReader? outputReader = null;
            AsyncStreamReader? errorReader = null;

            _process.Exited += delegate
            {
                ThreadHelper.FileAndForget(async () =>
                    {
                        if (_process is null)
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            operation.LogProcessEnd(new Exception("Process instance is null in Exited event"));
                            return;
                        }

                        // The process is exited already, but this command waits also until all output is received.
                        // Only WaitForExit when someone is connected to the exited event. For some reason a
                        // null reference is thrown sometimes when staging/unstaging in the commit dialog when
                        // we wait for exit, probably a timing issue...
                        try
                        {
                            // WaitForExit[Async] blocks here for unknown reason if the process has already exited
                            if (!_process.HasExited)
                            {
                                await _process.WaitForExitAsync();
                            }

                            _logProcessKilled = null;

                            if (_process is null)
                            {
                                // The process has been killed meanwhile.
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                            operation.LogProcessEnd(ex);
                        }

                        int exitCode = _process.ExitCode;

                        using CancellationTokenSource eofTimeoutTokenSource = new(millisecondsDelay: 5000);

                        if (outputReader is not null)
                        {
                            await outputReader.WaitUntilEofAsync(eofTimeoutTokenSource.Token);
                            outputReader.Dispose();
                        }

                        if (errorReader is not null)
                        {
                            await errorReader.WaitUntilEofAsync(eofTimeoutTokenSource.Token);
                            errorReader.Dispose();
                        }

                        await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                        operation.LogProcessEnd(exitCode);
                        WriteOutputText("Done");
                        _process.Dispose();
                        _process = null;
                        await _input!.DisposeAsync();
                        _input = null;
                        CommandProcessExited?.Invoke(this, new ConsoleProcessExitEventArgs(exitCode));
                    });
            };

            _process.StartInOwnProcessGroup();
            operation.SetProcessId(_process.Id);
            _input = _process.StandardInput;
            outputReader = new AsyncStreamReader(_process.StandardOutput, ForwardOutput);
            errorReader = new AsyncStreamReader(_process.StandardError, ForwardOutput);
        }
        catch (Exception ex)
        {
            operation.LogProcessEnd(ex);
            ex.Data.Add("command", command);
            ex.Data.Add("arguments", arguments);
            throw;
        }

        return;

        void ForwardOutput(string output)
        {
            output = output.Replace("\r\n", "\n");

            for (int startIndex = 0; startIndex < output.Length;)
            {
                int nextLineStart = output.IndexOfAny(Delimiters.LineFeedAndCarriageReturnSearchValues, startIndex) + 1;
                if (nextLineStart == 0)
                {
                    nextLineStart = output.Length;
                }

                string outputLine = output[startIndex..nextLineStart];
                CommandOutputReceived?.Invoke(this, new ConsoleOutputEventArgs(outputLine));

                startIndex = nextLineStart;
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        KillCommandProcess();
        _isDisposed = true;
        _process?.Dispose();
        _process = null;
        _input?.Dispose();
        _input = null;
    }
}

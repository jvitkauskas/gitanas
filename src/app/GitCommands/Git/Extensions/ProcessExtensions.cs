using System.Diagnostics;
using System.Runtime.InteropServices;
namespace GitCommands.Git.Extensions;

public static class ProcessExtensions
{
    /// <summary>
    /// Starts a console command in an isolated Linux process group when setsid is available.
    /// Descendant traversal remains the fallback on other platforms and minimal installations.
    /// Inspired by Nikola's Avalonia port (gitextensions/gitextensions#13189).
    /// </summary>
    public static bool StartInOwnProcessGroup(this Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (OperatingSystem.IsLinux() && !process.StartInfo.UseShellExecute)
        {
            string? launcher = FindSetsid();
            if (launcher is not null)
            {
                WrapInProcessGroup(process.StartInfo, launcher);
            }
        }

        return process.Start();
    }

    internal static string? FindSetsid(string? searchPath = null)
    {
        foreach (string directory in (searchPath ?? Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory, "setsid");
            if (Path.IsPathFullyQualified(candidate) && File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    internal static void WrapInProcessGroup(ProcessStartInfo startInfo, string launcher)
    {
        string command = startInfo.FileName;
        startInfo.FileName = launcher;
        if (startInfo.ArgumentList.Count > 0)
        {
            startInfo.ArgumentList.Insert(0, command);
            startInfo.ArgumentList.Insert(0, "--");
        }
        else
        {
            startInfo.Arguments = $"-- {command.Quote()} {startInfo.Arguments}";
        }
    }

    public static void TerminateTree(this Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (process.HasExited)
        {
            return;
        }

        if (!OperatingSystem.IsWindows())
        {
            // Capture the group before killing the leader. Never signal the application's
            // inherited group: only a command that is itself the group leader is safe.
            int processId = process.Id;
            int group = OperatingSystem.IsLinux() || OperatingSystem.IsMacOS()
                ? NativeMethods.GetProcessGroupId(processId)
                : -1;
            try
            {
                process.Kill(entireProcessTree: true);
            }
            finally
            {
                if (group == processId)
                {
                    // Also catches helpers reparented while the process tree was traversed.
                    NativeMethods.Kill(-group, 9);
                }
            }

            return;
        }

        if (OperatingSystem.IsWindows())
        {
            // Send Ctrl+C
            NativeMethods.AttachConsole(process.Id);
            NativeMethods.SetConsoleCtrlHandler(IntPtr.Zero, add: true);
            NativeMethods.GenerateConsoleCtrlEvent(0, 0);

            if (!process.HasExited)
            {
                process.WaitForExit(500);
            }
        }

        if (!process.HasExited)
        {
            process.Kill();
        }
    }

    private static class NativeMethods
    {
        [DllImport("libc", EntryPoint = "getpgid", SetLastError = true)]
        public static extern int GetProcessGroupId(int processId);

        [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
        public static extern int Kill(int processId, int signal);

        [DllImport("kernel32.dll")]
        public static extern bool SetConsoleCtrlHandler(IntPtr handlerRoutine, bool add);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, int dwProcessGroupId);
    }
}

using System.Diagnostics;
using System.Text;
using GitCommands;
using GitCommands.Git.Extensions;
using GitExtensions.Extensibility;

namespace GitCommandsTests.Git;

[TestFixture]
public sealed class ProcessExtensionsTests
{
    [TestCase(true)]
    [TestCase(false)]
    [Platform(Include = "Linux")]
    public async Task Cancel_console_command_kills_children_including_without_a_private_group(bool isolate)
    {
        using Process process = Shell("sleep 60 & echo $!; wait");
        int childId = 0;
        try
        {
            if (isolate)
            {
                process.StartInOwnProcessGroup().Should().BeTrue();
            }
            else
            {
                process.Start().Should().BeTrue();
            }

            childId = int.Parse((await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!);
            if (isolate && ProcessExtensions.FindSetsid() is not null)
            {
                string stat = File.ReadAllText($"/proc/{process.Id}/stat");
                string[] fields = stat[(stat.LastIndexOf(')') + 2)..].Split(' ');
                int.Parse(fields[2]).Should().Be(process.Id, "the wrapper must keep the command as its own group leader");
            }

            process.TerminateTree();
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await AssertStoppedAsync(childId);
        }
        finally
        {
            Cleanup(process);
            Cleanup(childId);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    [Platform(Include = "Linux")]
    public async Task Process_group_launcher_preserves_executable_and_arguments(bool useArgumentList)
    {
        string directory = Path.Combine(Path.GetTempPath(), $"ge process {Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string shell = Path.Combine(directory, "shell with spaces");
        File.CreateSymbolicLink(shell, "/bin/sh");
        using Process process = new() { StartInfo = new(shell) { UseShellExecute = false, RedirectStandardOutput = true } };
        if (useArgumentList)
        {
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("printf '%s' \"$1\"");
            process.StartInfo.ArgumentList.Add("probe");
            process.StartInfo.ArgumentList.Add("spaces ' quotes $ ;");
        }
        else
        {
            process.StartInfo.Arguments = "-c \"printf '%s' \\\"$1\\\"\" probe \"spaces ' quotes $ ;\"";
        }

        try
        {
            process.StartInOwnProcessGroup().Should().BeTrue();
            string output = await process.StandardOutput.ReadToEndAsync().WaitAsync(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            process.ExitCode.Should().Be(0);
            output.Should().Be("spaces ' quotes $ ;");
            process.TerminateTree(); // Already exited is harmless.
        }
        finally
        {
            Cleanup(process);
            Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void Missing_launcher_uses_the_normal_process_start_path()
    {
        ProcessExtensions.FindSetsid(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))).Should().BeNull();
    }

    [Test]
    [Platform(Include = "Linux")]
    public async Task Disposing_a_cancelled_executable_kills_its_children()
    {
        using CancellationTokenSource cancellation = new();
        IProcess process = new Executable("/bin/sh").Start(
            "-c \"sleep 60 & echo $!; wait\"", redirectOutput: true, outputEncoding: Encoding.UTF8, cancellationToken: cancellation.Token);
        int childId = 0;
        try
        {
            childId = int.Parse((await process.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5)))!);
            await cancellation.CancelAsync();
            process.Dispose();
            await AssertStoppedAsync(childId);
        }
        finally
        {
            if (!cancellation.IsCancellationRequested)
            {
                await cancellation.CancelAsync();
                process.Dispose();
            }

            Cleanup(childId);
        }
    }

    private static Process Shell(string script)
    {
        Process process = new() { StartInfo = new("/bin/sh") { UseShellExecute = false, RedirectStandardOutput = true } };
        process.StartInfo.ArgumentList.Add("-c");
        process.StartInfo.ArgumentList.Add(script);
        return process;
    }

    private static async Task AssertStoppedAsync(int processId)
    {
        // A killed orphan may remain a zombie until PID 1 reaps it in a container.
        for (int attempt = 0; attempt < 50; attempt++)
        {
            string path = $"/proc/{processId}/stat";
            try
            {
                string stat = File.ReadAllText(path);
                if (stat[(stat.LastIndexOf(')') + 2)..].StartsWith('Z'))
                {
                    return;
                }
            }
            catch (FileNotFoundException)
            {
                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }

            await Task.Delay(20);
        }

        Assert.Fail($"Child process {processId} is still running after cancellation.");
    }

    private static void Cleanup(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit(5000);
        }
    }

    private static void Cleanup(int processId)
    {
        if (processId == 0)
        {
            return;
        }

        try
        {
            using Process process = Process.GetProcessById(processId);
            Cleanup(process);
        }
        catch (ArgumentException)
        {
            // Already reaped.
        }
    }
}

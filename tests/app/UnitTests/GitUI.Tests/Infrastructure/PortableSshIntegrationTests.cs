using GitCommands;
using GitExtensions.Extensibility;
using GitUI;
using NSubstitute;

namespace GitUITests.Infrastructure;

[TestFixture]
public sealed class PortableSshIntegrationTests
{
    [TestCase(@"C:\PuTTY\plink.exe")]
    [TestCase(@"C:\TortoiseGit\TortoisePlink.exe")]
    public void Imported_PuTTY_command_only_enables_integration_on_Windows(string command)
    {
        string original = AppSettings.SshPath;
        try
        {
            AppSettings.SshPath = command;

            GitSshHelpers.IsPlink.Should().Be(OperatingSystem.IsWindows());
            AppSettings.SshPath.Should().Be(command, "the custom SSH command must be preserved");
        }
        finally
        {
            AppSettings.SshPath = original;
        }
    }

    [Test]
    [Platform(Exclude = "Win")]
    public void PuTTY_host_key_recovery_does_not_prompt_or_launch_Windows_processes_off_Windows()
    {
        ProcessDialogs.AskForCacheHostkey(Substitute.For<IWin32Window>(), "ssh://example.invalid/repo")
            .Should().BeFalse();
    }
}

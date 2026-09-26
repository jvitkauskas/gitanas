namespace GitCommands;

public static class GitSshHelpers
{
    /// <summary>Sets the git SSH command path.</summary>
    public static void SetGitSshEnvironmentVariable(string path)
    {
        // Git will use the embedded OpenSSH ssh.exe if empty/unset
        Environment.SetEnvironmentVariable("GIT_SSH", path?.Length is > 0 ? path : null, EnvironmentVariableTarget.Process);
    }

    // The PuTTY integration uses Windows processes and host-key storage. An imported path remains a custom SSH command
    // on other platforms. Variants like TortoisePlink.exe are supported on Windows too.
    public static bool IsPlink => OperatingSystem.IsWindows() && AppSettings.SshPath.EndsWith("plink.exe", StringComparison.CurrentCultureIgnoreCase);
}

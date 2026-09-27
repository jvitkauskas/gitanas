using GitExtUtils;

namespace GitExtUtilsTests;

/// <summary>The executable of the application, also when it is started by the host of .NET (<c>dotnet Gitanas.dll</c>).</summary>
public sealed class ApplicationInfoTests
{
    private static readonly string AppFolder = Path.Join(Path.GetTempPath(), "app");
    private static readonly string EntryAssembly = Path.Join(AppFolder, "Gitanas.dll");
    private static readonly string AppHost = Path.Join(AppFolder, OperatingSystem.IsWindows() ? "Gitanas.exe" : "Gitanas");

    [Test]
    public void The_executable_of_the_process_is_the_executable()
    {
        ApplicationInfo.GetExecutablePath(AppHost, EntryAssembly, _ => true).Should().Be(AppHost);
    }

    [TestCase("dotnet")]
    [TestCase("dotnet.exe")]
    public void Under_the_host_of_dotnet_the_executable_is_the_one_next_to_the_entry_assembly(string host)
    {
        string dotnet = Path.Join(Path.GetTempPath(), "sdk", host);

        ApplicationInfo.GetExecutablePath(dotnet, EntryAssembly, path => path == AppHost).Should().Be(AppHost);
        ApplicationInfo.GetExecutablePath(dotnet, EntryAssembly, _ => false).Should().Be(EntryAssembly, "without an executable, the folder is still the one of the application");
        ApplicationInfo.GetExecutablePath(dotnet, entryAssemblyPath: null, _ => true).Should().Be(dotnet);
    }
}

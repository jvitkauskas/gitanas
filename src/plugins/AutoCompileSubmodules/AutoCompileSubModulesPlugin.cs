using System.ComponentModel.Composition;
using System.Text;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Plugins.AutoCompileSubmodules.Properties;
using ResourceManager;

namespace GitExtensions.Plugins.AutoCompileSubmodules;

[Export(typeof(IGitPlugin))]
public class AutoCompileSubModulesPlugin : GitPluginBase, IGitPluginForRepository
{
    private readonly TranslationString _doYouWantBuild =
        new("Do you want to build {0}?\n\n{1}");
    private readonly TranslationString _enterCorrectMsBuildPath =
        new("Please enter correct MSBuild path in the plugin settings dialog and try again.");

    public AutoCompileSubModulesPlugin() : base(true)
    {
        Id = new Guid("D4D1ACB7-0B6B-4A3C-B0DB-A25056A277D9");
        Name = "Auto compile submodules";
        Translate(AppSettings.CurrentTranslation);
        IconImage = PluginImage.FromResource(GetType().Assembly, "PluginIcon.png");
    }

    private readonly BoolSetting _msBuildEnabled = new("Enabled", false);
    private readonly StringSetting _msBuildPath = new("Path to msbuild.exe", "Path to MSBuild or dotnet", FindMsBuild());
    private readonly StringSetting _msBuildArguments = new("msbuild.exe arguments", "MSBuild arguments", "/p:Configuration=Debug");

    private const string DefaultMsBuildPath = @"C:\Windows\Microsoft.NET\Framework\v3.5\msbuild.exe";

    private static string FindMsBuild()
    {
        if (OperatingSystem.IsWindows())
        {
            return File.Exists(DefaultMsBuildPath) ? DefaultMsBuildPath : "";
        }

        // Off Windows: msbuild (Mono) or dotnet (dotnet msbuild) on the PATH (docs/avalonia-port/CROSS-PLATFORM.md, phase 3).
        return PathUtil.TryFindFullPath("msbuild", out string? msbuild) ? msbuild
            : PathUtil.TryFindFullPath("dotnet", out string? dotnet) ? dotnet
            : "";
    }

    /// <summary>The msbuild of the settings: a path, or a program on the PATH.</summary>
    private static string? ResolveMsBuild(string? msbuildPath)
        => string.IsNullOrEmpty(msbuildPath) ? null
            : File.Exists(msbuildPath) ? msbuildPath
            : PathUtil.TryFindFullPath(msbuildPath, out string? onPath) ? onPath
            : null;

    #region IGitPlugin Members

    public override IEnumerable<ISetting> GetSettings()
    {
        yield return _msBuildEnabled;
        yield return _msBuildPath;
        yield return _msBuildArguments;
    }

    public override void Register(IGitUICommands gitUiCommands)
    {
        // Connect to events
        gitUiCommands.PostUpdateSubmodules += GitUiCommandsPostUpdateSubmodules;
    }

    public override void Unregister(IGitUICommands gitUiCommands)
    {
        // Connect to events
        gitUiCommands.PostUpdateSubmodules -= GitUiCommandsPostUpdateSubmodules;
    }

    public override bool Execute(GitUIEventArgs args)
    {
        // Only build when plugin is enabled
        if (string.IsNullOrEmpty(args.GitModule.WorkingDir))
        {
            return false;
        }

        string msbuildPath = _msBuildPath.ValueOrDefault(Settings);

        DirectoryInfo workingDir = new(args.GitModule.WorkingDir);
        FileInfo[] solutionFiles = workingDir.GetFiles("*.sln", SearchOption.AllDirectories);

        for (int n = solutionFiles.Length - 1; n > 0; n--)
        {
            FileInfo solutionFile = solutionFiles[n];

            PluginMessageBoxResult result =
                PluginMessageBoxes.Show(args.Owner,
                    string.Format(_doYouWantBuild.Text,
                                  solutionFile.Name,
                                  SolutionFilesToString(solutionFiles)),
                    "Build",
                    PluginMessageBoxButtons.YesNoCancel,
                    PluginMessageBoxIcon.Question);

            if (result == PluginMessageBoxResult.Cancel)
            {
                return false;
            }

            if (result != PluginMessageBoxResult.Yes)
            {
                continue;
            }

            if (ResolveMsBuild(msbuildPath) is not string msbuild)
            {
                PluginMessageBoxes.ShowError(args.Owner, _enterCorrectMsBuildPath.Text);
            }
            else
            {
                // dotnet builds with its msbuild command.
                string command = Path.GetFileNameWithoutExtension(msbuild).Equals("dotnet", StringComparison.OrdinalIgnoreCase) ? "msbuild " : "";
                args.GitUICommands.StartCommandLineProcessDialog(args.Owner, msbuild, command + solutionFile.FullName.Quote() + " " + _msBuildArguments.ValueOrDefault(Settings));
            }
        }

        return false;
    }

    #endregion

    /// <summary>
    ///   Automatically compile all solution files found in any submodule
    /// </summary>
    private void GitUiCommandsPostUpdateSubmodules(object? sender, GitUIPostActionEventArgs e)
    {
        if (e.ActionDone && _msBuildEnabled.ValueOrDefault(Settings))
        {
            Execute(e);
        }
    }

    private static string SolutionFilesToString(IReadOnlyList<FileInfo> solutionFiles)
    {
        StringBuilder solutionString = new();

        for (int n = solutionFiles.Count - 1; n > 0; n--)
        {
            FileInfo solutionFile = solutionFiles[n];
            solutionString.Append(solutionFile.Name);
            solutionString.Append('\n');
        }

        return solutionString.ToString();
    }
}

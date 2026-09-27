using GitCommands;
using GitCommands.ExternalLinks;
using GitCommands.Settings;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Settings;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;
using GitUIPluginInterfaces.BuildServerIntegration;
using static GitUI.AvaloniaTests.ViewModels.SettingsDialogViewModelTests;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>
///  View model tests of the ported settings pages of the Git settings, SSH, build server integration, revision links and
///  shell extension.
/// </summary>
[TestFixture]
public sealed class SettingsPagesBatchBViewModelTests
{
    [Test]
    public void The_advanced_git_config_page_shows_the_git_booleans_as_three_state_check_boxes()
    {
        FakePagesHost host = new();
        GitConfigSources sources = new();
        sources.Local.SetValue("pull.rebase", "yes");
        sources.Local.SetValue("fetch.prune", "0");
        sources.Local.SetValue("rerere.enabled", "maybe");
        GitConfigAdvancedSettingsPageViewModel page = new(new GitConfigAdvancedSettingsPageStrings(), host, supportsUpdateRefs: false);
        SettingsDialogViewModel dialog = CreateDialog(page, sources.Levels);
        dialog.Open(page.PageName);

        page.Levels.Select(l => l.Level).Should().Equal(SettingsLevel.Effective, SettingsLevel.Local, SettingsLevel.Global, SettingsLevel.System);
        page.Flags[0].Text.Should().Be("Rebase local branch when pulling (instead of merge) [pull.rebase]");
        page.Flags.Single(f => f.Key == "rebase.updaterefs").IsVisible.Should().BeFalse("git does not support it");

        page.Level = SettingsLevel.Local;
        Value("pull.rebase").Should().BeTrue();
        Value("fetch.prune").Should().BeFalse();
        Value("rerere.enabled").Should().BeNull("not a boolean");
        Value("merge.autostash").Should().BeNull("unset");

        page.Flags.Single(f => f.Key == "merge.autostash").Value = true;
        page.Flags.Single(f => f.Key == "pull.rebase").Value = null;
        page.Level = SettingsLevel.Global;

        sources.Local.GetValue("merge.autostash").Should().Be("true");
        sources.Local.GetValue("pull.rebase").Should().BeNull();
        sources.Local.GetValue("fetch.prune").Should().Be("false");
        host.GitConfigSaved.Should().Be(1, "the git config is saved with the page");

        bool? Value(string key) => page.Flags.Single(f => f.Key == key).Value;
    }

    [Test]
    public void The_git_config_page_edits_the_user_the_tools_and_the_line_endings()
    {
        FakePagesHost host = new();
        GitConfigSources sources = new();
        sources.Local.SetValue("user.name", "John");
        sources.Local.SetValue("user.email", "john@example.com");
        sources.Local.SetValue("core.autocrlf", "input");
        sources.Local.SetValue("diff.guitool", "custom");
        sources.Local.SetValue("difftool.custom.path", "C:/tools/custom.exe");
        sources.Local.SetValue("difftool.custom.cmd", "custom $LOCAL $REMOTE");
        GitConfigSettingsPageViewModel page = new(new GitConfigSettingsPageStrings(), host, new SmallDialogViewModelTests.FakeFileDialogs(), workingDir: null);
        SettingsDialogViewModel dialog = CreateDialog(page, sources.Levels);
        dialog.Open(page.PageName);
        page.Level = SettingsLevel.Local;

        page.UserName.Should().Be("John");
        page.UserEmail.Should().Be("john@example.com");
        page.AutoCrlfInput.Should().BeTrue();
        page.AutoCrlfNotSet.Should().BeFalse();
        page.DiffTool.Should().Be("custom");
        page.DiffToolPath.Should().Be("C:/tools/custom.exe");
        page.DiffToolCommand.Should().Be("custom $LOCAL $REMOTE");
        page.IsDiffToolSet.Should().BeTrue();
        page.IsCredentialHelperVisible.Should().BeFalse("only the settings of git config files have all the values of the credential helper");
        page.Editors.Should().Equal("notepad");
        page.CredentialHelpers.Should().Contain(["store", "cache"]);

        page.DiffTool = "";
        page.DiffToolPath.Should().BeEmpty("the tool has no path configured");
        page.IsDiffToolSet.Should().BeFalse();
        page.UserName = "Jane";
        page.AutoCrlfInput = false;
        page.AutoCrlfFalse = true;
        page.Level = SettingsLevel.Global;

        sources.Local.GetValue("user.name").Should().Be("Jane");
        sources.Local.GetValue("core.autocrlf").Should().Be("false");
        sources.Local.GetValue("diff.guitool").Should().BeEmpty("the diff tool is unset");
        host.GitConfigSaved.Should().Be(1);

        // As PageToSettings: without git, only the encoding is saved.
        host.CanFindGit = false;
        dialog.GotoPage("DetailedSettingsPage");
        dialog.GotoPage(page.PageName);
        page.CanFindGitCmd.Should().BeFalse();
        page.IsInvalidGitPathVisible.Should().BeTrue();
        page.UserName = "Nobody";
        page.Level = SettingsLevel.Local;
        sources.Global.GetValue("user.name").Should().BeNull();
    }

    [Test]
    public void The_git_config_page_controller_opens_the_file_picker_in_the_directory_of_the_tool()
    {
        GitConfigSettingsPageController controller = new();
        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

        controller.GetInitialDirectory(null, null).Should().Be(programFiles);
        controller.GetInitialDirectory("", "").Should().Be(programFiles);

        string tempFolder = Path.GetTempPath();
        controller.GetInitialDirectory(tempFolder, null).Should().Be(tempFolder);
        controller.GetInitialDirectory(tempFolder[..^1], null).Should().Be(tempFolder);
        controller.GetInitialDirectory(null, Path.Combine(tempFolder, "tool.exe")).Should().Be(tempFolder, "the preferred path of the tool is used next");
    }

    [Test]
    public void The_paths_page_shows_HOME_and_validates_the_git_path_typed() => WithAppSettings(() =>
    {
        FakePagesHost host = new() { GitEnvironment = (null, @"C:\Users\me") };
        SmallDialogViewModelTests.FakeFileDialogs fileDialogs = new() { Files = [@"C:\Git\cmd\git.exe"] };
        GitSettingsPageViewModel page = new(new GitSettingsPageStrings(), host, fileDialogs);
        SettingsDialogViewModel dialog = CreateDialog(page, new FakeSources().GlobalOnly);
        dialog.Open(page.PageName);

        page.HomeIsSetTo.Should().Be(OperatingSystem.IsWindows()
            ? @"%HOME% is set to: C:\Users\me    (%GIT_CONFIG_GLOBAL% is not set.)"
            : @"$HOME is set to: C:\Users\me    ($GIT_CONFIG_GLOBAL is not set.)");
        page.GitPath.Should().Be("git");
        host.SolvedGitCommands.Should().BeEmpty("the paths shown are not validated");

        page.GitPath = @" D:\git.exe ";
        host.SolvedGitCommands.Should().Equal(@"D:\git.exe");

        page.BrowseGitPathCommand.Execute(null);
        page.GitPath.Should().Be(@"C:\Git\cmd\git.exe");

        page.ChangeHomeCommand.Execute(null);
        host.FixHomeShown.Should().Be(1);
        host.GitCommandValue.Should().Be(@"C:\Git\cmd\git.exe", "the settings are saved before HOME is changed");

        host.GitEnvironment = (@"D:\config", @"C:\Users\me");
        dialog.LoadAll();
        page.HomeIsSetTo.Should().Be(OperatingSystem.IsWindows()
            ? @"%GIT_CONFIG_GLOBAL% is set to: D:\config"
            : @"$GIT_CONFIG_GLOBAL is set to: D:\config");
    });

    [Test]
    public void An_imported_PuTTY_path_remains_editable_on_other_platforms() => WithAppSettings(() =>
    {
        FakePagesHost host = new() { SshPath = @"C:\PuTTY\plink.exe" };
        SshSettingsPageViewModel page = new(new SshSettingsPageStrings(), host, new SmallDialogViewModelTests.FakeFileDialogs());
        SettingsDialogViewModel dialog = CreateDialog(page, new FakeSources().GlobalOnly);
        dialog.Open(page.PageName);

        page.IsPutty.Should().Be(OperatingSystem.IsWindows());
        page.IsOther.Should().Be(!OperatingSystem.IsWindows());
        if (!OperatingSystem.IsWindows())
        {
            page.OtherSsh.Should().Be(host.SshPath, "the unsupported path must stay visible for correction");
            dialog.SaveAll();
            host.SshPath.Should().Be(@"C:\PuTTY\plink.exe", "opening settings must not silently change the configured SSH command");
        }
    });

    [Test]
    [Platform(Include = "Win")] // PuTTY is found in the folders of Windows (docs/avalonia-port/CROSS-PLATFORM.md, phase 4).
    public void The_ssh_page_selects_the_client_and_finds_PuTTY() => WithAppSettings(() =>
    {
        string puttyDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(puttyDir);
        try
        {
            foreach (string file in new[] { "plink.exe", "puttygen.exe", "pageant.exe" })
            {
                File.WriteAllText(Path.Combine(puttyDir, file), "");
            }

            FakePagesHost host = new() { PuttyLocations = [puttyDir] };
            SshSettingsPageViewModel page = new(new SshSettingsPageStrings(), host, new SmallDialogViewModelTests.FakeFileDialogs());
            SettingsDialogViewModel dialog = CreateDialog(page, new FakeSources().GlobalOnly);
            AppSettings.Plink = "";
            dialog.Open(page.PageName);

            page.IsOpenSsh.Should().BeTrue();
            page.IsPutty.Should().BeFalse();

            page.IsPutty = true;
            page.IsOpenSsh.Should().BeFalse("the clients are exclusive");
            page.PlinkPath.Should().Be(Path.Combine(puttyDir, "plink.exe"), "PuTTY is found where it is installed");
            page.PageantPath.Should().Be(Path.Combine(puttyDir, "pageant.exe"));

            dialog.SaveAll();
            host.GitSsh.Should().Be(Path.Combine(puttyDir, "plink.exe"));
            host.SshPath.Should().Be(Path.Combine(puttyDir, "plink.exe"));

            page.IsOther = true;
            page.OtherSsh = "ssh.exe";
            dialog.SaveAll();
            host.SshPath.Should().Be("ssh.exe");
            dialog.LoadAll();
            page.IsOther.Should().BeTrue();
            page.OtherSsh.Should().Be("ssh.exe");
        }
        finally
        {
            Directory.Delete(puttyDir, recursive: true);
        }
    });

    [Test]
    public void The_build_server_page_shows_the_settings_once_the_plugins_are_found()
    {
        FakePagesHost host = new();
        FakeSources sources = new();
        sources.Local.SetValue("BuildServer.Type", "Mock");
        sources.Local.SetValue("BuildServer.EnableIntegration", "true");
        BuildServerIntegrationSettingsPageViewModel page = new(new BuildServerIntegrationSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = CreateDialog(page, sources.Distributed);
        dialog.Open(page.PageName);
        page.Level = SettingsLevel.Local;

        page.IsLoaded.Should().BeFalse();
        page.SelectedBuildServerType.Should().BeNull("the settings are shown once the plugins are found");

        host.BuildServerTypes.SetResult(["Mock"]);
        page.IsLoaded.Should().BeTrue();
        page.BuildServerTypes.Should().Equal("None", "Mock");
        page.SelectedBuildServerType.Should().Be("Mock");
        page.IntegrationEnabled.Should().BeTrue();
        page.ShowBuildResultPage.Should().BeNull("unset in the local settings");
        FakeBuildServerSettingsControl control = host.Controls.Single();
        page.SettingsControl.Should().BeSameAs(control);
        control.Loaded.Should().Equal("Mock");

        page.IntegrationEnabled = false;
        page.Level = SettingsLevel.Global;
        sources.Local.GetValue("BuildServer.EnableIntegration").Should().Be("false");
        control.Saved.Should().Equal("Mock");
        control.IsDisposed.Should().BeTrue("the control of the level shown before is replaced");

        page.SelectedBuildServerType = "None";
        page.SettingsControl.Should().BeNull();
        page.Dispose();
    }

    [Test]
    public void The_build_server_page_shows_the_settings_declared_by_a_plugin_of_API_v2()
    {
        FakePagesHost host = new();
        FakeSources sources = new();
        sources.Local.SetValue("BuildServer.Type", "Mock");
        sources.Local.SetValue("BuildServer.Mock.ServerUrl", "https://ci.example.org");
        StringSetting serverUrl = new("ServerUrl", "Server URL", defaultValue: "");
        StringSetting projectName = new("ProjectName", "Project name", defaultValue: "");
        string? error = null;
        List<string> shownErrors = [];
        host.PluginSettingsFactory = buildServerType =>
        {
            PluginSettingsPageViewModel settings = new(new PluginSettingsPageStrings(), new SettingValueStrings(), buildServerType, "BuildServerIntegrationSettingsPage", s => s);
            settings.AddRow(PluginSettingRow.ForValue(serverUrl.Caption, new StringSettingValue(serverUrl)));
            settings.AddRow(PluginSettingRow.ForValue(projectName.Caption, new StringSettingValue(projectName)));
            settings.AddRow(PluginSettingRow.ForAction(null, "Choose", values => projectName[values] = $"chosen on {serverUrl.ValueOrDefault(values)}"));
            BuildServerSettingsContext context = new("repo", []);
            context.SuggestValue(projectName, context.DefaultProjectName);
            return new BuildServerPluginSettings(settings, context.WithSuggestedValues, _ => error, shownErrors.Add);
        };
        BuildServerIntegrationSettingsPageViewModel page = new(new BuildServerIntegrationSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = CreateDialog(page, sources.Distributed);
        dialog.Open(page.PageName);
        page.Level = SettingsLevel.Local;
        host.BuildServerTypes.SetResult(["Mock"]);

        page.SettingsControl.Should().BeNull("the plugin declares its settings");
        host.Controls.Should().BeEmpty();
        PluginSettingsPageViewModel shown = page.PluginSettings!.Page;
        ((StringSettingValue)shown.Rows[0].Value!).Value.Should().Be("https://ci.example.org");
        ((StringSettingValue)shown.Rows[1].Value!).Value.Should().Be("repo", "the suggested value is shown while unset");

        // Invalid: nothing is saved.
        error = "invalid";
        page.Level = SettingsLevel.Global;
        shownErrors.Should().Equal("invalid");
        sources.Local.GetValue("BuildServer.Mock.ProjectName").Should().BeNull();
        page.PluginSettings.Should().BeNull("no build server at the global level");

        error = null;
        page.Level = SettingsLevel.Local;
        shown = page.PluginSettings!.Page;
        shown.Rows[2].Activate();
        ((StringSettingValue)shown.Rows[1].Value!).Value.Should().Be("chosen on https://ci.example.org", "the link edits the values shown");
        page.Level = SettingsLevel.Global;
        sources.Local.GetValue("BuildServer.Mock.ProjectName").Should().Be("chosen on https://ci.example.org");
        page.Dispose();
    }

    [Test]
    public void The_revision_links_page_edits_the_categories_and_their_links()
    {
        using DistributedSettingsFiles files = new();
        FakePagesHost host = new() { Remotes = [new Remote("origin", "https://github.com/gitextensions/gitextensions.git", "https://github.com/gitextensions/gitextensions.git")] };
        RevisionLinksSettingsPageViewModel page = new(new RevisionLinksSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = CreateDialog(page, files.Levels);
        dialog.Open(page.PageName);
        page.Level = SettingsLevel.Local;

        page.Categories.Should().BeEmpty();
        page.IsCategorySelected.Should().BeFalse();
        page.Templates.Select(t => t.Text).Should().Equal("Add GitHub templates", "Add Azure DevOps templates");

        page.AddCommand.Execute(null);
        page.SelectedCategory!.Name.Should().Be("<new>");
        page.UseRemotes.Should().Be("upstream|origin");
        page.SearchInMessage.Should().BeTrue();
        page.LinkFormats.Should().ContainSingle().Which.IsNewRow.Should().BeTrue();

        page.Name = "Issues";
        page.Categories.Single().Name.Should().Be("Issues", "the list shows the name edited");
        page.SearchPattern = @" #(\d+) ";
        page.SearchInLocalBranch = true;
        page.LinkFormats[0].Caption = "Issue {0}";
        page.LinkFormats.Should().HaveCount(2, "a new row follows the link added");
        page.LinkFormats[0].Uri = "https://example.com/{0}";

        page.Templates[0].Command.Execute(null);
        page.Categories.Select(c => c.Name).Should().Equal("Issues", "GitHub - Code", "GitHub - Issues", "GitHub - Pull Requests");
        page.SelectedCategory.Name.Should().Be("GitHub - Code");
        page.LinkFormats[0].Uri.Should().Be("https://github.com/gitextensions/gitextensions/commit/%COMMIT_HASH%", "the templates use the remote");

        page.RemoveCommand.Execute(null);
        page.SelectedCategory.Name.Should().Be("GitHub - Issues", "the next category is selected");

        page.Level = SettingsLevel.Global;
        // As ExternalLinksManager.Add: the new categories are added to the lowest level.
        IReadOnlyList<ExternalLinkDefinition> saved = [.. new ExternalLinksStorage().Load(files.Global)!];
        saved.Select(d => d.Name).Should().BeEquivalentTo("Issues", "GitHub - Issues", "GitHub - Pull Requests");
        ExternalLinkDefinition issues = saved.Single(d => d.Name == "Issues");
        issues.SearchPattern.Should().Be(@"#(\d+)");
        issues.SearchInParts.Should().Contain(ExternalLinkDefinition.RevisionPart.LocalBranches);
        issues.LinkFormats.Should().ContainSingle().Which.Format.Should().Be("https://example.com/{0}");
    }

    [Test]
    public void Saving_unchanged_shell_settings_does_not_write_the_registry() => WithAppSettings(() =>
    {
        FakePagesHost host = new();
        ShellExtensionSettingsPageViewModel page = new(new ShellExtensionSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = CreateDialog(page, new FakeSources().GlobalOnly);
        dialog.Open(page.PageName);

        dialog.SaveAll();

        host.ShellMenuWrites.Should().Be(0);
        host.AlwaysShowWrites.Should().Be(0);
    });

    [Test]
    public void The_shell_extension_page_cycles_the_menu_items_and_previews_the_menu() => WithAppSettings(() =>
    {
        FakePagesHost host = new();
        ShellExtensionSettingsPageViewModel page = new(new ShellExtensionSettingsPageStrings(), host);
        SettingsDialogViewModel dialog = CreateDialog(page, new FakeSources().GlobalOnly);
        host.CascadeShellMenuItems = "01" + new string('2', 16);
        dialog.Open(page.PageName);

        page.MenuEntries.Should().HaveCount(18);
        page.MenuEntries[0].State.Should().BeTrue();
        page.MenuEntries[1].State.Should().BeNull();
        page.MenuEntries[2].State.Should().BeFalse();
        page.Preview.Should().Be("GitExt Add files...\nGitanas > \n       Apply patch...\n");
        page.IsExplorerIntegrationEnabled.Should().BeTrue();
        page.CanRegister.Should().BeTrue();

        page.MenuEntries[0].State = false;
        page.MenuEntries[1].State = false;
        page.Preview.Should().Be("(no items)");
        page.MenuEntries[17].State = true;
        dialog.SaveAll();
        host.CascadeShellMenuItems.Should().Be(new string('2', 17) + "0");

        host.Registered = true;
        page.RegisterCommand.Execute(null);
        page.CanRegister.Should().BeFalse();
        page.OpenHelpCommand.Execute(null);
        host.OpenedUrls.Should().Equal("manual");
    });

    private static SettingsDialogViewModel CreateDialog(SettingsPageViewModel page, IReadOnlyDictionary<SettingsLevel, SettingsSource> sources)
    {
        (SettingsDialogViewModel dialog, _, _) = SettingsDialogViewModelTests.Create();
        dialog.AddPage(page, "GitSettingsGroup", null, sources);
        return dialog;
    }

    /// <summary>
    ///  Runs <paramref name="test"/> with the application settings in a temporary file. The pages write the settings stored in
    ///  the registry (shared by all the processes) through the fake host; they are checked unchanged, and restored if not.
    /// </summary>
    private static void WithAppSettings(Action test)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".settings");
        string gitCommand = AppSettings.GitCommandValue;
        string sshPath = AppSettings.SshPath;
        string cascadeShellMenuItems = AppSettings.CascadeShellMenuItems;
        bool alwaysShowAllCommands = AppSettings.AlwaysShowAllCommands;
        try
        {
            using GitExtSettingsCache cache = new(path, autoSave: false);
            AppSettings.UsingContainer(new DistributedSettings(lowerPriority: null, cache, SettingLevel.Global), test);
        }
        finally
        {
            File.Delete(path);
        }

        bool unchanged = AppSettings.GitCommandValue == gitCommand
            && AppSettings.SshPath == sshPath
            && AppSettings.CascadeShellMenuItems == cascadeShellMenuItems
            && AppSettings.AlwaysShowAllCommands == alwaysShowAllCommands;
        if (!unchanged)
        {
            AppSettings.GitCommandValue = gitCommand;
            AppSettings.SshPath = sshPath;
            AppSettings.CascadeShellMenuItems = cascadeShellMenuItems;
            AppSettings.AlwaysShowAllCommands = alwaysShowAllCommands;
        }

        unchanged.Should().BeTrue("the settings stored in the registry must not be changed by the tests");
    }

    /// <summary>In-memory git config settings of each level.</summary>
    internal sealed class GitConfigSources
    {
        public MemorySettings Effective { get; } = new(SettingLevel.Effective);

        public MemorySettings Local { get; } = new(SettingLevel.Local);

        public MemorySettings Global { get; } = new(SettingLevel.Global);

        public MemorySettings System { get; } = new(SettingLevel.SystemWide);

        public Dictionary<SettingsLevel, SettingsSource> Levels => new()
        {
            [SettingsLevel.Effective] = Effective,
            [SettingsLevel.Local] = Local,
            [SettingsLevel.Global] = Global,
            [SettingsLevel.System] = System,
        };
    }

    /// <summary>Distributed settings of each level in temporary files.</summary>
    internal sealed class DistributedSettingsFiles : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

        public DistributedSettingsFiles()
        {
            Directory.CreateDirectory(_directory);
            Global = new DistributedSettings(lowerPriority: null, new GitExtSettingsCache(Path.Combine(_directory, "global.settings"), autoSave: false), SettingLevel.Global);
            Distributed = new DistributedSettings(Global, new GitExtSettingsCache(Path.Combine(_directory, "distributed.settings"), autoSave: false), SettingLevel.Distributed);
            Local = new DistributedSettings(Distributed, new GitExtSettingsCache(Path.Combine(_directory, "local.settings"), autoSave: false), SettingLevel.Local);
        }

        public DistributedSettings Global { get; }

        public DistributedSettings Distributed { get; }

        public DistributedSettings Local { get; }

        public Dictionary<SettingsLevel, SettingsSource> Levels => new()
        {
            [SettingsLevel.Local] = Local,
            [SettingsLevel.Distributed] = Distributed,
            [SettingsLevel.Global] = Global,
        };

        public void Dispose()
        {
            Local.SettingsCache.Dispose();
            Distributed.SettingsCache.Dispose();
            Global.SettingsCache.Dispose();
            Directory.Delete(_directory, recursive: true);
        }
    }

    internal sealed class FakeBuildServerSettingsControl(string buildServerType) : IBuildServerSettingsControl
    {
        public List<string> Loaded { get; } = [];

        public List<string> Saved { get; } = [];

        public bool IsDisposed { get; private set; }

        public double PreferredHeight => 100;

        public nint Attach(nint parentWindow) => 0;

        public void Detach()
        {
        }

        public void LoadSettings(SettingsSource buildServerConfig) => Loaded.Add(buildServerType);

        public void SaveSettings(SettingsSource buildServerConfig) => Saved.Add(buildServerType);

        public void Dispose() => IsDisposed = true;
    }

    /// <summary>The application side of the pages, recording what they ask.</summary>
    internal sealed class FakePagesHost
        : IGitSettingsPageHost,
        IGitConfigSettingsPageHost,
        ISshSettingsPageHost,
        IBuildServerIntegrationSettingsPageHost,
        IRevisionLinksSettingsPageHost,
        IShellExtensionSettingsPageHost
    {
        // The settings stored in the registry, shared by all the processes, are faked.
        public string GitCommandValue { get; set; } = "git";

        public string LinuxToolsDir { get; set; } = "";

        public string SshPath { get; set; } = "";

        private string _cascadeShellMenuItems = "110111000111111111";
        private bool _alwaysShowAllCommands;

        public string CascadeShellMenuItems
        {
            get => _cascadeShellMenuItems;
            set
            {
                _cascadeShellMenuItems = value;
                ShellMenuWrites++;
            }
        }

        public bool AlwaysShowAllCommands
        {
            get => _alwaysShowAllCommands;
            set
            {
                _alwaysShowAllCommands = value;
                AlwaysShowWrites++;
            }
        }

        public int ShellMenuWrites { get; private set; }

        public int AlwaysShowWrites { get; private set; }

        public (string? GitConfigGlobal, string HomeDir) GitEnvironment { get; set; } = (null, "C:\\Users\\user");

        public List<string?> SolvedGitCommands { get; } = [];

        public int FixHomeShown { get; private set; }

        public List<string> OpenedUrls { get; } = [];

        public int GitConfigSaved { get; private set; }

        public bool CanFindGit { get; set; } = true;

        public IReadOnlyList<string> PuttyLocations { get; init; } = [];

        public string? GitSsh { get; private set; }

        public TaskCompletionSource<IReadOnlyList<string>> BuildServerTypes { get; } = new();

        public List<FakeBuildServerSettingsControl> Controls { get; } = [];

        public IReadOnlyList<Remote> Remotes { get; init; } = [];

        public bool Registered { get; set; }

        public bool SolveGitCommand(string? possibleNewPath)
        {
            SolvedGitCommands.Add(possibleNewPath);
            return true;
        }

        public bool SolveLinuxToolsDir(string? possibleNewPath) => true;

        public (string? GitConfigGlobal, string HomeDir) GetGitEnvironment() => GitEnvironment;

        public void ShowFixHome() => FixHomeShown++;

        public void OpenUrl(string url) => OpenedUrls.Add(url);

        public void SaveGitConfigSettings() => GitConfigSaved++;

        public bool CanFindGitCmd() => CanFindGit;

        public IReadOnlyList<string> GetEditors() => ["notepad"];

        public bool ShowAvailableEncodings() => false;

        public IEnumerable<string> GetPuttyLocations() => PuttyLocations;

        public void SetGitSshEnvironmentVariable(string path) => GitSsh = path;

        // The test completes the task, as the host finds the plugins in the background.
#pragma warning disable VSTHRD003 // Avoid awaiting foreign Tasks
        public Task<IReadOnlyList<string>> GetBuildServerTypesAsync() => BuildServerTypes.Task;
#pragma warning restore VSTHRD003

        public IBuildServerSettingsControl? CreateSettingsControl(string buildServerType)
        {
            FakeBuildServerSettingsControl control = new(buildServerType);
            Controls.Add(control);
            return control;
        }

        /// <summary>Creates the settings of a build server plugin of API v2; none by default (then its control of API v1).</summary>
        public Func<string, BuildServerPluginSettings?>? PluginSettingsFactory { get; set; }

        public BuildServerPluginSettings? CreatePluginSettings(string buildServerType) => PluginSettingsFactory?.Invoke(buildServerType);

        public IReadOnlyList<Remote> GetRemotes() => Remotes;

        public bool FilesExist() => true;

        public bool IsRegistered() => Registered;

        public void Register()
        {
        }

        public void Unregister()
        {
        }

        public string GetManualUrl() => "manual";
    }
}

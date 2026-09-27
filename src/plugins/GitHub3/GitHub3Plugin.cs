using System.ComponentModel.Composition;
using Git.hub;
using GitCommands;
using GitCommands.Config;
using GitCommands.Remotes;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Plugins.GitHub3.Properties;
using GitUI;
using GitUIPluginInterfaces;
using GitUIPluginInterfaces.RepositoryHosts;
using Microsoft;
using ResourceManager;

namespace GitExtensions.Plugins.GitHub3;

internal static class GitHubLoginInfo
{
    private static string? _username;

    public static string? Username
    {
        get
        {
            if (_username == "")
            {
                return null;
            }

            if (_username is not null)
            {
                return _username;
            }

            try
            {
                User user = GitHub3Plugin.GitHub.getCurrentUser();
                if (user is not null)
                {
                    _username = user.Login;
                    return _username;
                }
                else
                {
                    _username = "";
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public static string OAuthToken
    {
        get => GitHub3Plugin.Instance.PersonalAccessToken.ValueOrDefault(GitHub3Plugin.Instance.Settings);
        set
        {
            _username = null;
            GitHub3Plugin.Instance.PersonalAccessToken[GitHub3Plugin.Instance.Settings] = value;
            GitHub3Plugin.GitHub.setOAuth2Token(value);
        }
    }
}

[Export(typeof(IGitPlugin))]
[Export(typeof(IRepositoryHostPlugin))]
[Export(typeof(IGitPluginForCommit))]
public class GitHub3Plugin : GitPluginBase, IRepositoryHostPlugin, IGitPluginForCommit, IBlameContextMenuProvider
{
    private readonly TranslationString _viewInWebSite = new("View in {0}");
    private readonly TranslationString _tokenAlreadyExist = new("You already have an personal access token. To get a new one, delete your old one in Plugins > Plugin Settings first.");
    private readonly TranslationString _generateToken = new("Generate a GitHub personal access token");
    private readonly TranslationString _manageToken = new("Manage GitHub personal access token");
    private readonly TranslationString _openLinkFailed = new("Fail to open the link. Reason: ");
    private readonly TranslationString _noteRestartNeeded = new("Note: Gitanas need to be restarted so that the token is taken into account.");
    private readonly TranslationString _noTokenError = new("No GitHub personal access token (PAT) defined");
    private readonly TranslationString _noAssignedIssues = new("No assigned GitHub issues found");
    private readonly TranslationString _error = new("Error");

    public static string GitHubAuthorizationRelativeUrl = "authorizations";
    public static string UpstreamConventionName = "upstream";
    public readonly StringSetting GitHubHost = new("GitHub (Enterprise) hostname", "github.com");
    public readonly StringSetting PersonalAccessToken = new("OAuth Token", "Personal Access Token", "");
    private readonly BoolSetting _issueCommitMessageHelperEnabled = new("IssueCommitMessageHelperEnabled", "Enable commit message issue helper", true);
    private readonly NumberSetting<int> _issueCommitMessageHelperMaxCount = new("IssueCommitMessageHelperMaxCount", "Maximum number of issues retrieved", 10);
    public string GitHubApiEndpoint => $"https://api.{GitHubHost.ValueOrDefault(Settings)}";
    public string GitHubEndpoint => $"https://{GitHubHost.ValueOrDefault(Settings)}";

    internal static GitHub3Plugin Instance = null!;
    internal static Client? _gitHub;
    internal static Client GitHub => _gitHub ??= new(Instance.GitHubApiEndpoint);
    internal static GitHubRemoteParser _gitHubRemoteParser = new();

    private IGitUICommands? _currentGitUiCommands;

    // The hosted remotes of a repository, for the blame context menu (read once per repository, as ConfigureContextMenu did
    // when the blame was configured).
    private (IGitModule Module, IReadOnlyList<IHostedRemote> Remotes)? _hostedRemotesForModule;
    private List<string> _currentMessages = [];

    public GitHub3Plugin() : base(true)
    {
        Id = new Guid("2EC3E1F0-EF37-413F-BEA5-B8FE1F9C505C");
        Name = "GitHub";
        Translate(AppSettings.CurrentTranslation);

        Instance ??= this;

        IconImage = PluginImage.FromResource(GetType().Assembly, "PluginIcon.png");
    }

    public override IEnumerable<ISetting> GetSettings()
    {
        yield return PersonalAccessToken;

        yield return new ActionSetting(_generateToken.Text, context => OpenLink(context.Owner, $"https://{GitHubHost.ValueOrDefault(Instance.Settings)}/settings/tokens/new?description=Token%20for%20Gitanas&scopes=repo,public_repo"));

        yield return new ActionSetting(_manageToken.Text, context => OpenLink(context.Owner, $"https://{GitHubHost.ValueOrDefault(Instance.Settings)}/settings/tokens"));

        yield return new PseudoSetting(_noteRestartNeeded.Text);

        yield return _issueCommitMessageHelperEnabled;

        yield return _issueCommitMessageHelperMaxCount;
    }

    private void OpenLink(WindowOwner owner, string url)
    {
        try
        {
            OsShellUtil.OpenUrlInDefaultBrowser(url);
        }
        catch (Exception ex)
        {
            PluginMessageBoxes.Show(owner, _openLinkFailed.Text + ex.Message, caption: string.Empty);
        }
    }

    public override void Register(IGitUICommands gitUiCommands)
    {
        _currentGitUiCommands = gitUiCommands;
        _hostedRemotesForModule = null;
        if (!string.IsNullOrEmpty(GitHubLoginInfo.OAuthToken))
        {
            GitHub.setOAuth2Token(GitHubLoginInfo.OAuthToken);
        }

        gitUiCommands.PreCommit += GitUiCommands_PreCommit;
        gitUiCommands.PostCommit += GitUiCommands_PostCommit;
    }

    private void GitUiCommands_PreCommit(object? sender, GitUIEventArgs e)
    {
        if (!_issueCommitMessageHelperEnabled.ValueOrDefault(Settings))
        {
            return;
        }

        if (string.IsNullOrEmpty(GitHubLoginInfo.OAuthToken))
        {
            e.GitUICommands.RegisterCommitTemplate(_noTokenError.Text, () => string.Empty, IconImage);
            return;
        }

        ThreadHelper.FileAndForget(async () =>
        {
            IHostedRemote[] hostedRemotes = [.. GetHostedRemotes()];
            if (hostedRemotes.Length == 0)
            {
                return;
            }

            IReadOnlyList<Issue> issues = GitHub.GetAssignedIssues();

            if (issues?.All(i => i.Number == 0) ?? true)
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                e.GitUICommands.RegisterCommitTemplate(_noAssignedIssues.Text, () => string.Empty, IconImage);
                return;
            }

            Issue[] recentUserIssues = [.. issues.Where(i => i.Number != 0 && hostedRemotes.Any(r => r.Owner == i.Repository.Owner.Login && r.RemoteRepositoryName == i.Repository.Name))
                                                        .OrderByDescending(i => i.UpdatedAt)
                                                        .Take(_issueCommitMessageHelperMaxCount.ValueOrDefault(Settings))];

            bool multipleRemotes = hostedRemotes.Length > 1;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            foreach (Issue issue in recentUserIssues)
            {
                string remoteData = multipleRemotes ? $" ({issue.Repository.Owner.Login}/{issue.Repository.Name})" : string.Empty;
                string key = $"{issue.Number}: {issue.Title}{remoteData}";
                _currentMessages.Add(key);
                e.GitUICommands.RegisterCommitTemplate(key, () => GetIssueDescription(issue), IconImage);
            }

            static string GetIssueDescription(Issue issue)
                => $"""

                    Fixes #{issue.Number} : {issue.Title}

                    {issue.Body}

                    """;
        });
    }

    private void GitUiCommands_PostCommit(object? sender, GitUIEventArgs e)
    {
        if (_currentMessages.Count == 0)
        {
            return;
        }

        foreach (string key in _currentMessages)
        {
            e.GitUICommands.RemoveCommitTemplate(key);
        }

        _currentMessages.Clear();
    }

    public override bool Execute(GitUIEventArgs args)
    {
        if (string.IsNullOrEmpty(GitHubLoginInfo.OAuthToken))
        {
            args.GitUICommands.StartSettingsDialog(this);
        }
        else
        {
            PluginMessageBoxes.ShowError(args.Owner, _tokenAlreadyExist.Text, _error.Text);
        }

        return false;
    }

    // --

    public IReadOnlyList<IHostedRepository> SearchForRepository(string search)
    {
        return GitHub.searchRepositories(search).Select(repo => (IHostedRepository)new GitHubRepo(repo)).ToList();
    }

    public IReadOnlyList<IHostedRepository> GetRepositoriesOfUser(string user)
    {
        return GitHub.getRepositories(user).Select(repo => (IHostedRepository)new GitHubRepo(repo)).ToList();
    }

    public IHostedRepository GetRepository(string user, string repositoryName)
    {
        return new GitHubRepo(GitHub.getRepository(user, repositoryName));
    }

    public IReadOnlyList<IHostedRepository> GetMyRepos()
    {
        return GitHub.getRepositories().Select(repo => (IHostedRepository)new GitHubRepo(repo)).ToList();
    }

    public bool ConfigurationOk => !string.IsNullOrEmpty(GitHubLoginInfo.OAuthToken);

    public string? OwnerLogin => GitHub.getCurrentUser()?.Login;

    public async Task<string?> AddUpstreamRemoteAsync()
    {
        Validates.NotNull(_currentGitUiCommands);

        IGitModule gitModule = _currentGitUiCommands.Module;
        IHostedRemote? hostedRemote = GetHostedRemotesForModule().FirstOrDefault(r => r.IsOwnedByMe);
        if (hostedRemote is null)
        {
            return null;
        }

        IHostedRepository hostedRepository = hostedRemote.GetHostedRepository();
        if (!hostedRepository.IsAFork)
        {
            return null;
        }

        if ((await gitModule.GetRemotesAsync()).Any(r => r.Name == UpstreamConventionName || r.FetchUrl == hostedRepository.ParentUrl))
        {
            return null;
        }

        gitModule.AddRemote(UpstreamConventionName, hostedRepository.ParentUrl);
        return UpstreamConventionName;
    }

    public bool GitModuleIsRelevantToMe()
        => _currentGitUiCommands?.Module is not null && GetHostedRemotes().Any();

    /// <summary>
    /// Returns all relevant github-remotes for the current working directory
    /// </summary>
    public IReadOnlyList<IHostedRemote> GetHostedRemotesForModule()
        => _currentGitUiCommands?.Module is null ? [] : GetHostedRemotes().ToArray();

    private IEnumerable<IHostedRemote> GetHostedRemotes()
    {
        HashSet<IHostedRemote> set = [];

        IGitModule? gitModule = _currentGitUiCommands?.Module;
        if (gitModule is null)
        {
            yield break;
        }

        foreach (string remote in gitModule.GetRemoteNames())
        {
            string url = gitModule.GetSetting(string.Format(SettingKeyString.RemoteUrl, remote));

            if (string.IsNullOrEmpty(url))
            {
                continue;
            }

            if (_gitHubRemoteParser.TryExtractGitHubDataFromRemoteUrl(url, out string? owner, out string? repository))
            {
                GitHubHostedRemote hostedRemote = new(remote, owner, repository, url);

                if (set.Add(hostedRemote))
                {
                    yield return hostedRemote;
                }
            }
        }
    }

    /// <summary>"View in GitHub", with the blame of the line on each GitHub remote of the repository.</summary>
    public IReadOnlyList<PluginMenuItem> GetBlameContextMenuItems(GitBlameContext context)
    {
        IReadOnlyList<IHostedRemote> hostedRemotes = GetHostedRemotesForBlame();
        if (hostedRemotes.Count == 0)
        {
            return [];
        }

        return
        [
            new PluginMenuItem(
                string.Format(_viewInWebSite.Text, Name),
                children: [.. hostedRemotes
                    .OrderBy(r => r.Data)
                    .Select(hostedRemote => new PluginMenuItem(
                        hostedRemote.DisplayData,
                        () => OsShellUtil.OpenUrlInDefaultBrowser(hostedRemote.GetBlameUrl(context.BlameId.ToString(), context.FileName, context.LineIndex + 1))))])
            {
                IconImage = IconImage,
            },
        ];
    }

    private IReadOnlyList<IHostedRemote> GetHostedRemotesForBlame()
    {
        IGitModule? module = _currentGitUiCommands?.Module;
        if (module is null)
        {
            return [];
        }

        if (_hostedRemotesForModule is not { } cached || !ReferenceEquals(cached.Module, module))
        {
            cached = (module, GetHostedRemotesForModule());
            _hostedRemotesForModule = cached;
        }

        return cached.Remotes;
    }
}

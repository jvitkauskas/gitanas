using GitUI.CommandsDialogs.BrowseDialog;

namespace GitUITests.CommandsDialogs.BrowseDialog;

[TestFixture]
public sealed class GitanasReleaseFeedTests
{
    [TestCase(false, "v1.2.0")]
    [TestCase(true, "v1.3.0-rc1")]
    public void Selects_only_newer_published_releases_and_links_to_the_fork(bool includePrereleases, string expected)
    {
        const string json = """
            [
              {"tag_name":"v2.0.0","draft":true,"prerelease":false},
              {"tag_name":"v1.3.0-rc1","draft":false,"prerelease":true},
              {"tag_name":"v1.2.0","draft":false,"prerelease":false},
              {"tag_name":"nightly","draft":false,"prerelease":false},
              {"tag_name":"v1.0.0","draft":false,"prerelease":false}
            ]
            """;
        var update = GitanasReleaseFeed.FindUpdate(json, new Version(1, 1, 0), includePrereleases);
        update!.NewVersion.Should().Be(expected);
        update.UpdateUrl.Should().Be("https://github.com/jvitkauskas/gitanas/releases");
    }

    [TestCase("[]")]
    [TestCase("[{\"tag_name\":\"v1.0.0\",\"draft\":false,\"prerelease\":false}]")]
    public void Empty_or_older_release_lists_have_no_update(string json)
        => GitanasReleaseFeed.FindUpdate(json, new Version(1, 1, 0), false).Should().BeNull();
}

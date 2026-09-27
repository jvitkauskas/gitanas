using System.Text.Json;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;

namespace GitUI.CommandsDialogs.BrowseDialog;

internal static class GitanasReleaseFeed
{
    internal const string ApiUrl = "https://api.github.com/repos/jvitkauskas/gitanas/releases?per_page=100";

    internal static AvailableUpdate? FindUpdate(string json, Version currentVersion, bool includePrereleases)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        Version newest = currentVersion;
        string? newestTag = null;
        foreach (JsonElement release in document.RootElement.EnumerateArray())
        {
            if (release.GetProperty("draft").GetBoolean()
                || (release.GetProperty("prerelease").GetBoolean() && !includePrereleases))
            {
                continue;
            }

            string? tag = release.GetProperty("tag_name").GetString();
            string? versionText = tag?.TrimStart('v', 'V').Split('-', '+')[0];
            if (Version.TryParse(versionText, out Version? version) && version > newest)
            {
                newest = version;
                newestTag = tag;
            }
        }

        return newestTag is null ? null : new AvailableUpdate(newestTag, UpdatesViewModel.ReleasesUrl, null);
    }
}

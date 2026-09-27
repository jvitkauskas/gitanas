using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using GitCommands;
using GitExtensions.Extensibility;
using GitUI.Avalonia.CommandsDialogs.BrowseDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.CommandsDialogs.BrowseDialog;
using GitUI.Presentation.Translations;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of the check for updates dialog (docs/avalonia-port/PLAN.md, phase 2, batch 8).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>
    ///  As <c>FormUpdates.SearchForUpdatesAndShow</c>: searches for updates in the background and shows the dialog
    ///  right away if <paramref name="alwaysShow"/>, otherwise only once an update is found.
    /// </summary>
    public static bool TrySearchForUpdatesAndShow(IWin32Window? owner, bool alwaysShow)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        UpdatesWindow window = new();
        UpdatesViewModel viewModel = new(
            ViewStrings.Load<UpdatesStrings>(),
            isPortable: true,
            RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant(),
            [.. UserEnvironmentInformation.GetDotnetDesktopRuntimeVersions()],
            new UpdatesHost(),
            new MessageBoxService(window));
        window.DataContext = viewModel;

        Version currentVersion = AppSettings.AppVersion;
        ThreadHelper.FileAndForget(async () =>
        {
            await TaskScheduler.Default;

            AvailableUpdate? update = null;
            Exception? failure = null;
            try
            {
                update = await SearchForUpdateAsync(currentVersion);
            }
            catch (Exception ex) when (ex.Message.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            {
                // GitHub API rate limiting: nothing to tell the user.
            }
            catch (InvalidAsynchronousStateException)
            {
                // The application is closing.
                return;
            }
            catch (Exception ex)
            {
                failure = ex;
            }

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            if (failure is not null && window.IsVisible)
            {
                ExceptionUtils.ShowException(new NativeWindowOwner(window), failure, string.Empty, true);
            }

            viewModel.ReportSearchResult(update);
            if (update is not null && !alwaysShow)
            {
                ShowDialog(() => window, owner);
            }
        });

        if (alwaysShow)
        {
            ShowDialog(() => window, owner);
        }

        return true;
    }

    // Fork releases are selected on GitHub; never offer or install upstream WinForms binaries.
    private static async Task<AvailableUpdate?> SearchForUpdateAsync(Version currentVersion)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(20) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Gitanas");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using HttpResponseMessage response = await client.GetAsync(GitanasReleaseFeed.ApiUrl).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null; // A newly created repository may not have published releases yet.
        }

        response.EnsureSuccessStatusCode();
        string json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        return GitanasReleaseFeed.FindUpdate(json, currentVersion, AppSettings.CheckForReleaseCandidates);
    }

    private sealed class UpdatesHost : IUpdatesHost
    {
        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));

        public void DownloadAndInstall(string updateUrl, Action<string> reportDownloadFailure)
            => OpenUrl(UpdatesViewModel.ReleasesUrl);
    }
}

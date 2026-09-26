using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.Views;

/// <summary>Headless view tests of the Avalonia revision grid (phase 4).</summary>
[TestFixture]
public sealed class RevisionGridViewTests : HeadlessTest
{
    [Test]
    public Task Render_screenshots([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        (Window window, RevisionGridViewModel _) = Show(CreateHistory());

        SaveScreenshot(window.CaptureRenderedFrame(), $"revision-grid-{theme}");
        window.Close();
    });

    [Test]
    public Task Lists_the_revisions_in_graph_order_with_their_references_and_lays_out_the_graph() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = CreateHistory();
        (Window window, RevisionGridViewModel viewModel) = Show(history);

        viewModel.IsLoading.Should().BeFalse();
        viewModel.Rows.Select(r => r.Subject).Should().Equal(history.Select(r => r.Subject));
        viewModel.CachedGraphRowCount.Should().Be(history.Count);
        viewModel.Rows[0].Refs.Should().Equal(
            new RevisionRefItem("main", RevisionRefKind.Branch, IsCurrentBranch: true),
            new RevisionRefItem("origin/main", RevisionRefKind.RemoteBranch, IsCurrentBranch: false));
        viewModel.Rows.Single(r => r.Subject == "Release").Refs.Should().Equal(new RevisionRefItem("v1.0", RevisionRefKind.Tag, false));

        DataGrid grid = window.GetVisualDescendants().OfType<DataGrid>().Single();
        grid.Columns[0].ActualWidth.Should().BeGreaterThan(2 * RevisionGraphRenderer.LaneWidth, "the merged branch needs a second lane");
        window.GetVisualDescendants().OfType<RevisionGraphCell>().Should().NotBeEmpty();
        window.Close();
    });

    [Test]
    public Task Selects_a_revision_and_reports_its_activation() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = CreateHistory();
        (Window window, RevisionGridViewModel viewModel) = Show(history, toBeSelected: history[3].ObjectId);
        RevisionGridView view = window.GetVisualDescendants().OfType<RevisionGridView>().Single();
        RevisionGridRow? activated = null;
        view.RevisionActivated += (_, row) => activated = row;

        viewModel.SelectedRow!.ObjectId.Should().Be(history[3].ObjectId);
        viewModel.SelectRevision(ObjectId.Random()).Should().BeFalse();
        viewModel.SelectRevision(history[5].ObjectId).Should().BeTrue();

        window.GetVisualDescendants().OfType<DataGrid>().Single().Focus();
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Dispatcher.UIThread.RunJobs();

        activated.Should().NotBeNull();
        activated!.ObjectId.Should().Be(history[5].ObjectId);
        window.Close();
    });

    [Test]
    public Task Graph_width_stops_at_the_rendering_limit_and_resets_when_history_is_reloaded() => OnUiThreadAsync(() =>
    {
        GitRevision root = Revision("Root", "Alice", daysAgo: 3);
        GitRevision[] branches = [.. Enumerable.Range(0, 60).Select(i => Revision($"Branch {i}", "Alice", daysAgo: 2, root))];
        GitRevision merge = Revision("Merge", "Alice", daysAgo: 1, branches);
        List<GitRevision> history = [merge, .. branches, root];
        (Window window, RevisionGridViewModel viewModel) = Show(history);
        DataGrid grid = window.GetVisualDescendants().OfType<DataGrid>().Single();

        grid.Columns[0].Width.Value.Should().Be(RevisionGraphRenderer.GetWidth(branches.Length) + 4);

        history.Clear();
        history.Add(root);
        viewModel.Load();
        Dispatcher.UIThread.RunJobs();

        grid.Columns[0].Width.Value.Should().Be(RevisionGraphRenderer.GetWidth(1) + 4);
        window.Close();
    });

    [Test]
    public Task Copy_puts_the_hash_of_the_selected_revision_on_the_clipboard() => OnUiThreadAsync(() =>
    {
        List<GitRevision> history = CreateHistory();
        (Window window, RevisionGridViewModel viewModel) = Show(history, toBeSelected: history[1].ObjectId);
        viewModel.SelectedRow!.ObjectId.Should().Be(history[1].ObjectId);
        window.GetVisualDescendants().OfType<DataGrid>().Single().Focus();

        // As RevisionDataGridView: Ctrl+C (Cmd+C on macOS) copies the hashes of the selection, one per line.
        window.KeyPressQwerty(PhysicalKey.C, TestKeys.Command);
        Dispatcher.UIThread.RunJobs();

        string? text = window.Clipboard!.TryGetTextAsync().GetAwaiter().GetResult();
        text.Should().Be(history[1].ObjectId.ToString());
        window.Close();
    });

    /// <summary>main: a merge of a feature branch, a tag, and a side branch; newest first, as git log lists them.</summary>
    internal static List<GitRevision> CreateHistory()
    {
        GitRevision root = Revision("Initial commit", "Alice", daysAgo: 30);
        GitRevision release = Revision("Release", "Alice", daysAgo: 20, root);
        GitRevision feature1 = Revision("Start feature", "Bob", daysAgo: 15, release);
        GitRevision fix = Revision("Fix a bug", "Alice", daysAgo: 14, release);
        GitRevision feature2 = Revision("Finish feature", "Bob", daysAgo: 12, feature1);
        GitRevision merge = Revision("Merge branch 'feature'", "Alice", daysAgo: 10, fix, feature2);
        GitRevision side = Revision("Experiment", "Carol", daysAgo: 8, merge);
        GitRevision head = Revision("Update the documentation", "Alice", daysAgo: 1, merge);

        head.Refs = [Ref(head, "refs/heads/main"), Ref(head, "refs/remotes/origin/main", "origin")];
        side.Refs = [Ref(side, "refs/heads/experiment")];
        release.Refs = [Ref(release, "refs/tags/v1.0")];
        return [head, side, merge, feature2, fix, feature1, release, root];
    }

    private static GitRevision Revision(string subject, string author, int daysAgo, params GitRevision[] parents)
    {
        long time = DateTimeOffset.Now.AddDays(-daysAgo).ToUnixTimeSeconds();
        return new GitRevision(ObjectId.Random())
        {
            Subject = subject,
            Author = author,
            AuthorUnixTime = time,
            CommitUnixTime = time,
            ParentIds = [.. parents.Select(p => p.ObjectId)],
        };
    }

    private static IGitRef Ref(GitRevision revision, string completeName, string remote = "")
        => new GitRef(TestGitModule.Instance, revision.ObjectId, completeName, remote);

    private static (Window Window, RevisionGridViewModel ViewModel) Show(List<GitRevision> history, ObjectId? toBeSelected = null)
    {
        RevisionGridViewModel viewModel = new(new FakeRevisionGridHost(history), new RevisionGridDisplayOptions(RelativeDate: true, ShowAuthorDate: false));
        Window window = new() { Width = 760, Height = 260, Content = new RevisionGridView { DataContext = viewModel } };
        window.Show();
        viewModel.Load(toBeSelected);
        Dispatcher.UIThread.RunJobs();
        return (window, viewModel);
    }

    /// <summary>Adds the revisions in one batch and caches in place, synchronously.</summary>
    internal sealed class FakeRevisionGridHost(IReadOnlyList<GitRevision> revisions) : IRevisionGridHost
    {
        public string CurrentBranch => "main";

        public bool MatchesQuickSearch(GitRevision revision, string criteria)
            => revision.Subject.Contains(criteria, StringComparison.OrdinalIgnoreCase) || (revision.Author?.Contains(criteria, StringComparison.OrdinalIgnoreCase) ?? false);

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            graph.HeadId = revisions[0].ObjectId;
            foreach (GitRevision revision in revisions)
            {
                graph.Add(revision);
            }

            reportBatch();
            completed(null);
        }

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }
    }
}

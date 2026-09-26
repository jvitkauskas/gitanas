using GitExtensions.Extensibility.Git;
using GitUI.AvaloniaTests.Views;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the Avalonia revision grid and the choose commit dialog (phase 4).</summary>
[TestFixture]
public sealed class RevisionGridViewModelTests
{
    private static readonly RevisionGridDisplayOptions _options = new(RelativeDate: false, ShowAuthorDate: false);

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Row_date_preserves_the_selected_timestamp_and_display_options(bool authorDate, bool relativeDate)
    {
        DateTime author = new(2020, 1, 2, 3, 4, 5);
        DateTime commit = new(2022, 6, 7, 8, 9, 10);
        GitRevision revision = new(ObjectId.Random()) { AuthorUnixTime = new DateTimeOffset(author).ToUnixTimeSeconds(), CommitUnixTime = new DateTimeOffset(commit).ToUnixTimeSeconds() };
        RevisionGridRow row = new(0, revision, new RevisionGridDisplayOptions(relativeDate, authorDate), currentBranch: null);

        // Formatting is deferred, but the timestamp remains the one supplied when the history row was created.
        revision.AuthorUnixTime = 0;
        revision.CommitUnixTime = 0;

        row.Date.Should().Be(RevisionGridRow.FormatDate(authorDate ? author : commit, relativeDate));
    }

    [Test]
    public void Adds_the_rows_of_each_batch_and_selects_the_preselected_revision_once_loaded()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        BatchedHost host = new(history);
        RevisionGridViewModel viewModel = new(host, _options);
        bool loaded = false;
        viewModel.Loaded += (_, _) => loaded = true;

        viewModel.Load(toBeSelected: history[6].ObjectId);
        viewModel.IsLoading.Should().BeTrue();

        host.AddBatch(3);
        viewModel.Rows.Select(r => r.Subject).Should().Equal(history.Take(3).Select(r => r.Subject));
        viewModel.SelectedRow.Should().BeNull("the preselected revision is not loaded yet");

        host.AddBatch(5);
        viewModel.Rows.Should().HaveCount(8);
        viewModel.SelectedRow!.ObjectId.Should().Be(history[6].ObjectId, "it is selected as soon as it is loaded");

        host.Complete();
        viewModel.IsLoading.Should().BeFalse();
        loaded.Should().BeTrue();
        viewModel.CachedGraphRowCount.Should().Be(8, "the graph is laid out for the rows shown");
        viewModel.Rows[0].Date.Should().Be(RevisionGridRow.FormatDate(history[0].CommitDate, relative: false));
    }

    [Test]
    public void Reloading_ignores_the_batches_of_the_previous_load()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        BatchedHost host = new(history);
        RevisionGridViewModel viewModel = new(host, _options);

        viewModel.Load();
        Action staleBatch = host.ReportBatch!;
        viewModel.Load();
        host.AddBatch(2);
        staleBatch();

        viewModel.Rows.Should().HaveCount(2);
    }

    [Test]
    public void Choose_commit_shows_the_parents_of_the_selected_commit_and_accepts_it()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(history), _options);
        FakeChooseCommitHost host = new();
        ChooseCommitViewModel viewModel = new(new ChooseCommitStrings(), grid, host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        grid.Load();

        viewModel.OkCommand.CanExecute(null).Should().BeFalse();
        GitRevision merge = history[2];
        grid.SelectRevision(merge.ObjectId);
        viewModel.IsCommitSelected.Should().BeTrue();
        viewModel.Parents.Select(p => p.ObjectId).Should().Equal(merge.ParentIds);

        viewModel.SelectParentCommand.Execute(viewModel.Parents[1]);
        grid.SelectedRow!.ObjectId.Should().Be(merge.ParentIds![1]);

        viewModel.OkCommand.Execute(null);
        viewModel.SelectedRevision.Should().Be(history[3]);
        closed.Should().BeTrue();
    }

    [Test]
    public void Choose_commit_goes_to_a_commit_or_reports_that_it_is_not_shown()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel grid = new(new RevisionGridViewTests.FakeRevisionGridHost(history), _options);
        FakeChooseCommitHost host = new();
        ChooseCommitViewModel viewModel = new(new ChooseCommitStrings(), grid, host);
        grid.Load();

        host.GoTo = history[4].ObjectId;
        viewModel.GoToCommitCommand.Execute(null);
        grid.SelectedRow!.ObjectId.Should().Be(history[4].ObjectId);

        ObjectId filtered = ObjectId.Random();
        host.GoTo = filtered;
        viewModel.GoToCommitCommand.Execute(null);
        host.Filtered.Should().Equal(filtered);

        host.GoTo = ObjectId.Parse("0000000000000000000000000000000000000000");
        viewModel.GoToCommitCommand.Execute(null);
        host.NotFound.Should().Be(1);

        host.GoTo = null;
        viewModel.GoToCommitCommand.Execute(null);
        host.NotFound.Should().Be(1, "a cancelled go to commit does nothing");
    }

    [Test]
    public void Quick_search_selects_the_next_match_and_wraps_around()
    {
        List<GitRevision> history = RevisionGridViewTests.CreateHistory();
        RevisionGridViewModel viewModel = new(new RevisionGridViewTests.FakeRevisionGridHost(history), _options with { QuickSearchLabel = "Search: " });
        int restarts = 0;
        viewModel.QuickSearchRestarted += (_, _) => restarts++;
        viewModel.Load();

        viewModel.QuickSearchType("B");
        viewModel.QuickSearchText.Should().Be("b", "typed characters are lowercased");
        viewModel.SelectedRow!.Subject.Should().Be("Merge branch 'feature'", "the first match from the top");
        viewModel.QuickSearchLabel.Should().Be("Search: b");
        viewModel.IsQuickSearchVisible.Should().BeTrue();

        viewModel.QuickSearchType("ob");
        viewModel.SelectedRow!.AuthorName.Should().Be("Bob", "the current row is searched first");

        viewModel.QuickSearchNext(down: true);
        viewModel.SelectedRow!.Subject.Should().Be("Start feature");
        viewModel.QuickSearchNext(down: true);
        viewModel.SelectedRow!.Subject.Should().Be("Finish feature", "the search wraps around");
        viewModel.QuickSearchNext(down: false);
        viewModel.SelectedRow!.Subject.Should().Be("Start feature");

        viewModel.QuickSearchPaste("XYZ");
        viewModel.IsQuickSearchMatched.Should().BeFalse();
        viewModel.SelectedRow!.Subject.Should().Be("Start feature", "the selection stays without a match");

        viewModel.QuickSearchBackspace().Should().BeTrue();
        viewModel.QuickSearchText.Should().Be("bobXY");
        restarts.Should().Be(7);

        viewModel.HideQuickSearch();
        viewModel.IsQuickSearchVisible.Should().BeFalse();
        viewModel.QuickSearchBackspace().Should().BeFalse("nothing is left to remove");
    }

    /// <summary>Adds the revisions in batches on request, as <c>RevisionReader</c> reports them.</summary>
    private sealed class BatchedHost(IReadOnlyList<GitRevision> revisions) : IRevisionGridHost
    {
        private RevisionGraph? _graph;
        private int _added;
        private Action<Exception?>? _completed;

        public Action? ReportBatch { get; private set; }

        public string CurrentBranch => "main";

        public bool MatchesQuickSearch(GitRevision revision, string criteria)
            => revision.Subject.Contains(criteria, StringComparison.OrdinalIgnoreCase) || (revision.Author?.Contains(criteria, StringComparison.OrdinalIgnoreCase) ?? false);

        public void LoadRevisions(RevisionGraph graph, Action reportBatch, Action<Exception?> completed, CancellationToken cancellationToken)
        {
            _graph = graph;
            _added = 0;
            ReportBatch = reportBatch;
            _completed = completed;
        }

        public void AddBatch(int count)
        {
            foreach (GitRevision revision in revisions.Skip(_added).Take(count))
            {
                _graph!.Add(revision);
            }

            _added += count;
            ReportBatch!();
        }

        public void Complete() => _completed!(null);

        public void RunInBackground(Action work, Action then)
        {
            work();
            then();
        }
    }

    private sealed class FakeChooseCommitHost : IChooseCommitHost
    {
        public ObjectId? GoTo { get; set; }

        public List<ObjectId> Filtered { get; } = [];

        public int NotFound { get; private set; }

        public ObjectId? ChooseCommitToGoTo() => GoTo;

        public void ShowRevisionFiltered(ObjectId objectId) => Filtered.Add(objectId);

        public void ShowRevisionNotFound() => NotFound++;
    }
}

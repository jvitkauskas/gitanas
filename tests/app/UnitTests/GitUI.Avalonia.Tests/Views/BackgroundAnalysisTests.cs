using Avalonia.Controls;
using Avalonia.Threading;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Editor;
using GitUI.Presentation.Editor;
using GitUI.Presentation.UserControls.FileStatusList;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class BackgroundAnalysisTests : HeadlessTest
{
    [Test]
    public Task Large_diff_highlighting_matches_synchronous_analysis_and_stays_on_ui_thread() => OnUiThreadAsync(async () =>
    {
        string patch = LargePatch();
        TextEditorViewModel model = new();
        bool notified = false;
        model.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TextEditorViewModel.InlineDiffMarkers))
            {
                Dispatcher.UIThread.CheckAccess().Should().BeTrue();
                notified = true;
            }
        };
        model.LoadDiff(patch);
        model.Text.Should().Be(patch);
        model.InlineDiffMarkers.Should().BeEmpty("the document is available before word analysis finishes");
        await WaitAsync(() => model.InlineDiffLoading.IsCompleted);
        model.InlineDiffLoading.IsCompletedSuccessfully.Should().BeTrue();
        model.InlineDiffMarkers.Should().Equal(InlineDiffAnalyzer.Analyze(patch, model.DiffLines!));
        notified.Should().BeTrue();
    });

    [Test]
    public Task Applying_background_highlights_keeps_the_editors_selection() => OnUiThreadAsync(async () =>
    {
        TextEditorViewModel model = new();
        TextEditorView view = new() { DataContext = model };
        Window window = new() { Content = view, Width = 800, Height = 500 };
        window.Show();
        try
        {
            model.LoadDiff(LargePatch());
            view.Editor.Select(100, 20);
            await WaitAsync(() => model.InlineDiffLoading.IsCompleted);
            model.InlineDiffLoading.IsCompletedSuccessfully.Should().BeTrue();
            view.Editor.SelectionStart.Should().Be(100);
            view.Editor.SelectionLength.Should().Be(20);
            view.Editor.Text.Should().Be(model.Text);
        }
        finally
        {
            window.Close();
        }
    });

    [Test]
    public Task Replacing_a_large_diff_discards_its_pending_markers() => OnUiThreadAsync(async () =>
    {
        TextEditorViewModel model = new();
        model.LoadDiff(LargePatch());
        Task pending = model.InlineDiffLoading;
        model.Load("different file");
        await WaitAsync(() => pending.IsCompleted);
        pending.IsCompletedSuccessfully.Should().BeTrue();
        model.Text.Should().Be("different file");
        model.InlineDiffMarkers.Should().BeEmpty();
        model.DiffLines.Should().BeNull();
    });

    [Test]
    public Task Latest_tree_filter_wins_and_preserves_selection() => OnUiThreadAsync(async () =>
    {
        FileStatusListViewModel model = new(new FileStatusListStrings()) { IsFileTreeMode = true, SelectFirstItemOnSetItems = false };
        GitItemStatus[] files = [.. Enumerable.Range(0, 6000).Select(i => new GitItemStatus($"src/file{i:D5}.cs") { IsTracked = true })];
        model.SetGroups([new(null, null!, "", files)]);
        model.IsLoading.Should().BeTrue();
        await model.WaitForTreeAsync();
        model.AllEntries.Should().HaveCount(6000);
        model.Select(e => e.Item.Name == "src/file00042.cs");
        model.Filter = "file000";
        model.Filter = "file0004";
        await model.WaitForTreeAsync();
        model.AllEntries.Should().HaveCount(10);
        model.SelectedEntry!.Item.Name.Should().Be("src/file00042.cs");
        model.IsLoading.Should().BeFalse();
    });

    [Test]
    public Task Clearing_or_loading_a_new_revision_supersedes_a_pending_tree() => OnUiThreadAsync(async () =>
    {
        FileStatusListViewModel model = new(new FileStatusListStrings()) { IsFileTreeMode = true };
        GitItemStatus[] files = [.. Enumerable.Range(0, 6000).Select(i => new GitItemStatus($"src/{i}.cs") { IsTracked = true })];
        model.SetGroups([new(null, null!, "", files)]);
        Task pending = model.TreeLoading;
        model.Clear();
        await WaitAsync(() => pending.IsCompleted);
        model.AllEntries.Should().BeEmpty();
        model.IsLoading.Should().BeFalse();
        model.SetGroups([new(null, null!, "", files)]);
        pending = model.TreeLoading;
        model.SetLoading();
        model.Filter = "new";
        model.TreeLoading.IsCompleted.Should().BeTrue("filtering waits for the new revision's files");
        model.IsLoading.Should().BeTrue();
        model.SetGroups([new(null, null!, "", [new GitItemStatus("new.txt") { IsTracked = true }])]);
        await WaitAsync(() => pending.IsCompleted);
        model.AllEntries.Select(e => e.Item.Name).Should().Equal("new.txt");
    });

    private static string LargePatch()
        => "diff --git a/x b/x\n--- a/x\n+++ b/x\n@@ -1,3000 +1,3000 @@\n"
            + string.Concat(Enumerable.Range(0, 3000).Select(i => $"-some old source line {i} with enough text\n+some new source line {i} with enough text\n"));

    private static async Task WaitAsync(Func<bool> completed)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);
        while (!completed() && DateTime.UtcNow < deadline)
        {
            Dispatcher.UIThread.RunJobs();
            await Task.Delay(10);
        }

        completed().Should().BeTrue();
    }
}

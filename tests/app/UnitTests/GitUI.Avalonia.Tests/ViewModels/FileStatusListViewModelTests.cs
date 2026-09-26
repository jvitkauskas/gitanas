using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.FileStatusList;
using GitUIPluginInterfaces;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the file status list (port of <c>FileStatusList</c>; the tree is checked against WinForms elsewhere).</summary>
[TestFixture]
public sealed class FileStatusListViewModelTests
{
    internal static readonly GitRevision First = new(ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1"));
    internal static readonly GitRevision Second = new(ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2"));

    internal static List<GitItemStatus> CreateStatuses() =>
    [
        new("src/Program.cs") { IsChanged = true, IsTracked = true },
        new("src/Util/Helper.cs") { IsNew = true },
        new("docs/readme.md") { IsDeleted = true, IsTracked = true },
        new("build.cmd") { IsRenamed = true, IsTracked = true, RenameCopyPercentage = "90", OldName = "build.bat" },
    ];

    [Test]
    public void Filter_history_keeps_valid_matching_expressions_and_reuses_them_without_losing_selection()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.FilterHistory.Should().Equal("^(?!.*NotThisWord)", @"^(?!.*\bg?tests?/)");
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.Select(e => e.Item.Name == "src/Program.cs");
        viewModel.Filter = @"\.cs$";
        viewModel.RememberFilter();
        viewModel.Filter = "src";
        viewModel.RememberFilter();
        viewModel.ApplyFilterCommand.Execute(@"\.cs$");
        viewModel.FilterHistory.Take(2).Should().Equal(@"\.cs$", "src");
        viewModel.FilterHistory.Should().HaveCount(4);
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs");

        foreach (string ignored in new[] { "(", "does-not-match", "", " " })
        {
            viewModel.Filter = ignored;
            viewModel.RememberFilter();
        }

        viewModel.FilterHistory.Should().HaveCount(4);
        viewModel.ClearFilterCommand.Execute(null);
        viewModel.AllEntries.Should().HaveCount(4);
        Create().FilterHistory.Should().HaveCount(2, "each list has its own history");
    }

    [Test]
    public void Filter_history_is_bounded_and_moves_reused_filters_to_the_front()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());
        for (int index = 0; index < 12; index++)
        {
            viewModel.Filter = $"src(?#filter{index})";
            viewModel.RememberFilter();
        }

        viewModel.FilterHistory.Should().HaveCount(10);
        viewModel.FilterHistory.Should().NotContain("src(?#filter0)");
        viewModel.ApplyFilterCommand.Execute("src(?#filter4)");
        viewModel.FilterHistory[0].Should().Be("src(?#filter4)");
        viewModel.FilterHistory.Should().OnlyHaveUniqueItems().And.HaveCount(10);
    }

    [Test]
    public void Shows_a_tree_of_folders_and_selects_the_first_file()
    {
        FileStatusListViewModel viewModel = Create();
        int changes = 0;
        viewModel.SelectionChanged += (_, _) => changes++;

        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.Nodes.Select(n => n.Text).Should().Equal("docs", "src", "build.cmd (build.bat)");
        viewModel.Nodes[1].Children.Select(n => n.Text).Should().Equal("Util", "Program.cs");
        viewModel.Nodes[2].DisplayName.Should().Be("build.cmd");
        viewModel.Nodes[2].OldName.Should().Be("(build.bat)");
        viewModel.Nodes[2].IconKey.Should().Be("FileStatusModified", "a rename with changes");
        viewModel.SelectedEntry!.Item.Name.Should().Be("docs/readme.md");
        viewModel.SelectedEntry.FirstRevision.Should().BeSameAs(First);
        viewModel.SelectedEntry.SecondRevision.Should().BeSameAs(Second);
        changes.Should().Be(1);
        viewModel.ShowNoFiles.Should().BeFalse();
    }

    [Test]
    public void Selecting_a_folder_selects_its_files()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.SelectedNodes.Clear();
        viewModel.SelectedNodes.Add(viewModel.Nodes[1]);

        viewModel.SelectedEntries.Select(e => e.Item.Name).Should().Equal("src/Util/Helper.cs", "src/Program.cs");
        viewModel.SelectedEntry.Should().BeNull("more than one file");
    }

    [Test]
    public void Filter_keeps_the_matching_files_and_reports_an_invalid_expression()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());
        viewModel.Select(e => e.Item.Name == "src/Program.cs");

        viewModel.Filter = @"\.cs$";
        viewModel.AllEntries.Select(e => e.Item.Name).Should().Equal("src/Util/Helper.cs", "src/Program.cs");
        viewModel.IsFilterActive.Should().BeTrue();
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Program.cs", "the selection is kept");

        viewModel.Filter = "(";
        viewModel.FilterError.Should().NotBeNull();
        viewModel.AllEntries.Should().HaveCount(2, "the previous filter stays");

        viewModel.ClearFilterCommand.Execute(null);
        viewModel.AllEntries.Should().HaveCount(4);
        viewModel.FilterError.Should().BeNull();
    }

    [Test]
    public void Flat_list_and_sorting_rebuild_the_tree()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.ToggleFlatListCommand.Execute(null);
        viewModel.IsFlatList.Should().BeTrue();
        viewModel.Nodes.Should().OnlyContain(n => n.Entry != null);
        viewModel.Nodes.Select(n => n.Text).Should().Equal(["docs/readme.md", "src/Util/Helper.cs", "src/Program.cs", "build.cmd (build.bat)"], "the paths are sorted folders first, as in the tree");

        viewModel.SortByCommand.Execute(DiffListSortType.FileStatus);
        viewModel.Options.SortType.Should().Be(DiffListSortType.FileStatusFlat, "the flat variant is kept");
    }

    [Test]
    public void Next_and_previous_file_and_the_empty_list()
    {
        FileStatusListViewModel viewModel = Create();
        viewModel.SetDiff(First, Second, CreateStatuses());

        viewModel.SelectNextItem(backwards: false);
        viewModel.SelectedEntry!.Item.Name.Should().Be("src/Util/Helper.cs");
        viewModel.SelectNextItem(backwards: true);
        viewModel.SelectedEntry!.Item.Name.Should().Be("docs/readme.md");

        viewModel.SetLoading();
        viewModel.IsLoading.Should().BeTrue();
        viewModel.SetDiff(First, Second, []);
        viewModel.IsLoading.Should().BeFalse();
        viewModel.ShowNoFiles.Should().BeTrue();
        viewModel.SelectedEntries.Should().BeEmpty();
    }

    [Test]
    public void Several_diffs_are_groups_with_their_counts()
    {
        FileStatusListViewModel viewModel = Create();

        viewModel.SetGroups(
        [
            new FileStatusGroup(First, Second, "First parent", CreateStatuses()),
            new FileStatusGroup(null, Second, "Second parent", []),
        ]);

        viewModel.Nodes.Select(n => n.Text).Should().Equal("(4) First parent", "(0) Second parent");
        viewModel.Nodes[1].Children.Single().Text.Should().Be("- No changes -");
        viewModel.Nodes[1].Children.Single().IconKey.Should().Be(FileStatusIcons.FileStatusCopiedSame);
    }

    internal static FileStatusListViewModel Create() => new(new FileStatusListStrings());
}

using CommonTestUtils;
using GitCommands.UserRepositoryHistory;
using GitExtensions.Extensibility.Git;
using GitUI;
using GitUI.CommandsDialogs;
using NSubstitute;

namespace GitUITests;

public sealed class RepositoryHistoryUIServiceTests
{
    private RepositoryHistoryUIService _service = null!;
    private IRepositoryCurrentBranchNameCache _branchNameCache = null!;
    private IInvalidRepositoryRemover _invalidRepositoryRemover = null!;

    [SetUp]
    public void Setup()
    {
        _branchNameCache = Substitute.For<IRepositoryCurrentBranchNameCache>();
        _invalidRepositoryRemover = Substitute.For<IInvalidRepositoryRemover>();

        _service = new RepositoryHistoryUIService(Substitute.For<IGitExecutorProvider>(), _branchNameCache, _invalidRepositoryRemover);
    }

    [Test]
    public void CreateRepositoryItem_should_set_properties_correctly()
    {
        const string path = "";
        const string caption = "CAPTION";
        Repository repository = new(path);

        RepositoryMenuItem item = _service.GetTestAccessor().CreateRepositoryItem(repository, caption, number: 1);

        item.Text.Should().Be($"&1: {caption}");
        item.IsPinned.Should().BeFalse();
        item.IsSeparator.Should().BeFalse();
        item.ToolTip.Should().BeEmpty("the tooltip is the (empty) path");
        item.Open.Should().NotBeNull();
    }

    [TestCase(9, "&9: CAPTION")]
    [TestCase(10, "1&0: CAPTION")]
    [TestCase(11, "11: CAPTION")]
    public void CreateRepositoryItem_should_number_the_items(int number, string expected)
    {
        _service.GetTestAccessor().CreateRepositoryItem(new Repository("somepath"), "CAPTION", number).Text.Should().Be(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("master")]
    [TestCase("(no branch)")]
    public void CreateRepositoryItem_should_show_branch_correctly(string? branch)
    {
        _branchNameCache.GetCachedBranchName(Arg.Any<string>()).Returns(string.IsNullOrWhiteSpace(branch) ? null : branch);

        const string path = "somepath";
        const string caption = "CAPTION";
        Repository repository = new(path);

        RepositoryMenuItem item = _service.GetTestAccessor().CreateRepositoryItem(repository, caption, number: 1);

        if (string.IsNullOrWhiteSpace(branch))
        {
            item.BranchName.Should().BeNullOrEmpty();
        }
        else
        {
            item.BranchName.Should().Be(branch);
        }

        item.ToolTip.Should().Be(path, "the caption shortens the path");
    }

    [Test]
    public void ChangeWorkingDir_should_promt_user_to_delete_invalid_repo()
    {
        const string path = "";
        const string caption = "CAPTION";
        Repository repository = new(path);

        RepositoryMenuItem item = _service.GetTestAccessor().CreateRepositoryItem(repository, caption, number: 1);
        item.Open!();

        _invalidRepositoryRemover.Received(1).ShowDeleteInvalidRepositoryDialog(path);
    }

    [Test]
    public void GetFavouriteRepositoriesMenu_should_order_favourites_alphabetically()
    {
        List<Repository> repositoryHistory =
        [
            new Repository(TestPaths.Native(@"c:\repos\D")) { Category = "D" },
            new Repository(TestPaths.Native(@"c:\repos\A")) { Category = "A" },
            new Repository(TestPaths.Native(@"c:\repos\C")) { Category = "C" },
            new Repository(TestPaths.Native(@"c:\repos\B")) { Category = "B" }
        ];

        IReadOnlyList<RepositoryMenuItem> categories = _service.GetTestAccessor().GetFavouriteRepositoriesMenu(repositoryHistory);

        categories.Select(x => x.Text).Should().BeInAscendingOrder();
        categories.Should().OnlyContain(category => category.Children.Count == 1);
    }
}

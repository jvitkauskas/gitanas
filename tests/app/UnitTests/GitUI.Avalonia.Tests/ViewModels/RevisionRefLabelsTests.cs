using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.UserControls.RevisionGrid;
using GitUIPluginInterfaces;
using NSubstitute;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>The reference labels of a revision (the labels of <c>MessageColumnProvider</c>).</summary>
[TestFixture]
public sealed class RevisionRefLabelsTests
{
    private static readonly RevisionGridDisplayOptions _options = new(RelativeDate: false, ShowAuthorDate: false);

    [TestCase(false)]
    [TestCase(true)]
    public void Commits_without_visible_references_do_not_read_branch_tracking_data(bool hiddenReferences)
    {
        IReadOnlyDictionary<string, AheadBehindData> aheadBehind = Substitute.For<IReadOnlyDictionary<string, AheadBehindData>>();
        GitRevision revision = hiddenReferences ? Revision(Tag("v1.0"), Remote("origin", "main")) : Revision();

        List<RevisionRefItem> items = RevisionRefLabels.Build(revision, _options with { ShowTags = false, ShowRemoteBranches = false }, currentBranch: null, aheadBehind);

        items.Should().BeEmpty();
        aheadBehind.ReceivedCalls().Should().BeEmpty("unlabelled history rows need no branch tracking lookups");
    }

    [Test]
    public void A_branch_at_the_remote_branch_it_tracks_shows_it_nestled_and_another_remote_by_its_name()
    {
        IGitRef main = Local("main");
        IGitRef originMain = Remote("origin", "main");
        IGitRef upstreamMain = Remote("upstream", "main");
        main.IsTrackingRemote(originMain).Returns(true);

        List<RevisionRefItem> items = RevisionRefLabels.Build(Revision(upstreamMain, originMain, main), _options, currentBranch: "main", aheadBehind: null);

        items.Select(i => (i.Name, i.Nested?.Name)).Should().Equal(("main", "origin"), ("upstream", null));
        items[0].IsCurrentBranch.Should().BeTrue();
        items[0].Nested!.GitRef.Should().BeSameAs(originMain);
        items[0].Nested!.Kind.Should().Be(RevisionRefKind.RemoteBranch);
    }

    [Test]
    public void The_ahead_and_behind_counts_are_a_virtual_label_of_the_tracked_branch()
    {
        IGitRef feature = Local("feature");
        Dictionary<string, AheadBehindData> aheadBehind = new()
        {
            ["feature"] = new AheadBehindData("feature", "refs/remotes/origin/feature", AheadCount: "2", BehindCount: ""),
            ["old"] = new AheadBehindData("old", "refs/remotes/origin/old", AheadCount: AheadBehindData.Gone, BehindCount: ""),
        };

        List<RevisionRefItem> items = RevisionRefLabels.Build(Revision(feature, Local("old")), _options, currentBranch: null, aheadBehind);

        // From the perspective of the remote branch, the local one is ahead: the remote is behind.
        RevisionRefItem featureLabel = items.Single(i => i.Name == "feature");
        featureLabel.Nested!.Name.Should().Be("↓");
        featureLabel.Nested.IsVirtual.Should().BeTrue();
        featureLabel.Nested.RelatedRefCompleteName.Should().Be("refs/remotes/origin/feature", "a double click goes to the tracked branch");
        featureLabel.RelatedRefCompleteName.Should().Be("refs/remotes/origin/feature");
        featureLabel.ToolTip.Should().Be($"[feature]   2↑{Environment.NewLine}is tracking [origin/feature]");
        featureLabel.Nested.ToolTip.Should().Be($"[origin/feature]{Environment.NewLine}is tracked by [feature]   2↑");

        RevisionRefItem oldLabel = items.Single(i => i.Name == "old");
        oldLabel.Nested!.Name.Should().Be(AheadBehindData.GoneSymbol);
        oldLabel.Nested.IsGone.Should().BeTrue();
        oldLabel.Nested.GoneLocalBranch.Should().Be("old", "a double click offers to delete the branch");
        oldLabel.ToolTip.Should().Be($"[old]{Environment.NewLine}was tracking [origin/old], but the remote is gone");
    }

    [Test]
    public void The_labels_are_sorted_and_have_a_tooltip_only_with_data_unless_always()
    {
        IGitRef tag = Tag("v1.0");
        IGitRef branch = Local("b");
        IGitRef remote = Remote("origin", "a");

        List<RevisionRefItem> items = RevisionRefLabels.Build(Revision(tag, remote, branch), _options, currentBranch: null, aheadBehind: null);

        items.Select(i => i.Name).Should().Equal("b", "origin/a", "v1.0");
        items.Should().OnlyContain(i => i.ToolTip == null);

        items = RevisionRefLabels.Build(Revision(tag, remote, branch), _options with { ShowRevisionGridTooltips = true }, currentBranch: null, aheadBehind: null);
        items.Select(i => i.ToolTip).Should().Equal(
            $"[b]{Environment.NewLine}is a local branch",
            $"[origin/a]{Environment.NewLine}is a remote branch",
            $"[v1.0]{Environment.NewLine}is a tag");
    }

    private static GitRevision Revision(params IGitRef[] refs) => new(ObjectId.Random()) { Refs = refs };

    private static IGitRef Local(string name)
    {
        IGitRef gitRef = Substitute.For<IGitRef>();
        gitRef.IsHead.Returns(true);
        gitRef.Name.Returns(name);
        gitRef.LocalName.Returns(name);
        gitRef.CompleteName.Returns($"refs/heads/{name}");
        return gitRef;
    }

    private static IGitRef Remote(string remote, string name)
    {
        IGitRef gitRef = Substitute.For<IGitRef>();
        gitRef.IsRemote.Returns(true);
        gitRef.Remote.Returns(remote);
        gitRef.Name.Returns($"{remote}/{name}");
        gitRef.LocalName.Returns(name);
        gitRef.CompleteName.Returns($"refs/remotes/{remote}/{name}");
        return gitRef;
    }

    private static IGitRef Tag(string name)
    {
        IGitRef gitRef = Substitute.For<IGitRef>();
        gitRef.IsTag.Returns(true);
        gitRef.Name.Returns(name);
        gitRef.CompleteName.Returns($"refs/tags/{name}");
        return gitRef;
    }
}

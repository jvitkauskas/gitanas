using System.Text;
using GitCommands.Git;
using GitExtensions.Extensibility.Git;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.RevisionGrid;

/// <summary>The strings of the tooltips of the reference labels (<c>TranslatedStrings</c> of <c>MessageColumnProvider.TryGetToolTip</c>).</summary>
public sealed class RevisionRefLabelStrings : ViewStrings
{
    public RevisionRefLabelStrings()
        : base("RevisionGridControl")
    {
        IsLocalBranch = Add("_isLocalBranch", "Text", "is a local branch", category: "TranslatedStrings");
        IsRemoteBranch = Add("_isRemoteBranch", "Text", "is a remote branch", category: "TranslatedStrings");
        IsTag = Add("_isTag", "Text", "is a tag", category: "TranslatedStrings");
        IsTrackedByBranchAheadBehind = Add("_isTrackedBy_Branch_AheadBehind", "Text", "is tracked by [{0}]   {1}", category: "TranslatedStrings");
        IsTrackingRemote = Add("_isTracking_Remote", "Text", "is tracking [{0}]", category: "TranslatedStrings");
        WasTrackingRemote = Add("_wasTracking_Remote", "Text", "was tracking [{0}], but the remote is gone", category: "TranslatedStrings");
    }

    public TranslatedText IsLocalBranch { get; }

    public TranslatedText IsRemoteBranch { get; }

    public TranslatedText IsTag { get; }

    public TranslatedText IsTrackedByBranchAheadBehind { get; }

    public TranslatedText IsTrackingRemote { get; }

    public TranslatedText WasTrackingRemote { get; }
}

/// <summary>
///  The reference labels of a revision (port of the labels of <c>MessageColumnProvider.OnCellPainting</c>): a local branch at
///  the remote branch it tracks shows the remote nestled after it, a branch with ahead / behind counts shows them as a virtual
///  label of its tracked (or tracking) branch, and each label has the tooltip of <c>TryGetToolTip</c> and the branch a
///  double click goes to (<c>GoToRelatedRef</c>).
/// </summary>
public static class RevisionRefLabels
{
    private const string RefsHeadsPrefix = "refs/heads/";
    private const string RefsRemotesPrefix = "refs/remotes/";

    private static readonly Lazy<RevisionRefLabelStrings> _strings = new(ViewStrings.Load<RevisionRefLabelStrings>);

    /// <param name="aheadBehind">The ahead / behind data by local branch (<c>IAheadBehindDataProvider.GetData</c>), if shown.</param>
    public static List<RevisionRefItem> Build(GitRevision revision, RevisionGridDisplayOptions options, string? currentBranch, IReadOnlyDictionary<string, AheadBehindData>? aheadBehind)
    {
        // Most commits have no references. Avoid sorting and rebuilding the tracking lookup for every history row.
        if (revision.Refs.Count == 0)
        {
            return [];
        }

        List<IGitRef> gitRefs = SortRefs(revision.Refs.Where(r => r.IsTag ? options.ShowTags : !r.IsRemote || options.ShowRemoteBranches));
        if (gitRefs.Count == 0)
        {
            return [];
        }

        AheadBehindLookup lookup = new(aheadBehind);
        Dictionary<string, IGitRef> trackedRemotes = BuildTrackedRemoteMap(gitRefs);

        // As OnCellPainting: with a single local branch on the commit, a remote branch of the same name shows its remote only.
        string? singleLocalBranchName = gitRefs.Count(r => r.IsHead) == 1 ? trackedRemotes.Keys.FirstOrDefault() : null;
        List<RevisionRefItem> items = [];
        foreach (IGitRef gitRef in gitRefs)
        {
            // A remote branch tracked by a local branch of the commit is nestled after it.
            if (trackedRemotes.ContainsValue(gitRef))
            {
                continue;
            }

            RevisionRefItem item = CreateItem(gitRef, gitRef.Name, options, currentBranch, lookup);
            if (gitRef.IsHead && trackedRemotes.TryGetValue(gitRef.Name, out IGitRef? remote))
            {
                // As DrawBranchWithNestledRemote: the tracked remote, with the name of its remote.
                item = item with { Nested = CreateItem(remote, remote.Remote, options, currentBranch, lookup) };
            }
            else if (lookup.GetAheadBehind(gitRef) is ({ Length: > 0 } display, string trackedCompleteName, bool isGone))
            {
                // As NestledVirtualRef: the ahead / behind counts as a label of the tracked (or tracking) branch.
                item = item with
                {
                    Nested = new RevisionRefItem(display, RevisionRefKind.RemoteBranch, IsCurrentBranch: false)
                    {
                        IsVirtual = true,
                        IsGone = isGone,
                        RelatedRefCompleteName = isGone ? null : trackedCompleteName,
                        GoneLocalBranch = isGone ? gitRef.Name : null,
                        ToolTip = GetVirtualToolTip(gitRef, trackedCompleteName, lookup),
                    },
                };
            }
            else if (singleLocalBranchName is not null && gitRef.IsRemote && gitRef.LocalName == singleLocalBranchName)
            {
                item = item with { Name = gitRef.Remote };
            }

            items.Add(item);
        }

        return items;
    }

    private static RevisionRefItem CreateItem(IGitRef gitRef, string name, RevisionGridDisplayOptions options, string? currentBranch, AheadBehindLookup lookup)
    {
        // As GoToRelatedRef for a real reference: the other branch of its ahead / behind data, or the deletion of a gone one.
        AheadBehindData? data = lookup.Get(gitRef.IsRemote, gitRef.CompleteName);
        bool isGone = data?.AheadCount == AheadBehindData.Gone;
        return new RevisionRefItem(
            name,
            gitRef.IsHead ? RevisionRefKind.Branch : gitRef.IsRemote ? RevisionRefKind.RemoteBranch : gitRef.IsTag ? RevisionRefKind.Tag : RevisionRefKind.Other,
            gitRef.IsHead && gitRef.Name == currentBranch)
        {
            GitRef = gitRef,
            RelatedRefCompleteName = data is { } related && !isGone ? (gitRef.IsRemote ? RefsHeadsPrefix + related.Branch : related.RemoteRef) : null,
            GoneLocalBranch = isGone ? gitRef.Name : null,
            ToolTip = GetToolTip(gitRef, data, options),
        };
    }

    /// <summary>As <c>TryGetToolTip</c> with a highlighted reference: its name and its ahead / behind data (or what it is).</summary>
    private static string? GetToolTip(IGitRef gitRef, AheadBehindData? data, RevisionGridDisplayOptions options)
    {
        RevisionRefLabelStrings strings = _strings.Value;
        StringBuilder toolTip = new();
        toolTip.Append('[').Append(gitRef.Name).Append(']');
        if (gitRef.IsRemote)
        {
            if (data is { } remoteData)
            {
                toolTip.AppendLine().AppendFormat(strings.IsTrackedByBranchAheadBehind.Text, remoteData.Branch, remoteData.ToDisplay());
            }
            else if (options.ShowRevisionGridTooltips)
            {
                toolTip.AppendLine().Append(strings.IsRemoteBranch.Text);
            }
            else
            {
                return null;
            }
        }
        else if (gitRef.IsHead)
        {
            if (data is { } localData)
            {
                AppendTracking(toolTip, localData, strings);
            }
            else if (options.ShowRevisionGridTooltips)
            {
                toolTip.AppendLine().Append(strings.IsLocalBranch.Text);
            }
            else
            {
                return null;
            }
        }
        else if (gitRef.IsTag)
        {
            if (!options.ShowRevisionGridTooltips)
            {
                return null;
            }

            toolTip.AppendLine().Append(strings.IsTag.Text);
        }

        return toolTip.ToString();
    }

    /// <summary>As <c>TryGetToolTip</c> for a <c>NestledVirtualRef</c>: the tracked (or tracking) branch and its data.</summary>
    private static string GetVirtualToolTip(IGitRef gitRef, string trackedCompleteName, AheadBehindLookup lookup)
    {
        RevisionRefLabelStrings strings = _strings.Value;
        StringBuilder toolTip = new();

        // The label stands for the other branch: the tracked remote of a local branch, the tracking branch of a remote one.
        bool realRefIsRemote = !gitRef.IsRemote;
        string realRefName = trackedCompleteName.StartsWith(RefsRemotesPrefix, StringComparison.Ordinal) ? trackedCompleteName[RefsRemotesPrefix.Length..]
            : trackedCompleteName.StartsWith(RefsHeadsPrefix, StringComparison.Ordinal) ? trackedCompleteName[RefsHeadsPrefix.Length..]
            : trackedCompleteName;
        AheadBehindData? data = lookup.Get(realRefIsRemote, trackedCompleteName);
        toolTip.Append('[').Append(realRefName).Append(']');
        if (realRefIsRemote)
        {
            toolTip.AppendLine().AppendFormat(strings.IsTrackedByBranchAheadBehind.Text, data?.Branch, data?.ToDisplay());
        }
        else if (data is { } localData)
        {
            AppendTracking(toolTip, localData, strings);
        }

        return toolTip.ToString();
    }

    private static void AppendTracking(StringBuilder toolTip, AheadBehindData data, RevisionRefLabelStrings strings)
    {
        string remoteBranch = data.RemoteRef.StartsWith(RefsRemotesPrefix, StringComparison.Ordinal) ? data.RemoteRef[RefsRemotesPrefix.Length..] : data.RemoteRef;
        if (data.AheadCount == AheadBehindData.Gone)
        {
            toolTip.AppendLine().AppendFormat(strings.WasTrackingRemote.Text, remoteBranch);
        }
        else
        {
            toolTip.Append("   ").AppendLine(data.ToDisplay()).AppendFormat(strings.IsTrackingRemote.Text, remoteBranch);
        }
    }

    /// <summary>As <c>SortRefs</c>: bisect, the checked out branch, its merge source, the branches, the remote branches, the others.</summary>
    private static List<IGitRef> SortRefs(IEnumerable<IGitRef> refs)
        => [.. refs.OrderBy(Rank).ThenBy(r => r.Name, StringComparer.Ordinal)];

    private static int Rank(IGitRef gitRef)
        => gitRef.IsBisect ? 0
            : gitRef.IsSelected ? 1
            : gitRef.IsSelectedHeadMergeSource ? 2
            : gitRef.IsHead ? 3
            : gitRef.IsRemote ? 4
            : 5;

    /// <summary>As <c>BuildTrackedRemoteMap</c>: the remote branch of the commit each local branch of the commit tracks.</summary>
    private static Dictionary<string, IGitRef> BuildTrackedRemoteMap(IReadOnlyList<IGitRef> refs)
    {
        Dictionary<string, IGitRef> remoteByLocal = [];
        List<IGitRef> localBranches = [.. refs.Where(r => r.IsHead)];
        foreach (IGitRef remote in refs.Where(r => r.IsRemote))
        {
            foreach (IGitRef local in localBranches.Where(local => local.IsTrackingRemote(remote)))
            {
                remoteByLocal.TryAdd(local.LocalName, remote);
            }
        }

        return remoteByLocal;
    }

    /// <summary>The ahead / behind data by local branch and by remote branch (<c>GetAheadBehind</c>, <c>GetAheadBehindData</c>).</summary>
    private sealed class AheadBehindLookup(IReadOnlyDictionary<string, AheadBehindData>? byLocalBranch)
    {
        private readonly Dictionary<string, AheadBehindData> _byRemoteBranch = byLocalBranch is null
            ? []
            : byLocalBranch.Values.DistinctBy(data => data.RemoteRef).ToDictionary(data => data.RemoteRef);

        public AheadBehindData? Get(bool isRemote, string completeName)
        {
            if (isRemote)
            {
                return _byRemoteBranch.TryGetValue(completeName, out AheadBehindData remoteData) ? remoteData : null;
            }

            return byLocalBranch is not null && completeName.StartsWith(RefsHeadsPrefix, StringComparison.Ordinal)
                && byLocalBranch.TryGetValue(completeName[RefsHeadsPrefix.Length..], out AheadBehindData data)
                    ? data
                    : null;
        }

        /// <summary>
        ///  As <c>GetAheadBehind(withCounts: false)</c>: the arrows of the ahead / behind data (from the perspective of the other
        ///  branch for a local one), the other branch, and whether it is gone.
        /// </summary>
        public (string Display, string TrackedCompleteName, bool IsGone) GetAheadBehind(IGitRef gitRef)
        {
            if (gitRef.IsRemote)
            {
                if (_byRemoteBranch.TryGetValue(gitRef.CompleteName, out AheadBehindData remoteData))
                {
                    return (remoteData.ToDisplay(withCounts: false), RefsHeadsPrefix + remoteData.Branch, remoteData.AheadCount == AheadBehindData.Gone);
                }
            }
            else if (byLocalBranch is not null && byLocalBranch.TryGetValue(gitRef.Name, out AheadBehindData data))
            {
                return (data.ToDisplay(withCounts: false, reverse: true), data.RemoteRef, data.AheadCount == AheadBehindData.Gone);
            }

            return ("", "", false);
        }
    }
}

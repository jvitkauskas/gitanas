using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUIPluginInterfaces;

namespace GitUI.Presentation.UserControls.FileStatusList;

/// <summary>A file of a diff (the WinForms <c>FileStatusItem</c>).</summary>
public sealed record FileStatusEntry(GitRevision? FirstRevision, GitRevision SecondRevision, GitItemStatus Item, ObjectId? BaseA = null, ObjectId? BaseB = null);

/// <summary>The files of a diff between two revisions (the WinForms <c>FileStatusWithDescription</c>).</summary>
public sealed record FileStatusGroup(
    GitRevision? FirstRevision,
    GitRevision SecondRevision,
    string Summary,
    IReadOnlyList<GitItemStatus> Statuses,
    ObjectId? BaseA = null,
    ObjectId? BaseB = null,
    string IconName = FileStatusIcons.Diff);

/// <summary>A node of the file status tree: a diff group, a folder, a group of a sorting, or a file.</summary>
public sealed partial class FileStatusNode : ObservableObject
{
    public FileStatusNode(string text, string iconKey)
    {
        Text = text;
        IconKey = iconKey;
    }

    /// <summary>The text of the node, as the WinForms tree node (folders relative to their parent).</summary>
    [ObservableProperty]
    public partial string Text { get; set; }

    [ObservableProperty]
    public partial string IconKey { get; set; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; }

    /// <summary>The file, for a file node.</summary>
    public FileStatusEntry? Entry { get; set; }

    /// <summary>The path, for a folder node.</summary>
    public RelativePath? FolderPath { get; set; }

    /// <summary>The diff, for the node of a diff group.</summary>
    public FileStatusGroup? Group { get; set; }

    public FileStatusNode? Parent { get; private set; }

    public ObservableCollection<FileStatusNode> Children { get; } = [];

    /// <summary>
    ///  The file whose type the icon of a plain file shows: this one, or the first file below a group node (as
    ///  <c>FindMissingIcons</c>).
    /// </summary>
    public string? IconFileName => Entry?.Item.Name ?? DescendantsAndSelf().Select(node => node.Entry).OfType<FileStatusEntry>().FirstOrDefault()?.Item.Name;

    /// <summary>The name shown for a file: relative to its folder, and its old name if renamed.</summary>
    public string DisplayName => Entry is { Item: var item } && !item.IsRangeDiff ? GetDisplayName(item) : Text;

    /// <summary>The old name of a renamed or copied file, shown after its name.</summary>
    public string? OldName => Entry is { Item: { IsRangeDiff: false, OldName: { } oldName } } ? $"({oldName})" : null;

    public void Add(FileStatusNode child)
    {
        child.Parent = this;
        Children.Add(child);
    }

    public void Insert(int index, FileStatusNode child)
    {
        child.Parent = this;
        Children.Insert(index, child);
    }

    public void Remove(FileStatusNode child)
    {
        Children.Remove(child);
        child.Parent = null;
    }

    /// <summary>This node and its descendants, depth first (as <c>TreeNode.Items()</c> with the node itself).</summary>
    public IEnumerable<FileStatusNode> DescendantsAndSelf()
    {
        yield return this;
        foreach (FileStatusNode child in Children)
        {
            foreach (FileStatusNode node in child.DescendantsAndSelf())
            {
                yield return node;
            }
        }
    }

    public void ExpandAll()
    {
        foreach (FileStatusNode node in DescendantsAndSelf())
        {
            node.IsExpanded = node.Children.Count > 0;
        }
    }

    // As FileStatusList.FormatListViewItem (without the truncation to the width).
    private string GetDisplayName(GitItemStatus item)
    {
        string name = item.Name.TrimEnd('/');
        string? parentPath = Parent?.FolderPath?.Value;
        if (!string.IsNullOrEmpty(parentPath) && name.StartsWith(parentPath))
        {
            name = name[(parentPath.Length + 1)..];
        }

        return name + FileStatusIcons.GetSubmoduleStatusSuffix(item);
    }
}

/// <summary>The icons of the file status list (the image keys of the WinForms <c>FileStatusList</c>).</summary>
public static class FileStatusIcons
{
    public const string Diff = nameof(Diff);
    public const string DiffA = nameof(DiffA);
    public const string DiffB = nameof(DiffB);
    public const string DiffC = nameof(DiffC);
    public const string DiffR = nameof(DiffR);
    public const string FolderClosed = nameof(FolderClosed);
    public const string DefaultFileImage = nameof(DefaultFileImage);
    public const string GitGrepIconName = nameof(GitGrepIconName);
    public const string FileStatusUnknown = nameof(FileStatusUnknown);
    public const string FileStatusCopiedSame = nameof(FileStatusCopiedSame);

    /// <summary>
    ///  The image keys in the order of <c>FileStatusList.CreateImageListData</c>, which is also the second key of sorting by
    ///  file status (after the diff status).
    /// </summary>
    public static readonly IReadOnlyList<string> Keys =
    [
        FolderClosed,
        FileStatusUnknown,
        "Unmerged",
        "FileStatusModifiedUnequal",
        "FileStatusModifiedOnlyB",
        "FileStatusModifiedOnlyA",
        "FileStatusModifiedSame",
        "FileStatusModified",
        "FileStatusCopiedUnequal",
        "FileStatusCopiedOnlyB",
        "FileStatusCopiedOnlyA",
        FileStatusCopiedSame,
        "FileStatusCopied",
        "FileStatusRenamedUnequal",
        "FileStatusRenamedOnlyB",
        "FileStatusRenamedOnlyA",
        "FileStatusRenamedSame",
        "FileStatusRenamed",
        "FileStatusAddedUnequal",
        "FileStatusAddedOnlyB",
        "FileStatusAddedOnlyA",
        "FileStatusAddedSame",
        "FileStatusAdded",
        "FileStatusRemovedUnequal",
        "FileStatusRemovedOnlyB",
        "FileStatusRemovedOnlyA",
        "FileStatusRemovedSame",
        "FileStatusRemoved",
        "SubmodulesManage",
        "FolderSubmodule",
        "SubmoduleDirty",
        "SubmoduleRevisionUp",
        "SubmoduleRevisionUpDirty",
        "SubmoduleRevisionDown",
        "SubmoduleRevisionDownDirty",
        "SubmoduleRevisionSemiUp",
        "SubmoduleRevisionSemiUpDirty",
        "SubmoduleRevisionSemiDown",
        "SubmoduleRevisionSemiDownDirty",
        GitGrepIconName,
        DefaultFileImage,
        Diff,
        DiffR,
        DiffB,
        DiffA,
        DiffC,
    ];

    /// <summary>As <c>FileStatusList.GetItemImageKey</c>.</summary>
    public static string GetItemImageKey(GitItemStatus gitItemStatus)
    {
        if (gitItemStatus.IsDeleted)
        {
            return WithDiffStatus("FileStatusRemoved", gitItemStatus.DiffStatus);
        }

        if (gitItemStatus.IsRangeDiff)
        {
            return DiffR;
        }

        if (!string.IsNullOrWhiteSpace(gitItemStatus.GrepString))
        {
            return DefaultFileImage;
        }

        if (gitItemStatus.IsNew || !gitItemStatus.IsTracked)
        {
            return WithDiffStatus("FileStatusAdded", gitItemStatus.DiffStatus);
        }

        if (gitItemStatus.IsUnmerged)
        {
            return "Unmerged";
        }

        if (gitItemStatus.IsSubmodule)
        {
            return GetSubmoduleItemImageKey(gitItemStatus);
        }

        if (gitItemStatus.IsChanged || (gitItemStatus.IsRenamed && gitItemStatus.RenameCopyPercentage != "100"))
        {
            return WithDiffStatus("FileStatusModified", gitItemStatus.DiffStatus);
        }

        if (gitItemStatus.IsRenamed)
        {
            return WithDiffStatus("FileStatusRenamed", gitItemStatus.DiffStatus);
        }

        if (gitItemStatus.IsCopied)
        {
            return WithDiffStatus("FileStatusCopied", gitItemStatus.DiffStatus);
        }

        // Illegal flag combinations or no flags set?
        return FileStatusUnknown;

        static string WithDiffStatus(string key, DiffBranchStatus diffStatus) => diffStatus switch
        {
            DiffBranchStatus.OnlyAChange => key + "OnlyA",
            DiffBranchStatus.OnlyBChange => key + "OnlyB",
            DiffBranchStatus.SameChange => key + "Same",
            DiffBranchStatus.UnequalChange => key + "Unequal",
            _ => key
        };
    }

    /// <summary>As <c>FileStatusList.GetSubmoduleItemImageKey</c>.</summary>
    public static string GetSubmoduleItemImageKey(GitItemStatus gitItemStatus)
    {
        if (GetCompletedSubmoduleStatus(gitItemStatus) is not GitSubmoduleStatus status)
        {
            return gitItemStatus.IsDirty ? "SubmoduleDirty" : "SubmodulesManage";
        }

        return (status.Status, status.IsDirty) switch
        {
            (SubmoduleStatus.FastForward, true) => "SubmoduleRevisionUpDirty",
            (SubmoduleStatus.FastForward, false) => "SubmoduleRevisionUp",
            (SubmoduleStatus.Rewind, true) => "SubmoduleRevisionDownDirty",
            (SubmoduleStatus.Rewind, false) => "SubmoduleRevisionDown",
            (SubmoduleStatus.NewerTime, true) => "SubmoduleRevisionSemiUpDirty",
            (SubmoduleStatus.NewerTime, false) => "SubmoduleRevisionSemiUp",
            (SubmoduleStatus.OlderTime, true) => "SubmoduleRevisionSemiDownDirty",
            (SubmoduleStatus.OlderTime, false) => "SubmoduleRevisionSemiDown",
            (SubmoduleStatus.SameCommit, false) => "FolderSubmodule",
            _ => "SubmoduleDirty",
        };
    }

    /// <summary>As <c>FileStatusList.AppendItemSubmoduleStatus</c>: the added and removed commits of a submodule.</summary>
    public static string GetSubmoduleStatusSuffix(GitItemStatus item)
        => item.IsSubmodule && GetCompletedSubmoduleStatus(item) is { } status ? status.AddedAndRemovedString() : "";

    private static GitSubmoduleStatus? GetCompletedSubmoduleStatus(GitItemStatus item)
        => item.GetSubmoduleStatusAsync() is Task<GitSubmoduleStatus?> { IsCompletedSuccessfully: true } task ? task.CompletedResult() : null;
}

/// <summary>How the files are sorted and shown (the sorting of <c>DiffListSortService</c> and the file status settings).</summary>
public sealed record FileStatusTreeOptions(
    DiffListSortType SortType = DiffListSortType.FilePath,
    bool GroupByRevision = false,
    bool MergeSingleItemsWithFolder = false,
    bool ShowGroupNodesInFlatList = true);

/// <summary>
///  Builds the file status tree, a port of the WinForms <c>FileStatusList.GetNodes</c> and <c>StatusSorter</c>
///  (docs/avalonia-port/PLAN.md, phase 5); keep it in sync.
/// </summary>
public static class FileStatusTreeBuilder
{
    /// <summary>The nodes and how they are expanded (as <c>UpdateFileStatusListView</c> applies <c>ExpandCollapseState</c>).</summary>
    public static (List<FileStatusNode> Nodes, bool ShowDiffGroups, bool FilesPresent) Build(
        IReadOnlyList<FileStatusGroup> items,
        FileStatusTreeOptions options,
        Func<GitItemStatus, bool> isFilterMatch,
        GitItemStatus noItemStatus,
        bool expandIfFewFiles = true)
    {
        List<FileStatusNode> rootNodes = [];
        bool showDiffGroups = items.Count > 1 || (options.GroupByRevision && !(items.Count == 1 && items[0].Statuses.Count == 0));
        bool filesPresent = items.Any(x => x.Statuses.Count > 0);
        bool hasGrepGroup = items.Any(IsGrepItemStatuses);
        bool showGroupLabel = (filesPresent && (items.Count > 1 || options.GroupByRevision)) || hasGrepGroup;
        bool flatList = options.SortType.ToString().EndsWith("Flat");
        bool showGroupNodes = !flatList || options.ShowGroupNodesInFlatList;

        foreach (FileStatusGroup i in items)
        {
            bool emptyGroup = showGroupLabel && i.Statuses.Count == 0;

            (FileStatusNode diffGroup, int shownCount)
                = i.Statuses.Count == 1 && i.Statuses[0].IsRangeDiff
                    ? (CreateNode(i.Statuses[0], i), 1)
                    : CreateGroup(emptyGroup ? [noItemStatus] : i.Statuses.Where(isFilterMatch), i);

            // Always expand grep results; collapse some groups for diffs with common BASE.
            ExpandCollapseState state
                = emptyGroup
                    ? ExpandCollapseState.Collapsed
                    : hasGrepGroup
                        ? IsGrepItemStatuses(i)
                            ? expandIfFewFiles && shownCount < 100
                                ? ExpandCollapseState.Expanded
                                : ExpandCollapseState.PartiallyExpanded
                            : ExpandCollapseState.Collapsed
                        : ((i.Statuses.Count <= 7 && i.IconName == FileStatusIcons.Diff) || items.Count < 3 || i == items[0]) && i.Statuses.Count > 0
                            ? ExpandCollapseState.Expanded
                            : ExpandCollapseState.Collapsed;

            if (showDiffGroups)
            {
                Apply(diffGroup, state);
                rootNodes.Add(diffGroup);
            }
            else
            {
                // Add nodes of single group as root nodes
                if (state == ExpandCollapseState.PartiallyExpanded)
                {
                    state = ExpandCollapseState.Collapsed;
                }

                foreach (FileStatusNode node in diffGroup.Children.ToList())
                {
                    diffGroup.Remove(node);
                    Apply(node, state);
                    rootNodes.Add(node);
                }
            }
        }

        return (rootNodes, showDiffGroups, filesPresent);

        (FileStatusNode, int ShownCount) CreateGroup(IEnumerable<GitItemStatus> itemStatuses, FileStatusGroup fileStatusGroup)
        {
            FileStatusNode diffGroup;
            int shownCount = 0;

            GroupBy? groupBy = GetGroupBy(options.SortType);
            if (groupBy is null)
            {
                diffGroup = CreateTreeSortedByPath(itemStatuses, flatList, options.MergeSingleItemsWithFolder, CreateCountedNode);
            }
            else
            {
                diffGroup = new FileStatusNode("", FileStatusIcons.FolderClosed);
                foreach (IGrouping<string, GitItemStatus> group in itemStatuses.GroupBy(groupBy.GetGroupKey).OrderBy(group => group.Key, StringComparer.Ordinal))
                {
                    FileStatusNode groupNode = CreateTreeSortedByPath(group, flatList, options.MergeSingleItemsWithFolder, CreateCountedNode);
                    if (showGroupNodes && groupNode.Children.Count == 1 && groupNode.Children[0].Children.Count == 0)
                    {
                        FileStatusNode single = groupNode.Children[0];
                        groupNode.Remove(single);
                        groupNode = single;
                    }
                    else if (groupNode.Children.Count > 0)
                    {
                        groupNode.Text = groupBy.GetLabel(group);
                        groupNode.IconKey = groupBy.GetImageKey(group);
                        groupNode.FolderPath = null;
                    }

                    if (showGroupNodes)
                    {
                        diffGroup.Add(groupNode);
                    }
                    else
                    {
                        foreach (FileStatusNode node in groupNode.Children.ToList())
                        {
                            groupNode.Remove(node);
                            diffGroup.Add(node);
                        }
                    }
                }

                if (diffGroup.Children.Count == 1 && diffGroup.Children[0].Children.Count > 0)
                {
                    FileStatusNode single = diffGroup.Children[0];
                    diffGroup.Remove(single);
                    diffGroup = single;
                }
            }

            diffGroup.IconKey = fileStatusGroup.IconName;
            diffGroup.Group = fileStatusGroup;
            diffGroup.FolderPath = null;
            diffGroup.Text = GetGroupName(fileStatusGroup, shownCount);

            return (diffGroup, shownCount);

            FileStatusNode CreateCountedNode(GitItemStatus item)
            {
                ++shownCount;
                return CreateNode(item, fileStatusGroup);
            }
        }

        FileStatusNode CreateNode(GitItemStatus item, FileStatusGroup fileStatusGroup)
        {
            string oldName = item.OldName is null ? "" : $" ({item.OldName})";
            return new FileStatusNode($"{item.Name}{oldName}", GetItemImageKey(item, IsGrepItemStatuses(fileStatusGroup)))
            {
                Entry = new FileStatusEntry(fileStatusGroup.FirstRevision, fileStatusGroup.SecondRevision, item, fileStatusGroup.BaseA, fileStatusGroup.BaseB),
            };
        }

        string GetItemImageKey(GitItemStatus gitItemStatus, bool isGitGrep)
        {
            string imageKey;
            if (isGitGrep)
            {
                // The WinForms list shows the icon of the file type (from the shell) once loaded.
                imageKey = gitItemStatus.IsSubmodule ? FileStatusIcons.GetSubmoduleItemImageKey(gitItemStatus) : FileStatusIcons.DefaultFileImage;
            }
            else
            {
                imageKey = gitItemStatus.IsStatusOnly || !string.IsNullOrWhiteSpace(gitItemStatus.ErrorMessage)
                    ? gitItemStatus == noItemStatus
                        ? FileStatusIcons.FileStatusCopiedSame
                        : FileStatusIcons.FileStatusUnknown
                    : FileStatusIcons.GetItemImageKey(gitItemStatus);
            }

            return FileStatusIcons.Keys.Contains(imageKey) ? imageKey : FileStatusIcons.FileStatusUnknown;
        }

        static string GetGroupName(FileStatusGroup i, int shownCount)
        {
            // Show shown and total number of files only if different; avoid showing "1/0" for "- No changes -"
            string shownDisplay = shownCount >= i.Statuses.Count ? "" : $"{shownCount}/";
            return $"({shownDisplay}{i.Statuses.Count}) {i.Summary}";
        }

        static void Apply(FileStatusNode node, ExpandCollapseState state)
        {
            switch (state)
            {
                case ExpandCollapseState.Collapsed:
                    foreach (FileStatusNode subnode in node.Children)
                    {
                        subnode.ExpandAll();
                    }

                    break;

                case ExpandCollapseState.Expanded:
                    node.ExpandAll();
                    break;

                case ExpandCollapseState.PartiallyExpanded:
                    node.IsExpanded = true;
                    break;
            }
        }
    }

    /// <summary>As <c>FileStatusDiffCalculator.IsGrepItemStatuses</c>.</summary>
    public static bool IsGrepItemStatuses(FileStatusGroup group) => group.IconName == FileStatusIcons.GitGrepIconName;

    /// <summary>As <c>FileStatusList.SetupUnifiedDiffListSorting</c>.</summary>
    private static GroupBy? GetGroupBy(DiffListSortType sortType) => sortType switch
    {
        DiffListSortType.FilePath or DiffListSortType.FilePathFlat => null,
        DiffListSortType.FileExtension or DiffListSortType.FileExtensionFlat
            => new GroupBy(status => Path.GetExtension(status.Name), GetImageKey: _ => FileStatusIcons.DefaultFileImage, GetLabel: group => group.Key),
        DiffListSortType.FileStatus or DiffListSortType.FileStatusFlat
            => new GroupBy(GetStatusKey, GetImageKey: group => FileStatusIcons.GetItemImageKey(group.First()), GetLabel: _ => ""),
        _ => throw new NotSupportedException($"{sortType} is not a supported sorting method.")
    };

    private static string GetStatusKey(GitItemStatus status)
    {
        char inverseDiffStatus = (char)((int)'Z' - (int)status.DiffStatus);
        int imageIndex = IndexOf(FileStatusIcons.GetItemImageKey(status));
        return $"{inverseDiffStatus}{imageIndex:D02}";

        static int IndexOf(string key)
        {
            for (int i = 0; i < FileStatusIcons.Keys.Count; i++)
            {
                if (FileStatusIcons.Keys[i] == key)
                {
                    return i;
                }
            }

            return -1;
        }
    }

    /// <summary>As <c>StatusSorter.CreateTreeSortedByPath</c>.</summary>
    internal static FileStatusNode CreateTreeSortedByPath(IEnumerable<GitItemStatus> statuses, bool flat, bool mergeSingleItemsWithFolder, Func<GitItemStatus, FileStatusNode> createNode)
    {
        FileStatusNode root = CreateParent(RelativePath.From(""));

        FileStatusNode parent = root;
        foreach (GitItemStatus status in statuses.OrderBy(s => s, new PathFirstComparer()))
        {
            parent = flat ? root : GetOrCreateParent(parent, status.Path, root);
            parent.Add(createNode(status));
        }

        if (!flat)
        {
            // As root.Items(): the descendants, not the root.
            foreach (FileStatusNode node in root.DescendantsAndSelf().Skip(1).ToList())
            {
                RemoveParentPath(node);
            }
        }

        return root;

        void RemoveParentPath(FileStatusNode node)
        {
            if (mergeSingleItemsWithFolder && node.Children.Count == 1 && node.Children[0].Children.Count == 0 && node.Parent is not null)
            {
                FileStatusNode singleItem = node.Children[0];
                node.Remove(singleItem);
                node.IconKey = singleItem.IconKey;
                node.Entry = singleItem.Entry;
                node.FolderPath = null;
                node.Text = singleItem.Text;
            }

            if (node.Parent?.FolderPath is { } parentPath && parentPath.Length > 0 && node.Text.StartsWith(parentPath.Value))
            {
                node.Text = node.Text[(parentPath.Length + 1)..];
            }
        }
    }

    private static FileStatusNode CreateParent(RelativePath relativePath)
        => new(relativePath.Value, FileStatusIcons.FolderClosed) { FolderPath = relativePath };

    private static RelativePath GetCommonPath(RelativePath relativePathA, RelativePath relativePathB)
    {
        string a = $"{relativePathA}/";
        string b = $"{relativePathB}/";
        for (int commonEnd = 0; ; ++commonEnd)
        {
            if (commonEnd >= a.Length || commonEnd >= b.Length || a[commonEnd] != b[commonEnd])
            {
                // Revert possible partial match
                while (commonEnd > 0 && a[--commonEnd] != '/')
                {
                }

                return RelativePath.From(a[..commonEnd]);
            }
        }
    }

    private static FileStatusNode GetOrCreateParent(FileStatusNode previousParent, RelativePath currentPath, FileStatusNode root)
    {
        RelativePath previousPath = previousParent.FolderPath!;
        if (previousPath == currentPath)
        {
            return previousParent;
        }

        RelativePath commonPath = GetCommonPath(previousPath, currentPath);
        FileStatusNode commonParent = GetOrCreateCommonParent();
        if (currentPath == commonPath)
        {
            return commonParent;
        }

        FileStatusNode parent = CreateParent(currentPath);
        commonParent.Add(parent);
        return parent;

        FileStatusNode GetOrCreateCommonParent()
        {
            if (commonPath.Length == 0)
            {
                return root;
            }

            FileStatusNode splitCandidate = previousParent;
            RelativePath splitCandidatePath = previousPath;
            while (splitCandidate.Parent?.FolderPath is { } path && path.Value.StartsWith(commonPath.Value))
            {
                splitCandidate = splitCandidate.Parent;
                splitCandidatePath = path;
            }

            return splitCandidatePath == commonPath
                ? splitCandidate
                : Split(splitCandidate, commonPath);
        }
    }

    private static FileStatusNode Split(FileStatusNode subNode, RelativePath commonPath)
    {
        FileStatusNode parentNode = subNode.Parent ?? throw new ArgumentNullException($"{nameof(subNode)}.{nameof(subNode.Parent)}");
        int index = parentNode.Children.IndexOf(subNode);
        parentNode.Remove(subNode);
        FileStatusNode commonFolderNode = CreateParent(commonPath);
        commonFolderNode.Add(subNode);
        parentNode.Insert(index, commonFolderNode);
        return commonFolderNode;
    }

    private enum ExpandCollapseState
    {
        Collapsed,
        Expanded,
        PartiallyExpanded,
    }

    private sealed record GroupBy(
        Func<GitItemStatus, string> GetGroupKey,
        Func<IGrouping<string, GitItemStatus>, string> GetImageKey,
        Func<IGrouping<string, GitItemStatus>, string> GetLabel);

    /// <summary>As <c>StatusSorter.PathFirstComparer</c>.</summary>
    private sealed class PathFirstComparer : IComparer<GitItemStatus>
    {
        private readonly Dictionary<(string Left, string Right), int> _pathComparisons = [];

        public int Compare(GitItemStatus? l, GitItemStatus? r)
            => (l, r) switch
            {
                (null, null) => 0,
                (_, null) => -1,
                (null, _) => 1,
                _ => CompareNonNull(l, r)
            };

        private int CompareNonNull(GitItemStatus l, GitItemStatus r)
        {
            // Files often share a directory. Reuse its comparison instead of repeatedly comparing every component.
            int pathComparison = 0;
            if (l.Path.Value != r.Path.Value && !_pathComparisons.TryGetValue((l.Path.Value, r.Path.Value), out pathComparison))
            {
                pathComparison = ComparePaths(l.Path, r.Path);
                _pathComparisons.Add((l.Path.Value, r.Path.Value), pathComparison);
            }

            return pathComparison == 0 ? StringComparer.InvariantCulture.Compare(l.Name, r.Name) : pathComparison;
        }

        private static int ComparePaths(RelativePath l, RelativePath r)
        {
            int pathComparison = (l.Value, r.Value) switch
            {
                ("", "") => 0,
                (_, "") => -1,
                ("", _) => 1,
                _ => ComparePath(l.Value.AsSpan(), r.Value.AsSpan())
            };

            return pathComparison switch
            {
                -1 => StartsWith(r, l) ? 1 : -1,
                1 => StartsWith(l, r) ? -1 : 1,
                _ => 0
            };

            static int ComparePath(ReadOnlySpan<char> l, ReadOnlySpan<char> r)
            {
                if (l.IsEmpty || r.IsEmpty)
                {
                    return l.IsEmpty && r.IsEmpty ? 0 : l.IsEmpty ? -1 : 1;
                }

                Split(l, out ReadOnlySpan<char> topL, out ReadOnlySpan<char> subL);
                Split(r, out ReadOnlySpan<char> topR, out ReadOnlySpan<char> subR);
                return topL.CompareTo(topR, StringComparison.InvariantCulture) switch
                {
                    -1 => -1,
                    +1 => +1,
                    _ => ComparePath(subL, subR)
                };

                static void Split(ReadOnlySpan<char> path, out ReadOnlySpan<char> top, out ReadOnlySpan<char> sub)
                {
                    int separatorIndex = path.IndexOf('/');
                    if (separatorIndex == -1)
                    {
                        top = path;
                        sub = ReadOnlySpan<char>.Empty;
                        return;
                    }

                    top = path[..separatorIndex];
                    sub = path[(separatorIndex + 1)..];
                }
            }

            static bool StartsWith(RelativePath longPath, RelativePath shortPath)
            {
                return longPath.Value.StartsWith(shortPath.Value, StringComparison.InvariantCulture)
                    && longPath.Value[shortPath.Length] == '/';
            }
        }
    }
}

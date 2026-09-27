using GitExtensions.Extensibility.Git;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.ViewModels;

/// <summary>View model tests of the merge conflicts dialog (port of <c>FormResolveConflicts</c>).</summary>
[TestFixture]
public sealed class ResolveConflictsViewModelTests
{
    private static readonly ObjectId _base = ObjectId.Parse("a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1a1");
    private static readonly ObjectId _local = ObjectId.Parse("b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2b2");
    private static readonly ObjectId _remote = ObjectId.Parse("c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3c3");

    [Test]
    public void Lists_the_conflicts_and_describes_the_selected_one_during_a_merge()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("src/b.txt"), Conflict("a.txt"), Conflict("new.txt", hasBase: false)] };
        ResolveConflictsViewModel viewModel = Create(host);

        viewModel.InitializeView();

        viewModel.Conflicts.Select(c => c.Filename).Should().Equal("a.txt", "new.txt", "src/b.txt");
        viewModel.SelectedConflict.Should().BeSameAs(viewModel.Conflicts[0]);
        viewModel.SelectedConflicts.Should().Equal(viewModel.Conflicts[0]);
        viewModel.OpenMergeToolButtonText.Should().Be("Open in kdiff3");
        viewModel.OpenMergeToolMenuText.Should().Be("Open in kdiff3");
        viewModel.ChooseLocalText.Should().Be("Choose local/current (ours)");
        viewModel.ChooseLocalToolTip.Should().Be("Take only the changes from the current branch");
        viewModel.ChooseRemoteText.Should().Be("Choose remote/incoming (theirs)");
        viewModel.LocalLabel.Should().Be("Local/current (ours)");
        viewModel.LocalLabelToolTip.Should().Be("Changes from the current branch");
        viewModel.RemoteLabel.Should().Be("Remote/incoming (theirs)");
        viewModel.RemoteLabelToolTip.Should().Be("Changes from the branch you are merging");
        viewModel.ConflictDescription.Should().Be("The file has been changed both locally (ours) and remotely (theirs). Merge the changes.");
        viewModel.BaseFileName.Should().Be("a.txt");
        viewModel.LocalFileName.Should().Be("a.txt");
        viewModel.RemoteFileName.Should().Be("a.txt");
        viewModel.IsSingleFileSelected.Should().BeTrue();
        viewModel.CanOpenMergeTool.Should().BeTrue();
        viewModel.CanOpenBase.Should().BeTrue();

        viewModel.SelectedConflicts = [viewModel.Conflicts[1]];

        viewModel.ConflictDescription.Should().Be("A file with the same name has been created locally (ours) and remotely (theirs). Choose the file you want to keep or merge the files.");
        viewModel.BaseFileName.Should().Be("no base");
        viewModel.CanOpenBase.Should().BeFalse("there is no base to open or save");
        viewModel.CanOpenLocal.Should().BeTrue();
        host.Calls.Should().BeEmpty();
    }

    [Test]
    public void Swaps_ours_and_theirs_during_a_rebase()
    {
        FakeResolveConflictsHost host = new() { IsRebasing = true, Conflicts = [Conflict("a.txt", hasLocal: false)] };
        ResolveConflictsViewModel viewModel = Create(host);

        viewModel.InitializeView();

        viewModel.ChooseLocalText.Should().Be("Choose local/current (theirs)");
        viewModel.ChooseLocalToolTip.Should().Be("Take only the changes from the branch you are rebasing onto");
        viewModel.ChooseRemoteText.Should().Be("Choose remote/incoming (ours)");
        viewModel.ChooseRemoteToolTip.Should().Be("Take only the changes from the branch you are rebasing");
        viewModel.LocalLabel.Should().Be("Local/current (theirs)");
        viewModel.RemoteLabel.Should().Be("Remote/incoming (ours)");
        viewModel.ConflictDescription.Should().Be("The file has been deleted locally (theirs) and modified remotely (ours). Choose to delete the file or keep the modified version.");
        viewModel.LocalFileName.Should().Be("deleted");
        viewModel.CanOpenLocal.Should().BeFalse();
    }

    [TestCase(false, "ours", "theirs")]
    [TestCase(true, "theirs", "ours")]
    public void A_file_deleted_on_both_sides_has_its_own_description(bool isRebasing, string localSide, string remoteSide)
    {
        FakeResolveConflictsHost host = new()
        {
            IsRebasing = isRebasing,
            Conflicts = [Conflict("a.txt"), Conflict("deleted.txt", hasLocal: false, hasRemote: false)]
        };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.SelectedConflicts = [viewModel.Conflicts[1]];

        viewModel.ConflictDescription.Should().Be($"The file has been deleted both locally ({localSide}) and remotely ({remoteSide}).");
        viewModel.BaseFileName.Should().Be("deleted.txt");
        viewModel.LocalFileName.Should().Be("deleted");
        viewModel.RemoteFileName.Should().Be("deleted");
    }

    [TestCase(true, false)]
    [TestCase(false, true)]
    public void An_unhandled_conflict_clears_the_previous_description(bool hasLocal, bool hasRemote)
    {
        FakeResolveConflictsHost host = new()
        {
            Conflicts = [Conflict("a.txt"), Conflict("other.txt", hasBase: false, hasLocal: hasLocal, hasRemote: hasRemote)]
        };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();
        viewModel.ConflictDescription.Should().NotBeEmpty();

        viewModel.SelectedConflicts = [viewModel.Conflicts[1]];

        viewModel.ConflictDescription.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Merge_names_files_deleted_on_both_sides_even_after_other_conflicts(bool mixedSelection)
    {
        FakeResolveConflictsHost host = new()
        {
            Conflicts = [Conflict("deleted.txt", hasLocal: false, hasRemote: false)]
        };
        if (mixedSelection)
        {
            host.Conflicts.Insert(0, Conflict("a.txt", hasLocal: false));
        }

        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();
        viewModel.SelectedConflicts = [.. viewModel.Conflicts];

        await viewModel.MergeCommand.ExecuteAsync(null);

        host.Questions.Select(question => question.Heading).Should().Equal(mixedSelection ? ["a.txt", "deleted.txt"] : ["deleted.txt"]);
        host.Questions.Last().Text.Should().BeEmpty("this conflict is not counted among the modified/deleted conflicts");
        host.Questions.Last().ApplyToAllText.Should().BeEmpty();
        host.Calls.Should().NotContain(call => call.StartsWith("choose") || call.StartsWith("remove") || call.StartsWith("stage"),
            "cancelling the prompts must not change the files");
    }

    [Test]
    public void A_submodule_shows_the_commits_of_the_sides()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("sub", hasRemote: false)], Submodules = { "sub" } };
        ResolveConflictsViewModel viewModel = Create(host);

        viewModel.InitializeView();

        viewModel.BaseFileName.Should().Be("sub@a1a1a1a1");
        viewModel.LocalFileName.Should().Be("sub@b2b2b2b2");
        viewModel.RemoteFileName.Should().Be("deleted@deleted");
        viewModel.ConflictDescription.Should().Be("The file has been modified locally (ours) and deleted remotely (theirs). Choose to delete the file or keep the modified version.");
    }

    [Test]
    public void Several_selected_files_disable_the_commands_on_one_file()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt"), Conflict("b.txt")] };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.SelectedConflicts = [.. viewModel.Conflicts];

        viewModel.HasSelection.Should().BeTrue();
        viewModel.IsSingleFileSelected.Should().BeFalse();
        viewModel.CanOpenMergeTool.Should().BeFalse();
        viewModel.CanOpenLocal.Should().BeFalse();
        viewModel.CanOpenRemote.Should().BeFalse();
        viewModel.CanOpenBase.Should().BeFalse();
        viewModel.BaseFileName.Should().BeEmpty();
        viewModel.LocalFileName.Should().BeEmpty();
        viewModel.RemoteFileName.Should().BeEmpty();

        viewModel.SelectedConflicts = [];
        viewModel.HasSelection.Should().BeFalse("the menu does not open without a selected file");
    }

    [Test]
    public void Reports_a_missing_or_misconfigured_merge_tool()
    {
        ScriptedMessageBoxes messageBoxes = new();
        Create(new FakeResolveConflictsHost { MergeTool = null }, messageBoxes).InitializeView();
        messageBoxes.Errors.Should().Equal("There is no mergetool configured." + Environment.NewLine + "Please go to settings and set a mergetool!");

        messageBoxes = new();
        ResolveConflictsViewModel viewModel = Create(
            new FakeResolveConflictsHost { MergeTool = "meld", Settings = { ["mergetool.meld.path"] = "missing-meld" }, Conflicts = [Conflict("a.txt")] },
            messageBoxes);
        viewModel.InitializeView();
        messageBoxes.Warnings.Should().Equal("The mergetool is not correctly configured." + Environment.NewLine + "Please go to settings and configure the mergetool!");
        viewModel.CanOpenMergeTool.Should().BeTrue("the path is set, even if not found");
    }

    [Test]
    public void The_merge_tool_path_is_taken_from_the_command_on_Windows()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows only");
        }

        FakeResolveConflictsHost host = new()
        {
            MergeTool = "p4merge",
            Settings = { ["mergetool.p4merge.cmd"] = "\"C:/Tools/p4merge.exe\" \"$BASE\" \"$LOCAL\" \"$REMOTE\" \"$MERGED\"" },
            Conflicts = [Conflict("a.txt")],
            ExitCode = 0,
            ModifyOnMerge = true,
        };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.MergeCommand.Execute(null);

        host.Calls.Should().Contain("run C:/Tools/p4merge.exe: \"base/a.txt\" \"local/a.txt\" \"remote/a.txt\" \"a.txt\"");
    }

    [Test]
    [Platform(Exclude = "Win")]
    public void The_merge_tool_path_is_taken_from_the_command_off_Windows()
    {
        FakeResolveConflictsHost host = new()
        {
            MergeTool = "meld",
            Settings =
            {
                ["mergetool.meld.path"] = "/usr/bin/meld",
                ["mergetool.meld.cmd"] = "\"/usr/bin/meld\" \"$LOCAL\" \"$BASE\" \"$REMOTE\" --output \"$MERGED\"",
            },
            Conflicts = [Conflict("a.txt")],
            ExitCode = 0,
            ModifyOnMerge = true,
        };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.MergeCommand.Execute(null);

        // The path that Git Extensions writes at the start of the command is the program, not an argument.
        host.Calls.Should().Contain("run /usr/bin/meld:\"local/a.txt\" \"base/a.txt\" \"remote/a.txt\" --output \"a.txt\"");
    }

    [TestCase(null, "\"/usr/bin/opendiff\" \"$LOCAL\" \"$REMOTE\" -ancestor \"$BASE\" -merge \"$MERGED\" | cat",
        "/usr/bin/opendiff", "\"$LOCAL\" \"$REMOTE\" -ancestor \"$BASE\" -merge \"$MERGED\"")]
    [TestCase("/usr/bin/meld", "\"/usr/bin/meld\" \"$LOCAL\" --output \"$MERGED\"", "/usr/bin/meld", "\"$LOCAL\" --output \"$MERGED\"")]
    [TestCase("/opt/tool", "\"$LOCAL\" \"$MERGED\"", "/opt/tool", "\"$LOCAL\" \"$MERGED\"")]
    [TestCase("/opt/tool", "\"/usr/bin/other\" \"$MERGED\"", "/opt/tool", "\"/usr/bin/other\" \"$MERGED\"")]
    [TestCase(null, "meld \"$MERGED\"", null, "meld \"$MERGED\"")]
    public void The_command_of_a_merge_tool_is_split_off_Windows(string? path, string command, string? expectedPath, string expectedCommand)
    {
        ResolveConflictsViewModel.SplitUnixMergeToolCommand(path, command).Should().Be((expectedPath, expectedCommand));
    }

    [Test]
    public async Task Merge_runs_the_merge_tool_and_stages_the_merged_file()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], ExitCode = 0, ModifyOnMerge = true };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        host.Calls.Should().Equal(
            "checkout a.txt",
            "run kdiff3-full:\"base/a.txt\" \"local/a.txt\" \"remote/a.txt\" -o \"a.txt\"",
            "stage a.txt",
            "delete base/a.txt",
            "delete local/a.txt",
            "delete remote/a.txt",
            "update submodules",
            "confirm commit");
        viewModel.IsBusy.Should().BeFalse();
        viewModel.IsProgressVisible.Should().BeFalse();
    }

    [Test]
    public async Task Merge_asks_whether_the_conflict_is_solved_when_the_file_is_unchanged()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], ExitCode = 0, ModifyOnMerge = false };
        ScriptedMessageBoxes messageBoxes = new() { Answers = { true } };
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        messageBoxes.Questions.Should().Equal("Is the merge conflict solved?");
        host.Calls.Should().Contain("stage a.txt");
    }

    [Test]
    public async Task Merge_without_a_base_offers_a_two_way_merge()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt", hasBase: false)], ExitCode = 1, ModifyOnMerge = false };
        ScriptedMessageBoxes messageBoxes = new() { Answers = { true } };
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        messageBoxes.Questions.Should().Equal("There is no base revision for 'a.txt'." + Environment.NewLine + "Fall back to 2-way merge?");
        host.Calls.Should().Contain("run kdiff3-full: \"local/a.txt\" \"remote/a.txt\" -o \"a.txt\"")
            .And.NotContain("stage a.txt", "the merge tool failed and the file is unchanged");

        // Cancel: the merge tool is not run.
        host.Calls.Clear();
        messageBoxes.Answers.Add(null);
        await viewModel.MergeCommand.ExecuteAsync(null);
        host.Calls.Should().NotContain(call => call.StartsWith("run"));
    }

    [Test]
    public async Task Merge_reports_a_merge_tool_that_does_not_start()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], ExitCode = null };
        ScriptedMessageBoxes messageBoxes = new();
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        messageBoxes.Errors.Should().Equal("Error starting mergetool: kdiff3-full");
        host.Calls.Should().NotContain("stage a.txt");
    }

    [Test]
    public async Task Merge_without_a_merge_tool_command_runs_git_mergetool()
    {
        FakeResolveConflictsHost host = new() { MergeTool = "vimdiff", Conflicts = [Conflict("a.txt")] };
        host.FullPaths["vimdiff"] = null;
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.CanOpenMergeTool.Should().BeFalse("neither the path nor the command of the merge tool is set");
        await viewModel.MergeCommand.ExecuteAsync(null);

        host.Calls.Should().Contain("mergetool a.txt ").And.NotContain("stage a.txt");
    }

    [Test]
    public async Task Merge_of_a_binary_file_offers_to_choose_a_side()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("image.png")], BinaryFiles = { "image.png" } };
        host.Answers.Enqueue(new SolveConflictAnswer(ConflictResolutionChoice.KeepRemote, ApplyToAll: false));
        ScriptedMessageBoxes messageBoxes = new() { Answers = { false } };
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        messageBoxes.Questions.Should().Equal("The selected file appears to be a binary file." + Environment.NewLine + "Are you sure you want to open this file in kdiff3?");
        host.Questions.Should().ContainSingle().Which.Should().Be(new SolveConflictQuestion(
            "File 'image.png' appears to be binary." + Environment.NewLine + "Choose to keep the local 'ours', remote 'theirs' or base file.",
            "image.png",
            "Solve merge conflict",
            "",
            "Choose local (ours)",
            "Choose remote (theirs)",
            "Keep base file"));
        host.Calls.Should().Contain("choose image.png Remote").And.NotContain(call => call.StartsWith("run"));
    }

    [Test]
    public async Task Merge_of_a_submodule_opens_the_submodule_dialog()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("sub")], Submodules = { "sub" }, MergeSubmoduleResult = true };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        host.Calls.Should().ContainInOrder("merge submodule sub", "stage sub").And.NotContain(call => call.StartsWith("checkout"));
    }

    [Test]
    public async Task Merge_uses_the_custom_merge_script_of_the_file_type()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("doc.docx")], MergeScripts = { "merge-doc.js" }, ModifyOnMergeScript = true };
        ScriptedMessageBoxes messageBoxes = new() { Answers = { true, true } };
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        await viewModel.MergeCommand.ExecuteAsync(null);

        messageBoxes.Questions.Should().Equal(
            "There is a custom merge script (merge-doc.js) for this file type." + Environment.NewLine + Environment.NewLine + "Do you want to use this custom merge script?",
            "The merge conflict need to be solved and the result must be saved as:" + Environment.NewLine + "C:\\repo\\doc.docx" + Environment.NewLine + Environment.NewLine + "Is the merge conflict solved?");
        host.Calls.Should().ContainInOrder("script C:/scripts/merge-doc.js C:\\repo\\doc.docx remote/doc.docx local/doc.docx base/doc.docx", "stage doc.docx")
            .And.NotContain(call => call.StartsWith("run"));
    }

    [Test]
    public void ChooseLocal_resolves_the_selected_files_and_offers_to_commit_when_done()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt"), Conflict("b.txt")], ConfirmCommitResult = true };
        ResolveConflictsViewModel viewModel = Create(host);
        bool? closed = null;
        viewModel.CloseRequested += (_, accepted) => closed = accepted;
        viewModel.InitializeView();
        viewModel.SelectedConflicts = [.. viewModel.Conflicts];

        viewModel.ChooseLocalCommand.Execute(null);

        host.Calls.Should().Equal("choose a.txt Local", "choose b.txt Local", "update submodules", "confirm commit", "commit");
        closed.Should().BeTrue();
        viewModel.IsProgressVisible.Should().BeFalse();
    }

    [Test]
    public void The_commit_is_not_offered_during_a_rebase_or_when_not_asked_to()
    {
        FakeResolveConflictsHost host = new() { IsRebasing = true, Conflicts = [Conflict("a.txt")] };
        ResolveConflictsViewModel viewModel = Create(host);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        viewModel.InitializeView();

        viewModel.ChooseRemoteCommand.Execute(null);

        host.Calls.Should().Equal("choose a.txt Remote", "update submodules");
        closed.Should().BeTrue();

        host = new() { Conflicts = [Conflict("a.txt")] };
        viewModel = Create(host, offerCommit: false);
        viewModel.InitializeView();
        viewModel.ChooseBaseCommand.Execute(null);
        host.Calls.Should().Equal("choose a.txt Base", "update submodules");
    }

    [Test]
    public void The_dialog_stays_open_without_conflicts_when_there_were_none()
    {
        FakeResolveConflictsHost host = new();
        ResolveConflictsViewModel viewModel = Create(host);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.InitializeView();

        viewModel.Conflicts.Should().BeEmpty();
        viewModel.HasSelection.Should().BeFalse();
        closed.Should().BeFalse();
        host.Calls.Should().BeEmpty();
    }

    [Test]
    public void Choose_failures_are_reported()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], ChooseSucceeds = false };
        ScriptedMessageBoxes messageBoxes = new();
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        viewModel.ChooseLocalCommand.Execute(null);
        viewModel.ChooseRemoteCommand.Execute(null);
        viewModel.ChooseBaseCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Choose local file failed.", "Choose remote file failed.", "Choose base file failed.");
    }

    [Test]
    public void ChooseBase_without_a_base_asks_to_keep_a_side_or_delete_the_file()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt", hasBase: false)] };
        host.Answers.Enqueue(new SolveConflictAnswer(ConflictResolutionChoice.KeepBase, ApplyToAll: false));
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.ChooseBaseCommand.Execute(null);

        host.Questions.Should().ContainSingle().Which.Should().Be(new SolveConflictQuestion(
            "File 'a.txt' does not have a base revision." + Environment.NewLine
                + "A file with the same name has been created locally (ours) and remotely (theirs) causing this conflict." + Environment.NewLine + Environment.NewLine
                + "Choose the file you want to keep, merge the files or delete the file?",
            "a.txt",
            "Solve merge conflict",
            "",
            "Choose local (ours)",
            "Choose remote (theirs)",
            "Delete file"));
        host.Calls.Should().StartWith("remove a.txt");
    }

    [Test]
    public void ChooseLocal_of_files_deleted_locally_asks_once_with_apply_to_all()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt", hasLocal: false), Conflict("b.txt", hasLocal: false), Conflict("c.txt", hasLocal: false)] };
        host.Answers.Enqueue(new SolveConflictAnswer(ConflictResolutionChoice.KeepRemote, ApplyToAll: true));
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();
        viewModel.SelectedConflicts = [.. viewModel.Conflicts];

        viewModel.ChooseLocalCommand.Execute(null);

        host.Questions.Should().ContainSingle().Which.Should().Be(new SolveConflictQuestion(
            "'a.txt' and 2 other selected file(s) do not have a local revision." + Environment.NewLine
                + "The files have been deleted locally, but modified remotely" + Environment.NewLine + Environment.NewLine
                + "Choose to delete the files or keep the modified versions.",
            "a.txt",
            "Solve merge conflict",
            "Apply to 'a.txt' and 2 other file(s)",
            "Delete file (ours)",
            "Keep modified (theirs)",
            "Keep base file (ours)"));
        host.Calls.Should().ContainInOrder("choose a.txt Remote", "choose b.txt Remote", "choose c.txt Remote");
    }

    [Test]
    public void ChooseRemote_of_a_file_deleted_remotely_asks_and_a_cancel_does_nothing()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt", hasRemote: false), Conflict("b.txt")] };
        host.Answers.Enqueue(new SolveConflictAnswer(ConflictResolutionChoice.KeepRemote, ApplyToAll: false));
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.ChooseRemoteCommand.Execute(null);

        host.Questions.Should().ContainSingle().Which.Text.Should().Be(
            "File 'a.txt' does not have a remote revision." + Environment.NewLine
            + "The file has been modified locally (ours) but deleted remotely (theirs)." + Environment.NewLine + Environment.NewLine
            + "Choose to delete the file or keep the modified version.");
        host.Questions[0].Heading.Should().Be("a.txt");
        host.Questions[0].KeepLocalText.Should().Be("Keep modified (ours)");
        host.Questions[0].KeepRemoteText.Should().Be("Delete file (theirs)");
        host.Calls.Should().StartWith("remove a.txt");

        // A cancelled question does not repeat the previous answer.
        host.Conflicts = [Conflict("a.txt", hasRemote: false)];
        viewModel.RescanCommand.Execute(null);
        host.Calls.Clear();
        host.Answers.Enqueue(new SolveConflictAnswer(ConflictResolutionChoice.None, ApplyToAll: false));
        viewModel.ChooseRemoteCommand.Execute(null);
        host.Calls.Should().BeEmpty();
    }

    [Test]
    public void MarkAsSolved_stages_the_selected_files()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt"), Conflict("b.txt"), Conflict("c.txt")] };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();
        viewModel.SelectedConflicts = [viewModel.Conflicts[0], viewModel.Conflicts[2]];

        viewModel.MarkAsSolvedCommand.Execute(null);

        host.Calls.Should().Equal("stage a.txt", "stage c.txt");
        viewModel.Conflicts.Select(c => c.Filename).Should().Equal("b.txt");
        viewModel.SelectedConflicts.Should().Equal(viewModel.Conflicts[0]);
    }

    [Test]
    public void Rescan_keeps_the_selected_row_or_selects_the_new_last_one()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt"), Conflict("b.txt"), Conflict("c.txt")] };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.SelectedConflicts = [viewModel.Conflicts[1]];
        viewModel.RescanCommand.Execute(null);
        viewModel.SelectedConflict!.Filename.Should().Be("b.txt");

        viewModel.SelectedConflicts = [viewModel.Conflicts[2]];
        host.Conflicts = [Conflict("a.txt"), Conflict("b.txt")];
        viewModel.RescanCommand.Execute(null);
        viewModel.SelectedConflict!.Filename.Should().Be("b.txt", "the last row stays selected");
    }

    [Test]
    public void Reset_asks_twice_then_resets_and_closes()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")] };
        ScriptedMessageBoxes messageBoxes = new() { Answers = { false } };
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;
        viewModel.InitializeView();

        viewModel.ResetCommand.Execute(null);
        host.Calls.Should().BeEmpty();
        closed.Should().BeFalse();

        viewModel.ResetCommand.Execute(null);
        messageBoxes.Questions.Should().Equal(Enumerable.Repeat(
            "You can abort the current conflict resolution by resetting hard." + Environment.NewLine + "All changes since the last commit will be deleted." + Environment.NewLine
            + Environment.NewLine + "Do you want to reset the changes?", 2));
        host.Calls.Should().Equal("confirm delete all", "reset hard");
        closed.Should().BeTrue();
    }

    [Test]
    public async Task StartMergeTool_and_the_custom_merge_tools_run_git_mergetool()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], CustomMergeTools = ["kdiff3", "meld"] };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.CustomMergeTools.Should().Equal("kdiff3", "meld");
        viewModel.HasCustomMergeTools.Should().BeTrue();

        await viewModel.StartMergeToolCommand.ExecuteAsync(null);
        await viewModel.CustomMergeToolCommand.ExecuteAsync("meld");
        await viewModel.CustomMergeToolCommand.ExecuteAsync(null);

        host.Calls.Should().Equal("mergetool  ", "mergetool a.txt meld", "mergetool a.txt ");
    }

    [Test]
    public void The_file_commands_act_on_the_selected_file()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("dir/a.txt")], SaveFile = "C:/saved/a.txt" };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.OpenLocalWithCommand.Execute(null);
        viewModel.SaveRemoteAsCommand.Execute(null);
        viewModel.SaveBaseAsCommand.Execute(null);
        viewModel.OpenCommand.Execute(null);
        viewModel.OpenWithCommand.Execute(null);
        viewModel.ShowInFolderCommand.Execute(null);
        viewModel.FileHistoryCommand.Execute(null);
        viewModel.OpenHelpCommand.Execute(null);

        host.Calls.Should().Equal(
            "save dir/a.txt Local to C:/temp/a.txt",
            "open with C:/temp/a.txt",
            "choose save file dir/a.txt",
            "save dir/a.txt Remote to C:/saved/a.txt",
            "choose save file dir/a.txt",
            "save dir/a.txt Base to C:/saved/a.txt",
            "open C:\\repo\\dir/a.txt",
            "open with C:\\repo\\dir/a.txt",
            "show in folder C:\\repo\\dir/a.txt",
            "history dir/a.txt",
            "url https://manual/handle-merge-conflicts");
    }

    [Test]
    public void Save_failures_are_reported()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt")], SaveFile = "C:/saved/a.txt", SaveSucceeds = false };
        ScriptedMessageBoxes messageBoxes = new();
        ResolveConflictsViewModel viewModel = Create(host, messageBoxes);
        viewModel.InitializeView();

        viewModel.OpenBaseWithCommand.Execute(null);
        viewModel.SaveLocalAsCommand.Execute(null);

        messageBoxes.Errors.Should().Equal("Open temporary file failed.", "Save file failed.");
        host.Calls.Should().Contain("open with C:/temp/a.txt", "the temporary file is opened anyway, as in FormResolveConflicts");
    }

    [Test]
    public void Hotkeys_execute_the_commands()
    {
        FakeResolveConflictsHost host = new() { Conflicts = [Conflict("a.txt"), Conflict("b.txt"), Conflict("c.txt")], ExitCode = 0, ModifyOnMerge = true };
        ResolveConflictsViewModel viewModel = Create(host);
        viewModel.InitializeView();

        viewModel.ExecuteHotkeyCommand((int)ResolveConflictsHotkeyCommand.ChooseLocal).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand((int)ResolveConflictsHotkeyCommand.ChooseRemote).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand((int)ResolveConflictsHotkeyCommand.Merge).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand((int)ResolveConflictsHotkeyCommand.Rescan).Should().BeTrue();
        viewModel.ExecuteHotkeyCommand(99).Should().BeFalse();

        host.Calls.Should().ContainInOrder("choose a.txt Local", "choose b.txt Remote", "checkout c.txt", "stage c.txt");
    }

    internal static ConflictData Conflict(string fileName, bool hasBase = true, bool hasLocal = true, bool hasRemote = true)
        => new(
            hasBase ? new ConflictedFileData(_base, fileName) : default,
            hasLocal ? new ConflictedFileData(_local, fileName) : default,
            hasRemote ? new ConflictedFileData(_remote, fileName) : default);

    internal static ResolveConflictsViewModel Create(FakeResolveConflictsHost host, IMessageBoxService? messageBoxes = null, bool offerCommit = true)
        => new(new ResolveConflictsStrings(), host, messageBoxes ?? new ScriptedMessageBoxes(), offerCommit, () => "https://manual/handle-merge-conflicts", "Error", "Warning");

    /// <summary>Message boxes answering the questions in order (yes when there is no answer left).</summary>
    internal sealed class ScriptedMessageBoxes : IMessageBoxService
    {
        public List<bool?> Answers { get; } = [];

        public List<string> Questions { get; } = [];

        public List<string> Errors { get; } = [];

        public List<string> Warnings { get; } = [];

        public List<string> Informations { get; } = [];

        public void ShowError(string text, string caption) => Errors.Add(text);

        public void ShowInformation(string text, string caption) => Informations.Add(text);

        public void ShowWarning(string text, string caption) => Warnings.Add(text);

        public bool Confirm(string text, string caption, bool defaultNo = false) => ConfirmWithCancel(text, caption) == true;

        public bool? ConfirmWithCancel(string text, string caption)
        {
            Questions.Add(text);
            if (Answers.Count == 0)
            {
                return true;
            }

            bool? answer = Answers[0];
            Answers.RemoveAt(0);
            return answer;
        }
    }

    /// <summary>A repository with conflicts: choosing a side, removing or staging a file resolves it.</summary>
    internal sealed class FakeResolveConflictsHost : IResolveConflictsHost
    {
        private DateTime _time = new(2026, 9, 1);

        public List<string> Calls { get; } = [];

        public List<ConflictData> Conflicts { get; set; } = [];

        public bool IsRebasing { get; set; }

        public string? MergeTool { get; set; } = "kdiff3";

        public Dictionary<string, string?> Settings { get; } = [];

        public Dictionary<string, string?> FullPaths { get; } = new() { ["kdiff3"] = "kdiff3-full" };

        public IReadOnlyList<string> CustomMergeTools { get; set; } = [];

        public HashSet<string> Submodules { get; } = [];

        public HashSet<string> BinaryFiles { get; } = [];

        public HashSet<string> MergeScripts { get; } = [];

        public bool ChooseSucceeds { get; set; } = true;

        public bool SaveSucceeds { get; set; } = true;

        public bool MergeSubmoduleResult { get; set; }

        public int? ExitCode { get; set; } = 0;

        public bool ModifyOnMerge { get; set; }

        public bool ModifyOnMergeScript { get; set; }

        public bool ConfirmCommitResult { get; set; }

        public string? SaveFile { get; set; }

        public Queue<SolveConflictAnswer> Answers { get; } = [];

        public List<SolveConflictQuestion> Questions { get; } = [];

        public bool InTheMiddleOfRebase() => IsRebasing;

        public bool InTheMiddleOfConflictedMerge() => Conflicts.Count > 0;

        public bool InTheMiddleOfPatch() => false;

        public IReadOnlyList<ConflictData> GetConflicts() => [.. Conflicts];

        public string? GetMergeTool() => MergeTool;

        public string? GetEffectiveSetting(string name) => Settings.GetValueOrDefault(name);

        public string? FindFullPath(string? path) => path is not null && FullPaths.TryGetValue(path, out string? fullPath) ? fullPath : path is null || path.StartsWith("missing") ? null : path;

        public Task<IReadOnlyList<string>> GetCustomMergeToolsAsync(CancellationToken cancellationToken) => Task.FromResult(CustomMergeTools);

        public ConflictItemType GetItemType(string fileName) => Submodules.Contains(fileName) ? ConflictItemType.Submodule : ConflictItemType.File;

        public bool ChooseSide(string fileName, ConflictSide side)
        {
            Calls.Add($"choose {fileName} {side}");
            if (ChooseSucceeds)
            {
                Resolve(fileName);
            }

            return ChooseSucceeds;
        }

        public void RemoveFile(string fileName)
        {
            Calls.Add($"remove {fileName}");
            Resolve(fileName);
        }

        public void StageFile(string fileName, string errorTitle)
        {
            Calls.Add($"stage {fileName}");
            Resolve(fileName);
        }

        public Task RunMergeToolAsync(string? fileName, string? customTool)
        {
            Calls.Add($"mergetool {fileName} {customTool}");
            return Task.CompletedTask;
        }

        public bool MergeSubmodule(string fileName)
        {
            Calls.Add($"merge submodule {fileName}");
            return MergeSubmoduleResult;
        }

        public (string? BaseFile, string? LocalFile, string? RemoteFile) CheckoutConflictedFiles(ConflictData conflict)
        {
            Calls.Add($"checkout {conflict.Filename}");
            return (conflict.Base.Filename is null ? null : $"base/{conflict.Filename}", $"local/{conflict.Filename}", $"remote/{conflict.Filename}");
        }

        public void DeleteTemporaryFile(string? path)
        {
            if (path is not null)
            {
                Calls.Add($"delete {path}");
            }
        }

        public bool IsBinaryFile(string fileName) => BinaryFiles.Contains(fileName);

        public string? GetMergeScriptPath(string scriptName) => MergeScripts.Contains(scriptName) ? $"C:/scripts/{scriptName}" : null;

        public string GetFullPath(string fileName) => $"C:\\repo\\{fileName}";

        public DateTime? GetLastWriteTime(string fileName) => _time;

        public void StartMergeScript(string mergeScript, string filePath, string? remoteFile, string? localFile, string? baseFile)
        {
            Calls.Add($"script {mergeScript} {filePath} {remoteFile} {localFile} {baseFile}");
            if (ModifyOnMergeScript)
            {
                _time = _time.AddMinutes(1);
            }
        }

        public Task<int?> RunMergeToolProcessAsync(string path, string arguments)
        {
            Calls.Add($"run {path}:{arguments}");
            if (ModifyOnMerge)
            {
                _time = _time.AddMinutes(1);
            }

            return Task.FromResult(ExitCode);
        }

        public bool SaveSide(string fileName, string targetFile, ConflictSide side)
        {
            Calls.Add($"save {fileName} {side} to {targetFile}");
            return SaveSucceeds;
        }

        public string GetTemporaryPath(string fileName) => $"C:/temp/{Path.GetFileName(fileName)}";

        public string? ChooseSaveFile(string fileName)
        {
            Calls.Add($"choose save file {fileName}");
            return SaveFile;
        }

        public void Open(string path) => Calls.Add($"open {path}");

        public void OpenWith(string path) => Calls.Add($"open with {path}");

        public void ShowInFolder(string path) => Calls.Add($"show in folder {path}");

        public void ShowFileHistory(string fileName) => Calls.Add($"history {fileName}");

        public SolveConflictAnswer AskSolveConflict(SolveConflictQuestion question)
        {
            Questions.Add(question);
            return Answers.Count > 0 ? Answers.Dequeue() : default;
        }

        public bool ConfirmDeleteAllChanges(string text, string caption)
        {
            Calls.Add("confirm delete all");
            return true;
        }

        public void ResetHard() => Calls.Add("reset hard");

        public void UpdateSubmodules() => Calls.Add("update submodules");

        public bool ConfirmCommit(string text, string caption)
        {
            Calls.Add("confirm commit");
            return ConfirmCommitResult;
        }

        public void StartCommit() => Calls.Add("commit");

        public void OpenUrl(string url) => Calls.Add($"url {url}");

        private void Resolve(string fileName) => Conflicts.RemoveAll(c => c.Filename == fileName);
    }
}

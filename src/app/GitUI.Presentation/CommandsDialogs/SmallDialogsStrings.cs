using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the command line help; ids match <c>FormCommandlineHelp</c>.</summary>
public sealed class CommandlineHelpStrings : ViewStrings
{
    public CommandlineHelpStrings()
        : base("FormCommandlineHelp")
    {
        Title = Add("$this", "Text", "Commandline usage");
        Intro = Add("label1", "Text", "Supported commandline arguments for\r\ngitex.cmd / gitex (located in the same folder as Gitanas.exe):");
    }

    public TranslatedText Title { get; }

    public TranslatedText Intro { get; }
}

/// <summary>Strings of the add files dialog; ids match <c>FormAddFiles</c>.</summary>
public sealed class AddFilesStrings : ViewStrings
{
    public AddFilesStrings()
        : base("FormAddFiles")
    {
        Title = Add("$this", "Text", "Add files");
        Filter = Add("label1", "Text", "Filter");
        Force = Add("force", "Text", "Force");
        ShowFiles = Add("ShowFiles", "Text", "Show files");
        AddFiles = Add("AddFiles", "Text", "Add files");
    }

    public TranslatedText Title { get; }

    public TranslatedText Filter { get; }

    public TranslatedText Force { get; }

    public TranslatedText ShowFiles { get; }

    public TranslatedText AddFiles { get; }
}

/// <summary>Strings of the donation dialog; ids match <c>FormDonate</c>.</summary>
public sealed class DonateStrings : ViewStrings
{
    public DonateStrings()
        : base("SupportUpstreamDialog")
    {
        Title = Add("$this", "Text", "Support Git Extensions");
        Text = Add(
            "_donateText",
            "Text",
            "Gitanas is based on Git Extensions. You can support the original Git Extensions project by making a donation. "
            + "The button below opens the Git Extensions Open Collective page; donations go to the upstream project.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Text { get; }
}

/// <summary>Strings of the reset changes confirmation; ids match <c>FormResetChanges</c>.</summary>
public sealed class ResetChangesStrings : ViewStrings
{
    public ResetChangesStrings()
        : base("FormResetChanges")
    {
        Title = Add("$this", "Text", "Reset changes");
        Message = Add("txtMessage", "Text", "Are you sure you want to reset your changes?");
        DeleteHint = Add("lblDeleteHint", "Text", "This will delete any uncommitted work.");
        DeleteNewFiles = Add("cbDeleteNewFilesAndDirectories", "Text", "Also delete &new files and/or directories");
        Reset = Add("btnReset", "Text", "R&eset");
        Cancel = Add("btnCancel", "Text", "&Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText Message { get; }

    public TranslatedText DeleteHint { get; }

    public TranslatedText DeleteNewFiles { get; }

    public TranslatedText Reset { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>Strings of the delete tag dialog; ids match <c>FormDeleteTag</c> and its <c>GotoUserManualControl</c>.</summary>
public sealed class DeleteTagStrings : ViewStrings
{
    public DeleteTagStrings()
        : base("FormDeleteTag")
    {
        Title = Add("$this", "Text", "Delete tag");
        SelectTag = Add("label1", "Text", "Select tag");
        Explanation = Add("label2", "Text", "This will delete the selected tag from the (local) repository.");
        DeleteFromRemotes = Add("deleteTag", "Text", "Delete tag also from the following remote(s):");
        HelpNote = Add("label3", "Text", "(includes information about deleting tags which are already pushed)");
        Delete = Add("Ok", "Text", "Delete");
        Help = Add("linkLabelHelp", "Text", "Help", category: "GotoUserManualControl");
        HelpTooltip = Add("_gotoUserManualControlTooltip", "Text", "Read more about this feature at {0}", category: "GotoUserManualControl");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectTag { get; }

    public TranslatedText Explanation { get; }

    public TranslatedText DeleteFromRemotes { get; }

    public TranslatedText HelpNote { get; }

    public TranslatedText Delete { get; }

    public TranslatedText Help { get; }

    public TranslatedText HelpTooltip { get; }
}

/// <summary>Strings of the create repository dialog; ids match <c>FormInit</c> and its <c>FolderBrowserButton</c>.</summary>
public sealed class InitStrings : ViewStrings
{
    public InitStrings()
        : base("FormInit")
    {
        Title = Add("$this", "Text", "Create new repository");
        Directory = Add("label1", "Text", "Directory");
        RepositoryType = Add("groupBox1", "Text", "Repository type");
        Personal = Add("Personal", "Text", "Personal repository");
        Central = Add("Central", "Text", "Central repository, no working directory  (--bare --shared=all)");
        Create = Add("Init", "Text", "Create");
        ChooseDirectory = Add("_chooseDirectory", "Text", "Please choose a directory.");
        ChooseDirectoryCaption = Add("_chooseDirectoryCaption", "Text", "Choose directory");
        ChooseDirectoryNotFile = Add("_chooseDirectoryNotFile", "Text", "Cannot initialize a new repository on a file.\nPlease choose a directory.");
        InitCaption = Add("_initMsgBoxCaption", "Text", "Create new repository");
        Browse = Add("buttonBrowse", "Text", "&Browse...", category: "FolderBrowserButton");
    }

    public TranslatedText Title { get; }

    public TranslatedText Directory { get; }

    public TranslatedText RepositoryType { get; }

    public TranslatedText Personal { get; }

    public TranslatedText Central { get; }

    public TranslatedText Create { get; }

    public TranslatedText ChooseDirectory { get; }

    public TranslatedText ChooseDirectoryCaption { get; }

    public TranslatedText ChooseDirectoryNotFile { get; }

    public TranslatedText InitCaption { get; }

    public TranslatedText Browse { get; }
}

/// <summary>Strings of the go to line dialog; ids match <c>FormGoToLine</c>.</summary>
public sealed class GoToLineStrings : ViewStrings
{
    public GoToLineStrings()
        : base("FormGoToLine")
    {
        Title = Add("$this", "Text", "Go to line");
        LineNumber = Add("lineLabel", "Text", "Line number");
        Ok = Add("okBtn", "Text", "OK");
        Cancel = Add("cancelBtn", "Text", "Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText LineNumber { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>Strings of the script file prompt; ids match <c>FormFilePrompt</c>.</summary>
public sealed class FilePromptStrings : ViewStrings
{
    public FilePromptStrings()
        : base("FormFilePrompt")
    {
        Title = Add("$this", "Text", "Select script files");
        SelectFiles = Add("lblSelectFiles", "Text", "Select file(s)");
        Browse = Add("btnBrowse", "Text", "Browse...");
        Ok = Add("btnOk", "Text", "&OK");
    }

    public TranslatedText Title { get; }

    public TranslatedText SelectFiles { get; }

    public TranslatedText Browse { get; }

    public TranslatedText Ok { get; }
}

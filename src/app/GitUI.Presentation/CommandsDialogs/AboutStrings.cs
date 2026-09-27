using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs;

/// <summary>Strings of the about dialog; ids match <c>FormAbout</c>.</summary>
public sealed class AboutStrings : ViewStrings
{
    public AboutStrings()
        : base("FormAbout")
    {
        ThanksToContributors = Add("_thanksToContributors", "Text", "Thanks to over {0:#,##0} contributors: ");
        CopyTooltip = Add("_copyTooltip", "Text", "Copy environment info");
        Warranty = Add("label1", "Text", "This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY of FITNESS FOR A PARTICULAR PURPOSE.");
        GetInvolved = Add("label2", "Text", "Gitanas is open source. Get involved!");
    }

    public TranslatedText ThanksToContributors { get; }

    public TranslatedText CopyTooltip { get; }

    public TranslatedText Warranty { get; }

    public TranslatedText GetInvolved { get; }

    // Not translated in the WinForms dialog either (_NO_TRANSLATE_ labels).
    public string ProductDescription => "A cross-platform Git client";

    public string PresentedBy => "Based on Git Extensions, with thanks to its contributors.";
}

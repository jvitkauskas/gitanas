using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

/// <summary>Strings of the introduction of the Git settings; ids match <c>GitRootIntroductionPage</c>.</summary>
public sealed class GitRootIntroductionPageStrings : ViewStrings
{
    public GitRootIntroductionPageStrings()
        : base("GitRootIntroductionPage")
    {
        Title = Add("$this", "Text", "Git Settings");
        Text = Add("label1", "Text", "Select one of the subnodes to view or edit the Git settings");
    }

    public TranslatedText Title { get; }

    public TranslatedText Text { get; }
}

/// <summary>Strings of the introduction of the plugin settings; ids match <c>PluginRootIntroductionPage</c>.</summary>
public sealed class PluginRootIntroductionPageStrings : ViewStrings
{
    public PluginRootIntroductionPageStrings()
        : base("PluginRootIntroductionPage")
    {
        Title = Add("$this", "Text", "Plugins Settings");
        Text = Add("label1", "Text", "Select one of the subnodes to view or edit the settings of a Gitanas Plugin.");
    }

    public TranslatedText Title { get; }

    public TranslatedText Text { get; }
}

/// <summary>A page with a text only (<c>GitRootIntroductionPage</c>, <c>PluginRootIntroductionPage</c>), without header.</summary>
public sealed class IntroductionSettingsPageViewModel(string title, string text, string pageName) : SettingsPageViewModel
{
    public override string Title => title;

    public override string PageName => pageName;

    public string Text => text;

    public override IEnumerable<string> SearchKeywords => [text];

    public static IntroductionSettingsPageViewModel CreateGitRoot()
    {
        GitRootIntroductionPageStrings strings = ViewStrings.Load<GitRootIntroductionPageStrings>();
        return new(strings.Title.Text, strings.Text.Text, "GitRootIntroductionPage");
    }

    public static IntroductionSettingsPageViewModel CreatePluginRoot()
    {
        PluginRootIntroductionPageStrings strings = ViewStrings.Load<PluginRootIntroductionPageStrings>();
        return new(strings.Title.Text, strings.Text.Text, "PluginRootIntroductionPage");
    }
}

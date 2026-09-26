using System.Text;
using GitCommands;
using GitExtensions.Extensibility.Translations;

namespace GitUI.Presentation.Translations;

/// <summary>
///  Base class for the translatable strings of a ported view.
/// </summary>
/// <remarks>
///  <para>
///   Each entry is declared with the exact XLIFF id (<c>item.property</c>) of the WinForms form it replaces,
///   under the same category (the form's class name). The existing translations therefore apply unchanged,
///   and <c>TranslationApp</c> produces identical entries whether the WinForms form or the view is scanned.
///  </para>
///  <para>
///   Unlike <c>ResourceManager.Translate</c>, entries are declared explicitly instead of being discovered by
///   reflecting over WinForms controls, so they do not depend on WinForms naming or tooltip conventions.
///  </para>
/// </remarks>
public abstract class ViewStrings : ITranslate
{
    private readonly string _category;
    private readonly List<(string Category, string Item, string Property, TranslatedText Text)> _entries = [];

    protected ViewStrings(string category)
    {
        _category = category;
    }

    /// <summary>
    ///  Creates the strings of a view, translated to the current UI language.
    /// </summary>
    public static T Load<T>()
        where T : ViewStrings, new()
    {
        T strings = new();
        Translator.Translate(strings, AppSettings.CurrentTranslation);
        return strings;
    }

    /// <summary>
    ///  Declares a translatable entry, e.g. <c>Add("label1", "Text", "New name")</c> or <c>Add("$this", "Text", "Rename branch")</c> for the title.
    /// </summary>
    /// <param name="category">
    ///  The XLIFF category, if the entry belongs to another form or user control than the view's own category
    ///  (e.g. a user control the WinForms form embedded).
    /// </param>
    protected TranslatedText Add(string item, string property, string neutralText, string? category = null)
    {
        TranslatedText text = new(neutralText);
        _entries.Add((category ?? _category, item, property, text));
        return text;
    }

    /// <summary>
    ///  The translated texts of the view, without access key markers (e.g. the search keywords of a settings page, as
    ///  <c>SettingsPageBase.GetSearchKeywords</c> collects the texts of its controls).
    /// </summary>
    public IEnumerable<string> Texts => _entries.Select(e => e.Text.PlainText).Where(t => !string.IsNullOrWhiteSpace(t));

    void ITranslate.AddTranslationItems(ITranslation translation)
    {
        foreach ((string category, string item, string property, TranslatedText text) in _entries)
        {
            // Same filter as TranslationUtil.AllowTranslateProperty: strings without letters are not translatable.
            if (text.NeutralText.Any(char.IsLetter))
            {
                translation.AddTranslationItem(category, item, property, text.NeutralText);
            }
        }
    }

    void ITranslate.TranslateItems(ITranslation translation)
    {
        foreach ((string category, string item, string property, TranslatedText text) in _entries)
        {
            // Main and plugin catalogs are applied in sequence. A missing entry must not undo a translation
            // from another catalog, or cache this view's fallback as a translation for later views.
            text.Text = translation.TranslateItem(category, item, property, () => null) ?? text.Text;
        }
    }

    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}

/// <summary>
///  A single translatable string of a view.
/// </summary>
public sealed class TranslatedText
{
    internal TranslatedText(string neutralText)
    {
        NeutralText = neutralText;
        Text = neutralText;
    }

    /// <summary>The untranslated (English) text.</summary>
    public string NeutralText { get; }

    /// <summary>The text in the current UI language, as stored in the translation (WinForms access keys: <c>&amp;Abort</c>).</summary>
    public string Text { get; internal set; }

    /// <summary>
    ///  <see cref="Text"/> with its access key in Avalonia syntax (<c>_Abort</c>), for buttons, check boxes and labels.
    /// </summary>
    public string AccessKeyText => ToAccessKeyText(Text);

    /// <summary><see cref="Text"/> without the access key marker (<c>Abort</c>), for titles and tooltips.</summary>
    public string PlainText => ToAccessKeyText(Text).Replace("__", "\0").Replace("_", "").Replace('\0', '_');

    public override string ToString() => Text;

    /// <summary>
    ///  Converts WinForms mnemonics (<c>&amp;</c> marks the access key, <c>&amp;&amp;</c> is a literal ampersand) to
    ///  Avalonia's (<c>_</c> marks the access key, <c>__</c> is a literal underscore).
    /// </summary>
    public static string ToAccessKeyText(string text)
    {
        StringBuilder result = new(text.Length + 2);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '&')
            {
                if (i + 1 < text.Length && text[i + 1] == '&')
                {
                    result.Append('&');
                    i++;
                }
                else
                {
                    result.Append('_');
                }
            }
            else if (c == '_')
            {
                result.Append("__");
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }
}

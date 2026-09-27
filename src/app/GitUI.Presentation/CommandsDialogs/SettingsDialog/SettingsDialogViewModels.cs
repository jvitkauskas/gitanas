using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GitUI.Presentation.Translations;

namespace GitUI.Presentation.CommandsDialogs.SettingsDialog;

/// <summary>Strings of the language selection; ids match <c>FormChooseTranslation</c>.</summary>
public sealed class ChooseTranslationStrings : ViewStrings
{
    public ChooseTranslationStrings()
        : base("FormChooseTranslation")
    {
        Title = Add("$this", "Text", "Choose language");
        ChooseLanguage = Add("label1", "Text", "Choose your language");
        Ok = Add("ButtonOk", "Text", "OK", "FormAvailableEncodings");
        Cancel = Add("ButtonCancel", "Text", "Cancel", "FormAvailableEncodings");
        ChangeLater = Add("label2", "Text", "You can change the language at any time in the settings dialog");
    }

    public TranslatedText Title { get; }

    public TranslatedText ChooseLanguage { get; }

    public TranslatedText ChangeLater { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>View model of the language selection (port of <c>FormChooseTranslation</c>).</summary>
public sealed partial class ChooseTranslationViewModel : DialogViewModel
{
    public const string English = "English";

    public ChooseTranslationViewModel(ChooseTranslationStrings strings, IReadOnlyList<string> translations, string? currentTranslation = null)
    {
        Strings = strings;
        Translations = translations;
        Language = translations.FirstOrDefault(language => string.Equals(language, currentTranslation, StringComparison.OrdinalIgnoreCase))
            ?? translations.FirstOrDefault();
    }

    public ChooseTranslationStrings Strings { get; }

    public IReadOnlyList<string> Translations { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChooseCommand))]
    public partial string? Language { get; set; }

    /// <summary>The confirmed language, or <see langword="null"/> if the dialog was cancelled.</summary>
    public string? SelectedTranslation { get; private set; }

    /// <summary>English first, then the translations sorted by name.</summary>
    public static IReadOnlyList<string> CreateChoices(IEnumerable<string> translations)
        => [English, .. translations.Where(name => !string.Equals(name, English, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.CurrentCulture)];

    private bool CanChoose() => Language is not null && Translations.Contains(Language);

    [RelayCommand(CanExecute = nameof(CanChoose))]
    private void Choose()
    {
        if (CanChoose())
        {
            SelectedTranslation = Language;
            Close(accepted: true);
        }
    }

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

/// <summary>Strings of the encodings configuration; ids match <c>FormAvailableEncodings</c>.</summary>
public sealed class AvailableEncodingsStrings : ViewStrings
{
    public AvailableEncodingsStrings()
        : base("FormAvailableEncodings")
    {
        Title = Add("$this", "Text", "Configure available encodings");
        Selected = Add("lSelectedEncodings", "Text", "Selected:");
        Available = Add("lAvaolableEncodings", "Text", "Available:");
        Ok = Add("ButtonOk", "Text", "OK");
        Cancel = Add("ButtonCancel", "Text", "Cancel");
    }

    public TranslatedText Title { get; }

    public TranslatedText Selected { get; }

    public TranslatedText Available { get; }

    public TranslatedText Ok { get; }

    public TranslatedText Cancel { get; }
}

/// <summary>View model of the encodings configuration (port of <c>FormAvailableEncodings</c>).</summary>
public sealed partial class AvailableEncodingsViewModel : DialogViewModel
{
    public AvailableEncodingsViewModel(AvailableEncodingsStrings strings, IEnumerable<Encoding> included)
    {
        Strings = strings;
        Included = [.. included];
        Available = [.. GetSelectableEncodings(Included).OrderBy(e => e.EncodingName, StringComparer.CurrentCulture)];
    }

    public AvailableEncodingsStrings Strings { get; }

    /// <summary>The encodings offered in the application (<c>AppSettings.AvailableEncodings</c>).</summary>
    public ObservableCollection<Encoding> Included { get; }

    /// <summary>The other encodings, sorted by name.</summary>
    public ObservableCollection<Encoding> Available { get; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExcludeCommand))]
    public partial Encoding? SelectedIncluded { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(IncludeCommand))]
    public partial Encoding? SelectedAvailable { get; set; }

    /// <summary>The encodings of the system that are not in <paramref name="included"/> (as <c>FormAvailableEncodings.LoadEncoding</c>).</summary>
    public static IEnumerable<Encoding> GetSelectableEncodings(IEnumerable<Encoding> included)
    {
        HashSet<string> includedNames = [.. included.Select(e => e.WebName)];
        return Encoding.GetEncodings()
            .Select(ei => ei.GetEncoding())
            .Select(e => e.GetType() == typeof(UTF8Encoding) ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false) : e) // If exists utf-8, then replace to utf-8 without BOM
#pragma warning disable SYSLIB0001 // Type or member is obsolete
            .Where(e => e != Encoding.UTF7) // UTF-7 is no longer supported, see: https://github.com/dotnet/docs/issues/19274
#pragma warning restore SYSLIB0001 // Type or member is obsolete
            .Where(e => !includedNames.Contains(e.WebName))
            .GroupBy(e => e.WebName)
            .Select(group => group.First());
    }

    /// <summary>Whether the encoding is one of the built-in ones, which cannot be removed.</summary>
    public static bool IsBuiltIn(Encoding encoding)
    {
        Type type = encoding.GetType();
        return type == typeof(ASCIIEncoding)
            || type == typeof(UnicodeEncoding)
            || type == typeof(UTF8Encoding)
            || type == typeof(UTF7Encoding)
            || encoding == Encoding.Default;
    }

    private bool CanInclude() => SelectedAvailable is not null;

    [RelayCommand(CanExecute = nameof(CanInclude))]
    private void Include()
    {
        Encoding encoding = SelectedAvailable!;
        Available.Remove(encoding);
        Included.Add(encoding);
    }

    private bool CanExclude() => SelectedIncluded is not null && !IsBuiltIn(SelectedIncluded);

    [RelayCommand(CanExecute = nameof(CanExclude))]
    private void Exclude()
    {
        Encoding encoding = SelectedIncluded!;
        Included.Remove(encoding);

        // The list of available encodings stays sorted.
        int index = 0;
        while (index < Available.Count && StringComparer.CurrentCulture.Compare(Available[index].EncodingName, encoding.EncodingName) < 0)
        {
            index++;
        }

        Available.Insert(index, encoding);
    }

    [RelayCommand]
    private void Ok() => Close(accepted: true);

    [RelayCommand]
    private void Cancel() => Close(accepted: false);
}

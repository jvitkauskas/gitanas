using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  The application menu of macOS (the menu named after the application in the menu bar): "About" and "Settings" (Cmd+,)
///  of the main window that was activated last, before the items of the system that Avalonia adds (Services, Hide, Quit)
///  (docs/avalonia-port/CROSS-PLATFORM.md, phase 5). The main window keeps its own menu too.
/// </summary>
[SupportedOSPlatform("macos")]
public static class MacOSApplicationMenu
{
    private static readonly NativeMenuItem _about = new("About Gitanas");
    private static readonly NativeMenuItem _settings = new("Settings...") { Gesture = new KeyGesture(Key.OemComma, KeyModifiers.Meta) };
    private static readonly NativeMenu _menu = new() { Items = { _about, new NativeMenuItemSeparator(), _settings } };
    private static Action? _showAbout;
    private static Action? _showSettings;

    static MacOSApplicationMenu()
    {
        _about.Click += (_, _) => _showAbout?.Invoke();
        _settings.Click += (_, _) => _showSettings?.Invoke();
    }

    /// <summary>
    ///  Sets the menu of the application, while Avalonia sets up (in <see cref="Application.Initialize"/>): the native menu
    ///  is created from the menu the application has then (else with "About Avalonia"); later its items can change.
    /// </summary>
    public static void Install(Application application) => NativeMenu.SetMenu(application, _menu);

    /// <summary>Enables the items, or disables them while a modal dialog is the key window.</summary>
    public static void SetEnabled(bool isEnabled)
    {
        _about.IsEnabled = isEnabled;
        _settings.IsEnabled = isEnabled;
    }

    /// <summary>Makes the items run the commands of a main window (when it is activated), with its texts.</summary>
    public static void Attach(string aboutText, Action showAbout, string settingsText, Action showSettings)
    {
        _about.Header = aboutText;
        _settings.Header = settingsText;
        _showAbout = showAbout;
        _showSettings = showSettings;
    }
}

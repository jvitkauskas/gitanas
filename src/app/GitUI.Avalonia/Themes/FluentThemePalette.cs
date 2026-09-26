using Avalonia.Controls;

namespace GitUI.Avalonia;

/// <summary>Fluent control brushes using the same host-derived surfaces and contrast rules as the other themes.</summary>
internal static class FluentThemePalette
{
    internal static ResourceDictionary Create(bool dark, IReadOnlyDictionary<string, uint>? colors)
    {
        ResourceDictionary palette = SimpleThemePalette.Create(dark, colors);
        ResourceDictionary resources = new();

        foreach (string control in new[] { "Button", "RepeatButton", "ToggleButton" })
        {
            Map(control + "Background", "ThemeControlMidBrush");
            Map(control + "BackgroundPointerOver", "ThemeControlHighlightMidBrush");
            Map(control + "BackgroundPressed", "ThemeControlHighlightHighBrush");
            Map(control + "BorderBrush", "ThemeBorderLowBrush");
            Map(control + "BorderBrushPointerOver", "ThemeBorderMidBrush");
            Map(control + "BorderBrushPressed", "ThemeBorderMidBrush");
            foreach (string state in new[] { "", "PointerOver", "Pressed" })
            {
                Map(control + "Foreground" + state, "ThemeForegroundBrush");
            }
        }

        Map("AccentButtonBackground", "HighlightBrush");
        Map("AccentButtonBackgroundPointerOver", "SimpleAccentHoverBrush");
        Map("AccentButtonBackgroundPressed", "HighlightBrush2");
        foreach (string state in new[] { "", "PointerOver", "Pressed" })
        {
            Map("AccentButtonForeground" + state, "HighlightForegroundBrush");
        }

        Map("ToggleButtonBackgroundChecked", "ThemeAccentBrush3");
        Map("ToggleButtonBackgroundCheckedPointerOver", "ThemeAccentBrush2");
        Map("ToggleButtonBackgroundCheckedPressed", "ThemeAccentBrush");
        foreach (string state in new[] { "", "PointerOver", "Pressed" })
        {
            Map("ToggleButtonForegroundChecked" + state, "ThemeForegroundBrush");
            Map("ToggleButtonBorderBrushChecked" + state, "SimpleFocusBrush");
        }

        foreach (string control in new[] { "TextControl", "ComboBox" })
        {
            Map(control + "Background", "SimpleInputBackgroundBrush");
            Map(control + "BackgroundPointerOver", "SimpleInputBackgroundBrush");
            Map(control + "BorderBrush", "ThemeBorderMidBrush");
            Map(control + "BorderBrushPointerOver", "SimpleFocusBrush");
            Map(control + "Foreground", "ThemeForegroundBrush");
            Map(control + "ForegroundFocused", "ThemeForegroundBrush");
        }

        Map("TextControlPlaceholderForeground", "ThemeForegroundLowBrush");
        Map("TextControlPlaceholderForegroundPointerOver", "ThemeForegroundLowBrush");
        Map("TextControlPlaceholderForegroundFocused", "ThemeForegroundLowBrush");
        Map("ComboBoxPlaceHolderForeground", "ThemeForegroundLowBrush");
        Map("ComboBoxPlaceHolderForegroundPointerOver", "ThemeForegroundLowBrush");
        Map("ComboBoxPlaceHolderForegroundFocused", "ThemeForegroundLowBrush");
        Map("TextControlBackgroundFocused", "SimpleInputBackgroundBrush");
        Map("TextControlBorderBrushFocused", "SimpleFocusBrush");
        Map("TextControlForegroundPointerOver", "ThemeForegroundBrush");
        Map("ComboBoxBackgroundUnfocused", "SimpleInputBackgroundBrush");
        Map("ComboBoxBackgroundPressed", "ThemeControlHighlightMidBrush");
        Map("ComboBoxBackgroundBorderBrushFocused", "SimpleFocusBrush");
        Map("ComboBoxForegroundFocusedPressed", "ThemeForegroundBrush");

        foreach (string control in new[] { "TreeViewItem", "ComboBoxItem" })
        {
            Map(control + "BackgroundPointerOver", "ThemeControlHighlightMidBrush");
            Map(control + "BackgroundPressed", "ThemeControlHighlightHighBrush");
            Map(control + "BackgroundSelected", "ThemeAccentBrush4");
            Map(control + "BackgroundSelectedPointerOver", "ThemeAccentBrush3");
            Map(control + "BackgroundSelectedPressed", "ThemeAccentBrush2");
            foreach (string state in new[] { "", "PointerOver", "Pressed", "Selected", "SelectedPointerOver", "SelectedPressed" })
            {
                Map(control + "Foreground" + state, "ThemeForegroundBrush");
            }
        }

        // ListBox and DataGrid use these shared brushes directly rather than per-control aliases.
        Map("SystemControlHighlightListLowBrush", "ThemeControlHighlightMidBrush");
        Map("SystemControlHighlightListMediumBrush", "ThemeControlHighlightHighBrush");
        Map("SystemControlHighlightListAccentLowBrush", "ThemeAccentBrush4");
        Map("SystemControlHighlightListAccentMediumBrush", "ThemeAccentBrush3");
        Map("SystemControlHighlightListAccentHighBrush", "ThemeAccentBrush2");
        Map("SystemControlHighlightAltListAccentLowBrush", "ThemeAccentBrush4");
        Map("SystemControlHighlightAltListAccentMediumBrush", "ThemeAccentBrush3");
        Map("SystemControlHighlightAltListAccentHighBrush", "ThemeAccentBrush2");
        Map("SystemControlHighlightAltBaseHighBrush", "ThemeForegroundBrush");
        return resources;

        void Map(string key, string source) => resources[key] = palette[source];
    }
}

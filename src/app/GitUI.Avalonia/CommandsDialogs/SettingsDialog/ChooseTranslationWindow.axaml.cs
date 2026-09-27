using Avalonia.Input;
using Avalonia.Interactivity;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;

namespace GitUI.Avalonia.CommandsDialogs.SettingsDialog;

/// <summary>Avalonia port of <c>FormChooseTranslation</c>.</summary>
public partial class ChooseTranslationWindow : DialogWindow
{
    public ChooseTranslationWindow()
    {
        InitializeComponent();
        translationsListBox.AddHandler(KeyDownEvent, (_, e) =>
        {
            // ListBox handles Enter itself, so confirm before it consumes the key.
            if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None
                && DataContext is ChooseTranslationViewModel viewModel && viewModel.ChooseCommand.CanExecute(null))
            {
                viewModel.ChooseCommand.Execute(null);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);
        Opened += (_, _) =>
        {
            if (translationsListBox.SelectedItem is { } language)
            {
                translationsListBox.ScrollIntoView(language);
            }

            translationsListBox.UpdateLayout();
            if (translationsListBox.ContainerFromIndex(translationsListBox.SelectedIndex) is { } selected)
            {
                selected.Focus();
            }
            else
            {
                translationsListBox.Focus();
            }
        };
    }
}

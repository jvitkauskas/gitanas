using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Services;

namespace GitUI.AvaloniaTests.Views;

[TestFixture]
public sealed class PluginIconViewTests : HeadlessTest
{
    [Test]
    public Task Plugin_icons_render_in_menus_and_settings([Values("light", "dark")] string theme) => OnUiThreadAsync(() =>
    {
        UseTheme(theme == "dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        string[] names = [.. AssetLoader.GetAssets(new Uri("avares://GitUI.Avalonia/Assets/Modern/Light/"), null)
            .Select(uri => Path.GetFileNameWithoutExtension(uri.AbsolutePath))
            .Where(name => name.StartsWith("Plugin", StringComparison.Ordinal) && name != "Plugin")
            .Order(StringComparer.Ordinal)];
        names.Should().HaveCount(11);
        StackPanel rows = new() { Margin = new Thickness(16), Spacing = 12 };
        rows.Children.Add(new TextBlock { Text = "Plugin icons", FontSize = 18, FontWeight = FontWeight.SemiBold });
        foreach (string name in names)
        {
            Bitmap bitmap = (Bitmap)SettingsIconConverter.Instance.Convert(name, typeof(Bitmap), null, CultureInfo.InvariantCulture)!;
            bitmap.PixelSize.Should().Be(new PixelSize(32, 32));
            ImageLightness.IsModernIcon(bitmap).Should().BeTrue();
            MenuItem menu = (MenuItem)MenuModelRenderer.CreateItems([new MenuModelItem(name, Icon: name)])[0];
            ((Image)menu.Icon!).Source.Should().BeSameAs(bitmap);
            StackPanel row = new() { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 14 };
            row.Children.Add(new Image { Source = bitmap, Width = 16, Height = 16 });
            row.Children.Add(new Image { Source = bitmap, Width = 32, Height = 32 });
            row.Children.Add(new TextBlock { Text = name["Plugin".Length..], VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center });
            rows.Children.Add(row);
        }

        Window window = new() { Width = 360, SizeToContent = SizeToContent.Height, Content = rows };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        SaveScreenshot(window.CaptureRenderedFrame(), $"plugin-icons-{theme}");
        window.Close();
    });
}

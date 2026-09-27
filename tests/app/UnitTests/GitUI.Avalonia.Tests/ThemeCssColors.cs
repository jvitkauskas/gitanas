using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Media;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.Editor;
using DrawingColor = System.Drawing.Color;

namespace GitUI.AvaloniaTests;

/// <summary>
///  The colors of a Gitanas theme (<c>src/app/GitUI/Themes/*.css</c>), for the screenshots of colored diffs on dark themes:
///  the colors that git's output is parsed with, and the <c>AppColor.*</c> resources the host provides (as <c>AvaloniaHostServices</c>).
///  The colors that the theme does not set are the default ones.
/// </summary>
internal sealed partial class ThemeCssColors : IThemeColors
{
    private readonly Dictionary<AppColor, DrawingColor> _colors = [];

    private ThemeCssColors(string css, bool isDarkMode)
    {
        IsDarkMode = isDarkMode;
        foreach (Match match in ColorRegex.Matches(css))
        {
            if (Enum.TryParse(match.Groups["name"].Value, out AppColor color))
            {
                _colors[color] = DrawingColor.FromArgb(int.Parse(match.Groups["rgb"].ValueSpan, NumberStyles.HexNumber) | unchecked((int)0xff000000));
            }
        }
    }

    [GeneratedRegex(@"^\.(?<name>\w+)\s*\{\s*color:\s*#(?<rgb>[0-9a-fA-F]{6});", RegexOptions.Multiline | RegexOptions.ExplicitCapture)]
    private static partial Regex ColorRegex { get; }

    /// <summary>The dark theme of Gitanas (<c>dark.css</c>).</summary>
    public static ThemeCssColors Dark { get; } = Load("dark.css", isDarkMode: true);

    public bool IsDarkMode { get; }

    public DrawingColor GetColor(AppColor color) => _colors.TryGetValue(color, out DrawingColor themed) ? themed : Theme.Default.GetColor(color);

    /// <summary>Adds the colors as the resources of the host (<c>AppColor.&lt;name&gt;</c>).</summary>
    public void AddResources(IResourceDictionary resources)
    {
        foreach (AppColor color in Enum.GetValues<AppColor>())
        {
            DrawingColor value = GetColor(color);
            resources[ThemeColors.AppColorPrefix + color] = new SolidColorBrush(Color.FromArgb(value.A, value.R, value.G, value.B));
        }
    }

    private static ThemeCssColors Load(string fileName, bool isDarkMode)
    {
        for (DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory); directory is not null; directory = directory.Parent)
        {
            string path = Path.Combine(directory.FullName, "src", "app", "GitUI", "Themes", fileName);
            if (File.Exists(path))
            {
                return new ThemeCssColors(File.ReadAllText(path), isDarkMode);
            }
        }

        throw new FileNotFoundException("The theme is not found above the test directory.", fileName);
    }
}

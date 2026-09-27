using Avalonia;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using GitUI.Avalonia;
using GitUI.Avalonia.Hosting;

namespace GitUI.AvaloniaTests;

/// <summary>
///  The Avalonia application used by the headless view tests: the real <see cref="GitExtensionsAvaloniaApp"/>
///  (styles, theme) rendered with Skia, so that frames can be captured as screenshots.
/// </summary>
public static class HeadlessTestApp
{
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<GitExtensionsAvaloniaApp>()
            .UseSkia()
            .UseHarfBuzz()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .AfterSetup(builder => ((GitExtensionsAvaloniaApp)builder.Instance!).ApplyOptions(new AvaloniaUiOptions(IsDarkTheme: false, "Segoe UI", 12)));
}

/// <summary>
///  Base class for tests that run on the headless Avalonia UI thread.
/// </summary>
public abstract class HeadlessTest
{
    private static HeadlessUnitTestSession Session => HeadlessUnitTestSession.GetOrStartForAssembly(typeof(HeadlessTest).Assembly);

    /// <summary>Runs <paramref name="test"/> on the Avalonia UI thread.</summary>
    protected static Task OnUiThreadAsync(Action test) => Session.Dispatch(test, CancellationToken.None);

    /// <summary>Runs the asynchronous <paramref name="test"/> on the Avalonia UI thread (e.g. with the clipboard).</summary>
    protected static Task OnUiThreadAsync(Func<Task> test) => Session.Dispatch(async () =>
    {
        // Dispatch(Func<TResult>) with TResult = Task would return Task<Task> and hide assertion failures.
        await test();
        return 0;
    }, CancellationToken.None);

    /// <summary>Sets the theme variant for the rest of the current test (reset by <see cref="ResetThemeAsync"/>).</summary>
    protected static void UseTheme(ThemeVariant theme)
        => ((GitExtensionsAvaloniaApp)Application.Current!).ApplyOptions(new AvaloniaUiOptions(theme == ThemeVariant.Dark, "Segoe UI", 12));

    [TearDown]
    public Task ResetThemeAsync() => OnUiThreadAsync(() => UseTheme(ThemeVariant.Light));

    /// <summary>Saves a rendered frame to <c>&lt;test work dir&gt;/screenshots/&lt;name&gt;.png</c> for visual review.</summary>
    protected static void SaveScreenshot(WriteableBitmap? frame, string name)
    {
        frame.Should().NotBeNull();
        string directory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, $"{name}.png");
        frame!.Save(path, new PngBitmapEncoderOptions());
        TestContext.AddTestAttachment(path);
    }
}

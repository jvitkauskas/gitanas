using GitCommands;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtUtils;
using GitExtUtils.GitUI.Theming;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.CommandsDialogs.CommitDialog;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs;
using GitUI.CommandsDialogs.BrowseDialog;
using GitUI.CommandsDialogs.CommitDialog;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.CommandsDialogs.CommitDialog;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using GitUI.Properties;
using ResourceManager;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routes dialogs to their Avalonia ports (docs/avalonia-port/PLAN.md). Each <c>TryShow*</c> method returns
///  <see langword="false"/> when the Avalonia port is disabled, in which case the caller shows the WinForms form.
/// </summary>
/// <remarks>
///  This is the composition side of the port: it lives in GitUI because it gathers data and services from the
///  WinForms application (settings, environment info, progress dialogs) and hands them to the view models.
/// </remarks>
internal static partial class AvaloniaDialogs
{
    public static bool TryShowAbout(IWin32Window? owner)
    {
        ShowDialog(
            () =>
            {
                AboutWindow window = new();
                window.DataContext = new AboutViewModel(
                    ViewStrings.Load<AboutStrings>(),
                    AppSettings.ApplicationName,
                    UserEnvironmentInformation.GetInformation().Replace("- ", "").TrimEnd(),
                    GetContributors(),
                    new AboutDialogHost(window));
                return window;
            },
            owner);
        return true;

        // Same list as FormAbout shows.
        static IReadOnlyList<string> GetContributors()
            => new[] { Resources.Team, Resources.Coders, Resources.Translators, Resources.Designers }
                .Select(c => c.Replace(Environment.NewLine, ""))
                .SelectMany(line => line.LazySplit(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(contributor => contributor.Trim())
                .ToList();
    }

    public static bool TryShowRenameBranch(IWin32Window? owner, IGitUICommands commands, string branch, out bool renamed)
    {
        renamed = false;
        renamed = ShowDialog(
            () =>
            {
                RenameBranchWindow window = new();
                window.DataContext = new RenameBranchViewModel(
                    ViewStrings.Load<RenameBranchStrings>(),
                    branch,
                    commands.GetRequiredService<IGitBranchNameNormaliser>(),
                    new GitBranchNameOptions(AppSettings.AutoNormaliseSymbol),
                    AppSettings.AutoNormaliseBranchName,
                    (oldName, newName) => AvaloniaUi.RunInHostContext(() => ProcessDialogs.ShowProcess(
                        new NativeWindowOwner(window),
                        commands,
                        arguments: Commands.RenameBranch(oldName, newName),
                        commands.Module.WorkingDir,
                        input: null,
                        useDialogSettings: true)));
                return window;
            },
            owner,
            positionName: "FormRenameBranch");
        return true;
    }

    public static bool TryShowCommitTemplateSettings(IWin32Window? owner)
    {
        ShowDialog(
            () => new CommitTemplateSettingsWindow
            {
                DataContext = new CommitTemplateSettingsViewModel(
                    ViewStrings.Load<CommitTemplateSettingsStrings>(),
                    new AppSettingsCommitMessageSettingsStore()),
            },
            owner);
        return true;
    }

    /// <param name="positionName">
    ///  The name under which the WinForms form persisted its position (its type name), if it did
    ///  (<c>enablePositionRestore</c>); the Avalonia dialog shares it.
    /// </param>
    internal static bool ShowDialog(Func<DialogWindow> createWindow, IWin32Window? owner, string? positionName = null)
    {
        AvaloniaUi.EnsureInitialized(GetOptions);
        DialogWindow window = createWindow();
        window.PositionName = positionName;
        window.PositionStore = WindowPositionStore.Instance;
        return AvaloniaDialogHost.ShowDialog(window, owner?.Handle ?? 0);
    }

    /// <summary>As <c>GitExtensionsForm.LoadHotkeys</c>: the configured hotkeys of the form's setting.</summary>
    private static IReadOnlyList<HotkeyBinding> LoadHotkeys(IGitUICommands commands, string hotkeySettingsName)
        => [.. commands.GetRequiredService<IHotkeySettingsLoader>().LoadHotkeys(hotkeySettingsName)
            .Select(hotkey => new HotkeyBinding(hotkey.CommandCode, (int)hotkey.KeyData))];

    [System.Runtime.Versioning.SupportedOSPlatform("windows6.1")]
    private static byte[]? LoadFileTypeIcon(string fileName)
    {
        // Not disposed: the provider keeps the icons of the extensions.
        Icon? icon = new FileAssociatedIconProvider().Get(Path.GetTempPath(), Path.GetFileName(fileName));
        if (icon is null)
        {
            return null;
        }

        using Bitmap bitmap = icon.ToBitmap();
        return bitmap.ToPngData();
    }

    internal static AvaloniaUiOptions GetOptions()
    {
        // The exceptions of Avalonia code are reported as those of WinForms code (Application.ThreadException).
        AvaloniaUi.UnhandledExceptionHandler ??= exception => GitUI.NBugReports.BugReportInvoker.Report(exception, isTerminating: false);

        // Quit (Cmd+Q on macOS) closes the main windows, as closing each one would.
        AvaloniaUi.QuitRequested ??= CloseBrowseWindows;

        // As GitExtensionsDialog.OnHelpButtonClicked: F1 opens the section of the user manual.
        DialogWindow.OpenManualSection ??= (subfolder, anchor) => OsShellUtil.OpenUrlInDefaultBrowser(UserManual.UserManual.UrlFor(subfolder, anchor));

        // As FileStatusList.LoadFileIcons: the icon of the type of a file in the shell of Windows (for an extension, the files need
        // not exist); elsewhere the icons by extension of the file list.
        if (OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            GitUI.Avalonia.Controls.FileStatusList.FileStatusIconImage.LoadFileTypeIcon ??= LoadFileTypeIcon;
        }

        FontDescriptor font = AppSettings.Font;
        return new AvaloniaUiOptions(
            IsDarkTheme: ColorHelper.IsDarkTheme,
            FontFamily: font.FamilyName,
            FontSize: font.SizeInPixels,
            Colors: GetThemeColors(),
            MonospaceFontFamily: AppSettings.MonospaceFont.FamilyName,
            EditorFontFamily: AppSettings.FixedWidthFont.FamilyName,
            EditorFontSize: AppSettings.FixedWidthFont.SizeInPixels,
            CommitFontFamily: AppSettings.CommitFont.FamilyName,
            CommitFontSize: AppSettings.CommitFont.SizeInPixels);
    }

    private sealed class AboutDialogHost(DialogWindow window) : IAboutDialogHost
    {
        public void OpenUrl(string url) => AvaloniaUi.RunInHostContext(() => OsShellUtil.OpenUrlInDefaultBrowser(url));

        public void ShowContributors() => AvaloniaUi.RunInHostContext(() =>
        {
            TryShowContributors(new NativeWindowOwner(window));
        });

        public void CopyEnvironmentInfo() => AvaloniaUi.RunInHostContext(UserEnvironmentInformation.CopyInformation);
    }
}

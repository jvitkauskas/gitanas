using System.Text;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Translations;
using GitUI.Avalonia.CommandsDialogs.SettingsDialog;
using GitUI.Avalonia.HelperDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.Presentation.CommandsDialogs.SettingsDialog;
using GitUI.Presentation.HelperDialogs;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces.BuildServerIntegration;

namespace GitUI.AvaloniaHosting;

/// <summary>
///  Routing of small helper and settings dialogs (docs/avalonia-port/PLAN.md, phase 2, batch 2).
/// </summary>
internal static partial class AvaloniaDialogs
{
    /// <summary>The Avalonia port of <c>FormPuttyError.AskForKey</c>.</summary>
    public static bool TryShowPuttyError(IWin32Window? owner, out bool retry, out string? keyPath)
    {
        retry = false;
        keyPath = null;
        PuttyErrorViewModel? viewModel = null;
        ShowDialog(
            () =>
            {
                PuttyErrorWindow window = new();
                viewModel = new PuttyErrorViewModel(
                    ViewStrings.Load<PuttyErrorStrings>(),
                    () => AvaloniaUi.RunInHostContext(() => BrowseForPrivateKey.BrowseAndLoad(new NativeWindowOwner(window))));
                window.DataContext = viewModel;
                return window;
            },
            owner);

        retry = viewModel!.ShouldRetry;
        keyPath = viewModel.KeyPath;
        return true;
    }

    /// <summary>
    ///  The Avalonia port of <c>FormBuildServerCredentials</c>; <paramref name="credentials"/> are updated if accepted.
    /// </summary>
    public static bool TryShowBuildServerCredentials(IWin32Window? owner, string buildServerUniqueKey, IBuildServerCredentials credentials, out bool accepted)
    {
        accepted = false;
        BuildServerCredentialsViewModel viewModel = new(buildServerUniqueKey)
        {
            Authentication = credentials.BuildServerCredentialsType switch
            {
                BuildServerCredentialsType.UsernameAndPassword => BuildServerAuthentication.UsernameAndPassword,
                BuildServerCredentialsType.BearerToken => BuildServerAuthentication.BearerToken,
                _ => BuildServerAuthentication.Guest,
            },
            Username = credentials.Username ?? "",
            Password = credentials.Password ?? "",
            BearerToken = credentials.BearerToken ?? "",
        };

        accepted = ShowDialog(() => new BuildServerCredentialsWindow { DataContext = viewModel }, owner);
        if (accepted)
        {
            credentials.BuildServerCredentialsType = viewModel.Authentication switch
            {
                BuildServerAuthentication.UsernameAndPassword => BuildServerCredentialsType.UsernameAndPassword,
                BuildServerAuthentication.BearerToken => BuildServerCredentialsType.BearerToken,
                _ => BuildServerCredentialsType.Guest,
            };
            credentials.Username = viewModel.Username;
            credentials.Password = viewModel.Password;
            credentials.BearerToken = viewModel.BearerToken;
        }

        return true;
    }

    /// <summary>The Avalonia port of <c>FormSelectMultipleBranches</c>.</summary>
    public static bool TrySelectMultipleBranches(IWin32Window? owner, IReadOnlyList<IGitRef> branches, IEnumerable<IGitRef> selectedBranches, out IReadOnlyList<IGitRef> selected)
    {
        selected = [];
        SelectMultipleBranchesViewModel viewModel = new(
            ViewStrings.Load<SelectMultipleBranchesStrings>(),
            branches.Select(b => ((object)b, b.Name)),
            selectedBranches.Select(b => b.Name));
        ShowDialog(() => new SelectMultipleBranchesWindow { DataContext = viewModel }, owner, positionName: "FormSelectMultipleBranches");

        // As FormSelectMultipleBranches, which has no cancel button: closing the dialog keeps the checked branches.
        selected = [.. viewModel.SelectedBranches.Cast<IGitRef>()];
        return true;
    }

    /// <summary>The Avalonia port of <c>FormChooseTranslation</c>; sets <c>AppSettings.Translation</c>.</summary>
    public static bool TryShowChooseTranslation(IWin32Window? owner)
    {
        ChooseTranslationViewModel viewModel = new(
            ViewStrings.Load<ChooseTranslationStrings>(),
            ChooseTranslationViewModel.CreateChoices(Translator.GetAllTranslations()),
            AppSettings.Translation);
        ShowDialog(() => new ChooseTranslationWindow { DataContext = viewModel }, owner);

        if (viewModel.SelectedTranslation is not null)
        {
            AppSettings.Translation = viewModel.SelectedTranslation;
        }
        else if (string.IsNullOrEmpty(AppSettings.Translation))
        {
            AppSettings.Translation = ChooseTranslationViewModel.English;
        }

        return true;
    }

    /// <summary>The Avalonia port of <c>FormAvailableEncodings</c>; updates <c>AppSettings.AvailableEncodings</c> if accepted.</summary>
    public static bool TryShowAvailableEncodings(IWin32Window? owner, out bool accepted)
    {
        accepted = false;
        AvailableEncodingsViewModel viewModel = new(ViewStrings.Load<AvailableEncodingsStrings>(), AppSettings.AvailableEncodings.Values);
        accepted = ShowDialog(() => new AvailableEncodingsWindow { DataContext = viewModel }, owner);
        if (accepted)
        {
            Dictionary<string, Encoding> encodings = AppSettings.AvailableEncodings;
            encodings.Clear();
            foreach (Encoding encoding in viewModel.Included)
            {
                encodings.Add(encoding.WebName, encoding);
            }
        }

        return true;
    }
}

using GitCommands;
using GitCommands.Config;
using GitCommands.DiffMergeTools;
using GitCommands.Git;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Hosting;
using GitUI.CommandsDialogs.SettingsDialog;
using GitUI.CommandsDialogs.SettingsDialog.Pages;
using GitUI.CommandsDialogs.SettingsDialog.ShellExtension;
using GitUI.Presentation.CommandsDialogs.SettingsDialog.Pages;

namespace GitUI.AvaloniaHosting;

internal static partial class AvaloniaDialogs
{
    /// <summary>Whether all the checks of the checklist pass, without showing it (<c>ChecklistSettingsPage.CheckSettings</c>).</summary>
    internal static bool CheckSettings(IGitUICommands commands, CommonLogic commonLogic)
    {
        ChecklistSettingsPageStrings strings = GitUI.Presentation.Translations.ViewStrings.Load<ChecklistSettingsPageStrings>();
        return new ChecklistSettingsPageViewModel(strings, new ChecklistSettingsHost(strings, commonLogic, commands, () => null)).CheckSettings();
    }

    /// <summary>The checks and repairs of <c>ChecklistSettingsPage</c>.</summary>
    private sealed class ChecklistSettingsHost(ChecklistSettingsPageStrings strings, CommonLogic commonLogic, IGitUICommands commands, Func<IWin32Window?> owner)
        : IChecklistSettingsHost
    {
        private const string _putty = "PuTTY";
        private readonly CheckSettingsLogic _checkSettingsLogic = new(commonLogic);
        private DiffMergeToolConfigurationManager? _diffMergeToolConfigurationManager;

        public bool CheckAtStartup
        {
            get => AppSettings.CheckSettings;
            set => AppSettings.CheckSettings = value;
        }

        public void BeginChecks()
            => _diffMergeToolConfigurationManager = new DiffMergeToolConfigurationManager(() => commonLogic.GitConfigSettingsSet.EffectiveSettings);

        public ChecklistResult? Check(ChecklistCheck check) => check switch
        {
            ChecklistCheck.GitFound => CheckGitCmdValid(),
            ChecklistCheck.UserNameSet => SetOrUnset(
                string.IsNullOrEmpty(GetGlobalSetting(SettingKeyString.UserName)) || string.IsNullOrEmpty(GetGlobalSetting(SettingKeyString.UserEmail)),
                strings.NoEmailSet.Text,
                strings.EmailSet.Text),
            ChecklistCheck.MergeTool => CheckMergeTool(),
            ChecklistCheck.DiffTool => CheckDiffToolConfiguration(),
            ChecklistCheck.Translation => SetOrUnset(
                string.IsNullOrEmpty(AppSettings.Translation),
                strings.NoLanguageConfigured.Text,
                string.Format(strings.LanguageConfigured.Text, AppSettings.Translation)),
            ChecklistCheck.GitExtensionsInstall => OperatingSystem.IsWindows() ? CheckGitExtensionsInstall() : null,
            ChecklistCheck.ShellExtensionsRegistered => OperatingSystem.IsWindows() ? CheckGitExtensionRegistrySettings() : null,
            ChecklistCheck.GitBinFound => OperatingSystem.IsWindows() ? CheckGitExe() : null,
            ChecklistCheck.SshConfig => OperatingSystem.IsWindows() ? CheckSshSettings() : null,
            ChecklistCheck.GcmDetected => OperatingSystem.IsWindows() ? CheckGitCredentialWinStore() : null,
            _ => null,
        };

        public bool IsEditorConfigured() => !string.IsNullOrEmpty(commonLogic.GetGlobalEditor());

        public void ShowError(string text) => ShowMessage(text, TranslatedStrings.Error, MessageBoxIcon.Error);

        public void Repair(ChecklistCheck check, IChecklistActions actions)
        {
            switch (check)
            {
                case ChecklistCheck.GitFound:
                    // As GitFound_Click.
                    if (!CheckSettingsLogic.SolveGitCommand())
                    {
                        ShowMessage((OperatingSystem.IsWindows() ? strings.SolveGitCommandFailed : strings.SolveGitCommandFailedUnix).Text, strings.SolveGitCommandFailedCaption.Text, MessageBoxIcon.Error);
                        actions.GotoPage("GitSettingsPage");
                        return;
                    }

                    ShowMessage(string.Format(strings.GitCanBeRun.Text, AppSettings.GitCommandValue), strings.GitCanBeRunCaption.Text, MessageBoxIcon.Information);
                    actions.GotoPage("GitSettingsPage");
                    actions.SaveAndRescan();
                    break;

                case ChecklistCheck.UserNameSet:
                    actions.GotoPage("GitConfigSettingsPage");
                    break;

                case ChecklistCheck.MergeTool:
                case ChecklistCheck.DiffTool:
                    // As MergeToolFix_Click and DiffToolFix_Click.
                    string? tool = check == ChecklistCheck.MergeTool
                        ? _diffMergeToolConfigurationManager?.ConfiguredMergeTool
                        : _diffMergeToolConfigurationManager?.ConfiguredDiffTool;
                    if (string.IsNullOrEmpty(tool))
                    {
                        actions.GotoPage("GitConfigSettingsPage");
                        return;
                    }

                    actions.SaveAndRescan();
                    break;

                case ChecklistCheck.ShellExtensionsRegistered:
                    if (OperatingSystem.IsWindows())
                    {
                        ShellExtensionManager.Register();
                    }

                    actions.Rescan();
                    break;

                case ChecklistCheck.GitBinFound:
                    // As GitBinFound_Click.
                    if (!CheckSettingsLogic.SolveLinuxToolsDir())
                    {
                        ShowMessage(strings.LinuxToolsShNotFound.Text, strings.LinuxToolsShNotFoundCaption.Text, MessageBoxIcon.Error);
                        actions.GotoPage("GitSettingsPage");
                        return;
                    }

                    ShowMessage(string.Format(strings.ShCanBeRun.Text, AppSettings.LinuxToolsDir), strings.ShCanBeRunCaption.Text, MessageBoxIcon.Information);

                    // The settings are shown by the pages first, else the save would overwrite them.
                    actions.LoadAll();
                    actions.SaveAndRescan();
                    break;

                case ChecklistCheck.GitExtensionsInstall:
                    CheckSettingsLogic.SolveGitExtensionsDir();
                    actions.Rescan();
                    break;

                case ChecklistCheck.SshConfig:
                    // As SshConfig_Click.
                    if (GitSshHelpers.IsPlink && actions.AutoFindPuttyPaths())
                    {
                        ShowMessage(strings.PuttyFoundAuto.Text, _putty, MessageBoxIcon.Information);
                    }
                    else
                    {
                        actions.GotoPage("SshSettingsPage");
                    }

                    break;

                case ChecklistCheck.Translation:
                    // As translationConfig_Click: the chosen translation is set in the settings.
                    AvaloniaUi.RunInHostContext(() =>
                    {
                        TryShowChooseTranslation(owner());
                    });
                    actions.LoadAll();
                    actions.SaveAndRescan();
                    break;

                case ChecklistCheck.GcmDetected:
                    OsShellUtil.OpenUrlInDefaultBrowser(@"https://github.com/gitextensions/gitextensions/wiki/Fix-GitCredentialWinStore-missing");
                    break;
            }
        }

        private string? GetGlobalSetting(string name) => commonLogic.GitConfigSettingsSet.GlobalSettings.GetValue(name);

        private void ShowMessage(string text, string caption, MessageBoxIcon icon)
            => AvaloniaUi.RunInHostContext(() => MessageBoxes.Show(owner(), text, caption, MessageBoxButtons.OK, icon));

        private static ChecklistResult SetOrUnset(bool isUnset, string textUnset, string textSet)
            => isUnset ? new(ChecklistState.Unset, textUnset) : new(ChecklistState.Set, textSet);

        private ChecklistResult CheckGitCmdValid()
        {
            if (!_checkSettingsLogic.CanFindGitCmd())
            {
                return new(ChecklistState.Unset, strings.GitNotFound.Text);
            }

            IGitVersion nativeGitVersion = GitVersion.Current;
            IGitVersion usedGitVersion = commands.Module.IsValidGitWorkingDir() ? GitVersion.CurrentVersion(commands.Module.GitExecutable) : nativeGitVersion;
            string displayedVersion = nativeGitVersion == usedGitVersion ? $"{nativeGitVersion}" : $"{nativeGitVersion} / WSL {usedGitVersion}";

            if (usedGitVersion < GitVersion.LastSupportedVersion)
            {
                return new(ChecklistState.Unset, string.Format(strings.WrongGitVersion.Text, displayedVersion, GitVersion.LastRecommendedVersion));
            }

            if (usedGitVersion < GitVersion.LastRecommendedVersion)
            {
                return new(ChecklistState.NotRecommended, string.Format(strings.NotRecommendedGitVersion.Text, displayedVersion, GitVersion.LastRecommendedVersion));
            }

            return new(ChecklistState.Set, string.Format(strings.GitVersionFound.Text, displayedVersion));
        }

        private ChecklistResult CheckDiffToolConfiguration()
        {
            string? diffTool = _diffMergeToolConfigurationManager!.ConfiguredDiffTool;
            if (string.IsNullOrEmpty(diffTool) || string.IsNullOrWhiteSpace(_diffMergeToolConfigurationManager.GetToolCommand(diffTool, DiffMergeToolType.Diff)))
            {
                return new(ChecklistState.Unset, strings.AdviceDiffToolConfiguration.Text);
            }

            return new(ChecklistState.Set, string.Format(strings.DiffToolXConfigured.Text, diffTool));
        }

        private ChecklistResult CheckMergeTool()
        {
            string? mergeTool = _diffMergeToolConfigurationManager!.ConfiguredMergeTool;
            if (string.IsNullOrEmpty(mergeTool))
            {
                return new(ChecklistState.Unset, strings.ConfigureMergeTool.Text);
            }

            if (string.IsNullOrWhiteSpace(_diffMergeToolConfigurationManager.GetToolCommand(mergeTool, DiffMergeToolType.Merge)))
            {
                return new(ChecklistState.Unset, string.Format(strings.MergeToolXConfiguredNeedsCmd.Text, mergeTool));
            }

            return new(ChecklistState.Set, string.Format(strings.MergeToolXConfigured.Text, mergeTool));
        }

        private ChecklistResult CheckGitExtensionRegistrySettings()
        {
            if (!ShellExtensionManager.IsRegistered())
            {
                // Not an error when the shell extensions are not installed.
                return ShellExtensionManager.FilesExist()
                    ? new(ChecklistState.Unset, string.Format(strings.ShellExtNeedsToBeRegistered.Text, ShellExtensionManager.GitExtensionsShellEx32Name))
                    : new(ChecklistState.Set, strings.ShellExtNoInstalled.Text);
            }

            return new(ChecklistState.Set, strings.ShellExtRegistered.Text);
        }

        private ChecklistResult CheckGitExtensionsInstall()
        {
            string? installDir = AppSettings.GetInstallDir();
            if (string.IsNullOrEmpty(installDir))
            {
                return new(ChecklistState.Unset, strings.RegistryKeyGitExtensionsMissing.Text);
            }

            if (installDir.EndsWith(".exe") || !Directory.Exists(installDir)
                || (!System.Diagnostics.Debugger.IsAttached && installDir != AppSettings.GetGitExtensionsDirectory()))
            {
                return new(ChecklistState.Unset, strings.RegistryKeyGitExtensionsFaulty.Text);
            }

            return new(ChecklistState.Set, strings.RegistryKeyGitExtensionsCorrect.Text);
        }

        private ChecklistResult CheckGitExe()
            => SetOrUnset(
                !File.Exists(AppSettings.LinuxToolsDir + "sh.exe") && !File.Exists(AppSettings.LinuxToolsDir + "sh")
                    && !CheckSettingsLogic.CheckIfFileIsInPath("sh.exe") && !CheckSettingsLogic.CheckIfFileIsInPath("sh"),
                strings.LinuxToolsSshNotFound.Text,
                strings.LinuxToolsSshFound.Text);

        private ChecklistResult CheckSshSettings()
        {
            if (GitSshHelpers.IsPlink)
            {
                return SetOrUnset(
                    !File.Exists(AppSettings.Plink) || !File.Exists(AppSettings.Puttygen) || !File.Exists(AppSettings.Pageant),
                    strings.PlinkPuttyGenPageantNotFound.Text,
                    strings.PuttyConfigured.Text);
            }

            string ssh = AppSettings.SshPath;
            if (!string.IsNullOrEmpty(ssh) && !File.Exists(ssh))
            {
                return new(ChecklistState.Unset, string.Format(strings.SshClientNotFound.Text, ssh));
            }

            return new(ChecklistState.Set, string.IsNullOrEmpty(ssh) ? strings.OpensshUsed.Text : string.Format(strings.OtherSshClient.Text, ssh));
        }

        // The obsolete git-credential-winstore (see issue 3511), shown only when configured.
        private ChecklistResult? CheckGitCredentialWinStore()
            => (GetGlobalSetting(SettingKeyString.CredentialHelper) ?? "").Contains("git-credential-winstore.exe", StringComparison.OrdinalIgnoreCase)
                ? new(ChecklistState.Unset, strings.GcmDetectedCaption.Text)
                : null;
    }
}

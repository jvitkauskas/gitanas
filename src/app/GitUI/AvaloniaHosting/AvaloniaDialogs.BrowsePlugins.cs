using System.Drawing.Imaging;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;
using GitUI.Presentation.Translations;
using GitUIPluginInterfaces;
using Microsoft.VisualStudio.Threading;

namespace GitUI.AvaloniaHosting;

/// <summary>The plugins of the Avalonia main window: their menu, the git hosting menu, and their registration.</summary>
internal static partial class AvaloniaDialogs
{
    private static bool _pluginsLoaded;
    private static bool _pluginsLoading;

    /// <summary>Raised on the UI thread once the plugins are loaded.</summary>
    private static event EventHandler? PluginsLoaded;

    /// <summary>As <c>OnLoad</c> of FormBrowse: the plugins are loaded once, in the background (<c>PluginRegistry.InitializeAll</c>).</summary>
    private static void EnsurePluginsLoading()
    {
        if (_pluginsLoaded || _pluginsLoading)
        {
            return;
        }

        _pluginsLoading = true;
        ThreadHelper.FileAndForget(async () =>
        {
            await TaskScheduler.Default;
            PluginRegistry.InitializeAll();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            _pluginsLoading = false;
            _pluginsLoaded = true;
            PluginsLoaded?.Invoke(null, EventArgs.Empty);
        });
    }

    private sealed partial class BrowseHost : IBrowsePluginsHost
    {
        private EventHandler? _pluginsChanged;
        private bool _pluginsRegistered;

        public event EventHandler? PluginsChanged
        {
            add
            {
                if (_pluginsChanged is null)
                {
                    PluginsLoaded += OnPluginsLoaded;
                    EnsurePluginsLoading();
                }

                _pluginsChanged += value;
            }

            remove => _pluginsChanged -= value;
        }

        public IReadOnlyList<BrowsePlugin>? Plugins
        {
            get
            {
                if (!_pluginsLoaded)
                {
                    return null;
                }

                RegisterPlugins();
                lock (PluginRegistry.Plugins)
                {
                    return [.. PluginRegistry.Plugins.Select(plugin => new BrowsePlugin(plugin.Name ?? "", GetPluginIcon(plugin), plugin is IGitPluginForRepository, plugin))];
                }
            }
        }

        public string? RepositoryHostName => _pluginsLoaded && PluginRegistry.GitHosters.Count != 0 ? PluginRegistry.GitHosters[0].Name : null;

        // As the click of a plugin item of FormBrowse, with the Avalonia window as the owner (plugin API v2); a plugin of API
        // v1 gets it as OwnerForm (a wrapper of its handle).
        public void RunPlugin(BrowsePlugin plugin) => AvaloniaUi.RunInHostContext(() =>
        {
            if (((IGitPlugin)plugin.Plugin).Execute(CreatePluginEventArgs()))
            {
                _commands.RepoChangedNotifier.Notify();
            }
        });

        /// <summary>The arguments of the plugins run from this window.</summary>
        internal GitUIEventArgs CreatePluginEventArgs() => new(new WindowOwner(_window.OwnerHandle), _commands);

        public void OpenPluginSettings() => AvaloniaUi.RunInHostContext(() => _commands.StartPluginSettingsDialog(Owner));

        public void RunRepositoryHostCommand(RepositoryHostCommand command) => AvaloniaUi.RunInHostContext(() =>
        {
            BrowsePluginStrings strings = ViewStrings.Load<BrowsePluginStrings>();
            NativeWindowOwner owner = Owner;
            if (command == RepositoryHostCommand.ForkClone)
            {
                // As _forkCloneMenuItem_Click: the clone is shown in this window.
                if (PluginRegistry.GitHosters.Count > 0)
                {
                    _commands.StartCloneForkFromHoster(owner, PluginRegistry.GitHosters[0], (_, e) => _session.SetGitModule(e.GitModule));
                    RepositoryChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    MessageBoxes.ShowError(owner, strings.NoRepositoryHostPluginLoaded.Text, TranslatedStrings.Error);
                }

                return;
            }

            // As TryGetRepositoryHost.
            if (PluginRegistry.TryGetGitHosterForModule(Module) is not { } repoHost)
            {
                MessageBoxes.Show(owner, strings.NoRepositoryHostFound.Text, TranslatedStrings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            switch (command)
            {
                case RepositoryHostCommand.ViewPullRequests:
                    _commands.StartPullRequestsDialog(owner, repoHost);
                    break;
                case RepositoryHostCommand.CreatePullRequest:
                    _commands.StartCreatePullRequest(owner, repoHost);
                    break;
                case RepositoryHostCommand.AddUpstreamRemote:
                    _commands.AddUpstreamRemote(owner, repoHost);
                    break;
            }
        });

        // As RegisterPlugins: the plugins are registered for the commands of this repository (and unregistered by Dispose,
        // as SetGitModule and OnFormClosed do).
        private void RegisterPlugins()
        {
            if (_pluginsRegistered || !_pluginsLoaded)
            {
                return;
            }

            _pluginsRegistered = true;
            PluginRegistry.Register(_commands);
            _commands.RaisePostRegisterPlugin(Owner);
        }

        private void UnregisterPlugins()
        {
            PluginsLoaded -= OnPluginsLoaded;
            if (_pluginsRegistered)
            {
                _pluginsRegistered = false;
                PluginRegistry.Unregister(_commands);
            }
        }

        private void OnPluginsLoaded(object? sender, EventArgs e) => _pluginsChanged?.Invoke(this, EventArgs.Empty);
    }
}

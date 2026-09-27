using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Threading;
using GitCommands.Git;
using GitUI.Avalonia.CommandsDialogs;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.CommandsDialogs;

namespace GitExtensions.WaylandSmoke;

/// <summary>Opt-in native-platform test host; never loaded by the application.</summary>
internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            throw new ArgumentException("Usage: WaylandSmoke <report.json> <command.txt> <control-theme> <light|dark>");
        }

        string reportPath = Path.GetFullPath(args[0]);
        string commandPath = Path.GetFullPath(args[1]);
        AvaloniaUi.EnsureInitialized(() => new AvaloniaUiOptions(args[3] == "dark", "DejaVu Sans", 12, ControlTheme: args[2]));
        RenameBranchWindow root = CreateWindow("Git Extensions Wayland QA");
        RenameBranchWindow? modal = null;
        Task? modalTask = null;
        HashSet<double> scales = [];
        string previousCommand = string.Empty;
        using CancellationTokenSource stop = new(TimeSpan.FromSeconds(90));
        DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += (_, _) =>
        {
            string command = File.Exists(commandPath) ? File.ReadAllText(commandPath).Trim() : string.Empty;
            if (command != previousCommand)
            {
                previousCommand = command;
                switch (command)
                {
                    case "modal":
                        modal = CreateWindow("Git Extensions Wayland QA modal");
                        modalTask = modal.ShowDialog(root);
                        break;
                    case "close-modal":
                        modal?.Close();
                        modal = null;
                        break;
                    case "stop":
                        stop.Cancel();
                        break;
                }
            }

            scales.Add(root.RenderScaling);
            string json = JsonSerializer.Serialize(new
            {
                Theme = args[2],
                Color = args[3],
                Command = previousCommand,
                Root = Describe(root),
                Modal = modal is null ? null : Describe(modal),
                ModalOwnedByRoot = modal is not null && modal.Owner == root && root.OwnedWindows.Contains(modal),
                ModalTaskCompleted = modalTask?.IsCompleted,
                ObservedScales = scales.Order().ToArray(),
            });
            File.WriteAllText(reportPath + ".tmp", json);
            File.Move(reportPath + ".tmp", reportPath, overwrite: true);
        };
        root.Show();
        timer.Start();
        try
        {
            AvaloniaUi.RunMainLoop(stop.Token);
        }
        finally
        {
            timer.Stop();
            modal?.Close();
            root.Close();
        }
    }

    private static object Describe(Window window) => new
    {
        window.RenderScaling,
        window.ClientSize,
        window.IsEffectivelyEnabled,
        window.IsVisible,
    };

    private static RenameBranchWindow CreateWindow(string title) => new()
    {
        DataContext = new RenameBranchViewModel(new RenameBranchStrings(), "feature/scale-test",
            new ProbeNormaliser(), new GitBranchNameOptions("_"), false, (_, _) => true),
        Title = title,
    };

    private sealed class ProbeNormaliser : IGitBranchNameNormaliser
    {
        public string Normalise(string? branchName, GitBranchNameOptions options) => branchName ?? string.Empty;
    }
}

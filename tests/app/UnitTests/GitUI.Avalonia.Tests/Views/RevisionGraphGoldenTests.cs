using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using GitCommands;
using GitExtensions.Extensibility.Git;
using GitUI.Avalonia.Controls.RevisionGrid;
using GitUI.UserControls.RevisionGrid.Graph;
using GitUIPluginInterfaces;
using SkiaSharp;

namespace GitUI.AvaloniaTests.Views;

/// <summary>
/// Saved-image regression coverage inspired by Nikola's Avalonia port (#13189).
/// Uses our renderer, fixed colors and no fonts, so the fixtures work across control themes and operating systems.
/// </summary>
[TestFixture]
[NonParallelizable]
public sealed class RevisionGraphGoldenTests : HeadlessTest
{
    [Test]
    public Task Graph_matches_reviewed_image(
        [Values("linear", "merge", "crossing")] string topology,
        [Values(false, true)] bool dark,
        [Values(1.0, 1.25, 1.5, 2.0)] double scale) => OnUiThreadAsync(() =>
    {
        bool merge = AppSettings.MergeGraphLanesHavingCommonParent.Value;
        bool diagonals = AppSettings.RenderGraphWithDiagonals.Value;
        bool straighten = AppSettings.StraightenGraphDiagonals.Value;
        int limit = AppSettings.StraightenGraphSegmentsLimit.Value;
        try
        {
            AppSettings.MergeGraphLanesHavingCommonParent.Value = true;
            AppSettings.RenderGraphWithDiagonals.Value = true;
            AppSettings.StraightenGraphDiagonals.Value = true;
            AppSettings.StraightenGraphSegmentsLimit.Value = 80;
            RevisionGraph graph = BuildGraph(topology);
            GraphSheet sheet = new(graph, dark) { Width = 96, Height = graph.Count * 26 };
            sheet.Measure(new Size(sheet.Width, sheet.Height));
            sheet.Arrange(new Rect(0, 0, sheet.Width, sheet.Height));
            using RenderTargetBitmap bitmap = new(
                PixelSize.FromSize(sheet.Bounds.Size, scale), new Vector(96 * scale, 96 * scale));
            bitmap.Render(sheet);
            using MemoryStream stream = new();
            bitmap.Save(stream, new PngBitmapEncoderOptions());
            string name = $"graph-{topology}-{(dark ? "dark" : "light")}-{scale * 100:0}";
            VerifyImage(name, stream.ToArray());
        }
        finally
        {
            AppSettings.MergeGraphLanesHavingCommonParent.Value = merge;
            AppSettings.RenderGraphWithDiagonals.Value = diagonals;
            AppSettings.StraightenGraphDiagonals.Value = straighten;
            AppSettings.StraightenGraphSegmentsLimit.Value = limit;
        }
    });

    private static RevisionGraph BuildGraph(string topology)
    {
        GitRevision[] revisions = topology switch
        {
            "linear" => [Commit('1', '2'), Commit('2', '3'), Commit('3', '4'), Commit('4')],
            "merge" => [Commit('1', '2', '3'), Commit('2', '4'), Commit('3', '4'), Commit('4')],
            _ => [Commit('1', '2', '3'), Commit('a', '3'), Commit('2', '4'), Commit('3', '4'), Commit('4')],
        };
        RevisionGraph graph = new() { HeadId = Id('1') };
        foreach (GitRevision revision in revisions)
        {
            graph.Add(revision);
        }

        graph.HighlightBranch(graph.HeadId);
        graph.LoadingCompleted();
        graph.CacheTo(graph.Count - 1, graph.Count - 1);
        return graph;
    }

    private static ObjectId Id(char digit) => ObjectId.Parse(new string(digit, ObjectId.Sha1CharCount));

    private static GitRevision Commit(char id, params char[] parents) => new(Id(id)) { ParentIds = parents.Select(Id).ToArray() };

    private static void VerifyImage(string name, byte[] actual, [CallerFilePath] string sourcePath = "")
    {
        string baseline = Path.Combine(Path.GetDirectoryName(sourcePath)!, "..", "GoldenImages", name + ".png");
        if (Environment.GetEnvironmentVariable("GE_UPDATE_GOLDENS") == "1")
        {
            File.WriteAllBytes(baseline, actual);
            Assert.Inconclusive("Reference image generated; visually review it and rerun without GE_UPDATE_GOLDENS.");
        }

        string artifact = Path.Combine(TestContext.CurrentContext.WorkDirectory, "screenshots", name + "-actual.png");
        Directory.CreateDirectory(Path.GetDirectoryName(artifact)!);
        File.WriteAllBytes(artifact, actual);
        TestContext.AddTestAttachment(artifact);
        File.Exists(baseline).Should().BeTrue("the reviewed reference must exist; missing baselines never pass");
        using SKBitmap expected = SKBitmap.Decode(baseline);
        using SKBitmap rendered = SKBitmap.Decode(actual);
        rendered.Width.Should().Be(expected.Width);
        rendered.Height.Should().Be(expected.Height);
        int badPixels = 0;
        using SKBitmap difference = new(expected.Width, expected.Height);
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                SKColor a = expected.GetPixel(x, y);
                SKColor b = rendered.GetPixel(x, y);
                bool differs = Math.Abs(a.Red - b.Red) > 8 || Math.Abs(a.Green - b.Green) > 8
                    || Math.Abs(a.Blue - b.Blue) > 8 || Math.Abs(a.Alpha - b.Alpha) > 8;
                if (differs)
                {
                    badPixels++;
                }

                difference.SetPixel(x, y, differs ? SKColors.Magenta : SKColors.Transparent);
            }
        }

        double badShare = (double)badPixels / (expected.Width * expected.Height);
        if (badShare > 0.002)
        {
            string diffPath = Path.ChangeExtension(artifact, ".diff.png");
            using SKData encoded = difference.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(diffPath, encoded.ToArray());
            TestContext.AddTestAttachment(diffPath);
        }

        badShare.Should().BeLessThanOrEqualTo(0.002, "graph geometry and colors must match (allowing minor Skia antialiasing differences)");
    }

    private sealed class GraphSheet(RevisionGraph graph, bool dark) : Control
    {
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(dark ? Brushes.Black : Brushes.White, new Rect(Bounds.Size));
            RevisionGraphRenderer renderer = new(
                [Brushes.DodgerBlue, Brushes.DarkOrange, Brushes.MediumPurple, Brushes.SeaGreen],
                Brushes.Gray, dark ? Brushes.White : Brushes.Black);
            for (int row = 0; row < graph.Count; row++)
            {
                using (context.PushTransform(Matrix.CreateTranslation(0, row * 26)))
                using (context.PushClip(new Rect(0, 0, Bounds.Width, 26)))
                {
                    renderer.DrawRow(context, true, true, row, 26, graph.GetSegmentsForRow,
                        RevisionGraphDrawStyle.DrawNonRelativesGray, graph.HeadId);
                }
            }
        }
    }
}

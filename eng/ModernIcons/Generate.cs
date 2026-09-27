#:package SkiaSharp@3.119.4
#:package SkiaSharp.NativeAssets.Linux@3.119.4
#:property PublishAot=false

// Generates the icons of the Modern theme (docs/avalonia-port/MODERN.md) from Octicons (OCTICONS-LICENSE):
// src/app/GitUI.Avalonia/Assets/Modern/{Light,Dark}/<name>.png, 32 pixels (16 at 2x), for each line of icons.txt.
//   dotnet run eng/ModernIcons/Generate.cs
using System.Text.Json;
using SkiaSharp;

string here = Path.GetDirectoryName(Path.GetFullPath(ThisFile()))!;
string target = Path.GetFullPath(Path.Combine(here, "../../src/app/GitUI.Avalonia/Assets/Modern"));
// The Octicons, and the icons of custom-16.json (e.g. the Git mark, which Octicons has not), on the same 16 px grid.
JsonSerializerOptions json = new() { PropertyNameCaseInsensitive = true };
Dictionary<string, Octicon> octicons = JsonSerializer.Deserialize<Dictionary<string, Octicon>>(File.ReadAllText(Path.Combine(here, "octicons-16.json")), json)!;
foreach ((string name, Octicon icon) in JsonSerializer.Deserialize<Dictionary<string, Octicon>>(File.ReadAllText(Path.Combine(here, "custom-16.json")), json)!)
{
    octicons.Add(name, icon);
}

Dictionary<string, Dictionary<string, SKColor>> palettes = new()
{
    ["Light"] = new()
    {
        ["fg"] = SKColor.Parse("#3A3A3F"), ["gray"] = SKColor.Parse("#8E8E93"), ["green"] = SKColor.Parse("#1A7F37"),
        ["red"] = SKColor.Parse("#CF222E"), ["orange"] = SKColor.Parse("#BC4C00"), ["yellow"] = SKColor.Parse("#9A6700"),
        ["blue"] = SKColor.Parse("#0969DA"), ["purple"] = SKColor.Parse("#8250DF"),
    },
    ["Dark"] = new()
    {
        ["fg"] = SKColor.Parse("#D2D2D7"), ["gray"] = SKColor.Parse("#8E8E93"), ["green"] = SKColor.Parse("#3FB950"),
        ["red"] = SKColor.Parse("#F85149"), ["orange"] = SKColor.Parse("#DB6D28"), ["yellow"] = SKColor.Parse("#D29922"),
        ["blue"] = SKColor.Parse("#58A6FF"), ["purple"] = SKColor.Parse("#A371F7"),
    },
};

// Plugin API v3 also needs a self-contained PNG. A neutral gray stays readable on
// both light and dark backgrounds in hosts that cannot use named theme assets.
Dictionary<string, string> pluginFiles = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(here, "plugins.json")))!;
palettes["Plugin"] = palettes["Light"].ToDictionary(pair => pair.Key, _ => SKColor.Parse("#767676"));

const int Size = 32;
const float Scale = Size / 16f;
int count = 0;
foreach (string raw in File.ReadLines(Path.Combine(here, "icons.txt")))
{
    string line = raw.Split('#')[0].Trim();
    if (line.Length == 0)
    {
        continue;
    }

    string[] parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
    string name = parts[0];
    if (args.Contains("--plugins-only") && !pluginFiles.ContainsKey(name))
    {
        continue;
    }

    string[] tokens = parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
    string icon = tokens[0];
    string color = tokens.Skip(1).FirstOrDefault(t => !t.StartsWith('+')) ?? "fg";
    string? badge = tokens.FirstOrDefault(t => t.StartsWith('+'))?[1..];
    string? badgeColor = null;
    if (badge?.Split(':') is [string badgeName, string c])
    {
        (badge, badgeColor) = (badgeName, c);
    }

    foreach ((string variant, Dictionary<string, SKColor> palette) in palettes)
    {
        if (variant == "Plugin" && !pluginFiles.ContainsKey(name))
        {
            continue;
        }

        using SKBitmap bitmap = new(new SKImageInfo(Size, Size, SKColorType.Rgba8888, SKAlphaType.Unpremul));
        using (SKCanvas canvas = new(bitmap))
        {
            canvas.Clear(SKColors.Transparent);
            canvas.Scale(Scale);
            Draw(canvas, octicons[icon], palette[color]);
            if (badge is not null)
            {
                // The badge at the bottom right, over a cut-out of the icon.
                using SKPaint clear = new() { BlendMode = SKBlendMode.Clear, IsAntialias = true };
                canvas.DrawCircle(12, 12, 5, clear);
                canvas.Translate(8, 8);
                canvas.Scale(0.5f);
                Draw(canvas, octicons[badge], palette[badgeColor ?? color]);
            }
        }

        // The mark of the Modern icons (ImageLightness does not adapt them to the dark theme): invisible alphas in a corner.
        if (variant != "Plugin")
        {
            bitmap.SetPixel(0, 0, new SKColor(0, 0, 0, 1));
            bitmap.SetPixel(1, 0, new SKColor(0, 0, 0, 2));
            bitmap.SetPixel(0, 1, new SKColor(0, 0, 0, 3));
        }

        string file = variant == "Plugin"
            ? Path.GetFullPath(Path.Combine(here, "../../src/plugins", pluginFiles[name]))
            : Path.Combine(target, variant, name + ".png");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using FileStream stream = File.Create(file);
        bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
    }

    count++;
}

Console.WriteLine($"{count} icons in {target}");

static void Draw(SKCanvas canvas, Octicon icon, SKColor color)
{
    bool cuts = icon.Cuts is not null || icon.CutStrokes is not null;
    if (cuts)
    {
        canvas.SaveLayer();
    }

    using SKPaint paint = new() { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
    foreach (string data in icon.Paths)
    {
        using SKPath path = SKPath.ParseSvgPathData(data);
        path.FillType = icon.EvenOdd ? SKPathFillType.EvenOdd : SKPathFillType.Winding;
        canvas.DrawPath(path, paint);
    }

    // The shapes cut out of a custom icon: filled, and stroked (1.25 wide).
    using SKPaint cut = new() { BlendMode = SKBlendMode.Clear, IsAntialias = true, Style = SKPaintStyle.Fill };
    using SKPaint cutStroke = new() { BlendMode = SKBlendMode.Clear, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.25f, StrokeCap = SKStrokeCap.Round };
    foreach ((string[]? paths, SKPaint clear) in new[] { (icon.Cuts, cut), (icon.CutStrokes, cutStroke) })
    {
        foreach (string data in paths ?? [])
        {
            using SKPath path = SKPath.ParseSvgPathData(data);
            canvas.DrawPath(path, clear);
        }
    }

    if (cuts)
    {
        canvas.Restore();
    }
}

static string ThisFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

record Octicon(bool EvenOdd, string[] Paths, string[]? Cuts = null, string[]? CutStrokes = null);

using System.Net;
using System.Text;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;

namespace GitUI.Avalonia.Controls;

/// <summary>
///  Shows the XHTML subset of the commit renderers (<c>&lt;a href&gt;</c>, <c>&lt;b&gt;</c>, <c>&lt;i&gt;</c>, <c>&lt;u&gt;</c>,
///  <c>&lt;s&gt;</c>, <c>&lt;br/&gt;</c>, <c>&lt;p&gt;</c>), as the WinForms <c>RichTextBoxXhtmlSupportExtension.SetXHTMLText</c>
///  shows it in a RichTextBox. The text is selectable and a click on a link raises <see cref="LinkClicked"/>.
/// </summary>
public sealed class XhtmlTextBlock : SelectableTextBlock
{
    public static readonly StyledProperty<string?> XhtmlProperty = AvaloniaProperty.Register<XhtmlTextBlock, string?>(nameof(Xhtml));

    /// <summary>Raised (bubbling) when the user clicks a link.</summary>
    public static readonly RoutedEvent<LinkClickedEventArgs> LinkClickedEvent
        = RoutedEvent.Register<XhtmlTextBlock, LinkClickedEventArgs>(nameof(LinkClicked), RoutingStrategies.Bubble);

    private readonly List<(int Start, int End, string Uri)> _links = [];
    private Point? _pressedAt;

    public XhtmlTextBlock()
    {
        TextWrapping = TextWrapping.Wrap;
    }

    protected override Type StyleKeyOverride => typeof(SelectableTextBlock);

    /// <summary>The XHTML to show.</summary>
    public string? Xhtml
    {
        get => GetValue(XhtmlProperty);
        set => SetValue(XhtmlProperty, value);
    }

    /// <summary>Raised when the user clicks a link, with its URI.</summary>
    public event EventHandler<LinkClickedEventArgs>? LinkClicked
    {
        add => AddHandler(LinkClickedEvent, value);
        remove => RemoveHandler(LinkClickedEvent, value);
    }

    /// <summary>The links shown, as ranges of the text, e.g. for tests.</summary>
    public IReadOnlyList<(int Start, int End, string Uri)> Links => _links;

    /// <summary>Raises <see cref="LinkClicked"/> for the link at the position of the text, if any; returns whether there is one.</summary>
    public bool ClickLinkAt(int textPosition)
    {
        foreach ((int start, int end, string uri) in _links)
        {
            if (textPosition >= start && textPosition < end)
            {
                RaiseEvent(new LinkClickedEventArgs(LinkClickedEvent, uri));
                return true;
            }
        }

        return false;
    }

    /// <summary>The link at the position (in this control's coordinates), if any.</summary>
    public string? GetLinkAt(Point position)
        => GetTextPosition(position) is int textPosition
            ? _links.FirstOrDefault(link => textPosition >= link.Start && textPosition < link.End).Uri
            : null;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == XhtmlProperty)
        {
            Build(Xhtml ?? "");
        }
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pressedAt = e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ? e.GetPosition(this) : null;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // A click, not the end of a selection.
        if (_pressedAt is { } pressedAt && e.InitialPressMouseButton == MouseButton.Left)
        {
            Point position = e.GetPosition(this);
            if (Math.Abs(position.X - pressedAt.X) < 4 && Math.Abs(position.Y - pressedAt.Y) < 4 && GetTextPosition(position) is int textPosition)
            {
                ClickLinkAt(textPosition);
            }
        }

        _pressedAt = null;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        bool overLink = GetTextPosition(e.GetPosition(this)) is int textPosition && _links.Any(link => textPosition >= link.Start && textPosition < link.End);
        Cursor = overLink ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
    }

    private int? GetTextPosition(Point position)
    {
        if (TextLayout is null)
        {
            return null;
        }

        TextHitTestResult hit = TextLayout.HitTestPoint(position - new Point(Padding.Left, Padding.Top));
        return hit.IsInside ? hit.TextPosition : null;
    }

    private void Build(string xhtml)
    {
        _links.Clear();
        InlineCollection inlines = [];
        StringBuilder text = new();
        try
        {
            XmlReaderSettings settings = new() { DtdProcessing = DtdProcessing.Ignore, IgnoreComments = true, ConformanceLevel = ConformanceLevel.Fragment };
            using XmlReader reader = XmlReader.Create(new StringReader(xhtml), settings);
            Stack<(string Tag, string? Uri)> open = [];
            while (reader.Read())
            {
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        string tag = reader.Name.ToLowerInvariant();
                        if (tag == "br")
                        {
                            Add("\n");
                        }
                        else if (!reader.IsEmptyElement)
                        {
                            if (tag == "p" && text.Length > 0)
                            {
                                Add("\n");
                            }

                            open.Push((tag, tag == "a" ? reader.GetAttribute("href") : null));
                        }

                        break;

                    case XmlNodeType.EndElement:
                        if (open.Count > 0)
                        {
                            open.Pop();
                        }

                        break;

                    case XmlNodeType.Text:
                    case XmlNodeType.Whitespace:
                    case XmlNodeType.SignificantWhitespace:
                    case XmlNodeType.CDATA:
                        Add(reader.Value);
                        break;
                }
            }

            void Add(string value)
            {
                value = value.Replace("\r\n", "\n");
                if (value.Length == 0)
                {
                    return;
                }

                Run run = new(value);
                string? uri = null;
                foreach ((string openTag, string? href) in open)
                {
                    switch (openTag)
                    {
                        case "b":
                            run.FontWeight = FontWeight.Bold;
                            break;
                        case "i":
                            run.FontStyle = FontStyle.Italic;
                            break;
                        case "u":
                            run.TextDecorations = global::Avalonia.Media.TextDecorations.Underline;
                            break;
                        case "s":
                            run.TextDecorations = global::Avalonia.Media.TextDecorations.Strikethrough;
                            break;
                        case "a":
                            uri ??= href;
                            break;
                    }
                }

                if (uri is not null)
                {
                    run.TextDecorations = global::Avalonia.Media.TextDecorations.Underline;

                    // Resolve after attachment too, and follow later theme/resource changes.
                    run.Bind(TextElement.ForegroundProperty, new DynamicResourceExtension("SystemControlHyperlinkTextBrush"));
                    _links.Add((text.Length, text.Length + value.Length, uri));
                }

                text.Append(value);
                inlines.Add(run);
            }
        }
        catch (XmlException)
        {
            // Not well formed: the text without the markup (as the WinForms fallback).
            _links.Clear();
            inlines = [new Run(WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Replace(xhtml, "<[^>]*>", "")))];
        }

        Inlines = inlines;
    }
}

/// <summary>The link clicked in an <see cref="XhtmlTextBlock"/>.</summary>
public sealed class LinkClickedEventArgs(RoutedEvent routedEvent, string uri) : RoutedEventArgs(routedEvent)
{
    public string Uri { get; } = uri;
}

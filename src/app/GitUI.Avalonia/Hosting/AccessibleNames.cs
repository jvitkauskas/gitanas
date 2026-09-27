using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;

namespace GitUI.Avalonia.Hosting;

/// <summary>
///  Gives the buttons whose content is a panel (an image and a text) the name of their text, as the accessible name of a
///  WinForms button is its text; the automation peer would otherwise name them after the type of the panel
///  ("Avalonia.Controls.StackPanel"). A button with an image only is named after its tooltip.
/// </summary>
public static class AccessibleNames
{
    /// <summary>Names the buttons of <paramref name="root"/> and its descendants that have no name.</summary>
    public static void Apply(ILogical root)
    {
        foreach (Control control in root.GetSelfAndLogicalDescendants().OfType<Control>())
        {
            // Stable XAML names also identify inputs and lists to accessibility/automation clients.
            // Explicit metadata always wins, including identifiers supplied by a reusable view.
            if (!control.IsSet(AutomationProperties.AutomationIdProperty) && !string.IsNullOrEmpty(control.Name))
            {
                control.SetValue(AutomationProperties.AutomationIdProperty, control.Name);
            }

            if (!control.IsSet(AutomationProperties.HelpTextProperty) && ToolTip.GetTip(control) is string)
            {
                control.Bind(AutomationProperties.HelpTextProperty, control.GetObservable(ToolTip.TipProperty).Select(tip => tip as string));
            }

            if (control is not (Button or ToggleButton) || ((ContentControl)control).Content is not Control content || control.IsSet(AutomationProperties.NameProperty))
            {
                continue;
            }

            if (content.GetSelfAndLogicalDescendants().OfType<TextBlock>().FirstOrDefault() is { } text)
            {
                // Follows the text (e.g. "Commit & push" or "Commit & force push"), without the access key marker.
                control.Bind(AutomationProperties.NameProperty, text.GetObservable(TextBlock.TextProperty).Select(t => text is AccessText ? RemoveAccessKey(t) : t));
            }
            else if (ToolTip.GetTip(control) is string)
            {
                control.Bind(AutomationProperties.NameProperty, control.GetObservable(ToolTip.TipProperty).Select(tip => tip as string));
            }
        }
    }

    /// <summary>The text shown for an access text: "_Commit" is "Commit", "a__b" is "a_b".</summary>
    public static string? RemoveAccessKey(string? text)
    {
        if (text is null)
        {
            return null;
        }

        System.Text.StringBuilder result = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '_' && i + 1 < text.Length)
            {
                i++;
            }

            result.Append(text[i]);
        }

        return result.ToString();
    }

    private static IObservable<TResult> Select<TSource, TResult>(this IObservable<TSource> source, Func<TSource, TResult> selector)
        => new SelectObservable<TSource, TResult>(source, selector);

    private sealed class SelectObservable<TSource, TResult>(IObservable<TSource> source, Func<TSource, TResult> selector) : IObservable<TResult>
    {
        public IDisposable Subscribe(IObserver<TResult> observer) => source.Subscribe(new Observer(observer, selector));

        private sealed class Observer(IObserver<TResult> observer, Func<TSource, TResult> selector) : IObserver<TSource>
        {
            public void OnCompleted() => observer.OnCompleted();

            public void OnError(Exception error) => observer.OnError(error);

            public void OnNext(TSource value) => observer.OnNext(selector(value));
        }
    }
}

// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Full.cs" company="NBug Project">
//   Copyright (c) 2011 - 2013 Teoman Soygul. Licensed under MIT license.
// </copyright>
// <copyright file="BugReportForm.cs" company="Git Extensions">
//   Copyright (c) 2019 Igor Velikorossov. Licensed under MIT license.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using BugReporter.Serialization;
using GitCommands;
using GitExtensions.Extensibility;
using GitExtUtils;
using GitUI.Avalonia.BugReporter;
using GitUI.Avalonia.Hosting;
using GitUI.Presentation.BugReporter;
using GitUI.Presentation.Services;
using GitUI.Presentation.Translations;
using Report = BugReporter.Info.Report;

namespace BugReporter;

/// <summary>The bug report dialog (the Avalonia port of <c>BugReportForm</c>), for the application and for BugReporter.exe.</summary>
public static class BugReportDialog
{
    private const string NewIssueUrl = "https://github.com/jvitkauskas/gitanas/issues/new";

    private static readonly IErrorReportUrlBuilder _errorReportBodyBuilder = new ErrorReportUrlBuilder();
    private static readonly GitHubUrlBuilder _urlBuilder = new(_errorReportBodyBuilder);

    /// <summary>Shows the report of <paramref name="exception"/> modally over <paramref name="owner"/>.</summary>
    /// <param name="getOptions">The theme and fonts of Avalonia, if it is not set up yet.</param>
    /// <param name="canIgnore">Whether the application can go on (not terminating): the dialog can then be closed or ignored.</param>
    /// <param name="showIgnore">Whether the Ignore button is shown (for an external operation).</param>
    /// <param name="focusDetails">Whether the details are shown first, and the report cannot be sent (a user external operation).</param>
    public static BugReportResult Show(IWin32Window? owner, Func<AvaloniaUiOptions> getOptions, SerializableException exception, string exceptionInfo,
        string environmentInfo, bool canIgnore, bool showIgnore, bool focusDetails)
    {
        AvaloniaUi.EnsureInitialized(getOptions);

        Report report = new(exception);
        Info.GeneralInfo info = report.GeneralInfo!;

        BugReportWindow window = new();
        BugReportViewModel viewModel = new(
            ViewStrings.Load<BugReportStrings>(),
            new BugReportHost(exception, exceptionInfo, environmentInfo),
            new MessageBoxService(window),
            new BugReportInfo(
                HostApplication: info.HostApplication ?? "",
                ExceptionType: exception.Type ?? "",
                ExceptionMessage: exception.Message ?? "",
                TargetSite: exception.TargetSite ?? "",
                Application: $"{info.HostApplication} [{info.HostApplicationVersion}]",
                GitVersion: info.GitVersion ?? "",
                DateTime: info.DateTime ?? "",
                ClrVersion: info.ClrVersion ?? ""),
            ToReportException(exception),
            canIgnore,
            showIgnore,
            focusDetails);
        window.DataContext = viewModel;
        AvaloniaDialogHost.ShowDialog(window, owner?.Handle ?? 0);
        return viewModel.Result;
    }

    /// <summary>The exception with its properties and inner exceptions, as <c>ExceptionDetails</c> lists them.</summary>
    public static BugReportException ToReportException(SerializableException exception)
    {
        List<BugReportProperty> properties = [];

        void Add(string name, string? value)
        {
            if (value is not null)
            {
                properties.Add(new BugReportProperty(name, value));
            }
        }

        Add("Exception", exception.Type);
        Add("Message", exception.Message);
        Add("Target Site", exception.TargetSite);
        Add("Inner Exception", exception.InnerException?.Type);
        Add("Source", exception.Source);
        Add("Help Link", exception.HelpLink);
        Add("Stack Trace", exception.StackTrace);

        if (exception.Data is not null)
        {
            foreach (KeyValuePair<object, object> pair in exception.Data)
            {
                properties.Add(new BugReportProperty($"Data[\"{pair.Key}\"]", pair.Value?.ToString() ?? ""));
            }
        }

        if (exception.ExtendedInformation is not null)
        {
            foreach (KeyValuePair<string, object> info in exception.ExtendedInformation)
            {
                properties.Add(new BugReportProperty(info.Key, info.Value?.ToString() ?? "", IsExtended: true));
            }
        }

        List<BugReportException> innerExceptions = [];
        if (exception.InnerException is not null)
        {
            innerExceptions.Add(ToReportException(exception.InnerException));
        }

        if (exception.InnerExceptions is not null)
        {
            innerExceptions.AddRange(exception.InnerExceptions.Select(ToReportException));
        }

        return new BugReportException(exception.Type ?? "", properties, innerExceptions);
    }

    private sealed class BugReportHost(SerializableException exception, string exceptionInfo, string environmentInfo) : IBugReportHost
    {
        public string? BuildReportUrl(string description)
            => _urlBuilder.Build(NewIssueUrl, exception, exceptionInfo, environmentInfo, description);

        public string GetReportText(string description)
            => _errorReportBodyBuilder.CopyText(exception, exceptionInfo, environmentInfo, description);

        public void OpenUrl(string url)
            => new Executable(url).Start(useShellExecute: true, throwOnErrorExit: false);

        public void CopyToClipboard(string text) => ClipboardUtil.TrySetText(text);
    }

    /// <summary>The message boxes of the dialog, owned by its window.</summary>
    private sealed class MessageBoxService(DialogWindow window) : IMessageBoxService, IWin32Window
    {
        public nint Handle => window.NativeHandle;

        public void ShowError(string text, string caption)
            => MessageBoxes.Show(this, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Error);

        public void ShowInformation(string text, string caption)
            => MessageBoxes.Show(this, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Information);

        public void ShowWarning(string text, string caption)
            => MessageBoxes.Show(this, text, caption, MessageBoxButtons.OK, MessageBoxIcon.Warning);

        public bool Confirm(string text, string caption, bool defaultNo = false)
            => MessageBoxes.Show(this, text, caption, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                defaultNo ? MessageBoxDefaultButton.Button2 : MessageBoxDefaultButton.Button1) == DialogResult.Yes;

        public bool? ConfirmWithCancel(string text, string caption)
            => MessageBoxes.Show(this, text, caption, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) switch
            {
                DialogResult.Yes => true,
                DialogResult.No => false,
                _ => null
            };
    }
}

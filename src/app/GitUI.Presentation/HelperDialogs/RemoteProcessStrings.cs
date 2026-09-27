using GitUI.Presentation.Translations;

namespace GitUI.Presentation.HelperDialogs;

/// <summary>Strings of the progress dialog of the remote commands; ids match <c>FormRemoteProcess</c>.</summary>
public sealed class RemoteProcessStrings : ViewStrings
{
    public RemoteProcessStrings()
        : base("FormRemoteProcess")
    {
        FingerprintNotRegistered = Add("_fingerprintNotRegistredText", "Text", """
            The fingerprint of this host is not registered by PuTTY.
            This causes this process to hang, and that why it is automatically stopped.

            When the connection is opened detached from Git and Gitanas, the host's fingerprint can be registered.
            You could also manually add the host's fingerprint or run Test Connection from the remotes dialog.

            Do you want to register the host's fingerprint and restart the process?
            """.ReplaceLineEndings("\n"));
        FingerprintNotRegisteredCaption = Add("_fingerprintNotRegistredTextCaption", "Text", "Host Fingerprint not registered");
    }

    public TranslatedText FingerprintNotRegistered { get; }

    public TranslatedText FingerprintNotRegisteredCaption { get; }
}

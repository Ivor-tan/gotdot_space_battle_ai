using System;
using Godot;

public partial class PrivacyConsentDialog : CanvasLayer
{
    public event Action Accepted;
    public event Action Rejected;

    [Export] public RichTextLabel PolicyText;
    [Export] public Button AcceptButton;
    [Export] public Button RejectButton;

    public override void _Ready()
    {
        Layer = 100;

        if (PolicyText != null && IsInstanceValid(PolicyText))
        {
            PolicyText.MetaClicked += onMetaClicked;
        }

        if (AcceptButton != null && IsInstanceValid(AcceptButton))
        {
            AcceptButton.Pressed += onAccepted;
        }

        if (RejectButton != null && IsInstanceValid(RejectButton))
        {
            RejectButton.Pressed += onRejected;
        }
    }

    public override void _ExitTree()
    {
        if (PolicyText != null && IsInstanceValid(PolicyText))
        {
            PolicyText.MetaClicked -= onMetaClicked;
        }

        if (AcceptButton != null && IsInstanceValid(AcceptButton))
        {
            AcceptButton.Pressed -= onAccepted;
        }

        if (RejectButton != null && IsInstanceValid(RejectButton))
        {
            RejectButton.Pressed -= onRejected;
        }
    }

    private void onAccepted()
    {
        Accepted?.Invoke();
        QueueFree();
    }

    private void onRejected()
    {
        Rejected?.Invoke();
        QueueFree();
    }

    private static void onMetaClicked(Variant meta)
    {
        string url = meta.AsString();
        if (!string.IsNullOrWhiteSpace(url))
        {
            OS.ShellOpen(url);
        }
    }
}

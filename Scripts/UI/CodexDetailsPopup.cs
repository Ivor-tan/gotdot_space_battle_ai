using Godot;

public partial class CodexDetailsPopup : Control
{
    private const float PopupWidthRatio = 0.72f;
    private const float PopupHeightRatio = 0.72f;
    private const int MinimumPopupWidth = 280;
    private const int MinimumPopupHeight = 260;
    private const int MaximumPopupWidth = 620;
    private const int MaximumPopupHeight = 520;

    [Export] public TextureRect Icon;
    [Export] public Label TitleLabel;
    [Export] public Label DetailsLabel;
    [Export] public Button CloseButton;
    [Export] public PanelContainer DialogPanel;

    public override void _Ready()
    {
        if (CloseButton != null && IsInstanceValid(CloseButton))
        {
            CloseButton.Pressed += hideDetails;
        }
    }

    public override void _ExitTree()
    {
        if (CloseButton != null && IsInstanceValid(CloseButton))
        {
            CloseButton.Pressed -= hideDetails;
        }
    }

    public void showDetails(string title, Texture2D icon, string details)
    {
        if (Icon == null || !IsInstanceValid(Icon) || TitleLabel == null || !IsInstanceValid(TitleLabel) ||
            DetailsLabel == null || !IsInstanceValid(DetailsLabel))
        {
            LogUtil.Error("Codex details popup nodes are not configured.");
            return;
        }

        TitleLabel.Text = title;
        DetailsLabel.Text = details;
        Icon.Texture = icon;
        Icon.Visible = icon != null && IsInstanceValid(icon);
        Hide();
        // Apply the panel size after the text container has refreshed, then reveal
        // the full-screen centered container without PopupPanel's auto-sizing.
        CallDeferred(nameof(showCentered));
    }

    private void showCentered()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        int width = Mathf.Clamp(Mathf.RoundToInt(viewportSize.X * PopupWidthRatio), MinimumPopupWidth, MaximumPopupWidth);
        int height = Mathf.Clamp(Mathf.RoundToInt(viewportSize.Y * PopupHeightRatio), MinimumPopupHeight, MaximumPopupHeight);
        if (DialogPanel == null || !IsInstanceValid(DialogPanel))
        {
            LogUtil.Error("Codex details dialog panel is not configured.");
            return;
        }

        DialogPanel.CustomMinimumSize = new Vector2(width, height);
        Show();
        MoveToFront();
    }

    private void hideDetails()
    {
        Hide();
    }
}

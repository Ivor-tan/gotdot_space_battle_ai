using Godot;

public partial class shipArchiveItem : PanelContainer
{
    [Export] public Texture2D iconTexture;
    [Export] public string nameKey = "UI_ARCHIVE_SHIP_001_NAME";
    [Export] public string descriptionKey = "UI_ARCHIVE_SHIP_001_DESCRIPTION";
    [Export] public string statsKey = "UI_ARCHIVE_SHIP_001_STATS";
    [Export] public TextureRect icon;
    [Export] public Label nameLabel;
    [Export] public Label descriptionLabel;
    [Export] public Button detailsButton;
    private bool hasLoadedContents;
    private PlayerShipData _shipData;
    private CodexDetailsPopup detailsPage;

    public override void _Ready()
    {
        VisibilityChanged += onVisibilityChanged;
        connectButtons();

        if (Visible)
        {
            loadContents();
        }
    }

    public override void _ExitTree()
    {
        VisibilityChanged -= onVisibilityChanged;
        disconnectButtons();
    }

    public void loadContents()
    {
        if (hasLoadedContents)
        {
            return;
        }

        applyContents();
        hasLoadedContents = true;
    }

    public void Configure(PlayerShipData shipData, CodexDetailsPopup detailsPage)
    {
        _shipData = shipData;
        this.detailsPage = detailsPage;
        hasLoadedContents = false;
        if (IsInsideTree())
        {
            loadContents();
        }
    }

    private void onVisibilityChanged()
    {
        if (Visible)
        {
            loadContents();
        }
    }

    private void applyContents()
    {
        CustomMinimumSize = new Vector2(420, 150);
        if (_shipData != null && IsInstanceValid(_shipData))
        {
            iconTexture = _shipData.Icon;
            nameKey = _shipData.getDisplayName();
            descriptionKey = translate(_shipData.DescriptionKey, _shipData.Description);
            statsKey = formatStats(_shipData);
        }

        if (icon != null && IsInstanceValid(icon))
        {
            icon.Texture = iconTexture;
        }

        if (nameLabel != null && IsInstanceValid(nameLabel))
        {
            nameLabel.Text = nameKey;
        }

        if (descriptionLabel != null && IsInstanceValid(descriptionLabel))
        {
            descriptionLabel.Text = descriptionKey;
            descriptionLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            descriptionLabel.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        }

    }

    private void connectButtons()
    {
        if (detailsButton != null && IsInstanceValid(detailsButton))
        {
            detailsButton.Pressed += showDetails;
        }

    }

    private void resolveDetailsPage()
    {
        if (detailsPage != null && IsInstanceValid(detailsPage))
        {
            return;
        }

        detailsPage = GetTree().GetFirstNodeInGroup("codex_details_popup") as CodexDetailsPopup;
        if (detailsPage == null || !IsInstanceValid(detailsPage))
        {
            LogUtil.Error("Shared codex details page is not configured.");
            return;
        }
    }

    private void disconnectButtons()
    {
        if (detailsButton != null && IsInstanceValid(detailsButton))
        {
            detailsButton.Pressed -= showDetails;
        }

    }

    private void showDetails()
    {
        if (detailsPage == null || !IsInstanceValid(detailsPage))
        {
            LogUtil.Error("飞船详情弹窗节点未配置。");
            return;
        }

        if (_shipData != null && IsInstanceValid(_shipData))
        {
            string description = translate(_shipData.DescriptionKey, _shipData.Description);
            detailsPage.showDetails(_shipData.getDisplayName(), _shipData.Icon, $"{description}\n\n{formatStats(_shipData)}");
            return;
        }

        detailsPage.showDetails(nameKey, iconTexture, $"{descriptionKey}\n\n{statsKey}");
    }

    private static string translate(StringName key, string fallback)
    {
        return key == default ? fallback : TranslationServer.Translate(key);
    }

    private static string formatStats(PlayerShipData shipData)
    {
        return $"攻击 {shipData.AttackPower}  射程 {shipData.AttackRange:0}\n" +
            $"攻击间隔 {shipData.AttackInterval:0.00}s\n" +
            $"武器 {shipData.WeaponId}  被动 {shipData.PassiveAbilityId}";
    }
}

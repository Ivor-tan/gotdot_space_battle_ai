using Godot;
using Godot.Collections;
using GodotResourceGroups;

public partial class DebugTestPanel : PanelContainer
{
    private VBoxContainer _benefitRows;
    private VBoxContainer _upgradeRows;
    private Label _upgradeTitle;
    private readonly System.Collections.Generic.Dictionary<StringName, string> _shipDisplayNames = new();
    private bool _shipDisplayNamesLoaded;
    private bool _rowsBuilt;
    private bool _wasPausedBeforeOpen;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _benefitRows = GetNodeOrNull<VBoxContainer>("Content/Tables/BenefitTable/Scroll/Rows");
        _upgradeRows = GetNodeOrNull<VBoxContainer>("Content/Tables/UpgradeTable/Scroll/Rows");
        _upgradeTitle = GetNodeOrNull<Label>("Content/Tables/UpgradeTable/Title");
        Button advanceButton = GetNodeOrNull<Button>("Content/AdvanceButton");
        if (advanceButton != null && IsInstanceValid(advanceButton))
        {
            advanceButton.Pressed += advanceDebugWave;
        }
        Hide();
    }

    public override void _ExitTree()
    {
        Button advanceButton = GetNodeOrNull<Button>("Content/AdvanceButton");
        if (advanceButton != null && IsInstanceValid(advanceButton))
        {
            advanceButton.Pressed -= advanceDebugWave;
        }
    }

    public void Toggle()
    {
        if (!IsInsideTree() || !MetaProgressStore.IsProgressionDebugEnabled())
        {
            return;
        }

        if (!_rowsBuilt)
        {
            buildRows();
        }

        if (!Visible)
        {
            _wasPausedBeforeOpen = GetTree().Paused;
            GetTree().Paused = true;
            Visible = true;
        }
        else
        {
            Visible = false;
            GetTree().Paused = _wasPausedBeforeOpen;
        }

        LogUtil.Info($"Debug test panel {(Visible ? "opened" : "closed")}.");
    }

    private void buildRows()
    {
        if (_benefitRows == null || !IsInstanceValid(_benefitRows) || _upgradeRows == null || !IsInstanceValid(_upgradeRows))
        {
            LogUtil.Warning("Debug test panel data containers are unavailable.");
            return;
        }
        loadShipDisplayNames();
        if (EnhanceFunctionManager.Instance != null && IsInstanceValid(EnhanceFunctionManager.Instance))
        {
            foreach (BaseEnhanceFunction benefit in EnhanceFunctionManager.Instance.EnhanceFunctionList)
            {
                if (benefit == null || !IsInstanceValid(benefit) || benefit is CardBenefitEnhanceFunction)
                {
                    continue;
                }

                Button row = new()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Text = $"{benefit.Name}\nEffect: {benefit.Description}\nApplies to: {getBenefitTargetText(benefit)}",
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming
                };
                row.Pressed += () => applyBenefit(benefit);
                _benefitRows.AddChild(row);
            }

            foreach (CardBenefitData card in EnhanceFunctionManager.Instance.GetCardBenefits())
            {
                if (card == null || !IsInstanceValid(card))
                {
                    continue;
                }

                string cardStatus = card.IsImplementedInRuntime ? "[Card]" : "[Not implemented]";
                Button row = new()
                {
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    Text = $"{cardStatus} {card.DisplayName}\nEffect: {card.EffectDescription}\nApplies to: {getCardTargetText(card)}",
                    Disabled = !card.IsImplementedInRuntime,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart,
                    TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming
                };
                row.Pressed += () => applyCardBenefit(card);
                _benefitRows.AddChild(row);
            }
        }

        EnhanceFunctionManager enhanceFunctionManager = EnhanceFunctionManager.Instance;
        if (enhanceFunctionManager == null || !IsInstanceValid(enhanceFunctionManager))
        {
            setUpgradeTitle("Ship upgrades (manager is not ready)");
            LogUtil.Warning("Debug upgrade table is waiting for EnhanceFunctionManager.");
            return;
        }

        Array<ShipUpgradeNodeData> upgrades = enhanceFunctionManager.GetShipUpgradeNodes();
        loadShipDisplayNames();

        LogUtil.Info($"Debug upgrade table received {upgrades.Count} nodes from EnhanceFunctionManager.");

        if (upgrades.Count == 0)
        {
            _upgradeRows.AddChild(new Label { Text = "No ship upgrade nodes were loaded." });
            LogUtil.Warning("Debug upgrade table loaded no ship upgrade nodes.");
        }

        int createdRowCount = 0;
        foreach (ShipUpgradeNodeData upgrade in upgrades)
        {
            if (upgrade == null || !IsInstanceValid(upgrade))
            {
                continue;
            }

            string upgradeName = string.IsNullOrWhiteSpace(upgrade.BranchName)
                ? upgrade.NodeId.ToString()
                : $"{upgrade.BranchName} Tier {upgrade.Tier}";
            string shipName = getShipDisplayName(upgrade.ShipId);
            Button row = new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                Text = $"{shipName} - {upgradeName}\n{upgrade.EffectDescription}",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming
            };
            row.Resized += () => updateUpgradeRowHeight(row);
            row.Pressed += () => applyUpgrade(upgrade);
            _upgradeRows.AddChild(row);
            createdRowCount++;
        }

        setUpgradeTitle($"Ship upgrades ({createdRowCount}/{upgrades.Count} displayed; click to apply)");
        CallDeferred(nameof(refreshUpgradeRowHeights));

        _rowsBuilt = true;
    }

    private void setUpgradeTitle(string text)
    {
        if (_upgradeTitle != null && IsInstanceValid(_upgradeTitle))
        {
            _upgradeTitle.Text = text;
        }
    }

    private void refreshUpgradeRowHeights()
    {
        if (_upgradeRows == null || !IsInstanceValid(_upgradeRows))
        {
            return;
        }

        foreach (Node child in _upgradeRows.GetChildren())
        {
            if (child is Button row && IsInstanceValid(row))
            {
                updateUpgradeRowHeight(row);
            }
        }
    }

    private void loadShipDisplayNames()
    {
        if (_shipDisplayNamesLoaded)
        {
            return;
        }

        _shipDisplayNamesLoaded = true;
        Resource shipGroup = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (shipGroup == null || !IsInstanceValid(shipGroup))
        {
            LogUtil.Warning("Debug upgrade table could not load player ship data.");
            return;
        }

        Array<PlayerShipData> ships = new();
        ResourceGroup.Of(shipGroup).LoadAllInto(ships);
        foreach (PlayerShipData ship in ships)
        {
            if (ship == null || !IsInstanceValid(ship) || ship.ShipId == default)
            {
                continue;
            }

            _shipDisplayNames[ship.ShipId] = ship.getDisplayName();
        }
    }

    private string getShipDisplayName(StringName shipId)
    {
        return _shipDisplayNames.TryGetValue(shipId, out string shipName) && !string.IsNullOrWhiteSpace(shipName)
            ? shipName
            : shipId.ToString();
    }

    private string getBenefitTargetText(BaseEnhanceFunction benefit)
    {
        if (benefit.AppliesToAllShips)
        {
            return "All deployed ships";
        }

        if (benefit.TargetShipData != null && IsInstanceValid(benefit.TargetShipData))
        {
            return benefit.TargetShipData.getDisplayName();
        }

        return benefit.TargetShipId != default
            ? getShipDisplayName(benefit.TargetShipId)
            : "Select a deployed ship";
    }

    private static string getCardTargetText(CardBenefitData card)
    {
        return card.TargetScopeId.ToString() switch
        {
            "ship" => card.RequiresTargetSelection ? "Select a deployed ship" : "Single ship",
            "fleet" => "Fleet",
            "run" => "Run rule",
            "recruitment" => "Recruitment ship",
            _ => "Card rule"
        };
    }

    private static void updateUpgradeRowHeight(Button row)
    {
        if (row.Size.X <= 0.0f)
        {
            return;
        }

        Font font = row.GetThemeFont("font");
        if (font == null || !IsInstanceValid(font))
        {
            return;
        }

        int fontSize = row.GetThemeFontSize("font_size");
        float availableWidth = Mathf.Max(row.Size.X - 24.0f, 1.0f);
        float textHeight = font.GetMultilineStringSize(row.Text, HorizontalAlignment.Left, availableWidth, fontSize).Y;
        float minimumHeight = Mathf.Max(48.0f, textHeight + 16.0f);
        if (!Mathf.IsEqualApprox(row.CustomMinimumSize.Y, minimumHeight))
        {
            row.CustomMinimumSize = new Vector2(0.0f, minimumHeight);
        }
    }

    private static void applyBenefit(BaseEnhanceFunction benefit)
    {
        benefit.ApplyEffect();
        LogUtil.Info($"Debug benefit test applied: {benefit.ID}.");
    }

    private static void applyCardBenefit(CardBenefitData card)
    {
        if (!CardBenefitRuntime.TryApply(card, out string reason))
        {
            LogUtil.Warning($"Debug card test failed: {card.CardId}. {reason}");
            return;
        }

        LogUtil.Info($"Debug card test applied: {card.CardId}.");
    }

    private static void advanceDebugWave()
    {
        EnemySpawnerManager.Instance?.DebugAdvanceWave();
    }

    private static void applyUpgrade(ShipUpgradeNodeData upgrade)
    {
        RunShipUpgradeState.ForceSelect(upgrade);
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !IsInstanceValid(playerManager))
        {
            LogUtil.Warning($"Debug ship upgrade test stored without active fleet: {upgrade.NodeId}.");
            return;
        }

        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player != null && IsInstanceValid(player) && player.ShipData != null && player.ShipData.ShipId == upgrade.ShipId)
            {
                player.ApplyRunUpgrade(upgrade);
            }
        }

        LogUtil.Info($"Debug ship upgrade test applied: {upgrade.NodeId}.");
    }
}

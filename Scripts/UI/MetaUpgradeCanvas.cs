using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotResourceGroups;

public partial class MetaUpgradeCanvas : Control
{
    private const float NodeRadius = 20.0f;
    private const float CenterRadius = 42.0f;
    private const float TopContentSpace = 48.0f;
    private const float BottomDetailsSpace = 112.0f;
    private const float NodeTierSpacing = 112.0f;
    private const int RequiredEliteDefeats = 3;
    private static readonly PackedScene ShipSelectorScene = GD.Load<PackedScene>(Assets.ShipUpgradeShipSelector);
    private readonly Array<ShipUpgradeNodeData> _allNodes = new();
    private readonly List<StringName> _shipIds = new();
    private readonly System.Collections.Generic.Dictionary<StringName, PlayerShipData> _shipDataById = new();
    private readonly List<ShipUpgradeNodeData> _visibleNodes = new();
    private readonly List<PlayerShipData> _shipSelectorShips = new();
    private Vector2 _panOffset;
    private Vector2 _dragStart;
    private bool _isDragging;
    private int _selectedShipIndex;
    private ShipUpgradeShipSelector _shipSelectorPanel;
    private Label _nodeDetailsLabel;
    private string _feedback = "点击中心切换舰船；点击节点查看并解锁局内候选资格。";

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Stop;
        ClipContents = true;
        loadUpgradeData();
        createNodeDetailsPanel();
        updateNodeDetails(null);
        QueueRedraw();
    }

    public void Refresh()
    {
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left)
        {
            if (!mouseButton.Pressed)
            {
                _isDragging = false;
                AcceptEvent();
                return;
            }

            _dragStart = mouseButton.Position;
            if (mouseButton.Position.DistanceTo(getCenterPosition()) <= CenterRadius)
            {
                showShipSelector();
                AcceptEvent();
                return;
            }

            ShipUpgradeNodeData clickedNode = getNodeAt(mouseButton.Position);
            if (clickedNode != null)
            {
                tryUnlockNode(clickedNode);
                AcceptEvent();
                return;
            }

            _isDragging = true;
            AcceptEvent();
            return;
        }

        if (inputEvent is InputEventMouseMotion mouseMotion && _isDragging)
        {
            _panOffset += mouseMotion.Position - _dragStart;
            _dragStart = mouseMotion.Position;
            QueueRedraw();
            AcceptEvent();
        }
    }

    public override void _Draw()
    {
        rebuildVisibleNodes();
        Vector2 center = getCenterPosition();
        Font font = ThemeDB.FallbackFont;

        foreach (ShipUpgradeNodeData node in _visibleNodes)
        {
            Vector2 nodePosition = getNodePosition(node);
            bool isUnlocked = MetaProgressStore.IsShipUpgradeNodeUnlocked(node.NodeId);
            bool canUnlock = canUnlockNode(node, out _);
            Color nodeColor = isUnlocked ? new Color(0.78f, 0.55f, 0.16f) : canUnlock ? new Color(0.20f, 0.54f, 0.34f) : new Color(0.10f, 0.24f, 0.38f);

            DrawLine(center, nodePosition, new Color(0.22f, 0.56f, 0.82f, 0.7f), 3.0f, true);
            DrawCircle(nodePosition, NodeRadius, nodeColor, true);
            DrawArc(nodePosition, NodeRadius, 0.0f, Mathf.Tau, 6, new Color(0.52f, 0.9f, 1.0f), 2.0f, true);
            Texture2D icon = ShipUpgradeIconCatalog.GetIcon(node);
            if (icon != null && IsInstanceValid(icon))
            {
                const float iconSize = 32.0f;
                Vector2 iconPosition = nodePosition - new Vector2(iconSize * 0.5f, iconSize * 0.5f);
                DrawTextureRect(icon, new Rect2(iconPosition, new Vector2(iconSize, iconSize)), false, ShipUpgradeIconCatalog.GetTierTint(node.Tier));
            }

            DrawString(font, nodePosition + new Vector2(-18.0f, 24.0f), $"{node.Tier}级", HorizontalAlignment.Center, 36.0f, 13, Colors.White);
        }

        DrawCircle(center, CenterRadius, new Color(0.16f, 0.46f, 0.7f), true);
        DrawArc(center, CenterRadius, 0.0f, Mathf.Tau, 6, new Color(0.52f, 0.9f, 1.0f), 3.0f, true);
        PlayerShipData selectedShip = getSelectedShipData();
        if (selectedShip != null && IsInstanceValid(selectedShip) && selectedShip.Icon != null && IsInstanceValid(selectedShip.Icon))
        {
            const float shipIconSize = 60.0f;
            Vector2 shipIconPosition = center - new Vector2(shipIconSize * 0.5f, shipIconSize * 0.5f);
            DrawTextureRect(selectedShip.Icon, new Rect2(shipIconPosition, new Vector2(shipIconSize, shipIconSize)), false, Colors.White);
        }

        DrawString(font, center + new Vector2(-48.0f, 56.0f), getSelectedShipName(), HorizontalAlignment.Center, 96.0f, 14, Colors.White);
        float statusTextWidth = Mathf.Max(0.0f, Size.X - 32.0f);
        DrawString(font, new Vector2(16.0f, 26.0f), $"档案等级 {MetaProgressStore.GetProfileLevel()} · 星图经验 {MetaProgressStore.GetExperience()} · 下级还需 {MetaProgressStore.GetExperienceToNextProfileLevel()}", HorizontalAlignment.Left, statusTextWidth, 14, Colors.LightSteelBlue);
        DrawString(font, new Vector2(16.0f, 48.0f), "蓝：条件未满足  绿：可解锁  金：已解锁", HorizontalAlignment.Left, statusTextWidth, 12, Colors.LightSteelBlue);
    }

    private void loadUpgradeData()
    {
        Resource nodeGroup = GD.Load<Resource>(Assets.Ship_Upgrade_Nodes);
        if (nodeGroup != null && IsInstanceValid(nodeGroup))
        {
            ResourceGroup.Of(nodeGroup).LoadAllInto(_allNodes);
        }

        Resource shipGroup = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (shipGroup != null && IsInstanceValid(shipGroup))
        {
            Array<PlayerShipData> ships = new();
            ResourceGroup.Of(shipGroup).LoadAllInto(ships);
            foreach (PlayerShipData ship in ships)
            {
                if (ship != null && IsInstanceValid(ship) && ship.ShipId != default)
                {
                    _shipDataById[ship.ShipId] = ship;
                }
            }
        }

        foreach (ShipUpgradeNodeData node in _allNodes)
        {
            if (node != null && IsInstanceValid(node) && node.ShipId != default && !_shipIds.Contains(node.ShipId))
            {
                _shipIds.Add(node.ShipId);
            }
        }

        if (_shipIds.Count == 0)
        {
            _feedback = "未找到舰船天赋节点数据。";
        }
    }

    private void showShipSelector()
    {
        if (_shipIds.Count == 0 || _shipSelectorPanel != null || ShipSelectorScene == null || !IsInstanceValid(ShipSelectorScene))
        {
            if (ShipSelectorScene == null || !IsInstanceValid(ShipSelectorScene))
            {
                LogUtil.Error("Ship upgrade selector scene is unavailable.");
            }

            return;
        }

        _shipSelectorPanel = ShipSelectorScene.Instantiate<ShipUpgradeShipSelector>();
        if (_shipSelectorPanel == null)
        {
            LogUtil.Error("Unable to instantiate ship upgrade selector.");
            return;
        }

        _shipSelectorPanel.Position = new Vector2(Mathf.Max(12.0f, Size.X * 0.5f - 140.0f), 12.0f);
        _shipSelectorPanel.Size = new Vector2(Mathf.Min(280.0f, Size.X - 24.0f), Mathf.Min(360.0f, Size.Y - 24.0f));
        _shipSelectorPanel.MouseFilter = MouseFilterEnum.Stop;
        _shipSelectorShips.Clear();
        foreach (StringName shipId in _shipIds)
        {
            if (_shipDataById.TryGetValue(shipId, out PlayerShipData shipData) && shipData != null && IsInstanceValid(shipData))
            {
                _shipSelectorShips.Add(shipData);
            }
        }

        _shipSelectorPanel.Configure(_shipSelectorShips, selectShipByData);
        AddChild(_shipSelectorPanel);
    }

    private void selectShip(int shipIndex)
    {
        if (shipIndex < 0 || shipIndex >= _shipIds.Count)
        {
            return;
        }

        _selectedShipIndex = shipIndex;
        _feedback = $"已选择 {getSelectedShipName()}：点击节点查看说明与前置条件。";
        if (_shipSelectorPanel != null && IsInstanceValid(_shipSelectorPanel))
        {
            _shipSelectorPanel.QueueFree();
        }

        _shipSelectorPanel = null;
        updateNodeDetails(null);
        QueueRedraw();
    }

    private void selectShipByData(PlayerShipData shipData)
    {
        if (shipData == null || !IsInstanceValid(shipData))
        {
            return;
        }

        selectShip(_shipIds.IndexOf(shipData.ShipId));
    }

    private void rebuildVisibleNodes()
    {
        _visibleNodes.Clear();
        if (_shipIds.Count == 0)
        {
            return;
        }

        foreach (ShipUpgradeNodeData node in _allNodes)
        {
            if (node != null && IsInstanceValid(node) && node.ShipId == _shipIds[_selectedShipIndex])
            {
                _visibleNodes.Add(node);
            }
        }
    }

    private ShipUpgradeNodeData getNodeAt(Vector2 position)
    {
        rebuildVisibleNodes();
        foreach (ShipUpgradeNodeData node in _visibleNodes)
        {
            if (position.DistanceTo(getNodePosition(node)) <= NodeRadius)
            {
                return node;
            }
        }

        return null;
    }

    private void tryUnlockNode(ShipUpgradeNodeData node)
    {
        if (MetaProgressStore.IsShipUpgradeNodeUnlocked(node.NodeId))
        {
            _feedback = $"{node.BranchName} {node.Tier}级已解锁，会按局内规则进入候选池。";
        }
        else if (!canUnlockNode(node, out string requirement))
        {
            _feedback = requirement;
        }
        else if (MetaProgressStore.TryUnlockShipUpgradeNode(node.NodeId, MetaProgressStore.GetShipUpgradeCost(node.Tier)))
        {
            _feedback = $"已解锁 {node.BranchName} {node.Tier}级：消耗 {MetaProgressStore.GetShipUpgradeCost(node.Tier)} 星图经验，局内候选池已开放。";
        }
        else
        {
            _feedback = "节点存档写入失败，请检查用户存档目录。";
        }

        updateNodeDetails(node);
        QueueRedraw();
    }

    private void createNodeDetailsPanel()
    {
        PanelContainer panel = new()
        {
            MouseFilter = MouseFilterEnum.Ignore,
            AnchorTop = 1.0f,
            AnchorRight = 1.0f,
            AnchorBottom = 1.0f,
            OffsetLeft = 8.0f,
            OffsetTop = -104.0f,
            OffsetRight = -8.0f,
            OffsetBottom = -8.0f
        };
        _nodeDetailsLabel = new Label
        {
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore
        };
        panel.AddChild(_nodeDetailsLabel);
        AddChild(panel);
    }

    private void updateNodeDetails(ShipUpgradeNodeData node)
    {
        if (_nodeDetailsLabel == null || !IsInstanceValid(_nodeDetailsLabel))
        {
            return;
        }

        if (node == null)
        {
            _nodeDetailsLabel.Text = _feedback;
            return;
        }

        canUnlockNode(node, out string requirement);
        _nodeDetailsLabel.Text = $"{node.BranchName} · {node.Tier}级\n{node.EffectDescription}\n前置：{requirement}";
    }

    private bool canUnlockNode(ShipUpgradeNodeData node, out string requirement)
    {
        if (MetaProgressStore.IsShipUpgradeNodeUnlocked(node.NodeId))
        {
            requirement = "已解锁";
            return false;
        }

        int requiredProfileLevel = getRequiredProfileLevel(node.ShipId);
        int profileLevel = MetaProgressStore.GetProfileLevel();
        if (profileLevel < requiredProfileLevel)
        {
            requirement = $"需要档案等级 {requiredProfileLevel}（当前 {profileLevel}，距下级还需 {MetaProgressStore.GetExperienceToNextProfileLevel()} 星图经验）。";
            return false;
        }

        int cost = MetaProgressStore.GetShipUpgradeCost(node.Tier);
        if (MetaProgressStore.GetExperience() < cost)
        {
            requirement = $"需要 {cost} 星图经验进行手动解锁（当前 {MetaProgressStore.GetExperience()}）。";
            return false;
        }

        if (node.Tier >= 2 && !MetaProgressStore.IsShipUpgradeNodeUnlocked(node.PreviousNodeId))
        {
            requirement = "需要先解锁同分支的上一层节点。";
            return false;
        }

        if (node.Tier == 2 && MetaProgressStore.GetShipEliteDefeats(node.ShipId) < RequiredEliteDefeats)
        {
            requirement = $"需要该舰参与击败 {RequiredEliteDefeats} 个精英（当前 {MetaProgressStore.GetShipEliteDefeats(node.ShipId)}/{RequiredEliteDefeats}）。";
            return false;
        }

        if (node.Tier >= 3 && MetaProgressStore.GetShipBossVictories(node.ShipId) < 1)
        {
            requirement = "需要使用该舰完成一次 Boss 或守关战。";
            return false;
        }

        requirement = $"满足前置条件；解锁消耗 {cost} 星图经验。";
        return true;
    }

    private int getRequiredProfileLevel(StringName shipId)
    {
        return _shipDataById.TryGetValue(shipId, out PlayerShipData shipData)
            ? Mathf.Max(2, shipData.UnlockLevel + 1)
            : 2;
    }

    private Vector2 getCenterPosition()
    {
        float availableHeight = Mathf.Max(0.0f, Size.Y - TopContentSpace - BottomDetailsSpace);
        float centerY = TopContentSpace + availableHeight * 0.5f;
        return new Vector2(Size.X * 0.5f, centerY) + _panOffset;
    }

    private Vector2 getNodePosition(ShipUpgradeNodeData node)
    {
        int branchIndex = getBranchIndex(node.BranchId);
        float angle = -Mathf.Pi * 0.5f + Mathf.Tau * branchIndex / 3.0f;
        Vector2 direction = Vector2.FromAngle(angle);
        float distance = NodeTierSpacing * Mathf.Max(1, node.Tier);
        return getCenterPosition() + direction * distance;
    }

    private int getBranchIndex(StringName branchId)
    {
        int index = 0;
        foreach (ShipUpgradeNodeData node in _visibleNodes)
        {
            if (node.BranchId == branchId)
            {
                return index;
            }

            if (node.Tier == 1)
            {
                index++;
            }
        }

        return 0;
    }

    private string getSelectedShipName()
    {
        PlayerShipData shipData = getSelectedShipData();
        return shipData != null && IsInstanceValid(shipData) ? shipData.getDisplayName() : "未选择舰船";
    }

    private PlayerShipData getSelectedShipData()
    {
        if (_shipIds.Count == 0 || _selectedShipIndex < 0 || _selectedShipIndex >= _shipIds.Count)
        {
            return null;
        }

        return _shipDataById.TryGetValue(_shipIds[_selectedShipIndex], out PlayerShipData shipData) ? shipData : null;
    }

    private string getShipName(StringName shipId)
    {
        return _shipDataById.TryGetValue(shipId, out PlayerShipData shipData) ? shipData.getDisplayName() : shipId.ToString();
    }
}

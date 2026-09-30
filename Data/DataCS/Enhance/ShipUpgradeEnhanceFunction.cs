using Godot;

public partial class ShipUpgradeEnhanceFunction : BaseEnhanceFunction
{
    private ShipUpgradeNodeData _node;
    private int _playerLevel;

    public void Configure(ShipUpgradeNodeData node, int playerLevel)
    {
        _node = node;
        _playerLevel = playerLevel;
        Name = node.BranchName + " " + node.Tier + "级";
        Description = node.EffectDescription;
        Icon = ShipUpgradeIconCatalog.GetIcon(node);
        TargetShipId = node.ShipId;
        AppliesToAllShips = false;
        CanRepeat = false;
    }

    public override void ApplyEffect()
    {
        if (_node == null || !IsInstanceValid(_node))
        {
            LogUtil.Warning("Ship upgrade selection failed: node is unavailable.");
            return;
        }

        if (!RunShipUpgradeState.TrySelect(_node, _playerLevel, out string reason))
        {
            LogUtil.Warning(reason);
            return;
        }

        foreach (PlayerController player in PlayerManager.Instance.CurrentPlayerDic.Values)
        {
            if (player != null && IsInstanceValid(player) && player.ShipData != null && player.ShipData.ShipId == _node.ShipId)
            {
                player.ApplyRunUpgrade(_node);
            }
        }
    }
}

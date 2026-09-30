using Godot;

[GlobalClass]
public partial class AddPlayer : BaseEnhanceFunction
{
    // 兼容旧资源；新资源应通过 TargetShipData 或 TargetShipId 指定飞船。
    [Export] public PackedScene PlayerScene;

    public bool canRecruit(PlayerManager playerManager)
    {
        if (playerManager == null)
        {
            return false;
        }

        if (tryResolveTargetShipData(out PlayerShipData shipData))
        {
            return playerManager.CurrentPlayerCount < playerManager.MaxPlayerCount || playerManager.hasShip(shipData.ShipId);
        }

        return playerManager.CurrentPlayerCount < playerManager.MaxPlayerCount && PlayerScene != null && IsInstanceValid(PlayerScene);
    }

    public override void ApplyEffect()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            LogUtil.Warning("招募飞船失败：PlayerManager 未就绪。");
            return;
        }

        if (tryResolveTargetShipData(out PlayerShipData shipData))
        {
            if (shipData.ShipScene == null || !IsInstanceValid(shipData.ShipScene))
            {
                LogUtil.Warning($"招募飞船失败：{shipData.ShipId} 未配置 ShipScene。");
                return;
            }

            playerManager.AddPlayer(shipData.ShipScene, shipData);
            return;
        }

        if (PlayerScene == null || !IsInstanceValid(PlayerScene))
        {
            LogUtil.Warning($"招募飞船失败：增益 {ID} 未配置目标飞船数据或后备场景。");
            return;
        }

        playerManager.AddPlayer(PlayerScene);
    }

    public bool tryApplyAtSlot(int playerIndex)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !tryResolveTargetShipData(out PlayerShipData shipData) ||
            shipData.ShipScene == null || !IsInstanceValid(shipData.ShipScene))
        {
            return false;
        }

        if (!playerManager.isSlotAvailable(playerIndex))
        {
            return false;
        }

        playerManager.AddPlayerAtIndex(shipData.ShipScene, shipData, playerIndex);
        return true;
    }
}

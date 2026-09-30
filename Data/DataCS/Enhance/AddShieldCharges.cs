using Godot;

[GlobalClass]
public partial class AddShieldCharges : BaseEnhanceFunction
{
    [Export] public int ShieldChargeIncrease = 1;

    public override void ApplyEffect()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            LogUtil.Warning("护盾增益应用失败：PlayerManager 未就绪。");
            return;
        }

        playerManager.addFleetShieldCharges(ShieldChargeIncrease);
    }
}

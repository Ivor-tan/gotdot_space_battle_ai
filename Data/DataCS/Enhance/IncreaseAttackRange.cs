using Godot;
using System;
[GlobalClass]
public partial class IncreaseAttackRange : BaseEnhanceFunction
{
	[Export] public int RangeIncrease = 10; // 增加的攻击范围百分比
	public override void ApplyEffect()
	{
		var pm = PlayerManager.Instance;
		if (pm == null || pm.Player == null) return;
		float factor = 1.0f + RangeIncrease / 100.0f;
		pm.Player.CurrentPlayerStates.AttackRange *= factor;
		GlobalMessengerManager.Instance.EmitSignal(GlobalMessengerManager.SignalName.OnUpdateAttackRange, pm.Player.CurrentPlayerStates.AttackRange);
	}
}

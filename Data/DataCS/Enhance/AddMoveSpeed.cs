using Godot;
using System;

[GlobalClass]
public partial class AddMoveSpeed : BaseEnhanceFunction
{
	[Export] public int MoveIncrease = 1; // 增加舰队整体移动速度的绝对值

	public override void ApplyEffect()
	{
		var pm = PlayerManager.Instance;
		if (pm == null || pm.Player == null) return;
		pm.Player.CurrentPlayerStates.MaxSpeed += MoveIncrease;
	}
}

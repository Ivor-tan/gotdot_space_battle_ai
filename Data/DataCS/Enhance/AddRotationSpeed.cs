using Godot;
using System;

[GlobalClass]
public partial class AddRotationSpeed : BaseEnhanceFunction
{
	[Export] public int RotateIncrease = 1; // 增加旋转速度的绝对值，例如 1 表示 +1 的 RotateSpeed

	public override void ApplyEffect()
	{
		var pm = PlayerManager.Instance;
		if (pm == null || pm.Player == null) return;
		pm.Player.CurrentPlayerStates.RotateSpeed += RotateIncrease;
	}
}

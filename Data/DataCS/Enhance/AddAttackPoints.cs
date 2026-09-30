using Godot;
using System;
[GlobalClass]
public partial class AddAttackPoints : BaseEnhanceFunction
{
	[Export] public int AttackPointIncrease = 5;// 增加的攻击点百分比
	public override void ApplyEffect()
	{
		var pm = PlayerManager.Instance;
		if (pm == null) return;
		pm.SetBulletDamage(AttackPointIncrease);
	}
}

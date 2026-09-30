using Godot;
using System;

public partial class TestAbility : AbilityComponent
{
	public override void Upgrade()
	{
		LogUtil.Info("Upgrade");
	}
}

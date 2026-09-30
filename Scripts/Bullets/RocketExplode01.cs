using Godot;
using System;

public partial class RocketExplode01 : AnimatedSprite2D
{
	public override void _Ready()
	{
		Connect(AnimatedSprite2D.SignalName.AnimationLooped, Callable.From(OnExplodeFinish));
	}


	private void OnExplodeFinish()
	{
		ObjectPoolManager.Instance.Release(PoolType.RocketExplosionEffect01, this);
	}
}

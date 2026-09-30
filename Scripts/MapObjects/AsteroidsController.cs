using Godot;
using System;

public partial class AsteroidsController : CharacterBody2D, IDamageable
{
	public int GetDamage() => Asteroid_States.Damage;
	private AsteroidStatus AsteroidStates;
	[Export] public AsteroidStatus Asteroid_States;
	public override void _Ready()
	{
		Reset();
	}

	public void OnHit(int damage)
	{
		AsteroidStates.HP -= damage;
		if (AsteroidStates.HP <= 0)
		{
			ObjectPoolManager.Instance.Release(PoolType.Asteroid, this);
		}
	}
	public void Reset()
	{
		AsteroidStates = (AsteroidStatus)Asteroid_States.Duplicate();
	}

}

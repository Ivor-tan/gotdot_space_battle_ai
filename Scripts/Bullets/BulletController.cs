using Godot;
using System;
using System.Collections.Generic;

public partial class BulletController : Node2D
{
	[Export] public BulletStatus BulletStates;
	[Export] public Area2D DetectionArea;
	[Export] public BulletLine TrailLine;
	[Export] public Sprite2D Sprite;

	private BulletStatus CurrentBulletStates;
	private Texture2D _defaultTexture;
	private readonly HashSet<ulong> _hitTargetIds = new();
	// Damage can despawn enemies, so collect the current splash candidates before applying any hit.
	private readonly List<Node2D> _splashTargets = new(16);
	private int _remainingPierces;
	private float _splashRadius;
	private float _splashDamageMultiplier;
	private float _damageMultiplier = 1.0f;
	public event Action<BulletController, Node2D, int> HitConfirmed;
	[Export] public Timer LifeTimer;
	public float CurrentSpeed = 0f;
	public PlayerController CardOwner { get; set; }
	public Vector2 targetPosition = Vector2.Up;
	public bool IsRotate = true;
	public bool HasSplashDamage => _splashRadius > 0.0f && _splashDamageMultiplier > 0.0f;
	public float SplashRadius => _splashRadius;
	public override void _Ready()
	{
		if (Sprite != null && IsInstanceValid(Sprite))
		{
			_defaultTexture = Sprite.Texture;
		}
		LifeTimer.Timeout += OnDismissed;
		DetectionArea.AreaEntered += OnDetected;
		ResetBullet();
	}
	private void OnDetected(Area2D area)
	{
		if (area.GetParent() is IDamageable target)
		{
			ulong targetId = target is GodotObject targetObject ? targetObject.GetInstanceId() : 0;
			if (targetId != 0 && !_hitTargetIds.Add(targetId))
			{
				return;
			}

			int damage = Mathf.Max(1, Mathf.RoundToInt(CurrentBulletStates.Damage * _damageMultiplier));
			CardBenefitRuntime.OnBulletPreHit(CardOwner, area.GetParent() as Node2D, ref damage);
			bool defeatedByHit = area.GetParent() is EnemyController enemy && enemy.WillBeDefeatedBy(damage);
			target.OnHit(damage);
			HitConfirmed?.Invoke(this, area.GetParent() as Node2D, damage);
			CardBenefitRuntime.OnBulletHit(CardOwner, area.GetParent() as Node2D, damage);
			if (defeatedByHit)
			{
				CardBenefitRuntime.OnEnemyDefeated(CardOwner);
			}
			applySplashDamage(targetId);
			if (HasSplashDamage)
			{
				ObjectPoolManager.Instance.Spawn(PoolType.RocketExplosionEffect01, (GlobalPosition + area.GlobalPosition) / 2);
			}
			if (_remainingPierces > 0)
			{
				_remainingPierces--;
				return;
			}

			OnDismissed();
		}
	}

	public void ConfigureSpecialShot(int pierceCount, float splashRadius, float splashDamageMultiplier)
	{
		_remainingPierces = Mathf.Clamp(pierceCount, 0, 8);
		_splashRadius = Mathf.Clamp(splashRadius, 0.0f, 180.0f);
		_splashDamageMultiplier = Mathf.Clamp(splashDamageMultiplier, 0.0f, 1.0f);
	}

	public void MultiplySplashRadius(float multiplier)
	{
		_splashRadius = Mathf.Clamp(_splashRadius * Mathf.Max(0.0f, multiplier), 0.0f, 180.0f);
	}

	public void ConfigureDamageMultiplier(float damageMultiplier)
	{
		_damageMultiplier = Mathf.Clamp(damageMultiplier, 0.1f, 4.0f);
	}

	public void MultiplyDamageMultiplier(float multiplier)
	{
		_damageMultiplier = Mathf.Clamp(_damageMultiplier * multiplier, 0.1f, 4.0f);
	}

	public void MultiplyTurnSpeed(float multiplier)
	{
		if (CurrentBulletStates == null || !IsInstanceValid(CurrentBulletStates))
		{
			return;
		}

		CurrentBulletStates.RotateSpeed = Mathf.Clamp(CurrentBulletStates.RotateSpeed * multiplier, 0.1f, 50.0f);
	}

	/// <summary>Sets this pooled projectile's ship-specific appearance for its current spawn.</summary>
	public void SetProjectileTexture(Texture2D texture)
	{
		if (Sprite == null || !IsInstanceValid(Sprite))
		{
			return;
		}

		Sprite.Texture = texture ?? _defaultTexture;
	}
	public override void _PhysicsProcess(double delta)
	{
		Vector2 direction = Vector2.FromAngle(Rotation);
		if (CurrentSpeed < CurrentBulletStates.MaxSpeed)
		{
			CurrentSpeed = Mathf.Min(CurrentSpeed + CurrentBulletStates.Acceleration * (float)delta, CurrentBulletStates.MaxSpeed);
		}

		Position += direction * CurrentSpeed * (float)delta;

		if (IsRotate)
		{
			// 旋转逻辑：朝向目标位置
			Vector2 dir = targetPosition - GlobalPosition;

			float diff = Mathf.AngleDifference(Rotation, dir.Angle());

			float step = CurrentBulletStates.RotateSpeed * (float)delta;

			Rotation += Mathf.Clamp(diff, -step, step);

			if (Mathf.Abs(diff) < CurrentBulletStates.threshold)
			{
				IsRotate = false;
			}
		}

	}
	private void OnDismissed()
	{
		LifeTimer.Stop();
		ObjectPoolManager.Instance.Release(PoolType.Bullet, this);
	}
	public void ResetBullet()
	{
		_hitTargetIds.Clear();
		_remainingPierces = 0;
		_splashRadius = 0.0f;
		_splashDamageMultiplier = 0.0f;
		_damageMultiplier = 1.0f;
		HitConfirmed = null;
		CardOwner = null;
		CurrentSpeed = 0f;
		IsRotate = true;
		SetProjectileTexture(null);
		targetPosition = Vector2.Up;
		TrailLine.ResetTrail();
		LifeTimer.WaitTime = BulletStates.LifeTime;
		LifeTimer.Start();
		CurrentBulletStates = BulletStates.Duplicate() as BulletStatus;
	}

	private void applySplashDamage(ulong primaryTargetId)
	{
		if (_splashRadius <= 0.0f || _splashDamageMultiplier <= 0.0f || EnemySpawnerManager.Instance == null)
		{
			return;
		}

		int splashDamage = Mathf.Max(1, Mathf.RoundToInt(CurrentBulletStates.Damage * _splashDamageMultiplier));
		float splashRadiusSquared = _splashRadius * _splashRadius;
		_splashTargets.Clear();
		Godot.Collections.Array<Node2D> activeEnemies = EnemySpawnerManager.Instance.ActiveEnemies;
		for (int index = 0; index < activeEnemies.Count; index++)
		{
			Node2D enemy = activeEnemies[index];
			if (enemy == null || !IsInstanceValid(enemy) || enemy.GetInstanceId() == primaryTargetId ||
				enemy.GlobalPosition.DistanceSquaredTo(GlobalPosition) > splashRadiusSquared || enemy is not IDamageable target)
			{
				continue;
			}

			_splashTargets.Add(enemy);
		}

		foreach (Node2D enemy in _splashTargets)
		{
			if (enemy is IDamageable target && IsInstanceValid(enemy))
			{
				target.OnHit(splashDamage);
			}
		}
	}

}

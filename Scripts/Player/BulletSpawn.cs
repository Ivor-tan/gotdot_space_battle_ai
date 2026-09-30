using Godot;
using System;
using System.Collections.Generic;
using GodotResourceGroups;

public partial class BulletSpawn : Node2D
{
	[Export] public int Damage = 50;
	public event Action<BulletController> BulletSpawned;
	private static readonly Dictionary<StringName, Texture2D> ProjectileTextures = new();
	private static bool _hasLoadedProjectileTextures;
	private Texture2D _projectileTexture;

	public void ConfigureShipProjectile(StringName shipId)
	{
		ensureProjectileTexturesLoaded();
		_projectileTexture = ProjectileTextures.TryGetValue(shipId, out Texture2D texture) ? texture : null;
	}

	public void SpawnBullet(Vector2 TargetPosition)
	{
		spawnBullet(TargetPosition, 0.0f, true);
	}

	/// <summary>Fires an effect-owned projectile without re-entering the base shot event.</summary>
	public void SpawnSupplementaryBullet(Vector2 targetPosition, float rotationOffset, float damageMultiplier)
	{
		BulletController bullet = spawnBullet(targetPosition, rotationOffset, false);
		bullet?.ConfigureDamageMultiplier(damageMultiplier);
	}

	private BulletController spawnBullet(Vector2 targetPosition, float rotationOffset, bool notifyEffects)
	{
		var bullet = ObjectPoolManager.Instance.Spawn(PoolType.Bullet, GlobalPosition) as BulletController;
		if (bullet == null)
		{
			return null;
		}

		if (bullet.BulletStates != null)
		{
			bullet.BulletStates.Damage = Damage;
		}
		bullet.ResetBullet();
		bullet.SetProjectileTexture(_projectileTexture);
		bullet.Rotation = PlayerManager.Instance.Player.Rotation + rotationOffset;
		bullet.CurrentSpeed = PlayerManager.Instance.Player.CurrentSpeed;
		bullet.targetPosition = targetPosition;
		if (notifyEffects)
		{
			PlayerController owner = GetParent() as PlayerController;
			if (owner != null && IsInstanceValid(owner))
			{
				bullet.CardOwner = owner;
				bullet.ConfigureDamageMultiplier(owner.ConsumeNextShotDamageMultiplier());
				CardBenefitRuntime.OnBulletSpawned(owner, bullet);
			}
			BulletSpawned?.Invoke(bullet);
		}

		return bullet;
	}

	private static void ensureProjectileTexturesLoaded()
	{
		if (_hasLoadedProjectileTextures)
		{
			return;
		}

		_hasLoadedProjectileTextures = true;
		Resource group = GD.Load<Resource>(Assets.Ship_Design_Data);
		if (group == null || !GodotObject.IsInstanceValid(group))
		{
			LogUtil.Warning("Ship design data group could not be loaded for projectile appearance.");
			return;
		}

		Godot.Collections.Array<ShipDesignData> designs = new();
		ResourceGroup.Of(group).LoadAllInto(designs);
		foreach (ShipDesignData design in designs)
		{
			if (design != null && GodotObject.IsInstanceValid(design) && design.ShipId != default && design.ProjectileIcon != null)
			{
				ProjectileTextures[design.ShipId] = design.ProjectileIcon;
			}
		}
	}
}

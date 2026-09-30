using Godot;
using System;
using System.Collections.Generic;

public partial class EnemyController : CharacterBody2D, IDamageable
{
	public enum AttackPattern
	{
		Contact,
		Ranged,
		ShieldPulse,
		SummonPulse,
		RepairPulse
	}
	public int GetDamage() => EnemiesStates.Damage;

	[ExportGroup("RayCast")]
	[Export] private RayCast2D _rayFront;

	[ExportGroup("Attributes")]
	[Export] public Area2D DetectionArea;
	[Export] public Sprite2D Sprite;
	[Export] public CollisionShape2D BodyCollision;
	[Export] public Label DebugHealthLabel;
	[Export] public EnemyStatus EnemiesStates;
	[ExportGroup("Attack Behavior")]
	[Export] public AttackPattern Pattern = AttackPattern.Contact;
	[Export] public float AbilityRange = 48.0f;
	[Export] public float AbilityCooldown = 1.2f;
	[Export] public int AbilityTargets = 1;
	[Export] public float LifetimeSeconds;
	public EnemyStatus CurrentEnemiesStates;
	public EnemyDesignData DesignData { get; private set; }
	public int ShieldLayers { get; private set; }
	private bool IsRotate = false;
	private bool IsMoving = true;
	private Vector2 targetPosition;
	private const float MinDesignMoveSpeed = 60.0f;
	private const float MaxDesignMoveSpeed = 80.0f;
	private const int MaxSummonedMinionsPerEnemy = 3;
	private const float ProjectileLifetimeSeconds = 5.0f;

	private float CurrentSpeed = 0;
	private float _movementMultiplier = 1.0f;
	private float _movementModifierRemaining;
	private float _damageOverTimeRemaining;
	private float _damageOverTimeTickRemaining;
	private int _damageOverTimePerTick;
	private int _lastDebugHealth = -1;
	private int _lastDebugShieldLayers = -1;
	private bool _wasDebugHealthVisible;
	private ShaderMaterial _statusEffectMaterial;
	private const string StatusEffectShaderPath = "res://Material/Shader/enemy_status_effect.gdshader";
	private static readonly PackedScene EnemyProjectileScene = GD.Load<PackedScene>(Assets.EnemyProjectile);
	private static readonly EnemyDesignData SummonedChaserDesign = GD.Load<EnemyDesignData>("res://Data/Design/Enemies/ENM_ABR_SUM_CHS_001.tres");
	private float _abilityRemaining;
	private float _damageTakenMultiplier = 1.0f;
	private int _spawnedMinionCount;
	private float _lifetimeRemaining;
	private readonly Godot.Collections.Array<PlayerController> _adjacentPlayers = new();
	private readonly HashSet<PlayerController> _contactingPlayers = new();

	public bool IsEliteOrBoss => DesignData != null && (DesignData.TierId == "ELT" || DesignData.TierId == "BOS");
	public bool IsAtFullHealth => CurrentEnemiesStates != null && DesignData != null && CurrentEnemiesStates.Health >= DesignData.BaseHealth;
	public bool HasActiveControlOrBurn => _movementModifierRemaining > 0.0f || _damageOverTimeRemaining > 0.0f;
	public bool WillBeDefeatedBy(int damage)
	{
		return ShieldLayers <= 0 && CurrentEnemiesStates != null && CurrentEnemiesStates.Health <= damage;
	}

	public void RemoveShieldLayers(int count)
	{
		ShieldLayers = Mathf.Max(0, ShieldLayers - Mathf.Max(0, count));
		updateStatusEffectMaterial();
	}

	public void Heal(int amount)
	{
		if (CurrentEnemiesStates == null || DesignData == null || amount <= 0) return;
		CurrentEnemiesStates.Health = Mathf.Min(DesignData.BaseHealth, CurrentEnemiesStates.Health + amount);
	}
	public override void _Ready()
	{
		CurrentEnemiesStates = EnemiesStates.Duplicate() as EnemyStatus;
		LookAt(PlayerManager.Instance.Player.GlobalPosition);
		// 连接点击信号
		InputEvent += OnInputEvent;
		hideDebugHealthLabel();
		initializeStatusEffectMaterial();
		if (DetectionArea != null && IsInstanceValid(DetectionArea))
		{
			DetectionArea.AreaEntered += onDetectionAreaEntered;
			DetectionArea.AreaExited += onDetectionAreaExited;
		}

	}

	public override void _ExitTree()
	{
		if (DetectionArea != null && IsInstanceValid(DetectionArea))
		{
			DetectionArea.AreaEntered -= onDetectionAreaEntered;
			DetectionArea.AreaExited -= onDetectionAreaExited;
		}
	}

	public void ApplyDesignData(EnemyDesignData designData)
	{
		if (designData == null || !IsInstanceValid(designData) || CurrentEnemiesStates == null)
		{
			return;
		}

		DesignData = designData;
		_damageTakenMultiplier = 1.0f;
		_spawnedMinionCount = 0;
		_contactingPlayers.Clear();
		_lifetimeRemaining = LifetimeSeconds;
		CurrentEnemiesStates.Health = designData.BaseHealth;
		// Enemy movement is intentionally kept in a readable 60–80 band. The
		// design resource can still differentiate units inside that band.
		CurrentEnemiesStates.Speed = Mathf.RoundToInt(
			Mathf.Clamp(designData.MoveSpeed, MinDesignMoveSpeed, MaxDesignMoveSpeed));
		CurrentEnemiesStates.Damage = designData.DisplayDamage;
		// Ranged units stop at their designed firing distance; melee units still keep
		// a small collision buffer so they do not overlap the fleet anchor.
		CurrentEnemiesStates.PositionThreshold = Mathf.Max(
			12.0f,
			designData.AttackRange > 0.0f ? designData.AttackRange : designData.CollisionRadius * 2.0f);
		ShieldLayers = Mathf.Max(0, designData.EnemyShieldLayers);
		updateStatusEffectMaterial();
		Texture2D designTexture = designData.Icon ?? CurrentEnemiesStates.Texture as Texture2D;
		applyDesignCollision(designData, designTexture);
		if (Sprite != null && IsInstanceValid(Sprite) && designTexture != null)
		{
			Sprite.Texture = designTexture;
		}
	}

	private void applyDesignCollision(EnemyDesignData designData, Texture2D designTexture)
	{
		if (BodyCollision == null || !IsInstanceValid(BodyCollision))
		{
			return;
		}

		if (designTexture == null || !IsInstanceValid(designTexture))
		{
			BodyCollision.Shape = new CircleShape2D { Radius = Mathf.Max(8.0f, designData.CollisionRadius) };
			return;
		}

		Image image = designTexture.GetImage();
		if (image == null || image.IsEmpty())
		{
			BodyCollision.Shape = new CircleShape2D { Radius = Mathf.Max(8.0f, designData.CollisionRadius) };
			return;
		}

		int minX = image.GetWidth();
		int minY = image.GetHeight();
		int maxX = -1;
		int maxY = -1;
		for (int y = 0; y < image.GetHeight(); y++)
		{
			for (int x = 0; x < image.GetWidth(); x++)
			{
				if (image.GetPixel(x, y).A < 0.08f)
				{
					continue;
				}

				minX = Mathf.Min(minX, x);
				minY = Mathf.Min(minY, y);
				maxX = Mathf.Max(maxX, x);
				maxY = Mathf.Max(maxY, y);
			}
		}

		if (maxX < minX || maxY < minY)
		{
			BodyCollision.Shape = new CircleShape2D { Radius = Mathf.Max(8.0f, designData.CollisionRadius) };
			return;
		}

		Vector2 halfSize = new((maxX - minX + 1) * 0.5f, (maxY - minY + 1) * 0.5f);
		float targetRadius = designData.CollisionRadius > 0.0f ? designData.CollisionRadius : Mathf.Max(halfSize.X, halfSize.Y);
		float scale = targetRadius / Mathf.Max(Mathf.Max(halfSize.X, halfSize.Y), 1.0f);
		float rotation = Sprite != null && IsInstanceValid(Sprite) ? Sprite.Rotation : 0.0f;
		Vector2[] points =
		{
			new Vector2(-halfSize.X, -halfSize.Y) * scale,
			new Vector2(halfSize.X, -halfSize.Y) * scale,
			new Vector2(halfSize.X, halfSize.Y) * scale,
			new Vector2(-halfSize.X, halfSize.Y) * scale
		};
		for (int index = 0; index < points.Length; index++)
		{
			points[index] = points[index].Rotated(rotation);
		}

		BodyCollision.Shape = new ConvexPolygonShape2D { Points = points };
	}
	public override void _Process(double delta)
	{
		updateDebugHealthLabel();
		if (PlayerManager.Instance == null || PlayerManager.Instance.Player == null || CurrentEnemiesStates == null)
		{
			return;
		}

		advanceCombatEffects((float)delta);
		if (_lifetimeRemaining > 0.0f)
		{
			_lifetimeRemaining -= (float)delta;
			if (_lifetimeRemaining <= 0.0f)
			{
				OnDie();
				return;
			}
		}
		advanceAttack((float)delta);
		targetPosition = PlayerManager.Instance.Player.GlobalPosition;
		float threshold = Mathf.Max(12.0f, CurrentEnemiesStates.PositionThreshold);
		if (GlobalPosition.DistanceSquaredTo(targetPosition) > threshold * threshold)
		{
			IsMoving = true;
			IsRotate = true;
		}

		if (EnemySpawnerManager.Instance != null && EnemySpawnerManager.Instance.Player != null &&
			GlobalPosition.DistanceSquaredTo(EnemySpawnerManager.Instance.Player.GlobalPosition) >
			CurrentEnemiesStates.DespawnDistance)
		{
			OnDie(); // 离得太远，直接回收
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.Player == null || CurrentEnemiesStates == null)
		{
			return;
		}

		Vector2 targetPos = PlayerManager.Instance.Player.GlobalPosition;
		Vector2 targetDir = GlobalPosition.DirectionTo(targetPos); // 使用 DirectionTo 更简洁

		Vector2 avoidanceDir = Vector2.Zero;

		_rayFront.TargetPosition = targetDir * (CurrentSpeed * 0.5f + 10);//跑得快，射线应该长一点；跑得慢，射线短一点。
		if (_rayFront.IsColliding())
		{
			Vector2 normal = _rayFront.GetCollisionNormal();

			avoidanceDir = normal;

			// 强度可以根据需求调整
			float avoidanceForce = 3.0f;
			targetDir = (targetDir + avoidanceDir * avoidanceForce).Normalized();
		}

		if (IsMoving)
		{
			if (CurrentSpeed < CurrentEnemiesStates.Speed)
			{
				CurrentSpeed = Mathf.Min(CurrentSpeed + CurrentEnemiesStates.Acceleration * (float)delta, CurrentEnemiesStates.Speed * _movementMultiplier);
			}

			Velocity = targetDir * CurrentSpeed;

			if (GlobalPosition.DistanceSquaredTo(targetPos) < CurrentEnemiesStates.PositionThreshold * CurrentEnemiesStates.PositionThreshold)
			{
				IsMoving = false;
				IsRotate = false;
			}
		}
		else
		{
			CurrentSpeed = Mathf.Max(CurrentSpeed - CurrentEnemiesStates.Deceleration * (float)delta, 0);
			Velocity = targetDir * CurrentSpeed;
		}

		MoveAndSlide();

		if (IsRotate && Velocity.LengthSquared() > 1.0f)
		{
			Rotation = Mathf.LerpAngle(Rotation, Velocity.Angle(), (float)delta * CurrentEnemiesStates.RotateSpeed);
		}
	}
	private void OnInputEvent(Node viewport, InputEvent @event, long shapeIdx)
	{
		if (@event is InputEventMouseButton mouseBtn && mouseBtn.Pressed && mouseBtn.ButtonIndex == MouseButton.Left)
		{
			GlobalMessengerManager.Instance.SendTips(new TipsInfoMessages(Name, CurrentEnemiesStates.Health));
		}
	}
	public void OnHit(int damage)
	{
		if (ShieldLayers > 0)
		{
			ShieldLayers--;
			updateStatusEffectMaterial();
			return;
		}

		// GlobalMessengerManager.Instance.SendNotification($"敌人 {Name} 受到攻击！");
		updateBossExposure();
		CurrentEnemiesStates.Health -= Mathf.Max(1, Mathf.RoundToInt(damage * _damageTakenMultiplier));
		if (CurrentEnemiesStates.Health <= 0)
		{
			StringName tierId = DesignData != null ? DesignData.TierId : "NRM";
			int runExperience = tierId == "BOS" ? 100 : tierId == "ELT" ? 20 : 1;
			GlobalMessengerManager.Instance?.RegisterEnemyDefeated(tierId, runExperience);
			// CreateExp();
			CallDeferred(MethodName.CreateExp);
			OnDie();
			Reset();
		}
	}

	/// <summary>Applies the strongest active movement reduction without changing design data.</summary>
	public void ApplyMovementSlow(float multiplier, float duration)
	{
		if (duration <= 0.0f)
		{
			return;
		}

		if (DesignData != null)
		{
			duration *= 1.0f - Mathf.Clamp(DesignData.ControlResistance, 0.0f, 1.0f);
		}

		if (duration <= 0.0f) return;
		_movementMultiplier = Mathf.Min(_movementMultiplier, Mathf.Clamp(multiplier, 0.15f, 1.0f));
		_movementModifierRemaining = Mathf.Max(_movementModifierRemaining, duration);
		updateStatusEffectMaterial();
	}

	/// <summary>Schedules bounded periodic damage. It is reset when this pooled enemy returns.</summary>
	public void ApplyDamageOverTime(int damagePerTick, float duration, float tickInterval = 1.0f)
	{
		if (damagePerTick <= 0 || duration <= 0.0f)
		{
			return;
		}

		_damageOverTimePerTick = Mathf.Max(_damageOverTimePerTick, damagePerTick);
		_damageOverTimeRemaining = Mathf.Max(_damageOverTimeRemaining, duration);
		_damageOverTimeTickRemaining = Mathf.Min(_damageOverTimeTickRemaining <= 0.0f ? tickInterval : _damageOverTimeTickRemaining, tickInterval);
		updateStatusEffectMaterial();
	}

	public void OnDie()
	{
		EnemySpawnerManager.Instance.RemoveEnemy(this);
	}
	private void Reset()
	{
		CurrentEnemiesStates = EnemiesStates.Duplicate() as EnemyStatus;
		DesignData = null;
		ShieldLayers = 0;
		_movementMultiplier = 1.0f;
		_movementModifierRemaining = 0.0f;
		_damageOverTimeRemaining = 0.0f;
		_damageOverTimeTickRemaining = 0.0f;
		_damageOverTimePerTick = 0;
		_abilityRemaining = 0.0f;
		_damageTakenMultiplier = 1.0f;
		_spawnedMinionCount = 0;
		_lifetimeRemaining = 0.0f;
		updateStatusEffectMaterial();
		_lastDebugHealth = -1;
		_lastDebugShieldLayers = -1;
		hideDebugHealthLabel();
	}

	private void initializeStatusEffectMaterial()
	{
		if (Sprite == null || !IsInstanceValid(Sprite))
		{
			return;
		}

		Shader shader = GD.Load<Shader>(StatusEffectShaderPath);
		if (shader == null || !IsInstanceValid(shader))
		{
			LogUtil.Warning("Enemy status effect shader is unavailable.");
			return;
		}

		_statusEffectMaterial = new ShaderMaterial { Shader = shader };
		Sprite.Material = _statusEffectMaterial;
		updateStatusEffectMaterial();
	}

	private void updateStatusEffectMaterial()
	{
		if (_statusEffectMaterial == null || !IsInstanceValid(_statusEffectMaterial))
		{
			return;
		}

		_statusEffectMaterial.SetShaderParameter("burn_strength", _damageOverTimeRemaining > 0.0f ? 1.0f : 0.0f);
		_statusEffectMaterial.SetShaderParameter("slow_strength", _movementModifierRemaining > 0.0f ? 1.0f - _movementMultiplier : 0.0f);
		_statusEffectMaterial.SetShaderParameter("shield_active", ShieldLayers > 0 ? 1.0f : 0.0f);
	}

	private void updateDebugHealthLabel()
	{
		if (DebugHealthLabel == null || !IsInstanceValid(DebugHealthLabel))
		{
			return;
		}

		if (!MetaProgressStore.IsProgressionDebugEnabled() || CurrentEnemiesStates == null)
		{
			hideDebugHealthLabel();
			return;
		}

		DebugHealthLabel.GlobalPosition = GlobalPosition + new Vector2(-40.0f, -44.0f);
		if (!_wasDebugHealthVisible || _lastDebugHealth != CurrentEnemiesStates.Health || _lastDebugShieldLayers != ShieldLayers)
		{
			DebugHealthLabel.Text = ShieldLayers > 0
				? $"HP {CurrentEnemiesStates.Health}  Shield {ShieldLayers}"
				: $"HP {CurrentEnemiesStates.Health}";
			_lastDebugHealth = CurrentEnemiesStates.Health;
			_lastDebugShieldLayers = ShieldLayers;
		}

		_wasDebugHealthVisible = true;
		DebugHealthLabel.Show();
	}

	private void hideDebugHealthLabel()
	{
		if (DebugHealthLabel != null && IsInstanceValid(DebugHealthLabel))
		{
			DebugHealthLabel.Hide();
		}

		_wasDebugHealthVisible = false;
	}

	private void advanceCombatEffects(float delta)
	{
		if (_movementModifierRemaining > 0.0f)
		{
			_movementModifierRemaining -= delta;
			if (_movementModifierRemaining <= 0.0f)
			{
				_movementMultiplier = 1.0f;
				updateStatusEffectMaterial();
			}
		}

		if (_damageOverTimeRemaining <= 0.0f)
		{
			return;
		}

		_damageOverTimeRemaining -= delta;
		if (_damageOverTimeRemaining <= 0.0f)
		{
			_damageOverTimeRemaining = 0.0f;
			updateStatusEffectMaterial();
			return;
		}
		_damageOverTimeTickRemaining -= delta;
		if (_damageOverTimeTickRemaining > 0.0f)
		{
			return;
		}

		_damageOverTimeTickRemaining = 1.0f;
		OnHit(_damageOverTimePerTick);
	}

	private void advanceAttack(float delta)
	{
		if (DesignData == null || _abilityRemaining > 0.0f)
		{
			_abilityRemaining = Mathf.Max(0.0f, _abilityRemaining - delta);
			return;
		}

		if (HasActiveControlOrBurn && Pattern == AttackPattern.SummonPulse)
		{
			return;
		}

		PlayerController target = Pattern == AttackPattern.Contact ? getContactTarget() : getPreferredTarget();
		if (target == null)
		{
			return;
		}

		_abilityRemaining = Mathf.Max(0.2f, AbilityCooldown > 0.0f ? AbilityCooldown : DesignData.CooldownSeconds);
		switch (Pattern)
		{
			case AttackPattern.Contact:
				target.ReceiveEnemyHit();
				break;
			case AttackPattern.Ranged:
				spawnProjectile(target);
				break;
			case AttackPattern.ShieldPulse:
				applyEnemyShields();
				break;
			case AttackPattern.SummonPulse:
				spawnMinions();
				break;
			case AttackPattern.RepairPulse:
				repairNearestEnemy();
				break;
			default:
				return;
		}
	}

	private void onDetectionAreaEntered(Area2D area)
	{
		if (area is PlayerController player && IsInstanceValid(player))
		{
			_contactingPlayers.Add(player);
		}
	}

	private void onDetectionAreaExited(Area2D area)
	{
		if (area is PlayerController player)
		{
			_contactingPlayers.Remove(player);
		}
	}

	private PlayerController getContactTarget()
	{
		foreach (PlayerController player in _contactingPlayers)
		{
			if (player != null && IsInstanceValid(player))
			{
				return player;
			}
		}

		return null;
	}

	private PlayerController getPreferredTarget()
	{
		PlayerManager playerManager = PlayerManager.Instance;
		if (playerManager == null || !IsInstanceValid(playerManager)) return null;
		PlayerController preferred = null;
		float bestScore = float.MinValue;
		StringName roleId = DesignData?.RoleId ?? new StringName();
		Vector2 fleetCenter = playerManager.PlayersParent != null && IsInstanceValid(playerManager.PlayersParent)
			? playerManager.PlayersParent.GlobalPosition : Vector2.Zero;
		foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
		{
			if (player == null || !IsInstanceValid(player)) continue;
			float distance = GlobalPosition.DistanceSquaredTo(player.GlobalPosition);
			if (distance > AbilityRange * AbilityRange) continue;

			float score = -distance;
			if (roleId == "HAR")
			{
				score = distance;
			}
			else if (roleId == "ZON")
			{
				score = player.GlobalPosition.DistanceSquaredTo(fleetCenter);
			}
			else if (roleId == "JAM" || roleId == "CTL" || roleId == "FRM")
			{
				_adjacentPlayers.Clear();
				playerManager.GetAdjacentPlayers(player, _adjacentPlayers);
				score = _adjacentPlayers.Count * 100000.0f - distance;
			}

			if (score > bestScore) { bestScore = score; preferred = player; }
		}
		return preferred;
	}

	private void applyEnemyShields()
	{
		int protectedCount = 0;
		foreach (Node2D enemy in EnemySpawnerManager.Instance.ActiveEnemies)
		{
			if (enemy is EnemyController controller && controller != this && GlobalPosition.DistanceSquaredTo(controller.GlobalPosition) <= AbilityRange * AbilityRange)
			{
				controller.ShieldLayers += 2;
				controller.updateStatusEffectMaterial();
				if (++protectedCount >= AbilityTargets) return;
			}
		}
	}

	private void spawnMinions()
	{
		if (EnemySpawnerManager.Instance == null || ObjectPoolManager.Instance == null) return;
		int spawnCount = Mathf.Min(AbilityTargets, Mathf.Max(0, MaxSummonedMinionsPerEnemy - _spawnedMinionCount));
		for (int index = 0; index < spawnCount && EnemySpawnerManager.Instance.ActiveEnemies.Count < EnemySpawnerManager.Instance.MaxEnemyCount; index++)
		{
			Vector2 position = GlobalPosition + Vector2.Right.Rotated(Mathf.Tau * index / Mathf.Max(1, AbilityTargets)) * 48.0f;
			Node2D minion = SummonedChaserDesign?.EnemyScene != null && IsInstanceValid(SummonedChaserDesign.EnemyScene)
				? ObjectPoolManager.Instance.Create(PoolType.Enemy, SummonedChaserDesign.EnemyScene, position)
				: ObjectPoolManager.Instance.Spawn(PoolType.Enemy, position);
			if (minion is EnemyController controller)
			{
				controller.ApplyDesignData(SummonedChaserDesign ?? DesignData);
				EnemySpawnerManager.Instance.ActiveEnemies.Add(controller);
				_spawnedMinionCount++;
			}
		}
	}

	private void spawnProjectile(PlayerController target)
	{
		if (EnemyProjectileScene == null || !IsInstanceValid(EnemyProjectileScene) || GetParent() == null) return;
		EnemyProjectile projectile = EnemyProjectileScene.Instantiate<EnemyProjectile>();
		projectile.GlobalPosition = GlobalPosition;
		projectile.Configure(target, PlayerManager.Instance?.FleetMoveSpeed ?? 100.0f, ProjectileLifetimeSeconds);
		GetParent().AddChild(projectile);
	}

	private int getBossPhase()
	{
		if (CurrentEnemiesStates == null || DesignData == null || DesignData.BaseHealth <= 0) return 1;
		float healthRatio = (float)CurrentEnemiesStates.Health / DesignData.BaseHealth;
		return healthRatio > 0.66f ? 1 : healthRatio > 0.33f ? 2 : 3;
	}

	private void updateBossExposure()
	{
		if (DesignData == null || DesignData.TierId != "BOS") return;
		int phase = getBossPhase();
		if (phase < 3)
		{
			_damageTakenMultiplier = 1.0f;
			return;
		}

		_damageTakenMultiplier = DesignData.RoleId.ToString() switch
		{
			"CTL" => 1.25f,
			"FRM" => 1.35f,
			"ZON" => 1.50f,
			_ => 1.0f
		};
	}

	private void repairNearestEnemy()
	{
		if (EnemySpawnerManager.Instance == null) return;
		EnemyController target = null;
		float bestDistance = float.MaxValue;
		foreach (Node2D enemy in EnemySpawnerManager.Instance.ActiveEnemies)
		{
			if (enemy is not EnemyController controller || controller == this || controller.CurrentEnemiesStates == null || controller.DesignData == null) continue;
			if (controller.CurrentEnemiesStates.Health >= controller.DesignData.BaseHealth) continue;
			float distance = GlobalPosition.DistanceSquaredTo(controller.GlobalPosition);
			if (distance <= AbilityRange * AbilityRange && distance < bestDistance) { bestDistance = distance; target = controller; }
		}
		target?.Heal(10);
	}
	private void CreateExp()
	{
		ExpBallController ball = ObjectPoolManager.Instance.Spawn(PoolType.ExpBall, Position) as ExpBallController;
		ball.Init(RandomIndexUtil.GetWeightedIndex(RandomPoolType.ExpBall, MapResources.Instance.GetExpBallCount()));
	}

}

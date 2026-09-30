using Godot;
using System;

public partial class PlayerController : Area2D, IFireReady
{
    [Export] public BulletSpawn BulletSpawn;
    [Export] public PlayerShipData ShipData;
    [Export] private PlayerStatus PlayerStats;
    [Export] private Line2D TrailLine;
    [Export] public Area2D DetectionArea;
    [Export] public Sprite2D Sprite;
    [Export] public Timer AttackTimer;

    public PlayerStatus CurrentPlayerStates;
    public int ShieldCharges { get; private set; }
    public int PlayerIndex;
    public int StarLevel { get; private set; } = 1;
    public event Action<PlayerController> ShieldLost;

    private float _invulnerabilityRemaining;
    private float _enemyDisruptionRemaining;
    private float _nextShotDamageMultiplier = 1.0f;
    private ShaderMaterial _shieldOutlineMaterial;
    private const string ShieldOutlineShaderPath = "res://Material/Shader/shield_outline.gdshader";

    private const int MaxStarLevel = 4;
    private static readonly float[] StarDamageMultipliers = { 1.0f, 1.35f, 1.8f, 2.35f };
    private static readonly float[] StarAttackIntervalMultipliers = { 1.0f, 0.93f, 0.86f, 0.8f };

    public override void _Ready()
    {
        CurrentPlayerStates = PlayerStats.Duplicate() as PlayerStatus;
        AttackTimer.Timeout += CanFire;
        initializeShieldOutlineMaterial();
    }

    public override void _Process(double delta)
    {
        if (_invulnerabilityRemaining > 0.0f)
        {
            _invulnerabilityRemaining = Mathf.Max(0.0f, _invulnerabilityRemaining - (float)delta);
        }

        if (_enemyDisruptionRemaining > 0.0f)
        {
            _enemyDisruptionRemaining = Mathf.Max(0.0f, _enemyDisruptionRemaining - (float)delta);
            if (_enemyDisruptionRemaining <= 0.0f && AttackTimer != null && IsInstanceValid(AttackTimer))
            {
                AttackTimer.Start();
            }
        }
    }

    public void OnLoss()
    {
        GlobalMessengerManager.Instance.SendPlayerLoss(PlayerIndex);
    }

    public void ReceiveEnemyHit()
    {
        applyDamage();
    }

    public void ApplyEnemyDisruption(float duration)
    {
        _enemyDisruptionRemaining = Mathf.Max(_enemyDisruptionRemaining, duration);
        if (AttackTimer != null && IsInstanceValid(AttackTimer)) AttackTimer.Stop();
    }

    public void Reset()
    {
        PlayerIndex = 0;
        ShieldCharges = 0;
        updateShieldOutlineMaterial();
        StarLevel = 1;
        _invulnerabilityRemaining = 0.0f;
        _enemyDisruptionRemaining = 0.0f;
        _nextShotDamageMultiplier = 1.0f;
        CurrentPlayerStates = PlayerStats.Duplicate() as PlayerStatus;
        TrailLine.ClearPoints();
        ShipEffectController effectController = GetNodeOrNull<ShipEffectController>("EffectController");
        effectController?.ResetEffects();
    }

    public void addShieldCharges(int charges)
    {
        if (charges <= 0)
        {
            return;
        }

        ShieldCharges += charges;
        updateShieldOutlineMaterial();
        CardBenefitRuntime.OnShieldGranted(this, charges);
    }

    public bool TryConsumeShieldCharge()
    {
        if (ShieldCharges <= 0)
        {
            return false;
        }

        ShieldCharges--;
        updateShieldOutlineMaterial();
        return true;
    }

    public void GrantInvulnerability(float duration)
    {
        _invulnerabilityRemaining = Mathf.Max(_invulnerabilityRemaining, Mathf.Max(0.0f, duration));
    }

    public void GrantNextShotDamageMultiplier(float multiplier)
    {
        _nextShotDamageMultiplier = Mathf.Max(_nextShotDamageMultiplier, Mathf.Clamp(multiplier, 1.0f, 3.0f));
    }

    public float ConsumeNextShotDamageMultiplier()
    {
        float multiplier = _nextShotDamageMultiplier;
        _nextShotDamageMultiplier = 1.0f;
        return multiplier;
    }

    public void applyShipData(PlayerShipData shipData)
    {
        if (shipData == null || !IsInstanceValid(shipData))
        {
            return;
        }

        ShipData = shipData;
        if (CurrentPlayerStates != null && IsInstanceValid(CurrentPlayerStates))
        {
            CurrentPlayerStates.Hp = 1;
            CurrentPlayerStates.RotateSpeed = shipData.RotateSpeed;
            CurrentPlayerStates.AttackRange = shipData.AttackRange * getRangeMultiplier();
            CurrentPlayerStates.AttackInterval = shipData.AttackInterval;
        }

        if (BulletSpawn != null && IsInstanceValid(BulletSpawn))
        {
            BulletSpawn.Damage = Mathf.RoundToInt(shipData.AttackPower * getDamageMultiplier());
            BulletSpawn.ConfigureShipProjectile(shipData.ShipId);
        }

        if (AttackTimer != null && IsInstanceValid(AttackTimer))
        {
            AttackTimer.WaitTime = shipData.AttackInterval * getAttackIntervalMultiplier();
        }

        if (Sprite != null && IsInstanceValid(Sprite) && shipData.Icon != null)
        {
            Sprite.Texture = shipData.Icon;
        }

        ShipEffectController effectController = GetNodeOrNull<ShipEffectController>("EffectController");
        effectController?.OnShipDataApplied(shipData);
        applySelectedRunUpgrades(effectController, shipData.ShipId);
    }

    public bool tryPromoteStarLevel()
    {
        if (StarLevel >= MaxStarLevel || ShipData == null || !IsInstanceValid(ShipData))
        {
            return false;
        }

        float previousDamageMultiplier = getDamageMultiplier();
        float previousRangeMultiplier = getRangeMultiplier();
        StarLevel++;

        if (BulletSpawn != null && IsInstanceValid(BulletSpawn))
        {
            BulletSpawn.Damage = Mathf.RoundToInt(BulletSpawn.Damage * getDamageMultiplier() / previousDamageMultiplier);
        }

        if (CurrentPlayerStates != null && IsInstanceValid(CurrentPlayerStates))
        {
            CurrentPlayerStates.AttackRange *= getRangeMultiplier() / previousRangeMultiplier;
        }

        if (AttackTimer != null && IsInstanceValid(AttackTimer))
        {
            AttackTimer.WaitTime = ShipData.AttackInterval * getAttackIntervalMultiplier();
        }

        return true;
    }

    public void ApplyRunUpgrade(ShipUpgradeNodeData node)
    {
        ShipEffectController effectController = GetNodeOrNull<ShipEffectController>("EffectController");
        effectController?.ApplyRunUpgrade(node);
    }

    private static void applySelectedRunUpgrades(ShipEffectController effectController, StringName shipId)
    {
        if (effectController == null || !IsInstanceValid(effectController) || EnhanceFunctionManager.Instance == null)
        {
            return;
        }

        foreach (StringName nodeId in RunShipUpgradeState.GetSelectedNodeIds(shipId))
        {
            if (EnhanceFunctionManager.Instance.TryGetShipUpgradeNode(nodeId, out ShipUpgradeNodeData node))
            {
                effectController.ApplyRunUpgrade(node);
            }
        }
    }

    public void OnFire(Vector2 target)
    {
        if (BulletSpawn != null && IsInstanceValid(BulletSpawn) && BulletSpawn.IsInsideTree())
        {
            BulletSpawn.SpawnBullet(target);
        }
        else if (PlayerManager.Instance != null && PlayerManager.Instance.PlayerBulletSpawns != null &&
            PlayerManager.Instance.PlayerBulletSpawns.ContainsKey(PlayerIndex))
        {
            BulletSpawn bulletSpawn = PlayerManager.Instance.PlayerBulletSpawns[PlayerIndex];
            if (bulletSpawn != null && IsInstanceValid(bulletSpawn) && bulletSpawn.IsInsideTree())
            {
                bulletSpawn.SpawnBullet(target);
            }
            else
            {
                LogUtil.Warning($"Player {PlayerIndex} BulletSpawn invalid when firing.");
            }
        }
        else
        {
            LogUtil.Warning($"Player {PlayerIndex} has no valid BulletSpawn when firing.");
        }

        AttackTimer.Start();
    }

    // A hull is lost on its first unshielded hit. Enemy contact itself is not damage:
    // damage must arrive through a visible enemy attack effect or projectile.
    private void applyDamage()
    {
        CardBenefitRuntime.OnPlayerDamaged(this);
        if (_invulnerabilityRemaining > 0.0f)
        {
            return;
        }

        if (ShieldCharges > 0)
        {
            ShieldCharges--;
            updateShieldOutlineMaterial();
            ShieldLost?.Invoke(this);
            CardBenefitRuntime.OnShieldLost(this);
            LogUtil.Info($"飞船 {PlayerIndex} 的护盾抵挡了一次伤害，剩余 {ShieldCharges} 层。");
            return;
        }

        if (CardBenefitRuntime.TryPreventPlayerLoss(this))
        {
            return;
        }

        OnLoss();
    }

    private void CanFire()
    {
        PlayerManager.Instance.ReadyToAttackQueue.Enqueue(this);
    }

    private float getDamageMultiplier()
    {
        return StarDamageMultipliers[Mathf.Clamp(StarLevel - 1, 0, StarDamageMultipliers.Length - 1)];
    }

    private void initializeShieldOutlineMaterial()
    {
        if (Sprite == null || !IsInstanceValid(Sprite))
        {
            return;
        }

        Shader shader = GD.Load<Shader>(ShieldOutlineShaderPath);
        if (shader == null || !IsInstanceValid(shader))
        {
            LogUtil.Warning("Player shield outline shader is unavailable.");
            return;
        }

        _shieldOutlineMaterial = new ShaderMaterial { Shader = shader };
        Sprite.Material = _shieldOutlineMaterial;
        updateShieldOutlineMaterial();
    }

    private void updateShieldOutlineMaterial()
    {
        if (_shieldOutlineMaterial == null || !IsInstanceValid(_shieldOutlineMaterial))
        {
            return;
        }

        _shieldOutlineMaterial.SetShaderParameter("shield_active", ShieldCharges > 0 ? 1.0f : 0.0f);
    }

    private float getRangeMultiplier()
    {
        return 1.0f + (StarLevel - 1) * 0.08f;
    }

    private float getAttackIntervalMultiplier()
    {
        return StarAttackIntervalMultipliers[Mathf.Clamp(StarLevel - 1, 0, StarAttackIntervalMultipliers.Length - 1)];
    }
}

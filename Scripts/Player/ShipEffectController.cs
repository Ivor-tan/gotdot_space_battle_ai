using Godot;
using System.Collections.Generic;

/// <summary>
/// Shared lifecycle boundary for ship-specific combat effects. Concrete ships
/// own only their special rules; PlayerController remains responsible for base
/// stats, targeting, shields, pooling, and firing cadence.
/// </summary>
public partial class ShipEffectController : Node
{
    protected PlayerController Player;
    private readonly Dictionary<StringName, int> _branchTiers = new();
    protected StringName _shipId;
    private int _shotsSinceSupportPulse;
    private float _nextSolarFlareTime;
    private float _nextFrostSplitTime;
    private int _solarSiegeShotCount;
    private int _assaultImpactHitCount;
    private int _stormDistinctHitCount;
    private ShipAttackRangeEffect _attackRangeEffect;
    private ShipSingleImpactEffect _singleImpactEffect;

    public override void _Ready()
    {
        Player = GetParent() as PlayerController;
        if (Player == null || !IsInstanceValid(Player))
        {
            LogUtil.Warning("Ship effect controller requires a PlayerController parent.");
        }
    }

    public virtual void OnShipDataApplied(PlayerShipData shipData)
    {
        if (shipData == null || !IsInstanceValid(shipData) || !TryGetPlayer(out PlayerController player))
        {
            return;
        }

        _shipId = shipData.ShipId;
        if (player.BulletSpawn == null || !IsInstanceValid(player.BulletSpawn))
        {
            LogUtil.Warning($"Ship effect controller could not bind BulletSpawn for {_shipId}.");
            return;
        }

        player.BulletSpawn.BulletSpawned -= onBulletSpawned;
        player.BulletSpawn.BulletSpawned += onBulletSpawned;
        player.ShieldLost -= onOwnerShieldLost;
        player.ShieldLost += onOwnerShieldLost;
    }

    protected bool TryGetPlayer(out PlayerController player)
    {
        if (Player == null || !IsInstanceValid(Player))
        {
            Player = GetParent() as PlayerController;
        }

        player = Player;
        return player != null && IsInstanceValid(player);
    }

    public virtual void ResetEffects()
    {
        _branchTiers.Clear();
        _shipId = default;
        _shotsSinceSupportPulse = 0;
        _nextSolarFlareTime = 0.0f;
        _nextFrostSplitTime = 0.0f;
        _solarSiegeShotCount = 0;
        _assaultImpactHitCount = 0;
        _stormDistinctHitCount = 0;
    }

    public virtual void ApplyRunUpgrade(ShipUpgradeNodeData node)
    {
        if (node == null || !IsInstanceValid(node) || node.ShipId != _shipId)
        {
            return;
        }

        _branchTiers[node.BranchId] = Mathf.Max(getTier(node.BranchId), node.Tier);
    }

    private void onBulletSpawned(BulletController bullet)
    {
        if (bullet == null || !IsInstanceValid(bullet) || !TryGetPlayer(out PlayerController player))
        {
            return;
        }

        switch (_shipId.ToString())
        {
            case "SHP_PLY_ART_001":
                configureCoronaShot(bullet);
                break;
            case "SHP_PLY_CTL_001":
                configureTideShot(bullet);
                break;
            case "SHP_PLY_GRD_001":
                configurePrismShot(bullet);
                break;
            case "SHP_PLY_SUP_001":
                configureFoldlightShot(bullet, player);
                break;
            case "SHP_PLY_CAR_001":
                configureHiveShot(bullet, player);
                break;
            case "SHP_PLY_ASS_001":
                configureBreakerShot(bullet, player);
                break;
            case "SHP_PLY_ART_002":
                configureFrostShot(bullet);
                break;
            case "SHP_PLY_CTL_002":
                configureStormShot(bullet);
                break;
            case "SHP_PLY_GRD_002":
                configureIronShot(bullet, player);
                break;
        }

        applyRingShotEffect(bullet, player);
        bullet.HitConfirmed += onBulletHit;
    }

    private void onBulletHit(BulletController bullet, Node2D target, int damage)
    {
        if (bullet == null || !IsInstanceValid(bullet))
        {
            return;
        }

        getAttackRangeStyle(out float defaultAreaRadius, out Color color, out float segmentCount, out float swirlStrength);
        if (bullet.HasSplashDamage)
        {
            float areaRadius = Mathf.Max(defaultAreaRadius, bullet.SplashRadius);
            showAttackRangeEffect(bullet.GlobalPosition, areaRadius, color, segmentCount, swirlStrength);
            return;
        }

        showSingleImpactEffect(bullet.GlobalPosition, color);
    }

    protected void showAttackRangeEffect(Vector2 position, float radius, Color color, float segmentCount, float swirlStrength)
    {
        if (!TryGetPlayer(out PlayerController player))
        {
            return;
        }

        if (_attackRangeEffect == null || !IsInstanceValid(_attackRangeEffect))
        {
            _attackRangeEffect = new ShipAttackRangeEffect();
            player.AddChild(_attackRangeEffect);
        }

        _attackRangeEffect.Play(position, radius, color, segmentCount, swirlStrength, 0.34f);
    }

    protected void showSingleImpactEffect(Vector2 position, Color color)
    {
        if (!TryGetPlayer(out PlayerController player))
        {
            return;
        }

        if (_singleImpactEffect == null || !IsInstanceValid(_singleImpactEffect))
        {
            _singleImpactEffect = new ShipSingleImpactEffect();
            player.AddChild(_singleImpactEffect);
        }

        _singleImpactEffect.Play(position, color);
    }

    protected void getAttackRangeStyle(out float radius, out Color color, out float segmentCount, out float swirlStrength)
    {
        radius = 58.0f;
        color = new Color(0.32f, 0.78f, 1.0f, 0.9f);
        segmentCount = 6.0f;
        swirlStrength = 0.15f;

        switch (_shipId.ToString())
        {
            case "SHP_PLY_FLG_001":
                radius = 76.0f;
                color = new Color(0.96f, 0.76f, 0.24f, 0.94f);
                segmentCount = 5.0f;
                swirlStrength = 0.42f;
                break;
            case "SHP_PLY_ART_001":
                radius = 82.0f;
                color = new Color(1.0f, 0.40f, 0.12f, 0.92f);
                segmentCount = 8.0f;
                break;
            case "SHP_PLY_CTL_001":
                radius = 72.0f;
                color = new Color(0.36f, 0.58f, 1.0f, 0.9f);
                segmentCount = 4.0f;
                swirlStrength = 0.90f;
                break;
            case "SHP_PLY_GRD_001":
                radius = 62.0f;
                color = new Color(0.70f, 0.88f, 1.0f, 0.88f);
                segmentCount = 6.0f;
                break;
            case "SHP_PLY_SUP_001":
                radius = 54.0f;
                color = new Color(0.90f, 0.46f, 1.0f, 0.86f);
                segmentCount = 3.0f;
                swirlStrength = 0.55f;
                break;
            case "SHP_PLY_CAR_001":
                radius = 60.0f;
                color = new Color(0.38f, 1.0f, 0.58f, 0.86f);
                segmentCount = 10.0f;
                swirlStrength = 0.35f;
                break;
            case "SHP_PLY_ASS_001":
                radius = 58.0f;
                color = new Color(1.0f, 0.84f, 0.24f, 0.92f);
                segmentCount = 2.0f;
                break;
            case "SHP_PLY_ART_002":
                radius = 96.0f;
                color = new Color(0.40f, 0.92f, 1.0f, 0.9f);
                segmentCount = 7.0f;
                swirlStrength = 0.25f;
                break;
            case "SHP_PLY_CTL_002":
                radius = 86.0f;
                color = new Color(0.58f, 0.36f, 1.0f, 0.92f);
                segmentCount = 12.0f;
                swirlStrength = 0.75f;
                break;
            case "SHP_PLY_GRD_002":
                radius = 76.0f;
                color = new Color(1.0f, 0.66f, 0.22f, 0.9f);
                segmentCount = 6.0f;
                break;
        }
    }

    private void applyRingShotEffect(BulletController bullet, PlayerController player)
    {
        int ring = getRingIndex(player);
        float targetDistance = bullet.targetPosition.DistanceTo(player.GlobalPosition);
        switch (_shipId.ToString())
        {
            case "SHP_PLY_ART_001":
                if (ring == 0)
                {
                    bullet.MultiplySplashRadius(1.15f);
                }
                else if (ring == 1 && targetDistance <= 220.0f)
                {
                    bullet.MultiplyDamageMultiplier(1.20f);
                }
                else if (ring == 2 && targetDistance > 380.0f)
                {
                    bullet.MultiplySplashRadius(1.20f);
                }
                break;
            case "SHP_PLY_ART_002":
                if (ring == 0)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.80f, 2.0f);
                }
                else if (ring == 1 && targetDistance <= 260.0f)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.65f, 1.0f);
                }
                else if (ring == 2)
                {
                    bullet.MultiplyDamageMultiplier(1.10f);
                }
                break;
            case "SHP_PLY_CTL_001":
                if (ring == 0)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.85f, 1.15f);
                }
                else if (ring == 1)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.80f, 1.0f);
                }
                else if (ring == 2)
                {
                    bullet.ConfigureSpecialShot(1, 50.0f, 0.20f);
                }
                break;
            case "SHP_PLY_CAR_001":
                if (ring == 0)
                {
                    bullet.MultiplyDamageMultiplier(1.08f);
                }
                else if (ring == 2)
                {
                    bullet.MultiplyDamageMultiplier(1.15f);
                }
                break;
            case "SHP_PLY_ASS_001":
                if (ring == 1 && targetDistance <= 180.0f)
                {
                    bullet.MultiplyDamageMultiplier(1.25f);
                }
                else if (ring == 2 && targetDistance > 360.0f)
                {
                    player.GrantNextShotDamageMultiplier(1.20f);
                }
                break;
            case "SHP_PLY_GRD_001":
                if (ring == 1 && targetDistance <= 180.0f)
                {
                    bullet.MultiplyDamageMultiplier(1.25f);
                }
                else if (ring == 2)
                {
                    bullet.MultiplyDamageMultiplier(1.10f);
                }
                break;
            case "SHP_PLY_GRD_002":
                if (ring == 1)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.88f, 1.0f);
                }
                else if (ring == 2)
                {
                    bullet.ConfigureSpecialShot(1, 60.0f, 0.30f);
                }
                break;
            case "SHP_PLY_SUP_001":
                if (ring == 0)
                {
                    player.GrantInvulnerability(1.0f);
                }
                else if (ring == 1)
                {
                    _shotsSinceSupportPulse++;
                    if (_shotsSinceSupportPulse >= 4)
                    {
                        _shotsSinceSupportPulse = 0;
                        player.addShieldCharges(1);
                    }
                }
                else if (ring == 2)
                {
                    bullet.MultiplyDamageMultiplier(1.30f);
                }
                break;
            case "SHP_PLY_CTL_002":
                if (ring == 0)
                {
                    bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.45f, 0.3f);
                }
                else if (ring == 2)
                {
                    bullet.ConfigureSpecialShot(2, 0.0f, 0.0f);
                }
                break;
        }
    }

    private static void applySlow(Node2D target, float multiplier, float duration)
    {
        if (target is EnemyController enemy)
        {
            enemy.ApplyMovementSlow(multiplier, duration);
        }
    }

    private int getRingIndex(PlayerController player)
    {
        return PlayerManager.Instance != null ? PlayerManager.Instance.GetRingIndex(player) : -1;
    }

    private void configureCoronaShot(BulletController bullet)
    {
        int furnaceTier = getTier("ULK_UPG_SOL_FUR");
        int flareTier = getTier("ULK_UPG_SOL_FLR");
        int siegeTier = getTier("ULK_UPG_SOL_SIE");
        bullet.ConfigureSpecialShot(0, 70.0f + flareTier * 10.0f, 0.55f);
        if (siegeTier > 0 && TryGetPlayer(out PlayerController player) && bullet.targetPosition.DistanceTo(player.GlobalPosition) > 380.0f)
        {
            bullet.ConfigureDamageMultiplier(1.0f + siegeTier * 0.20f);
            if (siegeTier >= 2)
            {
                bullet.HitConfirmed += (_, target, _) => applySlow(target, 0.75f, 2.0f);
            }

            if (siegeTier >= 3 && ++_solarSiegeShotCount >= 3)
            {
                _solarSiegeShotCount = 0;
                bullet.MultiplySplashRadius(1.45f);
                bullet.MultiplyDamageMultiplier(1.40f);
            }
        }
        bullet.HitConfirmed += (_, target, damage) => applyCoronaHit(target, damage, furnaceTier, flareTier);

        if (flareTier > 0 && TryGetPlayer(out PlayerController flarePlayer))
        {
            bullet.HitConfirmed += (_, target, _) => spawnSolarFlares(flarePlayer, target, flareTier);
        }
    }

    private static void applyCoronaHit(Node2D target, int damage, int furnaceTier, int flareTier)
    {
        if (target is not EnemyController enemy)
        {
            return;
        }

        if (furnaceTier > 0)
        {
            enemy.ApplyDamageOverTime(Mathf.Max(1, Mathf.RoundToInt(damage * 0.20f)), 3.0f);
        }
        if (flareTier >= 2)
        {
            enemy.ApplyMovementSlow(0.80f, 1.5f);
        }
    }

    private void spawnSolarFlares(PlayerController player, Node2D target, int flareTier)
    {
        if (target == null || !IsInstanceValid(target) || player.BulletSpawn == null || !IsInstanceValid(player.BulletSpawn))
        {
            return;
        }

        float now = (float)Time.GetTicksMsec() * 0.001f;
        int count = now >= _nextSolarFlareTime && flareTier >= 3 ? 5 : 2;
        if (count == 5)
        {
            _nextSolarFlareTime = now + 6.0f;
        }

        float damageMultiplier = count == 5 ? 0.22f : 0.30f;
        Vector2 impactPosition = target.GlobalPosition;
        CallDeferred(nameof(spawnSolarFlaresDeferred), player, impactPosition, count, damageMultiplier);
    }

    private void spawnSolarFlaresDeferred(PlayerController player, Vector2 impactPosition, int count, float damageMultiplier)
    {
        if (player == null || !IsInstanceValid(player) || player.BulletSpawn == null || !IsInstanceValid(player.BulletSpawn))
        {
            return;
        }

        for (int index = 0; index < count; index++)
        {
            float angle = Mathf.Tau * index / count;
            player.BulletSpawn.SpawnSupplementaryBullet(impactPosition + Vector2.FromAngle(angle) * 28.0f, 0.0f, damageMultiplier);
        }
    }

    private void configureTideShot(BulletController bullet)
    {
        int gravityTier = getTier("ULK_UPG_TID_GRV");
        int relayTier = getTier("ULK_UPG_TID_RLY");
        int waveTier = getTier("ULK_UPG_TID_WAV");
        if (waveTier > 0)
        {
            bullet.ConfigureSpecialShot(waveTier >= 3 ? 2 : 1, 45.0f + waveTier * 10.0f, 0.25f);
        }
        bullet.HitConfirmed += (_, target, damage) =>
        {
            if (target is EnemyController enemy)
            {
                enemy.ApplyMovementSlow(gravityTier >= 2 ? 0.80f : 0.90f, gravityTier > 0 ? 2.0f : 0.8f);
                if (relayTier > 0)
                {
                    enemy.OnHit(Mathf.Max(1, Mathf.RoundToInt(damage * 0.12f * relayTier)));
                }
            }
        };
    }

    private void configurePrismShot(BulletController bullet)
    {
        int guardTier = getTier("ULK_UPG_PRM_GUA");
        int rayTier = getTier("ULK_UPG_PRM_RAY");
        int bastionTier = getTier("ULK_UPG_PRM_BST");
        if (bastionTier > 0)
        {
            bullet.ConfigureDamageMultiplier(1.0f + bastionTier * 0.05f);
        }
        bullet.HitConfirmed += (_, target, damage) =>
        {
            if (target is EnemyController enemy && TryGetPlayer(out PlayerController player) &&
                enemy.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= 180.0f * 180.0f)
            {
                enemy.OnHit(Mathf.RoundToInt(damage * (0.25f + rayTier * 0.05f)));
                if (guardTier >= 2)
                {
                    player.addShieldCharges(1);
                }
            }
        };
    }

    private void configureFoldlightShot(BulletController bullet, PlayerController player)
    {
        _shotsSinceSupportPulse++;
        int shieldTier = getTier("ULK_UPG_FLD_SHD");
        int rallyTier = getTier("ULK_UPG_FLD_RLY");
        if (shieldTier > 0 && _shotsSinceSupportPulse >= Mathf.Max(4, 10 - shieldTier))
        {
            _shotsSinceSupportPulse = 0;
            grantFoldlightShield(player, shieldTier >= 3);
        }
        if (getTier("ULK_UPG_FLD_RES") >= 2)
        {
            bullet.ConfigureDamageMultiplier(1.12f);
        }
        if (rallyTier >= 2 && _shotsSinceSupportPulse == 0)
        {
            player.addShieldCharges(1);
        }
    }

    private void grantFoldlightShield(PlayerController player, bool bounceToSecondTarget)
    {
        if (PlayerManager.Instance == null)
        {
            player.addShieldCharges(1);
            player.GrantInvulnerability(2.0f);
            return;
        }

        Godot.Collections.Array<PlayerController> adjacent = new();
        PlayerManager.Instance.GetAdjacentPlayers(player, adjacent);
        PlayerController target = player;
        foreach (PlayerController candidate in adjacent)
        {
            if (candidate.ShieldCharges < target.ShieldCharges)
            {
                target = candidate;
            }
        }

        target.addShieldCharges(1);
        target.GrantInvulnerability(2.0f);
        if (!bounceToSecondTarget || adjacent.Count < 2)
        {
            return;
        }

        PlayerController secondary = null;
        foreach (PlayerController candidate in adjacent)
        {
            if (candidate != target && (secondary == null || candidate.ShieldCharges < secondary.ShieldCharges))
            {
                secondary = candidate;
            }
        }

        if (secondary != null && IsInstanceValid(secondary))
        {
            secondary.addShieldCharges(1);
            secondary.GrantInvulnerability(2.0f);
        }
    }

    private void onOwnerShieldLost(PlayerController player)
    {
        if (player == null || !IsInstanceValid(player))
        {
            return;
        }

        if (_shipId == "SHP_PLY_GRD_001")
        {
            if (getTier("ULK_UPG_PRM_RAY") >= 2)
            {
                applySlowInRadius(player.GlobalPosition, 140.0f, 0.70f, 1.0f);
            }
            if (getTier("ULK_UPG_PRM_GUA") >= 3)
            {
                player.GrantNextShotDamageMultiplier(1.80f);
            }
        }
        else if (_shipId == "SHP_PLY_GRD_002" && getTier("ULK_UPG_IRN_CNT") > 0)
        {
            player.GrantNextShotDamageMultiplier(1.70f);
        }
    }

    private static void applySlowInRadius(Vector2 origin, float radius, float multiplier, float duration)
    {
        if (EnemySpawnerManager.Instance == null)
        {
            return;
        }

        float radiusSquared = radius * radius;
        foreach (Node2D node in EnemySpawnerManager.Instance.ActiveEnemies)
        {
            if (node is EnemyController enemy && IsInstanceValid(enemy) &&
                enemy.GlobalPosition.DistanceSquaredTo(origin) <= radiusSquared)
            {
                enemy.ApplyMovementSlow(multiplier, duration);
            }
        }
    }

    private void configureHiveShot(BulletController bullet, PlayerController player)
    {
        int swarmTier = getTier("ULK_UPG_HIV_SWM");
        int huntTier = getTier("ULK_UPG_HIV_HNT");
        int guardTier = getTier("ULK_UPG_HIV_GRD");
        bullet.ConfigureSpecialShot(swarmTier > 0 ? 1 : 0, guardTier > 0 ? 45.0f : 0.0f, guardTier > 0 ? 0.25f : 0.0f);
        if (swarmTier > 0)
        {
            int droneShots = swarmTier >= 3 ? 2 : 1;
            for (int index = 0; index < droneShots; index++)
            {
                float offset = droneShots == 1 ? 0.0f : index == 0 ? -0.12f : 0.12f;
                player.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, offset, 0.35f + swarmTier * 0.10f);
            }
        }
        if (huntTier > 0)
        {
            bullet.ConfigureDamageMultiplier(1.0f + huntTier * 0.10f);
        }
        if (guardTier >= 2)
        {
            player.addShieldCharges(1);
        }
    }

    private void configureBreakerShot(BulletController bullet, PlayerController player)
    {
        int impactTier = getTier("ULK_UPG_ASS_IMP");
        int shockTier = getTier("ULK_UPG_ASS_SHK");
        int huntTier = getTier("ULK_UPG_ASS_HNT");
        if (huntTier > 0)
        {
            bullet.ConfigureDamageMultiplier(1.0f + huntTier * 0.09f);
        }
        if (bullet.targetPosition.DistanceSquaredTo(player.GlobalPosition) <= 180.0f * 180.0f)
        {
            bullet.ConfigureDamageMultiplier(1.0f + 0.25f * Mathf.Max(1, impactTier));
            if (shockTier > 0)
            {
                bullet.ConfigureSpecialShot(0, 45.0f + shockTier * 5.0f, 0.25f);
            }

            bullet.HitConfirmed += (_, target, _) =>
            {
                _assaultImpactHitCount++;
                if (shockTier >= 3 && _assaultImpactHitCount % 6 == 0)
                {
                    applySlow(target, 0.65f, 1.0f);
                }
            };
        }
    }

    private void configureFrostShot(BulletController bullet)
    {
        int coldTier = getTier("ULK_UPG_FRS_COL");
        int shardTier = getTier("ULK_UPG_FRS_SHR");
        int zoneTier = getTier("ULK_UPG_FRS_ZON");
        bullet.ConfigureSpecialShot(shardTier >= 2 ? 1 : 0, 75.0f + zoneTier * 15.0f, 0.50f);
        bullet.HitConfirmed += (_, target, _) =>
        {
            if (target is EnemyController enemy)
            {
                enemy.ApplyMovementSlow(Mathf.Max(0.45f, 0.85f - coldTier * 0.10f), 1.0f + zoneTier * 0.5f);
            }
        };

        if (shardTier > 0 && TryGetPlayer(out PlayerController player))
        {
            bullet.HitConfirmed += (_, target, _) => spawnFrostShards(player, target, shardTier);
        }
    }

    private void spawnFrostShards(PlayerController player, Node2D target, int shardTier)
    {
        if (target == null || !IsInstanceValid(target) || player.BulletSpawn == null || !IsInstanceValid(player.BulletSpawn))
        {
            return;
        }

        float now = (float)Time.GetTicksMsec() * 0.001f;
        int shardCount = shardTier >= 3 && now >= _nextFrostSplitTime ? 3 : 2;
        if (shardCount == 3)
        {
            _nextFrostSplitTime = now + 7.0f;
        }

        for (int index = 0; index < shardCount; index++)
        {
            float offset = (index - (shardCount - 1) * 0.5f) * 0.18f;
            player.BulletSpawn.SpawnSupplementaryBullet(target.GlobalPosition, offset, 0.30f);
        }
    }

    private void configureStormShot(BulletController bullet)
    {
        int chainTier = getTier("ULK_UPG_STM_CHN");
        int overloadTier = getTier("ULK_UPG_STM_OVR");
        int netTier = getTier("ULK_UPG_STM_NET");
        bullet.ConfigureSpecialShot(chainTier > 0 ? 1 : 0, 0.0f, 0.0f);
        bullet.HitConfirmed += (_, target, damage) =>
        {
            if (target is EnemyController enemy && netTier > 0)
            {
                enemy.ApplyMovementSlow(0.75f, 1.0f + netTier * 0.5f);
            }
            applyStormChain(target, damage, chainTier, overloadTier);
            if (chainTier >= 3 && target != null && IsInstanceValid(target))
            {
                _stormDistinctHitCount++;
                if (_stormDistinctHitCount >= 4)
                {
                    _stormDistinctHitCount = 0;
                    applySlow(target, 0.15f, 0.5f);
                }
            }
        };
    }

    private static void applyStormChain(Node2D target, int damage, int chainTier, int overloadTier)
    {
        if (target == null || !IsInstanceValid(target) || EnemySpawnerManager.Instance == null)
        {
            return;
        }

        Node2D closest = null;
        float closestDistanceSquared = 160.0f * 160.0f;
        foreach (Node2D enemy in EnemySpawnerManager.Instance.ActiveEnemies)
        {
            if (enemy == null || !IsInstanceValid(enemy) || enemy == target || enemy is not IDamageable)
            {
                continue;
            }

            float distanceSquared = enemy.GlobalPosition.DistanceSquaredTo(target.GlobalPosition);
            if (distanceSquared < closestDistanceSquared)
            {
                closest = enemy;
                closestDistanceSquared = distanceSquared;
            }
        }

        if (closest is IDamageable chainedTarget)
        {
            chainedTarget.OnHit(Mathf.Max(1, Mathf.RoundToInt(damage * (chainTier > 0 ? 0.65f : 0.40f))));
            if (closest is EnemyController chainedEnemy && overloadTier > 0)
            {
                chainedEnemy.ApplyMovementSlow(0.80f, 1.0f + overloadTier * 0.5f);
            }
        }
    }

    private void configureIronShot(BulletController bullet, PlayerController player)
    {
        int frontlineTier = getTier("ULK_UPG_IRN_FRN");
        int counterTier = getTier("ULK_UPG_IRN_CNT");
        int coverTier = getTier("ULK_UPG_IRN_COV");
        bullet.ConfigureSpecialShot(frontlineTier > 0 ? 1 : 0, coverTier > 0 ? 55.0f : 0.0f, coverTier > 0 ? 0.35f : 0.0f);
        if (frontlineTier > 0)
        {
            player.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, -0.16f, 0.55f + frontlineTier * 0.10f);
            player.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, 0.16f, 0.55f + frontlineTier * 0.10f);
        }
        if (counterTier > 0)
        {
            bullet.ConfigureDamageMultiplier(1.0f + counterTier * 0.08f);
        }
    }

    private int getTier(StringName branchId)
    {
        return _branchTiers.TryGetValue(branchId, out int tier) ? tier : 0;
    }
}

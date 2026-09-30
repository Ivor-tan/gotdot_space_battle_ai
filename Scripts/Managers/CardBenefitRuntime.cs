using Godot;
using System.Collections.Generic;

/// <summary>
/// Shared application entry point for design cards. It intentionally centralizes
/// card selection so UI and debug tools cannot diverge in their target checks.
/// </summary>
public static class CardBenefitRuntime
{
    private static readonly Dictionary<ulong, HashSet<StringName>> _shipCards = new();
    private static readonly HashSet<StringName> _fleetCards = new();
    private static readonly Dictionary<ulong, int> _shotCounts = new();
    private static readonly Dictionary<ulong, Dictionary<ulong, int>> _consecutiveHits = new();
    private static readonly Dictionary<ulong, int> _overloadShotStacks = new();
    private static readonly Dictionary<ulong, float> _shieldPulseElapsed = new();
    private static readonly Dictionary<ulong, float> _untouchedElapsed = new();
    private static readonly Dictionary<ulong, float> _closeDefenseElapsed = new();
    private static readonly HashSet<ulong> _emergencySignalUsedShips = new();
    private static readonly Dictionary<ulong, int> _quantumHitCounts = new();
    private static bool _isApplyingShieldReaction;
    private static float _experienceAttractionMultiplier = 1.0f;

    public static float ExperienceAttractionMultiplier => _experienceAttractionMultiplier;
    public static bool TryApply(CardBenefitData card, out string reason)
    {
        return TryApply(card, null, out reason);
    }

    public static bool TryApply(CardBenefitData card, PlayerController selectedTarget, out string reason)
    {
        reason = string.Empty;
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            reason = "Card data is unavailable.";
            return false;
        }

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            reason = "PlayerManager is unavailable.";
            return false;
        }

        PlayerController target = selectedTarget != null && GodotObject.IsInstanceValid(selectedTarget)
            ? selectedTarget
            : getFirstValidPlayer(playerManager);

        if (card.CardId.ToString().StartsWith("SIG_") && tryQueueSignalRecruitment(card.CardId))
        {
            return true;
        }

        if (card.CardId == "SIG_019")
        {
            if (target == null)
            {
                reason = "No deployed ship can receive this signal.";
                return false;
            }
            foreach (ShipUpgradeNodeData node in EnhanceFunctionManager.Instance.GetShipUpgradeNodes())
            {
                if (node != null && GodotObject.IsInstanceValid(node) && node.ShipId == target.ShipData?.ShipId &&
                    !RunShipUpgradeState.HasNode(node.ShipId, node.NodeId))
                {
                    RunShipUpgradeState.ForceSelect(node);
                    target.ApplyRunUpgrade(node);
                    return true;
                }
            }

            reason = "No unselected ship upgrade is available.";
            return false;
        }

        if (card.CardId.ToString().StartsWith("SIG_"))
        {
            reason = "Signal cards require their recruitment or deployment flow.";
            return false;
        }

        if (target == null)
        {
            reason = "No deployed ship can receive this card.";
            return false;
        }

        if (!CanApplyToTarget(card, target, out reason))
        {
            return false;
        }

        if (card.CardId == "TRT_001" && !hasRole(playerManager, "artillery"))
        {
            reason = "No artillery ship is deployed.";
            return false;
        }

        if (card.CardId == "TRT_002" && !hasRole(playerManager, "guard"))
        {
            reason = "No guard ship is deployed.";
            return false;
        }

        switch (card.CategoryId.ToString())
        {
            case "wpn":
                applyWeaponBaseline(target, card.CardId);
                break;
            case "sys":
                applySystemBaseline(target, card.CardId);
                break;
            case "trt":
            case "flt":
                applyFleetBaseline(playerManager, card.CardId);
                break;
            default:
                reason = $"Unsupported card category: {card.CategoryId}.";
                return false;
        }

        if (card.CategoryId.ToString() is "wpn" or "sys")
        {
            getShipCards(target).Add(card.CardId);
        }
        else
        {
            _fleetCards.Add(card.CardId);
        }

        return true;
    }

    /// <summary>Checks whether a deployed ship can receive a single-target card without changing run state.</summary>
    public static bool CanApplyToTarget(CardBenefitData card, PlayerController target, out string reason)
    {
        reason = string.Empty;
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            reason = "Card data is unavailable.";
            return false;
        }

        if (target == null || !GodotObject.IsInstanceValid(target) || target.ShipData == null || !GodotObject.IsInstanceValid(target.ShipData))
        {
            reason = "No deployed ship is available.";
            return false;
        }

        if (card.CardId == "SYS_006" && target.ShipData.RoleId != "guard")
        {
            reason = "Sacrificial shield can only be applied to a guard ship.";
            return false;
        }

        return true;
    }

    /// <summary>Filters target-selection benefits to the ships that exist in the current fleet.</summary>
    public static bool HasCompatibleDeployedTarget(CardBenefitData card)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (card == null || !GodotObject.IsInstanceValid(card) || playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            return false;
        }

        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (CanApplyToTarget(card, player, out _))
            {
                return true;
            }
        }

        return false;
    }

    public static void OnBulletSpawned(PlayerController owner, BulletController bullet)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || bullet == null || !GodotObject.IsInstanceValid(bullet))
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(owner);
        ulong ownerId = owner.GetInstanceId();
        _shotCounts.TryGetValue(ownerId, out int shotCount);
        shotCount++;
        _shotCounts[ownerId] = shotCount;

        if (cards.Contains("WPN_004") && owner.BulletSpawn != null && GodotObject.IsInstanceValid(owner.BulletSpawn))
        {
            owner.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, -0.14f, 0.45f);
            owner.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, 0.14f, 0.45f);
        }

        if (cards.Contains("WPN_012") && shotCount % 3 == 0 && owner.BulletSpawn != null && GodotObject.IsInstanceValid(owner.BulletSpawn))
        {
            owner.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, -0.18f, 0.55f);
            owner.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, 0.18f, 0.55f);
        }

        if (owner.ShipData != null && owner.ShipData.ShipId == "SHP_PLY_CAR_001" && cards.Contains("WPN_013"))
        {
            bullet.MultiplyDamageMultiplier(1.15f);
        }

        if (owner.ShipData != null && owner.ShipData.ShipId == "SHP_PLY_CAR_001" && cards.Contains("WPN_014") && owner.BulletSpawn != null)
        {
            owner.BulletSpawn.SpawnSupplementaryBullet(bullet.targetPosition, 0.0f, 0.85f);
        }

        if (cards.Contains("WPN_001"))
        {
            bullet.ConfigureSpecialShot(1, 0.0f, 0.0f);
        }

        if (cards.Contains("WPN_008"))
        {
            bullet.MultiplyTurnSpeed(1.35f);
        }

        if (cards.Contains("SYS_020"))
        {
            _overloadShotStacks.TryGetValue(ownerId, out int overloadStacks);
            overloadStacks++;
            bullet.MultiplyDamageMultiplier(1.0f + Mathf.Min(overloadStacks, 12) * 0.01f);
            if (overloadStacks >= 12 && owner.TryConsumeShieldCharge())
            {
                overloadStacks = 0;
            }

            _overloadShotStacks[ownerId] = overloadStacks;
        }


    }

    public static void OnBulletHit(PlayerController owner, Node2D target, int damage)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || target == null || !GodotObject.IsInstanceValid(target))
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(owner);
        if (cards.Contains("WPN_006"))
        {
            damageNearestEnemy(target, Mathf.Max(1, Mathf.RoundToInt(damage * 0.30f)));
        }

        if (cards.Contains("WPN_010") && target is EnemyController enemy)
        {
            enemy.ApplyDamageOverTime(Mathf.Max(1, Mathf.RoundToInt(damage * 0.15f)), 2.0f);
        }

        if (cards.Contains("WPN_011") && owner.ShipData?.FactionId == "gravity")
        {
            damageNearestEnemy(target, Mathf.Max(1, Mathf.RoundToInt(damage * 0.15f)));
        }

        if (cards.Contains("WPN_009") && target is EnemyController shieldedEnemy)
        {
            Dictionary<ulong, int> hitCounts = getConsecutiveHits(owner);
            ulong targetId = shieldedEnemy.GetInstanceId();
            hitCounts.TryGetValue(targetId, out int hitCount);
            hitCount++;
            hitCounts[targetId] = hitCount;
            if (hitCount >= 4)
            {
                shieldedEnemy.RemoveShieldLayers(1);
                hitCounts[targetId] = 0;
            }
        }

        if (_fleetCards.Contains("FLT_010") && target is EnemyController eliteTarget && eliteTarget.IsEliteOrBoss)
        {
            foreach (PlayerController player in PlayerManager.Instance.CurrentPlayerDic.Values)
            {
                player?.GrantNextShotDamageMultiplier(1.06f);
            }
        }

        if (_fleetCards.Contains("TRT_017") && target is EnemyController affectedTarget && affectedTarget.HasActiveControlOrBurn)
        {
            affectedTarget.OnHit(Mathf.Max(1, Mathf.RoundToInt(damage * 0.12f)));
        }

        if (_fleetCards.Contains("TRT_004") && owner.ShipData?.RoleId == "control" && target is EnemyController controlledTarget)
        {
            controlledTarget.ApplyMovementSlow(0.65f, 1.0f);
        }

        if (_fleetCards.Contains("TRT_006") && owner.ShipData?.RoleId == "carrier" && target is EnemyController carrierTarget && carrierTarget.HasActiveControlOrBurn)
        {
            carrierTarget.OnHit(Mathf.Max(1, Mathf.RoundToInt(damage * 0.25f)));
        }

        if (_fleetCards.Contains("TRT_007") && owner.ShipData?.FactionId == "thermal" && countFactionShips("thermal") >= 2 &&
            target is EnemyController thermalTarget)
        {
            thermalTarget.ApplyDamageOverTime(Mathf.Max(1, Mathf.RoundToInt(damage * 0.20f)), 2.0f);
        }

        if (_fleetCards.Contains("TRT_010") && owner.ShipData?.FactionId == "gravity" && target is EnemyController gravityTarget)
        {
            gravityTarget.ApplyDamageOverTime(Mathf.Max(1, Mathf.RoundToInt(damage * 0.12f)), 1.0f);
        }

        if (_fleetCards.Contains("TRT_011") && owner.ShipData?.FactionId == "quantum")
        {
            ulong ownerId = owner.GetInstanceId();
            _quantumHitCounts.TryGetValue(ownerId, out int hitCount);
            hitCount++;
            _quantumHitCounts[ownerId] = hitCount;
            if (hitCount % 3 == 0 && PlayerManager.Instance != null)
            {
                Godot.Collections.Array<PlayerController> adjacent = new();
                PlayerManager.Instance.GetAdjacentPlayers(owner, adjacent);
                foreach (PlayerController player in adjacent)
                {
                    if (player.ShipData?.FactionId == "quantum")
                    {
                        player.GrantNextShotDamageMultiplier(1.50f);
                    }
                }
            }
        }

        if (_fleetCards.Contains("TRT_014") && target is EnemyController vanguardEliteTarget && vanguardEliteTarget.IsEliteOrBoss &&
            PlayerManager.Instance != null && PlayerManager.Instance.GetRingIndex(owner) == 1)
        {
            vanguardEliteTarget.OnHit(Mathf.Max(1, Mathf.RoundToInt(damage * 0.10f)));
        }
    }

    public static void OnBulletPreHit(PlayerController owner, Node2D target, ref int damage)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner) || target is not EnemyController enemy)
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(owner);
        if (cards.Contains("WPN_007") && enemy.IsAtFullHealth)
        {
            damage = Mathf.RoundToInt(damage * 1.25f);
        }

        if (cards.Contains("WPN_017") && enemy.IsEliteOrBoss)
        {
            damage = Mathf.RoundToInt(damage * 1.25f);
        }

        if (cards.Contains("WPN_005"))
        {
            enemy.ApplyDamageOverTime(Mathf.Max(1, Mathf.RoundToInt(damage * 0.20f)), 3.0f);
        }
    }

    public static void OnPlayerLost(PlayerManager playerManager, PlayerController lostPlayer)
    {
        if (playerManager == null || lostPlayer == null || !GodotObject.IsInstanceValid(lostPlayer))
        {
            return;
        }

        if (_fleetCards.Contains("FLT_013"))
        {
            Godot.Collections.Array<PlayerController> adjacent = new();
            playerManager.GetAdjacentPlayers(lostPlayer, adjacent);
            foreach (PlayerController player in adjacent)
            {
                player.addShieldCharges(1);
            }
        }

        if (_fleetCards.Contains("TRT_018"))
        {
            Godot.Collections.Array<PlayerController> adjacent = new();
            playerManager.GetAdjacentPlayers(lostPlayer, adjacent);
            foreach (PlayerController player in adjacent)
            {
                player.addShieldCharges(1);
            }
        }
    }

    public static void OnWaveCompleted(int wave)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            return;
        }

        if (_fleetCards.Contains("FLT_018"))
        {
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player?.ShipData?.ShipId == "SHP_PLY_CAR_001")
                {
                    player.GrantNextShotDamageMultiplier(1.35f);
                }
            }
        }

        if (_fleetCards.Contains("FLT_015") && hasActiveBond(playerManager))
        {
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                player?.GrantNextShotDamageMultiplier(1.15f);
            }
        }
    }

    public static void OnEnemyDefeated(PlayerController owner)
    {
        if (owner == null || !GodotObject.IsInstanceValid(owner))
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(owner);
        if (cards.Contains("WPN_018"))
        {
            owner.GrantNextShotDamageMultiplier(1.45f);
        }
        if (cards.Contains("SYS_016"))
        {
            owner.addShieldCharges(1);
        }
    }

    public static void OnShieldLost(PlayerController player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(player);
        if (cards.Contains("SYS_015"))
        {
            player.addShieldCharges(1);
        }
        if (cards.Contains("SYS_008"))
        {
            player.GrantNextShotDamageMultiplier(1.35f);
        }
    }

    public static void OnShieldGranted(PlayerController player, int charges)
    {
        if (_isApplyingShieldReaction || player == null || !GodotObject.IsInstanceValid(player) || charges <= 0)
        {
            return;
        }

        HashSet<StringName> cards = getShipCards(player);
        if (cards.Contains("SYS_010") && GD.Randf() <= 0.25f)
        {
            _isApplyingShieldReaction = true;
            player.addShieldCharges(1);
            _isApplyingShieldReaction = false;
        }

        if (cards.Contains("WPN_019"))
        {
            int baseDamage = player.BulletSpawn != null && GodotObject.IsInstanceValid(player.BulletSpawn)
                ? player.BulletSpawn.Damage
                : 1;
            damageNearestEnemy(player, Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.30f)));
        }

        if (_fleetCards.Contains("TRT_012") && (player.ShipData?.FactionId == "bio") &&
            (player.ShipData.RoleId == "repair" || player.ShipData.RoleId == "shield"))
        {
            _isApplyingShieldReaction = true;
            player.addShieldCharges(1);
            _isApplyingShieldReaction = false;
        }

        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager != null && GodotObject.IsInstanceValid(playerManager))
        {
            foreach (PlayerController candidate in playerManager.CurrentPlayerDic.Values)
            {
                if (candidate == null || !GodotObject.IsInstanceValid(candidate) || candidate == player ||
                    !getShipCards(candidate).Contains("SYS_012"))
                {
                    continue;
                }

                float adjacentDistance = playerManager.Spacing * 1.1f;
                if (candidate.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= adjacentDistance * adjacentDistance)
                {
                    candidate.GrantNextShotDamageMultiplier(1.25f);
                }
            }
        }

        if (_isApplyingShieldReaction || playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            return;
        }

        _isApplyingShieldReaction = true;
        foreach (PlayerController candidate in playerManager.CurrentPlayerDic.Values)
        {
            if (candidate == null || !GodotObject.IsInstanceValid(candidate) || candidate == player ||
                !getShipCards(candidate).Contains("SYS_017"))
            {
                continue;
            }

            float adjacentDistance = playerManager.Spacing * 1.1f;
            if (candidate.GlobalPosition.DistanceSquaredTo(player.GlobalPosition) <= adjacentDistance * adjacentDistance && GD.Randf() <= 0.20f)
            {
                candidate.addShieldCharges(1);
            }
        }
        _isApplyingShieldReaction = false;
    }

    public static void Process(double delta)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            return;
        }

        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player == null || !GodotObject.IsInstanceValid(player))
            {
                continue;
            }

            ulong playerId = player.GetInstanceId();
            HashSet<StringName> cards = getShipCards(player);
            if (cards.Contains("SYS_003"))
            {
                _shieldPulseElapsed.TryGetValue(playerId, out float pulseElapsed);
                pulseElapsed += (float)delta;
                if (pulseElapsed >= 8.0f)
                {
                    player.addShieldCharges(1);
                    pulseElapsed = 0.0f;
                }
                _shieldPulseElapsed[playerId] = pulseElapsed;
            }

            if (cards.Contains("SYS_007"))
            {
                _untouchedElapsed.TryGetValue(playerId, out float untouchedElapsed);
                untouchedElapsed += (float)delta;
                if (untouchedElapsed >= 4.0f)
                {
                    player.addShieldCharges(1);
                    untouchedElapsed = 0.0f;
                }
                _untouchedElapsed[playerId] = untouchedElapsed;
            }

            if (cards.Contains("WPN_015") && player.ShipData?.RoleId == "guard")
            {
                _closeDefenseElapsed.TryGetValue(playerId, out float closeDefenseElapsed);
                closeDefenseElapsed += (float)delta;
                if (closeDefenseElapsed >= 3.0f)
                {
                    int baseDamage = player.BulletSpawn != null && GodotObject.IsInstanceValid(player.BulletSpawn)
                        ? player.BulletSpawn.Damage
                        : 1;
                    damageNearestEnemy(player, Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.40f)), 160.0f);
                    closeDefenseElapsed = 0.0f;
                }
                _closeDefenseElapsed[playerId] = closeDefenseElapsed;
            }
        }
    }

    public static void OnPlayerDamaged(PlayerController player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
        {
            return;
        }

        ulong playerId = player.GetInstanceId();
        _untouchedElapsed[playerId] = 0.0f;
    }

    public static bool TryPreventPlayerLoss(PlayerController player)
    {
        if (player == null || !GodotObject.IsInstanceValid(player) || !getShipCards(player).Contains("SYS_018"))
        {
            return false;
        }

        ulong playerId = player.GetInstanceId();
        if (!_emergencySignalUsedShips.Add(playerId))
        {
            return false;
        }

        player.addShieldCharges(1);
        return true;
    }

    public static bool TryCreateRecruitment(CardBenefitData card, out AddPlayer recruitment)
    {
        recruitment = null;
        if (card == null || !GodotObject.IsInstanceValid(card))
        {
            return false;
        }

        StringName shipId = card.CardId.ToString() switch
        {
            "SIG_001" => "SHP_PLY_ART_001",
            "SIG_002" => "SHP_PLY_CTL_001",
            "SIG_003" => "SHP_PLY_GRD_001",
            "SIG_004" => "SHP_PLY_SUP_001",
            "SIG_005" => "SHP_PLY_CAR_001",
            "SIG_006" => "SHP_PLY_CTL_002",
            "SIG_007" => "SHP_PLY_ASS_001",
            "SIG_008" => "SHP_PLY_GRD_002",
            "SIG_009" => "SHP_PLY_SUP_001",
            _ => default
        };
        if (shipId == default)
        {
            return false;
        }

        recruitment = new AddPlayer
        {
            Name = card.DisplayName,
            Description = card.EffectDescription,
            FunctionType = EnhanceFunctionType.AddCraft,
            AppliesToAllShips = false,
            TargetShipId = shipId
        };
        return true;
    }

    private static bool tryQueueSignalRecruitment(StringName cardId)
    {
        if (EnhanceFunctionManager.Instance == null || !GodotObject.IsInstanceValid(EnhanceFunctionManager.Instance))
        {
            return false;
        }

        StringName factionId = default;
        StringName roleId = default;
        switch (cardId.ToString())
        {
            case "SIG_010": factionId = "thermal"; break;
            case "SIG_011": factionId = "electromagnetic"; break;
            case "SIG_012": factionId = "kinetic"; break;
            case "SIG_013": factionId = "gravity"; break;
            case "SIG_014": factionId = "bio"; break;
            case "SIG_015": roleId = "carrier"; break;
        }
        if (factionId == default && roleId == default)
        {
            return false;
        }

        EnhanceFunctionManager.Instance.QueueRecruitmentOffer(factionId, roleId);
        LogUtil.Info($"Queued recruitment signal {cardId}: faction={factionId}, role={roleId}.");
        return true;
    }

    private static PlayerController getFirstValidPlayer(PlayerManager playerManager)
    {
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player != null && GodotObject.IsInstanceValid(player))
            {
                return player;
            }
        }

        return null;
    }

    private static HashSet<StringName> getShipCards(PlayerController player)
    {
        ulong playerId = player.GetInstanceId();
        if (!_shipCards.TryGetValue(playerId, out HashSet<StringName> cards))
        {
            cards = new HashSet<StringName>();
            _shipCards[playerId] = cards;
        }

        return cards;
    }

    private static Dictionary<ulong, int> getConsecutiveHits(PlayerController player)
    {
        ulong playerId = player.GetInstanceId();
        if (!_consecutiveHits.TryGetValue(playerId, out Dictionary<ulong, int> hits))
        {
            hits = new Dictionary<ulong, int>();
            _consecutiveHits[playerId] = hits;
        }

        return hits;
    }

    private static void damageNearestEnemy(Node2D source, int damage, float radius = 180.0f)
    {
        if (EnemySpawnerManager.Instance == null)
        {
            return;
        }

        Node2D closest = null;
        float closestDistanceSquared = radius * radius;
        foreach (Node2D enemy in EnemySpawnerManager.Instance.ActiveEnemies)
        {
            if (enemy == null || !GodotObject.IsInstanceValid(enemy) || enemy == source || enemy is not IDamageable)
            {
                continue;
            }

            float distanceSquared = source.GlobalPosition.DistanceSquaredTo(enemy.GlobalPosition);
            if (distanceSquared < closestDistanceSquared)
            {
                closest = enemy;
                closestDistanceSquared = distanceSquared;
            }
        }

        if (closest is IDamageable damageable)
        {
            damageable.OnHit(damage);
        }
    }

    private static bool hasActiveBond(PlayerManager playerManager)
    {
        Dictionary<StringName, int> memberCounts = new();
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player?.ShipData == null || !GodotObject.IsInstanceValid(player.ShipData))
            {
                continue;
            }

            foreach (StringName bondId in player.ShipData.BondIds)
            {
                memberCounts.TryGetValue(bondId, out int count);
                memberCounts[bondId] = count + 1;
                if (memberCounts[bondId] >= 2)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void applyWeaponBaseline(PlayerController player, StringName cardId)
    {
        if (player.BulletSpawn == null || !GodotObject.IsInstanceValid(player.BulletSpawn))
        {
            return;
        }

        string id = cardId.ToString();
        if (id is "WPN_002" or "WPN_017")
        {
            player.AttackTimer.WaitTime = Mathf.Max(0.2f, player.AttackTimer.WaitTime * 0.9f);
            return;
        }

        if (id == "WPN_003")
        {
            player.BulletSpawn.Damage = Mathf.RoundToInt(player.BulletSpawn.Damage * 1.18f);
            player.CurrentPlayerStates.AttackRange *= 0.92f;
            return;
        }

        if (id == "WPN_011" && player.ShipData?.FactionId == "gravity")
        {
            player.CurrentPlayerStates.AttackRange *= 1.25f;
            return;
        }

        if (id is "WPN_001" or "WPN_003" or "WPN_011")
        {
            player.CurrentPlayerStates.AttackRange *= 1.12f;
            return;
        }

        player.BulletSpawn.Damage = Mathf.RoundToInt(player.BulletSpawn.Damage * 1.18f);
    }

    private static void applySystemBaseline(PlayerController player, StringName cardId)
    {
        string id = cardId.ToString();
        if (id is "SYS_003" or "SYS_007" or "SYS_018")
        {
            return;
        }

        if (id == "SYS_005")
        {
            PlayerManager playerManager = PlayerManager.Instance;
            if (playerManager?.Player?.CurrentPlayerStates != null)
            {
                playerManager.Player.CurrentPlayerStates.MaxSpeed *= 1.15f;
            }
            return;
        }

        if (id == "SYS_006")
        {
            if (player.ShipData?.RoleId != "guard")
            {
                return;
            }

            player.addShieldCharges(2);
            if (player.BulletSpawn != null && GodotObject.IsInstanceValid(player.BulletSpawn))
            {
                player.BulletSpawn.Damage = Mathf.Max(1, Mathf.RoundToInt(player.BulletSpawn.Damage * 0.88f));
            }
            return;
        }

        player.addShieldCharges(id == "SYS_018" ? 2 : 1);
    }

    private static void applyFleetBaseline(PlayerManager playerManager, StringName cardId)
    {
        string id = cardId.ToString();
        if (id == "TRT_001")
        {
            applyToRole(playerManager, "artillery", player => player.CurrentPlayerStates.AttackRange *= 1.12f);
            return;
        }

        if (id == "TRT_002")
        {
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player?.ShipData?.RoleId != "guard")
                {
                    continue;
                }

                player.addShieldCharges(playerManager.GetRingIndex(player) == 1 ? 2 : 1);
            }
            return;
        }

        if (id == "TRT_020")
        {
            HashSet<StringName> roles = new();
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player?.ShipData != null)
                {
                    roles.Add(player.ShipData.RoleId);
                }
            }

            if (roles.Count == playerManager.CurrentPlayerCount && roles.Count >= 3)
            {
                foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
                {
                    if (player?.BulletSpawn != null && GodotObject.IsInstanceValid(player.BulletSpawn))
                    {
                        player.BulletSpawn.Damage = Mathf.RoundToInt(player.BulletSpawn.Damage * 1.18f);
                    }
                }
            }
            return;
        }

        if (id == "TRT_015")
        {
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player == null || !GodotObject.IsInstanceValid(player) || playerManager.GetRingIndex(player) != 1 ||
                    (player.ShipData?.RoleId != "repair" && player.ShipData?.RoleId != "shield" && player.ShipData?.RoleId != "control"))
                {
                    continue;
                }

                Godot.Collections.Array<PlayerController> adjacent = new();
                playerManager.GetAdjacentPlayers(player, adjacent);
                foreach (PlayerController other in adjacent)
                {
                    if (other.ShipData?.RoleId == "repair" || other.ShipData?.RoleId == "shield" || other.ShipData?.RoleId == "control")
                    {
                        player.AttackTimer.WaitTime = Mathf.Max(0.2f, player.AttackTimer.WaitTime * 0.92f);
                        other.AttackTimer.WaitTime = Mathf.Max(0.2f, other.AttackTimer.WaitTime * 0.92f);
                    }
                }
            }
            return;
        }

        if (id == "TRT_005")
        {
            return;
        }

        if (id == "FLT_005")
        {
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player == null || !GodotObject.IsInstanceValid(player) || playerManager.GetRingIndex(player) != 2)
                {
                    continue;
                }

                player.CurrentPlayerStates.AttackRange *= 1.15f;
            }
            return;
        }

        if (id == "FLT_004")
        {
            int firstRingCount = 0;
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player != null && GodotObject.IsInstanceValid(player) && playerManager.GetRingIndex(player) == 1)
                {
                    firstRingCount++;
                }
            }

            if (firstRingCount >= 6)
            {
                playerManager.addFleetShieldCharges(1);
            }
            return;
        }

        if (id == "FLT_002")
        {
            if (playerManager.Player == null || !GodotObject.IsInstanceValid(playerManager.Player))
            {
                return;
            }

            Vector2 forward = Vector2.FromAngle(playerManager.Player.Rotation);
            foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
            {
                if (player == null || !GodotObject.IsInstanceValid(player) || player.BulletSpawn == null ||
                    !GodotObject.IsInstanceValid(player.BulletSpawn))
                {
                    continue;
                }

                Vector2 localOffset = player.GlobalPosition - playerManager.Player.GlobalPosition;
                if (localOffset.LengthSquared() > 1.0f && localOffset.Normalized().Dot(forward) < -0.25f)
                {
                    player.BulletSpawn.Damage = Mathf.RoundToInt(player.BulletSpawn.Damage * 1.12f);
                }
            }
            return;
        }

        if (id == "FLT_011")
        {
            _experienceAttractionMultiplier = Mathf.Max(_experienceAttractionMultiplier, 1.25f);
            return;
        }

        bool rangeEffect = cardId.ToString() is "TRT_001" or "TRT_005" or "FLT_005" or "FLT_011";
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player == null || !GodotObject.IsInstanceValid(player))
            {
                continue;
            }

            if (rangeEffect)
            {
                player.CurrentPlayerStates.AttackRange *= 1.12f;
            }
            else
            {
                player.BulletSpawn.Damage = Mathf.RoundToInt(player.BulletSpawn.Damage * 1.12f);
            }
        }
    }

    private static void applyToRole(PlayerManager playerManager, StringName roleId, System.Action<PlayerController> apply)
    {
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player?.ShipData?.RoleId == roleId)
            {
                apply(player);
            }
        }
    }

    private static bool hasRole(PlayerManager playerManager, StringName roleId)
    {
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player?.ShipData?.RoleId == roleId)
            {
                return true;
            }
        }

        return false;
    }

    private static int countFactionShips(StringName factionId)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !GodotObject.IsInstanceValid(playerManager))
        {
            return 0;
        }

        int count = 0;
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player?.ShipData?.FactionId == factionId)
            {
                count++;
            }
        }

        return count;
    }
}

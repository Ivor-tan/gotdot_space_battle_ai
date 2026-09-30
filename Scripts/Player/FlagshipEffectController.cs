using Godot;

/// <summary>
/// Runtime extension point for SHP_PLY_FLG_001. Its PRC, FRM and EXP routes
/// attach here without coupling their state to the shared ship controller.
/// </summary>
public partial class FlagshipEffectController : ShipEffectController
{
    private int _prcTier;
    private int _formationTier;
    private int _expeditionTier;
    private int _shotsSincePrism;
    private int _shotsSinceFormationPulse;
    private int _hitsSinceCommandPulse;
    private readonly Godot.Collections.Array<PlayerController> _adjacentPlayers = new();

    public override void OnShipDataApplied(PlayerShipData shipData)
    {
        if (shipData == null || !IsInstanceValid(shipData) || shipData.ShipId != "SHP_PLY_FLG_001")
        {
            return;
        }

        _shipId = shipData.ShipId;

        if (!TryGetPlayer(out PlayerController player) || player.BulletSpawn == null || !IsInstanceValid(player.BulletSpawn))
        {
            LogUtil.Warning("Flagship effect controller is waiting for BulletSpawn initialization.");
            return;
        }

        player.BulletSpawn.BulletSpawned -= onBulletSpawned;
        player.BulletSpawn.BulletSpawned += onBulletSpawned;
        LogUtil.Info("Flagship effect controller initialized.");
    }

    public override void ResetEffects()
    {
        _prcTier = 0;
        _formationTier = 0;
        _expeditionTier = 0;
        _shotsSinceFormationPulse = 0;
        _shotsSincePrism = 0;
        _hitsSinceCommandPulse = 0;
    }

    public override void ApplyRunUpgrade(ShipUpgradeNodeData node)
    {
        if (node == null || !IsInstanceValid(node))
        {
            return;
        }

        string nodeId = node.NodeId.ToString();
        if (nodeId.StartsWith("ULK_UPG_FLG_PRC_"))
        {
            _prcTier = Mathf.Max(_prcTier, node.Tier);
        }
        else if (nodeId.StartsWith("ULK_UPG_FLG_FRM_"))
        {
            _formationTier = Mathf.Max(_formationTier, node.Tier);
        }
        else if (nodeId.StartsWith("ULK_UPG_FLG_EXP_"))
        {
            _expeditionTier = Mathf.Max(_expeditionTier, node.Tier);
        }
    }

    private void onBulletSpawned(BulletController bullet)
    {
        if (bullet == null || !IsInstanceValid(bullet))
        {
            return;
        }

        _shotsSinceFormationPulse++;
        if (_formationTier > 0 && TryGetPlayer(out PlayerController player) && _shotsSinceFormationPulse >= 8)
        {
            _shotsSinceFormationPulse = 0;
            player.addShieldCharges(1);
        }

        if (_expeditionTier > 0)
        {
            bullet.ConfigureDamageMultiplier(1.0f + _expeditionTier * 0.04f);
        }

        bullet.HitConfirmed += onFlagshipHit;

        if (_prcTier <= 0)
        {
            return;
        }

        _shotsSincePrism++;
        if (_shotsSincePrism < 3)
        {
            return;
        }

        _shotsSincePrism = 0;
        int pierceCount = 1;
        float splashRadius = _prcTier >= 2 ? 55.0f : 0.0f;
        float splashMultiplier = _prcTier >= 2 ? 0.35f : 0.0f;
        bullet.ConfigureSpecialShot(pierceCount, splashRadius, splashMultiplier);

        if (_prcTier >= 3)
        {
            bullet.ConfigureDamageMultiplier(1.30f);
        }
    }

    private void onFlagshipHit(BulletController bullet, Node2D target, int damage)
    {
        if (bullet != null && IsInstanceValid(bullet))
        {
            getAttackRangeStyle(out float radius, out Color color, out float segmentCount, out float swirlStrength);
            if (bullet.HasSplashDamage)
            {
                showAttackRangeEffect(bullet.GlobalPosition, Mathf.Max(radius, bullet.SplashRadius), color, segmentCount, swirlStrength);
            }
            else
            {
                showSingleImpactEffect(bullet.GlobalPosition, color);
            }
        }

        if (!TryGetPlayer(out PlayerController player) || PlayerManager.Instance == null ||
            PlayerManager.Instance.GetRingIndex(player) != 0)
        {
            return;
        }

        _hitsSinceCommandPulse++;
        if (_hitsSinceCommandPulse < 3)
        {
            return;
        }

        _hitsSinceCommandPulse = 0;
        PlayerManager.Instance.GetAdjacentPlayers(player, _adjacentPlayers);
        foreach (PlayerController adjacentPlayer in _adjacentPlayers)
        {
            adjacentPlayer.GrantNextShotDamageMultiplier(1.12f);
        }
    }
}

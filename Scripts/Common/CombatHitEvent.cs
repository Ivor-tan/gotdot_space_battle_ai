using Godot;

/// <summary>
/// Immutable payload passed from projectiles to ship effect controllers.
/// Secondary effects must retain this source identity for hit de-duplication.
/// </summary>
public readonly struct CombatHitEvent
{
    public readonly StringName SourceShipId;
    public readonly ulong SourceInstanceId;
    public readonly ulong ProjectileId;
    public readonly Node2D Target;
    public readonly Vector2 Position;
    public readonly int Damage;

    public CombatHitEvent(StringName sourceShipId, ulong sourceInstanceId, ulong projectileId, Node2D target, Vector2 position, int damage)
    {
        SourceShipId = sourceShipId;
        SourceInstanceId = sourceInstanceId;
        ProjectileId = projectileId;
        Target = target;
        Position = position;
        Damage = damage;
    }
}

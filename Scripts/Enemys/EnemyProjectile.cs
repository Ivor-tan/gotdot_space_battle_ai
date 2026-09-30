using Godot;

public partial class EnemyProjectile : Node2D
{
    [Export] public float Speed = 100.0f;
    [Export] public float LifetimeSeconds = 5.0f;

    private PlayerController _target;
    private Vector2 _flightDirection = Vector2.Right;
    private float _remaining;
    private float _launchRemaining;

    public void Configure(PlayerController target, float speed, float lifetimeSeconds, float launchDelaySeconds = 0.28f)
    {
        _target = target;
        PlayerManager playerManager = PlayerManager.Instance;
        float fleetMoveSpeed = playerManager?.Player?.CurrentPlayerStates?.MaxSpeed ?? playerManager?.FleetMoveSpeed ?? 100.0f;
        Speed = Mathf.Min(Mathf.Max(0.0f, speed), Mathf.Max(0.0f, fleetMoveSpeed));
        LifetimeSeconds = Mathf.Max(0.0f, lifetimeSeconds);
        _remaining = LifetimeSeconds;
        _launchRemaining = Mathf.Max(0.0f, launchDelaySeconds);

        if (target != null && IsInstanceValid(target))
        {
            Vector2 launchVector = target.GlobalPosition - GlobalPosition;
            if (launchVector.LengthSquared() > Mathf.Epsilon)
            {
                _flightDirection = launchVector.Normalized();
                Rotation = _flightDirection.Angle();
            }
        }
    }

    public override void _Ready()
    {
        _remaining = LifetimeSeconds;
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _remaining -= (float)delta;
        if (_remaining <= 0.0f)
        {
            QueueFree();
            return;
        }

        if (_launchRemaining > 0.0f)
        {
            _launchRemaining = Mathf.Max(0.0f, _launchRemaining - (float)delta);
            QueueRedraw();
            return;
        }

        GlobalPosition += _flightDirection * Speed * (float)delta;
        if (_target != null && IsInstanceValid(_target) &&
            GlobalPosition.DistanceSquaredTo(_target.GlobalPosition) <= 16.0f * 16.0f)
        {
            _target.ReceiveEnemyHit();
            QueueFree();
        }
    }

    public override void _Draw()
    {
        Color projectileColor = new(1.0f, 0.38f, 0.18f);
        if (_launchRemaining > 0.0f)
        {
            float charge = 1.0f - _launchRemaining / 0.28f;
            float radius = Mathf.Lerp(5.0f, 11.0f, charge);
            DrawCircle(Vector2.Zero, radius, new Color(projectileColor.R, projectileColor.G, projectileColor.B, 0.30f));
            DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 20, projectileColor.Lightened(0.25f), 2.0f);
            return;
        }

        DrawCircle(Vector2.Zero, 5.0f, projectileColor);
        DrawCircle(Vector2.Zero, 9.0f, new Color(projectileColor.R, projectileColor.G, projectileColor.B, 0.18f));
    }
}

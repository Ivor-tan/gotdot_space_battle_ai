using Godot;

/// <summary>Reused single-target hit marker; it stays compact so it is not mistaken for an area attack.</summary>
public partial class ShipSingleImpactEffect : Node2D
{
    private const float DurationSeconds = 0.16f;

    private float _remainingDuration;
    private Color _color = Colors.White;

    public override void _Ready()
    {
        TopLevel = true;
    }

    public override void _Process(double delta)
    {
        if (_remainingDuration <= 0.0f)
        {
            return;
        }

        _remainingDuration = Mathf.Max(0.0f, _remainingDuration - (float)delta);
        QueueRedraw();
    }

    public void Play(Vector2 worldPosition, Color color)
    {
        GlobalPosition = worldPosition;
        _color = color;
        _remainingDuration = DurationSeconds;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_remainingDuration <= 0.0f)
        {
            return;
        }

        float progress = 1.0f - _remainingDuration / DurationSeconds;
        float radius = Mathf.Lerp(4.0f, 14.0f, progress);
        Color fadedColor = new(_color.R, _color.G, _color.B, _color.A * (1.0f - progress));
        DrawCircle(Vector2.Zero, radius * 0.42f, new Color(fadedColor.R, fadedColor.G, fadedColor.B, fadedColor.A * 0.32f));
        DrawArc(Vector2.Zero, radius, 0.0f, Mathf.Tau, 12, fadedColor, 2.0f);
        DrawLine(new Vector2(-radius, 0.0f), new Vector2(radius, 0.0f), fadedColor, 1.5f);
        DrawLine(new Vector2(0.0f, -radius), new Vector2(0.0f, radius), fadedColor, 1.5f);
    }
}

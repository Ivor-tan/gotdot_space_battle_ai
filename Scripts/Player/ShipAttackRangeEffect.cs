using Godot;

/// <summary>
/// Reuses one shader-driven world-space pulse per ship. The pulse is reset on
/// each shot, so range feedback never allocates materials or scene nodes in combat.
/// </summary>
public partial class ShipAttackRangeEffect : Node2D
{
    private const string ShaderPath = "res://Material/Shader/ship_attack_range.gdshader";

    private ColorRect _surface;
    private ShaderMaterial _material;
    private float _remainingDuration;
    private float _duration;

    public override void _Ready()
    {
        TopLevel = true;
        createSurface();
    }

    public override void _Process(double delta)
    {
        if (_remainingDuration <= 0.0f)
        {
            return;
        }

        _remainingDuration = Mathf.Max(0.0f, _remainingDuration - (float)delta);
        if (_material != null && GodotObject.IsInstanceValid(_material))
        {
            _material.SetShaderParameter("pulse_progress", 1.0f - _remainingDuration / _duration);
        }

        if (_remainingDuration <= 0.0f && _surface != null && GodotObject.IsInstanceValid(_surface))
        {
            _surface.Hide();
        }
    }

    public void Play(Vector2 worldPosition, float radius, Color color, float segmentCount, float swirlStrength, float duration)
    {
        if (!ensureSurface())
        {
            return;
        }

        float clampedRadius = Mathf.Max(8.0f, radius);
        _duration = Mathf.Max(0.08f, duration);
        _remainingDuration = _duration;
        GlobalPosition = worldPosition;
        _surface.Position = Vector2.One * -clampedRadius;
        _surface.Size = Vector2.One * clampedRadius * 2.0f;
        _material.SetShaderParameter("effect_color", color);
        _material.SetShaderParameter("segment_count", Mathf.Clamp(segmentCount, 1.0f, 16.0f));
        _material.SetShaderParameter("swirl_strength", Mathf.Clamp(swirlStrength, 0.0f, 1.0f));
        _material.SetShaderParameter("pulse_progress", 0.0f);
        _surface.Show();
    }

    private bool ensureSurface()
    {
        if (_surface != null && GodotObject.IsInstanceValid(_surface) && _material != null && GodotObject.IsInstanceValid(_material))
        {
            return true;
        }

        createSurface();
        return _surface != null && GodotObject.IsInstanceValid(_surface) && _material != null && GodotObject.IsInstanceValid(_material);
    }

    private void createSurface()
    {
        if (_surface != null && GodotObject.IsInstanceValid(_surface))
        {
            return;
        }

        Shader shader = GD.Load<Shader>(ShaderPath);
        if (shader == null || !GodotObject.IsInstanceValid(shader))
        {
            LogUtil.Error("Ship attack range shader could not be loaded.");
            return;
        }

        _material = new ShaderMaterial
        {
            Shader = shader
        };
        _surface = new ColorRect
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = _material,
            ZIndex = -1,
            Visible = false
        };
        AddChild(_surface);
    }
}

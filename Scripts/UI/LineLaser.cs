using Godot;
using System;

public partial class LineLaser : Line2D
{
	[Export] public AnimatedSprite2D Explode;
	[Export] public float LaserLength = 1000.0f;
	[Export] public float ProjectileSpeed = 10.0f;
	[Export] public float BeamBodyWidth = 0.3f;
	[Export] public float RandomJitter = 200.0f; // 允许的波动范围
	[Export] public Color currentColor = Color.Color8(255, 255, 255); // 允许的波动范围

	private float _currentRandomLength;
	private float _timer = 0.0f;
	[Export] public float FireRate = 2.0f;
	private float _timeActive = 0.0f;
	private bool _isFiring = false;
	private ShaderMaterial _mat;

	public override void _Ready()
	{
		_currentRandomLength = LaserLength;

		ClearPoints();
		AddPoint(Vector2.Zero);
		// FromAngle 计算方向，这里假设激光沿自己 GlobalRotation 方向发射
		AddPoint(-new Vector2(_currentRandomLength, 0));
		TextureMode = LineTextureMode.Stretch; // 必须设为 Stretch

		// 关键：复制一份材质副本，从此互不干扰
		if (Material != null)
		{
			Material = Material.Duplicate() as ShaderMaterial;
			_mat = Material as ShaderMaterial;
		}
		_mat.SetShaderParameter("head_pos", 0.0f);
		_mat.SetShaderParameter("tail_pos", 0.0f);
		_mat.SetShaderParameter("beam_color", currentColor);
		Explode.AnimationFinished += () =>
			{
				Explode.Hide();
			};

		// 初始状态确保它是隐藏的
		Explode.Hide();
	}

	public void FireLaser()
	{
		_currentRandomLength = LaserLength + (float)GD.RandRange(-RandomJitter, RandomJitter);
		SetPointPosition(1, -new Vector2(_currentRandomLength, 0));
		_timeActive = 0.0f;
		_isFiring = true;
	}

	public override void _Process(double delta)
	{

		_timer += (float)delta;

		if (_timer >= FireRate)
		{
			FireLaser();
			_timer = 0.0f; // 重置计数器
		}

		if (!_isFiring) return;

		_timeActive += (float)delta * ProjectileSpeed;

		float head = Mathf.Clamp(_timeActive, 0.0f, 1.0f);

		float tail = Mathf.Clamp(_timeActive - BeamBodyWidth, 0.0f, 1.0f);

		_mat.SetShaderParameter("head_pos", head);
		_mat.SetShaderParameter("tail_pos", tail);

		if (tail >= 1.0f)
		{
			_isFiring = false;
			_mat.SetShaderParameter("head_pos", 0.0f);
			_mat.SetShaderParameter("tail_pos", 0.0f);

			Explode.GlobalPosition = ToGlobal(GetPointPosition(1));
			Explode.Show();
			Explode.Frame = 0; // 确保从第一帧开始
			Explode.Play();
		}
	}

}

using Godot;
using System;

public partial class ExpBallController : AnimatedSprite2D
{

	[Export] public Area2D DetectionArea;
	private CollisionShape2D _detectionShape;
	private ExpBallPointsData expBallPointsData;
	private float MoveSpeed = 300.0f;
	// 每秒减少的透明度（例如 2.0f 表示 0.5 秒消失，1.0f 表示 1 秒消失）
	private float FadeSpeed = 2.0f;

	private Vector2 TargetPosition;
	private bool IsActive = false;
	public override void _Ready()
	{
		DetectionArea.AreaEntered += AreaEntered;
		_detectionShape = DetectionArea.GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
	}
	public override void _PhysicsProcess(double delta)
	{
		if (!IsActive) return;

		float fDelta = (float)delta;

		GlobalPosition = GlobalPosition.MoveToward(TargetPosition, MoveSpeed * fDelta);

		float currentAlpha = Modulate.A;
		currentAlpha -= FadeSpeed * fDelta;

		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, Mathf.Clamp(currentAlpha, 0.0f, 1.0f));

		if (Modulate.A <= 0.0f)
		{
			ObjectPoolManager.Instance.Release(PoolType.ExpBall, this);
			GlobalMessengerManager.Instance.SendGetExpPoints(expBallPointsData.ExpPoints);
		}
	}

	private void AreaEntered(Node2D body)
	{
		// LogUtil.Info($"检测到玩家进入: {body.Name}");
		if (body is PlayerController player)
		{
			// CallDeferred(MethodName.Activate);
			Activate(player.GlobalPosition);
		}
	}
	private void Activate(Vector2 target)
	{
		TargetPosition = target;
		IsActive = true;
	}
	public void Init(int index)
	{
		expBallPointsData = MapResources.Instance.GetExpBall(index);
		SpriteFrames = expBallPointsData.AnimationRes;
		Play();
		Modulate = new Color(Modulate.R, Modulate.G, Modulate.B, 1.0f);
		if (_detectionShape != null && IsInstanceValid(_detectionShape))
		{
			float attractionMultiplier = CardBenefitRuntime.ExperienceAttractionMultiplier;
			_detectionShape.Scale = Vector2.One * attractionMultiplier;
		}
		IsActive = false;
		GlobalMessengerManager.Instance.SendNotification($"掉落 {expBallPointsData.ExpPoints} 点经验！");
	}
}

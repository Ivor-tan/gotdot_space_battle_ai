using Godot;

public partial class PlayerMoveController : Node2D
{

	[ExportGroup("Player Stats")]
	[Export] PlayerStatus PlayerStats;


	// 当前速度
	public float CurrentSpeed = 0.0f;
	private bool isRotate = false;
	// 目标位置
	private Vector2 targetPosition;
	// 是否正在移动
	private bool isMoving = false;
	public PlayerStatus CurrentPlayerStates;

	//这两个要改改
	[Export] public int DetectionFrame = 10;
	private int FrameCounter = 0;
	public override void _Ready()
	{
		CurrentPlayerStates = PlayerStats.Duplicate() as PlayerStatus;
		targetPosition = Position;
	}
	public override void _PhysicsProcess(double delta)
	{
		MoveAndRotate(delta);
		DetectEnemy();
	}
	public void ApplyExternalDisplacement(Vector2 displacement)
	{
		GlobalPosition += displacement;
	}
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			var spaceState = GetWorld2D().DirectSpaceState;

			var query = new PhysicsPointQueryParameters2D();
			query.CollisionMask = (1 << 0) | (1 << 2);
			query.Position = GetGlobalMousePosition();
			query.CollideWithAreas = true; // 是否检测 Area2D
			query.CollideWithBodies = true; // 是否检测 CharacterBody2D
			if (spaceState.IntersectPoint(query).Count == 0)
			{
				targetPosition = GetGlobalMousePosition();
				isMoving = true;
				isRotate = true;
			}


		}
	}
	private void MoveAndRotate(double delta)
	{
		Vector2 forward = Vector2.FromAngle(Rotation);
		// 如果正在移动，则平滑插值到目标位置
		if (isMoving)
		{
			// 速度插值：加速到最大速度
			if (CurrentSpeed < CurrentPlayerStates.MaxSpeed)
			{
				CurrentSpeed = Mathf.Min(CurrentSpeed + CurrentPlayerStates.Acceleration * (float)delta, CurrentPlayerStates.MaxSpeed);
			}
			Position += forward * CurrentSpeed * (float)delta;
			// 接近目标位置时停止
			if (Position.DistanceTo(targetPosition) < CurrentPlayerStates.PositionThreshold)
			{
				isMoving = false;
				isRotate = false;
			}
		}
		else
		{
			// 停止时减速
			if (CurrentSpeed > 0)
			{
				CurrentSpeed = Mathf.Max(CurrentSpeed - CurrentPlayerStates.Deceleration * (float)delta, 0);
			}
			Position += forward * CurrentSpeed * (float)delta;
		}

		// 如果正在移动，则平滑插值到目标位置
		if (isRotate)
		{
			// 旋转逻辑：朝向目标位置
			Vector2 dir = targetPosition - GlobalPosition;

			float diff = Mathf.AngleDifference(Rotation, dir.Angle());

			float step = CurrentPlayerStates.RotateSpeed * (float)delta;

			Rotation += Mathf.Clamp(diff, -step, step);

			if (Mathf.Abs(diff) < CurrentPlayerStates.threshold)
			{
				isRotate = false;
			}

			// 阵型环位保持固定；每艘舰船只围绕自身中心转向旗舰朝向。
			PlayerManager.Instance.PlayersParent.GlobalRotation = 0;
			foreach (PlayerController player in PlayerManager.Instance.CurrentPlayerDic.Values)
			{
				if (player != null && IsInstanceValid(player))
				{
					player.GlobalRotation = Rotation;
				}
			}



		}
	}
	private void DetectEnemy()
	{
		FrameCounter++;
		if (FrameCounter % DetectionFrame == 0)
		{
			var target = EnemySpawnerManager.Instance.GetClosestEnemyTo(GlobalPosition, CurrentPlayerStates.AttackRange);

			if (target != null)
			{
				// 攻击逻辑...
				if (PlayerManager.Instance.ReadyToAttackQueue.Count > 0)
				{
					var attacker = PlayerManager.Instance.ReadyToAttackQueue.Peek();
					attacker.OnFire(target.GlobalPosition);
					PlayerManager.Instance.ReadyToAttackQueue.Dequeue();
				}
			}
		}
	}
}

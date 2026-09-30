using Godot;
using System;

public partial class BulletLine : Line2D
{


	[Export] public int MaxPoints = 100;
	[Export] public Node2D pointPosition = null;
	[Export] public float offset = 5.0f;

	private Vector2 lastPoint;

	public override void _Process(double delta)
	{
		if (pointPosition == null)
		{
			return;
		}
		Vector2 dir = Vector2.Right.Rotated(pointPosition.Rotation);
		Vector2 pos = pointPosition.GlobalPosition - dir * offset;

		AddPoint(pos);

		if (GetPointCount() > MaxPoints)
		{
			RemovePoint(0);
		}
	}
	public void ResetTrail()
	{
		ClearPoints(); // 清空所有旧点
	}
}

using Godot;
using System;

public partial class ShowAttackRange : Node2D
{
	[ExportGroup("Range Visual")]
	[Export] public float AttackRange;
	[Export] public Color RangeColor;
	[Export] public float LineWidth;

	public override void _Ready()
	{
		GlobalMessengerManager.Instance.Connect(
			GlobalMessengerManager.SignalName.OnUpdateAttackRange,
			Callable.From<float>(UpdateAttackRange));
		AttackRange = PlayerManager.Instance.Player.CurrentPlayerStates.AttackRange;
	}

	private void UpdateAttackRange(float range)
	{
		AttackRange = range;
		QueueRedraw();// 修改攻击范围之后，必须手动调用这一行，放到修改攻击范围函数里调用，否则屏幕上的圈大小不会更新！
	}

	// --- 绘制攻击范围 ---
	public override void _Draw()
	{
		int points = 30;
		DrawArc(Vector2.Zero, AttackRange, 0, Mathf.Tau, points, RangeColor, LineWidth, true);
	}
}

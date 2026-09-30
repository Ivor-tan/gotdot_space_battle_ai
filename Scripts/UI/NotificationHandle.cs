using Godot;
using System;
using System.Collections.Generic;

public partial class NotificationHandle : PanelContainer
{
	[Export] private GridContainer messageRoot;
	[Export] public int MaxMessages = 5;
	[Export] public float DisplayTime = 2.0f;
	[Export] public float FadeTime = 0.5f;

	private readonly Queue<Label> Pool = new Queue<Label>();
	private readonly List<Label> ActiveLabels = new List<Label>();

	public override void _Ready()
	{
		GlobalMessengerManager.Instance.OnNotification += OnNotificationReceived;
	}

	private void OnNotificationReceived(string message)
	{
		if (ActiveLabels.Count >= MaxMessages)
		{
			RecycleLabel(ActiveLabels[0]);
		}

		Label label = GetLabelFromPool();
		label.Text = message;
		label.Modulate = new Color(1, 1, 1, 1);
		label.Show();

		if (label.GetParent() == null)
		{
			messageRoot.AddChild(label);
		}
		messageRoot.MoveChild(label, -1); // 保证新消息在最下面

		ActiveLabels.Add(label);

		StartFadeOut(label);
	}

	private Label GetLabelFromPool()
	{
		return Pool.Count > 0 ? Pool.Dequeue() : new Label();
	}

	private void StartFadeOut(Label label)
	{
		if (label.HasMeta("active_tween"))
		{
			var oldTween = (Tween)label.GetMeta("active_tween");
			if (oldTween != null && oldTween.IsValid())
			{
				oldTween.Kill();
			}
		}

		Tween tween = CreateTween();
		label.SetMeta("active_tween", tween);

		tween.TweenInterval(DisplayTime);
		tween.TweenProperty(label, "modulate", new Color(1, 1, 1, 0), FadeTime);

		// 动画自然结束时的回调
		tween.Finished += () =>
		{
			RecycleLabel(label);
		};
	}

	private void RecycleLabel(Label label)
	{
		if (ActiveLabels.Contains(label))
		{
			ActiveLabels.Remove(label);

			// 强制停止当前动画
			if (label.HasMeta("active_tween"))
			{
				var tween = (Tween)label.GetMeta("active_tween");
				if (tween != null && tween.IsValid())
				{
					tween.Kill();
				}
			}

			label.Hide();
			Pool.Enqueue(label);
		}
	}

	public override void _ExitTree()
	{
		GlobalMessengerManager.Instance.OnNotification -= OnNotificationReceived;

		// 清理池子，释放内存
		while (Pool.Count > 0) Pool.Dequeue().QueueFree();
		ActiveLabels.Clear();
	}
}
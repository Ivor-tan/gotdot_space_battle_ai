using Godot;
using System;

public partial class TipsInfo : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	[Export] private Label NameLabel;
	[Export] private Label HpLabel;
	private Tween FadeTween;
	private Vector2 DefaultPosition;
	public override void _Ready()
	{
		DefaultPosition = Position;
		Hide(); // 初始隐藏
		GlobalMessengerManager.Instance.OnShowTipsInfo += OnShowTipsReceived;

	}

	public override void _ExitTree()
	{
		GlobalMessengerManager.Instance.OnShowTipsInfo -= OnShowTipsReceived;
	}

	private void OnShowTipsReceived(TipsInfoMessages messages)
	{
		UpdateInfo(messages.Name, messages.Hp);
	}

	// 更新并显示信息的方法
	public void UpdateInfo(string name, int hp)
	{
		NameLabel.Text = $"{name}";
		HpLabel.Text = $" {hp}";
		Show();
		PlayFancyFade();
	}


	public void PlayFancyFade()
	{
		if (FadeTween != null) FadeTween.Kill();

		Show();
		Modulate = new Color(1, 1, 1, 1); // 透明度恢复 100%
		Position = DefaultPosition;      // 位置恢复到初始点

		FadeTween = CreateTween().SetParallel(true);

		// 透明度淡出
		FadeTween.TweenProperty(this, "modulate:a", 0.0f, 0.8f).SetDelay(1.0f);

		// 向上漂浮 30 像素
		Vector2 targetPos = Position + new Vector2(0, -30);
		FadeTween.TweenProperty(this, "position", targetPos, 0.8f)
				  .SetDelay(1.0f)
				  .SetTrans(Tween.TransitionType.Back)
				  .SetEase(Tween.EaseType.Out);

		FadeTween.Chain().Connect("finished", Callable.From(Hide));
	}


}

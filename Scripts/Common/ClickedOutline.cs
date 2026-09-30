using Godot;
using System;

public partial class ClickedOutline : Sprite2D
{
	private Vector2 clickedPosition;
	public override void _Input(InputEvent @event)
	{

		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			// 获取鼠标点击的全局世界坐标
			clickedPosition = GetGlobalMousePosition();
			showOutline();
		}
	}

	private void showOutline()
	{
		Visible = true;
		Position = clickedPosition;
		var tween = CreateTween();

		Scale = Vector2.One * 0.2f;
		Modulate = new Color(1, 1, 1, 1);

		tween.TweenProperty(this, "scale", Vector2.One * 0.5f, 0.3f);
		tween.TweenProperty(this, "modulate:a", 0.0f, 0.3f);

		tween.TweenCallback(Callable.From(() =>
		{
			Visible = false;
		}));
	}
}

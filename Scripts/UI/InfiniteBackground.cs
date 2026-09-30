using Godot;
using System;

public partial class InfiniteBackground : Parallax2D
{
	private ColorRect _rect;

	public override void _Ready()
	{
		_rect = GetNode<ColorRect>("ColorRect");
		UpdateLayout();
		GetTree().Root.SizeChanged += UpdateLayout;
	}


	private void UpdateLayout()
	{
		Vector2 screenSize = GetViewportRect().Size;

		_rect.Size = screenSize;

		RepeatSize = screenSize;
		RepeatTimes = 3;
	}
}

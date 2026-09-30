using Godot;
using System;

public partial class CloseButton : Button
{
	[Export] CanvasItem CloseItem;
	public override void _Ready()
	{
		Pressed += OnClose;
	}

	private void OnClose()
	{
		CloseItem.Visible = false;
	}
}

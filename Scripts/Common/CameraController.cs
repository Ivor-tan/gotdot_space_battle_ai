using Godot;
using PhantomCamera;
using System;

public partial class CameraController : Camera2D
{
	public PhantomCamera2D phantomCamera;

	public override void _Ready()
	{
		phantomCamera = PlayerManager.Instance.Player.GetNode<PhantomCamera2D>("%PhantomCamera2D");
		LogUtil.Info(phantomCamera.Zoom);
	}



}

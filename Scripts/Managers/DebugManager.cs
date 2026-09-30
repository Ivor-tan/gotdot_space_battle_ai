using Godot;
using System;

public partial class DebugManager : Node
{
	[Export] public bool EnableDebug = true;
	[Export] public BaseEnhanceFunction DebugScene;
	[Export] public DebugTestPanel TestPanel;
	private bool _prevGDown = false;
	private bool _prevHDown = false;
	private bool _prevDDown = false;
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
	}
	public override void _Process(double delta)
	{
		bool gNow = Input.IsKeyPressed(Key.G);
		bool hNow = Input.IsKeyPressed(Key.H);
		bool dNow = Input.IsKeyPressed(Key.D);

		if (gNow && !_prevGDown)
		{
			OnGKeyPressed();
		}

		if (hNow && !_prevHDown)
		{
			OnHKeyPressed();
		}

		if (dNow && !_prevDDown)
		{
			OnDKeyPressed();
		}

		_prevGDown = gNow;
		_prevHDown = hNow;
		_prevDDown = dNow;
	}

	public bool IsGKeyJustPressed()
	{
		return Input.IsKeyPressed(Key.G) && !_prevGDown;
	}

	public bool IsHKeyJustPressed()
	{
		return Input.IsKeyPressed(Key.H) && !_prevHDown;
	}

	private void OnGKeyPressed()
	{
		if (!EnableDebug) return;
		GlobalMessengerManager.Instance.SendShowEnhanceInfo(1);
	}

	private void OnHKeyPressed()
	{
		if (!EnableDebug) return;
		DebugScene.ApplyEffect();
	}

	private void OnDKeyPressed()
	{
		if (!EnableDebug || !MetaProgressStore.IsProgressionDebugEnabled())
		{
			return;
		}

		if (TestPanel == null || !IsInstanceValid(TestPanel))
		{
			LogUtil.Warning("Debug test panel is unavailable.");
			return;
		}

		TestPanel.Toggle();
	}
}

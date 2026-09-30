using Godot;
using System;

public partial class MusicProgress : HSlider
{
	public override void _Ready()
	{
		Value = GameConfigManager.Instance.GameConfig.MusicVolume;
	}
	public void _on_value_changed(float value)
	{
		GameConfigManager.Instance.SetMusicVolume(value);
	}
}

using Godot;
using System;

public partial class SFXProgress : HSlider
{
	public override void _Ready()
	{
		Value = GameConfigManager.Instance.GameConfig.SFXVolume;
	}
	public void _on_value_changed(float value)
	{
		GameConfigManager.Instance.SetSFXVolume(value);
	}
}

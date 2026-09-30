using Godot;
using System;
[GlobalClass]
public partial class GameConfig : Resource
{
	[Export]
	public float MusicVolume = 1f;

	[Export]
	public float SFXVolume = 1f;

	[Export]
	public string Language = "zh_CN";

	[Export]
	public bool EnableProgressionDebug;

	public GameConfig() { }
}

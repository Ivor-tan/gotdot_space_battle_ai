using Godot;

[GlobalClass]
public partial class AssetConfig : Resource
{
	[Export]
	public Godot.Collections.Array<string> ScanPaths = new();

	[Export]
	public string OutputPath = "res://Scripts/Assets.cs";

	[Export]
	public Godot.Collections.Array<AssetEntry> Assets = new();
	public AssetConfig() { }
}
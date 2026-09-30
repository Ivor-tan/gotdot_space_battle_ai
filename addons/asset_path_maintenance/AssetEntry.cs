using Godot;

[GlobalClass]
public partial class AssetEntry : Resource
{
    [Export]
    public string Name = "";

    [Export]
    public string Path = "";
}
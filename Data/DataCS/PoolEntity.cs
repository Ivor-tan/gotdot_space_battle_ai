using Godot;

[GlobalClass]
public partial class PoolEntity : Resource
{
	[Export] public PoolType Type;
	[Export] public PackedScene Scene;
}
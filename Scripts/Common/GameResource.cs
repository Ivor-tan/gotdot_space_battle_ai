using Godot;

public abstract partial class GameResource : Resource
{
	public StringName ID
	{
		get
		{
			return ResourcePath.GetFile().GetBaseName();
		}
	}
}
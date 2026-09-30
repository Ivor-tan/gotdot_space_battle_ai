using Godot;

[GlobalClass]
public partial class AbilityComponent : Node2D
{
	[Export] public string AbilityId;
	public int Level = 1;

	// 将 abstract 改为 virtual
	public virtual void Upgrade()
	{
		// 留空或抛出异常提醒子类重写
	}
}
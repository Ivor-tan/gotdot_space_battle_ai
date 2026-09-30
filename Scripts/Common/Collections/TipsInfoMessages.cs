using Godot;

// 1. 必须继承自 RefCounted (会自动内存管理) 或 GodotObject
[GlobalClass]
public partial class TipsInfoMessages : RefCounted
{
	public string Name { get; set; }
	public int Hp { get; set; }

	public TipsInfoMessages(string name, int hp)
	{
		Name = name;
		Hp = hp;
	}

	public TipsInfoMessages() { }
}
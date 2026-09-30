using Godot;


public partial class PlantsController : CharacterBody2D
{
	[Export]
	public PlanetsStatus planetStats;
	[Export]
	public float Speed = 10f; // 度/秒

	[Export]
	public int spawnCount = 15; // 环上石头数量

	[Export]
	public float baseRadius = 100f; // 基本半径

	[Export]
	public float radiusRandomOffset = 30f; // 半径随机偏移范围 ±offset
	public override void _Ready()
	{
		for (int i = 0; i < spawnCount; i++)
		{
			LoadAsteroids(i);
		}
	}
	public override void _Process(double delta)
	{
		// rotation_degrees 顺时针，delta 秒
		RotationDegrees += (float)(GD.Randf() / 2 * Speed * (float)delta);
	}

	public void LoadAsteroids(int index)
	{
		Vector2 center = Position; // 当前节点作为圆心
		float angle = index * Mathf.Tau / spawnCount; // 平均角度分布

		float r = baseRadius + (GD.Randf() * 2 - 1) * radiusRandomOffset; // ±offset

		Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
		var asteroid = ObjectPoolManager.Instance.Spawn(PoolType.Asteroid, pos, this) as AsteroidsController;
		asteroid.Reset();
	}


}

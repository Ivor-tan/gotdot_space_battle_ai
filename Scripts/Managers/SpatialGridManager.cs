using Godot;
using Godot.Collections;
using System;


public partial class SpatialGridManager : Node
{
	[Export] public int CellSize = 128;
	[Export] public int MaxEnemies = 1000;

	private EnemyData[] _allEnemiesData;
	private const int MAX_LOCK = 3;
	private readonly Dictionary<Vector2I, Array<int>> _grid = new();

	public override void _Ready()
	{
		_allEnemiesData = new EnemyData[MaxEnemies];
	}

	/// <summary>
	/// 每帧由管理器统一调用，同步敌人位置并重建网格
	/// </summary>
	public void UpdateGrid(Array<Node2D> enemyNodes)
	{
		_grid.Clear();
		int count = Mathf.Min(enemyNodes.Count, MaxEnemies);

		for (int i = 0; i < count; i++)
		{
			Node2D enemy = enemyNodes[i];
			if (!IsInstanceValid(enemy)) continue;

			// 1. 同步数据到连续内存
			_allEnemiesData[i] = new EnemyData
			{
				Index = i,
				InstanceId = enemy.GetInstanceId(),
				Position = enemy.GlobalPosition,
				LockCount = 0,
			};

			// 2. 计算网格坐标
			Vector2I cell = WorldToGrid(_allEnemiesData[i].Position);

			if (!_grid.ContainsKey(cell))
			{
				_grid[cell] = new Array<int>();
			}
			_grid[cell].Add(i);
		}
	}

	public Vector2I WorldToGrid(Vector2 pos) =>
		new Vector2I(Mathf.FloorToInt(pos.X / CellSize), Mathf.FloorToInt(pos.Y / CellSize));
	/// <summary>
	/// 高效查询最近敌人
	/// </summary>
	public Node2D GetBestEnemy(Vector2 searchPos, float radius, Array<Node2D> enemyNodes)
	{
		Vector2I centerCell = WorldToGrid(searchPos);
		float radiusSq = radius * radius;

		float minScore = float.MaxValue;
		int bestIndex = -1;

		for (int x = -1; x <= 1; x++)
		{
			for (int y = -1; y <= 1; y++)
			{
				Vector2I cellCoords = centerCell + new Vector2I(x, y);

				if (_grid.TryGetValue(cellCoords, out Array<int> indices))
				{
					foreach (int idx in indices)
					{
						// Pool releases may shrink ActiveEnemies after the grid was built.
						if (idx < 0 || idx >= enemyNodes.Count || idx >= _allEnemiesData.Length)
						{
							continue;
						}

						ref EnemyData data = ref _allEnemiesData[idx];
						Node2D enemy = enemyNodes[idx];
						if (enemy == null || !IsInstanceValid(enemy) || enemy.GetInstanceId() != data.InstanceId)
						{
							continue;
						}
						// 过滤掉“已被占满的目标”
						if (data.LockCount >= MAX_LOCK)
							continue;
						float distSq = searchPos.DistanceSquaredTo(data.Position);
						if (distSq > radiusSq) continue;

						float score = distSq
									+ data.LockCount * 20000.0f;

						if (score < minScore)
						{
							minScore = score;
							bestIndex = data.Index;
						}
					}
				}
			}
		}

		if (bestIndex >= 0 && bestIndex < enemyNodes.Count && bestIndex < _allEnemiesData.Length)
		{
			ref EnemyData data = ref _allEnemiesData[bestIndex];
			Node2D enemy = enemyNodes[bestIndex];
			if (enemy == null || !IsInstanceValid(enemy) || enemy.GetInstanceId() != data.InstanceId)
			{
				return null;
			}

			data.LockCount++;
			return enemy;
		}

		return null;
	}
}

public struct EnemyData
{
	public int Index;        // 敌人在原始数组中的索引
	public Vector2 Position; // 缓存的全局位置
	public ulong InstanceId;
	public int LockCount;
}

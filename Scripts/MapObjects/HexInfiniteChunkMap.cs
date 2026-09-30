using Godot;
using System;
using System.Collections.Generic;

public partial class HexInfiniteChunkMap : TileMapLayer
{
	private Node2D Player;

	[ExportGroup("Spacing Settings")]
	/// <summary>
	/// 边缘留白宽度：1 表示边缘 1 格不生成障碍物。
	/// </summary>
	[Export] public int EdgeMargin = 2;

	/// <summary>
	/// 每个 Tile 占据的最小网格范围 (例如 4x4)
	/// </summary>
	[Export] public int MinDistance = 4;

	/// <summary>
	/// 随机生成障碍物概率 (0.0 - 1.0)
	/// </summary>
	[Export] public float SpawnChance = 0.6f;

	[ExportGroup("Chunk Settings")]
	[Export] public int ChunkSize = 16;
	[Export] public int LoadRadius = 3;
	[Export] public int TilesPerFrame = 40;

	private Vector2I currentChunk;
	private HashSet<Vector2I> loadedChunks = new();
	private Queue<Vector2I> buildQueue = new();
	private FastNoiseLite heightNoise;

	public override void _Ready()
	{
		Player = PlayerManager.Instance.Player;
		heightNoise = new FastNoiseLite();
		heightNoise.Seed = (int)GD.Randi();
		heightNoise.Frequency = 0.03f;

		Vector2I startCell = LocalToMap(Player.GlobalPosition);
		currentChunk = GetChunk(startCell);

		UpdateChunks();
	}

	public override void _Process(double delta)
	{
		UpdateBuildQueue();

		Vector2I playerCell = LocalToMap(Player.GlobalPosition);
		Vector2I chunk = GetChunk(playerCell);

		if (chunk != currentChunk)
		{
			currentChunk = chunk;
			UpdateChunks();
		}
	}

	void UpdateBuildQueue()
	{
		int batch = TilesPerFrame;
		while (batch-- > 0 && buildQueue.Count > 0)
		{
			Vector2I cell = buildQueue.Dequeue();
			GenerateTile(cell);
		}
	}

	Vector2I GetChunk(Vector2I cell)
	{
		return new Vector2I(
			Mathf.FloorToInt((float)cell.X / ChunkSize),
			Mathf.FloorToInt((float)cell.Y / ChunkSize)
		);
	}

	void UpdateChunks()
	{
		HashSet<Vector2I> needed = new();

		for (int x = -LoadRadius; x <= LoadRadius; x++)
		{
			for (int y = -LoadRadius; y <= LoadRadius; y++)
			{
				Vector2I chunk = currentChunk + new Vector2I(x, y);
				needed.Add(chunk);

				if (!loadedChunks.Contains(chunk))
				{
					QueueChunk(chunk);
					loadedChunks.Add(chunk);
				}
			}
		}

		List<Vector2I> remove = new();
		foreach (var c in loadedChunks)
		{
			if (!needed.Contains(c)) remove.Add(c);
		}

		foreach (var c in remove)
		{
			UnloadChunk(c);
			loadedChunks.Remove(c);
		}
	}

	// 入队逻辑
	void QueueChunk(Vector2I chunk)
	{
		for (int x = EdgeMargin; x < ChunkSize - EdgeMargin; x += MinDistance)
		{
			for (int y = EdgeMargin; y < ChunkSize - EdgeMargin; y += MinDistance)
			{
				int globalX = chunk.X * ChunkSize + x;
				int globalY = chunk.Y * ChunkSize + y;
				buildQueue.Enqueue(new Vector2I(globalX, globalY));
			}
		}
	}

	void GenerateTile(Vector2I gridOrigin)
	{
		//大质数 防止重叠
		int seed = (gridOrigin.X * 73856093) ^ (gridOrigin.Y * 19349663);
		Random prng = new Random(seed);

		float density = heightNoise.GetNoise2D(gridOrigin.X * 0.5f, gridOrigin.Y * 0.5f);
		if (density < -0.1f) return;

		if ((float)prng.NextDouble() > SpawnChance) return;

		int localX = gridOrigin.X % ChunkSize;
		if (localX < 0) localX += ChunkSize;
		int localY = gridOrigin.Y % ChunkSize;
		if (localY < 0) localY += ChunkSize;

		// 5. 限制偏移范围，防止跨越 EdgeMargin
		int maxOffsetX = Mathf.Min(MinDistance, (ChunkSize - EdgeMargin) - localX);
		int maxOffsetY = Mathf.Min(MinDistance, (ChunkSize - EdgeMargin) - localY);
		maxOffsetX = Mathf.Max(1, maxOffsetX);
		maxOffsetY = Mathf.Max(1, maxOffsetY);

		int offsetX = prng.Next(0, maxOffsetX);
		int offsetY = prng.Next(0, maxOffsetY);

		Vector2I finalPos = new Vector2I(gridOrigin.X + offsetX, gridOrigin.Y + offsetY);
		SetCell(finalPos, 6, new Vector2I(0, 0), 1);
	}

	void UnloadChunk(Vector2I chunk)
	{
		for (int x = EdgeMargin; x < ChunkSize - EdgeMargin; x += MinDistance)
		{
			for (int y = EdgeMargin; y < ChunkSize - EdgeMargin; y += MinDistance)
			{
				int globalX = chunk.X * ChunkSize + x;
				int globalY = chunk.Y * ChunkSize + y;

				for (int ox = 0; ox < MinDistance; ox++)
				{
					for (int oy = 0; oy < MinDistance; oy++)
					{
						if (x + ox >= ChunkSize - EdgeMargin || y + oy >= ChunkSize - EdgeMargin) continue;
						EraseCell(new Vector2I(globalX + ox, globalY + oy));
					}
				}
			}
		}
	}
}
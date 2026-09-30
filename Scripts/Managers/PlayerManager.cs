
using Godot;
using Godot.Collections;
using System.Collections.Generic;

using System.Linq;


public partial class PlayerManager : Singleton<PlayerManager>
{
	[Export] public PlayerMoveController Player;
	[Export] public Node2D PlayersParent;
	[Export] public float Spacing = 40;
	[Export] public float FleetMoveSpeed = 100.0f;
	private Array<Vector2> positionList = new Array<Vector2>();
	public Array<Vector2> PositionList => positionList;
	public Queue<IFireReady> ReadyToAttackQueue = new Queue<IFireReady>();
	[Export] public int MaxPlayerCount = 19;
	public int CurrentPlayerCount = 0;
	public int DisabledPlayerCount { get; private set; }
	public int FleetShieldCharges { get; private set; }
	private string InitialPlayerScenePath = Assets.BasePlayer;
	public Godot.Collections.Dictionary<int, PlayerController> CurrentPlayerDic = new();
	public Godot.Collections.Dictionary<int, BulletSpawn> PlayerBulletSpawns = new();
	private static readonly Vector2I[] HexDirections =
	{
		new Vector2I(1, 0),
		new Vector2I(0, 1),
		new Vector2I(-1, 1),
		new Vector2I(-1, 0),
		new Vector2I(0, -1),
		new Vector2I(1, -1)
	};

	// 全局 Addiction 类型的增益列表（由 EnhanceFunctionManager 过滤得到）
	public Array<BaseEnhanceFunction> AddictionEnhanceList = new Array<BaseEnhanceFunction>();
	// 每个玩家当前持有的增益（playerIndex -> list of BaseEnhanceFunction）
	public Godot.Collections.Dictionary<int, Array<BaseEnhanceFunction>> PlayerEnhances = new();

	public override void _Ready()
	{
		base._Ready();
		if (GlobalMessengerManager.Instance != null)
		{
			GlobalMessengerManager.Instance.Connect(
				GlobalMessengerManager.SignalName.OnPlayerLoss,
				Callable.From<int>(OnLoss)
			);
		}
		InitPoints();
		// Populate AddictionEnhanceList from EnhanceFunctionManager if available
		if (EnhanceFunctionManager.Instance != null)
		{
			foreach (var ef in EnhanceFunctionManager.Instance.EnhanceFunctionList)
			{
				if (ef != null && ef.FunctionType == EnhanceFunctionType.AddictionFunction)
				{
					AddictionEnhanceList.Add(ef);
				}
			}
		}

		PlayerShipData initialShipData = RunConfiguration.SelectedInitialShip;
		PackedScene initialShipScene = initialShipData?.ShipScene ?? GD.Load<PackedScene>(InitialPlayerScenePath);
		AddPlayer(initialShipScene, initialShipData);
		CallDeferred(nameof(applyFleetMoveSpeed));
	}

	public override void _Process(double delta)
	{
		CardBenefitRuntime.Process(delta);
	}

	private void applyFleetMoveSpeed()
	{
		if (Player != null && IsInstanceValid(Player) && Player.CurrentPlayerStates != null)
		{
			Player.CurrentPlayerStates.MaxSpeed = FleetMoveSpeed;
		}
	}
	public override void _ExitTree()
	{
		base._ExitTree();
		if (GlobalMessengerManager.Instance != null)
		{
			GlobalMessengerManager.Instance.OnPlayerLoss -= OnLoss;
		}
	}
	public void InitPoints()
	{
		if (MaxPlayerCount <= 0) return;
		positionList.Clear();
		positionList.Add(Vector2.Zero);
		if (MaxPlayerCount == 1)
		{
			return;
		}

		int layer = 1;
		while (positionList.Count < MaxPlayerCount)
		{
			Vector2I hexCoordinate = new(0, -layer);
			for (int directionIndex = 0; directionIndex < HexDirections.Length; directionIndex++)
			{
				for (int step = 0; step < layer && positionList.Count < MaxPlayerCount; step++)
				{
					positionList.Add(getFormationPosition(hexCoordinate));
					hexCoordinate += HexDirections[directionIndex];
				}
			}

			layer++;
		}
	}

	private Vector2 getFormationPosition(Vector2I hexCoordinate)
	{
		const float HexRowOffset = 0.8660254f;
		return new Vector2(
			(hexCoordinate.X + hexCoordinate.Y * 0.5f) * Spacing,
			hexCoordinate.Y * Spacing * HexRowOffset
		);
	}

	public (int, Vector2) GetNextSpawnPositionAndIndex()
	{
		int targetIndex = 1;
		while (CurrentPlayerDic.ContainsKey(targetIndex) && CurrentPlayerDic[targetIndex] != null)
		{
			targetIndex++;
		}

		return getSpawnPositionAtIndex(targetIndex);
	}

	public bool isSlotAvailable(int playerIndex)
	{
		return playerIndex > 0 && playerIndex <= MaxPlayerCount &&
			(!CurrentPlayerDic.ContainsKey(playerIndex) || CurrentPlayerDic[playerIndex] == null);
	}

	public bool tryPromoteShipAtIndex(int playerIndex, PlayerShipData shipData, int requiredStarLevel)
	{
		if (shipData == null || !IsInstanceValid(shipData) || !CurrentPlayerDic.ContainsKey(playerIndex))
		{
			return false;
		}

		PlayerController player = CurrentPlayerDic[playerIndex];
		if (player == null || !IsInstanceValid(player) || player.ShipData == null ||
			!IsInstanceValid(player.ShipData) || player.ShipData.ShipTypeId != shipData.ShipTypeId ||
			player.StarLevel != requiredStarLevel)
		{
			return false;
		}

		return player.tryPromoteStarLevel();
	}

	public bool tryMoveOrSwapShip(int sourceIndex, int targetIndex)
	{
		if (sourceIndex == targetIndex || !CurrentPlayerDic.ContainsKey(sourceIndex))
		{
			return false;
		}

		PlayerController sourcePlayer = CurrentPlayerDic[sourceIndex];
		if (sourcePlayer == null || !IsInstanceValid(sourcePlayer) || targetIndex < 1 || targetIndex > MaxPlayerCount)
		{
			return false;
		}

		PlayerController targetPlayer = CurrentPlayerDic.ContainsKey(targetIndex)
			? CurrentPlayerDic[targetIndex]
			: null;
		CurrentPlayerDic[sourceIndex] = targetPlayer;
		CurrentPlayerDic[targetIndex] = sourcePlayer;
		movePlayerToSlot(sourcePlayer, targetIndex);
		if (targetPlayer != null && IsInstanceValid(targetPlayer))
		{
			movePlayerToSlot(targetPlayer, sourceIndex);
		}

		swapPlayerSlotData(sourceIndex, targetIndex);
		return true;
	}

	public bool tryMergeShips(int sourceIndex, int targetIndex)
	{
		if (sourceIndex == targetIndex || !CurrentPlayerDic.ContainsKey(sourceIndex) ||
			!CurrentPlayerDic.ContainsKey(targetIndex))
		{
			return false;
		}

		PlayerController sourcePlayer = CurrentPlayerDic[sourceIndex];
		PlayerController targetPlayer = CurrentPlayerDic[targetIndex];
		if (sourcePlayer == null || targetPlayer == null || !IsInstanceValid(sourcePlayer) ||
			!IsInstanceValid(targetPlayer) || sourcePlayer.ShipData == null || targetPlayer.ShipData == null ||
			!IsInstanceValid(sourcePlayer.ShipData) || !IsInstanceValid(targetPlayer.ShipData) ||
			sourcePlayer.ShipData.ShipTypeId != targetPlayer.ShipData.ShipTypeId ||
			sourcePlayer.StarLevel != targetPlayer.StarLevel)
		{
			return false;
		}

		if (!targetPlayer.tryPromoteStarLevel())
		{
			return false;
		}

		OnLoss(sourceIndex);
		return true;
	}

	public void UpdatePlayerStats()
	{
		CurrentPlayerCount = CurrentPlayerDic.Values.Count(v => v != null);
	}

	//按百分比增加伤害
	public void SetBulletDamage(int damageIncrease)
	{
		int increaseFactor = (100 + damageIncrease) / 100;
		// Update already-registered BulletSpawns
		foreach (var kvp in PlayerBulletSpawns)
		{
			if (kvp.Value != null)
			{
				kvp.Value.Damage = kvp.Value.Damage * increaseFactor;
			}
		}

		// Ensure any unregistered players get updated and registered
		foreach (var kvp in CurrentPlayerDic)
		{
			int idx = kvp.Key;
			var player = kvp.Value;
			if (player == null) continue;
			if (!PlayerBulletSpawns.ContainsKey(idx))
			{
				if (player.BulletSpawn != null)
				{
					player.BulletSpawn.Damage = player.BulletSpawn.Damage * increaseFactor;
					PlayerBulletSpawns[idx] = player.BulletSpawn;
				}
				else
				{
					var bs = player.GetNodeOrNull<BulletSpawn>("BullteSpawn");
					if (bs != null)
					{
						bs.Damage = bs.Damage * increaseFactor;
						PlayerBulletSpawns[idx] = bs;
					}
				}
			}
		}
	}
	public void OnLoss(int index)
	{
		if (!CurrentPlayerDic.ContainsKey(index) || CurrentPlayerDic[index] == null) return;

		var player = CurrentPlayerDic[index];
		CardBenefitRuntime.OnPlayerLost(this, player);

		// 1) Unregister BulletSpawn mapping first to avoid later use

		if (PlayerBulletSpawns.ContainsKey(index))
		{
			PlayerBulletSpawns.Remove(index);
		}

		// Remove any enhance entries for this player to avoid dangling references
		if (PlayerEnhances.ContainsKey(index))
		{
			PlayerEnhances.Remove(index);
		}

		// 2) Stop timers / behaviors on player to prevent callbacks
		try
		{
			if (player.AttackTimer != null)
			{
				player.AttackTimer.Stop();
			}
		}
		catch { }

		// 3) Remove from ready-to-attack queue if present
		if (ReadyToAttackQueue.Count > 0)
		{
			var newQ = new Queue<IFireReady>();
			while (ReadyToAttackQueue.Count > 0)
			{
				var item = ReadyToAttackQueue.Dequeue();
				if (!object.ReferenceEquals(item, player)) newQ.Enqueue(item);
			}
			ReadyToAttackQueue = newQ;
		}

		// 4) Remove reference from current players map before returning to pool
		CurrentPlayerDic[index] = null;
		DisabledPlayerCount++;

		// also clear any player-specific enhancements list entry just in case (redundant but safe)
		if (PlayerEnhances.ContainsKey(index)) PlayerEnhances.Remove(index);

		// 5) Release to ObjectPoolManager instead of QueueFree for reuse
		if (ObjectPoolManager.Instance != null)
		{
			ObjectPoolManager.Instance.Release(PoolType.Player, player);
		}
		else
		{
			// fallback: free immediately
			player.QueueFree();
		}

		UpdatePlayerStats();
		if (CurrentPlayerCount <= 0)
		{
			GlobalMessengerManager.Instance.SendGameOver();
		}
	}
	public void AddPlayer(PackedScene scene)
	{
		AddPlayer(scene, null);
	}

	public bool hasShip(StringName shipId)
	{
		if (shipId == default)
		{
			return false;
		}

		foreach (PlayerController player in CurrentPlayerDic.Values)
		{
			if (player != null && IsInstanceValid(player) && player.ShipData != null &&
				IsInstanceValid(player.ShipData) && player.ShipData.ShipId == shipId)
			{
				return true;
			}
		}

		return false;
	}

	public int GetRingIndex(PlayerController player)
	{
		if (player == null || !IsInstanceValid(player) || PlayersParent == null || !IsInstanceValid(PlayersParent) || Spacing <= 0.0f)
		{
			return -1;
		}

		float distance = player.GlobalPosition.DistanceTo(PlayersParent.GlobalPosition);
		return Mathf.RoundToInt(distance / Spacing);
	}

	public void GetAdjacentPlayers(PlayerController source, Godot.Collections.Array<PlayerController> result)
	{
		result.Clear();
		if (source == null || !IsInstanceValid(source))
		{
			return;
		}

		float adjacentDistanceSquared = Spacing * Spacing * 1.21f;
		foreach (PlayerController candidate in CurrentPlayerDic.Values)
		{
			if (candidate == null || !IsInstanceValid(candidate) || candidate == source ||
				candidate.GlobalPosition.DistanceSquaredTo(source.GlobalPosition) > adjacentDistanceSquared)
			{
				continue;
			}

			result.Add(candidate);
		}
	}

	public void AddPlayer(PackedScene scene, PlayerShipData shipData)
	{
		var (targetIndex, _) = GetNextSpawnPositionAndIndex();
		if (targetIndex == -1)
		{
			LogUtil.Warning("No available fleet slot for the recruited ship.");
			return;
		}

		AddPlayerAtIndex(scene, shipData, targetIndex);
	}

	public void AddPlayerAtIndex(PackedScene scene, PlayerShipData shipData, int targetIndex)
	{
		if (scene == null || !IsInstanceValid(scene))
		{
			LogUtil.Warning("新增飞船失败：飞船场景未配置。");
			return;
		}

		if (Player == null || !IsInstanceValid(Player))
		{
			LogUtil.Warning("PlayerMoveController reference is null.");
			return;
		}

		if (!isSlotAvailable(targetIndex))
		{
			LogUtil.Warning($"Fleet slot {targetIndex} is unavailable.");
			return;
		}

		var (resolvedIndex, spawnPos) = getSpawnPositionAtIndex(targetIndex);
		if (resolvedIndex == -1)
		{
			LogUtil.Warning($"Fleet slot {targetIndex} exceeds the configured formation.");
			return;
		}
		if (targetIndex == -1)
		{
			LogUtil.Warning($"位置索引 {targetIndex - 1} 超出预设范围！");
			return;
		}

		Node2D obj = null;
		if (ObjectPoolManager.Instance != null)
		{
			obj = ObjectPoolManager.Instance.Create(PoolType.Player, scene, spawnPos, PlayersParent != null ? PlayersParent : Player);
		}
		else
		{
			obj = scene.Instantiate<Node2D>();
			if (PlayersParent != null) PlayersParent.AddChild(obj); else if (Player != null) Player.AddChild(obj);
			obj.GlobalPosition = spawnPos;
		}

		if (obj is PlayerController player)
		{
			player.Reset();
			player.applyShipData(shipData);
			player.addShieldCharges(FleetShieldCharges);
			player.PlayerIndex = targetIndex;
			CurrentPlayerDic[targetIndex] = player;
			player.Rotation = Player != null ? Player.Rotation : 0;
			// register player's BulletSpawn in manager for centralized access
			if (player.BulletSpawn != null)
			{
				PlayerBulletSpawns[player.PlayerIndex] = player.BulletSpawn;
			}
			else
			{
				// try to find child node (scene may have typo in NodePath)
				var bs = player.GetNodeOrNull<BulletSpawn>("BullteSpawn");
				if (bs != null) PlayerBulletSpawns[player.PlayerIndex] = bs;
			}
			// initialize enhance list for this player
			if (!PlayerEnhances.ContainsKey(targetIndex)) PlayerEnhances[targetIndex] = new Array<BaseEnhanceFunction>();
			UpdatePlayerStats();
		}
	}

	private (int, Vector2) getSpawnPositionAtIndex(int targetIndex)
	{
		int listIndex = targetIndex - 1;
		if (targetIndex <= 0 || listIndex >= positionList.Count)
		{
			return (-1, Vector2.Zero);
		}

		Vector2 spawnPos = positionList[listIndex] +
			(PlayersParent != null && IsInstanceValid(PlayersParent)
				? PlayersParent.GlobalPosition
				: Player != null ? Player.GlobalPosition : Vector2.Zero);
		return (targetIndex, spawnPos);
	}

	private void movePlayerToSlot(PlayerController player, int targetIndex)
	{
		var (resolvedIndex, targetPosition) = getSpawnPositionAtIndex(targetIndex);
		if (resolvedIndex == -1)
		{
			return;
		}

		player.PlayerIndex = targetIndex;
		player.GlobalPosition = targetPosition;
	}

	private void swapPlayerSlotData(int sourceIndex, int targetIndex)
	{
		BulletSpawn sourceBulletSpawn = PlayerBulletSpawns.ContainsKey(sourceIndex)
			? PlayerBulletSpawns[sourceIndex]
			: null;
		BulletSpawn targetBulletSpawn = PlayerBulletSpawns.ContainsKey(targetIndex)
			? PlayerBulletSpawns[targetIndex]
			: null;
		PlayerBulletSpawns.Remove(sourceIndex);
		PlayerBulletSpawns.Remove(targetIndex);
		if (sourceBulletSpawn != null && IsInstanceValid(sourceBulletSpawn))
		{
			PlayerBulletSpawns[targetIndex] = sourceBulletSpawn;
		}
		if (targetBulletSpawn != null && IsInstanceValid(targetBulletSpawn))
		{
			PlayerBulletSpawns[sourceIndex] = targetBulletSpawn;
		}

		Array<BaseEnhanceFunction> sourceEnhances = PlayerEnhances.ContainsKey(sourceIndex)
			? PlayerEnhances[sourceIndex]
			: null;
		Array<BaseEnhanceFunction> targetEnhances = PlayerEnhances.ContainsKey(targetIndex)
			? PlayerEnhances[targetIndex]
			: null;
		PlayerEnhances.Remove(sourceIndex);
		PlayerEnhances.Remove(targetIndex);
		if (sourceEnhances != null)
		{
			PlayerEnhances[targetIndex] = sourceEnhances;
		}
		if (targetEnhances != null)
		{
			PlayerEnhances[sourceIndex] = targetEnhances;
		}
	}

	public void addFleetShieldCharges(int charges)
	{
		if (charges <= 0)
		{
			return;
		}

		FleetShieldCharges += charges;
		foreach (PlayerController player in CurrentPlayerDic.Values)
		{
			if (player != null && IsInstanceValid(player))
			{
				player.addShieldCharges(charges);
			}
		}
	}
	// --- Enhance management for AddictionFunction type ---
	public void AddEnhanceToPlayer(int playerIndex, BaseEnhanceFunction enhance, bool applyEffect = true)
	{
		if (enhance == null) return;
		if (!PlayerEnhances.ContainsKey(playerIndex)) PlayerEnhances[playerIndex] = new Array<BaseEnhanceFunction>();
		PlayerEnhances[playerIndex].Add(enhance);
		if (applyEffect) enhance.ApplyEffect();
	}
	public void RemoveEnhanceFromPlayer(int playerIndex, BaseEnhanceFunction enhance)
	{
		if (enhance == null) return;
		if (!PlayerEnhances.ContainsKey(playerIndex)) return;
		PlayerEnhances[playerIndex].Remove(enhance);
	}
	public Array<BaseEnhanceFunction> GetPlayerEnhances(int playerIndex)
	{
		if (!PlayerEnhances.ContainsKey(playerIndex)) return new Array<BaseEnhanceFunction>();
		return PlayerEnhances[playerIndex];
	}
	public bool PlayerHasEnhance(int playerIndex, BaseEnhanceFunction enhance)
	{
		if (enhance == null) return false;
		return PlayerEnhances.ContainsKey(playerIndex) && PlayerEnhances[playerIndex].Contains(enhance);
	}

}

using Godot;
using Godot.Collections;
using System;
using GodotResourceGroups;


public partial class EnemySpawnerManager : Singleton<EnemySpawnerManager>
{
	[Export] public int CheckRadius = 2;
	[Export] public TileMapLayer MapLayer;
	[Export] public float MinRadius = 800f;
	[Export] public float MaxRadius = 1200f;
	[Export] public int MaxEnemyCount = 100;
	[Export] public Node2D Player;
	[Export] public Timer SpawnTimer;
	[Export] public SpatialGridManager Grid;
	[Export] public float WaveDurationSeconds = 30.0f;
	[Export] public int DebugEnemiesPerWave = 3;
	public readonly Array<Node2D> ActiveEnemies = new Array<Node2D>();
	private readonly Array<EnemyDesignData> enemyDesigns = new Array<EnemyDesignData>();
	private readonly Array<EnemyDesignData> pendingWaveDesigns = new Array<EnemyDesignData>();
	public int CurrentWave { get; private set; } = 1;
	private float waveElapsedSeconds;
	private int lastLoggedSpawnWave;
	private int lastLoggedCapWave;
	private int lastLoggedPositionFailureWave;
	private bool victoryPending;
	private bool victorySent;
	private bool finalBossSpawned;
	private bool bossEncounterActive;
	private bool awaitingRouteSelection;
	private int bossDefeatsAtEncounterStart;
	private float nextStageThreatMultiplier = 1.0f;
	private int debugDesignOffset;
	public event Action<int> WaveChanged;
	public event Action<int> WaveCompleted;
	public event Action<int, EncounterRouteType, EncounterRouteType> RouteSelectionRequested;
	public override void _Ready()
	{
		base._Ready();
		RunShipUpgradeState.Reset();
		GlobalMessengerManager.Instance?.ResetRunStats();
		SetProcess(true);
		SetPhysicsProcess(true);
		loadEnemyDesigns();
		prepareWaveEncounter();
		applyRunDifficulty();
		if (SpawnTimer != null && IsInstanceValid(SpawnTimer))
		{
			SpawnTimer.Timeout += SpawnEnemyRandomly;
			SpawnTimer.Start();
		}
		LogUtil.Success($"Enemy spawner ready: wave={CurrentWave}, duration={WaveDurationSeconds:0.0}s, designs={enemyDesigns.Count}");
	}

	private void applyRunDifficulty()
	{
		if (SpawnTimer == null || !IsInstanceValid(SpawnTimer))
		{
			LogUtil.Warning("Enemy spawner timer is unavailable; run difficulty was not applied.");
			return;
		}

		switch (RunConfiguration.DifficultyIndex)
		{
		case 0:
				MaxEnemyCount = 60;
				SpawnTimer.WaitTime = 2.2;
				break;
			case 2:
				MaxEnemyCount = 120;
				SpawnTimer.WaitTime = 1.1;
				break;
			default:
				MaxEnemyCount = 90;
				SpawnTimer.WaitTime = 1.6;
				break;
		}
	}
	public override void _PhysicsProcess(double delta)
	{
		if (ActiveEnemies.Count > 0)
		{
			Grid.UpdateGrid(ActiveEnemies);
		}
	}
	public override void _Process(double delta)
	{
		// Keep wave timing on the regular process loop; it continues to advance
		// even when physics ticks are throttled by the running platform.
		advanceWave(delta);
		if (GlobalMessengerManager.Instance != null &&
			(GlobalMessengerManager.Instance.DefeatedNormalCount > 0 || GlobalMessengerManager.Instance.DefeatedEliteCount > 0 || GlobalMessengerManager.Instance.DefeatedBossCount > 0))
		{
			GlobalMessengerManager.Instance.AddEffectiveRunTime((float)delta);
		}

		trySendVictory();
	}
	private void advanceWave(double delta)
	{
		if (awaitingRouteSelection)
		{
			return;
		}

		if (bossEncounterActive)
		{
			if (!hasDefeatedFinalBoss())
			{
				return;
			}

			if (RunConfiguration.IsInfiniteMode)
			{
				bossEncounterActive = false;
				finalBossSpawned = false;
				CurrentWave = 1;
				nextStageThreatMultiplier = 1.0f;
				prepareWaveEncounter();
				SpawnTimer?.Start();
				WaveChanged?.Invoke(CurrentWave);
				LogUtil.Info("Infinite-cycle Boss cleared; starting the next 10-wave cycle.");
				return;
			}

			trySendVictory();
			return;
		}

		waveElapsedSeconds += (float)delta;
		if (waveElapsedSeconds < WaveDurationSeconds)
		{
			return;
		}

		waveElapsedSeconds = 0.0f;

		int maxWave = RunConfiguration.IsInfiniteMode ? 10 : Mathf.Max(1, RunConfiguration.EnemyWaveCount);
		if (CurrentWave >= maxWave)
		{
			notifyWaveCompleted(CurrentWave);
			beginBossEncounter();
			return;
		}

		int completedStage = getEncounterStage();
		if (completedStage < 4 && isStageFinalWave(completedStage))
		{
			notifyWaveCompleted(CurrentWave);
			requestRouteSelection(completedStage);
			return;
		}

		notifyWaveCompleted(CurrentWave);
		CurrentWave++;
		prepareWaveEncounter();
		LogUtil.Info($"Wave cleared: advancing to {CurrentWave}.");
		WaveChanged?.Invoke(CurrentWave);
	}
	public void SpawnEnemyRandomly()
	{
		CleanUpDeadEnemies();
		if (awaitingRouteSelection || victorySent)
		{
			SpawnTimer?.Stop();
			return;
		}
		if (ActiveEnemies.Count >= MaxEnemyCount)
		{
			if (lastLoggedCapWave != CurrentWave)
			{
				lastLoggedCapWave = CurrentWave;
				LogUtil.Warning($"Enemy spawn skipped: active cap reached ({MaxEnemyCount}) at wave {CurrentWave}");
			}
			return;
		}
		if (pendingWaveDesigns.Count == 0)
		{
			if (bossEncounterActive)
			{
				addNextEncounterDesign("NRM", 4);
			}
			else
			{
				addNextEncounterDesign("NRM", getEncounterStage());
			}
		}

		EnemyDesignData design = dequeueWaveDesign();
		if (design == null)
		{
			return;
		}

		// 限制尝试次数，防止在死循环中卡死（比如地图全铺满了）
		int maxAttempts = 5;
		Vector2 spawnPos = Vector2.Zero;
		bool isValidPos = false;

		for (int i = 0; i < maxAttempts; i++)
		{
			float angle = GD.Randf() * Mathf.Tau;
			float distance = (float)GD.RandRange(MinRadius, MaxRadius);
			Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
			spawnPos = Player.GlobalPosition + offset;

			if (IsPositionEmpty(spawnPos))
			{
				isValidPos = true;
				break;
			}
		}


		if (isValidPos)
		{
			ObjectPoolManager poolManager = ObjectPoolManager.Instance;
			var enemy = design.EnemyScene != null && IsInstanceValid(design.EnemyScene)
				? poolManager?.Create(PoolType.Enemy, design.EnemyScene, spawnPos)
				: poolManager?.Spawn(PoolType.Enemy, spawnPos);
			if (enemy == null || !IsInstanceValid(enemy))
			{
				return;
			}

			// LogUtil.Info($"SpawnEnemyRandomly ... design: {design}");
			if (enemy is EnemyController enemyController)
			{
				enemyController.ApplyDesignData(design);
				if (victoryPending && design.TierId == "BOS")
				{
					finalBossSpawned = true;
				}

				if (lastLoggedSpawnWave != CurrentWave)
				{
					lastLoggedSpawnWave = CurrentWave;
					LogUtil.Info($"Wave {CurrentWave} encounter started with {design.EnemyId} ({design.TierId})");
				}
			}

			if (!ActiveEnemies.Contains(enemy))
			{
				ActiveEnemies.Add(enemy);
			}
		}
		else
		{
			if (lastLoggedPositionFailureWave != CurrentWave)
			{
				lastLoggedPositionFailureWave = CurrentWave;
				LogUtil.Warning($"Enemy spawn position unavailable after 5 attempts at wave {CurrentWave}");
			}
		}
	}
	/// <summary>
	/// Advances one encounter for the D-key debug shortcut. Existing enemies are returned
	/// to the pool so each press exposes the next wave's enemy designs without waiting.
	/// Press D once more after the final debug encounter to verify the victory settlement.
	/// </summary>
	public void DebugAdvanceWave()
	{
		if (victorySent)
		{
			LogUtil.Info("Debug wave advance skipped: victory has already been sent.");
			return;
		}

		int maxWave = RunConfiguration.IsInfiniteMode ? int.MaxValue : Mathf.Max(1, RunConfiguration.EnemyWaveCount);
		if (!RunConfiguration.IsInfiniteMode && CurrentWave >= maxWave)
		{
			if (!hasDefeatedFinalBoss())
			{
				LogUtil.Warning("Debug victory check requires defeating the spawned final Boss first.");
				return;
			}

			clearActiveEnemiesForDebug();
			trySendVictory();
			return;
		}

		clearActiveEnemiesForDebug();

		CurrentWave++;
		WaveCompleted?.Invoke(CurrentWave - 1);
		CardBenefitRuntime.OnWaveCompleted(CurrentWave - 1);
		waveElapsedSeconds = 0.0f;
		bool isFinalWave = !RunConfiguration.IsInfiniteMode && CurrentWave >= maxWave;
		if (isFinalWave)
		{
			victoryPending = true;
			SpawnTimer?.Stop();
		}

		spawnDebugEncounter(isFinalWave);
		WaveChanged?.Invoke(CurrentWave);
		LogUtil.Info($"Debug wave advanced: current={CurrentWave}, final={isFinalWave}, active={ActiveEnemies.Count}");
	}
	private void loadEnemyDesigns()
	{
		enemyDesigns.Clear();
		Resource groupResource = GD.Load<Resource>(Assets.Enemy_Design_Data);
		if (groupResource == null)
		{
			LogUtil.Warning($"Enemy design group not found: {Assets.Enemy_Design_Data}");
			return;
		}

		ResourceGroup.Of(groupResource).LoadAllInto(enemyDesigns);
		if (enemyDesigns.Count == 0)
		{
			LogUtil.Warning("Enemy design group is empty; using scene defaults.");
		}
	}
	private void prepareWaveEncounter()
	{
		pendingWaveDesigns.Clear();
		int stage = getEncounterStage();
		addNextEncounterDesign("NRM", stage);

		if (isEliteWave() || isCrisisWave() || isGateWave())
		{
			addNextEncounterDesign("ELT", stage);
		}

		if (pendingWaveDesigns.Count == 0)
		{
			LogUtil.Error($"Wave {CurrentWave} could not be prepared because it has no eligible enemy designs.");
		}
	}

	private void beginBossEncounter()
	{
		bossEncounterActive = true;
		victoryPending = !RunConfiguration.IsInfiniteMode;
		bossDefeatsAtEncounterStart = GlobalMessengerManager.Instance?.DefeatedBossCount ?? 0;
		pendingWaveDesigns.Clear();
		int stage = 4;
		addNextEncounterDesign("BOS", stage);
		SpawnTimer?.Start();
		LogUtil.Info($"Boss encounter prepared after clearing wave {CurrentWave}.");
	}

	private void requestRouteSelection(int completedStage)
	{
		awaitingRouteSelection = true;
		SpawnTimer?.Stop();
		EncounterRouteType first = RunConfiguration.DifficultyIndex == 0 ? EncounterRouteType.SteadySupply : EncounterRouteType.ExpansionSignal;
		EncounterRouteType second = RunConfiguration.DifficultyIndex == 2 ? EncounterRouteType.HighRiskHunt : EncounterRouteType.TacticalAdjustment;
		LogUtil.Info($"Route selection requested after stage {completedStage}, wave {CurrentWave}.");
		RouteSelectionRequested?.Invoke(completedStage, first, second);
	}

	public void SelectRoute(EncounterRouteType route)
	{
		if (!awaitingRouteSelection)
		{
			return;
		}

		nextStageThreatMultiplier = route == EncounterRouteType.SteadySupply ? 0.90f : route == EncounterRouteType.HighRiskHunt ? 1.15f : 1.0f;
		if (route == EncounterRouteType.SteadySupply)
		{
			PlayerManager.Instance?.addFleetShieldCharges(1);
		}
		awaitingRouteSelection = false;
		CurrentWave++;
		waveElapsedSeconds = 0.0f;
		prepareWaveEncounter();
		SpawnTimer?.Start();
		WaveChanged?.Invoke(CurrentWave);
		LogUtil.Info($"Route selected: {route}; next wave={CurrentWave}, threat={nextStageThreatMultiplier:0.00}.");
	}

	private EnemyDesignData dequeueWaveDesign()
	{
		if (pendingWaveDesigns.Count == 0)
		{
			return null;
		}

		EnemyDesignData design = pendingWaveDesigns[0];
		pendingWaveDesigns.RemoveAt(0);
		return design;
	}

	private void addNextEncounterDesign(StringName tierId, int stage)
	{
		for (int offset = 0; offset < enemyDesigns.Count; offset++)
		{
			int index = (debugDesignOffset + offset) % enemyDesigns.Count;
			EnemyDesignData design = enemyDesigns[index];
			if (!isEncounterDesignEligible(design, tierId, stage))
			{
				continue;
			}

			debugDesignOffset = (index + 1) % enemyDesigns.Count;
			pendingWaveDesigns.Add(design);
			return;
		}

		LogUtil.Warning($"Wave {CurrentWave} has no eligible {tierId} enemy design.");
	}

	private int getEncounterStage()
	{
		if (RunConfiguration.IsInfiniteMode)
		{
			return CurrentWave <= 2 ? 1 : CurrentWave <= 4 ? 2 : CurrentWave <= 7 ? 3 : 4;
		}

		for (int stage = 1; stage <= 4; stage++)
		{
			if (CurrentWave <= getStageEndWave(stage)) return stage;
		}
		return 4;
	}

	private int getStageEndWave(int stage)
	{
		if (RunConfiguration.IsInfiniteMode)
		{
			return stage switch { 1 => 2, 2 => 4, 3 => 7, _ => 10 };
		}

		int total = Mathf.Max(4, RunConfiguration.EnemyWaveCount);
		int first;
		int second;
		int third;
		switch (total)
		{
			case 5: first = 1; second = 1; third = 1; break;
			case 8: first = 2; second = 2; third = 2; break;
			case 10: first = 2; second = 2; third = 3; break;
			case 15: first = 3; second = 4; third = 4; break;
			case 20: first = 4; second = 5; third = 5; break;
			case 30: first = 6; second = 7; third = 8; break;
			default:
				first = Mathf.Max(1, Mathf.FloorToInt(total * 0.20f));
				second = Mathf.Max(1, Mathf.FloorToInt(total * 0.25f));
				third = Mathf.Max(1, Mathf.FloorToInt(total * 0.25f));
				while (first + second + third >= total)
				{
					if (third > 1) third--; else if (second > 1) second--; else first--;
				}
				break;
		}
		return stage switch { 1 => first, 2 => first + second, 3 => first + second + third, _ => total };
	}

	private bool isEliteWave()
	{
		return getEncounterStage() == 2 && isStageFinalWave(2);
	}

	private bool isCrisisWave()
	{
		int stage = getEncounterStage();
		if (RunConfiguration.IsInfiniteMode) return CurrentWave % 10 == 7 || CurrentWave % 10 == 9;
		return (stage == 3 && isStageFinalWave(3)) || (stage == 4 && CurrentWave == getStageEndWave(4) - 1 && RunConfiguration.EnemyWaveCount >= 8);
	}

	private bool isGateWave() => getEncounterStage() == 4 && isStageFinalWave(4);
	private bool isStageFinalWave(int stage) => CurrentWave == getStageEndWave(stage);

	private bool isEncounterDesignEligible(EnemyDesignData design, StringName tierId, int stage)
	{
		if (design == null || design.TierId != tierId)
		{
			return false;
		}

		if (tierId != "ELT")
		{
			return true;
		}

		switch (design.RoleId.ToString())
		{
			case "RAM":
				return stage >= 1;
			case "SUM":
				return stage >= 2;
			case "JAM":
				return stage >= 2 && CurrentWave > 4;
			case "ZON":
				return stage >= 3;
			default:
				return false;
		}
	}
	private void spawnDebugEncounter(bool includeBoss)
	{
		int targetCount = Mathf.Clamp(DebugEnemiesPerWave, 1, 10);
		for (int i = 0; i < targetCount; i++)
		{
			EnemyDesignData design = selectDebugEnemyDesign(includeBoss);
			if (design == null)
			{
				LogUtil.Warning("Debug encounter has no eligible enemy design.");
				return;
			}

			spawnEnemyWithDesign(design);
		}
	}

	private EnemyDesignData selectDebugEnemyDesign(bool includeBoss)
	{
		for (int offset = 0; offset < enemyDesigns.Count; offset++)
		{
			int index = (debugDesignOffset + offset) % enemyDesigns.Count;
			EnemyDesignData design = enemyDesigns[index];
			if (design == null || design.TierId == "SUM" || (!includeBoss && design.TierId == "BOS"))
			{
				continue;
			}

			debugDesignOffset = (index + 1) % enemyDesigns.Count;
			return design;
		}

		return null;
	}

	private void spawnEnemyWithDesign(EnemyDesignData design)
	{
		if (Player == null || !IsInstanceValid(Player) || ActiveEnemies.Count >= MaxEnemyCount)
		{
			return;
		}

		Vector2 spawnPosition = Player.GlobalPosition + new Vector2(MinRadius, 0.0f).Rotated(GD.Randf() * Mathf.Tau);
		ObjectPoolManager poolManager = ObjectPoolManager.Instance;
		Node2D enemy = design.EnemyScene != null && IsInstanceValid(design.EnemyScene)
			? poolManager?.Create(PoolType.Enemy, design.EnemyScene, spawnPosition)
			: poolManager?.Spawn(PoolType.Enemy, spawnPosition);
		if (enemy is not EnemyController enemyController)
		{
			return;
		}

		enemyController.ApplyDesignData(design);
		if (victoryPending && design.TierId == "BOS")
		{
			finalBossSpawned = true;
		}

		ActiveEnemies.Add(enemy);
	}
	private void trySendVictory()
	{
		if (victorySent || RunConfiguration.IsInfiniteMode || !victoryPending)
		{
			return;
		}

		int finalWave = Mathf.Max(1, RunConfiguration.EnemyWaveCount);
		if (CurrentWave < finalWave || !finalBossSpawned || !hasDefeatedFinalBoss())
		{
			return;
		}

		victorySent = true;
		SpawnTimer?.Stop();
		LogUtil.Success($"Run victory: all {finalWave} waves cleared and final Boss defeated.");
		GlobalMessengerManager.Instance?.SendVictory();
	}

	private void notifyWaveCompleted(int wave)
	{
		WaveCompleted?.Invoke(wave);
		CardBenefitRuntime.OnWaveCompleted(wave);
	}

	private bool hasDefeatedFinalBoss()
	{
		return GlobalMessengerManager.Instance != null &&
			GlobalMessengerManager.Instance.DefeatedBossCount > bossDefeatsAtEncounterStart;
	}
	private bool isDesignEligible(EnemyDesignData design, bool eliteWave)
	{
		if (design == null || design.TierId == "BOS")
		{
			return false;
		}

		if (design.TierId == "ELT")
		{
			return eliteWave;
		}

		return !eliteWave || design.TierId == "NRM";
	}
	private bool IsPositionEmpty(Vector2 globalPos)
	{
		Vector2I mapPos = MapLayer.LocalToMap(MapLayer.ToLocal(globalPos));
		for (int x = -CheckRadius; x <= CheckRadius; x++)
		{
			for (int y = -CheckRadius; y <= CheckRadius; y++)
			{
				Vector2I neighborPos = mapPos + new Vector2I(x, y);

				if (MapLayer.GetCellSourceId(neighborPos) != -1)
				{
					return false;
				}
			}
		}

		return true;
	}
	private void CleanUpDeadEnemies()
	{
		for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
		{
			if (!IsInstanceValid(ActiveEnemies[i]))
			{
				ActiveEnemies.RemoveAt(i);
			}
		}
	}
	private void clearActiveEnemiesForDebug()
	{
		for (int i = ActiveEnemies.Count - 1; i >= 0; i--)
		{
			Node2D enemy = ActiveEnemies[i];
			ActiveEnemies.RemoveAt(i);
			if (enemy != null && IsInstanceValid(enemy))
			{
				ObjectPoolManager.Instance?.Release(PoolType.Enemy, enemy);
			}
		}
	}
	public Node2D GetClosestEnemyTo(Vector2 pos, float range)
	{
		return Grid.GetBestEnemy(pos, range, ActiveEnemies);
	}
	/// <summary>
	/// 当敌人死亡或被回收时，必须调用此方法
	/// </summary>
	public void RemoveEnemy(Node2D enemy)
	{
		if (ActiveEnemies.Remove(enemy))
		{
			ObjectPoolManager.Instance.Release(PoolType.Enemy, enemy);
		}
	}
}

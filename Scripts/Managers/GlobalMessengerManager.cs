using Godot;
using System;
public partial class GlobalMessengerManager : Singleton<GlobalMessengerManager>
{


	// ---  全局数据 ---
	public int Score { get; private set; } = 0;
	public int RunExperience { get; private set; }
	public int DefeatedNormalCount { get; private set; }
	public int DefeatedEliteCount { get; private set; }
	public int DefeatedBossCount { get; private set; }
	public float EffectiveRunSeconds { get; private set; }
	public string CurrentLevelName { get; set; } = "Main Menu";

	// --- 全局信号 (用于跨场景通讯) ---
	[Signal] public delegate void ScoreChangedEventHandler(int newScore);
	[Signal] public delegate void GamePausedEventHandler(bool isPaused);
	[Signal] public delegate void OnNotificationEventHandler(string message);
	[Signal] public delegate void OnShowTipsInfoEventHandler(TipsInfoMessages message);
	[Signal] public delegate void OnGameOverEventHandler();
	[Signal] public delegate void OnVictoryEventHandler();
	[Signal] public delegate void OnPlayerLossEventHandler(int playerIndex);
	[Signal] public delegate void OnShowEnhanceInfoEventHandler(int levelUpCount);
	[Signal] public delegate void OnExecuteEnhanceFunctionEventHandler(EnhanceFunctionType enhanceFunction);
	[Signal] public delegate void OnGetExpPointsEventHandler(int exp);
	[Signal] public delegate void OnUpdateAttackRangeEventHandler(float range);

	// --- 显示tips ---
	public void SendTips(TipsInfoMessages message)
	{
		EmitSignal(SignalName.OnShowTipsInfo, message);
	}

	// --- 发送通知 ---
	public void SendNotification(string message)
	{
		EmitSignal(SignalName.OnNotification, message);
	}
	// --- 工具方法 ---
	public void SendGameOver()
	{
		LogUtil.Info("Game over signal emitted: fleet has no active ships.");
		EmitSignal(SignalName.OnGameOver);
	}
	public void SendVictory()
	{
		LogUtil.Success("Victory signal emitted.");
		EmitSignal(SignalName.OnVictory);
	}

	public void ResetRunStats()
	{
		RunExperience = 0;
		DefeatedNormalCount = 0;
		DefeatedEliteCount = 0;
		DefeatedBossCount = 0;
		EffectiveRunSeconds = 0.0f;
	}

	public void RegisterEnemyDefeated(StringName tierId, int experience)
	{
		RunExperience += Mathf.Max(0, experience);
		switch (tierId.ToString())
		{
			case "BOS": DefeatedBossCount++; break;
			case "ELT": DefeatedEliteCount++; break;
			default: DefeatedNormalCount++; break;
		}
	}

	public void AddEffectiveRunTime(float seconds)
	{
		EffectiveRunSeconds += Mathf.Max(0.0f, seconds);
	}

	public void SendPlayerLoss(int playerIndex)
	{
		EmitSignal(SignalName.OnPlayerLoss, playerIndex);
	}

	public void SendShowEnhanceInfo(int levelUpCount)
	{
		EmitSignal(SignalName.OnShowEnhanceInfo, levelUpCount);
	}

	public void SendExecuteEnhanceFunction(EnhanceFunctionType enhanceFunction)
	{
		EmitSignal(SignalName.OnExecuteEnhanceFunction, (int)enhanceFunction);
	}

	public void SendGetExpPoints(int exp)
	{
		EmitSignal(SignalName.OnGetExpPoints, exp);
	}

	public void SendUpdateAttackRange(int range)
	{
		EmitSignal(SignalName.OnUpdateAttackRange, range);
	}

	public void AddScore(int amount)
	{
		Score += amount;
		GD.Print($"全局单例：分数增加，当前总分: {Score}");

		// 发射信号，通知所有监听者（如 UI）
		EmitSignal(SignalName.ScoreChanged, Score);
	}

	public void ResetGame()
	{
		Score = 0;
		GetTree().ChangeSceneToFile(Assets.MainGameScene);
	}
}

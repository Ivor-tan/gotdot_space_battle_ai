using Godot;
using System;

public partial class GameOverInfo : MarginContainer
{

	[Export] Button Restart;
	[Export] Button MainMenu;
	private Label resultLabel;
	private bool _resultSettled;

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		if (GlobalMessengerManager.Instance != null && IsInstanceValid(GlobalMessengerManager.Instance))
		{
			GlobalMessengerManager.Instance.Connect(
				GlobalMessengerManager.SignalName.OnGameOver,
				Callable.From(OnGameOver));
			GlobalMessengerManager.Instance.Connect(
				GlobalMessengerManager.SignalName.OnVictory,
				Callable.From(OnVictory));
		}

		resultLabel = GetNodeOrNull<Label>("PanelContainer/CenterContainer/Label");
		if (Restart != null && IsInstanceValid(Restart))
		{
			Restart.Pressed += OnRestart;
		}

		if (MainMenu != null && IsInstanceValid(MainMenu))
		{
			MainMenu.Pressed += OnBackMainMenu;
		}
	}

	public override void _ExitTree()
	{
		if (GlobalMessengerManager.Instance == null || !IsInstanceValid(GlobalMessengerManager.Instance))
		{
			return;
		}

		Callable gameOverCallable = Callable.From(OnGameOver);
		if (GlobalMessengerManager.Instance.IsConnected(GlobalMessengerManager.SignalName.OnGameOver, gameOverCallable))
		{
			GlobalMessengerManager.Instance.Disconnect(GlobalMessengerManager.SignalName.OnGameOver, gameOverCallable);
		}

		Callable victoryCallable = Callable.From(OnVictory);
		if (GlobalMessengerManager.Instance.IsConnected(GlobalMessengerManager.SignalName.OnVictory, victoryCallable))
		{
			GlobalMessengerManager.Instance.Disconnect(GlobalMessengerManager.SignalName.OnVictory, victoryCallable);
		}
	}

	private void OnBackMainMenu()
	{
		GetTree().Paused = false;
		_ = SceneManager.Instance.ChangeScene(Assets.MainScene);
	}


	private void OnRestart()
	{
		GetTree().Paused = false;
		_ = SceneManager.Instance.ChangeScene(Assets.MainGameScene);
	}

	private void OnGameOver()
	{
		showResult(false);
	}

	private void OnVictory()
	{
		showResult(true);
	}

	private void showResult(bool victory)
	{
		if (_resultSettled)
		{
			return;
		}

		_resultSettled = true;
		GlobalMessengerManager stats = GlobalMessengerManager.Instance;
		if (resultLabel != null && stats != null)
		{
			recordInitialShipProgress(stats, victory);
			int metaExperience = calculateMetaExperience(stats, victory);
			int totalMetaExperience = MetaProgressStore.AddExperience(metaExperience);
			resultLabel.Text = TranslationServer.Translate(victory ? "UI_RESULTS_VICTORY" : "UI_RESULTS_DEFEAT");
			resultLabel.Text += "\n" + string.Format(TranslationServer.Translate("UI_RESULTS_WAVE"), EnemySpawnerManager.Instance?.CurrentWave ?? 1);
			resultLabel.Text += "\n" + string.Format(TranslationServer.Translate("UI_RESULTS_KILLS"), stats.DefeatedNormalCount, stats.DefeatedEliteCount, stats.DefeatedBossCount);
			resultLabel.Text += "\n" + string.Format(TranslationServer.Translate("UI_RESULTS_RUN_XP"), stats.RunExperience, metaExperience);
			resultLabel.Text += "\n" + string.Format(TranslationServer.Translate("UI_RESULTS_TOTAL_META_XP"), totalMetaExperience);
		}
		Visible = true;
		GetTree().Paused = true;
	}

	private static int calculateMetaExperience(GlobalMessengerManager stats, bool victory)
	{
		int minutes = Mathf.FloorToInt(stats.EffectiveRunSeconds / 60.0f);
		int value = Mathf.Min(minutes * 3, 45) + stats.DefeatedNormalCount / 4 + stats.DefeatedEliteCount * 8 + stats.DefeatedBossCount * 20;
		return Mathf.Max(0, value + (victory ? 12 : 0));
	}

	private static void recordInitialShipProgress(GlobalMessengerManager stats, bool victory)
	{
		PlayerShipData initialShip = RunConfiguration.SelectedInitialShip;
		if (initialShip == null || !IsInstanceValid(initialShip))
		{
			return;
		}

		// Only the selected flagship is guaranteed to have participated throughout
		// the encounter; recruited ships have independent in-run participation.
		MetaProgressStore.RecordShipEncounterProgress(
			initialShip.ShipId,
			stats.DefeatedEliteCount,
			victory && stats.DefeatedBossCount > 0 ? 1 : 0);
	}

}

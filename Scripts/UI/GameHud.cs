using Godot;

public partial class GameHud : Control
{
    [Export] public Label WaveLabel;
    [Export] public Label TimeLabel;
    [Export] public Label DangerLabel;
    [Export] public Button PauseButton;
    [Export] public Label FlagshipLabel;
    [Export] public Label FleetLabel;
    [Export] public Label EnemyLabel;
    [Export] public Label ShieldLabel;
    [Export] public Label LevelLabel;
    [Export] public PlayerExpProgressBar ExpProgressBar;
    [Export] public PanelContainer PausePanel;
    [Export] public Label PauseTitleLabel;
    [Export] public RichTextLabel PauseContentLabel;
    [Export] public Button ResumeButton;
    [Export] public Button FleetStatusButton;
    [Export] public Button EditFleetButton;
    [Export] public Button SettingsButton;
    [Export] public Button MainMenuButton;
    [Export] public Button PauseBackButton;
    [Export] public ConfirmationDialog MainMenuConfirmDialog;
    [Export] public Control UpgradePanel;
    [Export] public FleetEditor FleetEditor;
    [Export] public PanelContainer RouteSelectionPanel;
    [Export] public Label RouteStageLabel;
    [Export] public Button FirstRouteButton;
    [Export] public Button SecondRouteButton;

    private double _elapsedSeconds;
    private double _refreshTimer;
    private Label _levelLabel;
    private bool _routeSelectionPausedGame;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _levelLabel = LevelLabel ?? GetNodeOrNull<Label>("LevelLabel");

        PauseButton.Pressed += togglePauseMenu;
        ResumeButton.Pressed += resumeGame;
        FleetStatusButton.Pressed += showFleetStatus;
        EditFleetButton.Pressed += openFleetEditor;
        SettingsButton.Pressed += showSettings;
        MainMenuButton.Pressed += requestBackToMainMenu;
        PauseBackButton.Pressed += showPauseHome;
        MainMenuConfirmDialog.Confirmed += backToMainMenu;
        FirstRouteButton.Pressed += onFirstRoutePressed;
        SecondRouteButton.Pressed += onSecondRoutePressed;

        if (GameConfigManager.Instance != null)
        {
            GameConfigManager.Instance.LanguageChanged += onLanguageChanged;
        }

        if (EnemySpawnerManager.Instance != null)
        {
            EnemySpawnerManager.Instance.WaveChanged += onWaveChanged;
            EnemySpawnerManager.Instance.RouteSelectionRequested += showRouteSelection;
        }

        RouteSelectionPanel.Hide();
        showPauseHome();
        PausePanel.Hide();
        refreshHud();
    }

    public override void _ExitTree()
    {
        PauseButton.Pressed -= togglePauseMenu;
        ResumeButton.Pressed -= resumeGame;
        FleetStatusButton.Pressed -= showFleetStatus;
        EditFleetButton.Pressed -= openFleetEditor;
        SettingsButton.Pressed -= showSettings;
        MainMenuButton.Pressed -= requestBackToMainMenu;
        PauseBackButton.Pressed -= showPauseHome;
        MainMenuConfirmDialog.Confirmed -= backToMainMenu;
        FirstRouteButton.Pressed -= onFirstRoutePressed;
        SecondRouteButton.Pressed -= onSecondRoutePressed;

        if (GameConfigManager.Instance != null)
        {
            GameConfigManager.Instance.LanguageChanged -= onLanguageChanged;
        }

        if (EnemySpawnerManager.Instance != null)
        {
            EnemySpawnerManager.Instance.WaveChanged -= onWaveChanged;
            EnemySpawnerManager.Instance.RouteSelectionRequested -= showRouteSelection;
        }
    }

    public override void _Process(double delta)
    {
        if (!GetTree().Paused)
        {
            _elapsedSeconds += delta;
        }

        _refreshTimer += delta;
        if (_refreshTimer >= 0.25)
        {
            _refreshTimer = 0.0;
            refreshHud();
        }

        if (Input.IsActionJustPressed("ui_cancel"))
        {
            if (RouteSelectionPanel != null && IsInstanceValid(RouteSelectionPanel) && RouteSelectionPanel.Visible)
            {
                return;
            }

            if (FleetEditor != null && IsInstanceValid(FleetEditor) && FleetEditor.Visible)
            {
                FleetEditor.closeEditor();
                return;
            }

            togglePauseMenu();
        }
    }

    private void togglePauseMenu()
    {
        if (UpgradePanel != null && IsInstanceValid(UpgradePanel) && UpgradePanel.Visible)
        {
            return;
        }

        if (PausePanel.Visible)
        {
            resumeGame();
            return;
        }

        GetTree().Paused = true;
        PausePanel.Show();
        showPauseHome();
    }

    private void resumeGame()
    {
        PausePanel.Hide();
        GetTree().Paused = false;
    }

    private void showPauseHome()
    {
        PauseTitleLabel.Text = translate("UI_GAME_PAUSE_TITLE");
        PauseContentLabel.Text = translate("UI_GAME_PAUSE_DESCRIPTION");
        ResumeButton.Show();
        FleetStatusButton.Show();
        EditFleetButton.Show();
        SettingsButton.Show();
        MainMenuButton.Show();
        PauseBackButton.Hide();
    }

    private void showFleetStatus()
    {
        PlayerManager playerManager = PlayerManager.Instance;
        int activeShipCount = playerManager?.CurrentPlayerCount ?? 0;
        int shieldLayers = playerManager?.FleetShieldCharges ?? 0;

        PauseTitleLabel.Text = translate("UI_GAME_FLEET_STATUS_TITLE");
        PauseContentLabel.Text = buildFleetStatusText(playerManager, activeShipCount, shieldLayers);
        showPauseSubpage();
    }

    private void showSettings()
    {
        PauseTitleLabel.Text = translate("UI_GAME_SETTINGS_TITLE");
        PauseContentLabel.Text = translate("UI_GAME_SETTINGS_DESCRIPTION");
        showPauseSubpage();
    }

    private void showPauseSubpage()
    {
        ResumeButton.Hide();
        FleetStatusButton.Hide();
        EditFleetButton.Hide();
        SettingsButton.Hide();
        MainMenuButton.Hide();
        PauseBackButton.Show();
    }

    private void requestBackToMainMenu()
    {
        MainMenuConfirmDialog.PopupCentered();
    }

    private void openFleetEditor()
    {
        if (FleetEditor == null || !IsInstanceValid(FleetEditor))
        {
            LogUtil.Warning("Unable to open fleet editor from pause menu: editor is unavailable.");
            return;
        }

        PausePanel.Hide();
        if (!FleetEditor.openEditor(showPauseMenuAfterFleetEditing))
        {
            PausePanel.Show();
            return;
        }

        LogUtil.Info("Opened fleet editor from pause menu while the game remains paused.");
    }

    private void showPauseMenuAfterFleetEditing()
    {
        PausePanel.Show();
        showPauseHome();
        LogUtil.Info("Returned to pause menu after fleet editing.");
    }

    private void createRoutePanel()
    {
#if false
        if (_routePanel != null && IsInstanceValid(_routePanel))
        {
            return;
        }

        _routePanel = new PanelContainer
        {
            Name = "RouteSelectionPanel",
            AnchorsPreset = (int)LayoutPreset.Center,
            OffsetLeft = -310.0f,
            OffsetTop = -190.0f,
            OffsetRight = 310.0f,
            OffsetBottom = 190.0f,
            ProcessMode = ProcessModeEnum.Always,
            MouseFilter = MouseFilterEnum.Stop,
            ZIndex = 100
        };
        VBoxContainer content = new();
        _routePanel.AddChild(content);
        content.AddChild(new Label
        {
            Text = "选择下一条航线",
            HorizontalAlignment = HorizontalAlignment.Center
        });
        content.AddChild(new Label
        {
            Name = "StageLabel",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        _firstRouteButton = createRouteButton("FirstRoute", EncounterRouteType.SteadySupply);
        _secondRouteButton = createRouteButton("SecondRoute", EncounterRouteType.ExpansionSignal);
        content.AddChild(_firstRouteButton);
        content.AddChild(_secondRouteButton);
        AddChild(_routePanel);
        _routePanel.Hide();
#endif
    }

    private void showRouteSelection(int completedStage, EncounterRouteType first, EncounterRouteType second)
    {
        if (RouteSelectionPanel == null || !IsInstanceValid(RouteSelectionPanel) ||
            RouteStageLabel == null || !IsInstanceValid(RouteStageLabel) ||
            FirstRouteButton == null || !IsInstanceValid(FirstRouteButton) ||
            SecondRouteButton == null || !IsInstanceValid(SecondRouteButton))
        {
            LogUtil.Warning("Route selection UI scene nodes are unavailable.");
            return;
        }

        Label stageLabel = RouteStageLabel;
        stageLabel.Text = string.Format(translate("UI_ROUTE_STAGE_COMPLETE"), completedStage);
        configureRouteButton(FirstRouteButton, first);
        configureRouteButton(SecondRouteButton, second);
        RouteSelectionPanel.MoveToFront();
        RouteSelectionPanel.Show();
        _routeSelectionPausedGame = !GetTree().Paused;
        if (_routeSelectionPausedGame)
        {
            GetTree().Paused = true;
        }

        FirstRouteButton.GrabFocus();
        LogUtil.Info($"Route selection dialog opened: stage={completedStage}, first={first}, second={second}.");
    }

    private static void configureRouteButton(Button button, EncounterRouteType route)
    {
        button.SetMeta("route", (int)route);
        button.Text = route switch
        {
            EncounterRouteType.SteadySupply => "稳健补给\n下阶段威胁 -10%；立即获得 1 层共享护盾。",
            EncounterRouteType.ExpansionSignal => "扩编信号\n保持标准威胁；下次升级优先提供舰船招募。",
            EncounterRouteType.HighRiskHunt => "高风险猎杀\n下阶段威胁 +15%；提高高价值奖励机会。",
            _ => "战术调整\n保持标准威胁；优先提供编队与控制相关奖励。"
        };
    }

    private void selectRoute(Button button, EncounterRouteType fallbackRoute)
    {
        if (RouteSelectionPanel == null || !IsInstanceValid(RouteSelectionPanel))
        {
            return;
        }

        EncounterRouteType route = button != null && button.HasMeta("route")
            ? (EncounterRouteType)(int)button.GetMeta("route")
            : fallbackRoute;
        RouteSelectionPanel.Hide();
        LogUtil.Info($"Route selection dialog confirmed: route={route}.");
        EnemySpawnerManager.Instance?.SelectRoute(route);

        if (_routeSelectionPausedGame)
        {
            _routeSelectionPausedGame = false;
            GetTree().Paused = false;
        }
    }

    private void onFirstRoutePressed()
    {
        selectRoute(FirstRouteButton, EncounterRouteType.SteadySupply);
    }

    private void onSecondRoutePressed()
    {
        selectRoute(SecondRouteButton, EncounterRouteType.ExpansionSignal);
    }

    private void backToMainMenu()
    {
        GetTree().Paused = false;
        _ = SceneManager.Instance.ChangeScene(Assets.MainScene);
    }

    private void onLanguageChanged(string locale)
    {
        refreshHud();
        if (PausePanel.Visible)
        {
            showPauseHome();
        }
    }

    private void refreshHud()
    {
        int elapsedWholeSeconds = Mathf.FloorToInt((float)_elapsedSeconds);
        int minutes = elapsedWholeSeconds / 60;
        int seconds = elapsedWholeSeconds % 60;
        int level = ExpProgressBar?.CurrentLevel ?? 1;
        PlayerManager playerManager = PlayerManager.Instance;
        EnemySpawnerManager enemySpawner = EnemySpawnerManager.Instance;
        int shieldLayers = playerManager?.FleetShieldCharges ?? 0;
        int activeShipCount = playerManager?.CurrentPlayerCount ?? 0;
        int maxShipCount = playerManager?.MaxPlayerCount ?? 0;
        int emptySlotCount = Mathf.Max(maxShipCount - activeShipCount, 0);
        int activeEnemyCount = enemySpawner?.ActiveEnemies.Count ?? 0;
        int maxEnemyCount = enemySpawner?.MaxEnemyCount ?? 0;

        WaveLabel.Text = getCurrentWaveText(enemySpawner?.CurrentWave ?? 1);
        TimeLabel.Text = string.Format(translate("UI_GAME_TIME"), $"{minutes:00}:{seconds:00}");
        DangerLabel.Text = getDangerText(activeEnemyCount, maxEnemyCount);
        FlagshipLabel.Text = string.Format(translate("UI_GAME_DEPLOYED_SHIPS"), activeShipCount);
        FleetLabel.Text = string.Format(translate("UI_GAME_EMPTY_SLOTS"), emptySlotCount, maxShipCount);
        EnemyLabel.Text = string.Format(translate("UI_GAME_ACTIVE_ENEMIES"), activeEnemyCount, maxEnemyCount);
        ShieldLabel.Text = string.Format(translate("UI_GAME_SHARED_SHIELD"), shieldLayers);
        if (_levelLabel != null && IsInstanceValid(_levelLabel))
        {
            _levelLabel.Text = string.Format(translate("UI_GAME_LEVEL"), level);
        }

        PauseButton.Text = translate("UI_GAME_PAUSE_BUTTON");
        ResumeButton.Text = translate("UI_GAME_PAUSE_RESUME");
        FleetStatusButton.Text = translate("UI_GAME_PAUSE_FLEET");
        EditFleetButton.Text = translate("UI_GAME_PAUSE_EDIT_FLEET");
        SettingsButton.Text = translate("UI_GAME_PAUSE_SETTINGS");
        MainMenuButton.Text = translate("UI_GAME_PAUSE_MAIN_MENU");
        PauseBackButton.Text = translate("UI_GAME_PAUSE_BACK");
        MainMenuConfirmDialog.Title = translate("UI_GAME_MAIN_MENU_CONFIRM_TITLE");
        MainMenuConfirmDialog.DialogText = translate("UI_GAME_MAIN_MENU_CONFIRM_DESCRIPTION");
        MainMenuConfirmDialog.OkButtonText = translate("UI_GAME_MAIN_MENU_CONFIRM_OK");
        MainMenuConfirmDialog.CancelButtonText = translate("UI_GAME_MAIN_MENU_CONFIRM_CANCEL");
    }

    private static string translate(StringName key)
    {
        return TranslationServer.Translate(key);
    }

    private void onWaveChanged(int currentWave)
    {
        if (WaveLabel == null || !IsInstanceValid(WaveLabel))
        {
            return;
        }

        WaveLabel.Text = getCurrentWaveText(currentWave);
        LogUtil.Info($"HUD wave display updated: {currentWave}");
    }

    private static string getCurrentWaveText(int currentWave)
    {
        int stage = getWaveStage(currentWave);
        return RunConfiguration.IsInfiniteMode
            ? $"循环 {Mathf.Max(1, (currentWave - 1) / 10 + 1)} · 阶段 {stage} · 波次 {((currentWave - 1) % 10) + 1:00} / ∞"
            : $"阶段 {stage} · 波次 {currentWave:00} / {RunConfiguration.EnemyWaveCount}";
    }

    private static int getWaveStage(int currentWave)
    {
        int totalWaves = Mathf.Max(4, RunConfiguration.EnemyWaveCount);
        float progress = Mathf.Clamp((float)(currentWave - 1) / Mathf.Max(1, totalWaves - 1), 0.0f, 1.0f);
        return Mathf.Clamp(Mathf.FloorToInt(progress * 4.0f) + 1, 1, 4);
    }

    private static string buildFleetStatusText(PlayerManager playerManager, int activeShipCount, int shieldLayers)
    {
        if (playerManager == null || !IsInstanceValid(playerManager))
        {
            return "舰队数据尚未初始化。";
        }

        System.Text.StringBuilder builder = new();
        int emptySlotCount = Mathf.Max(playerManager.MaxPlayerCount - activeShipCount, 0);
        builder.Append($"已部署：{activeShipCount} 艘\n可补员空格：{emptySlotCount}\n共享护盾：{shieldLayers}\n\n");
        builder.Append("在场舰船\n");
        foreach (PlayerController player in playerManager.CurrentPlayerDic.Values)
        {
            if (player == null || !IsInstanceValid(player))
            {
                continue;
            }

            PlayerShipData shipData = player.ShipData;
            string shipName = shipData != null && IsInstanceValid(shipData)
                ? shipData.getDisplayName()
                : "未命名舰船";
            builder.Append($"• {shipName}  格位 {player.PlayerIndex}\n");
        }

        return builder.ToString();
    }

    private static string getRunModeText()
    {
        return RunConfiguration.IsInfiniteMode
            ? "无限循环 · 循环 1 · 波次 01 / ∞"
            : $"阶段 1 · 波次 01 / {RunConfiguration.EnemyWaveCount}";
    }

    private static string getDangerText(int activeEnemyCount, int maxEnemyCount)
    {
        if (maxEnemyCount <= 0 || activeEnemyCount <= 0)
        {
            return "威胁：低";
        }

        float ratio = (float)activeEnemyCount / maxEnemyCount;
        if (ratio >= 0.75f)
        {
            return "威胁：极高";
        }

        if (ratio >= 0.4f)
        {
            return "威胁：升高";
        }

        return "威胁：低";
    }

    private static string getDifficultyName()
    {
        return RunConfiguration.DifficultyIndex switch
        {
            0 => "轻松",
            2 => "困难",
            _ => "标准"
        };
    }
}

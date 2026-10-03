using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GodotResourceGroups;

public partial class MainSceneUI : CanvasLayer
{
    [Export] public Button gameStart;
    [Export] public Button gameExit;
    [Export] public Button gameSettings;
    [Export] public Button gameContinue;
    [Export] public Button shipArchive;
    [Export] public Button codex;
    [Export] public Button metaUpgrade;
    [Export] public PanelContainer metaUpgradeWindow;
    [Export] public Label metaUpgradePoints;
    [Export] public Button metaUpgradeClose;
    [Export] public MetaUpgradeCanvas metaUpgradeCanvas;
    [Export] public MarginContainer settingsWindow;
    [Export] public MarginContainer shipArchiveWindow;
    [Export] public MarginContainer codexWindow;
    [Export] public VBoxContainer codexContents;
    [Export] public TabContainer codexTabs;
    [Export] public Button shipArchiveBack;
    [Export] public Button codexBack;
    [Export] public MarginContainer runSetupWindow;
    [Export] public VBoxContainer runSetupContents;
    [Export] public OptionButton initialShipSelector;
    [Export] public ScrollContainer initialShipScroll;
    [Export] public HBoxContainer initialShipCards;
    [Export] public OptionButton difficultySelector;
    [Export] public OptionButton waveSelector;
    [Export] public OptionButton waveModeSelector;
    [Export] public SpinBox customWaveInput;
    [Export] public Label waveModeDescription;
    [Export] public Label initialShipPreview;
    [Export] public Button runSetupBack;
    [Export] public Button runSetupConfirm;
    [Export] public ScrollContainer shipArchiveScroll;
    [Export] public GridContainer shipArchiveItems;
    [Export] public PackedScene shipArchiveItemScene;
    [Export] public GridContainer codexShipGrid;
    [Export] public GridContainer codexEnemyGrid;
    [Export] public GridContainer codexBondGrid;
    [Export] public GridContainer codexCardGrid;
    [Export] public PackedScene codexListItemScene;
    [Export] public CodexDetailsPopup codexDetailsPopup;

    private readonly Array<PlayerShipData> _initialShipOptions = new();
    private readonly Array<Button> _initialShipCardButtons = new();
    private readonly Array<PlayerShipData> _archiveShips = new();
    private readonly Array<PlayerShipData> _blueprintShips = new();
    private readonly Array<ShipDesignData> _shipDesigns = new();
    private readonly Array<EnemyDesignData> _enemyDesigns = new();
    private readonly Array<CardBenefitData> _cardBenefits = new();
    private readonly Array<BondDefinitionData> _bondDefinitions = new();
    private int _archiveLoadedCount;
    private int _codexLoadedCount;
    private int _enemyLoadedCount;
    private int _cardLoadedCount;
    private bool _cardBatchQueued;
    private bool _bondLoaded;
    private bool _archiveBatchQueued;
    private bool _codexBatchQueued;
    private const int ArchiveBatchSize = 4;
    private const int CodexBatchSize = 6;
    private const float CompactViewportWidth = 680.0f;
    private const float MediumViewportWidth = 960.0f;
    private ScrollContainer _codexScroll;
    private int _selectedInitialShipIndex;
    private TapTapComplianceDialog _complianceDialog;

    public override void _Ready()
    {
        // Keep primary menu navigation available even if optional Codex data has
        // an invalid or missing resource. The previous order deferred bindings
        // until after all data loads, leaving the new meta-upgrade entry inert.
        connectButtons();
        refreshContinueButton();
        populateRunSetupOptions();
        loadHomepageData();
        if (runSetupWindow != null && IsInstanceValid(runSetupWindow))
        {
            runSetupWindow.Resized += updateRunSetupLayout;
            CallDeferred(nameof(updateRunSetupLayout));
        }

        GetViewport().SizeChanged += updateMainMenuPopupLayout;
        if (GameConfigManager.Instance != null && IsInstanceValid(GameConfigManager.Instance))
        {
            GameConfigManager.Instance.LanguageChanged += onLanguageChanged;
        }

        if (TapTapComplianceManager.Instance != null && IsInstanceValid(TapTapComplianceManager.Instance))
        {
            TapTapComplianceManager.Instance.AuthenticationSucceeded += onComplianceAuthenticationSucceeded;
            TapTapComplianceManager.Instance.AuthenticationBlocked += onComplianceAuthenticationBlocked;
        }

        updateComplianceAccessControls();
        refreshCodexTabTitles();
        CallDeferred(nameof(updateMainMenuPopupLayout));
        CallDeferred(nameof(showLastComplianceBlockIfNeeded));
        SoundManager.Instance.PlayMusicFade("Main_BGM");
    }

    public override void _ExitTree()
    {
        disconnectButtons();
        if (runSetupWindow != null && IsInstanceValid(runSetupWindow))
        {
            runSetupWindow.Resized -= updateRunSetupLayout;
        }

        if (GetViewport() != null && IsInstanceValid(GetViewport()))
        {
            GetViewport().SizeChanged -= updateMainMenuPopupLayout;
        }

        if (GameConfigManager.Instance != null && IsInstanceValid(GameConfigManager.Instance))
        {
            GameConfigManager.Instance.LanguageChanged -= onLanguageChanged;
        }

        if (TapTapComplianceManager.Instance != null && IsInstanceValid(TapTapComplianceManager.Instance))
        {
            TapTapComplianceManager.Instance.AuthenticationSucceeded -= onComplianceAuthenticationSucceeded;
            TapTapComplianceManager.Instance.AuthenticationBlocked -= onComplianceAuthenticationBlocked;
        }

        disconnectComplianceDialog();
    }

    private void connectButtons()
    {
        connectButton(gameStart, onStart);
        connectButton(gameExit, onExit);
        connectButton(gameSettings, onGameSettings);
        connectButton(gameContinue, onGameContinue);
        connectButton(shipArchive, onShipArchive);
        connectButton(codex, onCodex);
        connectButton(metaUpgrade, onMetaUpgrade);
        connectButton(metaUpgradeClose, hideMetaUpgrade);
        connectButton(shipArchiveBack, hideSecondaryWindows);
        connectButton(codexBack, hideSecondaryWindows);
        connectButton(runSetupBack, hideRunSetup);
        connectButton(runSetupConfirm, startConfiguredRun);

        if (waveModeSelector != null && IsInstanceValid(waveModeSelector))
        {
            waveModeSelector.ItemSelected += onWaveModeSelected;
        }
    }

    private void disconnectButtons()
    {
        disconnectButton(gameStart, onStart);
        disconnectButton(gameExit, onExit);
        disconnectButton(gameSettings, onGameSettings);
        disconnectButton(gameContinue, onGameContinue);
        disconnectButton(shipArchive, onShipArchive);
        disconnectButton(codex, onCodex);
        disconnectButton(metaUpgrade, onMetaUpgrade);
        disconnectButton(metaUpgradeClose, hideMetaUpgrade);
        disconnectButton(shipArchiveBack, hideSecondaryWindows);
        disconnectButton(codexBack, hideSecondaryWindows);
        disconnectButton(runSetupBack, hideRunSetup);
        disconnectButton(runSetupConfirm, startConfiguredRun);

        if (waveModeSelector != null && IsInstanceValid(waveModeSelector))
        {
            waveModeSelector.ItemSelected -= onWaveModeSelected;
        }

        if (_codexScroll != null && IsInstanceValid(_codexScroll))
        {
            _codexScroll.ScrollEnded -= onCodexScrollEnded;
            _codexScroll.GetVScrollBar().ValueChanged -= onCodexScrollValueChanged;
        }

        if (shipArchiveScroll != null && IsInstanceValid(shipArchiveScroll))
        {
            shipArchiveScroll.ScrollEnded -= onArchiveScrollEnded;
            shipArchiveScroll.GetVScrollBar().ValueChanged -= onArchiveScrollValueChanged;
        }
    }

    private void connectButton(Button button, Action callback)
    {
        if (button == null || !IsInstanceValid(button))
        {
            return;
        }

        button.Pressed += callback;
    }

    private void disconnectButton(Button button, Action callback)
    {
        if (button == null || !IsInstanceValid(button))
        {
            return;
        }

        button.Pressed -= callback;
    }

    private void onGameContinue()
    {
        if (!ensureComplianceAllowsGameplay())
        {
            return;
        }

        if (!RunConfiguration.tryLoadLastConfiguration())
        {
            refreshContinueButton();
            LogUtil.Warning("Continue game is unavailable because no valid last-run configuration was found.");
            return;
        }

        if (SceneManager.Instance == null || !IsInstanceValid(SceneManager.Instance))
        {
            LogUtil.Error("Continue game failed because SceneManager is unavailable.");
            return;
        }

        LogUtil.Info("Continuing with the most recently selected ship and run rules.");
        _ = SceneManager.Instance.ChangeScene(Assets.MainGameScene);
    }

    private void onGameSettings()
    {
        showWindow(settingsWindow);
    }

    private void onShipArchive()
    {
        // Blueprint data now lives in the first Codex tab; keep this handler as
        // a safe compatibility path for any scene or shortcut still targeting it.
        ensureCodexLoaded();
        showWindow(codexWindow);
    }

    private void onCodex()
    {
        ensureCodexLoaded();
        showWindow(codexWindow);
    }

    private void onLanguageChanged(string locale)
    {
        refreshCodexTabTitles();
        refreshCodexLocalization();
        if (codexWindow != null && IsInstanceValid(codexWindow) && codexWindow.Visible)
        {
            ensureCodexLoaded();
        }
    }

    private void refreshCodexTabTitles()
    {
        if (codexTabs == null || !IsInstanceValid(codexTabs))
        {
            return;
        }

        codexTabs.SetTabTitle(0, translate("UI_CODEX_TAB_SHIPS"));
        codexTabs.SetTabTitle(1, translate("UI_CODEX_TAB_ENEMIES"));
        codexTabs.SetTabTitle(2, translate("UI_CODEX_TAB_BONDS"));
        codexTabs.SetTabTitle(3, translate("UI_CODEX_TAB_CARDS"));
    }

    private void refreshCodexLocalization()
    {
        if (_codexScroll != null && IsInstanceValid(_codexScroll))
        {
            _codexScroll.ScrollEnded -= onCodexScrollEnded;
            _codexScroll.GetVScrollBar().ValueChanged -= onCodexScrollValueChanged;
        }

        _codexScroll = null;
        _codexLoadedCount = 0;
        _enemyLoadedCount = 0;
        _cardLoadedCount = 0;
        _cardBatchQueued = false;
        _bondLoaded = false;
        _codexBatchQueued = false;
        clearCodexGrid(codexShipGrid);
        clearCodexGrid(codexEnemyGrid);
        clearCodexGrid(codexBondGrid);
        clearCodexGrid(codexCardGrid);

        if (codexDetailsPopup != null && IsInstanceValid(codexDetailsPopup))
        {
            codexDetailsPopup.Hide();
        }
    }

    private static void clearCodexGrid(GridContainer grid)
    {
        if (grid == null || !IsInstanceValid(grid))
        {
            return;
        }

        foreach (Node child in grid.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void onMetaUpgrade()
    {
        if (metaUpgradeWindow == null || !IsInstanceValid(metaUpgradeWindow))
        {
            LogUtil.Warning("Meta upgrade window is not configured.");
            return;
        }

        if (metaUpgradePoints != null && IsInstanceValid(metaUpgradePoints))
        {
            metaUpgradePoints.Text = string.Format(
                TranslationServer.Translate("UI_META_UPGRADE_SUMMARY"),
                MetaProgressStore.GetProfileLevel(),
                MetaProgressStore.GetExperience());
        }

        metaUpgradeCanvas?.Refresh();
        metaUpgradeWindow.MoveToFront();
        metaUpgradeWindow.Visible = true;
    }

    private void hideMetaUpgrade()
    {
        if (metaUpgradeWindow != null && IsInstanceValid(metaUpgradeWindow))
        {
            metaUpgradeWindow.Visible = false;
        }
    }

    private void showWindow(MarginContainer targetWindow)
    {
        setWindowVisible(settingsWindow, targetWindow == settingsWindow);
        setWindowVisible(shipArchiveWindow, targetWindow == shipArchiveWindow);
        setWindowVisible(codexWindow, targetWindow == codexWindow);
        setWindowVisible(runSetupWindow, false);
    }

    private void hideSecondaryWindows()
    {
        setWindowVisible(shipArchiveWindow, false);
        setWindowVisible(codexWindow, false);
    }

    private void setWindowVisible(MarginContainer window, bool isVisible)
    {
        if (window == null || !IsInstanceValid(window))
        {
            return;
        }

        window.Visible = isVisible;
    }

    private void onExit()
    {
        GetTree().Quit();
    }

    private void onStart()
    {
        if (!ensureComplianceAllowsGameplay())
        {
            showLastComplianceBlockIfNeeded();
            return;
        }

        setWindowVisible(runSetupWindow, true);
    }

    private void hideRunSetup()
    {
        setWindowVisible(runSetupWindow, false);
    }

    private void populateRunSetupOptions()
    {
        if (initialShipSelector == null || difficultySelector == null || waveSelector == null)
        {
            LogUtil.Warning("Run setup UI is missing one or more selection controls.");
            return;
        }

        Resource shipDataGroup = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (shipDataGroup != null && IsInstanceValid(shipDataGroup))
        {
            Array<PlayerShipData> availableShips = new();
            ResourceGroup.Of(shipDataGroup).LoadAllInto(availableShips);
            List<PlayerShipData> sortedShips = new();
            foreach (PlayerShipData shipData in availableShips)
            {
                if (shipData != null && IsInstanceValid(shipData) && shipData.ShipScene != null)
                {
                    sortedShips.Add(shipData);
                }
            }

            sortedShips.Sort(compareInitialShipOptions);
            _initialShipOptions.AddRange(sortedShips);
        }

        buildInitialShipCards();

        difficultySelector.Clear();
        difficultySelector.AddItem(translate("UI_RUN_SETUP_DIFFICULTY_CASUAL"));
        difficultySelector.AddItem(translate("UI_RUN_SETUP_DIFFICULTY_NORMAL"));
        difficultySelector.AddItem(translate("UI_RUN_SETUP_DIFFICULTY_HARD"));
        difficultySelector.Select(1);

        waveSelector.Clear();
        waveSelector.AddItem("5 波 · 快速");
        waveSelector.AddItem("8 波 · 短局");
        waveSelector.AddItem("10 波 · 标准");
        waveSelector.AddItem("15 波 · 扩展");
        waveSelector.AddItem("20 波 · 长局");
        waveSelector.AddItem("30 波 · 远征");
        waveSelector.Select(2);

        if (waveModeSelector != null && customWaveInput != null)
        {
            waveModeSelector.Clear();
            waveModeSelector.AddItem("预设波数");
            waveModeSelector.AddItem("自定义波数");
            waveModeSelector.AddItem("无限循环");
            waveModeSelector.Select(0);
            customWaveInput.MinValue = 4;
            customWaveInput.MaxValue = 60;
            customWaveInput.Step = 1;
            customWaveInput.Value = 10;
            updateWaveModePresentation();
        }

        if (_initialShipOptions.Count > 0)
        {
            selectInitialShip(0);
        }
    }

    private void loadHomepageData()
    {
        loadArchiveData();
        _blueprintShips.AddRange(_archiveShips);
        loadShipDesignData();
        loadEnemyDesignData();
        loadCardBenefitData();
        loadBondDefinitions();
        wrapCodexGridInScroll(codexShipGrid);
        wrapCodexGridInScroll(codexEnemyGrid);
        wrapCodexGridInScroll(codexBondGrid);
        wrapCodexGridInScroll(codexCardGrid);

        if (shipArchiveScroll != null && IsInstanceValid(shipArchiveScroll))
        {
            shipArchiveScroll.ScrollEnded += onArchiveScrollEnded;
            shipArchiveScroll.GetVScrollBar().ValueChanged += onArchiveScrollValueChanged;
        }
    }

    private void loadArchiveData()
    {
        Resource group = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (group == null || !IsInstanceValid(group))
        {
            LogUtil.Warning("飞船图鉴数据组未找到。");
            return;
        }

        ResourceGroup.Of(group).LoadAllInto(_archiveShips);
        for (int index = _archiveShips.Count - 1; index >= 0; index--)
        {
            PlayerShipData shipData = _archiveShips[index];
            if (shipData == null || !IsInstanceValid(shipData) || shipData.ShipScene == null)
            {
                _archiveShips.RemoveAt(index);
            }
        }
    }

    private void loadShipDesignData()
    {
        Resource group = GD.Load<Resource>(Assets.Ship_Design_Data);
        if (group == null || !IsInstanceValid(group))
        {
            LogUtil.Warning("舰船蓝图数据组未找到。");
            return;
        }

        ResourceGroup.Of(group).LoadAllInto(_shipDesigns);
        for (int index = _shipDesigns.Count - 1; index >= 0; index--)
        {
            if (_shipDesigns[index] == null || !IsInstanceValid(_shipDesigns[index]))
            {
                _shipDesigns.RemoveAt(index);
            }
        }
    }

    private void loadEnemyDesignData()
    {
        Resource group = GD.Load<Resource>(Assets.Enemy_Design_Data);
        if (group == null || !IsInstanceValid(group))
        {
            LogUtil.Warning("敌人图鉴数据组未找到。");
            return;
        }

        ResourceGroup.Of(group).LoadAllInto(_enemyDesigns);
        for (int index = _enemyDesigns.Count - 1; index >= 0; index--)
        {
            if (_enemyDesigns[index] == null || !IsInstanceValid(_enemyDesigns[index]))
            {
                _enemyDesigns.RemoveAt(index);
            }
        }
    }

    private void ensureArchiveLoaded()
    {
        if (shipArchiveItems == null || !IsInstanceValid(shipArchiveItems) || _archiveLoadedCount > 0)
        {
            return;
        }

        foreach (Node child in shipArchiveItems.GetChildren())
        {
            child.QueueFree();
        }

        _archiveLoadedCount = 0;
        queueArchiveBatch();
    }

    private void onArchiveScrollEnded()
    {
        LogUtil.Info("Archive scroll ended, checking for batch load.");
        if (shipArchiveScroll == null || !IsInstanceValid(shipArchiveScroll))
        {
            return;
        }

        if (shipArchiveScroll.GetVScrollBar().Value >= shipArchiveScroll.GetVScrollBar().MaxValue - shipArchiveScroll.Size.Y * 1.5f)
        {
            queueArchiveBatch();
        }
    }

    private void onArchiveScrollValueChanged(double value)
    {
        onArchiveScrollEnded();
    }

    private void queueArchiveBatch()
    {
        if (_archiveBatchQueued || _archiveLoadedCount >= _archiveShips.Count)
        {
            return;
        }

        _archiveBatchQueued = true;
        CallDeferred(nameof(loadNextArchiveBatch));
    }

    private void loadNextArchiveBatch()
    {
        _archiveBatchQueued = false;
        if (shipArchiveItems == null || !IsInstanceValid(shipArchiveItems) || shipArchiveItemScene == null || !IsInstanceValid(shipArchiveItemScene))
        {
            return;
        }

        int end = Mathf.Min(_archiveLoadedCount + ArchiveBatchSize, _archiveShips.Count);
        for (int index = _archiveLoadedCount; index < end; index++)
        {
            shipArchiveItem item = shipArchiveItemScene.Instantiate<shipArchiveItem>();
            shipArchiveItems.AddChild(item);
            item.Configure(_archiveShips[index], codexDetailsPopup);
        }

        _archiveLoadedCount = end;

        // 少量数据不足以形成滚动条时继续补一批，避免用户看不到后续舰船。
        if (_archiveLoadedCount < _archiveShips.Count && shipArchiveScroll != null && IsInstanceValid(shipArchiveScroll))
        {
            VScrollBar scrollBar = shipArchiveScroll.GetVScrollBar();
            if (scrollBar == null || scrollBar.MaxValue <= shipArchiveScroll.Size.Y)
            {
                queueArchiveBatch();
            }
        }
    }

    private void ensureCodexLoaded()
    {
        if (codexShipGrid == null || !IsInstanceValid(codexShipGrid) || _codexLoadedCount > 0)
        {
            return;
        }

        foreach (Node child in codexShipGrid.GetChildren())
        {
            child.QueueFree();
        }

        _codexLoadedCount = 0;
        _codexScroll = codexShipGrid.GetParent() as ScrollContainer;
        if (_codexScroll != null && IsInstanceValid(_codexScroll))
        {
            _codexScroll.ScrollEnded += onCodexScrollEnded;
            _codexScroll.GetVScrollBar().ValueChanged += onCodexScrollValueChanged;
        }
        queueCodexBatch();
        buildEnemyCards();
        buildBondCards();
        if (codexCardGrid != null && IsInstanceValid(codexCardGrid) && _cardLoadedCount == 0)
        {
            foreach (Node child in codexCardGrid.GetChildren())
            {
                child.QueueFree();
            }
        }
        queueCardBatch();
    }

    private void queueCodexBatch()
    {
        if (_codexBatchQueued || _codexLoadedCount >= _blueprintShips.Count)
        {
            return;
        }

        _codexBatchQueued = true;
        CallDeferred(nameof(loadNextCodexBatch));
    }

    private void loadNextCodexBatch()
    {
        _codexBatchQueued = false;
        if (codexShipGrid == null || !IsInstanceValid(codexShipGrid))
        {
            return;
        }

        int end = Mathf.Min(_codexLoadedCount + CodexBatchSize, _blueprintShips.Count);
        for (int index = _codexLoadedCount; index < end; index++)
        {
            PlayerShipData shipData = _blueprintShips[index];
            ShipDesignData design = findShipDesign(shipData.ShipId);
            addCodexEntry(codexShipGrid, shipData.getDisplayName(), shipData.Icon, formatBlueprint(shipData, design));
        }

        _codexLoadedCount = end;
    }

    private void onCodexScrollEnded()
    {
        if (_codexScroll == null || !IsInstanceValid(_codexScroll))
        {
            return;
        }

        VScrollBar scrollBar = _codexScroll.GetVScrollBar();
        if (scrollBar.Value >= scrollBar.MaxValue - _codexScroll.Size.Y * 0.75f)
        {
            queueCodexBatch();
        }
    }

    private void onCodexScrollValueChanged(double value)
    {
        onCodexScrollEnded();
    }

    private ShipDesignData findShipDesign(StringName shipId)
    {
        for (int index = 0; index < _shipDesigns.Count; index++)
        {
            ShipDesignData design = _shipDesigns[index];
            if (design != null && design.ShipId == shipId)
            {
                return design;
            }
        }

        return null;
    }

    private static string formatBlueprint(PlayerShipData shipData, ShipDesignData design)
    {
        string name = shipData?.getDisplayName() ?? translate(design?.DisplayNameKey, design?.DisplayName);
        string designText = design == null
            ? translate("UI_CODEX_SHIP_NO_BLUEPRINT")
            : string.Format(
                translate("UI_CODEX_SHIP_DESIGN_DETAILS"),
                translate(design.CenterRingEffectKey, design.CenterRingEffect),
                translate(design.BranchOneDescriptionKey, design.BranchOneDescription),
                translate(design.BranchTwoDescriptionKey, design.BranchTwoDescription),
                translate(design.BranchThreeDescriptionKey, design.BranchThreeDescription));
        return $"{name}\n" +
            string.Format(translate("UI_CODEX_SHIP_STAR_AND_ROLE"), design?.MaxStarLevel ?? 1, shipData?.RoleId) + "\n" +
            string.Format(translate("UI_CODEX_SHIP_COMBAT_STATS"), shipData?.AttackPower, shipData?.AttackRange ?? 0) + "\n" +
            designText;
    }

    private static string formatBlueprintTooltip(PlayerShipData shipData, ShipDesignData design)
    {
        return design == null
            ? string.Format(translate("UI_CODEX_SHIP_BASE_ABILITIES"), shipData?.WeaponId, shipData?.ActiveAbilityId, shipData?.PassiveAbilityId)
            : string.Format(
                translate("UI_CODEX_SHIP_BRANCH_TOOLTIP"),
                translate(design.BranchOneDescriptionKey, design.BranchOneDescription),
                translate(design.BranchTwoDescriptionKey, design.BranchTwoDescription),
                translate(design.BranchThreeDescriptionKey, design.BranchThreeDescription));
    }

    private static string formatBlueprint(ShipDesignData design)
    {
        PlayerShipData shipData = design?.ShipData;
        string name = translate(design?.DisplayNameKey, design?.DisplayName);
        if (string.IsNullOrEmpty(name) && shipData != null)
        {
            name = shipData.getDisplayName();
        }

        return $"{name}\n" +
            string.Format(translate("UI_CODEX_SHIP_STAR_AND_ROLE"), design?.MaxStarLevel ?? 0, shipData?.RoleId) + "\n" +
            string.Format(
                translate("UI_CODEX_SHIP_DESIGN_DETAILS"),
                translate(design?.CenterRingEffectKey, design?.CenterRingEffect),
                translate(design?.BranchOneDescriptionKey, design?.BranchOneDescription),
                translate(design?.BranchTwoDescriptionKey, design?.BranchTwoDescription),
                translate(design?.BranchThreeDescriptionKey, design?.BranchThreeDescription));
    }

    private static string formatBlueprintTooltip(ShipDesignData design)
    {
        return string.Format(
            translate("UI_CODEX_SHIP_BRANCH_TOOLTIP"),
            translate(design?.BranchOneDescriptionKey, design?.BranchOneDescription),
            translate(design?.BranchTwoDescriptionKey, design?.BranchTwoDescription),
            translate(design?.BranchThreeDescriptionKey, design?.BranchThreeDescription));
    }

    private void buildEnemyCards()
    {
        if (codexEnemyGrid == null || !IsInstanceValid(codexEnemyGrid) || _enemyLoadedCount > 0)
        {
            return;
        }

        foreach (Node child in codexEnemyGrid.GetChildren())
        {
            child.QueueFree();
        }

        for (int index = 0; index < _enemyDesigns.Count; index++)
        {
            EnemyDesignData enemy = _enemyDesigns[index];
            Texture2D icon = enemy.Icon ?? loadEnemyIcon(index);
            if (enemy.Icon == null)
            {
                enemy.Icon = icon;
            }
            addCodexEntry(codexEnemyGrid, translate(enemy.DisplayNameKey, enemy.DisplayName), icon, $"{formatEnemy(enemy)}\n{formatEnemyTooltip(enemy)}");
        }

        _enemyLoadedCount = _enemyDesigns.Count;
    }

    private void loadCardBenefitData()
    {
        Resource group = GD.Load<Resource>(Assets.Card_Benefit_Data);
        if (group == null || !IsInstanceValid(group))
        {
            LogUtil.Warning("卡牌图鉴数据组未找到。");
            return;
        }

        ResourceGroup.Of(group).LoadAllInto(_cardBenefits);
    }

    private void loadBondDefinitions()
    {
        Resource group = GD.Load<Resource>(Assets.Bond_Definition_Data);
        if (group != null && IsInstanceValid(group))
        {
            ResourceGroup.Of(group).LoadAllInto(_bondDefinitions);
        }

        if (_bondDefinitions.Count == 0)
        {
            LogUtil.Warning("羁绊资源组为空，请检查 Bond_Definition_Data.tres。");
        }
    }

    private void buildBondCards()
    {
        if (_bondLoaded || codexBondGrid == null || !IsInstanceValid(codexBondGrid))
        {
            return;
        }

        foreach (Node child in codexBondGrid.GetChildren())
        {
            child.QueueFree();
        }

        for (int index = 0; index < _bondDefinitions.Count; index++)
        {
            BondDefinitionData bond = _bondDefinitions[index];
            codexBondGrid.AddChild(createCodexCard(
                $"{translate(bond.DisplayNameKey, bond.DisplayName)}\n{bond.BondId}\n{translate(bond.MemberConditionKey, bond.MemberCondition)}\n{translate(bond.EffectDescriptionKey, bond.EffectDescription)}",
                translate(bond.CounterplayKey, bond.Counterplay),
                CardBenefitIconCatalog.GetBondIcon()));
        }

        _bondLoaded = true;
    }

    private void queueCardBatch()
    {
        if (_cardBatchQueued || _cardLoadedCount >= _cardBenefits.Count || codexCardGrid == null || !IsInstanceValid(codexCardGrid))
        {
            return;
        }

        _cardBatchQueued = true;
        CallDeferred(nameof(loadNextCardBatch));
    }

    private void loadNextCardBatch()
    {
        _cardBatchQueued = false;
        int end = Mathf.Min(_cardLoadedCount + 12, _cardBenefits.Count);
        for (int index = _cardLoadedCount; index < end; index++)
        {
            CardBenefitData card = _cardBenefits[index];
            string targetScopeLabel = TranslationServer.Translate("UI_CODEX_TARGET_SCOPE");
            codexCardGrid.AddChild(createCodexCard(
                $"{translate(card.DisplayNameKey, card.DisplayName)}\n{card.RarityId} · {card.CategoryId}\n{translate(card.EffectDescriptionKey, card.EffectDescription)}",
                $"{translate(card.SynergyDescriptionKey, card.SynergyDescription)}\n{targetScopeLabel}：{card.TargetScopeId}",
                CardBenefitIconCatalog.GetCardIcon(card.CategoryId, card.CardId)));
        }

        _cardLoadedCount = end;
        if (_cardLoadedCount < _cardBenefits.Count)
        {
            queueCardBatch();
        }
    }

    private void addCodexEntry(GridContainer grid, string displayName, Texture2D icon, string details)
    {
        if (grid == null || !IsInstanceValid(grid) || codexListItemScene == null || !IsInstanceValid(codexListItemScene) ||
            codexDetailsPopup == null || !IsInstanceValid(codexDetailsPopup))
        {
            LogUtil.Warning("Codex item scene or details popup is not configured.");
            return;
        }

        CodexListItem item = codexListItemScene.Instantiate<CodexListItem>();
        item.configure(displayName, icon, () => codexDetailsPopup.showDetails(displayName, icon, details));
        grid.AddChild(item);
    }

    private Control createCodexCard(string textValue, string details, Texture2D icon)
    {
        if (codexListItemScene == null || !IsInstanceValid(codexListItemScene) ||
            codexDetailsPopup == null || !IsInstanceValid(codexDetailsPopup))
        {
            LogUtil.Warning("Codex item scene or details popup is not configured.");
            return new Control();
        }

        string displayName = textValue.Split('\n')[0];
        CodexListItem item = codexListItemScene.Instantiate<CodexListItem>();
        item.configure(displayName, icon, () => codexDetailsPopup.showDetails(displayName, icon, textValue + "\n" + details));
        return item;
    }

    private static Texture2D loadEnemyIcon(int index)
    {
        int enemyNumber = index + 1;
        if (enemyNumber <= 8)
        {
            string enemyPath = $"res://Resource/Enemies/enemy_{enemyNumber:00}.png";
            if (FileAccess.FileExists(enemyPath))
            {
                Texture2D icon = GD.Load<Texture2D>(enemyPath);
                if (icon != null && IsInstanceValid(icon))
                {
                    return icon;
                }
            }
        }

        int shipNumber = Mathf.Clamp(enemyNumber, 1, 7);
        return GD.Load<Texture2D>($"res://Resource/Ships/ship_{shipNumber:00}.png");
    }

    private static void wrapCodexGridInScroll(GridContainer grid)
    {
        if (grid == null || !IsInstanceValid(grid) || grid.GetParent() is ScrollContainer)
        {
            return;
        }

        Node parent = grid.GetParent();
        if (parent == null || !IsInstanceValid(parent))
        {
            return;
        }

        ScrollContainer scroll = new()
        {
            Name = $"{grid.Name}Scroll",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(0, 260)
        };
        parent.AddChild(scroll);
        grid.Reparent(scroll, false);
        grid.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
    }

    private static string formatEnemy(EnemyDesignData enemy)
    {
        return $"{translate(enemy.DisplayNameKey, enemy.DisplayName)}\n" +
            $"生命 {enemy.BaseHealth}  护盾 {enemy.EnemyShieldLayers}\n" +
            $"攻击 {enemy.DisplayDamage}  射程 {enemy.AttackRange:0}\n" +
            $"间隔 {enemy.CooldownSeconds:0.0}s  抗性 {enemy.ControlResistance:P0}\n" +
            $"掉落：{translate(enemy.DropSummaryKey, enemy.DropSummary)}";
    }

    private static string formatEnemyTooltip(EnemyDesignData enemy)
    {
        return $"目标与攻击：{translate(enemy.TargetAndAttackKey, enemy.TargetAndAttack)}\n" +
            $"机制与预警：{translate(enemy.MechanicAndWarningKey, enemy.MechanicAndWarning)}\n" +
            $"生成规则：{translate(enemy.SpawnRuleKey, enemy.SpawnRule)}";
    }

    private void buildInitialShipCards()
    {
        if (initialShipSelector != null && IsInstanceValid(initialShipSelector))
        {
            if (initialShipSelector.GetParent() is Control initialShipRow)
            {
                initialShipRow.Hide();
            }
        }

        if (initialShipCards == null || !IsInstanceValid(initialShipCards))
        {
            LogUtil.Warning("Initial ship card list is not configured.");
            return;
        }

        foreach (Node child in initialShipCards.GetChildren())
        {
            child.QueueFree();
        }

        _initialShipCardButtons.Clear();
        for (int index = 0; index < _initialShipOptions.Count; index++)
        {
            int shipIndex = index;
            PlayerShipData shipData = _initialShipOptions[index];
            Button card = new()
            {
                CustomMinimumSize = new Vector2(148, 124),
                ToggleMode = true,
                Text = formatShipCardText(shipData),
                TooltipText = formatShipTooltip(shipData),
                Icon = shipData.Icon,
                // Keep the ship artwork at its imported size rather than stretching it to fill the card.
                ExpandIcon = true,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
                Alignment = HorizontalAlignment.Center,
                IconAlignment = HorizontalAlignment.Center,
                VerticalIconAlignment = VerticalAlignment.Top
            };
            card.Pressed += () => selectInitialShip(shipIndex);
            initialShipCards.AddChild(card);
            _initialShipCardButtons.Add(card);
        }
    }

    private void selectInitialShip(int index)
    {
        if (index < 0 || index >= _initialShipOptions.Count)
        {
            return;
        }

        _selectedInitialShipIndex = index;
        for (int cardIndex = 0; cardIndex < _initialShipCardButtons.Count; cardIndex++)
        {
            Button card = _initialShipCardButtons[cardIndex];
            if (card != null && IsInstanceValid(card))
            {
                card.ButtonPressed = cardIndex == index;
            }
        }

        updateInitialShipPreview(_initialShipOptions[index]);
    }

    private void onWaveModeSelected(long index)
    {
        updateWaveModePresentation();
    }

    private void updateWaveModePresentation()
    {
        bool isCustom = waveModeSelector != null && waveModeSelector.Selected == 1;
        bool isInfinite = waveModeSelector != null && waveModeSelector.Selected == 2;

        if (waveSelector != null)
        {
            waveSelector.Visible = !isCustom && !isInfinite;
        }

        if (customWaveInput != null)
        {
            customWaveInput.Visible = isCustom;
            if (customWaveInput.GetParent() is Control customWaveRow)
            {
                customWaveRow.Visible = isCustom;
            }
        }

        if (waveModeDescription != null)
        {
            waveModeDescription.Text = isInfinite
                ? "无限循环：每 10 波进入一次循环 Boss，Boss 后可安全撤离。"
                : isCustom
                    ? "自定义总波数：4～60 波。路线、危机波与 Boss 会按总波数自动分配。"
                    : "选择预设总波数：波数越长，路线节点和后期危机波越多。";
        }
    }

    private void updateInitialShipPreview(PlayerShipData shipData)
    {
        if (initialShipPreview == null || !IsInstanceValid(initialShipPreview) || shipData == null || !IsInstanceValid(shipData))
        {
            return;
        }

        initialShipPreview.Text =
            $"{shipData.getDisplayName()}\n" +
            $"{translate(shipData.DescriptionKey, shipData.Description)}\n" +
            $"攻击 {shipData.AttackPower}  射程 {shipData.AttackRange:0}  间隔 {shipData.AttackInterval:0.00}s  舰队速度统一";
    }

    private void startConfiguredRun()
    {
        if (!ensureComplianceAllowsGameplay())
        {
            return;
        }

        if (_initialShipOptions.Count == 0 || initialShipSelector == null || difficultySelector == null || waveSelector == null)
        {
            LogUtil.Warning("Run setup cannot start because no initial ship is available.");
            return;
        }

        int shipIndex = Mathf.Clamp(_selectedInitialShipIndex, 0, _initialShipOptions.Count - 1);
        int waveCount = waveSelector.Selected switch
        {
            0 => 5,
            1 => 8,
            3 => 15,
            4 => 20,
            5 => 30,
            _ => 10
        };

        bool isInfinite = waveModeSelector != null && waveModeSelector.Selected == 2;
        if (waveModeSelector != null && waveModeSelector.Selected == 1 && customWaveInput != null)
        {
            waveCount = Mathf.Clamp(Mathf.RoundToInt((float)customWaveInput.Value), 4, 60);
        }

        RunConfiguration.Configure(_initialShipOptions[shipIndex], difficultySelector.Selected, waveCount, isInfinite);
        _ = SceneManager.Instance.ChangeScene(Assets.MainGameScene);
    }

    private bool ensureComplianceAllowsGameplay()
    {
        if (!OS.HasFeature("android"))
        {
            return true;
        }

        TapTapComplianceManager complianceManager = TapTapComplianceManager.Instance;
        if (complianceManager != null && IsInstanceValid(complianceManager) && complianceManager.CanEnterGameplay)
        {
            return true;
        }

        LogUtil.Warning("Gameplay entry was blocked until TapTap real-name and anti-addiction verification succeeds.");
        if (complianceManager != null && IsInstanceValid(complianceManager) && !complianceManager.IsVerificationPending)
        {
            complianceManager.BeginVerification();
        }

        return false;
    }

    private void onComplianceAuthenticationSucceeded()
    {
        dismissComplianceDialog();
        updateComplianceAccessControls();
        LogUtil.Success("Main menu gameplay entry is enabled by TapTap compliance.");
    }

    private void onComplianceAuthenticationBlocked(long code)
    {
        updateComplianceAccessControls();
        showComplianceBlockedDialog(code);
        LogUtil.Warning($"Main menu gameplay entry remains blocked by TapTap compliance code {code}.");
    }

    private void showLastComplianceBlockIfNeeded()
    {
        if (!OS.HasFeature("android"))
        {
            return;
        }

        TapTapComplianceManager complianceManager = TapTapComplianceManager.Instance;
        if (complianceManager == null || !IsInstanceValid(complianceManager) ||
            complianceManager.CanEnterGameplay || complianceManager.LastComplianceCode == 0)
        {
            return;
        }

        showComplianceBlockedDialog(complianceManager.LastComplianceCode);
    }

    private void showComplianceBlockedDialog(long code)
    {
        if (_complianceDialog != null && IsInstanceValid(_complianceDialog))
        {
            _complianceDialog.Configure(code);
            return;
        }

        PackedScene dialogScene = GD.Load<PackedScene>(Assets.TapTapComplianceDialog);
        _complianceDialog = dialogScene?.Instantiate<TapTapComplianceDialog>();
        if (_complianceDialog == null)
        {
            LogUtil.Error("TapTap compliance dialog could not be loaded.");
            return;
        }

        _complianceDialog.RetryRequested += onComplianceRetryRequested;
        _complianceDialog.ExitRequested += onComplianceExitRequested;
        AddChild(_complianceDialog);
        _complianceDialog.Configure(code);
    }

    private void onComplianceRetryRequested()
    {
        dismissComplianceDialog();
        updateComplianceAccessControls();

        TapTapComplianceManager complianceManager = TapTapComplianceManager.Instance;
        if (complianceManager == null || !IsInstanceValid(complianceManager))
        {
            LogUtil.Error("TapTap compliance verification cannot retry because the manager is unavailable.");
            return;
        }

        complianceManager.BeginVerification();
    }

    private void onComplianceExitRequested()
    {
        GetTree().Quit();
    }

    private void updateComplianceAccessControls()
    {
        bool isBlocked = isComplianceBlockingGameplay();
        if (gameStart != null && IsInstanceValid(gameStart))
        {
            gameStart.Disabled = isBlocked;
        }

        if (runSetupConfirm != null && IsInstanceValid(runSetupConfirm))
        {
            runSetupConfirm.Disabled = isBlocked;
        }

        refreshContinueButton();
    }

    private static bool isComplianceBlockingGameplay()
    {
        if (!OS.HasFeature("android"))
        {
            return false;
        }

        TapTapComplianceManager complianceManager = TapTapComplianceManager.Instance;
        return complianceManager == null || !IsInstanceValid(complianceManager) || !complianceManager.CanEnterGameplay;
    }

    private void disconnectComplianceDialog()
    {
        if (_complianceDialog == null || !IsInstanceValid(_complianceDialog))
        {
            _complianceDialog = null;
            return;
        }

        _complianceDialog.RetryRequested -= onComplianceRetryRequested;
        _complianceDialog.ExitRequested -= onComplianceExitRequested;
    }

    private void dismissComplianceDialog()
    {
        disconnectComplianceDialog();
        if (_complianceDialog != null && IsInstanceValid(_complianceDialog))
        {
            _complianceDialog.QueueFree();
        }

        _complianceDialog = null;
    }

    private void refreshContinueButton()
    {
        if (gameContinue == null || !IsInstanceValid(gameContinue))
        {
            return;
        }

        bool hasLastRun = RunConfiguration.hasLastConfiguration();
        gameContinue.Disabled = !hasLastRun || isComplianceBlockingGameplay();
        gameContinue.TooltipText = hasLastRun
            ? "使用最近一次选择的初始舰船、难度和波次规则开始新一局。"
            : "完成一次开局配置后可继续使用最近的配置。";
    }

    private void updateRunSetupLayout()
    {
        if (runSetupWindow == null || !IsInstanceValid(runSetupWindow))
        {
            return;
        }

        float availableHeight = runSetupWindow.Size.Y;
        if (initialShipScroll != null && IsInstanceValid(initialShipScroll))
        {
            initialShipScroll.CustomMinimumSize = new Vector2(0.0f, Mathf.Clamp(availableHeight * 0.24f, 96.0f, 160.0f));
        }

        if (initialShipPreview != null && IsInstanceValid(initialShipPreview))
        {
            initialShipPreview.CustomMinimumSize = new Vector2(0.0f, Mathf.Clamp(availableHeight * 0.10f, 52.0f, 76.0f));
        }

        if (runSetupContents == null || !IsInstanceValid(runSetupContents))
        {
            return;
        }

        float actionWidth = Mathf.Max(0.0f, runSetupContents.Size.X - 16.0f);
        if (runSetupBack != null && IsInstanceValid(runSetupBack))
        {
            runSetupBack.CustomMinimumSize = new Vector2(Mathf.Clamp(actionWidth * 0.45f, 96.0f, 144.0f), 0.0f);
        }

        if (runSetupConfirm != null && IsInstanceValid(runSetupConfirm))
        {
            runSetupConfirm.CustomMinimumSize = new Vector2(Mathf.Clamp(actionWidth * 0.55f, 112.0f, 176.0f), 0.0f);
        }
    }

    private void updateMainMenuPopupLayout()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize.X <= 0.0f || viewportSize.Y <= 0.0f)
        {
            return;
        }

        setPopupMargins(settingsWindow, viewportSize, 0.10f, 0.07f, 16.0f, 16.0f);
        setPopupMargins(shipArchiveWindow, viewportSize, 0.08f, 0.06f, 16.0f, 16.0f);
        setPopupMargins(codexWindow, viewportSize, 0.08f, 0.06f, 16.0f, 16.0f);
        setPopupMargins(runSetupWindow, viewportSize, 0.04f, 0.035f, 12.0f, 12.0f);
        setPopupContentMargins(codexContents, viewportSize, 0.035f, 0.04f, 12.0f, 14.0f);
        setPopupContentMargins(runSetupContents, viewportSize, 0.035f, 0.035f, 12.0f, 12.0f);

        if (metaUpgradeWindow != null && IsInstanceValid(metaUpgradeWindow))
        {
            float inset = viewportSize.X < CompactViewportWidth ? 0.03f : 0.075f;
            metaUpgradeWindow.AnchorLeft = inset;
            metaUpgradeWindow.AnchorTop = inset;
            metaUpgradeWindow.AnchorRight = 1.0f - inset;
            metaUpgradeWindow.AnchorBottom = 1.0f - inset;
        }

        int columns = viewportSize.X < CompactViewportWidth ? 1 : viewportSize.X < MediumViewportWidth ? 2 : 3;
        updateGridColumns(shipArchiveItems, columns == 3 ? 2 : columns);
        updateGridColumns(codexShipGrid, columns);
        updateGridColumns(codexEnemyGrid, columns);
        updateGridColumns(codexBondGrid, columns);
        updateGridColumns(codexCardGrid, columns);
        updateRunSetupLayout();
    }

    private static void setPopupMargins(MarginContainer window, Vector2 viewportSize, float horizontalRatio, float verticalRatio, float minimumHorizontal, float minimumVertical)
    {
        if (window == null || !IsInstanceValid(window))
        {
            return;
        }

        int horizontalMargin = Mathf.RoundToInt(Mathf.Max(minimumHorizontal, viewportSize.X * horizontalRatio));
        int verticalMargin = Mathf.RoundToInt(Mathf.Max(minimumVertical, viewportSize.Y * verticalRatio));
        window.AddThemeConstantOverride("margin_left", horizontalMargin);
        window.AddThemeConstantOverride("margin_top", verticalMargin);
        window.AddThemeConstantOverride("margin_right", horizontalMargin);
        window.AddThemeConstantOverride("margin_bottom", verticalMargin);
    }

    private static void updateGridColumns(GridContainer grid, int columns)
    {
        if (grid == null || !IsInstanceValid(grid))
        {
            return;
        }

        grid.Columns = columns;
    }

    private static void setPopupContentMargins(Control contents, Vector2 viewportSize, float horizontalRatio, float verticalRatio, float minimumHorizontal, float minimumVertical)
    {
        if (contents == null || !IsInstanceValid(contents))
        {
            return;
        }

        float horizontalMargin = Mathf.Max(minimumHorizontal, viewportSize.X * horizontalRatio);
        float verticalMargin = Mathf.Max(minimumVertical, viewportSize.Y * verticalRatio);
        contents.OffsetLeft = horizontalMargin;
        contents.OffsetTop = verticalMargin;
        contents.OffsetRight = -horizontalMargin;
        contents.OffsetBottom = -verticalMargin;
    }

    private static string translate(StringName key, string fallback = "")
    {
        if (key == default)
        {
            return fallback;
        }

        string translated = TranslationServer.Translate(key);
        return string.IsNullOrWhiteSpace(translated) || translated == key.ToString() ? fallback : translated;
    }

    private static string formatShipCardText(PlayerShipData shipData)
    {
        return shipData.getDisplayName();
    }

    private static string formatShipTooltip(PlayerShipData shipData)
    {
        return $"{shipData.getDisplayName()}\n" +
            $"{translate(shipData.DescriptionKey, shipData.Description)}\n" +
            $"攻击 {shipData.AttackPower}  射程 {shipData.AttackRange:0}  间隔 {shipData.AttackInterval:0.00}s";
    }

    private static int compareInitialShipOptions(PlayerShipData left, PlayerShipData right)
    {
        int rarityComparison = getRaritySortOrder(right.RarityId).CompareTo(getRaritySortOrder(left.RarityId));
        if (rarityComparison != 0)
        {
            return rarityComparison;
        }

        int nameComparison = string.CompareOrdinal(left.getDisplayName(), right.getDisplayName());
        return nameComparison != 0 ? nameComparison : string.CompareOrdinal(left.ShipId.ToString(), right.ShipId.ToString());
    }

    private static int getRaritySortOrder(StringName rarityId)
    {
        return rarityId.ToString() switch
        {
            "legendary" => 4,
            "epic" => 3,
            "rare" => 2,
            _ => 1
        };
    }
}

using System;
using Godot;

public partial class FleetEditor : PanelContainer
{
    [Export] public Label TitleLabel;
    [Export] public Label PendingShipLabel;
    [Export] public Label HintLabel;
    [Export] public FleetFormationCanvas FormationCanvas;
    [Export] public Button ConfirmButton;
    [Export] public Button CancelButton;

    private const int PendingStarLevel = 1;
    private AddPlayer _recruitment;
    private PlayerShipData _pendingShip;
    private CardBenefitEnhanceFunction _targetedBenefit;
    private Action _onCompleted;
    private Action _onCancelled;
    private int _selectedSlotIndex = -1;
    private bool _selectedIsFusion;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        ConfirmButton.Pressed += confirmSelection;
        CancelButton.Pressed += cancelSelection;
        FormationCanvas.SlotSelected += onFormationSlotSelected;
        FormationCanvas.ShipDragged += onExistingShipDragged;
        Hide();
    }

    public override void _ExitTree()
    {
        ConfirmButton.Pressed -= confirmSelection;
        CancelButton.Pressed -= cancelSelection;
        FormationCanvas.SlotSelected -= onFormationSlotSelected;
        FormationCanvas.ShipDragged -= onExistingShipDragged;
    }

    public bool openRecruitment(AddPlayer recruitment, Action onCompleted, Action onCancelled)
    {
        if (recruitment == null || !IsInstanceValid(recruitment) ||
            !recruitment.tryResolveTargetShipData(out PlayerShipData shipData))
        {
            LogUtil.Warning("Unable to open fleet editor: recruitment ship data is missing.");
            return false;
        }

        _recruitment = recruitment;
        _pendingShip = shipData;
        _targetedBenefit = null;
        _onCompleted = onCompleted;
        _onCancelled = onCancelled;
        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        TitleLabel.Text = translate("UI_FLEET_RECRUIT_TITLE");
        PendingShipLabel.Text = string.Format(translate("UI_FLEET_RECRUIT_PENDING"), getShipName(shipData), PendingStarLevel);
        HintLabel.Text = translate("UI_FLEET_RECRUIT_HINT");
        ConfirmButton.Show();
        ConfirmButton.Text = translate("UI_FLEET_CONFIRM_DEPLOY");
        ConfirmButton.Disabled = true;
        CancelButton.Text = translate("UI_FLEET_BACK_TO_SELECTION");
        FormationCanvas.configure(PlayerManager.Instance, shipData);
        Show();
        return true;
    }

    public bool openEditor(Action onClosed)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !IsInstanceValid(playerManager))
        {
            LogUtil.Warning("Unable to open fleet editor: player manager is unavailable.");
            return false;
        }

        _recruitment = null;
        _pendingShip = null;
        _targetedBenefit = null;
        _onCompleted = onClosed;
        _onCancelled = onClosed;
        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        TitleLabel.Text = translate("UI_FLEET_EDITOR_TITLE");
        PendingShipLabel.Text = translate("UI_FLEET_EDITOR_HINT");
        HintLabel.Text = translate("UI_FLEET_EDITOR_HINT");
        ConfirmButton.Hide();
        CancelButton.Text = translate("UI_FLEET_BACK_TO_PAUSE");
        FormationCanvas.configure(playerManager);
        Show();
        return true;
    }

    /// <summary>
    /// Opens the shared formation view to select a deployed ship for a single-ship benefit.
    /// Ship dragging is disabled so the player cannot alter formation while choosing a target.
    /// </summary>
    public bool openBenefitTargetSelection(CardBenefitEnhanceFunction targetedBenefit, Action onCompleted, Action onCancelled)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (targetedBenefit?.Card == null || !IsInstanceValid(targetedBenefit.Card) ||
            playerManager == null || !IsInstanceValid(playerManager) || playerManager.CurrentPlayerCount < 1)
        {
            LogUtil.Warning("Unable to select a benefit target because the benefit or deployed fleet is unavailable.");
            return false;
        }

        _recruitment = null;
        _pendingShip = null;
        _targetedBenefit = targetedBenefit;
        _onCompleted = onCompleted;
        _onCancelled = onCancelled;
        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        TitleLabel.Text = translate("UI_FLEET_BENEFIT_TARGET_TITLE");
        PendingShipLabel.Text = string.Format(translate("UI_FLEET_BENEFIT_PENDING"), targetedBenefit.Card.DisplayName);
        HintLabel.Text = translate("UI_FLEET_BENEFIT_HINT");
        ConfirmButton.Show();
        ConfirmButton.Text = translate("UI_FLEET_APPLY_BENEFIT");
        ConfirmButton.Disabled = true;
        CancelButton.Text = translate("UI_FLEET_BACK_TO_SELECTION");
        FormationCanvas.configure(playerManager, null, true);
        Show();
        LogUtil.Info($"Opened fleet editor to select a target for benefit {targetedBenefit.Card.CardId}.");
        return true;
    }

    public void closeEditor()
    {
        if (!Visible)
        {
            return;
        }

        finish(false);
    }

    private void onFormationSlotSelected(int slotIndex)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null || !IsInstanceValid(playerManager))
        {
            return;
        }

        if (_targetedBenefit != null && IsInstanceValid(_targetedBenefit))
        {
            if (!playerManager.CurrentPlayerDic.TryGetValue(slotIndex, out PlayerController target) ||
                target == null || !IsInstanceValid(target) || target.ShipData == null || !IsInstanceValid(target.ShipData))
            {
                return;
            }

            if (!CardBenefitRuntime.CanApplyToTarget(_targetedBenefit.Card, target, out string reason))
            {
                HintLabel.Text = $"该舰不可使用此增益：{reason}";
                ConfirmButton.Disabled = true;
                return;
            }

            _selectedSlotIndex = slotIndex;
            _selectedIsFusion = false;
            FormationCanvas.setSelectedSlot(slotIndex);
            HintLabel.Text = $"已选择：{getShipName(target.ShipData)}。点击“应用增益”确认。";
            ConfirmButton.Disabled = false;
            return;
        }

        if (_pendingShip == null || !IsInstanceValid(_pendingShip))
        {
            return;
        }

        if (playerManager.isSlotAvailable(slotIndex))
        {
            selectSlot(slotIndex, false);
            return;
        }

        if (FormationCanvas.canFuseAtSlot(slotIndex))
        {
            selectSlot(slotIndex, true);
            return;
        }

        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        FormationCanvas.setSelectedSlot(-1);
        HintLabel.Text = "该位置的舰船型号或星级不匹配，不能融合。";
        ConfirmButton.Disabled = true;
    }

    private void selectSlot(int slotIndex, bool isFusion)
    {
        _selectedSlotIndex = slotIndex;
        _selectedIsFusion = isFusion;
        FormationCanvas.setSelectedSlot(slotIndex);
        HintLabel.Text = isFusion
            ? $"已选择位置 {slotIndex:D2}：同型号 ★{PendingStarLevel} 舰船融合，攻击 +20%、射程 +8%。"
            : $"已选择位置 {slotIndex:D2}：部署获得的舰船。";
        ConfirmButton.Disabled = false;
    }

    private void onExistingShipDragged(int sourceSlotIndex, int targetSlotIndex)
    {
        PlayerManager playerManager = PlayerManager.Instance;
        if (playerManager == null)
        {
            return;
        }

        bool merged = _pendingShip != null && IsInstanceValid(_pendingShip) &&
            playerManager.tryMergeShips(sourceSlotIndex, targetSlotIndex);
        bool rearranged = !merged && playerManager.tryMoveOrSwapShip(sourceSlotIndex, targetSlotIndex);
        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        if (ConfirmButton.Visible)
        {
            ConfirmButton.Disabled = true;
        }
        HintLabel.Text = merged
            ? "同类型、同星级舰船已合并升星。"
            : rearranged
                ? "舰船位置已交换。"
                : "舰船无法移动到该位置。";
        FormationCanvas.configure(playerManager, _pendingShip);
    }

    private void confirmSelection()
    {
        if (_targetedBenefit != null && IsInstanceValid(_targetedBenefit))
        {
            PlayerManager playerManager = PlayerManager.Instance;
            if (playerManager == null || !IsInstanceValid(playerManager) || _selectedSlotIndex < 1 ||
                !playerManager.CurrentPlayerDic.TryGetValue(_selectedSlotIndex, out PlayerController target) ||
                target == null || !IsInstanceValid(target) || target.ShipData == null || !IsInstanceValid(target.ShipData))
            {
                return;
            }

            if (!CardBenefitRuntime.TryApply(_targetedBenefit.Card, target, out string reason))
            {
                HintLabel.Text = $"应用失败：{reason}";
                LogUtil.Warning($"Benefit {_targetedBenefit.Card.CardId} could not be applied to selected ship: {reason}");
                return;
            }

            LogUtil.Info($"Applied benefit {_targetedBenefit.Card.CardId} to ship {target.ShipData?.ShipId}.");
            finish(true);
            return;
        }

        if (_recruitment == null || !IsInstanceValid(_recruitment) || _pendingShip == null ||
            !IsInstanceValid(_pendingShip) || _selectedSlotIndex < 1)
        {
            return;
        }

        bool applied = _selectedIsFusion
            ? PlayerManager.Instance != null && PlayerManager.Instance.tryPromoteShipAtIndex(
                _selectedSlotIndex, _pendingShip, PendingStarLevel)
            : _recruitment.tryApplyAtSlot(_selectedSlotIndex);
        if (!applied)
        {
            HintLabel.Text = "该位置状态已变化，请重新拖动选择。";
            FormationCanvas.configure(PlayerManager.Instance, _pendingShip);
            ConfirmButton.Disabled = true;
            return;
        }

        finish(true);
    }

    private void cancelSelection()
    {
        finish(false);
    }

    private void finish(bool wasApplied)
    {
        Hide();
        Action onCompleted = _onCompleted;
        Action onCancelled = _onCancelled;
        _recruitment = null;
        _pendingShip = null;
        _targetedBenefit = null;
        _onCompleted = null;
        _onCancelled = null;
        _selectedSlotIndex = -1;
        _selectedIsFusion = false;
        if (wasApplied)
        {
            onCompleted?.Invoke();
        }
        else
        {
            onCancelled?.Invoke();
        }
    }

    private static string getShipName(PlayerShipData shipData)
    {
        return shipData.getDisplayName();
    }

    private static string translate(StringName key)
    {
        return TranslationServer.Translate(key);
    }
}

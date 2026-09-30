using Godot;

public partial class EnhanceInfo : MarginContainer
{
	[Export]
	public
	EnhanceSelector SelectOne;
	[Export] public EnhanceSelector SelectTwo;
	[Export] public EnhanceSelector SelectThree;
	[Export] public FleetEditor FleetEditor;
	private int LevelUpCount;
	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;
		Visible = false;
		SelectOne.Selected.Pressed += () => OnOptionSelected(SelectOne);
		SelectTwo.Selected.Pressed += () => OnOptionSelected(SelectTwo);
		SelectThree.Selected.Pressed += () => OnOptionSelected(SelectThree);
		GlobalMessengerManager.Instance.Connect(
				GlobalMessengerManager.SignalName.OnShowEnhanceInfo,
				Callable.From<int>(OnShowEnhanceInfo)
			);
	}
	private void OnShowEnhanceInfo(int levelUpCount)
	{
		LevelUpCount = levelUpCount;
		Visible = true;
		GetTree().Paused = true;
		GetRandomEnhanceFunction();

	}
	private void OnOptionSelected(EnhanceSelector selector)
	{
		if (selector.EnhanceFunctionEntity != null)
		{
			if (selector.EnhanceFunctionEntity is CardBenefitEnhanceFunction cardFunction &&
				cardFunction.Card != null && IsInstanceValid(cardFunction.Card))
			{
				if (CardBenefitRuntime.TryCreateRecruitment(cardFunction.Card, out AddPlayer cardRecruitment) &&
					cardRecruitment.tryResolveTargetShipData(out _) &&
					FleetEditor != null && IsInstanceValid(FleetEditor))
				{
					Visible = false;
					if (!FleetEditor.openRecruitment(cardRecruitment, () => completeOptionSelection(cardFunction), () => Visible = true))
					{
						// 舰队编辑器未能打开（如目标舰船数据缺失）时恢复增益面板，避免游戏一直暂停。
						Visible = true;
					}
					return;
				}

				if (cardFunction.Card.TargetScopeId == "recruitment")
				{
					LogUtil.Warning($"Recruitment card {cardFunction.Card.CardId} has no resolvable target ship data.");
					return;
				}

				if (cardFunction.Card.RequiresTargetSelection)
				{
					Visible = false;
					if (FleetEditor == null || !IsInstanceValid(FleetEditor) ||
						!FleetEditor.openBenefitTargetSelection(cardFunction, () => completeOptionSelection(cardFunction), () => Visible = true))
					{
						Visible = true;
					}
					return;
				}
			}
			if (selector.EnhanceFunctionEntity is AddPlayer recruitment && FleetEditor != null &&
				IsInstanceValid(FleetEditor) && recruitment.tryResolveTargetShipData(out _))
			{
				Visible = false;
				if (!FleetEditor.openRecruitment(recruitment, () => completeOptionSelection(selector.EnhanceFunctionEntity), () => Visible = true))
				{
					Visible = true;
				}
				return;
			}

			selector.EnhanceFunctionEntity.ApplyEffect();
			EnhanceFunctionManager.Instance.registerAcquiredFunction(selector.EnhanceFunctionEntity);
		}

		completeOptionSelection(null);
	}

	private void completeOptionSelection(BaseEnhanceFunction enhanceFunction)
	{
		if (enhanceFunction != null && IsInstanceValid(enhanceFunction))
		{
			EnhanceFunctionManager.Instance.registerAcquiredFunction(enhanceFunction);
		}

		LevelUpCount--;
		if (LevelUpCount > 0)
		{
			// 从舰队编辑器完成招募返回时面板处于隐藏状态，恢复显示后再刷新选项，
			// 否则剩余升级次数无界面可选，游戏会一直保持暂停。
			Visible = true;
			GetRandomEnhanceFunction();
		}
		else
		{
			GetTree().Paused = false;
			Visible = false;
		}
	}

	public void GetRandomEnhanceFunction()
	{
		if (EnhanceFunctionManager.Instance == null)
		{
			LogUtil.Warning("EnhanceFunctionManager is unavailable when generating level-up options.");
			return;
		}

		Godot.Collections.Array<BaseEnhanceFunction> options = EnhanceFunctionManager.Instance.getRandomEnhanceFunctions(3);
		BaseEnhanceFunction shipUpgradeOffer = EnhanceFunctionManager.Instance.getShipUpgradeOffer(PlayerExpProgressBar.CurrentRunLevel);
		if (shipUpgradeOffer != null)
		{
			if (options.Count < 3)
			{
				options.Add(shipUpgradeOffer);
			}
			else
			{
				options[2] = shipUpgradeOffer;
			}
		}
		updateSelector(SelectOne, options, 0);
		updateSelector(SelectTwo, options, 1);
		updateSelector(SelectThree, options, 2);
	}

	private static void updateSelector(EnhanceSelector selector, Godot.Collections.Array<BaseEnhanceFunction> options, int index)
	{
		bool hasOption = selector != null && index < options.Count;
		if (selector == null)
		{
			return;
		}

		selector.Visible = hasOption;
		selector.EnhanceFunctionEntity = hasOption ? options[index] : null;
		if (hasOption)
		{
			selector.UpdateContents(options[index]);
		}
	}


}

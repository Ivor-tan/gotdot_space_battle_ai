using Godot;
using System;

public partial class PlayerExpProgressBar : ProgressBar
{
	public static int CurrentRunLevel { get; private set; } = 1;
	[Export] public int CurrentLevel = 1;
	[Export] public float CurrentExp = 0;
	[Export] public float MaxExp = 10;
	[Export] public float NextLevel = 1.5f;

	private int LevelUpCount = 0;

	public override void _Ready()
	{
		GlobalMessengerManager.Instance.Connect(
			GlobalMessengerManager.SignalName.OnGetExpPoints,
			Callable.From<int>(GainExperience));
		UpdateUI();
		CurrentRunLevel = CurrentLevel;
	}

	/// <summary>
	/// 增加经验的方法
	/// </summary>
	/// <param name="amount">获得的经验值数量</param>
	public void GainExperience(int amount)
	{
		LevelUpCount = 0;
		CurrentExp += amount;

		while (CurrentExp >= MaxExp)
		{
			LevelUp();
		}

		UpdateUI();
	}

	private void LevelUp()
	{
		CurrentExp -= MaxExp; // 扣除当前等级所需的经验
		CurrentLevel++;
		CurrentRunLevel = CurrentLevel;
		LevelUpCount++;
		MaxExp = Mathf.Floor(MaxExp * NextLevel);

		GlobalMessengerManager.Instance.SendShowEnhanceInfo(LevelUpCount);
		GlobalMessengerManager.Instance.SendNotification($"升级，当前 {CurrentLevel} 级！");
	}

	private void UpdateUI()
	{
		MaxValue = MaxExp;
		Tween tween = CreateTween();
		tween.TweenProperty(this, "value", CurrentExp, 0.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
	}
}

using Godot;

public partial class RewardAdTestControl : VBoxContainer
{
    [Export] public Button WatchButton;
    [Export] public Label StatusLabel;

    public override void _Ready()
    {
        if (!OS.IsDebugBuild())
        {
            Hide();
            SetProcess(false);
            return;
        }
        if (WatchButton == null || StatusLabel == null)
        {
            LogUtil.Error("Reward ad test controls are not configured.");
            SetProcess(false);
            return;
        }
        WatchButton.Pressed += onWatch;
    }

    public override void _Process(double delta)
    {
        TapTapAdManager ads = TapTapAdManager.Instance;
        bool available = ads != null && IsInstanceValid(ads);
        WatchButton.Disabled = !available || ads.IsBusy || !ads.CanWatchReward();
        StatusLabel.Text = Tr(available ? ads.StatusKey : "UI_AD_TEST_UNAVAILABLE");
    }

    private void onWatch()
    {
        TapTapAdManager ads = TapTapAdManager.Instance;
        if (ads != null && IsInstanceValid(ads))
        {
            ads.ShowRewardVideo(1067951L, "广告测试");
        }
    }

    public override void _ExitTree()
    {
        if (WatchButton != null && IsInstanceValid(WatchButton))
        {
            WatchButton.Pressed -= onWatch;
        }
    }
}

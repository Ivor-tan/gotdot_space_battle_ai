using Godot;
using System;

public partial class TapTapAdManager : Singleton<TapTapAdManager>
{
    [Export] public long MediaId = 1110466L;
    [Export] public string MediaName = "星海奇点";
    [Export] public double LoadTimeoutSeconds = 30;
    public bool IsBusy => _pending;
    public bool IsInitialized => _initialized;
    public string StatusKey => _statusKey;
    public event Action<bool> RewardFinished;
    public event Action<long, string> RewardVerified;
    private long _spaceId;
    private string _rewardName;
    private int _rewardAmount;
    private string _userId;
    private string _extra;
    private bool _rewardNotified;
    private Node _ads;
    private bool _initialized;
    private bool _pending;
    private bool _shown;
    private bool _rewardVerified;
    private double _elapsed;
    private string _statusKey = "UI_AD_TEST_HINT";

    protected override void onSingletonReady()
    {
        _ads = GetNodeOrNull<Node>("/root/GodotTapTapAd");
        LogUtil.RuntimeInfo($"[TapTapAd] ui_ready android={OS.HasFeature("android")} autoload_present={_ads != null} native_present={Engine.HasSingleton("GodotTapTapAdSDK")}");
        if (_ads != null)
        {
            bind("initialized", Callable.From(onInitialized));
            bind("initialization_failed", Callable.From<string>(onInitializationFailed));
            bind("reward_loaded", Callable.From(onLoaded));
            bind("reward_load_failed", Callable.From<long, string>(onLoadFailed));
            bind("reward_shown", Callable.From(onShown));
            bind("reward_closed", Callable.From(onClosed));
            bind("reward_verified", Callable.From<bool, long, string, long, string>(onVerified));
            bind("reward_video_skipped", Callable.From(onSkipped));
            bind("reward_video_error", Callable.From(onVideoError));
        }
    }

    public override void _Process(double delta)
    {
        if (_pending && !_shown)
        {
            _elapsed += delta;
            if (_elapsed >= LoadTimeoutSeconds)
            {
                disposeAds();
                finish("UI_AD_TEST_TIMEOUT");
            }
        }
    }

    public bool CanWatchReward()
    {
        return OS.HasFeature("android") && Engine.HasSingleton("GodotTapTapAdSDK") &&
            _ads != null && IsInstanceValid(_ads) && TapTapComplianceManager.Instance != null &&
            IsInstanceValid(TapTapComplianceManager.Instance) && TapTapComplianceManager.Instance.CanEnterGameplay;
    }

    /// <summary>Starts one reward video request. Rewards are notified only after SDK verification.</summary>
    public bool ShowRewardVideo(long spaceId, string rewardName, int rewardAmount = 1, string userId = "", string extra = "")
    {
        LogUtil.RuntimeInfo($"[TapTapAd] button_clicked pending={_pending} initialized={_initialized} can_watch={CanWatchReward()} native_present={Engine.HasSingleton("GodotTapTapAdSDK")}");
        if (_pending || !CanWatchReward() || spaceId <= 0 || string.IsNullOrWhiteSpace(rewardName) || rewardAmount <= 0)
        {
            LogUtil.RuntimeInfo("[TapTapAd] button_ignored reason=pending_or_access_blocked");
            return false;
        }
        _spaceId = spaceId;
        _rewardName = rewardName;
        _rewardAmount = rewardAmount;
        _userId = userId;
        _extra = extra;
        _rewardNotified = false;
        _pending = true;
        _shown = false;
        _rewardVerified = false;
        _elapsed = 0;
        _statusKey = "UI_AD_TEST_LOADING";

        // Credentials are injected after privacy consent and compliance verification.
        if (!_initialized)
        {
            GodotObject credentials = Engine.GetSingleton("TapTapCredentials");
            bool credentialsAvailable = credentials != null && IsInstanceValid(credentials);
            LogUtil.RuntimeInfo($"[TapTapAd] credentials_check singleton_present={credentialsAvailable}");
            if (!credentialsAvailable)
            {
                LogUtil.RuntimeInfo("[TapTapAd] initialize_aborted reason=credentials_missing");
                finish("UI_AD_TEST_UNAVAILABLE");
                return false;
            }
            string mediaKey = credentials.Call("getAdMediaKey").AsString();
            LogUtil.RuntimeInfo($"[TapTapAd] credentials_read media_key_present={!string.IsNullOrWhiteSpace(mediaKey)}");
            if (string.IsNullOrWhiteSpace(mediaKey))
            {
                LogUtil.RuntimeInfo("[TapTapAd] initialize_aborted reason=media_key_empty");
                finish("UI_AD_TEST_UNAVAILABLE");
                return false;
            }
            LogUtil.RuntimeInfo($"[TapTapAd] initialize_request media_id={MediaId} sdk_log=false personalized_ads=false");
            Variant result = _ads.Call("initialize", MediaId, MediaName, mediaKey,
                credentials.Call("getClientId"), false, false, new Godot.Collections.Dictionary());
            bool accepted = result.AsBool();
            LogUtil.RuntimeInfo($"[TapTapAd] initialize_dispatched accepted={accepted}");
            if (!accepted) finish("UI_AD_TEST_FAILED");
        }
        else
        {
            loadAd();
        }
        return _pending;
    }

    private void onInitialized()
    {
        LogUtil.RuntimeInfo("[TapTapAd] callback=initialized");
        _initialized = true;
        if (_pending) loadAd();
    }

    private void loadAd()
    {
        LogUtil.RuntimeInfo($"[TapTapAd] reward_load space_id={_spaceId}");
        _ads.Call("load_reward_video", _spaceId, _rewardName, _rewardAmount, _userId, _extra);
    }

    private void onLoaded()
    {
        LogUtil.RuntimeInfo($"[TapTapAd] callback=reward_loaded pending={_pending} shown={_shown}");
        if (!_pending || _shown) return;
        if (!CanWatchReward())
        {
            finish("UI_AD_TEST_UNAVAILABLE");
            return;
        }
        _shown = true;
        LogUtil.RuntimeInfo("[TapTapAd] reward_show_request");
        _ads.Call("show_reward_video");
    }

    private void onShown() { LogUtil.RuntimeInfo("[TapTapAd] reward_shown"); _statusKey = "UI_AD_TEST_PLAYING"; }
    private void onClosed() { LogUtil.RuntimeInfo("[TapTapAd] reward_closed"); finish(_rewardVerified ? "UI_AD_TEST_VERIFIED" : "UI_AD_TEST_CLOSED"); }
    private void onSkipped() { _statusKey = "UI_AD_TEST_SKIPPED"; }
    private void onVideoError() { finish("UI_AD_TEST_FAILED"); }
    private void onInitializationFailed(string message) { LogUtil.RuntimeInfo("[TapTapAd] initialization_failed"); finish("UI_AD_TEST_FAILED"); }
    private void onLoadFailed(long code, string message) { LogUtil.RuntimeInfo($"[TapTapAd] reward_load_failed code={code}"); finish("UI_AD_TEST_FAILED"); }
    private void onVerified(bool verified, long amount, string name, long code, string message)
    {
        if (!_pending || _rewardNotified) return;
        _rewardVerified = verified;
        if (verified)
        {
            _rewardNotified = true;
            RewardVerified?.Invoke(amount, name);
        }
        LogUtil.RuntimeInfo($"[TapTapAd] reward_verified verified={verified} code={code} amount={amount}");
        _statusKey = verified ? "UI_AD_TEST_VERIFIED" : "UI_AD_TEST_NOT_VERIFIED";
    }

    private void finish(string key)
    {
        LogUtil.RuntimeInfo($"[TapTapAd] test_finished status={key}");
        bool wasPending = _pending;
        _pending = false;
        _shown = false;
        _statusKey = key;
        if (wasPending) RewardFinished?.Invoke(_rewardVerified);
    }

    private void bind(string name, Callable callback) { _ads.Connect(name, callback); }

    protected override void onSingletonExitTree()
    {
        if (_ads == null || !IsInstanceValid(_ads)) return;
        unbind("initialized", Callable.From(onInitialized));
        unbind("initialization_failed", Callable.From<string>(onInitializationFailed));
        unbind("reward_loaded", Callable.From(onLoaded));
        unbind("reward_load_failed", Callable.From<long, string>(onLoadFailed));
        unbind("reward_shown", Callable.From(onShown));
        unbind("reward_closed", Callable.From(onClosed));
        unbind("reward_verified", Callable.From<bool, long, string, long, string>(onVerified));
        unbind("reward_video_skipped", Callable.From(onSkipped));
        unbind("reward_video_error", Callable.From(onVideoError));
        disposeAds();
    }

    private void disposeAds()
    {
        _initialized = false;
        if (_ads != null && IsInstanceValid(_ads)) _ads.Call("dispose_ads");
    }

    private void unbind(string name, Callable callback)
    {
        if (_ads.IsConnected(name, callback)) _ads.Disconnect(name, callback);
    }
}

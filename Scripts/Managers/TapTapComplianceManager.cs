using System;
using Godot;

public partial class TapTapComplianceManager : Singleton<TapTapComplianceManager>
{
    public event Action AuthenticationSucceeded;
    public event Action<long> AuthenticationBlocked;

    private const string PrivacyConsentPath = "user://privacy_consent.cfg";
    private const string PrivacyDialogPath = "res://Scene/UI/PrivacyConsentDialog.tscn";
    private const string TapTapAutoloadPath = "/root/GodotTapTap";
    private const string TapTapCredentialsSingleton = "TapTapCredentials";
    private const string AndroidLogTag = "[TapTapCompliance]";
    private const long LoginSuccessCode = 200;
    private const long LoginFailedCode = 400;
    private const long LoginCancelledCode = 499;
    private const long ComplianceSuccessCode = 500;
    private const long ComplianceExitedCode = 1000;
    private const long SwitchAccountCode = 1001;
    private const long PeriodRestrictedCode = 1030;
    private const long DurationLimitedCode = 1050;
    private const long AgeLimitedCode = 1100;
    private const long InvalidClientOrNetworkCode = 1200;
    private const long RealNameStoppedCode = 9002;
    private const double InitializationDelaySeconds = 1.0;
    private const bool EnableTapTapSdkLog = false;

    private Node _tapTap;
    private bool _initialized;
    private bool _logoutRequested;

    public bool IsAuthenticated { get; private set; }
    public bool IsVerificationPending { get; private set; }
    public long LastComplianceCode { get; private set; }
    public bool CanEnterGameplay => !OS.HasFeature("android") || IsAuthenticated;

    protected override void onSingletonReady()
    {

        ProcessMode = ProcessModeEnum.Always;

        if (!OS.HasFeature("android"))
        {
            return;
        }

        _tapTap = GetNodeOrNull<Node>(TapTapAutoloadPath);
        if (_tapTap == null || !IsInstanceValid(_tapTap))
        {
            LogUtil.Error("TapTap plugin autoload is unavailable on Android.");
            return;
        }

        _tapTap.Connect("onLoginResult", Callable.From<long, string>(onLoginResult));
        _tapTap.Connect("onAntiAddictionCallback", Callable.From<long>(onComplianceResult));
        logAndroidEvent("callbacks_connected", "login=onLoginResult compliance=onAntiAddictionCallback");

        if (hasPrivacyConsent())
        {
            CallDeferred(MethodName.BeginVerification);
        }
        else
        {
            CallDeferred(MethodName.ShowPrivacyConsent);
        }

    }

    protected override void onSingletonExitTree()
    {
        if (_tapTap != null && IsInstanceValid(_tapTap))
        {
            Callable loginCallable = Callable.From<long, string>(onLoginResult);
            Callable complianceCallable = Callable.From<long>(onComplianceResult);

            if (_tapTap.IsConnected("onLoginResult", loginCallable))
            {
                _tapTap.Disconnect("onLoginResult", loginCallable);
            }

            if (_tapTap.IsConnected("onAntiAddictionCallback", complianceCallable))
            {
                _tapTap.Disconnect("onAntiAddictionCallback", complianceCallable);
            }
        }

    }

    public void BeginVerification()
    {
        if (IsVerificationPending)
        {
            logAndroidEvent("verification_ignored", "reason=already_pending");
            return;
        }

        if (!OS.HasFeature("android"))
        {
            LogUtil.Warning("TapTap compliance verification is only available in an Android export.");
            return;
        }

        if (_tapTap == null || !IsInstanceValid(_tapTap))
        {
            LogUtil.Error("TapTap compliance verification cannot start because the plugin is unavailable.");
            return;
        }

        bool wasInitialized = _initialized;
        if (!ensureInitialized())
        {
            IsVerificationPending = false;
            return;
        }

        IsAuthenticated = false;
        IsVerificationPending = true;
        logAndroidEvent("verification_begin", $"sdk_initialized={wasInitialized}");

        if (!wasInitialized)
        {
            SceneTreeTimer initializationTimer = GetTree().CreateTimer(InitializationDelaySeconds);
            initializationTimer.Timeout += requestLoginOrCompliance;
            return;
        }

        requestLoginOrCompliance();
    }

    private void requestLoginOrCompliance()
    {
        if (_tapTap == null || !IsInstanceValid(_tapTap))
        {
            IsVerificationPending = false;
            LogUtil.Error("TapTap login could not start because the plugin became unavailable.");
            return;
        }

        bool isLoggedIn = _tapTap.Call("isLogin").AsBool();
        string requestedMethod = isLoggedIn ? "quickCheck" : "tap_login";
        logAndroidEvent("verification_request", $"logged_in={isLoggedIn} method={requestedMethod}");
        LogUtil.Info($"TapTap compliance verification started. Existing login: {isLoggedIn}.");
        _tapTap.Call(requestedMethod);
    }

    public void ShowPrivacyConsent()
    {
        PackedScene dialogScene = GD.Load<PackedScene>(PrivacyDialogPath);
        PrivacyConsentDialog dialog = dialogScene?.Instantiate<PrivacyConsentDialog>();
        if (dialog == null)
        {
            LogUtil.Error("Privacy consent dialog could not be loaded.");
            return;
        }

        dialog.Accepted += onPrivacyAccepted;
        dialog.Rejected += onPrivacyRejected;
        GetTree().Root.AddChild(dialog);
    }

    private bool ensureInitialized()
    {
        if (_initialized)
        {
            return true;
        }

        GodotObject credentials = Engine.GetSingleton(TapTapCredentialsSingleton);
        if (credentials == null || !IsInstanceValid(credentials))
        {
            LogUtil.Error("TapTap build credentials plugin is unavailable.");
            return false;
        }

        string clientId = credentials.Call("getClientId").AsString();
        string clientToken = credentials.Call("getClientToken").AsString();
        string region = credentials.Call("getRegion").AsString();
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientToken))
        {
            LogUtil.Error("TapTap Client ID or Client Token is missing from the Android build configuration.");
            return false;
        }

        _tapTap.Call("initialize", clientId, clientToken, region, EnableTapTapSdkLog);
        _initialized = true;
        logAndroidEvent("sdk_initialize", $"region={region} sdk_log={EnableTapTapSdkLog.ToString().ToLowerInvariant()} debug_build={OS.IsDebugBuild().ToString().ToLowerInvariant()}");
        LogUtil.Info("TapTap SDK initialization request completed.");
        return true;
    }

    private static bool hasPrivacyConsent()
    {
        ConfigFile consent = new();
        return consent.Load(PrivacyConsentPath) == Error.Ok &&
               consent.GetValue("privacy", "accepted", false).AsBool();
    }

    private void onPrivacyAccepted()
    {
        ConfigFile consent = new();
        consent.SetValue("privacy", "accepted", true);
        Error saveError = consent.Save(PrivacyConsentPath);
        if (saveError != Error.Ok)
        {
            LogUtil.Warning($"Privacy consent could not be persisted: {saveError}.");
        }

        BeginVerification();
    }

    private void onPrivacyRejected()
    {
        GetTree().Quit();
    }

    private void onLoginResult(long code, string accountJson)
    {
        logAndroidCallback("onLoginResult", code, getLoginResultReason(code), $"account_payload={(!string.IsNullOrWhiteSpace(accountJson)).ToString().ToLowerInvariant()}");

        if (code != LoginSuccessCode)
        {
            IsAuthenticated = false;
            IsVerificationPending = false;
            LastComplianceCode = code;
            LogUtil.Warning($"TapTap login did not succeed. Code: {code}.");
            AuthenticationBlocked?.Invoke(code);
            return;
        }

        LogUtil.Success("TapTap login succeeded; starting compliance verification.");
        _tapTap.Call("quickCheck");
    }

    private void onComplianceResult(long code)
    {
        logAndroidCallback("onAntiAddictionCallback", code, getComplianceReason(code), $"logout_requested={_logoutRequested.ToString().ToLowerInvariant()}");

        if (code == ComplianceExitedCode && _logoutRequested)
        {
            _logoutRequested = false;
            logAndroidEvent("logout_follow_up_consumed", $"code={code}");
            return;
        }

        LastComplianceCode = code;
        IsVerificationPending = false;

        if (code == ComplianceSuccessCode)
        {
            IsAuthenticated = true;
            LogUtil.Success("TapTap real-name and anti-addiction verification succeeded.");
            AuthenticationSucceeded?.Invoke();
            return;
        }

        IsAuthenticated = false;
        if (requiresTapTapLogout(code))
        {
            requestTapTapLogout();
        }

        LogUtil.Warning($"TapTap compliance verification blocked gameplay. Code: {code}, reason: {getComplianceReason(code)}.");
        AuthenticationBlocked?.Invoke(code);
        returnToMainMenuIfPlaying();
    }

    private static bool requiresTapTapLogout(long code)
    {
        return code == ComplianceExitedCode ||
               code == SwitchAccountCode ||
               code == RealNameStoppedCode;
    }

    private void requestTapTapLogout()
    {
        if (_logoutRequested || _tapTap == null || !IsInstanceValid(_tapTap))
        {
            return;
        }

        // The installed plugin's logOut bridge first calls TapTapCompliance.exit(),
        // which produces another 1000 callback, and then clears the TapTap account.
        // Guard that follow-up callback so it cannot recursively request logout.
        _logoutRequested = true;
        logAndroidEvent("logout_request", "method=logOut expected_follow_up_code=1000");
        _tapTap.Call("logOut");
    }

    private static string getLoginResultReason(long code)
    {
        return code switch
        {
            LoginSuccessCode => "login success",
            LoginFailedCode => "plugin login failure",
            LoginCancelledCode => "user cancelled login",
            _ => "unknown login result"
        };
    }

    private static string getComplianceReason(long code)
    {
        return code switch
        {
            ComplianceExitedCode => "compliance session exited or credentials invalid",
            SwitchAccountCode => "account switch requested",
            PeriodRestrictedCode => "current period is restricted",
            DurationLimitedCode => "play-time allowance exhausted",
            AgeLimitedCode => "configured age limit not met",
            InvalidClientOrNetworkCode => "client configuration or network request failed",
            RealNameStoppedCode => "real-name verification window closed",
            _ => "unknown compliance result"
        };
    }

    private static void logAndroidCallback(string callbackName, long code, string meaning, string details)
    {
        LogUtil.RuntimeInfo($"{AndroidLogTag} callback={callbackName} code={code} meaning=\"{meaning}\" {details}");
    }

    private static void logAndroidEvent(string eventName, string details)
    {
        LogUtil.RuntimeInfo($"{AndroidLogTag} event={eventName} {details}");
    }

    private void returnToMainMenuIfPlaying()
    {
        SceneTree tree = GetTree();
        Node currentScene = tree?.CurrentScene;
        if (tree == null || currentScene == null || !IsInstanceValid(currentScene) || currentScene.SceneFilePath == Assets.MainScene)
        {
            return;
        }

        LogUtil.Warning("Active gameplay is being stopped because TapTap compliance no longer allows entry.");
        CallDeferred(MethodName.ReturnToMainMenu);
    }

    private void ReturnToMainMenu()
    {
        SceneTree tree = GetTree();
        if (tree == null)
        {
            return;
        }

        tree.Paused = false;
        Error changeError = tree.ChangeSceneToFile(Assets.MainScene);
        if (changeError != Error.Ok)
        {
            LogUtil.Error($"Could not return to the main menu after a compliance block: {changeError}.");
        }
    }
}

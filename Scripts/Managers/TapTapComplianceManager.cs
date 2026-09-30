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
    private const long LoginSuccessCode = 200;
    private const long ComplianceSuccessCode = 500;
    private const double InitializationDelaySeconds = 1.0;

    private Node _tapTap;
    private bool _initialized;

    public bool IsAuthenticated { get; private set; }

    public override void _Ready()
    {
        base._Ready();

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

        if (hasPrivacyConsent())
        {
            CallDeferred(MethodName.BeginVerification);
        }
        else
        {
            CallDeferred(MethodName.ShowPrivacyConsent);
        }

    }

    public override void _ExitTree()
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

        base._ExitTree();
    }

    public void BeginVerification()
    {
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
            return;
        }

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
            LogUtil.Error("TapTap login could not start because the plugin became unavailable.");
            return;
        }

        bool isLoggedIn = _tapTap.Call("isLogin").AsBool();
        LogUtil.Info($"TapTap compliance verification started. Existing login: {isLoggedIn}.");
        _tapTap.Call(isLoggedIn ? "quickCheck" : "tap_login");
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

        _tapTap.Call("initialize", clientId, clientToken, region, OS.IsDebugBuild());
        _initialized = true;
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
        if (code != LoginSuccessCode)
        {
            LogUtil.Warning($"TapTap login did not succeed. Code: {code}.");
            AuthenticationBlocked?.Invoke(code);
            return;
        }

        LogUtil.Success("TapTap login succeeded; starting compliance verification.");
        _tapTap.Call("quickCheck");
    }

    private void onComplianceResult(long code)
    {
        if (code == ComplianceSuccessCode)
        {
            IsAuthenticated = true;
            LogUtil.Success("TapTap real-name and anti-addiction verification succeeded.");
            AuthenticationSucceeded?.Invoke();
            return;
        }

        IsAuthenticated = false;
        LogUtil.Warning($"TapTap compliance verification blocked entry. Code: {code}.");
        AuthenticationBlocked?.Invoke(code);
    }
}

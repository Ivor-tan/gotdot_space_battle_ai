using System;
using Godot;

public partial class TapTapComplianceDialog : CanvasLayer
{
    public event Action RetryRequested;
    public event Action ExitRequested;

    [Export] public Label MessageLabel;
    [Export] public Label CodeLabel;
    [Export] public Button RetryButton;
    [Export] public Button ExitButton;

    private long _code;

    public override void _Ready()
    {
        Layer = 120;
        ProcessMode = ProcessModeEnum.Always;

        if (RetryButton != null && IsInstanceValid(RetryButton))
        {
            RetryButton.Pressed += onRetryPressed;
        }

        if (ExitButton != null && IsInstanceValid(ExitButton))
        {
            ExitButton.Pressed += onExitPressed;
        }

        refreshContent();
    }

    public override void _ExitTree()
    {
        if (RetryButton != null && IsInstanceValid(RetryButton))
        {
            RetryButton.Pressed -= onRetryPressed;
        }

        if (ExitButton != null && IsInstanceValid(ExitButton))
        {
            ExitButton.Pressed -= onExitPressed;
        }
    }

    public void Configure(long code)
    {
        _code = code;
        if (IsNodeReady())
        {
            refreshContent();
        }
    }

    private void refreshContent()
    {
        if (MessageLabel != null && IsInstanceValid(MessageLabel))
        {
            MessageLabel.Text = TranslationServer.Translate(getMessageKey(_code));
        }

        if (CodeLabel != null && IsInstanceValid(CodeLabel))
        {
            CodeLabel.Text = string.Format(TranslationServer.Translate("UI_COMPLIANCE_CODE"), _code);
        }

        if (RetryButton != null && IsInstanceValid(RetryButton))
        {
            RetryButton.Visible = canRetry(_code);
            RetryButton.Text = TranslationServer.Translate(getRetryKey(_code));
        }

        if (ExitButton != null && IsInstanceValid(ExitButton))
        {
            ExitButton.Text = TranslationServer.Translate("UI_COMPLIANCE_EXIT");
        }
    }

    private static bool canRetry(long code)
    {
        return code == 400 || code == 499 || code == 1000 || code == 1001 || code == 1200 || code == 9002;
    }

    private static StringName getMessageKey(long code)
    {
        return code switch
        {
            400 => "UI_COMPLIANCE_LOGIN_FAILED",
            499 => "UI_COMPLIANCE_LOGIN_CANCELLED",
            1000 => "UI_COMPLIANCE_SESSION_EXITED",
            1001 => "UI_COMPLIANCE_ACCOUNT_SWITCHED",
            1030 => "UI_COMPLIANCE_PERIOD_RESTRICTED",
            1050 => "UI_COMPLIANCE_DURATION_LIMITED",
            1100 => "UI_COMPLIANCE_AGE_LIMITED",
            1200 => "UI_COMPLIANCE_NETWORK_ERROR",
            9002 => "UI_COMPLIANCE_REAL_NAME_STOPPED",
            _ => "UI_COMPLIANCE_UNKNOWN_ERROR"
        };
    }

    private static StringName getRetryKey(long code)
    {
        return code == 1200 ? "UI_COMPLIANCE_RETRY" : "UI_COMPLIANCE_REAUTHENTICATE";
    }

    private void onRetryPressed()
    {
        RetryRequested?.Invoke();
    }

    private void onExitPressed()
    {
        ExitRequested?.Invoke();
    }
}

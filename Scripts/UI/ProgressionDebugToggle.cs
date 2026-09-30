using Godot;

public partial class ProgressionDebugToggle : CheckButton
{
    public override void _Ready()
    {
        if (!OS.HasFeature("editor") || !OS.IsDebugBuild())
        {
            if (GetParent() is CanvasItem debugSettingRow)
            {
                debugSettingRow.Visible = false;
            }

            Visible = false;
            return;
        }

        GameConfigManager configManager = GameConfigManager.Instance;
        if (configManager == null || !GodotObject.IsInstanceValid(configManager))
        {
            Disabled = true;
            LogUtil.Warning("Progression debug toggle is unavailable because the game config manager is not ready.");
            return;
        }

        ButtonPressed = configManager.IsProgressionDebugEnabled;
        Toggled += onToggled;
    }

    public override void _ExitTree()
    {
        Toggled -= onToggled;
    }

    private void onToggled(bool isEnabled)
    {
        GameConfigManager configManager = GameConfigManager.Instance;
        if (configManager == null || !GodotObject.IsInstanceValid(configManager))
        {
            LogUtil.Warning("Progression debug toggle change was ignored because the game config manager is unavailable.");
            return;
        }

        configManager.SetProgressionDebugEnabled(isEnabled);
    }
}

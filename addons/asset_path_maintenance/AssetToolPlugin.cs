#if TOOLS
using Godot;

[Tool]
public partial class AssetToolPlugin : EditorPlugin
{
    AssetToolWindow window;

    public override void _EnterTree()
    {
        window = new AssetToolWindow();
        // 默认隐藏
        window.Visible = false;

        EditorInterface.Singleton
            .GetBaseControl()
            .AddChild(window);

        AddToolMenuItem(
            "Asset Path Tool",
            Callable.From(OpenWindow)
        );
    }

    public override void _ExitTree()
    {
        RemoveToolMenuItem("Asset Path Tool");

        window.QueueFree();
    }

    void OpenWindow()
    {
        window.PopupCentered(
            new Vector2I(600, 400)
        );
    }
}

#endif
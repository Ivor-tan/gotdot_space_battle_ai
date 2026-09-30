using Godot;

public abstract partial class Singleton<T> : Node where T : Singleton<T>
{
    private static T instance;

    public static T Instance
    {
        get => instance;
    }

    public override void _Ready()
    {
        if (instance != null && instance != this)
        {
            QueueFree();
            return;
        }

        instance = (T)this;
        // 可选：确保单例在场景切换时不被销毁
        // GetTree().Root.CallDeferred("add_child", this); 
    }

    public override void _ExitTree()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
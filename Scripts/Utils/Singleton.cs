using Godot;

public abstract partial class Singleton<T> : Node where T : Singleton<T>
{
    private static T _instance;
    private bool _ownsInstance;

    public static T Instance => TryGetInstance(out T instance) ? instance : null;

    public static bool TryGetInstance(out T instance)
    {
        instance = _instance;
        if (instance == null || !IsInstanceValid(instance) ||
            instance.IsQueuedForDeletion() || !instance.IsInsideTree())
        {
            instance = null;
            return false;
        }
        return true;
    }

    public sealed override void _EnterTree()
    {
        if (TryGetInstance(out T existing) && existing != this)
        {
            _ownsInstance = false;
            ProcessMode = ProcessModeEnum.Disabled;
            LogUtil.Warning($"Duplicate singleton {typeof(T).Name} rejected: {GetPath()}.");
            QueueFree();
            return;
        }

        _instance = (T)this;
        _ownsInstance = true;
        onSingletonEnterTree();
    }

    public sealed override void _Ready()
    {
        if (!_ownsInstance || IsQueuedForDeletion())
        {
            return;
        }
        onSingletonReady();
    }

    public sealed override void _ExitTree()
    {
        if (!_ownsInstance)
        {
            return;
        }
        try
        {
            onSingletonExitTree();
        }
        finally
        {
            if (ReferenceEquals(_instance, this))
            {
                _instance = null;
            }
            _ownsInstance = false;
        }
    }

    protected virtual void onSingletonEnterTree() { }
    protected virtual void onSingletonReady() { }
    protected virtual void onSingletonExitTree() { }
}

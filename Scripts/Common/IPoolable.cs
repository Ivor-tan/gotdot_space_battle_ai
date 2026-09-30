using Godot;


public interface IPoolable
{
    void OnSpawn();   // 产生时调用（重置状态）
    void OnDespawn(); // 回收时调用（清理引用）
}
using Godot;
using System.Collections.Generic;

public partial class ObjectPoolManager : Singleton<ObjectPoolManager>
{
	[Export] public PoolEntity[] PoolConfigs;

	// 支持每个 PoolType 下维护多个 scene 的映射：PoolType -> (sceneName -> PackedScene)
	private readonly Dictionary<PoolType, Dictionary<string, PackedScene>> SceneMapping = new();
	// Pools 改为二级字典：PoolType -> (sceneName -> Queue<Node2D>)
	private readonly Dictionary<PoolType, Dictionary<string, Queue<Node2D>>> Pools = new();

	// 一级根节点：PoolType -> Node
	private readonly Dictionary<PoolType, Node2D> PoolRoots = new();
	// 二级根节点：PoolType -> { SceneName -> Node }
	private readonly Dictionary<PoolType, Dictionary<string, Node2D>> SubRoots = new();

	protected override void onSingletonReady()
	{
		foreach (var entry in PoolConfigs)
		{
			if (entry == null || entry.Scene == null) continue;
			var type = entry.Type;
			var scene = entry.Scene;
			var sceneName = scene.ResourcePath.GetFile().GetBaseName();
			if (!SceneMapping.ContainsKey(type)) SceneMapping[type] = new Dictionary<string, PackedScene>();
			SceneMapping[type][sceneName] = scene;
		}
	}

	public Node2D Create(PoolType type, PackedScene scene, Vector2 globalPosition, Node2D rootParent = null)
	{
		if (scene == null) return null;
		var sceneName = scene.ResourcePath.GetFile().GetBaseName();
		if (!SceneMapping.ContainsKey(type)) SceneMapping[type] = new Dictionary<string, PackedScene>();
		if (!SceneMapping[type].ContainsKey(sceneName) || SceneMapping[type][sceneName] == null)
			SceneMapping[type][sceneName] = scene;
		return SpawnWithSceneName(type, sceneName, globalPosition, rootParent);
	}

	public Node2D Spawn(PoolType type, Vector2 globalPosition, Node2D rootParent = null)
	{
		// 旧的 Spawn 保持兼容，委托给 SpawnWithSceneName（默认为 null，表示使用 type 下的第一个 scene）
		return SpawnWithSceneName(type, null, globalPosition, rootParent);
	}

	// 新的 Spawn 方法，支持按 sceneName 分组的池化
	public Node2D SpawnWithSceneName(PoolType type, string sceneName, Vector2 globalPosition, Node2D rootParent = null)
	{
		if (!SceneMapping.ContainsKey(type) || SceneMapping[type].Count == 0) return null;

		if (string.IsNullOrEmpty(sceneName))
		{
			// 取第一个已注册的 scene 作为默认
			foreach (var kv in SceneMapping[type]) { sceneName = kv.Key; break; }
			if (sceneName == null) return null;
		}
		if (!SceneMapping[type].ContainsKey(sceneName)) return null;
		var scene = SceneMapping[type][sceneName];

		// 一级根节点
		if (!PoolRoots.ContainsKey(type))
		{
			Node2D root = new Node2D { Name = $"{type}_Pool" };
			AddChild(root);
			PoolRoots[type] = root;
			SubRoots[type] = new Dictionary<string, Node2D>();
		}

		// 二级根节点（按 sceneName 分组）
		if (!SubRoots[type].ContainsKey(sceneName))
		{
			Node2D subRoot = new Node2D { Name = $"{sceneName}_Group" };
			PoolRoots[type].AddChild(subRoot);
			SubRoots[type][sceneName] = subRoot;
		}

		if (!Pools.ContainsKey(type)) Pools[type] = new Dictionary<string, Queue<Node2D>>();
		if (!Pools[type].ContainsKey(sceneName)) Pools[type][sceneName] = new Queue<Node2D>();

		var queue = Pools[type][sceneName];
		Node2D obj;

		if (queue.Count > 0)
		{
			obj = queue.Dequeue();
			Node2D targetParent = rootParent ?? SubRoots[type][sceneName];
			if (obj.GetParent() != targetParent)
			{
				obj.GetParent()?.RemoveChild(obj);
				targetParent.AddChild(obj);
			}
		}
		else
		{
			obj = scene.Instantiate<Node2D>();
			Node2D targetParent = rootParent ?? SubRoots[type][sceneName];
			targetParent.AddChild(obj);
			obj.Name = $"{sceneName}_{queue.Count}";
			// 记录该实例属于哪个 scene 分组，方便 Release 时归队
			obj.SetMeta("pool_scene", sceneName);
		}

		obj.GlobalPosition = globalPosition;
		obj.Visible = true;
		obj.ProcessMode = ProcessModeEnum.Inherit;

		if (obj is IPoolable poolable) poolable.OnSpawn();
		return obj;
	}

	public void Release(PoolType type, Node2D obj)
	{
		if (obj == null) return;
		if (obj is IPoolable poolable) poolable.OnDespawn();

		obj.SetDeferred(CanvasItem.PropertyName.Visible, false);
		obj.CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);

		string sceneName = null;
		if (obj.HasMeta("pool_scene")) { sceneName = obj.GetMeta("pool_scene").ToString(); }
		if (sceneName == null)
		{
			// fallback to first registered scene for this type
			if (SceneMapping.ContainsKey(type) && SceneMapping[type].Count > 0)
			{
				foreach (var kv in SceneMapping[type]) { sceneName = kv.Key; break; }
			}
		}

		if (!Pools.ContainsKey(type)) Pools[type] = new Dictionary<string, Queue<Node2D>>();
		if (!Pools[type].ContainsKey(sceneName)) Pools[type][sceneName] = new Queue<Node2D>();
		Pools[type][sceneName].Enqueue(obj);
	}
}

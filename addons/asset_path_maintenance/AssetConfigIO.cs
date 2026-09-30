using Godot;
using System.Text.Json;

public static class AssetConfigIO
{

    private const string ConfigPath = "res://addons/asset_path_maintenance/config.tres";

    public static void Save(AssetConfig config)
    {
        // 确保目录存在
        string dir = ConfigPath.GetBaseDir();
        if (!DirAccess.DirExistsAbsolute(dir))
        {
            DirAccess.MakeDirRecursiveAbsolute(dir);
        }

        // 使用 ResourceSaver 进行保存
        // .tres 是文本格式（可读），.res 是二进制格式（体积小）
        Error err = ResourceSaver.Save(config, ConfigPath);

        if (err != Error.Ok)
        {
            GD.PrintErr($"资源保存失败: {err}");
        }
        else
        {
            GD.Print($"资源已保存至: {ConfigPath}");

        }
    }

    public static AssetConfig Load()
    {
        // string path = "res://addons/asset_path_maintenance/config.tres";
        if (!FileAccess.FileExists(ConfigPath)) return new AssetConfig();

        var res = ResourceLoader.Load(ConfigPath, "", ResourceLoader.CacheMode.Replace);

        // 1. 尝试正常转换
        if (res is AssetConfig config)
            return config;

        // 2. 如果强转失败，利用反射“偷梁换柱”
        GD.Print("[兼容模式] 强转失败，正在通过反射手动提取数据...");
        var manualConfig = new AssetConfig();

        // 手动同步字段 (确保变量名与 [Export] 的一致)
        manualConfig.ScanPaths = new Godot.Collections.Array<string>((string[])res.Get("ScanPaths"));
        manualConfig.OutputPath = (string)res.Get("OutputPath");

        // 处理嵌套的 Resource 数组
        var assetsRaw = res.Get("Assets").AsGodotArray();
        foreach (var item in assetsRaw)
        {
            if (item.As<Resource>() is AssetEntry entry)
            {
                manualConfig.Assets.Add(entry);
            }
        }

        return manualConfig;
    }
}
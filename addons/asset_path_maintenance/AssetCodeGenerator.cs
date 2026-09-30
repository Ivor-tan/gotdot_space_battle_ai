using Godot;
using System.Text;
using System.IO;

public static class AssetCodeGenerator
{
    public static void Generate(AssetConfig config)
    {
        if (config == null)
        {
            GD.PrintErr("AssetConfig is null");
            return;
        }

        if (string.IsNullOrEmpty(config.OutputPath))
        {
            GD.PrintErr("OutputPath is empty");
            return;
        }

        //---------------------------------------
        // 生成代码
        //---------------------------------------

        StringBuilder sb = new StringBuilder();

        sb.AppendLine("// Auto generated file");
        sb.AppendLine("public static class Assets");
        sb.AppendLine("{");

        foreach (var entry in config.Assets)
        {
            if (entry == null)
                continue;

            if (string.IsNullOrEmpty(entry.Name))
                continue;

            if (string.IsNullOrEmpty(entry.Path))
                continue;

            string name = Sanitize(entry.Name);

            sb.AppendLine(
                $"    public const string {name} = \"{entry.Path}\";"
            );
        }

        sb.AppendLine("}");

        //---------------------------------------
        // 获取绝对路径
        //---------------------------------------

        string absolutePath =
            ProjectSettings.GlobalizePath(config.OutputPath);

        //---------------------------------------
        // 自动创建目录
        //---------------------------------------

        string directory =
            Path.GetDirectoryName(absolutePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);

            GD.Print("Created directory: " + directory);
        }

        //---------------------------------------
        // 写入文件
        //---------------------------------------

        File.WriteAllText(absolutePath, sb.ToString());

        GD.Print("Assets.cs generated: " + absolutePath);
    }

    //---------------------------------------
    // 变量名合法化
    //---------------------------------------

    static string Sanitize(string name)
    {
        if (string.IsNullOrEmpty(name))
            return "Asset";

        name = name.Replace(" ", "_")
                   .Replace("-", "_")
                   .Replace(".", "_");

        if (char.IsDigit(name[0]))
            name = "_" + name;

        return name;
    }
}
using System.IO;
using Godot;

public static class AssetScanner
{
    public static void Scan(AssetConfig config)
    {
        config.Assets.Clear();

        foreach (string path in config.ScanPaths)
        {
            ScanDirectory(path, config);
        }

        GD.Print("Scan finished: " + config.Assets.Count);
    }
    static void ScanDirectory(string path, AssetConfig config)
    {
        var dir = DirAccess.Open(path);

        if (dir == null)
            return;

        dir.ListDirBegin();

        while (true)
        {
            string file = dir.GetNext();

            if (file == "")
                break;

            if (file == "." || file == "..")
                continue;

            string fullPath = path + "/" + file;

            if (dir.CurrentIsDir())
            {
                ScanDirectory(fullPath, config);
            }
            else
            {
                AddAsset(fullPath, config);
            }
        }

        dir.ListDirEnd();
    }
    static void AddAsset(string path, AssetConfig config)
    {
        if (Path.GetExtension(path).ToLower() != ".cs" && Path.GetExtension(path).ToLower() != ".gd"
        && Path.GetExtension(path).ToLower() != ".uid")
        {
            string name =
            System.IO.Path.GetFileNameWithoutExtension(path);

            name = name.Replace(" ", "_");

            AssetEntry entry = new AssetEntry();

            entry.Name = name;
            entry.Path = path;

            config.Assets.Add(entry);
        }

    }
}
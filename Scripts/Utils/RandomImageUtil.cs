using Godot;
using System;
using System.Collections.Generic;

public static class RandomImageUtil
{
    private static RandomNumberGenerator rng = new RandomNumberGenerator();

    static RandomImageUtil()
    {
        rng.Randomize();
    }

    /// <summary>
    /// 获取指定路径下随机一张图片
    /// </summary>
    public static Texture2D GetRandomImage(string resourcePath, bool recursive = false)
    {
        var files = GetImageFiles(resourcePath, recursive);
        if (files.Count == 0)
            return null;

        int index = rng.RandiRange(0, files.Count - 1);
        return GD.Load<Texture2D>(files[index]);
    }

    /// <summary>
    /// 获取指定路径下随机 N 张图片，可选择是否允许重复
    /// 自动判断可用数量，如果不重复时 N 大于总图片数，则返回最大可用数量
    /// </summary>
    public static List<Texture2D> GetRandomImages(string resourcePath, int count, bool recursive = false, bool allowDuplicate = true)
    {
        var files = GetImageFiles(resourcePath, recursive);
        var result = new List<Texture2D>();

        if (files.Count == 0 || count <= 0)
            return result;

        if (allowDuplicate)
        {
            for (int i = 0; i < count; i++)
            {
                int index = rng.RandiRange(0, files.Count - 1);
                result.Add(GD.Load<Texture2D>(files[index]));
            }
        }
        else
        {
            // 不重复，数量不能超过总图片数
            int n = Math.Min(count, files.Count);

            // 打乱列表
            var shuffled = new List<string>(files);
            ShuffleList(shuffled);

            for (int i = 0; i < n; i++)
                result.Add(GD.Load<Texture2D>(shuffled[i]));
        }

        return result;
    }

    /// <summary>
    /// 获取指定路径下图片总数
    /// </summary>
    public static int GetImageCount(string resourcePath, bool recursive = false)
    {
        return GetImageFiles(resourcePath, recursive).Count;
    }

    private static List<string> GetImageFiles(string resourcePath, bool recursive)
    {
        var result = new List<string>();
        var dir = DirAccess.Open(resourcePath);
        if (dir == null)
        {
            GD.PrintErr($"路径不存在: {resourcePath}");
            return result;
        }

        dir.ListDirBegin();
        string fileName = dir.GetNext();

        while (!string.IsNullOrEmpty(fileName))
        {
            if (fileName == "." || fileName == "..")
            {
                fileName = dir.GetNext();
                continue;
            }

            string fullPath = resourcePath + fileName;

            if (dir.CurrentIsDir())
            {
                if (recursive)
                    result.AddRange(GetImageFiles(fullPath + "/", true));
            }
            else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg"))
            {
                result.Add(fullPath);
            }

            fileName = dir.GetNext();
        }

        dir.ListDirEnd();
        return result;
    }

    private static void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.RandiRange(0, i);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
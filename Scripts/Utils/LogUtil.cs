using Godot;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
public static class LogUtil
{
    // 是否启用日志（可以在设置或单例中动态修改）
    public static bool IsEnabled = true;

    /// <summary>
    /// 普通调试信息（仅在编辑器/Debug模式显示）
    /// </summary>
    public static void Info(object message, [CallerFilePath] string path = "")
    {
#if DEBUG
        if (!IsEnabled) return;
        PrintFormatted("INFO", message, "white", path);
#endif
    }

    /// <summary>
    /// Runtime diagnostics that must remain visible in exported builds, including Android logcat.
    /// Do not include credentials, access tokens, or personally identifiable account data.
    /// </summary>
    public static void RuntimeInfo(object message, [CallerFilePath] string path = "")
    {
        if (!IsEnabled) return;
        PrintFormatted("RUNTIME", message, "cyan", path);
    }

    /// <summary>
    /// 警告信息
    /// </summary>
    public static void Warning(object message, [CallerFilePath] string path = "")
    {
        if (!IsEnabled) return;
        PrintFormatted("WARN", message, "yellow", path);
        GD.PushWarning($"[{GetFileName(path)}] {message}");
    }

    /// <summary>
    /// 错误信息（始终显示，并推送到 Godot 错误面板）
    /// </summary>
    public static void Error(object message, [CallerFilePath] string path = "")
    {
        PrintFormatted("ERROR", message, "red", path);
        GD.PushError($"[{GetFileName(path)}] {message}");
    }

    /// <summary>
    /// 成功/高亮信息
    /// </summary>
    public static void Success(object message, [CallerFilePath] string path = "")
    {
#if DEBUG
        if (!IsEnabled) return;
        PrintFormatted("SUCCESS", message, "green", path);
#endif
    }

    private static void PrintFormatted(string level, object message, string color, string path)
    {
        string fileName = GetFileName(path);
        string time = DateTime.Now.ToString("HH:mm:ss");

        // 使用 Godot 的 BBCode 格式化控制台输出（仅在 Godot 编辑器输出栏有效）
        GD.PrintRich($"[color=gray][{time}][/color] [b][color={color}][{level}][/color][/b] [color=aqua][{fileName}][/color]: {message}");
    }


    private static string GetFileName(string path)
    {
        if (string.IsNullOrEmpty(path)) return "Unknown";
        return System.IO.Path.GetFileNameWithoutExtension(path);
    }
}

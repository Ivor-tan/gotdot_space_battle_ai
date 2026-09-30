using Godot;
using System;

public partial class GameConfigManager : Singleton<GameConfigManager>
{
	public event Action<string> LanguageChanged;
	// 暴露在编辑器，方便快速指定默认配置文件
	[Export] public GameConfig GameConfig;

	private const string DefaultConfigPath = "res://Config/GameConfigs.tres";
	private const string SaveConfigPath = "user://Config/GameConfigs.tres";

	public bool IsProgressionDebugEnabled => GameConfig != null && GameConfig.EnableProgressionDebug;

	public override void _Ready()
	{
		base._Ready();
		InitConfig();
	}

	private void InitConfig()
	{
		if (FileAccess.FileExists(SaveConfigPath))
		{
			GameConfig = ResourceLoader.Load<GameConfig>(SaveConfigPath);
		}

		if (GameConfig == null)
		{
			GameConfig = ResourceLoader.Load<GameConfig>(DefaultConfigPath);
		}

		ApplyAllConfigs();
	}

	/// <summary>
	/// 将当前的配置应用到音频服务器和语言引擎
	/// </summary>
	public void ApplyAllConfigs()
	{
		if (GameConfig == null) return;

		SoundManager.Instance.SetVolume("Music", GameConfig.MusicVolume);
		SoundManager.Instance.SetVolume("SFX", GameConfig.SFXVolume);
		TranslationServer.SetLocale(GameConfig.Language);
		UpdateApplicationTitle();
	}

	/// <summary>
	/// 执行持久化保存（写入磁盘）
	/// </summary>
	public void SaveToDisk()
	{
		if (GameConfig == null) return;

		// 确保 user:// 内部的文件夹路径存在，否则保存会失败
		string dirPath = SaveConfigPath.GetBaseDir();
		if (!DirAccess.DirExistsAbsolute(dirPath))
		{
			DirAccess.MakeDirRecursiveAbsolute(dirPath);
		}

		Error err = ResourceSaver.Save(GameConfig, SaveConfigPath);
		if (err != Error.Ok)
		{
			LogUtil.Warning($"[Config] 保存失败: {err}");
		}
		else
		{
			LogUtil.Info("[Config] 设置已保存至 user://");
		}
	}

	// --- 修改并立即应用的方法 ---

	public void SetMusicVolume(float value)
	{
		GameConfig.MusicVolume = value;
		SoundManager.Instance.SetVolume("Music", value);
		SaveToDisk();
	}

	public void SetSFXVolume(float value)
	{
		GameConfig.SFXVolume = value;
		SoundManager.Instance.SetVolume("SFX", value);
		SaveToDisk();
	}

	public void SetLanguage(string locale)
	{
		GameConfig.Language = locale;
		TranslationServer.SetLocale(locale);
		UpdateApplicationTitle();
		LanguageChanged?.Invoke(locale);
		SaveToDisk();
	}

	private static void UpdateApplicationTitle()
	{
		DisplayServer.WindowSetTitle(TranslationServer.Translate("APP_NAME"));
	}

	public void SetProgressionDebugEnabled(bool isEnabled)
	{
		if (GameConfig == null)
		{
			LogUtil.Warning("Progression debug setting could not be saved because the game config is unavailable.");
			return;
		}

		GameConfig.EnableProgressionDebug = isEnabled;
		SaveToDisk();
	}
}

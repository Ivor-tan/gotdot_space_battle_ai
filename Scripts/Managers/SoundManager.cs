using Godot;
using GodotResourceGroups;
using System.Collections.Generic;

public partial class SoundManager : Singleton<SoundManager>
{

	[Export] public float CrossfadeTime = 1.5f;
	[Export] public int SfxPoolSize = 10;

	private AudioStreamPlayer _musicPlayerA;
	private AudioStreamPlayer _musicPlayerB;
	private AudioStreamPlayer _activePlayer;
	private List<AudioStreamPlayer> _sfxPool = new();
	private Dictionary<string, AudioStream> _sounds = new();
	private List<AudioStream> SoundList = new();

	protected override void onSingletonReady()
	{

		// 自动加载该路径下所有音频
		ResourceGroup.Of(Assets.Sound_Rse).LoadAllInto(SoundList);
		ConvertListToDictionarySafe();
		// 初始化音乐播放器
		_musicPlayerA = CreatePlayer("MusicPlayerA", "Music");
		_musicPlayerB = CreatePlayer("MusicPlayerB", "Music");
		_activePlayer = _musicPlayerA;

		// 初始化音效池
		for (int i = 0; i < SfxPoolSize; i++)
		{
			_sfxPool.Add(CreatePlayer($"SFXPlayer_{i}", "SFX"));
		}
		// LogUtil.Info("SoundManager ready" + SoundList[0].ResourcePath);
	}

	private AudioStreamPlayer CreatePlayer(string name, string bus)
	{
		var player = new AudioStreamPlayer { Name = name, Bus = bus };
		AddChild(player);
		return player;
	}

	public void RegisterSound(string name, string path) => _sounds[name] = GD.Load<AudioStream>(path);

	// --- 播放逻辑 ---

	public void PlaySfx(string name)
	{
		if (!_sounds.ContainsKey(name)) return;
		foreach (var player in _sfxPool)
		{
			if (!player.Playing)
			{
				player.Stream = _sounds[name];
				player.Play();
				return;
			}
		}
	}

	public void PlayMusicFade(string name, float targetVolumeDb = 0f)
	{
		if (!_sounds.ContainsKey(name)) return;
		AudioStream nextStream = _sounds[name];
		if (_activePlayer.Playing && _activePlayer.Stream == nextStream) return;

		AudioStreamPlayer oldPlayer = _activePlayer;
		AudioStreamPlayer newPlayer = (_activePlayer == _musicPlayerA) ? _musicPlayerB : _musicPlayerA;

		newPlayer.Stream = nextStream;
		newPlayer.VolumeDb = -80f;
		newPlayer.Play();

		Tween tween = CreateTween().SetParallel(true);
		tween.TweenProperty(oldPlayer, "volume_db", -80f, CrossfadeTime);
		tween.TweenProperty(newPlayer, "volume_db", targetVolumeDb, CrossfadeTime);
		tween.Chain().TweenCallback(Callable.From(oldPlayer.Stop));

		_activePlayer = newPlayer;
	}

	// --- 停止功能 ---

	public void StopMusic(bool fadeOut = true)
	{
		if (!fadeOut)
		{
			_activePlayer.Stop();
			return;
		}
		Tween tween = CreateTween();
		tween.TweenProperty(_activePlayer, "volume_db", -80f, CrossfadeTime);
		tween.TweenCallback(Callable.From(_activePlayer.Stop));
	}

	public void StopAllSfx()
	{
		foreach (var player in _sfxPool) player.Stop();
	}

	// --- 音量控制 (0.0 到 1.0) ---

	public void SetVolume(string busName, float volumeLinear)
	{
		int busIndex = AudioServer.GetBusIndex(busName);

		if (busIndex == -1)
		{
			LogUtil.Warning($"[AudioError] 找不到名为 '{busName}' 的音轨总线！请检查 Audio 面板。");
			return;
		}

		float db = Mathf.LinearToDb(volumeLinear);
		AudioServer.SetBusVolumeDb(busIndex, db);
		AudioServer.SetBusMute(busIndex, volumeLinear <= 0.001f);
	}

	public void ConvertListToDictionarySafe()
	{
		_sounds.Clear(); // 转换前先清空旧数据

		foreach (var stream in SoundList)
		{
			if (stream == null) continue;

			string key = stream.ResourcePath.GetFile().GetBaseName();

			if (!_sounds.ContainsKey(key))
			{
				_sounds.Add(key, stream);
			}
			else
			{
				LogUtil.Warning($"[SoundManager] 发现重复的文件名: {key}，路径: {stream.ResourcePath}。已跳过。");
			}
		}
	}
}

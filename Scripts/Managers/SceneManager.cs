using Godot;
using System;
using System.Threading.Tasks;
public partial class SceneManager : CanvasLayer
{
	// [Export] public AnimationPlayer AnimPlayer;
	[Export] public ProgressBar LoadingBar;
	[Export] public PanelContainer LoadingContainer;
	[Export] public ColorRect FadeAnimation;

	public static SceneManager Instance { get; private set; }

	public override void _Ready()
	{
		Layer = 10;
		Instance = this;
		LoadingContainer.Visible = false;

		// 初始设为透明并隐藏，防止挡住主菜单
		FadeAnimation.Modulate = new Color(FadeAnimation.Modulate, 0.0f);
		FadeAnimation.Visible = false;
		FadeAnimation.MouseFilter = Control.MouseFilterEnum.Ignore;

	}

	/// <summary>
	/// 异步切换场景并显示进度条
	/// </summary>
	public async Task ChangeScene(string path)
	{
		await FadeOut(2);
		ResourceLoader.LoadThreadedRequest(path);
		LoadingContainer.Visible = true;
		LoadingBar.Value = 0;

		var progress = new Godot.Collections.Array();
		while (ResourceLoader.LoadThreadedGetStatus(path, progress) == ResourceLoader.ThreadLoadStatus.InProgress)
		{
			// progress[0] 是 0.0 到 1.0 的浮点数
			LoadingBar.Value = (double)progress[0] * 100;

			await DefaultTimeout(); // 等待一帧
		}

		var newSceneResource = (PackedScene)ResourceLoader.LoadThreadedGet(path);
		GetTree().ChangeSceneToPacked(newSceneResource);

		LoadingContainer.Visible = false;
		await FadeIn(2);
	}

	private SignalAwaiter DefaultTimeout() => ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

	// 淡入（变透明）：显示场景
	async Task FadeIn(float duration = 1.0f)
	{
		FadeAnimation.Visible = true;
		FadeAnimation.Modulate = new Color(FadeAnimation.Modulate, 1.0f);
		Tween tween = CreateTween();
		tween.TweenProperty(FadeAnimation, "modulate:a", 0.0f, duration);
		await ToSignal(tween, Tween.SignalName.Finished);
		FadeAnimation.Visible = false;
		GetTree().Paused = false;
	}

	// 淡出（变不透明）：遮盖场景
	async Task FadeOut(float duration = 1.0f)
	{
		GetTree().Paused = true;
		FadeAnimation.Visible = true;
		FadeAnimation.Modulate = new Color(FadeAnimation.Modulate, 0.0f);
		Tween tween = CreateTween();
		tween.TweenProperty(FadeAnimation, "modulate:a", 1.0f, duration);
		await ToSignal(tween, Tween.SignalName.Finished);
	}
}

using Godot;
using System;

public partial class EnhanceSelector : VBoxContainer
{
	[Export] public Label Title;
	[Export] public TextureRect Icon;
	[Export] public Label Description;
	[Export] public Button Selected;
	public BaseEnhanceFunction EnhanceFunctionEntity;

	public override void _Ready()
	{
		if (GameConfigManager.Instance != null)
		{
			GameConfigManager.Instance.LanguageChanged += onLanguageChanged;
		}
	}

	public override void _ExitTree()
	{
		if (GameConfigManager.Instance != null)
		{
			GameConfigManager.Instance.LanguageChanged -= onLanguageChanged;
		}
	}

	public void UpdateContents(BaseEnhanceFunction entity)
	{
		EnhanceFunctionEntity = entity;
		refreshLocalizedContents();
		Icon.Texture = entity.Icon;
	}
	public void UpdateContents(string title, string describe, Texture2D icon)
	{
		Title.Text = title;
		Description.Text = describe;
		Title.TooltipText = title;
		Description.TooltipText = describe;
		Icon.Texture = icon;
	}

	private void onLanguageChanged(string locale)
	{
		refreshLocalizedContents();
	}

	private void refreshLocalizedContents()
	{
		if (EnhanceFunctionEntity == null || !IsInstanceValid(EnhanceFunctionEntity))
		{
			return;
		}

		string title = getLocalizedText(EnhanceFunctionEntity.NameKey, EnhanceFunctionEntity.Name);
		string description = getLocalizedText(EnhanceFunctionEntity.DescriptionKey, EnhanceFunctionEntity.Description);
		Title.Text = title;
		Description.Text = description;
		Title.TooltipText = title;
		Description.TooltipText = description;
	}

	private static string getLocalizedText(StringName key, string fallback)
	{
		return key == default ? fallback : TranslationServer.Translate(key);
	}
}

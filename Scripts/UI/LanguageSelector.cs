using Godot;
using System;
using System.Collections.Generic;

public partial class LanguageSelector : OptionButton
{
	// 存储语言 ID 的列表，方便与索引对应
	private List<string> Locales = new List<string> { "zh_CN", "en" };

	public override void _Ready()
	{
		ItemSelected += OnLanguageSelected;

		UpdateOptionTexts();

		string currentLocale = TranslationServer.GetLocale();
		Selected = Locales.IndexOf(currentLocale);

	}

	private void UpdateOptionTexts()
	{
		int currentSelected = Selected;
		Clear();
		AddItem(TranslationServer.Translate("UI_SETTINGS_LANGUAGE_CHINESE"), 0);
		AddItem(TranslationServer.Translate("UI_SETTINGS_LANGUAGE_ENGLISH"), 1);

		Selected = currentSelected;
	}

	private void OnLanguageSelected(long index)
	{
		string targetLocale = Locales[(int)index];
		GameConfigManager.Instance.SetLanguage(targetLocale);

		UpdateOptionTexts();


	}
}

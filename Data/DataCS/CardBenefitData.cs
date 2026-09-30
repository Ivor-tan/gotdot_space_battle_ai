using Godot;

[GlobalClass]
public partial class CardBenefitData : GameResource
{
    [ExportCategory("Identity")]
    [Export] public StringName CardId;
    [Export] public StringName CategoryId;
    [Export] public StringName RarityId;
    [Export] public string DisplayName = "";
    [ExportCategory("Localization")]
    [Export] public StringName DisplayNameKey;
    [Export] public StringName EffectDescriptionKey;
    [Export] public StringName SynergyDescriptionKey;

    [ExportCategory("Design Reference")]
    [Export(PropertyHint.MultilineText)] public string EffectDescription = "";
    [Export(PropertyHint.MultilineText)] public string SynergyDescription = "";
    [Export] public StringName TargetScopeId;
    [Export] public bool RequiresTargetSelection;
    [Export] public bool IsImplementedInRuntime;
}

using Godot;

[GlobalClass]
public partial class BondDefinitionData : GameResource
{
    [Export] public StringName BondId;
    [Export] public string DisplayName = "";
    [ExportCategory("Localization")]
    [Export] public StringName DisplayNameKey;
    [Export] public StringName MemberConditionKey;
    [Export] public StringName EffectDescriptionKey;
    [Export] public StringName CounterplayKey;
    [ExportCategory("Design Reference")]
    [Export(PropertyHint.MultilineText)] public string MemberCondition = "";
    [Export(PropertyHint.MultilineText)] public string EffectDescription = "";
    [Export(PropertyHint.MultilineText)] public string Counterplay = "";
    [Export] public int RequiredMemberCount = 2;
}

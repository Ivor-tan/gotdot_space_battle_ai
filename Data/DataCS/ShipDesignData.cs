using Godot;

[GlobalClass]
public partial class ShipDesignData : GameResource
{
    [ExportCategory("Identity")]
    [Export] public StringName ShipId;
    [Export] public PlayerShipData ShipData;
    [Export] public string DisplayName = "";
    [Export] public StringName DisplayNameKey;
    [Export] public Texture2D PreviewIcon;
    [Export] public Texture2D ProjectileIcon;

    [ExportCategory("In-Run Growth")]
    [Export] public int MaxStarLevel = 4;
    [Export] public Godot.Collections.Array<float> StarDamageMultipliers = new() { 1.0f, 1.35f, 1.8f, 2.35f };
    [Export] public Godot.Collections.Array<float> StarAttackIntervalMultipliers = new() { 1.0f, 0.93f, 0.86f, 0.8f };
    [Export] public Godot.Collections.Array<float> StarPassiveMultipliers = new() { 1.0f, 1.15f, 1.35f, 1.6f };
    [Export] public StringName CenterRingEffectId;
    [Export] public StringName RingOneEffectId;
    [Export] public StringName RingTwoEffectId;
    [Export(PropertyHint.MultilineText)] public string CenterRingEffect = "";
    [Export] public StringName CenterRingEffectKey;
    [Export(PropertyHint.MultilineText)] public string RingOneEffect = "";
    [Export(PropertyHint.MultilineText)] public string RingTwoEffect = "";

    [ExportCategory("Out-of-Run Upgrade Tree")]
    [Export] public Godot.Collections.Array<StringName> BranchIds = new();
    [Export(PropertyHint.MultilineText)] public string BranchOneDescription = "";
    [Export] public StringName BranchOneDescriptionKey;
    [Export(PropertyHint.MultilineText)] public string BranchTwoDescription = "";
    [Export] public StringName BranchTwoDescriptionKey;
    [Export(PropertyHint.MultilineText)] public string BranchThreeDescription = "";
    [Export] public StringName BranchThreeDescriptionKey;
}

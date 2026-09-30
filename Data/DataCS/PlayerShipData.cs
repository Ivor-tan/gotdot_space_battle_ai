using Godot;

[GlobalClass]
public partial class PlayerShipData : GameResource
{
    [ExportCategory("Identity")]
    [Export] public StringName ShipId;
    [Export] public StringName ShipTypeId = "command";
    [Export] public StringName FactionId = "quantum";
    [Export] public StringName RoleId = "command";
    [Export] public StringName RarityId = "basic";
    [Export] public string DisplayName = "";
    [Export(PropertyHint.MultilineText)] public string Description = "";
    [Export] public StringName DisplayNameKey;
    [Export] public StringName DescriptionKey;
    [Export] public Texture2D Icon;
    [Export] public PackedScene ShipScene;
    [Export] public StringName RecommendedPositionId = "middle";

    [ExportCategory("Flight and Combat Stats")]
    [Export] public float RotateSpeed = 5.0f;
    [Export] public float AttackRange = 150.0f;
    [Export] public float AttackInterval = 1.5f;
    [Export] public int AttackPower = 10;

    [ExportCategory("Abilities and Progression")]
    [Export] public StringName WeaponId;
    [Export] public StringName ActiveAbilityId;
    [Export] public StringName PassiveAbilityId;
    [Export] public StringName UnlockSourceId;
    [Export] public int UnlockLevel;
    [Export] public int DeployCost = 1;
    [Export] public Godot.Collections.Array<StringName> TagIds = new();
    [Export] public Godot.Collections.Array<StringName> TraitIds = new();
    [Export] public Godot.Collections.Array<StringName> BondIds = new();

    /// <summary>Uses the configured name unless a valid localization key supplies an override.</summary>
    public string getDisplayName()
    {
        string fallback = string.IsNullOrWhiteSpace(DisplayName) ? ShipId.ToString() : DisplayName;
        if (DisplayNameKey == default)
        {
            return fallback;
        }

        string keyText = DisplayNameKey.ToString();
        if (string.IsNullOrWhiteSpace(keyText))
        {
            return fallback;
        }

        string translated = TranslationServer.Translate(DisplayNameKey);
        return string.IsNullOrWhiteSpace(translated) || translated == keyText ? fallback : translated;
    }
}

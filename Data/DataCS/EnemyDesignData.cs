using Godot;

[GlobalClass]
public partial class EnemyDesignData : GameResource
{
    [ExportCategory("Identity")]
    [Export] public StringName EnemyId;
    [Export] public string DisplayName = "";
    [ExportCategory("Localization")]
    [Export] public StringName DisplayNameKey;
    [Export] public StringName TargetAndAttackKey;
    [Export] public StringName MechanicAndWarningKey;
    [Export] public StringName SpawnRuleKey;
    [Export] public StringName DropSummaryKey;
    [ExportCategory("Identity")]
    [Export] public StringName FactionId = "ABR";
    [Export] public StringName TierId = "NRM";
    [Export] public StringName RoleId;
    [Export] public Texture2D Icon;
    [Export] public PackedScene EnemyScene;

    [ExportCategory("Core Stats")]
    [Export] public int BaseHealth;
    [Export] public int EnemyShieldLayers;
    [Export] public float CollisionRadius;
    [Export] public float MoveSpeed;
    [Export] public float AttackRange;
    [Export] public float WindupSeconds;
    [Export] public float CooldownSeconds;
    [Export] public int DisplayDamage;
    [Export] public int EffectiveHits;
    [Export(PropertyHint.Range, "0,1,0.01")] public float ControlResistance;

    [ExportCategory("Spawn and Drops")]
    [Export] public int SpawnCap;
    [Export] public float ThreatWeight;
    [Export] public StringName DropTableId;
    [Export(PropertyHint.MultilineText)] public string DropSummary = "";

    [ExportCategory("Behavior Reference")]
    [Export(PropertyHint.MultilineText)] public string TargetAndAttack = "";
    [Export(PropertyHint.MultilineText)] public string MechanicAndWarning = "";
    [Export(PropertyHint.MultilineText)] public string SpawnRule = "";
    [Export(PropertyHint.MultilineText)] public string PhaseOne = "";
    [Export(PropertyHint.MultilineText)] public string PhaseTwo = "";
    [Export(PropertyHint.MultilineText)] public string PhaseThree = "";
}

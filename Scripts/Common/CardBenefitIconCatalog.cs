using Godot;

/// <summary>Provides shared category icons for benefit cards and fleet bond entries.</summary>
public static class CardBenefitIconCatalog
{
    private const string ShieldIconPath = "res://Resource/Icons/CardBenefits/card_shield.png";
    private const string WeaponIconPath = "res://Resource/Icons/CardBenefits/card_weapon.png";
    private const string EngineIconPath = "res://Resource/Icons/CardBenefits/card_engine.png";
    private const string FleetIconPath = "res://Resource/Icons/CardBenefits/card_fleet.png";
    private const string SystemIconPath = "res://Resource/Icons/CardBenefits/card_system.png";
    private const string BondIconPath = "res://Resource/Icons/CardBenefits/bond_synergy.png";
    private const string RapidFireIconPath = "res://Resource/Icons/CardBenefits/detail_rapid_fire.png";
    private const string CriticalIconPath = "res://Resource/Icons/CardBenefits/detail_critical.png";
    private const string MissileIconPath = "res://Resource/Icons/CardBenefits/detail_missile.png";
    private const string RangeIconPath = "res://Resource/Icons/CardBenefits/detail_range.png";
    private const string ArmorIconPath = "res://Resource/Icons/CardBenefits/detail_armor.png";
    private const string RepairIconPath = "res://Resource/Icons/CardBenefits/detail_repair.png";
    private const string SpeedIconPath = "res://Resource/Icons/CardBenefits/utility_speed.png";
    private const string RecruitmentIconPath = "res://Resource/Icons/CardBenefits/utility_recruit.png";
    private const string FormationIconPath = "res://Resource/Icons/CardBenefits/utility_formation.png";
    private const string ControlIconPath = "res://Resource/Icons/CardBenefits/utility_control.png";
    private const string PierceIconPath = "res://Resource/Icons/CardBenefits/combat_pierce.png";
    private const string SplashIconPath = "res://Resource/Icons/CardBenefits/combat_splash.png";
    private const string SiphonIconPath = "res://Resource/Icons/CardBenefits/combat_siphon.png";
    private const string OverloadIconPath = "res://Resource/Icons/CardBenefits/combat_overload.png";
    private const string GuardIconPath = "res://Resource/Icons/CardBenefits/combat_guard.png";
    private const string EvasionIconPath = "res://Resource/Icons/CardBenefits/combat_evasion.png";

    private static readonly Texture2D ShieldIcon = GD.Load<Texture2D>(ShieldIconPath);
    private static readonly Texture2D WeaponIcon = GD.Load<Texture2D>(WeaponIconPath);
    private static readonly Texture2D EngineIcon = GD.Load<Texture2D>(EngineIconPath);
    private static readonly Texture2D FleetIcon = GD.Load<Texture2D>(FleetIconPath);
    private static readonly Texture2D SystemIcon = GD.Load<Texture2D>(SystemIconPath);
    private static readonly Texture2D BondIcon = GD.Load<Texture2D>(BondIconPath);
    private static readonly Texture2D RapidFireIcon = GD.Load<Texture2D>(RapidFireIconPath);
    private static readonly Texture2D CriticalIcon = GD.Load<Texture2D>(CriticalIconPath);
    private static readonly Texture2D MissileIcon = GD.Load<Texture2D>(MissileIconPath);
    private static readonly Texture2D RangeIcon = GD.Load<Texture2D>(RangeIconPath);
    private static readonly Texture2D ArmorIcon = GD.Load<Texture2D>(ArmorIconPath);
    private static readonly Texture2D RepairIcon = GD.Load<Texture2D>(RepairIconPath);
    private static readonly Texture2D SpeedIcon = GD.Load<Texture2D>(SpeedIconPath);
    private static readonly Texture2D RecruitmentIcon = GD.Load<Texture2D>(RecruitmentIconPath);
    private static readonly Texture2D FormationIcon = GD.Load<Texture2D>(FormationIconPath);
    private static readonly Texture2D ControlIcon = GD.Load<Texture2D>(ControlIconPath);
    private static readonly Texture2D PierceIcon = GD.Load<Texture2D>(PierceIconPath);
    private static readonly Texture2D SplashIcon = GD.Load<Texture2D>(SplashIconPath);
    private static readonly Texture2D SiphonIcon = GD.Load<Texture2D>(SiphonIconPath);
    private static readonly Texture2D OverloadIcon = GD.Load<Texture2D>(OverloadIconPath);
    private static readonly Texture2D GuardIcon = GD.Load<Texture2D>(GuardIconPath);
    private static readonly Texture2D EvasionIcon = GD.Load<Texture2D>(EvasionIconPath);

    public static Texture2D GetCardIcon(StringName categoryId, StringName cardId)
    {
        int variant = getVariant(cardId);
        return categoryId.ToString() switch
        {
            "wpn" => (variant % 6) switch { 0 => RapidFireIcon, 1 => CriticalIcon, 2 => MissileIcon, 3 => PierceIcon, 4 => SplashIcon, _ => OverloadIcon },
            "sig" => variant switch { 0 => EngineIcon, 1 => SpeedIcon, _ => ControlIcon },
            "flt" => variant switch { 0 => FleetIcon, 1 => FormationIcon, _ => RecruitmentIcon },
            "trt" => (variant % 6) switch { 0 => ShieldIcon, 1 => ArmorIcon, 2 => RepairIcon, 3 => GuardIcon, 4 => EvasionIcon, _ => SiphonIcon },
            _ => variant switch { 0 => SystemIcon, 1 => ControlIcon, _ => RepairIcon }
        };
    }

    public static Texture2D GetBondIcon()
    {
        return BondIcon;
    }

    private static int getVariant(StringName cardId)
    {
        string id = cardId.ToString();
        if (id.Length >= 3 && int.TryParse(id.Substring(id.Length - 3), out int value))
        {
            return value % 6;
        }

        return 0;
    }
}

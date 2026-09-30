using Godot;

/// <summary>
/// Resolves the shared pixel-art icon and tier accent for every ship upgrade branch.
/// The branch ID is stable design data, so runtime offers and the meta tree stay visually aligned.
/// </summary>
public static class ShipUpgradeIconCatalog
{
    private const string BeamIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_beam.png";
    private const string ShockwaveIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_shockwave.png";
    private const string ShieldIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_shield.png";
    private const string RepairIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_repair.png";
    private const string GravityIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_gravity.png";
    private const string CommandIconPath = "res://Resource/Icons/ShipUpgrades/upgrade_command.png";

    private static readonly Texture2D BeamIcon = GD.Load<Texture2D>(BeamIconPath);
    private static readonly Texture2D ShockwaveIcon = GD.Load<Texture2D>(ShockwaveIconPath);
    private static readonly Texture2D ShieldIcon = GD.Load<Texture2D>(ShieldIconPath);
    private static readonly Texture2D RepairIcon = GD.Load<Texture2D>(RepairIconPath);
    private static readonly Texture2D GravityIcon = GD.Load<Texture2D>(GravityIconPath);
    private static readonly Texture2D CommandIcon = GD.Load<Texture2D>(CommandIconPath);

    public static Texture2D GetIcon(ShipUpgradeNodeData node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return CommandIcon;
        }

        return node.BranchId.ToString() switch
        {
            "ULK_UPG_ASS_HNT" or "ULK_UPG_FLG_PRC" or "ULK_UPG_FRS_COL" or "ULK_UPG_PRM_RAY" or "ULK_UPG_SOL_FUR" or "ULK_UPG_SOL_SIE" => BeamIcon,
            "ULK_UPG_ASS_IMP" or "ULK_UPG_ASS_SHK" or "ULK_UPG_FLG_EXP" or "ULK_UPG_SOL_FLR" or "ULK_UPG_STM_OVR" or "ULK_UPG_TID_WAV" => ShockwaveIcon,
            "ULK_UPG_FLD_SHD" or "ULK_UPG_HIV_GRD" or "ULK_UPG_IRN_COV" or "ULK_UPG_IRN_FRN" or "ULK_UPG_PRM_GUA" => ShieldIcon,
            "ULK_UPG_FLD_RES" or "ULK_UPG_HIV_HNT" or "ULK_UPG_HIV_SWM" or "ULK_UPG_PRM_BST" => RepairIcon,
            "ULK_UPG_FRS_SHR" or "ULK_UPG_FRS_ZON" or "ULK_UPG_TID_GRV" => GravityIcon,
            "ULK_UPG_FLD_RLY" or "ULK_UPG_FLG_FRM" or "ULK_UPG_IRN_CNT" or "ULK_UPG_STM_CHN" or "ULK_UPG_STM_NET" or "ULK_UPG_TID_RLY" => CommandIcon,
            _ => CommandIcon
        };
    }

    public static Color GetTierTint(int tier)
    {
        return tier switch
        {
            1 => Colors.White,
            2 => new Color(0.42f, 0.88f, 1.0f),
            3 => new Color(1.0f, 0.78f, 0.3f),
            _ => Colors.White
        };
    }
}

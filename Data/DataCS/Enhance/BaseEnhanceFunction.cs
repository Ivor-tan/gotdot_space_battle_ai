using Godot;
using Godot.Collections;
using GodotResourceGroups;

[GlobalClass]
public partial class BaseEnhanceFunction : GameResource
{
    [Export] public string Name;
    [Export] public string Description;
    [ExportCategory("Localization")]
    [Export] public StringName NameKey;
    [Export] public StringName DescriptionKey;
    [Export] public Texture2D Icon;
    [Export] public EnhanceFunctionType FunctionType;

    [ExportCategory("Random Selection")]
    // Allows test, debug, or unfinished resources to stay out of the level-up pool.
    [Export] public bool IsRandomlyAvailable = true;
    // Permanent functions can opt out after they have been selected once in this run.
    [Export] public bool CanRepeat = true;
    [Export(PropertyHint.Range, "0.01,10,0.01")] public float SelectionWeight = 1.0f;

    [ExportCategory("Target Ship")]
    [Export] public bool AppliesToAllShips = true;
    [Export] public PlayerShipData TargetShipData;
    [Export] public StringName TargetShipId;

    private static readonly Array<PlayerShipData> _playerShipData = new();
    private static bool _hasLoadedPlayerShipData;

    public virtual void ApplyEffect()
    {

    }

    /// <summary>
    /// Resolves the ship resource configured by the Inspector reference or stable ship ID.
    /// </summary>
    public bool tryResolveTargetShipData(out PlayerShipData targetShipData)
    {
        return tryResolveTargetShipData(out targetShipData, true);
    }

    /// <summary>Resolves a configured target without emitting selection-time warnings.</summary>
    public bool tryResolveTargetShipDataSilently(out PlayerShipData targetShipData)
    {
        return tryResolveTargetShipData(out targetShipData, false);
    }

    private bool tryResolveTargetShipData(out PlayerShipData targetShipData, bool logFailure)
    {
        targetShipData = null;
        if (TargetShipData != null && IsInstanceValid(TargetShipData))
        {
            targetShipData = TargetShipData;
            return true;
        }

        if (TargetShipId == default)
        {
            return false;
        }

        loadPlayerShipData();
        foreach (PlayerShipData shipData in _playerShipData)
        {
            if (shipData != null && IsInstanceValid(shipData) && shipData.ShipId == TargetShipId)
            {
                targetShipData = shipData;
                return true;
            }
        }

        if (logFailure)
        {
            LogUtil.Warning($"增益 {ID} 未找到目标飞船数据：{TargetShipId}");
        }
        targetShipData = null;
        return false;
    }

    /// <summary>
    /// Allows selection UIs and derived effects to filter this benefit against a ship.
    /// </summary>
    public bool canApplyToShip(PlayerShipData shipData)
    {
        if (AppliesToAllShips)
        {
            return shipData != null && IsInstanceValid(shipData);
        }

        if (shipData == null || !IsInstanceValid(shipData))
        {
            return false;
        }

        if (tryResolveTargetShipDataSilently(out PlayerShipData targetShipData))
        {
            return shipData.ShipId == targetShipData.ShipId;
        }

        return TargetShipId != default && shipData.ShipId == TargetShipId;
    }

    private static void loadPlayerShipData()
    {
        if (_hasLoadedPlayerShipData)
        {
            return;
        }

        _hasLoadedPlayerShipData = true;
        Resource shipDataGroup = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (shipDataGroup == null || !IsInstanceValid(shipDataGroup))
        {
            LogUtil.Warning($"未找到飞船资源组：{Assets.Player_Ship_Data}");
            return;
        }

        ResourceGroup.Of(shipDataGroup).LoadAllInto(_playerShipData);
    }
}

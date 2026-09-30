using Godot;
using Godot.Collections;
using GodotResourceGroups;

/// <summary>
/// Stores the selections for the next run while transitioning from the main menu.
/// </summary>
public static class RunConfiguration
{
    private const string SavePath = "user://last_run_configuration.cfg";
    private const string Section = "last_run";
    private const string ShipIdKey = "initial_ship_id";
    private const string DifficultyKey = "difficulty_index";
    private const string WaveCountKey = "enemy_wave_count";
    private const string InfiniteModeKey = "is_infinite_mode";

    public static PlayerShipData SelectedInitialShip { get; private set; }
    public static int DifficultyIndex { get; private set; } = 1;
    public static int EnemyWaveCount { get; private set; } = 10;
    public static bool IsInfiniteMode { get; private set; }

    public static void Configure(PlayerShipData selectedInitialShip, int difficultyIndex, int enemyWaveCount, bool isInfiniteMode = false)
    {
        SelectedInitialShip = selectedInitialShip;
        DifficultyIndex = Mathf.Clamp(difficultyIndex, 0, 2);
        IsInfiniteMode = isInfiniteMode;
        // Infinite mode uses the existing ten-wave encounter loop as its current runtime unit.
        EnemyWaveCount = isInfiniteMode ? 10 : Mathf.Clamp(enemyWaveCount, 4, 60);
        saveLastConfiguration();
    }

    /// <summary>
    /// Restores the last valid main-menu setup. Runtime battle progress is not saved;
    /// continuing starts a new run with the remembered ship and rules.
    /// </summary>
    public static bool tryLoadLastConfiguration()
    {
        ConfigFile config = new();
        if (config.Load(SavePath) != Error.Ok)
        {
            return false;
        }

        StringName shipId = config.GetValue(Section, ShipIdKey, default(StringName)).AsStringName();
        PlayerShipData shipData = findShipData(shipId);
        if (shipData == null || !GodotObject.IsInstanceValid(shipData))
        {
            LogUtil.Warning("Last-run configuration cannot continue because its initial ship is unavailable.");
            return false;
        }

        Configure(
            shipData,
            Mathf.Clamp((int)config.GetValue(Section, DifficultyKey, 1), 0, 2),
            Mathf.Clamp((int)config.GetValue(Section, WaveCountKey, 10), 4, 60),
            (bool)config.GetValue(Section, InfiniteModeKey, false));
        return true;
    }

    public static bool hasLastConfiguration()
    {
        if (!FileAccess.FileExists(SavePath))
        {
            return false;
        }

        ConfigFile config = new();
        return config.Load(SavePath) == Error.Ok &&
            config.GetValue(Section, ShipIdKey, default(StringName)).AsStringName() != default;
    }

    private static void saveLastConfiguration()
    {
        if (SelectedInitialShip == null || !GodotObject.IsInstanceValid(SelectedInitialShip))
        {
            return;
        }

        ConfigFile config = new();
        config.SetValue(Section, ShipIdKey, SelectedInitialShip.ShipId);
        config.SetValue(Section, DifficultyKey, DifficultyIndex);
        config.SetValue(Section, WaveCountKey, EnemyWaveCount);
        config.SetValue(Section, InfiniteModeKey, IsInfiniteMode);
        Error error = config.Save(SavePath);
        if (error != Error.Ok)
        {
            LogUtil.Error($"Failed to save last-run configuration: {error}");
        }
    }

    private static PlayerShipData findShipData(StringName shipId)
    {
        if (shipId == default)
        {
            return null;
        }

        Resource group = GD.Load<Resource>(Assets.Player_Ship_Data);
        if (group == null || !GodotObject.IsInstanceValid(group))
        {
            LogUtil.Warning("Player ship data group is unavailable while loading the last-run configuration.");
            return null;
        }

        Array<PlayerShipData> ships = new();
        ResourceGroup.Of(group).LoadAllInto(ships);
        foreach (PlayerShipData ship in ships)
        {
            if (ship != null && GodotObject.IsInstanceValid(ship) && ship.ShipId == shipId)
            {
                return ship;
            }
        }

        return null;
    }
}

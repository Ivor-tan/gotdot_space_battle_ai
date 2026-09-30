using Godot;

/// <summary>
/// Persists non-combat progression between runs. Profile experience determines
/// permanent profile level, while map experience is the spendable node currency.
/// </summary>
public static class MetaProgressStore
{
    private const string SavePath = "user://meta_progress.cfg";
    private const string Section = "profile";
    private const string ExperienceKey = "map_experience";
    private const string ProfileExperienceKey = "profile_experience";
    private const string UnlockedNodeIdsKey = "unlocked_ship_upgrade_node_ids";
    private const string EliteDefeatsKeyPrefix = "ship_elite_defeats_";
    private const string BossVictoriesKeyPrefix = "ship_boss_victories_";
    private const int DebugExperience = 1_000_000;
    private const int DebugProfileLevel = 99;
    private static readonly int[] ProfileLevelRequirements = { 40, 60, 80, 110, 140, 170, 210, 250, 300, 360 };

    public static int GetExperience()
    {
        if (IsProgressionDebugEnabled())
        {
            return DebugExperience;
        }

        ConfigFile config = new();
        Error error = config.Load(SavePath);
        if (error != Error.Ok)
        {
            return 0;
        }

        return Mathf.Max(0, (int)config.GetValue(Section, ExperienceKey, 0));
    }

    public static int AddExperience(int amount)
    {
        ConfigFile config = loadConfig();
        int earnedExperience = Mathf.Max(0, amount);
        int total = Mathf.Max(0, (int)config.GetValue(Section, ExperienceKey, 0)) + earnedExperience;
        int profileExperience = getProfileExperience(config) + earnedExperience;
        config.SetValue(Section, ExperienceKey, total);
        config.SetValue(Section, ProfileExperienceKey, profileExperience);
        Error error = config.Save(SavePath);
        if (error != Error.Ok)
        {
            LogUtil.Error($"Failed to save meta progression: {error}");
        }

        return total;
    }

    public static int GetProfileLevel()
    {
        if (IsProgressionDebugEnabled())
        {
            return DebugProfileLevel;
        }

        int experience = getProfileExperience(loadConfig());
        int level = 1;
        int requiredTotal = 0;
        for (int index = 0; index < ProfileLevelRequirements.Length; index++)
        {
            requiredTotal += ProfileLevelRequirements[index];
            if (experience < requiredTotal)
            {
                return level;
            }

            level++;
        }

        int overflowExperience = Mathf.Max(0, experience - requiredTotal);
        return level + overflowExperience / 430;
    }

    public static int GetExperienceToNextProfileLevel()
    {
        if (IsProgressionDebugEnabled())
        {
            return 0;
        }

        int experience = getProfileExperience(loadConfig());
        int requiredTotal = 0;
        for (int index = 0; index < ProfileLevelRequirements.Length; index++)
        {
            requiredTotal += ProfileLevelRequirements[index];
            if (experience < requiredTotal)
            {
                return requiredTotal - experience;
            }
        }

        return 430 - (experience - requiredTotal) % 430;
    }

    public static bool IsShipUpgradeNodeUnlocked(StringName nodeId)
    {
        if (nodeId == default)
        {
            return false;
        }

        if (IsProgressionDebugEnabled())
        {
            return true;
        }

        return getUnlockedNodeIds().Contains(nodeId.ToString());
    }

    public static bool IsProgressionDebugEnabled()
    {
        GameConfigManager configManager = GameConfigManager.Instance;
        return configManager != null && GodotObject.IsInstanceValid(configManager) && configManager.IsProgressionDebugEnabled;
    }

    public static int GetShipUpgradeCost(int tier)
    {
        return tier switch
        {
            1 => 20,
            2 => 45,
            _ => 80
        };
    }

    public static bool TryUnlockShipUpgradeNode(StringName nodeId, int cost)
    {
        if (nodeId == default || cost < 0 || IsShipUpgradeNodeUnlocked(nodeId))
        {
            return false;
        }

        ConfigFile config = loadConfig();
        int experience = Mathf.Max(0, (int)config.GetValue(Section, ExperienceKey, 0));
        if (experience < cost)
        {
            return false;
        }

        string nodeIds = config.GetValue(Section, UnlockedNodeIdsKey, "").AsString();
        string updatedNodeIds = string.IsNullOrEmpty(nodeIds) ? nodeId.ToString() : $"{nodeIds}\n{nodeId}";
        config.SetValue(Section, UnlockedNodeIdsKey, updatedNodeIds);
        config.SetValue(Section, ProfileExperienceKey, getProfileExperience(config));
        config.SetValue(Section, ExperienceKey, experience - cost);
        return saveConfig(config);
    }

    public static int GetShipEliteDefeats(StringName shipId)
    {
        return getShipProgress(shipId, EliteDefeatsKeyPrefix);
    }

    public static int GetShipBossVictories(StringName shipId)
    {
        return getShipProgress(shipId, BossVictoriesKeyPrefix);
    }

    public static void RecordShipEncounterProgress(StringName shipId, int eliteDefeats, int bossVictories)
    {
        if (shipId == default || (eliteDefeats <= 0 && bossVictories <= 0))
        {
            return;
        }

        ConfigFile config = loadConfig();
        if (eliteDefeats > 0)
        {
            string key = $"{EliteDefeatsKeyPrefix}{shipId}";
            config.SetValue(Section, key, Mathf.Max(0, (int)config.GetValue(Section, key, 0)) + eliteDefeats);
        }

        if (bossVictories > 0)
        {
            string key = $"{BossVictoriesKeyPrefix}{shipId}";
            config.SetValue(Section, key, Mathf.Max(0, (int)config.GetValue(Section, key, 0)) + bossVictories);
        }

        saveConfig(config);
    }

    private static Godot.Collections.Array<string> getUnlockedNodeIds()
    {
        string serializedNodeIds = loadConfig().GetValue(Section, UnlockedNodeIdsKey, "").AsString();
        Godot.Collections.Array<string> nodeIds = new();
        foreach (string nodeId in serializedNodeIds.Split('\n', System.StringSplitOptions.RemoveEmptyEntries))
        {
            nodeIds.Add(nodeId);
        }

        return nodeIds;
    }

    private static int getShipProgress(StringName shipId, string keyPrefix)
    {
        if (shipId == default)
        {
            return 0;
        }

        return Mathf.Max(0, (int)loadConfig().GetValue(Section, $"{keyPrefix}{shipId}", 0));
    }

    private static ConfigFile loadConfig()
    {
        ConfigFile config = new();
        config.Load(SavePath);
        return config;
    }

    private static int getProfileExperience(ConfigFile config)
    {
        // Existing saves had a single balance. Use it as the initial profile
        // total so adding the new spendable balance cannot reduce a profile level.
        return Mathf.Max(0, (int)config.GetValue(
            Section,
            ProfileExperienceKey,
            config.GetValue(Section, ExperienceKey, 0)));
    }

    private static bool saveConfig(ConfigFile config)
    {
        Error error = config.Save(SavePath);
        if (error == Error.Ok)
        {
            return true;
        }

        LogUtil.Error($"Failed to save meta progression: {error}");
        return false;
    }
}

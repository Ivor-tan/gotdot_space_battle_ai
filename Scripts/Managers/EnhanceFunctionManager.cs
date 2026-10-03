using Godot;
using Godot.Collections;
using GodotResourceGroups;

public partial class EnhanceFunctionManager : Singleton<EnhanceFunctionManager>
{
    public Array<BaseEnhanceFunction> EnhanceFunctionList = new();
    private readonly Array<CardBenefitData> _cardBenefits = new();

    // 运行后按类型索引，避免选择增益时重复遍历全部资源。
    private readonly System.Collections.Generic.Dictionary<EnhanceFunctionType, Array<BaseEnhanceFunction>> _enhanceFunctionDict = new();
    private readonly System.Collections.Generic.HashSet<StringName> _acquiredNonRepeatableFunctionIds = new();
    private readonly Array<ShipUpgradeNodeData> _shipUpgradeNodes = new();
    private StringName _pendingRecruitFactionId;
    private StringName _pendingRecruitRoleId;

    protected override void onSingletonReady()
    {
        loadEnhanceFunctions();
        loadCardBenefits();
        ResourceGroup.Of(Assets.Ship_Upgrade_Nodes).LoadAllInto(_shipUpgradeNodes);
        buildFunctionTypeIndex();
    }

    public BaseEnhanceFunction GetEnhanceFunction(int index)
    {
        return EnhanceFunctionList[index];
    }

    /// <summary>
    /// Returns the upgrade nodes loaded for this run. Consumers must treat this
    /// collection as read-only; the manager owns its loading and lifetime.
    /// </summary>
    public Array<ShipUpgradeNodeData> GetShipUpgradeNodes()
    {
        return _shipUpgradeNodes;
    }

    /// <summary>Returns the card benefits loaded from the design resource group.</summary>
    public Array<CardBenefitData> GetCardBenefits()
    {
        return _cardBenefits;
    }

    /// <summary>
    /// Creates a distinct level-up offer. The first pass gives each available function
    /// category one slot, so recruitment, stat boosts, and special functions can coexist.
    /// </summary>
    public Array<BaseEnhanceFunction> getRandomEnhanceFunctions(int desiredCount)
    {
        Array<BaseEnhanceFunction> result = new();
        if (desiredCount <= 0)
        {
            return result;
        }

        addPendingRecruitmentOffer(result);

        System.Collections.Generic.Dictionary<EnhanceFunctionType, Array<BaseEnhanceFunction>> availableByType = new();
        foreach (BaseEnhanceFunction function in EnhanceFunctionList)
        {
            if (!canOffer(function))
            {
                continue;
            }

            if (!availableByType.TryGetValue(function.FunctionType, out Array<BaseEnhanceFunction> functions))
            {
                functions = new Array<BaseEnhanceFunction>();
                availableByType[function.FunctionType] = functions;
            }

            functions.Add(function);
        }

        Array<EnhanceFunctionType> types = new();
        foreach (EnhanceFunctionType type in availableByType.Keys)
        {
            types.Add(type);
        }

        shuffle(types);
        foreach (EnhanceFunctionType type in types)
        {
            if (result.Count >= desiredCount)
            {
                break;
            }

            addWeightedCandidate(availableByType[type], result);
        }

        Array<BaseEnhanceFunction> remaining = new();
        foreach (Array<BaseEnhanceFunction> functions in availableByType.Values)
        {
            foreach (BaseEnhanceFunction function in functions)
            {
                if (!result.Contains(function))
                {
                    remaining.Add(function);
                }
            }
        }

        while (result.Count < desiredCount && remaining.Count > 0)
        {
            BaseEnhanceFunction selected = takeWeightedCandidate(remaining);
            if (selected == null)
            {
                break;
            }

            result.Add(selected);
        }

        return result;
    }

    public void QueueRecruitmentOffer(StringName factionId, StringName roleId = default)
    {
        _pendingRecruitFactionId = factionId;
        _pendingRecruitRoleId = roleId;
    }

    public void registerAcquiredFunction(BaseEnhanceFunction function)
    {
        if (function == null || !IsInstanceValid(function) || function.CanRepeat)
        {
            return;
        }

        _acquiredNonRepeatableFunctionIds.Add(function.ID);
    }

    public BaseEnhanceFunction getShipUpgradeOffer(int playerLevel)
    {
        foreach (ShipUpgradeNodeData node in _shipUpgradeNodes)
        {
            if (node == null || !IsInstanceValid(node) || RunShipUpgradeState.HasNode(node.ShipId, node.NodeId) ||
                !MetaProgressStore.IsShipUpgradeNodeUnlocked(node.NodeId) ||
                !PlayerManager.Instance.hasShip(node.ShipId) ||
                !RunShipUpgradeState.CanSelect(node, playerLevel, out _))
            {
                continue;
            }

            ShipUpgradeEnhanceFunction offer = new();
            offer.Configure(node, playerLevel);
            return offer;
        }

        return null;
    }

    public bool TryGetShipUpgradeNode(StringName nodeId, out ShipUpgradeNodeData shipUpgradeNode)
    {
        foreach (ShipUpgradeNodeData node in _shipUpgradeNodes)
        {
            if (node != null && IsInstanceValid(node) && node.NodeId == nodeId)
            {
                shipUpgradeNode = node;
                return true;
            }
        }

        shipUpgradeNode = null;
        return false;
    }

    private void loadEnhanceFunctions()
    {
        EnhanceFunctionList.Clear();
        ResourceGroup.Of(Assets.Player_Enhance_Data).LoadAllInto(EnhanceFunctionList);

        foreach (BaseEnhanceFunction function in EnhanceFunctionList)
        {
            ensureDisplayContent(function);
        }
    }

    private void loadCardBenefits()
    {
        _cardBenefits.Clear();
        ResourceGroup.Of(Assets.Card_Benefit_Data).LoadAllInto(_cardBenefits);
        foreach (CardBenefitData card in _cardBenefits)
        {
            if (card == null || !IsInstanceValid(card) || !card.IsImplementedInRuntime)
            {
                continue;
            }

            CardBenefitEnhanceFunction cardFunction = new();
            cardFunction.Configure(card);
            EnhanceFunctionList.Add(cardFunction);
        }
        LogUtil.Info($"Loaded {_cardBenefits.Count} card benefits; only verified runtime cards were added to the level-up pool.");
    }

    private void buildFunctionTypeIndex()
    {
        _enhanceFunctionDict.Clear();
        foreach (BaseEnhanceFunction entry in EnhanceFunctionList)
        {
            if (entry == null || !IsInstanceValid(entry))
            {
                continue;
            }

            if (!_enhanceFunctionDict.TryGetValue(entry.FunctionType, out Array<BaseEnhanceFunction> functions))
            {
                functions = new Array<BaseEnhanceFunction>();
                _enhanceFunctionDict[entry.FunctionType] = functions;
            }

            functions.Add(entry);
        }
    }

    private static void ensureDisplayContent(BaseEnhanceFunction function)
    {
        if (function == null || !IsInstanceValid(function))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(function.Name))
        {
            function.Name = $"{getFunctionTypeDisplayName(function.FunctionType)} · {function.ID}";
            LogUtil.Warning($"增益 {function.ID} 未配置名称，已使用运行时兜底文本。");
        }

        if (string.IsNullOrWhiteSpace(function.Description))
        {
            function.Description = "该增益的详细说明尚未配置。";
            LogUtil.Warning($"增益 {function.ID} 未配置描述，已使用运行时兜底文本。");
        }
    }

    private static string getFunctionTypeDisplayName(EnhanceFunctionType functionType)
    {
        return functionType switch
        {
            EnhanceFunctionType.AddCraft => "飞船招募",
            EnhanceFunctionType.ModifySelfValue => "属性强化",
            EnhanceFunctionType.AddictionFunction => "特殊功能",
            _ => "未知增益"
        };
    }

    private bool canOffer(BaseEnhanceFunction function)
    {
        if (function == null || !IsInstanceValid(function) || !function.IsRandomlyAvailable ||
            (!function.CanRepeat && _acquiredNonRepeatableFunctionIds.Contains(function.ID)))
        {
            return false;
        }

        if (function is AddPlayer recruitFunction)
        {
            return recruitFunction.canRecruit(PlayerManager.Instance);
        }

        if (function is CardBenefitEnhanceFunction cardFunction && cardFunction.Card != null &&
            IsInstanceValid(cardFunction.Card) && cardFunction.Card.RequiresTargetSelection)
        {
            return CardBenefitRuntime.HasCompatibleDeployedTarget(cardFunction.Card);
        }

        if (!function.AppliesToAllShips)
        {
            if (!function.tryResolveTargetShipDataSilently(out PlayerShipData targetShipData))
            {
                return false;
            }

            return PlayerManager.Instance != null && PlayerManager.Instance.hasShip(targetShipData.ShipId);
        }

        return true;
    }

    private void addWeightedCandidate(Array<BaseEnhanceFunction> candidates, Array<BaseEnhanceFunction> result)
    {
        BaseEnhanceFunction selected = takeWeightedCandidate(candidates);
        if (selected != null)
        {
            result.Add(selected);
        }
    }

    private void addPendingRecruitmentOffer(Array<BaseEnhanceFunction> result)
    {
        if (_pendingRecruitFactionId == default && _pendingRecruitRoleId == default)
        {
            return;
        }

        Array<BaseEnhanceFunction> matches = new();
        foreach (BaseEnhanceFunction function in EnhanceFunctionList)
        {
            if (function is not AddPlayer recruitment || !canOffer(recruitment) ||
                !recruitment.tryResolveTargetShipData(out PlayerShipData shipData))
            {
                continue;
            }

            if ((_pendingRecruitFactionId == default || shipData.FactionId == _pendingRecruitFactionId) &&
                (_pendingRecruitRoleId == default || shipData.RoleId == _pendingRecruitRoleId))
            {
                matches.Add(recruitment);
            }
        }

        if (matches.Count == 0)
        {
            LogUtil.Warning($"No recruit offer matches the queued signal faction={_pendingRecruitFactionId}, role={_pendingRecruitRoleId}.");
            return;
        }

        BaseEnhanceFunction selected = takeWeightedCandidate(matches);
        result.Add(selected);
        _pendingRecruitFactionId = default;
        _pendingRecruitRoleId = default;
    }

    private static BaseEnhanceFunction takeWeightedCandidate(Array<BaseEnhanceFunction> candidates)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        float totalWeight = 0.0f;
        foreach (BaseEnhanceFunction candidate in candidates)
        {
            totalWeight += Mathf.Max(candidate.SelectionWeight, 0.01f);
        }

        float roll = GD.Randf() * totalWeight;
        float accumulatedWeight = 0.0f;
        for (int index = 0; index < candidates.Count; index++)
        {
            BaseEnhanceFunction candidate = candidates[index];
            accumulatedWeight += Mathf.Max(candidate.SelectionWeight, 0.01f);
            if (roll <= accumulatedWeight)
            {
                candidates.RemoveAt(index);
                return candidate;
            }
        }

        BaseEnhanceFunction fallback = candidates[candidates.Count - 1];
        candidates.RemoveAt(candidates.Count - 1);
        return fallback;
    }

    private static void shuffle(Array<EnhanceFunctionType> values)
    {
        if (values.Count < 2)
        {
            return;
        }

        for (int index = values.Count - 1; index > 0; index--)
        {
            // RandRange's upper bound can be returned, so the valid maximum is index.
            int swapIndex = (int)GD.RandRange(0, index);
            EnhanceFunctionType value = values[index];
            values[index] = values[swapIndex];
            values[swapIndex] = value;
        }
    }
}

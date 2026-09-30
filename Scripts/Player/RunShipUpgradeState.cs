using System.Collections.Generic;
using Godot;

/// <summary>
/// Run-local selection state for ship upgrade nodes. MetaProgressStore owns
/// permanent unlocks; this class records only upgrades actually chosen in a run.
/// </summary>
public static class RunShipUpgradeState
{
    private static readonly Dictionary<StringName, HashSet<StringName>> SelectedNodesByShip = new();
    private static readonly Dictionary<StringName, StringName> SelectedBranchByShip = new();

    public static void Reset()
    {
        SelectedNodesByShip.Clear();
        SelectedBranchByShip.Clear();
    }

    public static bool HasNode(StringName shipId, StringName nodeId)
    {
        return SelectedNodesByShip.TryGetValue(shipId, out HashSet<StringName> nodes) && nodes.Contains(nodeId);
    }

    public static bool TrySelect(ShipUpgradeNodeData node, int playerLevel, out string failureReason)
    {
        if (!CanSelect(node, playerLevel, out failureReason))
        {
            return false;
        }

        if (!SelectedNodesByShip.TryGetValue(node.ShipId, out HashSet<StringName> nodes))
        {
            nodes = new HashSet<StringName>();
            SelectedNodesByShip[node.ShipId] = nodes;
        }

        nodes.Add(node.NodeId);
        SelectedBranchByShip[node.ShipId] = node.BranchId;
        failureReason = string.Empty;
        return true;
    }

    /// <summary>Debug-only bypass for isolated upgrade-effect verification.</summary>
    public static void ForceSelect(ShipUpgradeNodeData node)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            return;
        }

        if (!SelectedNodesByShip.TryGetValue(node.ShipId, out HashSet<StringName> nodes))
        {
            nodes = new HashSet<StringName>();
            SelectedNodesByShip[node.ShipId] = nodes;
        }

        nodes.Add(node.NodeId);
        SelectedBranchByShip[node.ShipId] = node.BranchId;
    }

    public static bool CanSelect(ShipUpgradeNodeData node, int playerLevel, out string failureReason)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
        {
            failureReason = "升级节点无效。";
            return false;
        }

        if (!MetaProgressStore.IsShipUpgradeNodeUnlocked(node.NodeId))
        {
            failureReason = "该节点尚未在局外解锁。";
            return false;
        }

        int requiredLevel = node.Tier switch
        {
            1 => 2,
            2 => 5,
            _ => 8
        };
        if (playerLevel < requiredLevel)
        {
            failureReason = $"需要局内等级 {requiredLevel}。";
            return false;
        }

        if (SelectedBranchByShip.TryGetValue(node.ShipId, out StringName selectedBranch) && selectedBranch != node.BranchId)
        {
            failureReason = "同一舰船的三条路线在本局互斥。";
            return false;
        }

        if (node.Tier > 1 && !HasNode(node.ShipId, node.PreviousNodeId))
        {
            failureReason = "需要先在本局选择同分支的上一层。";
            return false;
        }

        if (HasNode(node.ShipId, node.NodeId))
        {
            failureReason = "该节点已在本局选择。";
            return false;
        }

        failureReason = string.Empty;
        return true;
    }

    public static IEnumerable<StringName> GetSelectedNodeIds(StringName shipId)
    {
        if (!SelectedNodesByShip.TryGetValue(shipId, out HashSet<StringName> nodes))
        {
            yield break;
        }

        foreach (StringName nodeId in nodes)
        {
            yield return nodeId;
        }
    }
}

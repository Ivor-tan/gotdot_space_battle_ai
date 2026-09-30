using Godot;

[GlobalClass]
public partial class ShipUpgradeNodeData : GameResource
{
    [Export] public StringName NodeId;
    [Export] public StringName ShipId;
    [Export] public StringName BranchId;
    [Export(PropertyHint.Range, "1,3,1")] public int Tier;
    [Export] public StringName PreviousNodeId;
    [Export] public string BranchName = "";
    [Export(PropertyHint.MultilineText)] public string EffectDescription = "";
    [Export] public bool IsImplementedInRuntime;
}

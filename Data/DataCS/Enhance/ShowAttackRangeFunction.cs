using Godot;
using System;
[GlobalClass]
public partial class ShowAttackRangeFunction : BaseEnhanceFunction
{

    [Export]
    public PackedScene FunctionScene;

    public override void ApplyEffect()
    {
        base.ApplyEffect();
        PlayerManager.Instance.Player.AddChild(FunctionScene.Instantiate());
    }


}

using Godot;
using System;

[GlobalClass]
public partial class ExpBallPointsData : GameResource
{
	[Export] public int ExpPoints = 1;
	[Export] public SpriteFrames AnimationRes;
	[Export] public ExpBallPointsType ExpType;
}

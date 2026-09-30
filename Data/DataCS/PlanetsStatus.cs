using Godot;
using System;
[GlobalClass]
public partial class PlanetsStatus : GameResource
{
	[Export] public int Hp = 100;
	[Export] public int Damage = 10;
	[Export] public float Speed = 50;
}

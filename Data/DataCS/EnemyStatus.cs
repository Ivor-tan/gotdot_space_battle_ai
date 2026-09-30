using Godot;
using System;

[GlobalClass]
public partial class EnemyStatus : GameResource
{
	[Export] public int Health = 100;
	[Export] public int Speed = 20;
	[Export] public int Damage = 20;
	[Export] public float Acceleration = 20;
	[Export] public float RotateSpeed = 0.3f;
	[Export] public float PositionThreshold = 50f;
	[Export] public float Deceleration = 30f;
	[Export] public float DespawnDistance = 2000f * 2000f;
	[Export] public Texture Texture;
	[Export] public ExpBallPointsType ExpPointsType;
	[Export] public EnemyType EnemyType;
	public EnemyStatus() { }
}

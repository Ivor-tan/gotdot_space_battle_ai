using Godot;
using System;

[GlobalClass]
public partial class BulletStatus : Resource
{
	//伤害
	[Export] public int Damage = 50;

	//子弹来源
	[Export] public BulletType BulletFrom = BulletType.Plyer;

	//子弹存活时间
	[Export] public int LifeTime = 10;
	//转向速度
	[Export] public float RotateSpeed = 0.1f;

	// 最大移动速度
	[Export] public float MaxSpeed = 200.0f;
	// 加速度
	[Export] public float Acceleration = 50.0f;

	//位置误差
	[Export] public float PositionThreshold = 10f;

	//转角误差
	[Export] public float threshold = 0.001f;

	//是否有目标
	[Export] public bool HasTarget = true;

}

using Godot;
using System;
[GlobalClass]
public partial class PlayerStatus : GameResource
{
	// 飞船本体没有耐久数值；未被护盾抵挡的任意一次命中都会损失该飞船。
	[Export] public int Hp = 1;

	// 最大移动速度
	[Export] public float MaxSpeed = 40.0f;
	// 加速度
	[Export] public float Acceleration = 20.0f;

	// 减速度（停止时用）
	[Export] public float Deceleration = 10.0f;
	//转向速度
	[Export] public float RotateSpeed = 0.1f;

	//点击位置误差
	[Export] public float PositionThreshold = 2f;

	//转角误差
	[Export] public float threshold = 0.001f;

	//攻击距离，判断的平方距离
	[Export] public float AttackRange = 600f;
	[Export] public float AttackInterval = 1.5f;

}

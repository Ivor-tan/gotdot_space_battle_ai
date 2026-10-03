using System;

using Godot;
using Godot.Collections;
using GodotResourceGroups;
public partial class MapResources : Singleton<MapResources>
{
	Array<Texture2D> AsteroidTextureList = new Array<Texture2D>();
	private Array<Texture2D> PlanetsTexture = new Array<Texture2D>();
	private Array<Texture2D> MechanicalTexture = new Array<Texture2D>();//这个好像用不到
	private Array<PackedScene> EnemiesScenes = new Array<PackedScene>();//这个好像用不到
	private Array<ExpBallPointsData> ExpBalls = new();
	// private Dictionary<ExpBallPointsType, ExpBallPointsData> ExpBalls = new();

	protected override void onSingletonEnterTree()
	{
		AsteroidTextureList.Clear();
		PlanetsTexture.Clear();
		MechanicalTexture.Clear();
		EnemiesScenes.Clear();
		ExpBalls.Clear();
		ResourceGroup.Of(Assets.Asteroid_Png).LoadAllInto(AsteroidTextureList);
		ResourceGroup.Of(Assets.Planet_Png).LoadAllInto(PlanetsTexture);
		ResourceGroup.Of(Assets.Mechanical_Png).LoadAllInto(MechanicalTexture);
		ResourceGroup.Of(Assets.Enemy_Tscn).LoadAllInto(EnemiesScenes);
		ResourceGroup.Of(Assets.Exp_Tres).LoadAllInto(ExpBalls);
	}




	public ExpBallPointsData GetExpBall(int index)
	{
		return ExpBalls[index];
	}

	public int GetExpBallCount()
	{
		return ExpBalls.Count;
	}
	public Array<Texture2D> GetAsteroidTexList()
	{

		return AsteroidTextureList;
	}
	public Array<Texture2D> GetPlanetsTexList()
	{
		return PlanetsTexture;
	}
	public Array<Texture2D> GetMechanicalTexList()
	{
		return MechanicalTexture;
	}

}

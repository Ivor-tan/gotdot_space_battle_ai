using Godot;
using System;

public partial class PlanetImage : Sprite2D
{
	public override void _Ready()
	{
		InitPlanet();
	}

	private void InitPlanet()
	{
		GD.Randomize();

		Material = Material.Duplicate() as Material;

		if (Material is ShaderMaterial mat)
		{
			mat.SetShaderParameter("speed", GD.Randf() / 2);
		}

		float randomScale = (float)GD.RandRange(0.5f, 1f); // 0.5-1.5之间的随机数
		Scale = new Vector2(randomScale, randomScale);
		int index = GD.RandRange(0, MapResources.Instance.GetPlanetsTexList().Count - 1);
		Texture = MapResources.Instance.GetPlanetsTexList()[index];
	}
}

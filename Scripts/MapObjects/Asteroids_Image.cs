using Godot;
using System;

public partial class Asteroids_Image : Sprite2D
{
	public override void _Ready()
	{
		InitAsteroid();
	}

	private void InitAsteroid()
	{
		GD.Randomize();

		Material = Material.Duplicate() as Material;

		if (Material is ShaderMaterial mat)
		{
			mat.SetShaderParameter("speed", GD.Randf() / 2);
		}

		float randomScale = (float)GD.RandRange(0.3f, 1f); // 0.5-1.5之间的随机数
		Scale = new Vector2(randomScale, randomScale);
		int index = GD.RandRange(0, MapResources.Instance.GetAsteroidTexList().Count - 1);
		Texture = MapResources.Instance.GetAsteroidTexList()[index];
	}
}

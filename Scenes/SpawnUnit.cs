using Godot;
using System;

public partial class SpawnUnit : Node2D
{
	[Export] public PackedScene SpawnScene;
	[Export] public int count = 100;

	private Node2D _unitsContainer;

	public override void _Ready()
	{
		_unitsContainer = GetNode<Node2D>("/root/World/Entities/Units");

		for (int i = 0; i < count; i++)
		{
			SpawnNewUnit(new Vector2(
				(float)GD.RandRange(100, 1000),
				(float)GD.RandRange(100, 1000)
			));
		}
	}
		public BaseUnit SpawnNewUnit(Vector2 worldPosition, int ownerId = 0)
	{
		var unit = SpawnScene.Instantiate<BaseUnit>();
		unit.Position = worldPosition;
		unit.UniqueID = ownerId;
		_unitsContainer.AddChild(unit);
		return unit;
	}
}

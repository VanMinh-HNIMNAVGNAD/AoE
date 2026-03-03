using Godot;
using System;

public partial class SpawnUnit : Node2D
{
	[Export] public PackedScene SpawnScene;
	[Export] public int count = 100;

	private Node2D _unitsContainer;

	public override void _Ready()
	{
		// Units are spawned into Entities/Units so they Y-sort correctly
		// with buildings, resources, and other entities
		_unitsContainer = GetNode<Node2D>("/root/World/Entities/Units");

		for (int i = 0; i < count; i++)
		{
			SpawnNewUnit(new Vector2(
				(float)GD.RandRange(100, 1000),
				(float)GD.RandRange(100, 1000)
			));
		}
	}

	/// <summary>
	/// Spawn a unit at the given world position.
	/// Can be called from other systems (e.g. Barracks production).
	/// </summary>
	public BaseUnit SpawnNewUnit(Vector2 worldPosition, int ownerId = 0)
	{
		var unit = SpawnScene.Instantiate<BaseUnit>();
		unit.Position = worldPosition;
		unit.UniqueID = ownerId;
		_unitsContainer.AddChild(unit);
		return unit;
	}
}

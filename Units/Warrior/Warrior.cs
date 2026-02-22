using Godot;
using System;

public partial class Warrior : SelectableUnit
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		base._Ready();

	}

	public void AttackEnemy()
	{
		GD.Print("Dang tan cong....");
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

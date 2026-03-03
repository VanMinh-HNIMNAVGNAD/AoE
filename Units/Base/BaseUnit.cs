using Godot;
using System;

public partial class BaseUnit : CharacterBody2D
{
	[Export] public AnimatedSprite2D anima;
	[Export] public float Speed = 80.0f;
	[Export] public int MaxHp = 150;
	[Export] public int UniqueID = 0;

	protected int _currentHp;

	public bool IsSelected = false;

    public override void _Ready()
    {
        _currentHp = MaxHp;
		AddToGroup("units");
    }

	public virtual void TakeDamage(int amount)
	{
		_currentHp -= amount;
		if (_currentHp <= 0)
		{
			Die();
		}
	}
	protected virtual void Die()
	{
		QueueFree();
	}

    public override void _PhysicsProcess(double delta)
    {
        if (anima != null && Velocity.X != 0)
        {
            anima.FlipH = Velocity.X < 0;
        }
    }

}

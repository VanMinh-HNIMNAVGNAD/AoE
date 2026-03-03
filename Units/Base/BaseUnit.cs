using Godot;
using System;

public partial class BaseUnit : CharacterBody2D
{
	[Export] public AnimatedSprite2D anima;
	[Export] public float Speed = 80.0f;
	[Export] public int MaxHp = 150;
	[Export] public int UniqueID = 0;

	protected NavigationAgent2D _navAgent2D;
	protected int _currentHp;

	public bool IsSelected = false;

    public override void _Ready()
    {
        _currentHp = MaxHp;
		AddToGroup("units");
		_navAgent2D = GetNode<NavigationAgent2D>("NavigationAgent2D");
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

	public void MoveAction(Vector2 target){
		_navAgent2D.TargetPosition = target;
	}
    public override void _PhysicsProcess(double delta)
    {
        if (anima != null && Velocity.X != 0)
        {
            anima.FlipH = Velocity.X < 0;
        }

		if (!_navAgent2D.IsNavigationFinished())
		{
			var dir = (_navAgent2D.GetNextPathPosition() - GlobalPosition);
			Velocity = dir * Speed;
		}else{
			Velocity = Vector2.Zero;
		}
		MoveAndSlide();
    }

}

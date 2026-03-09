using Godot;

public partial class BaseUnit : CharacterBody2D
{
    [Export] public float MoveSpeed = 150.0f;

    protected NavigationAgent2D NavAgent = null!;

    public override void _Ready()
    {
        AddToGroup("units");

        NavAgent = GetNode<NavigationAgent2D>("NavigationAgent2D");
        NavAgent.TargetDesiredDistance = 5.0f;
    }

    public virtual void MoveTo(Vector2 targetPosition)
    {
        NavAgent.TargetPosition = targetPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (NavAgent.IsNavigationFinished())
        {
            Velocity = Vector2.Zero;
            return;
        }

        Vector2 currentAgentPosition = GlobalPosition;
        Vector2 nextPathPosition = NavAgent.GetNextPathPosition();
        Vector2 newVelocity = (nextPathPosition - currentAgentPosition).Normalized() * MoveSpeed;

        Velocity = newVelocity;
        MoveAndSlide();
    }

    public void Select()
    {
        Modulate = new Color(0, 1, 0);
    }

    public void Deselect()
    {
        Modulate = Colors.White;
    }
}

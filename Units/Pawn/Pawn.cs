using Godot;

public enum PawnState { Idle, Moving, Gathering }

public partial class Pawn : BaseUnit
{
	public PawnState CurrentState = PawnState.Idle;
	[Export] public int CarryCapacity = 10;
	public int CurrentCarry = 0;
	[Export] public float GatherRate = 1.0f; 
	
	private ResourceNode _targetResource = null!;
	private double _gatherTimer = 0;

	public override void MoveTo(Vector2 targetPosition)
	{
		_targetResource = null;
		_gatherTimer = 0;
		CurrentState = PawnState.Moving;
		base.MoveTo(targetPosition);
	}

	public void CommandGather(ResourceNode resource)
	{
		_targetResource = resource;
		_gatherTimer = 0;
		CurrentState = PawnState.Moving;
		base.MoveTo(resource.GlobalPosition); 
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta); 

		switch (CurrentState)
		{
			case PawnState.Moving:
				if (!NavAgent.IsNavigationFinished())
				{
					break;
				}

				Velocity = Vector2.Zero;
				if (_targetResource != null && IsInstanceValid(_targetResource) && CurrentCarry < CarryCapacity)
				{
					CurrentState = PawnState.Gathering;
				}
				else
				{
					CurrentState = PawnState.Idle;
				}
				break;

			case PawnState.Gathering:
				if (_targetResource == null || !IsInstanceValid(_targetResource) || CurrentCarry >= CarryCapacity)
				{
					_targetResource = null;
					CurrentState = PawnState.Idle; 
					break;
				}

				_gatherTimer += delta;
				if (_gatherTimer >= GatherRate)
				{
					_gatherTimer = 0; 
					
					if (CurrentCarry < CarryCapacity)
					{
						// Rút 1 đơn vị tài nguyên từ Cây
						int amount = _targetResource.Extract(1); 
						if (amount <= 0)
						{
							_targetResource = null;
							CurrentState = PawnState.Idle;
							break;
						}

						CurrentCarry += amount;
						GD.Print($"Đang chặt... Gỗ trong người: {CurrentCarry}/{CarryCapacity}");

						// Nếu đầy túi
						if (CurrentCarry >= CarryCapacity)
						{
							GD.Print("Đầy túi! Tạm thời đứng im.");
							_targetResource = null;
							CurrentState = PawnState.Idle; 
						}
					}
				}
				break;
		}
	}
}

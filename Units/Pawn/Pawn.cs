using Godot;

public enum PawnState { Idle, Moving, Gathering }

public partial class Pawn : BaseUnit
{
	public PawnState CurrentState = PawnState.Idle;
	[Export] public int CarryCapacity = 10;
	public int CurrentCarry = 0;
	[Export] public float GatherRate = 1.0f; 
	[Export] public float GatherDistance = 48.0f;
	
	private ResourceNode _targetResource = null!;
	private double _gatherTimer = 0;

	public override void MoveTo(Vector2 targetPosition)
	{
		_targetResource = null;
		_gatherTimer = 0;
		CurrentState = PawnState.Moving;
		NavAgent.TargetDesiredDistance = 5.0f;
		base.MoveTo(targetPosition);
	}

	public void CommandGather(ResourceNode resource)
	{
		_targetResource = resource;
		_gatherTimer = 0;
		CurrentState = PawnState.Moving;
		NavAgent.TargetDesiredDistance = GatherDistance;
		GD.Print($"[Pawn] CommandGather called. Target: {resource.GlobalPosition}, GatherDist: {GatherDistance}");
		base.MoveTo(resource.GlobalPosition); 
	}

	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta); 

		switch (CurrentState)
		{
			case PawnState.Moving:
				bool closeEnoughToGather = _targetResource != null
					&& IsInstanceValid(_targetResource)
					&& GlobalPosition.DistanceTo(_targetResource.GlobalPosition) <= GatherDistance;

				if (!NavAgent.IsNavigationFinished() && !closeEnoughToGather)
				{
					break;
				}

				Velocity = Vector2.Zero;
				if (_targetResource != null && IsInstanceValid(_targetResource) && CurrentCarry < CarryCapacity)
				{
					GD.Print($"[Pawn] Moving -> Gathering. Dist to resource: {GlobalPosition.DistanceTo(_targetResource.GlobalPosition):F1}");
					CurrentState = PawnState.Gathering;
				}
				else
				{
					GD.Print($"[Pawn] Moving -> Idle. HasTarget: {_targetResource != null}, Carry: {CurrentCarry}/{CarryCapacity}");
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

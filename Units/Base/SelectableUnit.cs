using Godot;
using System;

public enum UnitState
{
	Idle,
	Move,
	MoveToInteract,
	Gather,
}
public partial class SelectableUnit : CharacterBody2D
{
	[ExportGroup("Cấu hình Hiển thị")] 
	[Export] public Sprite2D IndicatorSprite; 

	[ExportGroup("Cấu hình Kích thước")]
	[Export] public Vector2 ManualBoxSize = new Vector2(40, 40); 
	[Export] public float BoxScaleRatio = 1.0f; 

	[ExportGroup("Cấu hình Tương tác")]
	
	[Export] public float GatherRate = 1.0f;
	[Export] public float GatherAmount = 10.0f; // Lượng tài nguyên thu được mỗi lần chặt (có thể là 10 gỗ, 10 vàng, v.v.)
	[Export] public float InteractionRange = 60.0f;

	[ExportGroup("Cấu hình Di chuyển")]
	[Export] public float MoveSpeed = 200.0f; // Tốc độ chạy
	[Export] public NavigationAgent2D NavAgent; // Kéo node NavigationAgent2D vào đây

	public UnitState CurrentState = UnitState.Idle;
	public Node2D CurrentTarget = null; // Đối tượng mục tiêu (có thể là tài nguyên, công trình, v.v.)
	// Biến trạng thái
	private bool _isSelected = false;
	public bool IsSelected
	{
		get => _isSelected;
		set => SetSelected(value);
	}

	private float _gatherTimer = 0.0f; // Bộ đếm thời gian để tính toán khi nào thu thập được tài nguyên tiếp theo

	public override void _Ready()
	{
		// Ẩn khung đi khi bắt đầu
		if (IndicatorSprite != null) IndicatorSprite.Visible = false;
		
		// Setup Navigation (quan trọng để tránh lỗi Actor chưa sync)
		if (NavAgent != null)
		{
			NavAgent.PathDesiredDistance = 4.0f;
			NavAgent.TargetDesiredDistance = 4.0f;
			
			// Kết nối tín hiệu để xử lý né tránh (Velocity Computed)
			NavAgent.VelocityComputed += OnVelocityComputed;
		}

		CallDeferred(nameof(ApplyBoxScale));
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (CurrentState)
		{
			case UnitState.Idle:
			break;
			case UnitState.Move:
				HandleMovement();
				if (NavAgent.IsNavigationFinished())
				{
					CurrentState = UnitState.Idle;
				}
			break;
			case UnitState.MoveToInteract:
				// 1. Nếu tự nhiên mục tiêu biến mất (ví dụ cây bị thằng khác chặt mất rồi) -> Đứng chơi
				if (CurrentTarget == null)
				{
					CurrentState = UnitState.Idle;
					break;
				}

				// 2. Đo khoảng cách từ vị trí hiện tại (GlobalPosition) đến Cây (CurrentTarget.GlobalPosition)
				float distanceToTarget = GlobalPosition.DistanceTo(CurrentTarget.GlobalPosition);

				// 3. Kiểm tra xem đã vào tầm với chưa?
				if (distanceToTarget <= InteractionRange)
				{
					// ĐÃ ĐẾN NƠI!
					Velocity = Vector2.Zero; // Phanh gấp lại (Vận tốc = 0)
					CurrentState = UnitState.Gather; // Chuyển sang trạng thái "Bắt đầu khai thác"
					
					// In ra để test xem nó có thực sự chuyển trạng thái không
					GD.Print($"[{Name}] Đã vào vị trí! Chuẩn bị chặt cây...");
				}
				else
				{
					// VẪN CÒN XA -> Tiếp tục gọi hàm chạy bộ
					HandleMovement();
				}
				break;
			case UnitState.Gather:
				// (Lát nữa chúng ta sẽ viết logic vung rìu ở đây)
				Velocity = Vector2.Zero; // Đảm bảo đang chặt thì không bị trượt đi
				break;
		}
	}

	// Hàm này nhận lệnh: "Ê, đi đến chỗ này đi!"
	public void MoveToTarget(Vector2 targetPosition)
	{
		if (NavAgent != null)
		{
			CurrentTarget = null;
			NavAgent.TargetPosition = targetPosition;
			CurrentState = UnitState.Move;
		}
	}

	public void SetInteractTarget(Node2D target)
	{
		if (NavAgent != null && target != null)
		{
			CurrentTarget = target;
			
			NavAgent.TargetPosition = target.GlobalPosition; 
			// Chuyển trạng thái sang: Đi để làm việc
			CurrentState = UnitState.MoveToInteract; 
		}
	}

	// Hàm callback xử lý di chuyển thực tế (Sau khi đã tính toán né tránh)
	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		// CHỈ cho phép di chuyển khi đang ở trạng thái chạy
		// Nếu không check, callback này sẽ ghi đè Velocity = 0 ở Gather/Idle
		// khiến lính bay loạn xạ
		if (CurrentState != UnitState.Move && CurrentState != UnitState.MoveToInteract)
			return;

		Velocity = safeVelocity;
		MoveAndSlide();
	}

	// --- CÁC HÀM CŨ (GIỮ NGUYÊN) ---
	private void SetSelected(bool selected)
	{
		_isSelected = selected;
		if (IndicatorSprite != null)
		{
			IndicatorSprite.Visible = selected;
			if (selected) ApplyBoxScale();
		}
	}

	private void ApplyBoxScale()
	{
		if (IndicatorSprite == null || IndicatorSprite.Texture == null) return;
		Vector2 frameTextureSize = IndicatorSprite.Texture.GetSize();
		if (frameTextureSize.X == 0 || frameTextureSize.Y == 0) return;

		// Fallback nếu ManualBoxSize hoặc BoxScaleRatio bị null/zero (do scene override)
		Vector2 boxSize = (ManualBoxSize == Vector2.Zero) ? new Vector2(40, 40) : ManualBoxSize;
		float scaleRatio = (BoxScaleRatio == 0.0f) ? 1.0f : BoxScaleRatio;

		Vector2 finalScale = (boxSize / frameTextureSize) * scaleRatio;
		IndicatorSprite.Scale = finalScale;
	}

	private void HandleMovement()
	{
		// Nếu không có NavAgent hoặc đã đến nơi thì dừng
		if (NavAgent == null || NavAgent.IsNavigationFinished())
		{
			return;
		}

		// 1. Lấy vị trí tiếp theo trên đường đi
		Vector2 nextPathPosition = NavAgent.GetNextPathPosition();

		// 2. Tính toán vận tốc mong muốn
		Vector2 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * MoveSpeed;

		// 3. Gửi vận tốc này cho NavAgent để nó tính toán né tránh (Avoidance)
		if (NavAgent.AvoidanceEnabled)
		{
			NavAgent.Velocity = newVelocity;
		}
		else
		{
			// Nếu không bật né tránh thì đi luôn
			OnVelocityComputed(newVelocity); 
		}
	}
	
}

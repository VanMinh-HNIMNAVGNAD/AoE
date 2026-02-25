using Godot;
using System;

public enum UnitState
{
	Idle,
	Move,
	MoveToInteract,
	Action,
}

public partial class SelectableUnit : CharacterBody2D
{
	// ──────────────────── DATA ────────────────────
	// [RỦI RO] Stats PHẢI được gán trong Inspector, nếu null sẽ gây NullReferenceException
	// tại _PhysicsProcess (MoveToInteract) và PerformAction của lớp con.
	[ExportGroup("Dữ liệu Lính")]
	[Export] public UnitsData Stats;

	// ──────────────────── HIỂN THỊ ────────────────────
	[ExportGroup("Cấu hình Hiển thị")]
	[Export] public Sprite2D IndicatorSprite;
	// [FIX] Đổi tên từ 'animated' → 'AnimSprite' để khớp với mọi chỗ dùng trong code.
	// Trước đây tên là 'animated' nhưng code gọi 'AnimSprite' → gây lỗi compile.
	[Export] public AnimatedSprite2D AnimSprite;

	[ExportGroup("Cấu hình Kích thước")]
	[Export] public Vector2 ManualBoxSize = new Vector2(40, 40);
	[Export] public float BoxScaleRatio = 1.0f;

	// [FIX] Xóa các Export trùng lặp: GatherRate, GatherAmount, InteractionRange, MoveSpeed.
	// Tất cả bây giờ đọc từ Stats (UnitsData) để tránh nhầm lẫn giữa 2 nguồn dữ liệu.
	// Nếu cần fallback, dùng property MoveSpeed bên dưới.

	[ExportGroup("Cấu hình Di chuyển")]
	[Export] public NavigationAgent2D NavAgent;

	// ──────────────────── RUNTIME STATE ────────────────────
	public UnitState CurrentState = UnitState.Idle;
	public Node2D CurrentTarget = null;

	private bool _isSelected = false;
	public bool IsSelected
	{
		get => _isSelected;
		set => SetSelected(value);
	}

	// [FIX] Xóa _gatherTimer ở lớp cha — chỉ Pawn mới dùng, để lớp con tự khai báo.

	/// <summary>Tốc độ di chuyển — ưu tiên đọc từ Stats, fallback 200.</summary>
	public float MoveSpeed => Stats != null ? Stats.MaxSpeed : 200.0f;

	// ──────────────────── VIRTUAL (lớp con override) ────────────────────
	public virtual bool CanInteractWith(Node2D target) { return false; }
	protected virtual void PerformAction(double delta) { }

	// [FIX] Timer chống kẹt: nếu unit không tiến được sau N giây, force idle
	private float _stuckTimer = 0.0f;
	private const float STUCK_TIMEOUT = 2.0f;
	private Vector2 _lastProgressPosition;

	public override void _Ready()
	{
		if (IndicatorSprite != null) IndicatorSprite.Visible = false;

		if (NavAgent != null)
		{
			// [FIX] Tăng khoảng cách waypoint lên 12px — giá trị cũ (4px) quá nhỏ
			// khiến unit overshoot rồi oscillate qua lại quanh waypoint
			NavAgent.PathDesiredDistance = 12.0f;
			NavAgent.TargetDesiredDistance = 12.0f;
			NavAgent.VelocityComputed += OnVelocityComputed;
		}

		_lastProgressPosition = GlobalPosition;
		CallDeferred(nameof(ApplyBoxScale));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (Velocity.X != 0 && AnimSprite != null)
		{
			AnimSprite.FlipH = Velocity.X < 0; 
		}

		switch (CurrentState)
		{
			case UnitState.Idle:
				// [FIX] Reset velocity khi Idle — trước đây velocity cũ vẫn còn
				// khiến unit trôi thêm 1-2 frame sau khi dừng
				Velocity = Vector2.Zero;
				if (AnimSprite != null) AnimSprite.Play("idle");
				break;
				
			case UnitState.Move:
				if (AnimSprite != null) AnimSprite.Play("run");
				HandleMovement();
				// [FIX] Thêm null check cho NavAgent — tránh NullReferenceException
				if (NavAgent != null && NavAgent.IsNavigationFinished())
				{
					Velocity = Vector2.Zero;
					CurrentState = UnitState.Idle;
				}
				// [FIX] Phát hiện kẹt: nếu di chuyển nhưng vị trí không đổi > 2s → dừng
				else
				{
					CheckStuck(delta);
				}
				break;
				
			case UnitState.MoveToInteract:
				if (AnimSprite != null) AnimSprite.Play("run");
				// [FIX] Kiểm tra target còn tồn tại + kiểm tra ResourceNode.IsExhausted
				if (!IsInstanceValid(CurrentTarget) || IsTargetExhaustedResource(CurrentTarget))
				{
					CurrentTarget = null;
					Velocity = Vector2.Zero;
					CurrentState = UnitState.Idle;
					break;
				}

				// [FIX] Cập nhật lại đích đến của NavAgent theo vị trí mới nhất của target
				// Trước đây chỉ set 1 lần trong SetInteractTarget → nếu target di chuyển,
				// unit vẫn đi đến vị trí cũ
				NavAgent.TargetPosition = CurrentTarget.GlobalPosition;

				float interactRange = Stats != null ? Stats.InteractionRange : 60.0f;
				float distanceToTarget = GlobalPosition.DistanceTo(CurrentTarget.GlobalPosition);
				if (distanceToTarget <= interactRange)
				{
					Velocity = Vector2.Zero;
					CurrentState = UnitState.Action;
				}
				else
				{
					HandleMovement();
					CheckStuck(delta);
				}
				break;
				
			case UnitState.Action:
				// LỚP CHA KHÔNG LÀM GÌ CẢ, GIAO LẠI CHO LỚP CON XỬ LÝ!
				PerformAction(delta);
				break;
		}
	}

	public void MoveToTarget(Vector2 targetPosition)
	{
		if (NavAgent != null)
		{
			CurrentTarget = null;
			// [FIX] Reset velocity trước khi gán đích mới
			// Trước đây velocity cũ vẫn còn → unit lao 1 frame theo hướng cũ
			Velocity = Vector2.Zero;
			NavAgent.TargetPosition = targetPosition;
			CurrentState = UnitState.Move;
			// [FIX] Reset stuck timer khi nhận lệnh mới
			_stuckTimer = 0.0f;
			_lastProgressPosition = GlobalPosition;
		}
	}

	public void SetInteractTarget(Node2D target)
	{
		if (NavAgent != null && target != null)
		{
			if (!CanInteractWith(target))
			{
				MoveToTarget(target.GlobalPosition);
				return;
			}
			CurrentTarget = target;
			// [FIX] Reset velocity + stuck timer khi nhận lệnh mới
			Velocity = Vector2.Zero;
			NavAgent.TargetPosition = target.GlobalPosition;
			CurrentState = UnitState.MoveToInteract;
			_stuckTimer = 0.0f;
			_lastProgressPosition = GlobalPosition;
		}
	}

	private void OnVelocityComputed(Vector2 safeVelocity)
	{
		if (CurrentState != UnitState.Move && CurrentState != UnitState.MoveToInteract)
			return;

		Velocity = safeVelocity;
		MoveAndSlide();
	}

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

		Vector2 boxSize = (ManualBoxSize == Vector2.Zero) ? new Vector2(40, 40) : ManualBoxSize;
		float scaleRatio = (BoxScaleRatio == 0.0f) ? 1.0f : BoxScaleRatio;

		Vector2 finalScale = (boxSize / frameTextureSize) * scaleRatio;
		IndicatorSprite.Scale = finalScale;
	}

	/// <summary>
	/// Kiểm tra xem target có phải ResourceNode đã cạn kiệt hay không.
	/// Dùng trong MoveToInteract để dừng di chuyển ngay khi tài nguyên bị Pawn khác khai thác hết,
	/// thay vì đợi đến cuối frame (khi QueueFree thực sự xóa node).
	/// </summary>
	protected bool IsTargetExhaustedResource(Node2D target)
	{
		return target is ResourceNode res && res.IsExhausted;
	}

	/// <summary>
	/// Kiểm tra unit có bị kẹt không (di chuyển nhưng vị trí không thay đổi).
	/// Nếu kẹt quá STUCK_TIMEOUT giây → force về Idle.
	/// </summary>
	private void CheckStuck(double delta)
	{
		const float PROGRESS_THRESHOLD = 8.0f; // Phải di chuyển ít nhất 8px để tính là "tiến được"
		if (GlobalPosition.DistanceTo(_lastProgressPosition) > PROGRESS_THRESHOLD)
		{
			_lastProgressPosition = GlobalPosition;
			_stuckTimer = 0.0f;
		}
		else
		{
			_stuckTimer += (float)delta;
			if (_stuckTimer >= STUCK_TIMEOUT)
			{
				GD.Print($"[Unit] Bị kẹt quá {STUCK_TIMEOUT}s, chuyển về Idle.");
				Velocity = Vector2.Zero;
				CurrentState = UnitState.Idle;
				_stuckTimer = 0.0f;
			}
		}
	}

	private void HandleMovement()
	{
		if (NavAgent == null || NavAgent.IsNavigationFinished())
		{
			// [FIX] Reset velocity khi navigation kết thúc
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 nextPathPosition = NavAgent.GetNextPathPosition();
		float distToNext = nextPathPosition.DistanceTo(GlobalPosition);

		// [FIX] Xóa bỏ logic cũ bị lỗi:
		//   if (dist < 1.0f) { NavAgent.TargetPosition = NavAgent.TargetPosition; return; }
		// Godot 4 KHÔNG tính lại đường khi gán cùng giá trị → unit kẹt vĩnh viễn.
		// Thay vào đó: nếu next position quá gần, chờ NavigationAgent tự advance.
		if (distToNext < 0.5f)
		{
			// Không di chuyển frame này, NavigationAgent sẽ tự chuyển waypoint tiếp theo
			return;
		}

		Vector2 newVelocity = GlobalPosition.DirectionTo(nextPathPosition) * MoveSpeed;

		if (NavAgent.AvoidanceEnabled)
		{
			NavAgent.Velocity = newVelocity;
		}
		else
		{
			OnVelocityComputed(newVelocity);
		}
	}
}

// ========================== LUỒNG HOẠT ĐỘNG CỦA SelectableUnit ==========================
//
// SelectableUnit là lớp CHA (base class) cho tất cả đơn vị trong game (Pawn, Warrior...).
// Nó kế thừa CharacterBody2D và quản lý: di chuyển, chọn lính, tương tác mục tiêu.
//
// ─── MÁY TRẠNG THÁI (State Machine) ───
//
//   UnitState có 4 trạng thái, xử lý trong _PhysicsProcess():
//
//   ┌─────────┐
//   │  Idle   │ ← Trạng thái mặc định, lính đứng yên, play "idle"
//   └────┬────┘
//        │
//        ├── MoveToTarget() được gọi (click phải ra đất trống)
//        │       → CurrentTarget = null, NavAgent nhận target position
//        │       → Chuyển sang Move
//        │       │
//        │       ▼
//        │   ┌─────────┐
//        │   │  Move   │ → HandleMovement() di chuyển theo NavAgent
//        │   └────┬────┘   play "run", FlipH theo hướng di chuyển
//        │        │        Khi NavAgent.IsNavigationFinished() → quay về Idle
//        │        ▼
//        │   [Đến nơi] → Idle
//        │
//        └── SetInteractTarget() được gọi (click phải vào object)
//                → Gọi CanInteractWith() để kiểm tra (virtual, lớp con override)
//                → Nếu KHÔNG tương tác được → fallback MoveToTarget()
//                → Nếu ĐƯỢC → lưu CurrentTarget, chuyển sang MoveToInteract
//                │
//                ▼
//            ┌───────────────┐
//            │ MoveToInteract│ → HandleMovement() di chuyển đến mục tiêu
//            └───────┬───────┘   play "run"
//                    │
//                    ├── Mục tiêu bị hủy (IsInstanceValid == false) → Idle
//                    │
//                    └── Khoảng cách <= Stats.InteractionRange (fallback 60)
//                            → Velocity = Zero, chuyển sang Action
//                            │
//                            ▼
//                        ┌────────┐
//                        │ Action │ → Gọi PerformAction(delta) — LỚP CON XỬ LÝ
//                        └────────┘   Ví dụ: Pawn chặt cây, Warrior đánh
//
// ─── DI CHUYỂN (Navigation) ───
//
//   HandleMovement():
//       → Kiểm tra NavAgent null hoặc đã đến nơi → return
//       → Lấy vị trí tiếp theo từ NavAgent.GetNextPathPosition()
//       → Tính vận tốc = hướng × MoveSpeed (đọc từ Stats.MaxSpeed)
//       → Nếu AvoidanceEnabled → gửi cho NavAgent tính né tránh → callback OnVelocityComputed
//       → Nếu không → gọi thẳng OnVelocityComputed()
//
//   OnVelocityComputed(safeVelocity):
//       → Chỉ xử lý nếu đang ở state Move hoặc MoveToInteract
//       → Gán Velocity = safeVelocity → MoveAndSlide()
//
// ─── CHỌN LÍNH ───
//
//   IsSelected (property) ← RTSController set giá trị:
//       → Gọi SetSelected() → hiện/ẩn IndicatorSprite (khung chọn)
//       → Nếu đang chọn → ApplyBoxScale() scale khung cho vừa unit
//
// ─── CÁC ĐIỂM ĐÃ FIX ───
//
//   1. [FIX] Đổi tên field 'animated' → 'AnimSprite' — trước đây code và khai báo không khớp tên
//   2. [FIX] Thêm null check cho Stats khi đọc InteractionRange — tránh crash nếu chưa gán
//   3. [FIX] Thêm null check cho NavAgent.IsNavigationFinished() ở state Move
//   4. [FIX] Xóa _gatherTimer ở lớp cha — chỉ Pawn dùng, để lớp con tự khai báo
//   5. [FIX] Xóa Export trùng lặp (GatherRate, GatherAmount, InteractionRange, MoveSpeed)
//            — tất cả đọc từ Stats (UnitsData) để tránh 2 nguồn dữ liệu xung đột
//   6. MoveSpeed bây giờ là property, đọc Stats.MaxSpeed với fallback 200
//
// ========================== HẾT ==========================

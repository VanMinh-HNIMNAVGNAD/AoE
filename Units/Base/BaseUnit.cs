using Godot;
using System;
public enum UnitState
{
	Idle,       // Đứng yên, phát animation "idle"
	Moving,     // Đang di chuyển tới mục tiêu, phát animation "walk"
	Attacking,  // (Phase 6 – chưa dùng)
	Gathering,  // (Phase 3 – chưa dùng)
	Building,   // (Phase 4 – chưa dùng)
	Dead        // Unit đã chết, chờ xoá
}

public partial class BaseUnit : CharacterBody2D
{
	[Export] public AnimatedSprite2D anima;
	[Export] public float Speed = 80.0f;
	[Export] public int MaxHp = 150;
	[Export] public int UniqueID = 0;

	protected NavigationAgent2D _navAgent2D;
	protected int _currentHp;

	// ── Stuck detection: nếu unit ở state Moving nhưng hầu như không di
	//    chuyển được (bị kẹt bởi unit khác / terrain) quá 0.5s → về Idle.
	private Vector2 _stuckCheckPosition;
	private float _stuckTimer;
	private const float StuckTimeThreshold = 0.5f;
	private const float StuckMoveThreshold = 2.0f; // world pixels trong 0.5s

	// ── 1.4  Biến lưu trạng thái hiện tại.  Đặt protected để class con
	//         (Pawn, Warrior…) có thể đọc/ghi, nhưng bên ngoài thì không.
	protected UnitState _state = UnitState.Idle;

	// ── Node con vẽ vòng chọn. Được tạo tự động trong _Ready().
	//    Tách ra node riêng để BaseUnit không bị "bẩn" bởi logic UI.
	private SelectionCircle _selectionCircle;

	// ── IsSelected là PROPERTY (không phải field) để khi giá trị thay đổi,
	//    ta có thể bật/tắt visual ngay lập tức mà không cần poll mỗi frame.
	private bool _isSelected = false;
	public bool IsSelected
	{
		get => _isSelected;
		set
		{
			if (_isSelected == value) return; // Tránh cập nhật thừa
			_isSelected = value;

			// Bật/tắt node vẽ vòng tròn theo trạng thái mới.
			// _selectionCircle có thể null nếu _Ready() chưa chạy (edge case spawn).
			if (_selectionCircle != null)
				_selectionCircle.Visible = value;
		}
	}

	// ── Getter công khai để các Manager (SelectionManager…) kiểm tra state
	//    mà không thể tự ý đổi state từ bên ngoài.
	public UnitState CurrentState => _state;

	public override void _Ready()
	{
		_currentHp = MaxHp;
		AddToGroup("units");
		_navAgent2D = GetNode<NavigationAgent2D>("NavigationAgent2D");

		// ── Tạo SelectionCircle và gắn vào unit này.
		//    ZIndex = -1 → vẽ phía SAU sprite (nằm dưới chân, không che unit).
		//    Visible = false → ẩn mặc định, chỉ hiện khi IsSelected = true.
		_selectionCircle = new SelectionCircle();
		_selectionCircle.ZIndex = -1;
		_selectionCircle.Visible = false;
		AddChild(_selectionCircle);
	}

	public virtual void TakeDamage(int amount)
	{
		_currentHp -= amount;
		if (_currentHp <= 0)
		{
			
			ChangeState(UnitState.Dead);
		}
	}

	protected virtual void Die()
	{
		QueueFree();
	}

	// ── 1.6  MoveAction giờ không chỉ set target cho NavAgent mà còn
	//         chuyển state → Moving.  Nhờ vậy _PhysicsProcess biết cần
	//         tính toán di chuyển và animation sẽ chuyển sang "walk".
	public void MoveAction(Vector2 target)
	{
		_navAgent2D.TargetPosition = target;
		_stuckTimer = 0f;
		_stuckCheckPosition = GlobalPosition;
		ChangeState(UnitState.Moving);
	}

	// ── 1.4  Hàm chuyển state tập trung.  Mọi lần đổi state đều đi qua
	//         đây để dễ debug (GD.Print) và sau này có thể thêm logic
	//         exit-state / enter-state (ví dụ huỷ gather khi bị attack).
	protected void ChangeState(UnitState newState)
	{
		if (_state == newState) return;   // Tránh xử lý lại nếu state không đổi
		_state = newState;
		OnStateChanged(newState);
	}

	// ── 1.5 + 1.6  Khi state thay đổi, cập nhật animation tương ứng.
	//               Class con có thể override để thêm animation riêng
	//               (ví dụ Warrior có animation "attack").
	protected virtual void OnStateChanged(UnitState newState)
	{
		if (anima == null) return;

		switch (newState)
		{
			case UnitState.Idle:
				PlayAnimation("idle");
				break;
			case UnitState.Moving:
				PlayAnimation("walk");
				break;
			case UnitState.Dead:
				Die();
				break;
			// Các state khác sẽ được xử lý ở phase sau
		}
	}

	// ── 1.5  Helper phát animation an toàn: chỉ gọi Play() khi
	//         SpriteFrames thực sự có animation đó, tránh crash runtime.
	protected void PlayAnimation(string animName)
	{
		if (anima.SpriteFrames != null && anima.SpriteFrames.HasAnimation(animName))
		{
			if (anima.Animation != animName)
				anima.Play(animName);
		}
	}

	// ── 1.4 + 1.6  _PhysicsProcess giờ dùng switch trên _state.
	//    - Idle:   không di chuyển, Velocity = Zero
	//    - Moving: dùng NavigationAgent tính đường, khi đến nơi → Idle
	//    - Các state khác: giữ nguyên Velocity (mở rộng ở phase sau)
	public override void _PhysicsProcess(double delta)
	{
		switch (_state)
		{
			case UnitState.Idle:
				Velocity = Vector2.Zero;
				break;

			case UnitState.Moving:
				ProcessMoving();
				break;

			// Các trạng thái tương lai (Attacking, Gathering, Building)
			// sẽ được thêm case ở đây trong phase tương ứng.
		}

		// ── 1.5  Flip sprite theo hướng di chuyển (trái/phải)
		if (anima != null && Velocity.X != 0)
		{
			anima.FlipH = Velocity.X < 0;
		}

		// ── 1.6  MoveAndSlide luôn được gọi mỗi frame bất kể state,
		//         vì Godot CharacterBody2D cần gọi nó để cập nhật vị trí.
		MoveAndSlide();

		// ── Stuck detection: kiểm tra sau MoveAndSlide.
		//    Mỗi 0.5s kiểm tra xem unit có thực sự di chuyển không.
		//    Nếu khoảng cách nhỏ hơn ngưỡng → unit bị kẹt → về Idle.
		if (_state == UnitState.Moving)
		{
			_stuckTimer += (float)delta;
			if (_stuckTimer >= StuckTimeThreshold)
			{
				if (GlobalPosition.DistanceTo(_stuckCheckPosition) < StuckMoveThreshold)
				{
					Velocity = Vector2.Zero;
					ChangeState(UnitState.Idle);
				}
				_stuckTimer = 0f;
				_stuckCheckPosition = GlobalPosition;
			}
		}
	}

	// ── 1.6  Tách riêng logic Moving ra method để dễ đọc.
	//    Luồng:  NavAgent chưa đến nơi → tính hướng → đặt Velocity
	//            NavAgent đã đến nơi   → dừng lại  → state = Idle
	private void ProcessMoving()
	{
		if (_navAgent2D.IsNavigationFinished())
		{
			// Đã đến đích → dừng di chuyển, quay về Idle
			Velocity = Vector2.Zero;
			ChangeState(UnitState.Idle);
			return;
		}

		// Lấy điểm tiếp theo trên đường đi và tính vector hướng
		Vector2 nextPos = _navAgent2D.GetNextPathPosition();
		Vector2 direction = (nextPos - GlobalPosition).Normalized();
		Velocity = direction * Speed;
	}
}

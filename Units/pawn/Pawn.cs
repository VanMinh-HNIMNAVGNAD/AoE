using Godot;
using System;

/// <summary>
/// Pawn — Lính nông dân, chuyên khai thác tài nguyên (gỗ, vàng, thức ăn).
/// Kế thừa SelectableUnit, override CanInteractWith + PerformAction.
/// </summary>
public partial class Pawn : SelectableUnit 
{
	// Timer đếm ngược giữa mỗi lần khai thác
	private float _gatherTimer = 0.0f;

	/// <summary>Pawn chỉ tương tác được với object thuộc group "Resource".</summary>
	public override bool CanInteractWith(Node2D target)
	{
		return target.IsInGroup("Resource");
	}

	/// <summary>
	/// Xử lý logic khai thác khi ở state Action.
	/// Được gọi mỗi _PhysicsProcess frame bởi lớp cha.
	/// </summary>
	protected override void PerformAction(double delta)
	{
		Velocity = Vector2.Zero; 

		// Bước 0: Kiểm tra mục tiêu còn tồn tại không (cây có thể đã bị QueueFree)
		// [FIX] Thêm kiểm tra IsExhausted — vì QueueFree() là deferred,
		// IsInstanceValid vẫn trả true trong cùng frame → dùng IsExhausted để phát hiện ngay.
		if (!IsInstanceValid(CurrentTarget) || IsTargetExhaustedResource(CurrentTarget))
		{
			CurrentState = UnitState.Idle;
			CurrentTarget = null;
			return;
		}

		// Bước 1: Chạy Animation vung rìu
		if (AnimSprite != null) AnimSprite.Play("use_axe");

		// [RỦI RO] Nếu Stats == null sẽ crash → thêm null check + fallback
		float gatherRate = Stats != null ? Stats.GatherRate : 1.0f;
		float gatherAmount = Stats != null ? Stats.GatherAmount : 10.0f;

		// Bước 2: Đếm ngược timer, mỗi khi hết → thực hiện 1 lần khai thác
		_gatherTimer -= (float)delta; 
		if (_gatherTimer <= 0.0f)
		{
			_gatherTimer = gatherRate;

			if (CurrentTarget is ResourceNode resourceNode)
			{
				// [FIX] Kiểm tra IsExhausted TRƯỚC khi gọi TakeResource
				// Tránh trường hợp Pawn khác đã khai thác hết trong cùng frame
				if (resourceNode.IsExhausted)
				{
					CurrentTarget = null;
					CurrentState = UnitState.Idle;
					return;
				}

				// Xác định loại tài nguyên dựa trên group của mục tiêu
				string resourceType = "Wood"; // Mặc định là gỗ
				if (CurrentTarget.IsInGroup("Gold")) resourceType = "Gold";
				else if (CurrentTarget.IsInGroup("Food")) resourceType = "Food";

				// TakeResource sẽ trả 0 nếu cây đã cạn (IsExhausted guard bên trong)
				int amountGot = resourceNode.TakeResource((int)gatherAmount);
				
				if (GameManager.Instance != null && amountGot > 0)
				{
					GameManager.Instance.AddResource(resourceType, amountGot);
				}

				// [FIX] Dùng IsExhausted thay vì IsInstanceValid
				// Vì QueueFree() deferred → IsInstanceValid trả true trong cùng frame
				// → check cũ luôn bỏ qua → Pawn ở Action thêm 1 frame thừa
				if (resourceNode.IsExhausted || !IsInstanceValid(CurrentTarget))
				{
					CurrentTarget = null;
					CurrentState = UnitState.Idle;
				}
			}
		}
	}
}

// ========================== LUỒNG HOẠT ĐỘNG CỦA Pawn ==========================
//
// Pawn kế thừa SelectableUnit, chuyên khai thác tài nguyên.
//
// ─── LUỒNG KHAI THÁC ───
//
//   [RTSController] Click phải vào cây/mỏ
//       │
//       ▼
//   SetInteractTarget(target) → CanInteractWith() trả true (group "Resource")
//       → CurrentTarget = cây, state = MoveToInteract
//       │
//       ▼
//   [Di chuyển đến gần cây] (SelectableUnit xử lý)
//       → Khi distance <= InteractionRange → state = Action
//       │
//       ▼
//   PerformAction(delta) — mỗi frame:
//       1. Kiểm tra cây còn tồn tại (IsInstanceValid)
//       2. Play animation "use_axe"
//       3. _gatherTimer -= delta
//       4. Khi timer <= 0:
//           → Reset timer = Stats.GatherRate (fallback 1.0s)
//           → Gọi resourceNode.TakeResource(Stats.GatherAmount)
//           → Cộng tài nguyên vào GameManager
//           → Nếu cây bị xóa (QueueFree) → Idle
//
// ─── CÁC ĐIỂM ĐÃ FIX ───
//
//   1. [FIX] Thêm null check cho Stats → fallback GatherRate=1, GatherAmount=10
//   2. [LƯU Ý] Nhiều Pawn cùng chặt 1 cây → race condition nhẹ:
//      TakeResource() trả đúng số lượng nhưng Pawn thứ 2 có thể gọi
//      TakeResource() sau QueueFree() → IsInstanceValid sẽ bắt được.
//
// ========================== HẾT ==========================
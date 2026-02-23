using Godot;
using System;

/// <summary>
/// Warrior — Lính chiến đấu, tương tác với kẻ thù (group "Enemy").
/// Kế thừa SelectableUnit, override CanInteractWith + PerformAction.
/// </summary>
public partial class Warrior : SelectableUnit 
{
	/// <summary>
	/// Warrior chỉ tương tác được với object thuộc group "Enemy".
	/// [LƯU Ý] Hiện tại chưa có node nào gắn group "Enemy" trong game,
	/// nên CanInteractWith luôn trả false → Warrior chỉ MoveToTarget.
	/// Cần gắn group "Enemy" cho đơn vị địch khi làm Combat System.
	/// </summary>
	public override bool CanInteractWith(Node2D target)
	{
		return target.IsInGroup("Enemy");
	}

	/// <summary>
	/// Xử lý logic tấn công khi ở state Action.
	/// [CHƯA HOÀN THIỆN] Chỉ có animation, chưa có damage logic.
	/// </summary>
	protected override void PerformAction(double delta)
	{
		Velocity = Vector2.Zero;

		// Kiểm tra mục tiêu còn tồn tại không
		if (!IsInstanceValid(CurrentTarget))
		{
			CurrentState = UnitState.Idle;
			CurrentTarget = null; // [FIX] Clear reference để tránh giữ object đã free
			return;
		}

		if (AnimSprite != null) AnimSprite.Play("attack_right");

		// TODO: Thêm logic tấn công khi làm Combat System:
		// - Đếm attackTimer giống _gatherTimer của Pawn
		// - Gọi target.TakeDamage(attackDamage)
		// - Kiểm tra target chết → Idle
	}
}

// ========================== LUỒNG HOẠT ĐỘNG CỦA Warrior ==========================
//
// Warrior kế thừa SelectableUnit, chuyên chiến đấu với kẻ thù.
//
// ─── LUỒNG TẤN CÔNG (chưa hoàn thiện) ───
//
//   [RTSController] Click phải vào kẻ thù (group "Enemy")
//       │
//       ▼
//   SetInteractTarget(target) → CanInteractWith() trả true
//       → CurrentTarget = enemy, state = MoveToInteract
//       │
//       ▼
//   [Di chuyển đến gần kẻ thù]
//       → Khi distance <= InteractionRange → state = Action
//       │
//       ▼
//   PerformAction(delta) — mỗi frame:
//       1. Kiểm tra target còn tồn tại
//       2. Play animation "attack_right"
//       3. (TODO) Logic damage chưa có
//
// ─── CÁC RỦI RO ───
//
//   1. [LƯU Ý] Chưa có group "Enemy" trong game → Warrior không thể tương tác
//   2. [LƯU Ý] Chưa có damage logic → Warrior chỉ đứng animation
//   3. [FIX] Thêm CurrentTarget = null khi target bị free
//
// ========================== HẾT ==========================
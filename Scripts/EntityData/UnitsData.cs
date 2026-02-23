using Godot;
using System;

public partial class UnitsData : EntityData
{
	[ExportGroup("Chỉ số Di chuyển")]
	[Export] public float MaxSpeed = 200.0f;

	[ExportGroup("Chỉ số Tương tác & Khai thác")]
	[Export] public float GatherRate = 1.0f;
	[Export] public float GatherAmount = 10.0f;
	[Export] public float InteractionRange = 60.0f;
}

// ========================== GIẢI THÍCH UnitsData ==========================
//
// UnitsData kế thừa EntityData → có thêm thông số riêng cho UNIT (đơn vị di chuyển được).
//
// ─── Quan hệ kế thừa ───
//
//   Resource (Godot)
//       └── EntityData (EntityName, MaxHealth, VisionRange, Cost...)
//               └── UnitsData (MaxSpeed, GatherRate, GatherAmount, InteractionRange)
//
// ─── Các trường bổ sung ───
//
//   MaxSpeed          : Tốc độ di chuyển tối đa của unit
//   GatherRate         : Khoảng thời gian (giây) giữa mỗi lần khai thác
//   GatherAmount       : Lượng tài nguyên thu được mỗi lần khai thác
//   InteractionRange   : Khoảng cách tối thiểu để bắt đầu tương tác với mục tiêu
//
// ─── Cách liên kết với SelectableUnit ───
//
//   SelectableUnit có [Export] public UnitsData Stats;
//   → Kéo file .tres (UnitsData) vào Inspector
//   → Hiện tại SelectableUnit dùng trực tiếp các [Export] riêng (MoveSpeed, GatherRate...)
//   → Tương lai nên đọc từ Stats để tách biệt data và logic:
//      MoveSpeed = Stats.MaxSpeed;
//      GatherRate = Stats.GatherRate;
//
// ========================== HẾT ==========================

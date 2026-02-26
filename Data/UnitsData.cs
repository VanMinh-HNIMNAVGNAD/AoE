using Godot;
using System;

/// <summary>
/// UnitsData — dữ liệu cho đơn vị di chuyển, kế thừa từ EntityData.
/// Chứa các thông số liên quan tới di chuyển và tương tác/khai thác.
/// </summary>
public partial class UnitsData : EntityData
{
	[ExportGroup("Chỉ số Di chuyển")]
	[Export] public float MaxSpeed = 200.0f;

	[ExportGroup("Chỉ số Tương tác & Khai thác")]
	[Export] public float GatherRate = 1.0f;
	[Export] public float GatherAmount = 3.0f;
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
// ========================== HẾT ==========================

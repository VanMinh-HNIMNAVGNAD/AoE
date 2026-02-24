using Godot;
using System;

[GlobalClass]
public partial class EntityData : Resource
{
	[ExportGroup("Thông tin cơ bản")]
	[Export] public string EntityName = "No name";
	[Export] public int MaxHealth = 100;
	[Export] public float VisionRange = 200.0f;

	[ExportGroup("Giá Trị Tài Nguyên")]
	[Export] public int CostMeal = 0;
	[Export] public int CostWood = 0;
	[Export] public int CostGold = 0;
}

// ========================== GIẢI THÍCH EntityData ==========================
//
// EntityData là lớp DỮ LIỆU (Resource) dùng để lưu thông số của 1 thực thể.
// Kế thừa Godot Resource → tạo file .tres trong Editor để cấu hình mà không cần code.
//
// [GlobalClass] → cho phép Godot nhận diện class này trong Inspector.
//
// ─── Cách sử dụng ───
//
//   1. Trong Editor: Create New Resource → chọn EntityData
//   2. Điền thông số: EntityName, MaxHealth, VisionRange, Cost...
//   3. Lưu thành file .tres (ví dụ: PawnRS.tres, Warrior.tres)
//   4. Trong script của unit: [Export] public EntityData Stats;
//   5. Kéo file .tres vào Inspector → unit tự đọc thông số
//
// ─── Các trường ───
//
//   EntityName   : Tên hiển thị của thực thể
//   MaxHealth    : Máu tối đa
//   VisionRange  : Tầm nhìn (dùng cho AI hoặc fog of war)
//   CostMeal/Wood/Gold : Chi phí tài nguyên để tạo/mua thực thể này
//
// ========================== HẾT ==========================

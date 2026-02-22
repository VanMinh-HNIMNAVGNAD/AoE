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

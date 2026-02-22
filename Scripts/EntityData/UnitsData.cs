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

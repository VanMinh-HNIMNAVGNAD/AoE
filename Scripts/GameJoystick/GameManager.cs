using Godot;
using System;

public partial class GameManager : Node2D
{
	public static GameManager Instance { get; private set; }

	[ExportGroup("Cấu hình Chung")]
	[Export] public int Wood = 0;
	[Export] public int Gold = 0;
	[Export] public int Food = 0;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
	}

	public void AddResource(string type, int amount)
	{
		switch (type.ToLower()) // tolower() để tránh lỗi do viết hoa/thường
		{
			case "wood":
				Wood += amount;
				GD.Print($"[GameManager] Thu thập được {amount} gỗ. Tổng gỗ: {Wood}");
				break;
			case "gold":
				Gold += amount;
				GD.Print($"[GameManager] Thu thập được {amount} vàng. Tổng vàng: {Gold}");
				break;
			case "food":
				Food += amount;
				GD.Print($"[GameManager] Thu thập được {amount} thực phẩm. Tổng thực phẩm: {Food}");
				break;
			default:
				GD.PrintErr($"[GameManager] Loại tài nguyên không hợp lệ: {type}");
				break;
		}
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}

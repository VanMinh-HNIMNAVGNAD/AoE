using Godot;
using System;

public partial class GameManager : Node2D
{
	public static GameManager Instance { get; private set; }

	[ExportGroup("Cấu hình Chung")]
	[Export] public int Wood = 0;
	[Export] public int Gold = 0;
	[Export] public int Food = 0;

	[ExportGroup("Giao diện UI")]
	[Export] public Label WoodLabel;
	[Export] public Label GoldLabel;
	[Export] public Label FoodLabel;

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		
		// Cập nhật UI ngay khi vừa vào game
		UpdateUI(); 
	}

	public void AddResource(string type, int amount)
	{
		switch (type.ToLower()) 
		{
			case "wood":
				Wood += amount;
				break;
			case "gold":
				Gold += amount;
				break;
			case "food":
				Food += amount;
				break;
			default:
				GD.PrintErr($"[GameManager] Loại tài nguyên không hợp lệ: {type}");
				return; // Thoát ra nếu lỗi, không cập nhật UI
		}
		
		// In ra console (nếu bạn muốn giữ lại)
		GD.Print($"[Kinh tế] +{amount} {type} | Tổng: Gỗ({Wood}) Thực({Food}) Vàng({Gold})");
		
		// Cập nhật lên màn hình
		UpdateUI();
	}
	
	private void UpdateUI()
	{
		if (WoodLabel != null) WoodLabel.Text = $"Gỗ: {Wood}";
		if (FoodLabel != null) FoodLabel.Text = $"Thực: {Food}";
		if (GoldLabel != null) GoldLabel.Text = $"Vàng: {Gold}";
	}
}
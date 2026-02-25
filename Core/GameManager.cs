using Godot;
using System;

public partial class GameManager : Node2D
{
	public static GameManager Instance { get; private set; }
	[ExportGroup("Mục xây (debug)")]
	[Export] public PackedScene HouseScene;
	[Export] public int HouseWoodCost = 50;
	[Export] public int HouseGoldCost = 0;
	[Export] public int HouseMealCost = 0;

	[Export] public AcceptDialog warning;

	[ExportGroup("Cấu hình Chung")]
	[Export] public int Wood = 0;
	[Export] public int Gold = 0;
	[Export] public int Food = 0;

	[ExportGroup("Giao diện UI")]
	[Export] public Label WoodLabel;
	[Export] public Label GoldLabel;
	[Export] public Label FoodLabel;

	// ──────────────────── XÂY DỰNG ────────────────────
	/// <summary>Ghost đang hiển thị (nếu có). Chỉ cho phép 1 ghost cùng lúc.</summary>
	private BuildingGhostBase _currentGhost = null;

	/// <summary>Scene ghost nhà — preload sẵn để dùng khi bấm hotkey.</summary>
	private PackedScene _houseGhostScene;

	/// <summary>Đang ở chế độ xây dựng hay không.</summary>
	public bool IsBuildMode => _currentGhost != null && IsInstanceValid(_currentGhost);

	public override void _Ready()
	{
		if (Instance == null)
		{
			Instance = this;
		}

		_houseGhostScene = GD.Load<PackedScene>("res://Buildings/House/house.tscn");
		
		UpdateUI(); 
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Bấm phím B → Bắt đầu chế độ xây nhà
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.B && !IsBuildMode)
			{
				StartBuildMode(_houseGhostScene);
				GetViewport().SetInputAsHandled();
			}
		}
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
				return; 
		}
		
		
		GD.Print($"[Kinh tế] +{amount} {type} | Tổng: Gỗ({Wood}) Thịt({Food}) Vàng({Gold})");
		UpdateUI();
	}
	
	private void UpdateUI()
	{
		if (WoodLabel != null) WoodLabel.Text = $"Gỗ: {Wood}";
		if (FoodLabel != null) FoodLabel.Text = $"Thực: {Food}";
		if (GoldLabel != null) GoldLabel.Text = $"Vàng: {Gold}";
	}

	// ──────────────────── XÂY DỰNG ────────────────────

	public void StartBuildMode(PackedScene ghostScene)
	{
		// Nếu đang có ghost cũ → hủy trước
		if (_currentGhost != null && IsInstanceValid(_currentGhost))
		{
			_currentGhost.QueueFree();
			_currentGhost = null;
		}

		if (ghostScene == null)
		{
			GD.PrintErr("[GameManager] ghostScene là null, không thể bắt đầu build mode!");
			return;
		}

		Node instance = ghostScene.Instantiate();
		if (instance is BuildingGhostBase ghost)
		{
			_currentGhost = ghost;
			ghost.BuildingPlaced += OnBuildingPlaced;
			ghost.BuildingCancelled += OnBuildingCancelled;

			GetTree().CurrentScene.AddChild(ghost);
			GD.Print("[GameManager] Bắt đầu chế độ xây dựng.");
		}
		else
		{
			GD.PrintErr("[GameManager] Scene không phải BuildingGhostBase!");
			instance.QueueFree();
		}
	}

	private void OnBuildingPlaced(Vector2 position, int textureIndex)
	{
		GD.Print($"[GameManager] Nhà đã được đặt tại {position}, hướng {textureIndex}");
		
		if (Wood >= HouseWoodCost)
		{
			Wood -= HouseWoodCost;
			UpdateUI();

			if(HouseScene != null){
				Node RealBuilding = HouseScene.Instantiate();
				if(RealBuilding is Node2D building2d){
					building2d.GlobalPosition = position;
				}
				GetTree().CurrentScene.AddChild(RealBuilding);

				NavBaker navBaker = GetTree().CurrentScene.GetNodeOrNull<NavBaker>("NavBaker");
				if (navBaker != null){
					navBaker.RebakeNavigation();
				}else{
					GD.Print("co loi khi xay nha -- navbaker loi");
				}
			}
		}
		else
		{
			GD.Print("Khong du tai nguyen");
			ShowWarningMessage("ehehe");
			
		}
		_currentGhost = null;
	}

	private void OnBuildingCancelled()
	{
		GD.Print("[GameManager] Hủy xây dựng.");
		_currentGhost = null;
	}

	private void ShowWarningMessage(string message)
{
    if (warning != null)
    {
        warning.DialogText = message;
        warning.PopupCentered();
    }
    else
    {
        GD.PrintErr($"[UI Báo Lỗi] {message}");
    }
}
}
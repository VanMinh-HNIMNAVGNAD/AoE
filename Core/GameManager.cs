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

		// Preload house ghost scene
		_houseGhostScene = GD.Load<PackedScene>("res://Buildings/House/house.tscn");
		
		// Cập nhật UI ngay khi vừa vào game
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

	// ──────────────────── XÂY DỰNG ────────────────────

	/// <summary>
	/// Bắt đầu chế độ xây dựng: tạo ghost từ PackedScene và thêm vào scene tree.
	/// Gọi từ UI hoặc hotkey. Ví dụ: GameManager.Instance.StartBuildMode(houseScene);
	/// </summary>
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
			// Kết nối signal để xử lý khi đặt nhà hoặc hủy
			ghost.BuildingPlaced += OnBuildingPlaced;
			ghost.BuildingCancelled += OnBuildingCancelled;
			// Thêm trực tiếp vào scene root — ZIndex=4096 trong BuildingGhostBase đảm bảo hiển thị trên
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
		// TODO: Trừ tài nguyên, tạo building thật tại vị trí này
		_currentGhost = null;
	}

	private void OnBuildingCancelled()
	{
		GD.Print("[GameManager] Hủy xây dựng.");
		_currentGhost = null;
	}
}
using Godot;
using System;

public partial class RTSController : Node2D
{
	[Export] public Panel SelectionPanel; // Kéo cái UI xanh vào đây

	private Vector2 _dragStartGlobal; // Điểm bắt đầu (Tọa độ Game)
	private Vector2 _dragStartScreen; // Điểm bắt đầu (Tọa độ Màn hình)
	private bool _isDragging = false;

	// Biến để vẽ Debug (Khung màu đỏ)
	private Rect2 _debugRect = new Rect2();
	private bool _showDebug = false;

	public override void _Ready()
	{
		// QUAN TRỌNG: Đặt MouseFilter = Ignore để Panel không nuốt sự kiện chuột
		// Nếu không, khi Panel hiện lên trong lúc kéo, nó sẽ chặn mouse release
		// khiến EndSelection() không bao giờ được gọi
		if (SelectionPanel != null)
		{
			SelectionPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					// --- BẮT ĐẦU KÉO ---
					_isDragging = true;
					_dragStartGlobal = GetGlobalMousePosition();
					_dragStartScreen = GetViewport().GetMousePosition();
				}
				else if (_isDragging)
				{
					// --- THẢ CHUỘT (KẾT THÚC) ---
					_isDragging = false;
					if (SelectionPanel != null) SelectionPanel.Visible = false;
					
					// Gọi hàm chọn và vẽ debug
					EndSelection(GetGlobalMousePosition());
				}
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
			{
				Vector2 targetPoint = GetGlobalMousePosition();
				Node2D targetObj = GetObjectUnderMouse();

				var allUnits = GetTree().GetNodesInGroup("Units");
				foreach (Node node in allUnits)
				{
					if (node is SelectableUnit unit && unit.IsSelected)
					{
						if (targetObj != null && targetObj.IsInGroup("Resource"))
						{
							unit.SetInteractTarget(targetObj);
							GD.Print($"[LỆNH] Lính {unit.Name} đi chặt cây {targetObj.Name}!");
						}
						else
						{
							unit.MoveToTarget(targetPoint);
							GD.Print($"[LỆNH] Lính {unit.Name} di chuyển đến {targetPoint}!");
						}
					}
				}
			}
		}
		else if (@event is InputEventMouseMotion mouseMotion && _isDragging)
		{
			UpdateSelectionBoxVisual();
		}
	}

	private void UpdateSelectionBoxVisual()
	{
		if (SelectionPanel == null) return;
		
		// Cập nhật UI Xanh (Màn hình)
		Vector2 currentScreen = GetViewport().GetMousePosition();
		Rect2 screenRect = new Rect2(_dragStartScreen, currentScreen - _dragStartScreen).Abs();
		
		if (screenRect.Size.Length() > 5)
		{
			SelectionPanel.Visible = true;
			SelectionPanel.Position = screenRect.Position;
			SelectionPanel.Size = screenRect.Size;
		}
	}

	private void EndSelection(Vector2 dragEndGlobal)
	{
		// Tạo vùng chọn trong thế giới Game
		Rect2 selectionRect = new Rect2(_dragStartGlobal, dragEndGlobal - _dragStartGlobal).Abs();

		// Xử lý click đơn (nếu kéo quá nhỏ)
		if (selectionRect.Size.Length() < 5)
		{
			selectionRect.Size = new Vector2(10, 10);
			selectionRect.Position -= new Vector2(5, 5); // Căn giữa
		}

		// --- PHẦN DEBUG QUAN TRỌNG ---
		_debugRect = selectionRect; // Lưu lại để vẽ màu đỏ
		_showDebug = true;
		QueueRedraw(); // Yêu cầu Godot vẽ lại ngay

		GD.Print("\n--- KẾT QUẢ QUÉT ---");
		GD.Print($"Vùng quét (World): {selectionRect}");
		
		var allUnits = GetTree().GetNodesInGroup("Units");
		GD.Print($"Tổng số lính trong Group 'Units': {allUnits.Count}");

		bool found = false;
		foreach (Node node in allUnits)
		{
			if (node is SelectableUnit unit)
			{
				// In ra khoảng cách để biết sai bao nhiêu
				bool isInside = selectionRect.HasPoint(unit.GlobalPosition);
				
				if (isInside)
				{
					GD.Print($"✅ TRÚNG: {unit.Name} | Vị trí lính: {unit.GlobalPosition}");
					unit.IsSelected = true;
					found = true;
				}
				else
				{
					GD.Print($"❌ TRƯỢT: {unit.Name} | Vị trí lính: {unit.GlobalPosition}");
					// Nếu muốn giữ chọn nhiều con thì thêm phím Shift vào đây
					unit.IsSelected = false; 
				}
			}
		}
		
		if (!found) GD.Print("=> KHÔNG CHỌN ĐƯỢC AI CẢ!");
	}

	// Hàm vẽ debug của Godot (Vẽ hình chữ nhật đỏ lên mặt đất)
	public override void _Draw()
	{
		if (_showDebug)
		{
			// Vẽ khung đỏ, nét mảnh, không tô màu nền
			DrawRect(_debugRect, Colors.Red, false, 2.0f);
		}
	}
	private Node2D GetObjectUnderMouse()
	{
		var spaceState = GetWorld2D().DirectSpaceState;
		var query = new PhysicsPointQueryParameters2D();
		query.Position = GetGlobalMousePosition();
		query.CollideWithBodies = true; 
		query.CollideWithAreas = false;

		var result = spaceState.IntersectPoint(query);
		foreach (Godot.Collections.Dictionary item in result)
	{
		Node2D collider = (Node2D)item["collider"];
		
		// Nếu thấy bất cứ cái nào có thẻ "Resource", ƯU TIÊN chọn nó luôn!
		if (collider.IsInGroup("Resource"))
		{
			return collider;
		}
	}

	// Nếu không có tài nguyên nào (click ra đất), thì mới trả về cái mặt đất
	if (result.Count > 0)
	{
		return (Node2D)result[0]["collider"];
	}

	return null;
	}
}

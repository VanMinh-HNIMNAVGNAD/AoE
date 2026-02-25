using Godot;
using System;

public partial class RTSController : Node2D
{
	[Export] public Panel SelectionPanel;

	private Vector2 _dragStartGlobal;
	private Vector2 _dragStartScreen;
	private bool _isDragging = false;

	private Rect2 _debugRect = new Rect2();
	private bool _showDebug = false;

	public override void _Ready()
	{
		if (SelectionPanel != null)
		{
			SelectionPanel.MouseFilter = Control.MouseFilterEnum.Ignore;
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// Nếu đang ở chế độ xây dựng → không xử lý input chọn lính / ra lệnh
		if (GameManager.Instance != null && GameManager.Instance.IsBuildMode)
			return;

		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					_isDragging = true;
					_dragStartGlobal = GetGlobalMousePosition();
					_dragStartScreen = GetViewport().GetMousePosition();

					// [FIX] Reset debug rect khi bắt đầu kéo mới — trước đây _showDebug không bao giờ tắt
					_showDebug = false;
					QueueRedraw();
				}
				else if (_isDragging)
				{
					_isDragging = false;
					if (SelectionPanel != null) SelectionPanel.Visible = false;
					EndSelection(GetGlobalMousePosition());
				}
			}
			else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
			{
				Vector2 targetPoint = GetGlobalMousePosition();
				Node2D targetObj = GetObjectUnderMouse();

				// [FIX] Đếm số lính đang chọn để tính formation offset
				// Trước đây tất cả unit đi đến cùng 1 điểm → avoidance system
				// tạo traffic jam, unit đẩy nhau và oscillate liên tục
				var allUnits = GetTree().GetNodesInGroup("Units");
				var selectedUnits = new System.Collections.Generic.List<SelectableUnit>();
				foreach (Node node in allUnits)
				{
					if (node is SelectableUnit unit && unit.IsSelected)
						selectedUnits.Add(unit);
				}

				for (int i = 0; i < selectedUnits.Count; i++)
				{
					var unit = selectedUnits[i];
					if (targetObj != null && targetObj.IsInGroup("Resource"))
					{
						unit.SetInteractTarget(targetObj);
					}
					else
					{
						// [FIX] Formation offset: xếp lính theo lưới xung quanh điểm click
						// Tránh tất cả unit dồn 1 pixel → avoidance đẩy lung tung
						Vector2 offset = GetFormationOffset(i, selectedUnits.Count);
						unit.MoveToTarget(targetPoint + offset);
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
		Rect2 selectionRect = new Rect2(_dragStartGlobal, dragEndGlobal - _dragStartGlobal).Abs();

		if (selectionRect.Size.Length() < 5)
		{
			selectionRect.Size = new Vector2(10, 10);
			selectionRect.Position -= new Vector2(5, 5);
		}

		_debugRect = selectionRect;
		_showDebug = true;
		QueueRedraw();

		// [LƯU Ý] Mỗi lần kéo thả sẽ BỎ CHỌN tất cả unit ngoài vùng.
		// Nếu muốn thêm Shift+Click giữ chọn, cần kiểm tra modifier key ở đây.
		var allUnits = GetTree().GetNodesInGroup("Units");
		foreach (Node node in allUnits)
		{
			if (node is SelectableUnit unit)
			{
				bool isInside = selectionRect.HasPoint(unit.GlobalPosition);
				unit.IsSelected = isInside;
			}
		}
	}

	public override void _Draw()
	{
		if (_showDebug)
		{
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
			if (collider.IsInGroup("Resource"))
			{
				return collider;
			}
		}

		if (result.Count > 0)
		{
			return (Node2D)result[0]["collider"];
		}

		return null;
	}

	/// <summary>
	/// Tính offset formation cho từng unit khi di chuyển theo nhóm.
	/// Xếp theo lưới vuông, cách nhau SPACING pixel, căn giữa quanh điểm click.
	/// Nếu chỉ có 1 unit → offset = Zero (đi thẳng đến điểm click).
	/// </summary>
	private Vector2 GetFormationOffset(int unitIndex, int totalUnits)
	{
		if (totalUnits <= 1) return Vector2.Zero;

		const float SPACING = 40.0f; // Khoảng cách giữa các unit trong formation
		int columns = Mathf.CeilToInt(Mathf.Sqrt(totalUnits));
		int row = unitIndex / columns;
		int col = unitIndex % columns;

		// Căn giữa formation quanh điểm (0,0)
		float totalWidth = (columns - 1) * SPACING;
		int rowsCount = Mathf.CeilToInt((float)totalUnits / columns);
		float totalHeight = (rowsCount - 1) * SPACING;

		float x = col * SPACING - totalWidth / 2.0f;
		float y = row * SPACING - totalHeight / 2.0f;

		return new Vector2(x, y);
	}
}

// ========================== LUỒNG HOẠT ĐỘNG CỦA RTSController ==========================
//
// RTSController là bộ điều khiển trung tâm xử lý input chuột của người chơi.
// Nó chịu trách nhiệm 2 việc chính: CHỌN LÍNH và RA LỆNH.
//
// ─── CÁC ĐIỂM ĐÃ FIX ───
//
//   1. [FIX] _showDebug bây giờ được reset khi bắt đầu kéo mới — trước đây debug rect vẽ mãi
//   2. [FIX] Xóa biến 'found' không dùng trong EndSelection()
//   3. [LƯU Ý] _Draw() vẽ debugRect theo tọa độ local — nếu RTSController không ở (0,0) sẽ bị lệch
//   4. [LƯU Ý] GetObjectUnderMouse() ưu tiên Resource, không phân biệt team — cần mở rộng khi có Enemy
//
// ─── 1. CHỌN LÍNH (Chuột trái - Kéo thả) ───
//
//   Người chơi nhấn giữ chuột trái → kéo → thả:
//
//   [Nhấn chuột trái]
//       │
//       ▼
//   _UnhandledInput() bắt sự kiện MouseButton.Left.Pressed
//       → Lưu vị trí bắt đầu: _dragStartGlobal (tọa độ game) + _dragStartScreen (tọa độ màn hình)
//       → _isDragging = true
//       │
//       ▼
//   [Kéo chuột]
//       │
//       ▼
//   _UnhandledInput() bắt sự kiện MouseMotion khi _isDragging == true
//       → Gọi UpdateSelectionBoxVisual()
//       → Tính Rect2 trên màn hình từ _dragStartScreen đến vị trí chuột hiện tại
//       → Cập nhật vị trí + kích thước SelectionPanel (hình chữ nhật xanh trên UI)
//       │
//       ▼
//   [Thả chuột trái]
//       │
//       ▼
//   _UnhandledInput() bắt sự kiện MouseButton.Left released
//       → Ẩn SelectionPanel
//       → Gọi EndSelection(vị trí thả)
//           │
//           ▼
//       EndSelection():
//           → Tạo Rect2 selectionRect trong tọa độ GAME (world) từ _dragStartGlobal đến dragEndGlobal
//           → Nếu vùng quá nhỏ (click đơn) → mở rộng thành 10x10 pixel
//           → Lưu vào _debugRect để _Draw() vẽ khung đỏ debug
//           → Duyệt tất cả node trong group "Units":
//               - Nếu GlobalPosition của unit nằm TRONG selectionRect → IsSelected = true
//               - Nếu NGOÀI → IsSelected = false
//
// ─── 2. RA LỆNH (Chuột phải) ───
//
//   Người chơi click chuột phải vào một điểm trên bản đồ:
//
//   [Click chuột phải]
//       │
//       ▼
//   _UnhandledInput() bắt sự kiện MouseButton.Right.Pressed
//       → Lấy targetPoint = vị trí chuột trong game
//       → Gọi GetObjectUnderMouse() để kiểm tra có vật thể nào tại vị trí đó không
//           │
//           ├── Có object thuộc group "Resource" (cây, mỏ vàng...)
//           │       → Duyệt tất cả unit đang được chọn (IsSelected == true)
//           │       → Gọi unit.SetInteractTarget(targetObj) → lính đi khai thác
//           │
//           └── Không có resource (click ra đất trống)
//                   → Duyệt tất cả unit đang được chọn
//                   → Gọi unit.MoveToTarget(targetPoint) → lính di chuyển đến điểm đó
//
// ─── 3. GetObjectUnderMouse() ───
//
//   Dùng PhysicsPointQueryParameters2D để raycast tại vị trí chuột:
//       → Ưu tiên trả về object thuộc group "Resource" nếu có
//       → Nếu không có resource → trả về collider đầu tiên (mặt đất)
//       → Nếu không có gì → trả về null
//
// ─── 4. _Draw() ───
//
//   Godot gọi _Draw() khi QueueRedraw() được gọi.
//   Vẽ khung chữ nhật đỏ (_debugRect) lên tọa độ game để debug vùng chọn.
//
// ========================== HẾT ==========================

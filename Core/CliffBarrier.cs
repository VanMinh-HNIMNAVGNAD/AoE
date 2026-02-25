using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// CliffBarrier — Tự động đặt StaticBody2D rào cản vô hình dọc theo rìa đồi/vách đá.
///
/// ─── VẤN ĐỀ ───
///
///   Godot 2D navigation không hiểu "độ cao". Khi tile trên đỉnh đồi và tile mặt đất
///   cùng có navigation polygon, NavBaker có thể merge chúng → unit "nhảy" từ đồi xuống
///   mà không cần đi cầu thang.
///
/// ─── GIẢI PHÁP: CLIFF BARRIER ───
///
///   1. Quét TileMapLayer tìm tile rìa đồi (cliff edge)
///   2. Đặt StaticBody2D vô hình (không render) tại vị trí rìa đồi
///   3. NavBaker bake → thấy StaticBody2D → khoét lỗ trên nav mesh
///   4. Unit BẮT BUỘC phải đi qua ramp/cầu thang (tile không có barrier)
///
/// ─── CÁCH DÙNG ───
///
///   1. Gắn script này vào Node trong scene (cùng level với NavBaker)
///   2. Set ElevationLayerPath → TileMapLayer chứa tile elevation/cliff
///   3. Set CliffTileCoords → danh sách atlas coord của tile rìa đồi
///      HOẶC dùng CliffGroupName để đánh dấu tile cliff bằng custom data
///   4. Set RampTileCoords → tile cầu thang/ramp (KHÔNG đặt barrier ở đây)
///   5. CliffBarrier chạy TRƯỚC NavBaker (Priority cao hơn hoặc gọi thủ công)
///
/// ─── CÁCH THIẾT LẬP ĐƠN GIẢN NHẤT ───
///
///   Nếu không muốn code phức tạp, bạn có thể:
///   • Tạo TileMapLayer mới tên "CliffBarrierLayer"
///   • Vẽ tile ở những vị trí rìa đồi (cùng tile set hoặc tile đặc biệt)
///   • Set UseBarrierLayer = true, BarrierLayerPath = đường dẫn đến layer đó
///   • Script sẽ đọc tile positions từ layer → đặt StaticBody2D tại đó
///
/// </summary>
public partial class CliffBarrier : Node
{
	// ─── OPTION A: Dùng TileMapLayer riêng để đánh dấu cliff ───
	[ExportGroup("Option A: Barrier Layer")]
	[Export] public bool UseBarrierLayer = true;
	/// <summary>
	/// Đường dẫn đến TileMapLayer dùng để đánh dấu vị trí rào cản.
	/// Vẽ tile ở đâu → StaticBody2D đặt ở đó.
	/// </summary>
	[Export] public NodePath BarrierLayerPath;

	// ─── OPTION B: Tự động detect từ Elevation layer ───
	[ExportGroup("Option B: Auto Detect")]
	[Export] public bool UseAutoDetect = false;
	/// <summary>Đường dẫn đến TileMapLayer chứa tile elevation (vách đá, đồi).</summary>
	[Export] public NodePath ElevationLayerPath;
	/// <summary>Đường dẫn đến TileMapLayer chứa tile mặt đất (cỏ dưới chân đồi).</summary>
	[Export] public NodePath GroundLayerPath;

	// ─── CHUNG ───
	[ExportGroup("Cấu hình")]
	/// <summary>Kích thước barrier (nên bằng hoặc hơi lớn hơn tile size).</summary>
	[Export] public Vector2 BarrierSize = new Vector2(16, 16);
	/// <summary>Node cha để chứa các StaticBody2D barrier (mặc định = tạo mới).</summary>
	[Export] public NodePath BarrierContainerPath;

	private Node _barrierContainer;
	private int _barrierCount = 0;

	public override void _Ready()
	{
		// Tạo container chứa barriers
		if (BarrierContainerPath != null && !BarrierContainerPath.IsEmpty)
		{
			_barrierContainer = GetNode(BarrierContainerPath);
		}

		if (_barrierContainer == null)
		{
			_barrierContainer = new Node2D();
			_barrierContainer.Name = "CliffBarriers";
			GetTree().CurrentScene.CallDeferred(Node.MethodName.AddChild, _barrierContainer);
		}

		// Chờ 1 frame để đảm bảo tất cả node đã sẵn sàng
		CallDeferred(nameof(GenerateBarriers));
	}

	private void GenerateBarriers()
	{
		if (UseBarrierLayer)
		{
			GenerateFromBarrierLayer();
		}
		else if (UseAutoDetect)
		{
			GenerateFromAutoDetect();
		}
		else
		{
			GD.PrintErr("[CliffBarrier] Chưa chọn phương thức! Bật UseBarrierLayer hoặc UseAutoDetect.");
		}
	}

	// ═══════════════════════════════════════════════════
	// OPTION A: Đọc từ TileMapLayer đánh dấu
	// ═══════════════════════════════════════════════════

	/// <summary>
	/// Đọc tất cả tile đã vẽ trong BarrierLayer → đặt StaticBody2D tại đó.
	/// Đây là cách dễ nhất: bạn vẽ tile ở rìa đồi trong editor → barrier tự tạo.
	/// </summary>
	private void GenerateFromBarrierLayer()
	{
		if (BarrierLayerPath == null || BarrierLayerPath.IsEmpty)
		{
			GD.PrintErr("[CliffBarrier] BarrierLayerPath chưa được gán!");
			return;
		}

		var barrierLayer = GetNode<TileMapLayer>(BarrierLayerPath);
		if (barrierLayer == null)
		{
			GD.PrintErr("[CliffBarrier] Không tìm thấy TileMapLayer tại BarrierLayerPath!");
			return;
		}

		var usedCells = barrierLayer.GetUsedCells();
		GD.Print($"[CliffBarrier] Tìm thấy {usedCells.Count} tile barrier trong layer '{barrierLayer.Name}'");

		foreach (Vector2I cellCoord in usedCells)
		{
			// Chuyển từ tọa độ tile → tọa độ world
			Vector2 worldPos = barrierLayer.MapToLocal(cellCoord);
			// Tính thêm offset nếu layer có global transform
			worldPos = barrierLayer.ToGlobal(worldPos);

			CreateBarrierAt(worldPos);
		}

		// Ẩn layer đánh dấu (không cần hiển thị in-game)
		barrierLayer.Visible = false;

		GD.Print($"[CliffBarrier] Đã tạo {_barrierCount} barrier từ BarrierLayer.");
	}

	// ═══════════════════════════════════════════════════
	// OPTION B: Tự động detect rìa đồi
	// ═══════════════════════════════════════════════════

	/// <summary>
	/// Tự động tìm rìa đồi: tile elevation có tile mặt đất liền kề bên dưới.
	/// Phức tạp hơn nhưng không cần vẽ thủ công.
	/// </summary>
	private void GenerateFromAutoDetect()
	{
		if (ElevationLayerPath == null || ElevationLayerPath.IsEmpty)
		{
			GD.PrintErr("[CliffBarrier] ElevationLayerPath chưa được gán!");
			return;
		}

		var elevationLayer = GetNode<TileMapLayer>(ElevationLayerPath);
		if (elevationLayer == null)
		{
			GD.PrintErr("[CliffBarrier] Không tìm thấy Elevation TileMapLayer!");
			return;
		}

		TileMapLayer groundLayer = null;
		if (GroundLayerPath != null && !GroundLayerPath.IsEmpty)
		{
			groundLayer = GetNode<TileMapLayer>(GroundLayerPath);
		}

		var elevationCells = elevationLayer.GetUsedCells();
		var elevationSet = new HashSet<Vector2I>();
		foreach (var cell in elevationCells)
		{
			elevationSet.Add(cell);
		}

		GD.Print($"[CliffBarrier] Auto-detect: {elevationCells.Count} tile elevation");

		// Tìm rìa: tile elevation mà có ít nhất 1 hướng KHÔNG có tile elevation
		// (= rìa đồi, nơi unit có thể "nhảy" xuống)
		Vector2I[] directions = new Vector2I[]
		{
			new Vector2I(0, -1),  // Trên
			new Vector2I(0, 1),   // Dưới
			new Vector2I(-1, 0),  // Trái
			new Vector2I(1, 0),   // Phải
		};

		int edgeCount = 0;
		foreach (Vector2I cell in elevationCells)
		{
			foreach (Vector2I dir in directions)
			{
				Vector2I neighbor = cell + dir;

				// Nếu hàng xóm KHÔNG phải elevation tile → đây là rìa
				if (!elevationSet.Contains(neighbor))
				{
					// Kiểm tra thêm: nếu hàng xóm là ground tile → cần barrier
					bool needsBarrier = true;

					if (groundLayer != null)
					{
						// Chỉ đặt barrier nếu phía đó CÓ tile ground (có thể đi được)
						var tileData = groundLayer.GetCellTileData(neighbor);
						needsBarrier = tileData != null;
					}

					if (needsBarrier)
					{
						Vector2 worldPos = elevationLayer.ToGlobal(
							elevationLayer.MapToLocal(cell)
						);
						CreateBarrierAt(worldPos);
						edgeCount++;
					}
				}
			}
		}

		GD.Print($"[CliffBarrier] Auto-detect: {edgeCount} rìa → {_barrierCount} barriers.");
	}

	// ═══════════════════════════════════════════════════
	// TẠO STATIC BODY
	// ═══════════════════════════════════════════════════

	/// <summary>
	/// Tạo 1 StaticBody2D vô hình tại vị trí worldPos.
	/// NavBaker sẽ thấy body này và khoét lỗ trên nav mesh.
	/// </summary>
	private void CreateBarrierAt(Vector2 worldPos)
	{
		var body = new StaticBody2D();
		body.Name = $"CliffBarrier_{_barrierCount}";
		body.GlobalPosition = worldPos;

		// Collision shape
		var shape = new RectangleShape2D();
		shape.Size = BarrierSize;

		var collision = new CollisionShape2D();
		collision.Shape = shape;

		body.AddChild(collision);

		// ĐẶT COLLISION LAYER cho barrier:
		// - Layer 1 (bit 0): collision chung (để NavBaker detect)
		// - Nhưng KHÔNG đặt trên unit collision layer nếu bạn dùng layer riêng
		// Mặc định: layer 1 = environment collision
		body.CollisionLayer = 1;
		body.CollisionMask = 0; // Barrier không cần detect gì

		_barrierContainer.CallDeferred(Node.MethodName.AddChild, body);
		_barrierCount++;
	}

	/// <summary>
	/// Xóa tất cả barrier hiện tại (gọi trước khi regenerate).
	/// </summary>
	public void ClearBarriers()
	{
		if (_barrierContainer == null) return;

		foreach (Node child in _barrierContainer.GetChildren())
		{
			child.QueueFree();
		}
		_barrierCount = 0;
		GD.Print("[CliffBarrier] Đã xóa tất cả barriers.");
	}

	/// <summary>
	/// Regenerate barriers (ví dụ sau khi thay đổi map).
	/// </summary>
	public void Regenerate()
	{
		ClearBarriers();
		// Chờ QueueFree hoàn tất
		CallDeferred(nameof(GenerateBarriers));
	}
}

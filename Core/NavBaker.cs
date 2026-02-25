using Godot;
using System;

/// <summary>
/// NavBaker — Tự động bake lại NavigationPolygon khi game khởi động.
///
/// CÁCH DÙNG: Tạo 1 Node mới trong World.tscn, gắn script NavBaker.cs.
///
/// ─── NGUYÊN LÝ HOẠT ĐỘNG (Tile-Based Navigation) ───
///
/// Thay vì dùng 1 polygon lớn bao phủ toàn map rồi khoét lỗ bằng StaticBody,
/// hệ thống mới ĐỌC NAVIGATION POLYGON TỪ TỪNG TILE trong TileMapLayer:
///
///   • Tile có navigation polygon (đất, cỏ, ramp, cầu) → unit ĐI ĐƯỢC
///   • Tile KHÔNG có navigation polygon (nước, vách đá) → unit KHÔNG ĐI ĐƯỢC
///   • StaticBody2D (đá, cây, nhà) → khoét lỗ thêm trên vùng đi được
///
/// parsed_geometry_type = 2 (Both) → đọc cả TileMap nav polygon + StaticBody obstacle
/// source_geometry_mode  = 0 (RootNodeChildren) → tìm từ root scene, thấy tất cả
///
/// ─── CÁCH THIẾT LẬP TRONG EDITOR ───
///
///   1. Mở TileSet → chọn từng tile → tab Navigation → vẽ polygon cho tile đi được
///   2. Tile nước, vách đá: KHÔNG vẽ navigation polygon
///   3. Tile ramp/bậc thang: CÓ vẽ navigation polygon (nối vùng thấp ↔ cao)
///   4. NavBaker sẽ tự merge tất cả tile nav polygon thành 1 navigation mesh
///
/// </summary>
public partial class NavBaker : Node
{
	[Export] public NodePath NavRegionPath;
	[Export] public bool BakeOnReady = true;

	private NavigationRegion2D _navRegion;

	public override void _Ready()
	{
		// Tìm NavigationRegion2D
		if (NavRegionPath != null && !NavRegionPath.IsEmpty)
		{
			_navRegion = GetNode<NavigationRegion2D>(NavRegionPath);
		}

		if (_navRegion == null)
		{
			_navRegion = FindNavRegion(GetTree().Root);
		}

		if (_navRegion == null)
		{
			GD.PrintErr("[NavBaker] Không tìm thấy NavigationRegion2D!");
			return;
		}

		if (BakeOnReady)
		{
			// Chờ physics đăng ký xong tất cả StaticBody2D rồi mới bake
			CallDeferred(nameof(WaitAndBake));
		}
	}

	private async void WaitAndBake()
	{
		// Chờ 2 physics frame → đảm bảo tất cả node đã đăng ký
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		// Chờ thêm 1 frame nữa cho CliffBarrier tạo xong StaticBody2D
		// (CliffBarrier dùng CallDeferred → cần thêm 1 frame)
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		DoBake();
	}

	private void DoBake()
	{
		var navPoly = _navRegion.NavigationPolygon;
		if (navPoly == null)
		{
			GD.PrintErr("[NavBaker] NavigationPolygon chưa được gán!");
			return;
		}

		// ─── BƯỚC 1: XÓA OUTLINE CŨ ───
		// NavigationPolygon cũ có 1 outline lớn bao phủ toàn map
		// Outline → toàn bộ map walkable → nước cũng đi được (SAI)
		// Xóa outline → chỉ tile có navigation polygon mới walkable
		while (navPoly.GetOutlineCount() > 0)
		{
			navPoly.RemoveOutline(0);
		}

		// ─── BƯỚC 2: CẤU HÌNH ───
		// Both: đọc navigation polygon từ TileMapLayer + khoét lỗ bằng StaticBody2D
		//   → tile có nav polygon = đi được, tile không có = không đi được
		//   → StaticBody2D (đá, cây, nhà) = khoét lỗ trên vùng đi được
		// RootNodeChildren: tìm toàn bộ scene tree
		navPoly.ParsedGeometryType = NavigationPolygon.ParsedGeometryTypeEnum.Both;
		navPoly.SourceGeometryMode = NavigationPolygon.SourceGeometryModeEnum.RootNodeChildren;
		navPoly.AgentRadius = 0.0f;

		// ─── DEBUG ───
		GD.Print("═══════════════════════════════════════");
		GD.Print("[NavBaker] BẮT ĐẦU BAKE RUNTIME (Tile-Based)");
		GD.Print($"  ParsedGeometryType: {navPoly.ParsedGeometryType}");
		GD.Print($"  SourceGeometryMode: {navPoly.SourceGeometryMode}");
		GD.Print($"  Outlines sau xóa: {navPoly.GetOutlineCount()}");

		int tileLayerCount = 0;
		int staticBodyCount = 0;
		CountNodes(GetTree().CurrentScene, ref tileLayerCount, ref staticBodyCount);
		GD.Print($"  TileMapLayer tìm thấy: {tileLayerCount}");
		GD.Print($"  StaticBody2D (vật cản): {staticBodyCount}");

		// ─── BƯỚC 3: PARSE + BAKE ───
		var sourceGeometry = new NavigationMeshSourceGeometryData2D();
		Node rootNode = GetTree().CurrentScene;

		NavigationServer2D.ParseSourceGeometryData(navPoly, sourceGeometry, rootNode);
		GD.Print($"  Source geometry có data: {sourceGeometry.HasData()}");

		NavigationServer2D.BakeFromSourceGeometryData(navPoly, sourceGeometry);

		// Force cập nhật
		_navRegion.NavigationPolygon = navPoly;

		// ─── KẾT QUẢ ───
		GD.Print($"  KẾT QUẢ: Vertices={navPoly.Vertices.Length}, Polygons={navPoly.GetPolygonCount()}");
		if (navPoly.GetPolygonCount() == 0)
		{
			GD.PrintErr("[NavBaker] ⚠️ Nav mesh TRỐNG! Kiểm tra tile đã gán navigation polygon chưa.");
		}
		GD.Print("[NavBaker] BAKE HOÀN TẤT!");
		GD.Print("═══════════════════════════════════════");
	}

	/// <summary>
	/// Gọi thủ công khi cần rebake (ví dụ: sau khi phá hủy building/cây).
	/// </summary>
	public void RebakeNavigation()
	{
		DoBake();
	}

	private NavigationRegion2D FindNavRegion(Node root)
	{
		if (root is NavigationRegion2D nav) return nav;
		foreach (Node child in root.GetChildren())
		{
			var result = FindNavRegion(child);
			if (result != null) return result;
		}
		return null;
	}

	/// <summary>Đếm số TileMapLayer và StaticBody2D trong scene (debug).</summary>
	private void CountNodes(Node node, ref int tileLayers, ref int staticBodies)
	{
		if (node is TileMapLayer) tileLayers++;
		if (node is StaticBody2D) staticBodies++;
		foreach (Node child in node.GetChildren())
		{
			CountNodes(child, ref tileLayers, ref staticBodies);
		}
	}
}

using Godot;
using System;

/// <summary>
/// NavBaker — Tự động bake lại NavigationPolygon khi game khởi động.
///
/// CÁCH DÙNG: Tạo 1 Node mới trong World.tscn, gắn script NavBaker.cs.
///
/// BUG GỐC ĐÃ FIX:
/// ─────────────────────────────────────────────────────────────────
/// 1. BakeNavigationPolygon() mặc định dùng NavPath làm ROOT để tìm vật cản.
///    NavPath nằm trong map1.tscn → không bao giờ thấy Element (group "Colli")
///    ở World.tscn → polygon bake ra KHÔNG CÓ LỖ.
///
/// 2. FIX: Dùng NavigationServer2D.ParseSourceGeometryData() trực tiếp,
///    truyền GetTree().CurrentScene (World) làm root → thấy TOÀN BỘ vật cản.
/// ─────────────────────────────────────────────────────────────────
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
		// Chờ 2 physics frame → đảm bảo tất cả StaticBody2D đã đăng ký vào physics space
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
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

		// ─── DEBUG: In thông tin trước khi bake ───
		GD.Print("═══════════════════════════════════════");
		GD.Print("[NavBaker] BẮT ĐẦU BAKE RUNTIME");
		GD.Print($"  Outlines: {navPoly.GetOutlineCount()}");
		if (navPoly.GetOutlineCount() > 0)
		{
			var outline = navPoly.GetOutline(0);
			GD.Print($"  Outline[0] points: {outline.Length}");
		}
		GD.Print($"  ParsedGeometryType: {navPoly.ParsedGeometryType}");
		GD.Print($"  SourceGeometryMode: {navPoly.SourceGeometryMode}");
		GD.Print($"  SourceGeometryGroupName: {navPoly.SourceGeometryGroupName}");

		// Đếm nodes trong group "Colli" để debug
		var colliNodes = GetTree().GetNodesInGroup("Colli");
		GD.Print($"  Nodes trong group 'Colli': {colliNodes.Count}");
		foreach (Node n in colliNodes)
		{
			GD.Print($"    - {n.Name} ({n.GetType().Name}) path={n.GetPath()}");
		}

		// ─── BAKE BẰNG NavigationServer2D (KEY FIX!) ───
		// Dùng CurrentScene (World) làm root → tìm được Element + rocks + trees
		// KHÔNG dùng BakeNavigationPolygon() vì nó chỉ tìm trong con cháu của NavPath
		var sourceGeometry = new NavigationMeshSourceGeometryData2D();
		Node rootNode = GetTree().CurrentScene;
		GD.Print($"  Root node cho parsing: {rootNode.Name} ({rootNode.GetPath()})");

		NavigationServer2D.ParseSourceGeometryData(navPoly, sourceGeometry, rootNode);
		GD.Print($"  Source geometry có data: {sourceGeometry.HasData()}");

		NavigationServer2D.BakeFromSourceGeometryData(navPoly, sourceGeometry);

		// Force NavigationRegion2D cập nhật polygon mới
		_navRegion.NavigationPolygon = navPoly;

		// ─── DEBUG: In kết quả sau bake ───
		GD.Print($"  KẾT QUẢ: Vertices={navPoly.Vertices.Length}, Polygons={navPoly.GetPolygonCount()}");
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
}

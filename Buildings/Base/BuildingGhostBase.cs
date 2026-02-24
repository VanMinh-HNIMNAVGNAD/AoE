using Godot;
using System;

/// <summary>
/// Lớp cha cho TẤT CẢ các bóng mờ công trình (Ghost).
/// Đã bao gồm: Đi theo chuột, Check va chạm, Đổi màu và XOAY BẰNG CÁCH ĐỔI ẢNH (Phím R).
/// </summary>
public partial class BuildingGhostBase : Node2D
{
    [ExportGroup("Tham chiếu Node")]
    [Export] public Sprite2D GhostSprite;
    [Export] public Area2D CollisionArea;

    [ExportGroup("Dữ liệu Hình ảnh")]
    // Mảng chứa các mặt khác nhau của ngôi nhà
    [Export] public Godot.Collections.Array<Texture2D> BuildingTextures = new Godot.Collections.Array<Texture2D>();
    private int _currentTextureIndex = 0;

    protected bool _isValidPosition = true;
    private int _overlappingCount = 0;

    public override void _Ready()
    {
        // [FIX] Đặt ZIndex cao để ghost luôn hiển thị TRÊN tilemap và các object khác
        ZIndex = 100;

        if (CollisionArea != null)
        {
            // [FIX] Ghost không cần bị detect bởi object khác → collision_layer = 0
            // Chỉ detect các vật cản trên layer 1 (nhưng sẽ lọc thêm trong callback)
            CollisionArea.CollisionLayer = 0;
            CollisionArea.CollisionMask = 1;

            CollisionArea.BodyEntered += OnBodyEntered;
            CollisionArea.BodyExited += OnBodyExited;
            CollisionArea.AreaEntered += OnAreaEntered;
            CollisionArea.AreaExited += OnAreaExited;
        }

        // Gán hình ảnh đầu tiên khi mới xuất hiện (nếu có)
        if (BuildingTextures != null && BuildingTextures.Count > 0 && GhostSprite != null)
        {
            GhostSprite.Texture = BuildingTextures[0];
        }
        
        UpdateColor();
    }

    public override void _Process(double delta)
    {
        GlobalPosition = GetGlobalMousePosition();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // Khi bấm phím R -> Đổi ảnh sang mặt tiếp theo
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.R)
        {
            RotateBuildingVisuals();
        }

        // Khi bấm Escape → Hủy đặt nhà, xóa ghost
        if (@event is InputEventKey escKey && escKey.Pressed && escKey.Keycode == Key.Escape)
        {
            CancelPlacement();
        }

        // Click chuột trái → Xác nhận đặt nhà (nếu vị trí hợp lệ)
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (_isValidPosition)
                {
                    ConfirmPlacement();
                }
                else
                {
                    GD.Print("[Ghost] Vị trí không hợp lệ! Không thể đặt nhà ở đây.");
                }
                GetViewport().SetInputAsHandled();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right)
            {
                CancelPlacement();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    private void RotateBuildingVisuals()
    {
        // Nếu không có ảnh nào, hoặc chỉ có 1 ảnh thì không làm gì cả
        if (BuildingTextures == null || BuildingTextures.Count <= 1 || GhostSprite == null) return;

        // Tăng index lên 1. Nếu vượt quá số lượng ảnh thì quay về 0
        _currentTextureIndex = (_currentTextureIndex + 1) % BuildingTextures.Count;
        
        // Cập nhật ảnh mới cho Sprite
        GhostSprite.Texture = BuildingTextures[_currentTextureIndex];
        
        // (Tùy chọn) Nếu nhà của bạn là hình chữ nhật, bạn có thể cần xoay cả CollisionShape ở đây
        // CollisionArea.RotationDegrees += 90;
    }

    // --- LOGIC KIỂM TRA VA CHẠM ---

    /// <summary>
    /// Kiểm tra xem body có phải là vật cản cho việc đặt nhà hay không.
    /// [FIX] Bỏ qua TileMapLayer (mặt đất) — trước đây ghost luôn detect đất → luôn đỏ.
    /// Chỉ tính các vật cản thực sự: cây, đá, unit, building.
    /// </summary>
    private bool IsBlockingBody(Node body)
    {
        // Bỏ qua TileMapLayer — đây là mặt đất, không phải vật cản
        if (body is TileMapLayer) return false;
        if (body is TileMap) return false;

        // Chỉ chặn bởi các nhóm cụ thể: vật cản, tài nguyên, unit, building
        if (body is Node2D node2d)
        {
            return node2d.IsInGroup("Colli") 
                || node2d.IsInGroup("Resource") 
                || node2d.IsInGroup("Units") 
                || node2d.IsInGroup("Building");
        }

        return false;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (!IsBlockingBody(body)) return;
        _overlappingCount++;
        UpdateValidity();
    }

    private void OnBodyExited(Node2D body)
    {
        if (!IsBlockingBody(body)) return;
        _overlappingCount--;
        UpdateValidity();
    }

    private void OnAreaEntered(Area2D area) { _overlappingCount++; UpdateValidity(); }
    private void OnAreaExited(Area2D area) { _overlappingCount--; UpdateValidity(); }

    protected virtual void UpdateValidity()
    {
        _isValidPosition = _overlappingCount == 0;
        UpdateColor();
    }

    protected virtual void UpdateColor()
    {
        if (GhostSprite != null)
        {
            GhostSprite.Modulate = _isValidPosition ? new Color(0, 1, 0, 0.7f) : new Color(1, 0, 0, 0.7f);
        }
    }

    public bool IsValidPlacement()
    {
        return _isValidPosition;
    }

    // --- LOGIC ĐẶT NHÀ ---

    /// <summary>
    /// Signal phát ra khi người chơi xác nhận đặt nhà.
    /// Truyền vị trí đặt và index texture hiện tại (hướng nhà).
    /// </summary>
    [Signal]
    public delegate void BuildingPlacedEventHandler(Vector2 position, int textureIndex);

    /// <summary>
    /// Signal phát ra khi người chơi hủy đặt nhà.
    /// </summary>
    [Signal]
    public delegate void BuildingCancelledEventHandler();

    /// <summary>
    /// Xác nhận đặt nhà tại vị trí hiện tại.
    /// Phát signal BuildingPlaced rồi xóa ghost.
    /// </summary>
    protected virtual void ConfirmPlacement()
    {
        GD.Print($"[Ghost] Đặt nhà tại {GlobalPosition}, hướng {_currentTextureIndex}");
        EmitSignal(SignalName.BuildingPlaced, GlobalPosition, _currentTextureIndex);
        QueueFree();
    }

    /// <summary>
    /// Hủy bỏ đặt nhà, xóa ghost khỏi scene.
    /// </summary>
    protected virtual void CancelPlacement()
    {
        GD.Print("[Ghost] Hủy đặt nhà.");
        EmitSignal(SignalName.BuildingCancelled);
        QueueFree();
    }
}
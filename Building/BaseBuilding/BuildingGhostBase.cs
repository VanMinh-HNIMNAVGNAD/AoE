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
        if (CollisionArea != null)
        {
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

    // --- LOGIC KIỂM TRA VA CHẠM (Giữ nguyên) ---
    private void OnBodyEntered(Node2D body) { _overlappingCount++; UpdateValidity(); }
    private void OnBodyExited(Node2D body) { _overlappingCount--; UpdateValidity(); }
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
}
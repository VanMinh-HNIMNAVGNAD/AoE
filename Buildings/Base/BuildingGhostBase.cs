using Godot;
using System;

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
        ZAsRelative = false;
        ZIndex = 4096;

        if (CollisionArea != null)
        {
            CollisionArea.CollisionLayer = 0;
            CollisionArea.CollisionMask = 1;

            CollisionArea.BodyEntered += OnBodyEntered;
            CollisionArea.BodyExited += OnBodyExited;
            CollisionArea.AreaEntered += OnAreaEntered;
            CollisionArea.AreaExited += OnAreaExited;
        }

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
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && keyEvent.Keycode == Key.R)
        {
            RotateBuildingVisuals();
        }

        
        if (@event is InputEventKey escKey && escKey.Pressed && escKey.Keycode == Key.Escape)
        {
            CancelPlacement();
        }

        
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


    private bool IsBlockingBody(Node body)
    {
        
        if (body is TileMapLayer) return false;
        if (body is TileMap) return false;

        
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

    // LOGIC ĐẶT NHÀ 
    [Signal]
    public delegate void BuildingPlacedEventHandler(Vector2 position, int textureIndex);

    [Signal]
    public delegate void BuildingCancelledEventHandler();

    
    protected virtual void ConfirmPlacement()
    {
        GD.Print($"[Ghost] Đặt nhà tại {GlobalPosition}, hướng {_currentTextureIndex}");
        EmitSignal(SignalName.BuildingPlaced, GlobalPosition, _currentTextureIndex);
        QueueFree();
    }

    protected virtual void CancelPlacement()
    {
        GD.Print("[Ghost] Hủy đặt nhà.");
        EmitSignal(SignalName.BuildingCancelled);
        QueueFree();
    }
}
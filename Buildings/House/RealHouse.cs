using Godot;
using System;

// Thực thi Interface IInteractable
public partial class RealHouse : StaticBody2D, IInteractable
{
    // Lưu ý: Hãy chắc chắn trong Editor, Node ảnh của bạn là AnimatedSprite2D chứ không phải Sprite2D thường nhé!
    [Export] public AnimatedSprite2D BuildingSprite; 

    [ExportGroup("Thông số Xây dựng")]
    [Export] public int MaxHealth = 100;
    
    private int _currentHealth;
    private bool _isConstructed = false;

    public override void _Ready()
    {
        AddToGroup("Building"); 
        ZAsRelative = false; 
        ZIndex = 100; 

        // Khi vừa đặt móng, máu bắt đầu từ 1
        _currentHealth = 1;
        _isConstructed = false;
        
        if (BuildingSprite != null) 
        {
            BuildingSprite.ZIndex = ZIndex; 
            BuildingSprite.ZAsRelative = false; 
            
            // Dừng autoplay (nếu có) vì ta sẽ tự điều khiển frame bằng code
            BuildingSprite.Pause(); 
        }

        // Gọi ngay lần đầu để hiện cái móng nhà
        UpdateConstructionVisual();
    }

    // ==========================================
    // THỰC THI INTERFACE IInteractable
    // ==========================================

    public void Interact(Node2D interactor)
    {
        if (_isConstructed) return; 

        // Mỗi nhát búa cộng 10 máu (Có thể lấy từ Nông dân sau này)
        int buildPower = 10;
        _currentHealth += buildPower;
        
        GD.Print($"[NHÀ] Đang thi công... Máu: {_currentHealth}/{MaxHealth}");

        // Cập nhật hình ảnh ngay sau khi được cộng máu
        UpdateConstructionVisual();

        // Kiểm tra xem đã đầy máu chưa
        if (_currentHealth >= MaxHealth)
        {
            _currentHealth = MaxHealth;
            _isConstructed = true;
            GD.Print("[NHÀ] ĐÃ XÂY XONG!");
        }
    }

    // ─── HÀM MỚI: TỰ ĐỘNG CẬP NHẬT HÌNH ẢNH THEO % MÁU ───
    private void UpdateConstructionVisual()
    {
        if (BuildingSprite == null || BuildingSprite.SpriteFrames == null) return;

        // 1. Tính % hoàn thành (từ 0.0 đến 1.0)
        float progress = (float)_currentHealth / MaxHealth;
        progress = Mathf.Clamp(progress, 0f, 1f); // Ép giới hạn an toàn

        // 2. Lấy tổng số khung hình của animation hiện tại (thường là "default")
        string currentAnim = BuildingSprite.Animation;
        int totalFrames = BuildingSprite.SpriteFrames.GetFrameCount(currentAnim);

        // 3. Tính toán xem với % máu này thì tương ứng với Frame số mấy
        // Công thức: % máu * (tổng số khung hình - 1)
        int targetFrame = Mathf.FloorToInt(progress * (totalFrames - 1));

        // 4. Gán hình ảnh tương ứng
        BuildingSprite.Frame = targetFrame;
    }

    public Vector2 GetInteractionPosition()
    {
        return GlobalPosition;
    }

    public bool CanInteract()
    {
        return !_isConstructed; 
    }
}
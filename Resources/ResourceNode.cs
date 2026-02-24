using Godot;
using System;

public partial class ResourceNode : StaticBody2D
{
    // [Export] để bạn có thể chỉnh mỗi cây có bao nhiêu gỗ ngay trong Editor
    [Export] public int MaxResource = 100; 
    
    // Biến lưu trữ số gỗ hiện tại
    private int _currentResource;

    /// <summary>
    /// Cờ đánh dấu tài nguyên đã cạn kiệt.
    /// Dùng để tránh race condition khi nhiều Pawn cùng chặt:
    /// QueueFree() là deferred (cuối frame mới xóa) nên IsInstanceValid
    /// vẫn trả true trong cùng frame → cần cờ này để chặn ngay lập tức.
    /// </summary>
    public bool IsExhausted { get; private set; } = false;

    public override void _Ready()
    {
        // Khi mới sinh ra, cây sẽ đầy ắp tài nguyên
        _currentResource = MaxResource; 
    }

    // Hàm này được con Lính gọi mỗi khi nó "vung rìu"
    // Trả về số lượng tài nguyên thực tế lấy được
    // [LƯU Ý] Nếu nhiều Pawn cùng chặt 1 cây, hàm này có thể được gọi nhiều lần
    // trong cùng 1 frame. QueueFree() không xóa ngay mà đợi cuối frame,
    // nên Pawn thứ 2 vẫn có thể gọi TakeResource() → gathered = 0 (không crash).
    public int TakeResource(int amount)
    {
        // [FIX] Nếu cây đã cạn kiệt (đã gọi QueueFree ở frame trước hoặc cùng frame)
        // → trả 0 ngay lập tức, tránh double QueueFree và tính tài nguyên sai.
        if (IsExhausted) return 0;

        // Dùng Mathf.Min để lấy số lượng thực tế có thể khai thác
        int gathered = Mathf.Min(amount, _currentResource);
        _currentResource -= gathered;

        GD.Print($"[CÂY] Bị chặt! Còn lại: {_currentResource}/{MaxResource} gỗ");

        // Kiểm tra xem cây đã cạn kiệt chưa?
        if (_currentResource <= 0)
        {
            IsExhausted = true; // Đánh dấu TRƯỚC QueueFree → Pawn đọc được ngay trong cùng frame
            GD.Print("[CÂY] Đã hết tài nguyên. Cây đổ!");
            QueueFree();
        }

        return gathered;
    }
}
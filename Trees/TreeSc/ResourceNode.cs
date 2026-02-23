using Godot;
using System;

public partial class ResourceNode : StaticBody2D
{
    // [Export] để bạn có thể chỉnh mỗi cây có bao nhiêu gỗ ngay trong Editor
    [Export] public int MaxResource = 100; 
    
    // Biến lưu trữ số gỗ hiện tại
    private int _currentResource;

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
        int gathered = 0;

        if (_currentResource >= amount)
        {
            // Nếu cây còn nhiều gỗ hơn sức chặt của lính
            _currentResource -= amount;
            gathered = amount;
        }
        else
        {
            // Nếu cây sắp hết (ví dụ lính chặt 10, nhưng cây chỉ còn 3 gỗ)
            gathered = _currentResource;
            _currentResource = 0;
        }

        GD.Print($"[CÂY] Bị chặt! Còn lại: {_currentResource}/{MaxResource} gỗ");

        // Kiểm tra xem cây đã cạn kiệt chưa?
        if (_currentResource <= 0)
        {
            GD.Print("[CÂY] Đã hết tài nguyên. Cây đổ!");
            // QueueFree() là hàm quyền lực nhất Godot dùng để xóa sổ đối tượng khỏi bộ nhớ
            QueueFree(); 
        }

        return gathered;
    }
}
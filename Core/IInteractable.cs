using Godot;
using System;

public interface IInteractable
{
    // Lệnh thực hiện khi Nông dân (hoặc Lính) tương tác
    void Interact(Node2D interactor);

    // Lấy tọa độ của vật thể để Nông dân biết đường chạy tới
    Vector2 GetInteractionPosition();

    // Kiểm tra xem vật thể này còn tương tác được không (VD: Cây đã đổ chưa? Nhà đã xây xong chưa?)
    bool CanInteract();
}
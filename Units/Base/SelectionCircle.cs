using Godot;

/// <summary>
/// SelectionCircle là một Node2D nhỏ chịu trách nhiệm DUY NHẤT:
/// vẽ vòng ellipse xanh dưới chân unit khi unit đó đang được chọn.
///
/// Luồng hoạt động:
///   1. BaseUnit._Ready() tạo node này và AddChild() vào chính nó.
///   2. Khi IsSelected thay đổi, BaseUnit gọi selectionCircle.Visible = value.
///   3. Godot tự động gọi _Draw() mỗi khi node này Visible=true hoặc
///      khi QueueRedraw() được gọi.
/// </summary>
public partial class SelectionCircle : Node2D
{
    // ── Kích thước ellipse tính theo local coordinates của BaseUnit.
    //    BaseUnit có scale = 0.28, sprite frame 192×192px.
    //    → Cần dùng giá trị lớn trong local space để ellipse nhìn vừa mắt.
    private const float RadiusX = 110f;   // Bán kính ngang  (world ≈ 31px)
    private const float RadiusY  = 38f;   // Bán kính dọc    (world ≈ 11px)
    private const int   Segments = 36;    // Số đoạn thẳng ghép thành ellipse
                                          // (càng cao càng tròn, nhưng 36 là đủ)

    // ── Màu sắc vòng chọn: xanh lá bán trong suốt
    private static readonly Color RingColor = new Color(0.1f, 0.9f, 0.2f, 0.85f);

    // ── Độ dày nét vẽ (tính theo local units; world ≈ 1px sau scale 0.28)
    private const float LineWidth = 4.5f;

    // ── Offset theo trục Y để ellipse nằm ở chân unit thay vì giữa thân.
    //    y > 0 nghĩa là dịch xuống dưới trong local space.
    private const float FootOffsetY = 78f;

    /// <summary>
    /// _Draw() được Godot gọi tự động mỗi khi:
    ///   - Node này lần đầu được vẽ (Visible = true)
    ///   - QueueRedraw() được gọi từ bên ngoài
    ///   - Viewport refresh sau khi node cha di chuyển/resize
    ///
    /// Tất cả vẽ ở đây dùng local coordinates (trước khi áp scale của cha).
    /// </summary>
    public override void _Draw()
    {
        Vector2 center = new Vector2(0f, FootOffsetY);

        // Tính trước mảng điểm của ellipse để dùng DrawPolyline.
        // DrawPolyline nhanh hơn gọi DrawLine nhiều lần riêng lẻ.
        var points = new Vector2[Segments + 1];

        for (int i = 0; i <= Segments; i++)
        {
            // Chia đều góc từ 0 → 2π
            float angle = i * Mathf.Tau / Segments;

            points[i] = center + new Vector2(
                Mathf.Cos(angle) * RadiusX,
                Mathf.Sin(angle) * RadiusY
            );
        }

        // Vẽ polyline khép kín thành ellipse
        // antialiased = true → đường mượt hơn khi zoom
        DrawPolyline(points, RingColor, LineWidth, antialiased: true);
    }
}

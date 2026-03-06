using Godot;
using System;

public partial class SelectionManager : Node2D
{
    // ─────────────────────────────────────────────
    //  CLICK
    // ─────────────────────────────────────────────
    // Bán kính tìm unit gần nhất khi click đơn (world units).
    private const float SelectionRadius = 32f;

    // ─────────────────────────────────────────────
    //  DRAG-SELECT STATE
    // ─────────────────────────────────────────────
    // Ngưỡng pixel (screen space) phải di chuyển trước khi
    // coi hành động là "drag" thay vì "click".
    // Giá trị nhỏ → nhạy hơn; 6px là cảm giác tự nhiên.
    private const float DragThreshold = 6f;

    private bool    _isPressing      = false; // Chuột trái đang nhấn xuống
    private bool    _isDragging      = false; // Đã vượt ngưỡng, coi là drag

    // Vị trí SCREEN khi mới nhấn — chỉ dùng để tính ngưỡng drag.
    private Vector2 _pressScreenPos;

    // Vị trí WORLD khi bắt đầu kéo và vị trí hiện tại.
    // _Draw() và HandleDragSelect() đều dùng world coords.
    private Vector2 _dragStartWorld;
    private Vector2 _dragCurrentWorld;

    // ─────────────────────────────────────────────
    //  MÀU SẮC HỘP CHỌN
    // ─────────────────────────────────────────────
    private static readonly Color FillColor   = new Color(0.2f, 0.6f, 1.0f, 0.15f); // Nền xanh dương mờ
    private static readonly Color BorderColor = new Color(0.3f, 0.8f, 1.0f, 0.90f); // Viền xanh sáng

    // ─────────────────────────────────────────────
    //  INPUT
    // ─────────────────────────────────────────────
    /// <summary>
    /// Xử lý tất cả sự kiện chuột.
    /// Phân luồng:
    ///   LB Press      → bắt đầu theo dõi drag
    ///   Motion        → cập nhật vị trí, kích hoạt drag khi vượt ngưỡng
    ///   LB Release    → kết thúc: drag → chọn vùng; không drag → click đơn
    ///   RB Press      → di chuyển units đang được chọn
    /// </summary>
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left)
            {
                if (mouseEvent.Pressed)
                {
                    OnLeftMouseDown();
                }
                else
                {
                    OnLeftMouseUp();
                }
            }
            else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
            {
                Vector2 worldPos = GetGlobalMousePosition();

                foreach (var node in GetTree().GetNodesInGroup("units"))
                {
                    if (node is not BaseUnit baseUnit || !baseUnit.IsSelected)
                        continue;

                    baseUnit.MoveAction(worldPos);
                }
            }
        }
        else if (@event is InputEventMouseMotion && _isPressing)
        {
            OnMouseDrag();
        }
    }

    /// <summary>
    /// Khi nhấn chuột trái xuống:
    /// Lưu vị trí bắt đầu (cả screen lẫn world), reset flag drag.
    /// Chưa xác nhận là drag hay click — chờ mouse motion.
    /// </summary>
    private void OnLeftMouseDown()
    {
        _isPressing       = true;
        _isDragging       = false;
        _pressScreenPos   = GetViewport().GetMousePosition();
        _dragStartWorld   = GetGlobalMousePosition();
        _dragCurrentWorld = _dragStartWorld;
    }

    /// <summary>
    /// Khi chuột di chuyển trong khi đang nhấn:
    ///   1. Cập nhật _dragCurrentWorld (vị trí world mới nhất của chuột).
    ///   2. Nếu khoảng cách screen vượt DragThreshold → kích hoạt _isDragging.
    ///   3. Gọi QueueRedraw() để _Draw() vẽ lại hộp chọn mỗi frame.
    /// </summary>
    private void OnMouseDrag()
    {
        _dragCurrentWorld = GetGlobalMousePosition();

        if (!_isDragging)
        {
            // Đo bằng screen coords để ngưỡng không bị ảnh hưởng bởi camera zoom
            float screenDist = GetViewport().GetMousePosition()
                                            .DistanceTo(_pressScreenPos);
            if (screenDist > DragThreshold)
                _isDragging = true;
        }

        // Yêu cầu Godot gọi lại _Draw() để cập nhật hộp chọn trên màn hình
        QueueRedraw();
    }

    /// <summary>
    /// Khi thả chuột trái:
    ///   - Nếu đang drag   → chọn tất cả units trong hình chữ nhật
    ///   - Nếu click đơn   → chọn unit gần nhất
    /// </summary>
    private void OnLeftMouseUp()
    {
        Vector2 worldPos = GetGlobalMousePosition();

        if (_isDragging)
        {
            HandleDragSelect(GetDragRect());
        }
        else if (_isPressing)
        {
            HandleLeftClick(worldPos);
        }

        _isPressing = false;
        _isDragging = false;
        QueueRedraw(); // Xoá hình chữ nhật khỏi màn hình
    }

    // ─────────────────────────────────────────────
    //  SELECTION LOGIC
    // ─────────────────────────────────────────────

    /// <summary>
    /// Click đơn: DeselectAll → chọn unit gần nhất trong SelectionRadius.
    /// </summary>
    private void HandleLeftClick(Vector2 worldPos)
    {
        var allUnits = GetTree().GetNodesInGroup("units");

        BaseUnit closestUnit    = null;
        float    closestDistance = SelectionRadius;

        foreach (var node in allUnits)
        {
            if (node is not BaseUnit unit) continue;
            float distance = worldPos.DistanceTo(unit.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestUnit     = unit;
            }
        }

        DeselectAll(allUnits);
        if (closestUnit != null)
            closestUnit.IsSelected = true;
    }

    /// <summary>
    /// Drag-select: DeselectAll → chọn tất cả units trong worldRect.
    /// Dùng Rect2.HasPoint() — hoạt động đúng mọi hướng kéo nhờ GetDragRect().
    /// </summary>
    private void HandleDragSelect(Rect2 worldRect)
    {
        var allUnits = GetTree().GetNodesInGroup("units");
        DeselectAll(allUnits);

        int count = 0;
        foreach (var node in allUnits)
        {
            if (node is not BaseUnit unit) continue;
            if (worldRect.HasPoint(unit.GlobalPosition))
            {
                unit.IsSelected = true;
                count++;
            }
        }

        GD.Print($"Drag selected: {count} units");
    }

    /// <summary>
    /// Bỏ chọn tất cả units trong danh sách được truyền vào.
    /// Tách ra method riêng để tránh lặp code giữa click và drag.
    /// </summary>
    private void DeselectAll(Godot.Collections.Array<Godot.Node> allUnits)
    {
        foreach (var node in allUnits)
        {
            if (node is BaseUnit unit)
                unit.IsSelected = false;
        }
    }

    // ─────────────────────────────────────────────
    //  HELPER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Tạo Rect2 từ điểm bắt đầu và điểm hiện tại của chuột.
    /// Rect2(pos, size) yêu cầu size dương, nhưng người chơi có thể kéo
    /// theo bất kỳ hướng nào. Dùng Expand() để chuẩn hoá:
    ///   Rect2 bắt đầu từ điểm zero-size tại _dragStartWorld
    ///   → Expand(_dragCurrentWorld) → Rect2 hợp lệ bất kể hướng kéo.
    /// </summary>
    private Rect2 GetDragRect()
    {
        return new Rect2(_dragStartWorld, Vector2.Zero)
                   .Expand(_dragCurrentWorld);
    }

    // ─────────────────────────────────────────────
    //  VISUAL (vẽ hộp chọn)
    // ─────────────────────────────────────────────

    /// <summary>
    /// _Draw() được Godot tự động gọi mỗi khi QueueRedraw() được gọi.
    /// Vẽ trong LOCAL SPACE của SelectionManager (= world space vì node ở origin).
    /// Godot render pipeline áp dụng camera transform, nên hộp sẽ đúng vị trí
    /// bất kể camera di chuyển hay zoom thế nào.
    ///
    /// Chỉ vẽ khi _isDragging = true, các frame còn lại không vẽ gì (= trong suốt).
    /// </summary>
    public override void _Draw()
    {
        if (!_isDragging) return;

        Rect2 rect = GetDragRect();

        // Lớp 1: nền mờ để người chơi thấy vùng đang chọn
        DrawRect(rect, FillColor, filled: true);

        // Lớp 2: viền rõ ràng để dễ nhìn ranh giới
        DrawRect(rect, BorderColor, filled: false, width: 1.5f);
    }
}


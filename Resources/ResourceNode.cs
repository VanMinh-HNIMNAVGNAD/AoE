using Godot;
using System;

/// <summary>
/// Enum phân loại tài nguyên — dùng chung cho cả ResourceNode và hệ thống
/// kho/inventory sau này.
/// </summary>
public enum ResourceType
{
    Wood,
    Gold,
    Stone,
    Meat
}

/// <summary>
/// Base class cho mọi điểm tài nguyên trên bản đồ (cây, mỏ vàng, mỏ đá, thịt).
/// Kế thừa StaticBody2D vì resource không di chuyển nhưng cần collision
/// để unit phát hiện và tương tác.
///
/// Class con (GoldMineNode, TreeNode…) chỉ cần override để tuỳ chỉnh
/// ResourceType, animation khi cạn, v.v.
/// </summary>
public partial class ResourceNode : StaticBody2D
{
    [Export] public ResourceType Type = ResourceType.Wood;
    [Export] public int MaxAmount = 500;
    [Export] public int HarvestPerTick = 10;

    /// <summary>
    /// Sprite node — gắn trong Inspector giống BaseUnit.anima.
    /// </summary>
    [Export] public AnimatedSprite2D Anima;

    protected int _currentAmount;

    /// <summary>Lượng tài nguyên còn lại.</summary>
    public int CurrentAmount => _currentAmount;

    /// <summary>true khi đã khai thác hết.</summary>
    public bool IsDepleted => _currentAmount <= 0;

    public override void _Ready()
    {
        _currentAmount = MaxAmount;
        AddToGroup("resources");
    }

    /// <summary>
    /// Worker gọi hàm này mỗi tick gather.
    /// Trả về lượng tài nguyên thực tế thu được (có thể ít hơn
    /// HarvestPerTick nếu gần hết).
    /// </summary>
    public virtual int Harvest()
    {
        if (IsDepleted) return 0;

        int harvested = Mathf.Min(HarvestPerTick, _currentAmount);
        _currentAmount -= harvested;

        if (IsDepleted)
        {
            OnDepleted();
        }

        return harvested;
    }

    /// <summary>
    /// Gọi khi tài nguyên cạn kiệt. Class con override để thay đổi
    /// sprite, phát hiệu ứng, hoặc xoá node.
    /// </summary>
    protected virtual void OnDepleted()
    {
        GD.Print($"{Name} đã cạn tài nguyên.");
    }
}

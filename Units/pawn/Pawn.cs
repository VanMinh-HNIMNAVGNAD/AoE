using Godot;
using System;

public partial class Pawn : SelectableUnit
{
    private const int BUILD_HIT_FRAME = 2; 
    private const int CHOP_HIT_FRAME = 2;

    public override void _Ready()
    {
        base._Ready(); 
        if (AnimSprite != null) AnimSprite.FrameChanged += OnAnimationFrameChanged;
    }

    // 1. ÉP BUỘC CHẤP NHẬN GROUP ĐỂ LÍNH BIẾT ĐƯỜNG CHẠY TỚI
    public override bool CanInteractWith(Node2D target)
    {
        bool canInteract = target.IsInGroup("Resource") || target.IsInGroup("Building");
        GD.Print($"[Pawn Debug] Bấm chuột vào {target.Name}. Có thuộc Group mục tiêu không? -> {canInteract}");
        return canInteract;
    }

    protected override void PerformAction(double delta)
    {
        Velocity = Vector2.Zero; 

        if (!IsInstanceValid(CurrentTarget))
        {
            CurrentState = UnitState.Idle;
            return;
        }

        // 2. CHỖ NÀY CỰC QUAN TRỌNG: NẾU THIẾU INTERFACE, NÓ SẼ BÁO LỖI VÀ ĐỨNG IM!
        if (!(CurrentTarget is IInteractable interactable))
        {
            GD.PrintErr($"[Pawn Lỗi Nặng] Mục tiêu {CurrentTarget.Name} CHƯA ĐƯỢC GẮN IInteractable ở file script của nó! Lính không thể gõ!");
            CurrentState = UnitState.Idle;
            CurrentTarget = null;
            return;
        }

        if (!interactable.CanInteract())
        {
            GD.Print("[Pawn Debug] Mục tiêu đã cạn kiệt hoặc đã xây xong.");
            CurrentState = UnitState.Idle;
            CurrentTarget = null;
            return;
        }

        // Quay mặt
        if (CurrentTarget.GlobalPosition.X < GlobalPosition.X) AnimSprite.FlipH = true;
        else if (CurrentTarget.GlobalPosition.X > GlobalPosition.X) AnimSprite.FlipH = false;

        // Bật Animation
        if (CurrentTarget.IsInGroup("Resource")) 
        {
            if (AnimSprite.Animation != "use_axe") 
            {
                GD.Print("[Pawn Debug] VÀO KHUNG HÌNH! Bắt đầu bật animation 'use_axe'");
                AnimSprite.Play("use_axe");
            }
        }
        else if (CurrentTarget.IsInGroup("Building")) 
        {
            if (AnimSprite.Animation != "build") 
            {
                GD.Print("[Pawn Debug] VÀO KHUNG HÌNH! Bắt đầu bật animation 'build'");
                AnimSprite.Play("build");
            }
        }
    }

    private void OnAnimationFrameChanged()
    {
        if (CurrentState != UnitState.Action || !IsInstanceValid(CurrentTarget)) return;

        bool isHitFrame = false;
        if (AnimSprite.Animation == "build" && AnimSprite.Frame == BUILD_HIT_FRAME) isHitFrame = true;
        else if (AnimSprite.Animation == "use_axe" && AnimSprite.Frame == CHOP_HIT_FRAME) isHitFrame = true;

        if (isHitFrame)
        {
            GD.Print($"[Pawn Debug] BÚA CHẠM ĐẤT tại frame {AnimSprite.Frame}! Gửi lệnh trừ máu/gỗ.");
            
            if (CurrentTarget is IInteractable interactable && interactable.CanInteract())
            {
                if (CurrentTarget is ResourceNode resourceNode)
                {
                    float gatherAmount = Stats != null ? Stats.GatherAmount : 10.0f;
                    int amountGot = resourceNode.TakeResource((int)gatherAmount);
                    
                    if (GameManager.Instance != null && amountGot > 0)
                    {
                        string resourceType = "Wood"; 
                        if (CurrentTarget.IsInGroup("Gold")) resourceType = "Gold";
                        else if (CurrentTarget.IsInGroup("Food")) resourceType = "Food";
                        GameManager.Instance.AddResource(resourceType, amountGot);
                    }
                }
                else
                {
                    interactable.Interact(this);
                }
            }
            else
            {
                CurrentState = UnitState.Idle;
                CurrentTarget = null;
            }
        }
    }
}
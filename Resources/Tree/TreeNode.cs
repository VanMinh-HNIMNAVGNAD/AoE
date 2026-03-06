using Godot;

public partial class TreeNode : ResourceNode
{
    protected override void OnDepleted()
    {
        base.OnDepleted();
        // Cây hết gỗ → xoá luôn khỏi scene.
        QueueFree();
    }
}

using Godot;

public partial class MeatNode : ResourceNode
{
    protected override void OnDepleted()
    {
        base.OnDepleted();
        // Thịt hết → xoá khỏi scene.
        QueueFree();
    }
}

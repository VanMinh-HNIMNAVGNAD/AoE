using Godot;

public partial class StoneMineNode : ResourceNode
{
    protected override void OnDepleted()
    {
        base.OnDepleted();
        if (Anima != null) Anima.Visible = false;
    }
}

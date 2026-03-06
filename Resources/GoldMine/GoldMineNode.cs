using Godot;

public partial class GoldMineNode : ResourceNode
{
    protected override void OnDepleted()
    {
        base.OnDepleted();
        if (Anima != null)
        {
            Anima.Visible = false;
        }
    }
}


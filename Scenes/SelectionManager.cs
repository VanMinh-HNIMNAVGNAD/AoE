using Godot;
using System;

public partial class SelectionManager : Node2D
{
    private const float SelectionRadius = 32f;

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
        if (@event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed)
            {
                Vector2 globalPos = GetGlobalMousePosition();
                HandleLeftClick(globalPos);
            }
			else if(mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
			{
				foreach (var node in GetTree().GetNodesInGroup("units"))
				{
					if(node is BaseUnit baseUnit && baseUnit.IsSelected)
					baseUnit.MoveAction(GetGlobalMousePosition());
				}
			}
        }
    }

    private void HandleLeftClick(Vector2 worldPos)
    {
        var allUnits = GetTree().GetNodesInGroup("units");

        // Deselect all
        foreach (var node in allUnits)
        {
            if (node is BaseUnit unit)
                unit.IsSelected = false;
        }

        // Find closest unit within click radius
        BaseUnit closestUnit = null;
        float closestDistance = SelectionRadius;

        foreach (var node in allUnits)
        {
            if (node is not BaseUnit unit) continue;

            float distance = worldPos.DistanceTo(unit.GlobalPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestUnit = unit;
            }
        }

        if (closestUnit != null)
        {
            closestUnit.IsSelected = true;
            GD.Print("Selected: " + closestUnit.Name);
        }
    }
}

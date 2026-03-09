using Godot;
using System;
public enum ResourceType { Wood, Food, Gold, Stone }

public partial class ResourceNode : StaticBody2D
{
    [Export] public ResourceType Type = ResourceType.Wood;
    [Export] public int AmountLeft = 500; 

    public override void _Ready()
    {
        AddToGroup("resource_nodes");
    }

    public int Extract(int amount)
    {
        if (AmountLeft <= 0) return 0;
        
        int extracted = Mathf.Min(amount, AmountLeft);
        AmountLeft -= extracted;
        
        if (AmountLeft <= 0) 
        {
            QueueFree(); 
        }
        return extracted;
    }
}
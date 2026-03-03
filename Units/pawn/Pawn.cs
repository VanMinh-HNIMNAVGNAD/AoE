using Godot;
using System;

public partial class Pawn : BaseUnit
{
    [Export] public float GatherRate = 1.0f;
    [Export] public int CarryCapacity = 10;

    private Vector2 _moveTarget;
    private bool _hasTarget = false;
}

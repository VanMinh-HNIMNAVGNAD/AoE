    using Godot;
    using System.Collections.Generic;

    public partial class RTScontroller : Node2D
    {
        private const float ClickSelectionRadius = 12.0f;
        private const float ResourceCommandRadius = 72.0f;

        private Vector2 _dragStart;
        private bool _isDragging;
        private Rect2 _selectionRect;

        private readonly List<BaseUnit> _selectedUnits = new();

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventMouseButton mouseEvent)
            {
                if (mouseEvent.ButtonIndex == MouseButton.Left)
                {
                    if (mouseEvent.Pressed)
                    {
                        _isDragging = true;
                        _dragStart = GetGlobalMousePosition();
                        _selectionRect = new Rect2(_dragStart, Vector2.Zero);
                        QueueRedraw();
                    }
                    else
                    {
                        UpdateSelectionRect();
                        _isDragging = false;
                        SelectUnitsInBox();
                        QueueRedraw();
                    }
                }
                else if (mouseEvent.ButtonIndex == MouseButton.Right && mouseEvent.Pressed)
                {
                    Vector2 clickPos = GetGlobalMousePosition();
                    ResourceNode resourceNode = FindResourceNear(clickPos);
                    GD.Print($"[RTS] Right-click at {clickPos}, found resource: {resourceNode != null}, selected units: {_selectedUnits.Count}");

                    if (resourceNode != null)
                    {
                        foreach (BaseUnit unit in _selectedUnits)
                        {
                            if (unit is Pawn pawn)
                            {
                                GD.Print($"[RTS] Sending CommandGather to pawn");
                                pawn.CommandGather(resourceNode);
                            }
                        }
                    }
                    else
                    {
                        CommandSelectedUnits(clickPos);
                    }
                }
            }
            else if (@event is InputEventMouseMotion && _isDragging)
            {
                UpdateSelectionRect();
                QueueRedraw();
            }
        }

        public override void _Draw()
        {
            if (_isDragging)
            {
                DrawRect(_selectionRect, new Color(0, 0.5f, 1, 0.2f), filled: true);
                DrawRect(_selectionRect, new Color(0, 0.5f, 1, 0.8f), filled: false, width: 2.0f);
            }
        }

        private void UpdateSelectionRect()
        {
            Vector2 currentMousePos = GetGlobalMousePosition();
            _selectionRect = new Rect2(_dragStart, currentMousePos - _dragStart).Abs();
        }

        private void SelectUnitsInBox()
        {
            foreach (BaseUnit unit in _selectedUnits)
            {
                unit.Deselect();
            }

            _selectedUnits.Clear();

            var allUnits = GetTree().GetNodesInGroup("units");

            foreach (Node node in allUnits)
            {
                if (node is BaseUnit unit)
                {
                    if (IsUnitInsideSelection(unit))
                    {
                        _selectedUnits.Add(unit);
                        unit.Select();
                    }
                }
            }
        }

        private bool IsUnitInsideSelection(BaseUnit unit)
        {
            if (_selectionRect.Size.LengthSquared() <= ClickSelectionRadius * ClickSelectionRadius)
            {
                return unit.GlobalPosition.DistanceSquaredTo(_dragStart) <= ClickSelectionRadius * ClickSelectionRadius;
            }

            return _selectionRect.HasPoint(unit.GlobalPosition);
        }

        private ResourceNode FindResourceNear(Vector2 clickPosition)
        {
            ResourceNode nearestResource = null;
            float nearestDistanceSquared = ResourceCommandRadius * ResourceCommandRadius;

            foreach (Node node in GetTree().GetNodesInGroup("resource_nodes"))
            {
                if (node is not ResourceNode resourceNode || !IsInstanceValid(resourceNode))
                {
                    continue;
                }

                float distanceSquared = resourceNode.GlobalPosition.DistanceSquaredTo(clickPosition);
                if (distanceSquared > nearestDistanceSquared)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestResource = resourceNode;
            }

            return nearestResource;
        }

        private void CommandSelectedUnits(Vector2 targetPosition)
        {
            foreach (BaseUnit unit in _selectedUnits)
            {
                unit.MoveTo(targetPosition);
            }
        }
    }
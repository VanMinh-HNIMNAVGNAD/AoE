using Godot;

public partial class HoverComponent : Area2D
{
	[Export] public Vector2 BoxPadding = new(10.0f, 10.0f);

	private NinePatchRect _selectionBox = null!;
	private CollisionShape2D _myCollision = null!;

	public override void _Ready()
	{
		_selectionBox = GetNode<NinePatchRect>("NinePatchRect");
		_myCollision = GetNode<CollisionShape2D>("CollisionShape2D");

		InputPickable = true;
		MouseEntered += OnMouseEntered;
		MouseExited += OnMouseExited;

		CallDeferred(nameof(AutoScaleBox));
	}

	public void AutoScaleBox()
	{
		if (_myCollision.Shape == null)
		{
			return;
		}

		if (_myCollision.Shape is RectangleShape2D rectShape)
		{
			_selectionBox.Size = rectShape.Size + BoxPadding;
		}
		else if (_myCollision.Shape is CircleShape2D circleShape)
		{
			float diameter = circleShape.Radius * 2.0f;
			_selectionBox.Size = new Vector2(diameter, diameter) + BoxPadding;
		}
		else if (_myCollision.Shape is CapsuleShape2D capsuleShape)
		{
			float width = capsuleShape.Radius * 2.0f;
			_selectionBox.Size = new Vector2(width, capsuleShape.Height) + BoxPadding;
		}

		_selectionBox.Position = -(_selectionBox.Size / 2.0f);
	}

	private void OnMouseEntered()
	{
		_selectionBox.Visible = true;
	}

	private void OnMouseExited()
	{
		_selectionBox.Visible = false;
	}
}
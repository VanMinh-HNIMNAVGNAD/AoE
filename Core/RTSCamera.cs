using Godot;
using System;

public partial class RTSCamera : Camera2D
{

	[Export] public float Speed = 300.0f;
	[Export] public float ZoomSpeed = 0.1f;
	[Export] public float MinZoom = 0.5f;
	[Export] public float MaxZoom = 3.0f;
	[Export] public int Margin = 20;

	private Vector2 zoomTarget;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		zoomTarget = Zoom;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		MoveCamera((float)delta);
		ZoomCamera((float)delta);
	}

	private void MoveCamera(float delta)
	{
		Vector2 _inputvector = Vector2.Zero;
		Viewport _viewport = GetViewport();
		Vector2 _mousePosition = _viewport.GetMousePosition();
		Vector2 _screensize = _viewport.GetVisibleRect().Size;

		if (Input.IsActionPressed("ui_right")) _inputvector.X += 1;
		if (Input.IsActionPressed("ui_left")) _inputvector.X -= 1;
		if (Input.IsActionPressed("ui_down")) _inputvector.Y += 1;
		if (Input.IsActionPressed("ui_up")) _inputvector.Y -= 1;

		if (GetWindow().HasFocus())
		{
			if (_mousePosition.X > _screensize.X - Margin) _inputvector.X += 1;
			if (_mousePosition.X < Margin) _inputvector.X -= 1;
			if (_mousePosition.Y > _screensize.Y - Margin) _inputvector.Y += 1;
			if (_mousePosition.Y < Margin) _inputvector.Y -= 1;
		}
		_inputvector = _inputvector.Normalized();
		GlobalPosition += _inputvector * Speed * delta;
	}

    public override void _UnhandledInput(InputEvent @event)
    {
        base._UnhandledInput(@event);
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
			{
				zoomTarget += new Vector2(ZoomSpeed, ZoomSpeed);
			}else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
			{
				zoomTarget -= new Vector2(ZoomSpeed, ZoomSpeed);
			}
			zoomTarget.X = Mathf.Clamp(zoomTarget.X, MinZoom, MaxZoom);
			zoomTarget.Y = Mathf.Clamp(zoomTarget.Y, MinZoom, MaxZoom);
		}
    }
	private void ZoomCamera(float delta)
	{
		Zoom = Zoom.Lerp(zoomTarget, 10f * delta);
	}
}

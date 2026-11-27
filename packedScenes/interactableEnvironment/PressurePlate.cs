using Godot;
using System;

public partial class PressurePlate : StaticBody2D
{
	[Signal]	
	public delegate void PlateActivatedEventHandler();

	private Area2D _area;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Ready()
	{
		_area = GetNode<Area2D>("Area2D");

		_area.BodyEntered += OnBodyEntered;
	}
	private void OnBodyEntered(Node2D body)
	{
		if(body.IsInGroup("player"))
		{
			GD.Print("Pressure plate activated!");

			EmitSignal(SignalName.PlateActivated);
		}
	}
}

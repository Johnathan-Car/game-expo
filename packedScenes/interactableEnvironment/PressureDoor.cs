using Godot;
using System;

public partial class PressureDoor : StaticBody2D
{
	[Export] public PressurePlate _pressurePlate;
	private CollisionShape2D _collision;
	public override void _Ready()
	{
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");
		_pressurePlate.PlateActivated += OpenDoor;

		_collision.SetDeferred("disabled", true);
	}
	public void OpenDoor()
	{
		GD.Print("Door Opening!");
		_collision.SetDeferred("disabled", false);
	}
}

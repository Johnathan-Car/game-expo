using Godot;
using System;

public partial class PlatformMoving : AnimatableBody2D
{
	[Export]
	public float Speed = 100.0f; // Speed adjustable in Inspector
	
	[Export]
	public float MoveTime = 3.0f; // Speed adjustable in Inspector

	[Export]
	public bool movingUp = true;

	[Export]
	public bool movingRight = false;

	private Vector2 direction = Vector2.Up;
	private float timer = 0.0f; // Setting up timer

	public override void _PhysicsProcess(double delta)
	{
		timer += (float)delta; // Every physics update, the timer increases

		if(movingUp == false)
			{
				if(movingRight)
				{
					direction = Vector2.Right;
				}
				else
				{
					direction = Vector2.Left;
				}
			
			}

		Position += direction * Speed * (float)delta;
		// Moves at 100 units per second making note of the time passing

		if(timer >= MoveTime) // If the timer has been moving for at least 3 seconds
		{
			if(movingUp == false)
			{
				movingRight = !movingRight;
			}
			
			else
			{
				direction = -direction;
			}
			timer = 0.0f;
		}
	}
}

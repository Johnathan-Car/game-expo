using Godot;
using System;

public partial class DeathFadeOut : CanvasLayer
{
	private ColorRect _fade;
	public override void _Ready()
	{
		_fade = GetNode<ColorRect>("ColorRect");
	}

	public async void FadeToBlack()
	{
		// Allows the fade to be visible
		_fade.Visible = true;

		// Slowly changes the transparency from 0 to 1.
		var tween = CreateTween();

		tween.TweenProperty(_fade, "color:a", 1.0f, 1.3f);

		// Wait?
		await ToSignal(tween, "finished");

		// The Restart
		GetTree().ReloadCurrentScene();
	}
}

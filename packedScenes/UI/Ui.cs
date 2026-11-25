using Godot;
using System;

public partial class Ui : Panel
{
	[Export] public TextureRect MyTextureRect;
	[Export] public TextureRect MyTextureRect2;

	
	public void ChangeImage(string imagePath)
	{
		MyTextureRect.Texture = GD.Load<Texture2D>(imagePath);
	}
	public void ChangeImage2(string imagePath)
	{
		MyTextureRect2.Texture = GD.Load<Texture2D>(imagePath);
	}
}


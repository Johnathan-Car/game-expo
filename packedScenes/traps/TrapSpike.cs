using Godot;
using System;

public partial class TrapSpike : StaticBody2D
{
	private Timer _timer;
    private AnimatedSprite2D _animation;
	private bool isTrapActive = false;
	private bool _isSwitching = false;
	private CollisionShape2D _collision;

	public override void _Ready()
	{
		_animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_timer = GetNode<Timer>("Timer");
		_collision = GetNode<CollisionShape2D>("CollisionShape2D");


		_timer.WaitTime = 3.0;
		_timer.Timeout += OnTimerTimeout;
	}

	private void PlayAnimation(string animationName)
    {
        if (_animation.Animation != animationName)
        {
            _animation.Play(animationName);
        }
    }

	public async void OnTimerTimeout()
	{
		GD.Print("Time activates");
		if(_isSwitching)
		{
			return;
		}

		_isSwitching = true;

		if(isTrapActive == false)
		{
			PlayAnimation("transition_off");
			await ToSignal(_animation, AnimatedSprite2D.SignalName.AnimationFinished);

			PlayAnimation("off");
			_collision.SetDeferred("disabled", true);
			await ToSignal(_animation, AnimatedSprite2D.SignalName.AnimationFinished);

			isTrapActive = true;
		}
		else
		{
			PlayAnimation("transition_on");
			await ToSignal(_animation, AnimatedSprite2D.SignalName.AnimationFinished);

			PlayAnimation("on");
			_collision.SetDeferred("disabled", false);
			await ToSignal(_animation, AnimatedSprite2D.SignalName.AnimationFinished);

			isTrapActive = false;
		}

		_isSwitching = false;
	}

	private async void Switch()
    {
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

    }
}

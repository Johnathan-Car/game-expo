
using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    public const float JumpVelocity = -400.0f;
    public const float Acceleration = 8.0f;
    public const float Deceleration = 12.0f;
    public Vector2 respawnPoint; 

    private AnimatedSprite2D _animation;

    private bool _isHidden = false;

    public override void _Ready()
    {
        _animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _animation.Play("idle");

        respawnPoint = this.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {

        Vector2 velocity = Velocity;

        // Hide when  H is held
        _isHidden = Input.IsActionPressed("hide");

        // Gravity 
        if (!IsOnFloor())
        {
            velocity += GetGravity() * (float)delta;
        }

        // Get movement direction
        Vector2 direction = Input.GetVector(
            "left", "right",
            "up", "down"
        );

        // Jump (disabled while hiding)
        if (!_isHidden &&
            Input.IsActionJustPressed("ui_accept") &&
            IsOnFloor())
        {
            velocity.Y = JumpVelocity;
        }

        // Movement
        if (_isHidden)
        {
            // Stop horizontal movement immediately
            velocity.X = 0;
        }
        else
        {
            float targetSpeed = direction.X * Speed;

            // Acceleration and deceleration
            float lerpSpeed = direction.X != 0
                ? Acceleration
                : Deceleration;

            velocity.X = Mathf.Lerp(
                velocity.X,
                targetSpeed,
                lerpSpeed * (float)delta
            );
        }

        // Apply velocity
        Velocity = velocity;
        MoveAndSlide();

        // Flip character
        if (!_isHidden && direction.X != 0)
        {
            _animation.FlipH = direction.X < 0;
        }

        // Animation selection
        if (_isHidden)
        {
            PlayAnimation("hidden");
        }
        else if (Mathf.Abs(Velocity.X) > 10)
        {
            PlayAnimation("drive");
        }
        else
        {
            PlayAnimation("idle");
        }
    }

    private void PlayAnimation(string animationName)
    {
        if (_animation.Animation != animationName)
        {
            _animation.Play(animationName);
        }
    }

    public void HideCharacter()
    {
        _isHidden = true;
        Velocity = new Vector2(0, Velocity.Y);
        PlayAnimation("hidden");
    }

    public void UnhideCharacter()
    {
        _isHidden = false;
        PlayAnimation("idle");
    }
    public bool hasKey() {
        //retturn key status
        GD.Print("player keycheck not done, right now just returns true");
        return true;
        }

    public void respawn()
    {
        this.GlobalPosition = respawnPoint;
    }
    public bool isHidden() { return _isHidden; }    
}


using Godot;
using System;

public partial class Player : CharacterBody2D
{
    [Export] public float Speed = 300.0f;
    [Export] public PackedScene SavePointScene;
    [Export] public Ui UI;
    [Export] public Marker2D SpawnPoint;
    public const float JumpVelocity = -400.0f;
    public const float Acceleration = 8.0f;
    public const float Deceleration = 12.0f;
    public Vector2 respawnPoint;
    private bool _hasSaved = false;
    private bool _playerHit = false;
    private Polygon2D _savePointDot;

    // Death float mode
    private bool _deathFloatMode = false;
    private float _deathFloatTimer = 0f;
    [Export] public float DeathFloatDuration = 4.0f;
    [Export] public float DeathFloatSpeed = 250.0f;
    [Export] public float DeathFloatAcceleration = 3.0f;
    [Export] public float DeathFloatFriction = 0.5f;



    private AnimatedSprite2D _animation;

    private bool _isHidden = false;

    public override void _Ready()
    {
        _animation = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _animation.Play("idle");
        GlobalPosition = SpawnPoint.GlobalPosition;
        respawnPoint = this.GlobalPosition;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Input.IsActionJustPressed("DEATH"))
        {
            respawn();

            _deathFloatMode = true;
            _deathFloatTimer = DeathFloatDuration;
        }

        if (!_hasSaved && Input.IsActionJustPressed("save"))
        {
            if (SavePointScene != null)
            {
                respawnPoint = GlobalPosition;
                CreateSavePointDot();
                _hasSaved = true;
            }
            else
            {
                GD.PushWarning("Assign your SavePoint scene in the Inspector first!");
            }
        }

        Vector2 velocity = Velocity;

        // Hide when  H is held
        _isHidden = Input.IsActionPressed("hide");

        // Get movement direction
        Vector2 direction = Input.GetVector(
            "left", "right",
            "up", "down"
        );

        // Death float timer
        if (_deathFloatMode)
        {
            _deathFloatTimer -= (float)delta;

            if (_deathFloatTimer <= 0f)
            {
                _deathFloatMode = false;
            }
        }

        if (_deathFloatMode)
        {
            // Free movement with momentum and drifting
            if (direction != Vector2.Zero)
            {
                Vector2 targetVelocity = direction * DeathFloatSpeed;

                velocity = velocity.Lerp(
                    targetVelocity,
                    DeathFloatAcceleration * (float)delta
                );
            }
            else
            {
                // Slowly lose momentum when no input is held
                velocity = velocity.Lerp(
                    Vector2.Zero,
                    DeathFloatFriction * (float)delta
                );
            }
        }
        else
        {
            // Normal gravity
            if (!IsOnFloor())
            {
                velocity += GetGravity() * (float)delta;
            }

            // Jump (disabled while hiding)
            if (!_isHidden &&
                Input.IsActionJustPressed("ui_accept") &&
                IsOnFloor())
            {
                velocity.Y = JumpVelocity;
            }

            // Normal horizontal movement
            if (_isHidden)
            {
                velocity.X = 0;
            }
            else
            {
                float targetSpeed = direction.X * Speed;

                float lerpSpeed = direction.X != 0
                    ? Acceleration
                    : Deceleration;

                velocity.X = Mathf.Lerp(
                    velocity.X,
                    targetSpeed,
                    lerpSpeed * (float)delta
                );
            }
        }

        // Apply velocity
        Velocity = velocity;
        MoveAndSlide();

          // Checking Collision for collectables / weak platform
        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            KinematicCollision2D collision = GetSlideCollision(i);

            Node collider = collision.GetCollider() as Node;

            // Weak Platform
            if (collider.IsInGroup("WEAK"))
            {
                GD.Print("The player touched a Weak Platform!");

                // Delete the Weak Platform
				DeleteWeakPlatformDelay(collider);
            }

            // Traps
            if(collider.IsInGroup("TRAP"))
            {
                GD.Print("The player touched a Trap!");
            }

            // Collectables
            if (collider.IsInGroup("COLLECTABLE"))
            {
                GD.Print("The player touched a Collectable!");
                if(collider.IsInGroup("KEYCARD"))
                {
                    GD.Print("The player touched a Keycard!");   
                    UI.ChangeImage("res://GameExpo Files/Assets/Items/key_card.png");               
                }
                else if(collider.IsInGroup("STAR"))
                {
                    GD.Print("The player touched a Star!");
                    UI.ChangeImage2("res://GameExpo Files/Assets/Items/Star.png");           
                }
                // Delete the Collectable
				collider.QueueFree();
                
            }
        }
          //

        // Flip character
        if (!_isHidden && direction.X != 0)
        {
            _animation.FlipH = direction.X < 0;
        }

        // Animation selection
        if (_playerHit == true)
        {
            PlayAnimation("death");
            _playerHit = false;
        }
        else if (_deathFloatMode)
        {
            PlayAnimation("idle"); // Replace with "float" if you create that animation
        }
        else if (_isHidden)
        {
            PlayAnimation("hidden");
        }
        else if (Mathf.Abs(Velocity.X) > 10 && IsOnFloor())
        {
            PlayAnimation("drive");
        }
        else
        {
            PlayAnimation("idle");
        }
    }

    // Specifically only for Weak Platform Delayed Deletion
    private async void DeleteWeakPlatformDelay(Node collider)
    {
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

        if (IsInstanceValid(collider))
        {
            collider.QueueFree();
        }
    }

    // Specifically only for Respawning at start location
    private async void Death(Node collider)
    {
        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);

        if (IsInstanceValid(collider))
        {
            collider.QueueFree();
        }
    }
    public void kill()
    {
        respawn();

        _deathFloatMode = true;
        _deathFloatTimer = DeathFloatDuration;
    }
    private void CreateSavePointDot()
    {
        if (SavePointScene == null)
        {
            GD.PushWarning("SavePointScene has not been assigned in the Inspector!");
            return;
        }

        Node2D savePointInstance = SavePointScene.Instantiate<Node2D>();

        GetTree().CurrentScene.AddChild(savePointInstance);

        savePointInstance.GlobalPosition = respawnPoint;
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
        GlobalPosition = respawnPoint;
        Velocity = Vector2.Zero;
    }
    public bool isHidden() { return _isHidden; }    
}


using Godot;

public partial class FlyerDeprecated : Node2D
{
    [Export] public float WanderRadius = 150f;
    [Export] public float WanderSpeed = 65f;
    [Export] public float ChaseSpeed = 140f;
    [Export] public float WanderPause = 1.0f;
    [Export] public float ExplosionDelay = 4.0f;

    private AnimatedSprite2D sprite;
    private Area2D detection;
    private Area2D explodeRadius;

    private Vector2 spawnPosition;
    private Vector2 targetPosition;
    private Vector2 wanderTarget;

    private float wanderTimer;
    private float explosionTimer;

    private bool exploding = false;
    private bool detonated = false;
    private bool explodeNOW = false;
    private bool finalFramesStarted = false;

    private RandomNumberGenerator rng = new();

    public override void _Ready()
    {
        rng.Randomize();

        spawnPosition = GlobalPosition;
        targetPosition = GlobalPosition;

        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        detection = GetNode<Area2D>("detection");
        explodeRadius = GetNode<Area2D>("explode_radius");

        sprite.AnimationFinished += OnAnimationFinished;

        sprite.Play("idle");
        PickWanderTarget();
    }

    public override void _PhysicsProcess(double delta)
    {
        FindPlayer();

        if (!exploding)
        {
            IdleMovement((float)delta);
        }
        else
        {
            ExplodeMovement((float)delta);

            // Immediately detonate if the player enters explode_radius
            if (PlayerInExplodeRadius())
            {
                if (!finalFramesStarted)
                {
                    finalFramesStarted = true;
                    SkipToLast4Frames();
                }

                return; // Wait AnimationFinished
            }

            ExplosionCountdown((float)delta);
        }
    }



    private void FindPlayer()
    {
        foreach (Node2D body in detection.GetOverlappingBodies())
        {
            if (!body.IsInGroup("player"))
                continue;

            targetPosition = body.GlobalPosition;

            // Start the explosion sequence
            if (!exploding)
                StartExplosion();

            return;
        }
        // targetPosition remains at the last known position.
    }

    private void StartExplosion()
    {
        exploding = true;
        explosionTimer = ExplosionDelay;

        sprite.Play("explode");
    }

    private void IdleMovement(float delta)
    {
        wanderTimer -= delta;

        if (wanderTimer <= 0f ||
            GlobalPosition.DistanceTo(wanderTarget) < 8f)
        {
            PickWanderTarget();
        }

        GlobalPosition = GlobalPosition.MoveToward(
            wanderTarget,
            WanderSpeed * delta
        );
    }

    private void PickWanderTarget()
    {
        wanderTimer = WanderPause + rng.Randf();

        Vector2 randomOffset = new Vector2(
            rng.RandfRange(-WanderRadius, WanderRadius),
            rng.RandfRange(-WanderRadius, WanderRadius)
        );

        wanderTarget = spawnPosition + randomOffset;
    }

    private void ExplodeMovement(float delta)
    {
        GlobalPosition = GlobalPosition.MoveToward(
            targetPosition,
            ChaseSpeed * delta
        );
    }

    private void ExplosionCountdown(float delta)
    {
        explosionTimer -= delta;

        if (explosionTimer <= 0f && !detonated)
        {
            Detonate();
        }
    }

    private void Detonate()
    {
        detonated = true;

        foreach (Node2D body in explodeRadius.GetOverlappingBodies())
        {
            if (!body.IsInGroup("player"))
                continue;

            // Calls respawn() on the player.
            body.Call("respawn");
        }

        QueueFree();
    }

    private bool PlayerInExplodeRadius()
    {
        foreach (Node2D body in explodeRadius.GetOverlappingBodies())
        {
            if (body.IsInGroup("player"))
                return true;
        }

        return false;
    }

    private void SkipToLast4Frames()
    {
        int totalFrames = sprite.SpriteFrames.GetFrameCount("explode");

        sprite.Frame = Mathf.Max(0, totalFrames - 4);
        sprite.FrameProgress = 0f;
        sprite.Play();
    }

    private void OnAnimationFinished()
    {
        GD.Print("Animation done");

        if (sprite.Animation == "explode" && finalFramesStarted)
        {
            Detonate();
        }
    }
}

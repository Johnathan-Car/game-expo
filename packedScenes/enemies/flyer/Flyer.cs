using Godot;

public partial class Flyer : Node2D
{
    [Export] public float WanderRadius = 150f;
    [Export] public float WanderSpeed = 65f;
    [Export] public float ChaseSpeed = 140f;
    [Export] public float WanderPause = 1.0f;
    [Export] public float ExplosionDelay = 4.0f;

    //used to handle states
    private enum FlyerState
    {
        Wander,
        Chase,
        Exploding
    }

    private FlyerState state = FlyerState.Wander;

    private AnimatedSprite2D sprite;
    private Area2D detection;
    private Area2D explodeRadius;

    private Vector2 spawnPosition;
    private Vector2 targetPosition;
    private Vector2 wanderTarget;

    private float wanderTimer;
    private float explosionTimer;

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
        float dt = (float)delta;

        switch (state)
        {
            case FlyerState.Wander:
                FindPlayer();

                if (state == FlyerState.Wander)
                    IdleMovement(dt);

                break;

            case FlyerState.Chase:
                FindPlayer();

                if (PlayerInExplodeRadius())
                {
                    BeginExplosion();
                    return;
                }

                ExplodeMovement(dt);

                explosionTimer -= dt;

                if (explosionTimer <= 0f)
                {
                    BeginExplosion();
                }

                break;

            case FlyerState.Exploding:
                // Stops all movement while exploding.
                break;
        }
    }

    private void FindPlayer()
    {
        foreach (Node2D body in detection.GetOverlappingBodies())
        {
            if (!body.IsInGroup("player"))
                continue;

            targetPosition = body.GlobalPosition;

            if (state == FlyerState.Wander)
            {
                StartChase();
            }

            return;
        }
    }

    private void StartChase()
    {
        state = FlyerState.Chase;
        explosionTimer = ExplosionDelay;

        sprite.Play("chase");
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

    private bool PlayerInExplodeRadius()
    {
        foreach (Node2D body in explodeRadius.GetOverlappingBodies())
        {
            if (body.IsInGroup("player"))
                return true;
        }

        return false;
    }

    private void BeginExplosion()
    {
        if (state == FlyerState.Exploding)
            return;

        // Lock the state so the Flyer cannot move anymore.
        state = FlyerState.Exploding;

        // Damage/kill the player if they are within the radiu
        KillPlayersInRadius();

        // Play the explosion animation.
        sprite.Play("explode");
    }

    private void KillPlayersInRadius()
    {
        foreach (Node2D body in explodeRadius.GetOverlappingBodies())
        {
            if (body is Player player)
            {
                player.kill();
            }
        }
    }

    private void OnAnimationFinished()
    {
        if (state == FlyerState.Exploding &&
            sprite.Animation == "explode")
        {
            QueueFree();
        }
    }
}

using Godot;

public partial class Walker : CharacterBody2D
{
    [Export] public float PatrolSpeed = 45f;
    [Export] public float ChaseSpeed = 90f;
    [Export] public float TurnPause = 1f;
    [Export] public float DetectionRange = 180f;

    private enum WalkerState
    {
        Patrol,
        Chase
    }

    private WalkerState state = WalkerState.Patrol;

    private AnimatedSprite2D sprite;
    private RayCast2D floorChecker;
    private RayCast2D detection;
    private Area2D contactArea;


    private Player player;

    private float pointAX;
    private float pointBX;
    private float targetX;
    private float playerX;

    private int patrolDirection = 1;
    private float turnTimer = 0f;

    public override void _Ready()
    {
        player = GetTree().GetFirstNodeInGroup("player") as Player;
        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        floorChecker = GetNode<RayCast2D>("FloorChecker");
        detection = GetNode<RayCast2D>("Detection");
        contactArea = GetNode<Area2D>("ContactArea");

        floorChecker.Enabled = true;
        detection.Enabled = true;

        UpdatePatrolPoints();

        // Start by walking toward point B.
        patrolDirection = 1;
        targetX = pointBX;

        sprite.Play("walk");
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        CheckPlayerContact();
        DetectPlayer();

        // Apply gravity.
        if (!IsOnFloor())
        {
            Velocity = new Vector2(
                Velocity.X,
                Velocity.Y + GetGravity().Y * dt
            );
        }
        else if (Velocity.Y > 0f)
        {
            Velocity = new Vector2(Velocity.X, 0f);
        }

        // Pause briefly when turning around.
        if (turnTimer > 0f)
        {
            turnTimer -= dt;
            Velocity = new Vector2(0f, Velocity.Y);
            sprite.Play("idle");

            MoveAndSlide();
            return;
        }

        if (state == WalkerState.Patrol)
        {
            Patrol();
        }
        else if (state == WalkerState.Chase)
        {
            ChasePlayer();
        }

        MoveAndSlide();
    }

    private void UpdatePatrolPoints()
    {
        pointAX = GetNode<Marker2D>("PatrolPointA").GlobalPosition.X;
        pointBX = GetNode<Marker2D>("PatrolPointB").GlobalPosition.X;
    }

    private void DetectPlayer()
    {
        if (player == null)
            return;

        // Always check hiding, even while already chasing.
        if (player.isHidden())
        {
            if (state == WalkerState.Chase)
                StopChasing();

            return;
        }

        detection.ForceRaycastUpdate();

        // Chase only when the forward ray hits the player.
        if (detection.IsColliding() &&
            detection.GetCollider() == player)
        {
            playerX = player.GlobalPosition.X;
            state = WalkerState.Chase;
            return;
        }

        // Player is no longer detected.
        if (state == WalkerState.Chase)
            StopChasing();
    }

    private void StopChasing()
    {
        state = WalkerState.Patrol;

        if (Mathf.Abs(GlobalPosition.X - pointAX) <
            Mathf.Abs(GlobalPosition.X - pointBX))
        {
            patrolDirection = -1;
        }
        else
        {
            patrolDirection = 1;
        }

        targetX = patrolDirection == 1 ? pointBX : pointAX;

        Velocity = new Vector2(0f, Velocity.Y);
    }

    private void Patrol()
    {
        float distance = Mathf.Abs(GlobalPosition.X - targetX);

        // Reached the current patrol point.
        if (distance < 4f)
        {
            TurnAround();
            return;
        }

        float direction = Mathf.Sign(
            targetX - GlobalPosition.X
        );

        // Turn around if there is no floor ahead.
        if (!HasFloorAhead(direction))
        {
            TurnAround();
            return;
        }

        Walk(direction, PatrolSpeed);
    }

    private void TurnAround()
    {
        patrolDirection *= -1;

        targetX = patrolDirection == 1
            ? pointBX
            : pointAX;

        Velocity = new Vector2(0f, Velocity.Y);

        turnTimer = TurnPause;

        sprite.Play("idle");
        SetFacingDirection(patrolDirection);
    }

    private void ChasePlayer()
    {
        float difference = playerX - GlobalPosition.X;

        if (Mathf.Abs(difference) < 4f)
        {
            Velocity = new Vector2(0f, Velocity.Y);
            sprite.Play("idle");
            return;
        }

        float direction = Mathf.Sign(difference);

        if (!HasFloorAhead(direction))
        {
            Velocity = new Vector2(0f, Velocity.Y);
            sprite.Play("idle");
            return;
        }

        Walk(direction, ChaseSpeed);
    }

    private void Walk(float direction, float speed)
    {
        Velocity = new Vector2(
            direction * speed,
            Velocity.Y
        );

        sprite.FlipH = direction < 0;

        // Point detection in the direction the Walker is facing _OTHERWISE it can only detect right (I SPENT TOO MUCH TIME ON THIS)
        SetFacingDirection(direction);


        sprite.Play("walk");
    }

    private void SetFacingDirection(float direction)
    {
        sprite.FlipH = direction < 0;

        Vector2 rayPosition = detection.TargetPosition;
        rayPosition.X = Mathf.Abs(rayPosition.X) * direction;
        detection.TargetPosition = rayPosition;
    }

    private bool HasFloorAhead(float direction)
    {
        floorChecker.TargetPosition = new Vector2(
            direction * 12f,
            28f
        );

        floorChecker.ForceRaycastUpdate();

        return floorChecker.IsColliding();
    }

    private void CheckPlayerContact()
    {
        foreach (Node2D body in contactArea.GetOverlappingBodies())
        {
            if (body is Player player &&
                body.IsInGroup("player"))
            {
                player.kill();
                return;
            }
        }
    }
}

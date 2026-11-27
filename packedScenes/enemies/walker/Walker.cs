
using Godot;

public partial class Walker : CharacterBody2D
{
    private enum WalkerState
    {
        Patrol,
        Chase
    }

    private WalkerState state = WalkerState.Patrol;

    [Export] public float PatrolSpeed = 45.0f;
    [Export] public float ChaseSpeed = 90.0f;
    [Export] public float ArrivalDistance = 3.0f;
    [Export] public float ChaseCooldown = 1.0f;

    private float chaseCooldownTimer = 0.0f;
    private float lastKnownPlayerX;

    private int facingDirection = 1;

    private Player player;
    private AnimatedSprite2D sprite;
    private RayCast2D floorChecker;
    private RayCast2D detection;
    private Area2D contactArea;

    private Vector2 patrolPositionA;
    private Vector2 patrolPositionB;
    private Vector2 currentPatrolTarget;

    public override void _Ready()
    {
        base._Ready();

        player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (player != null)
            AddCollisionExceptionWith(player);

        

        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        floorChecker = GetNode<RayCast2D>("FloorChecker");
        detection = GetNode<RayCast2D>("Detection");

        // Ignore other enemies
        foreach (Node enemy in GetTree().GetNodesInGroup("enemy"))
        {
            if (enemy is PhysicsBody2D body && body != this)
            {
                AddCollisionExceptionWith(body);
                detection.AddException(body);
            }
        }

        patrolPositionA = GetNode<Marker2D>("PatrolPointA").GlobalPosition;
        patrolPositionB = GetNode<Marker2D>("PatrolPointB").GlobalPosition;

        currentPatrolTarget = patrolPositionA;

        contactArea = GetNode<Area2D>("ContactArea");
        contactArea.BodyEntered += CollisionHandler;
    }

    private void CollisionHandler(Node2D other)
    {
        if (other is not Player body)
            return;

        if (body.isHidden() && state == WalkerState.Patrol)
            return;

        body.killCheck();

        state = WalkerState.Patrol;
        chaseCooldownTimer = ChaseCooldown;

        // Turn away from the player
        currentPatrolTarget = facingDirection > 0
            ? patrolPositionA
            : patrolPositionB;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!IsOnFloor())
            Velocity += GetGravity() * (float)delta;

        // Update chase cooldown
        if (chaseCooldownTimer > 0)
            chaseCooldownTimer = Mathf.Max(0, chaseCooldownTimer - (float)delta);

        bool playerDetected = chaseCooldownTimer <= 0 && DetectPlayer();

        if (playerDetected)
            state = WalkerState.Chase;

        Vector2 targetPosition;
        float movementSpeed;

        if (state == WalkerState.Chase)
        {
            targetPosition = playerDetected
                ? player.GlobalPosition
                : new Vector2(lastKnownPlayerX, GlobalPosition.Y);

            movementSpeed = ChaseSpeed;

            // Return to patrol after reaching the last known player position
            if (!playerDetected &&
                Mathf.Abs(GlobalPosition.X - lastKnownPlayerX) <= ArrivalDistance)
            {
                ReturnToPatrol();
                targetPosition = currentPatrolTarget;
                movementSpeed = PatrolSpeed;
            }
        }
        else
        {
            targetPosition = currentPatrolTarget;
            movementSpeed = PatrolSpeed;

            // Switch patrol points when reaching the current target
            if (Mathf.Abs(targetPosition.X - GlobalPosition.X) <= ArrivalDistance)
            {
                currentPatrolTarget = currentPatrolTarget == patrolPositionA
                    ? patrolPositionB
                    : patrolPositionA;

                targetPosition = currentPatrolTarget;
            }
        }

        float direction = Mathf.Sign(targetPosition.X - GlobalPosition.X);

        // Check for a cliff before moving
        if (direction != 0)
        {
            SetFacingDirection((int)direction);
            floorChecker.ForceRaycastUpdate();

            if (!floorChecker.IsColliding())
            {
                Velocity = new Vector2(0, Velocity.Y);

                state = WalkerState.Patrol;

                // Turn away from the cliff
                if (currentPatrolTarget == patrolPositionA)
                {
                    currentPatrolTarget = patrolPositionB;
                } else
                {
                    currentPatrolTarget = patrolPositionA;
                }

                PlayAnimation("idle");
                MoveAndSlide();
                return;
            }
        }

        Velocity = new Vector2(direction * movementSpeed, Velocity.Y);

        PlayAnimation(direction == 0 ? "idle" : "walk");

        MoveAndSlide();
    }

    private bool DetectPlayer()
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return false;

        if (player.isHidden())
            return false;

        detection.ForceRaycastUpdate();

        if (detection.GetCollider() is Player detectedPlayer)
        {
            lastKnownPlayerX = detectedPlayer.GlobalPosition.X;
            return true;
        }

        return false;
    }

    private void SetFacingDirection(int direction)
    {
        if (direction == facingDirection)
            return;

        facingDirection = direction;
        sprite.FlipH = direction < 0;

        // Mirror both raycasts horizontally
        floorChecker.Position = new Vector2(
            Mathf.Abs(floorChecker.Position.X) * direction,
            floorChecker.Position.Y
        );

        floorChecker.TargetPosition = new Vector2(
            Mathf.Abs(floorChecker.TargetPosition.X) * direction,
            floorChecker.TargetPosition.Y
        );

        detection.Position = new Vector2(
            Mathf.Abs(detection.Position.X) * direction,
            detection.Position.Y
        );

        detection.TargetPosition = new Vector2(
            Mathf.Abs(detection.TargetPosition.X) * direction,
            detection.TargetPosition.Y
        );
    }

    private void PlayAnimation(string animation)
    {
        if (sprite.Animation != animation || !sprite.IsPlaying())
            sprite.Play(animation);
    }

    private void ReturnToPatrol()
    {
        state = WalkerState.Patrol;

        float distanceA = Mathf.Abs(GlobalPosition.X - patrolPositionA.X);
        float distanceB = Mathf.Abs(GlobalPosition.X - patrolPositionB.X);

        currentPatrolTarget = distanceA < distanceB
            ? patrolPositionA
            : patrolPositionB;
    }
}

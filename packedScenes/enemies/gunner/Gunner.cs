using Godot;

public partial class Gunner : CharacterBody2D
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

    [Export] public PackedScene PlasmaBoltScene;
    [Export] public float FireCooldown = 1.5f;
    [Export] public float FireDelay = 0.5f;

    private float chaseCooldownTimer = 0.0f;
    private float fireCooldownTimer = 0.0f;
    private float fireDelayTimer = 0.0f;

    private float lastKnownPlayerX;

    private int facingDirection = 1;

    private bool isFiring = false;

    private Player player;
    private AnimatedSprite2D sprite;
    private AnimatedSprite2D weapon;

    private RayCast2D floorChecker;
    private RayCast2D detection;

    private Area2D contactArea;

    private Node2D gunflip;
    private Marker2D shootPoint;

    private Vector2 patrolPositionA;
    private Vector2 patrolPositionB;
    private Vector2 currentPatrolTarget;


    public override void _Ready()
    {
        base._Ready();

        // Weapon nodes
        gunflip = GetNode<Node2D>("gunflip");
        weapon = GetNode<AnimatedSprite2D>("gunflip/weapon");
        shootPoint = GetNode<Marker2D>("gunflip/weapon/ShootPoint");

        weapon.AnimationFinished += OnWeaponAnimationFinished;

        // Player reference
        player = GetTree().GetFirstNodeInGroup("player") as Player;

        if (player != null)
            AddCollisionExceptionWith(player);

        

        // Main sprite and raycasts
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

        // Patrol points
        patrolPositionA = GetNode<Marker2D>("PatrolPointA").GlobalPosition;
        patrolPositionB = GetNode<Marker2D>("PatrolPointB").GlobalPosition;

        currentPatrolTarget = patrolPositionA;

        // Contact area
        contactArea = GetNode<Area2D>("ContactArea");
        contactArea.BodyEntered += CollisionHandler;
    }


    private void CollisionHandler(Node2D other)
    {
        if (other is not Player body)
            return;

        if (body.isHidden() && state == WalkerState.Patrol)
            return;

        body.kill();

        state = WalkerState.Patrol;
        chaseCooldownTimer = ChaseCooldown;

        isFiring = false;

        // Turn away from the player
        currentPatrolTarget = facingDirection > 0
            ? patrolPositionA
            : patrolPositionB;
    }


    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        // Gravity
        if (!IsOnFloor())
            Velocity += GetGravity() * (float)delta;

        // Fire cooldown
        if (fireCooldownTimer > 0)
        {
            fireCooldownTimer = Mathf.Max(
                0,
                fireCooldownTimer - (float)delta
            );
        }

        // Chase cooldown
        if (chaseCooldownTimer > 0)
        {
            chaseCooldownTimer = Mathf.Max(
                0,
                chaseCooldownTimer - (float)delta
            );
        }

        // Detect player
        bool playerDetected = chaseCooldownTimer <= 0 && DetectPlayer();

        if (playerDetected)
            state = WalkerState.Chase;

        if (playerDetected)
        {
            int directionToPlayer =
                player.GlobalPosition.X > GlobalPosition.X ? 1 : -1;

            SetFacingDirection(directionToPlayer);

            // Begin firing when cooldown is finished
            if (fireCooldownTimer <= 0 && !isFiring)
            {
                StartFiring();
            }
        }


        if (isFiring)
        {
            Velocity = new Vector2(0, Velocity.Y);

            PlayAnimation("idle");

            // Wait before playing weapon animation
            if (fireDelayTimer > 0)
            {
                fireDelayTimer -= (float)delta;

                if (fireDelayTimer <= 0)
                {
                    weapon.Play("default");
                }
            }

            MoveAndSlide();
            return;
        }

        Vector2 targetPosition;
        float movementSpeed;

        if (state == WalkerState.Chase)
        {
            targetPosition = playerDetected
                ? player.GlobalPosition
                : new Vector2(lastKnownPlayerX, GlobalPosition.Y);

            movementSpeed = ChaseSpeed;

            // Return to patrol after reaching last known position
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

            // Switch patrol points when reaching current target
            if (Mathf.Abs(targetPosition.X - GlobalPosition.X) <= ArrivalDistance)
            {
                currentPatrolTarget = currentPatrolTarget == patrolPositionA
                    ? patrolPositionB
                    : patrolPositionA;

                targetPosition = currentPatrolTarget;
            }
        }


        float direction = Mathf.Sign(
            targetPosition.X - GlobalPosition.X
        );


        if (direction != 0)
        {
            SetFacingDirection((int)direction);

            floorChecker.ForceRaycastUpdate();

            if (!floorChecker.IsColliding())
            {
                Velocity = new Vector2(0, Velocity.Y);

                state = WalkerState.Patrol;

                // Turn away from cliff
                if (currentPatrolTarget == patrolPositionA)
                {
                    currentPatrolTarget = patrolPositionB;
                }
                else
                {
                    currentPatrolTarget = patrolPositionA;
                }

                PlayAnimation("idle");
                MoveAndSlide();
                return;
            }
        }


        Velocity = new Vector2(
            direction * movementSpeed,
            Velocity.Y
        );

        PlayAnimation(direction == 0 ? "idle" : "walk");

        MoveAndSlide();
    }



    private bool DetectPlayer()
    {
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

        // Flip main sprite
        sprite.FlipH = direction < 0;

        // Flip weapon and ShootPoint together
        gunflip.Scale = new Vector2(direction, 1);

        // Mirror FloorChecker
        floorChecker.Position = new Vector2(
            Mathf.Abs(floorChecker.Position.X) * direction,
            floorChecker.Position.Y
        );

        floorChecker.TargetPosition = new Vector2(
            Mathf.Abs(floorChecker.TargetPosition.X) * direction,
            floorChecker.TargetPosition.Y
        );

        // Mirror Detection ray
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

        float distanceA = Mathf.Abs(
            GlobalPosition.X - patrolPositionA.X
        );

        float distanceB = Mathf.Abs(
            GlobalPosition.X - patrolPositionB.X
        );

        currentPatrolTarget = distanceA < distanceB
            ? patrolPositionA
            : patrolPositionB;
    }


    // SHOOTING

    private void StartFiring()
    {
        if (isFiring)
            return;

        isFiring = true;

        // Start the half-second windup
        fireDelayTimer = FireDelay;
    }


    private void OnWeaponAnimationFinished()
    {
        if (!isFiring)
            return;

        // Spawn projectile when firing animation finishes
        SpawnPlasmaBolt();

        // Shooting sequence complete
        isFiring = false;

        // Start cooldown
        fireCooldownTimer = FireCooldown;
    }


    private void SpawnPlasmaBolt()
    {
        if (PlasmaBoltScene == null)
            return;

        PlasmaBolt bolt = PlasmaBoltScene.Instantiate<PlasmaBolt>();

        GetTree().CurrentScene.AddChild(bolt);

        bolt.GlobalPosition = shootPoint.GlobalPosition;

        bolt.SetDirection(facingDirection);
    }
}
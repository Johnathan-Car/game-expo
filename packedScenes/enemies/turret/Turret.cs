
using Godot;

public partial class Turret : CharacterBody2D
{
    [Export] public PackedScene PlasmaBoltScene;
    [Export] public float FireCooldown = 1.5f;
    [Export] public float FireDelay = 0.5f;

    // True = face left, False = face right
    [Export] public bool MirrorDirection = false;

    private float fireCooldownTimer = 0.0f;
    private float fireDelayTimer = 0.0f;

    private int facingDirection = 1;

    private bool isFiring = false;

    private Player player;

    private AnimatedSprite2D sprite;
    private AnimatedSprite2D weapon;

    private RayCast2D detection;

    private Area2D contactArea;

    private Node2D gunflip;
    private Marker2D shootPoint;


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

        // Ignore other enemies
        foreach (Node enemy in GetTree().GetNodesInGroup("enemy"))
        {
            if (enemy is PhysicsBody2D body && body != this)
                AddCollisionExceptionWith(body);
        }

        // Main sprite and detection ray
        sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        detection = GetNode<RayCast2D>("Detection");

        // Contact area
        contactArea = GetNode<Area2D>("ContactArea");
        contactArea.BodyEntered += CollisionHandler;

        // Set initial facing direction
        facingDirection = MirrorDirection ? -1 : 1;

        ApplyFacingDirection();
    }


    private void CollisionHandler(Node2D other)
    {
        if (other is Player body)
        {
            if (body.isHidden())
                return;

            body.kill();

            isFiring = false;
            weapon.Stop();

            fireDelayTimer = 0;
        }
    }


    public override void _PhysicsProcess(double delta)
    {
        // Fire cooldown
        if (fireCooldownTimer > 0)
        {
            fireCooldownTimer = Mathf.Max(
                0,
                fireCooldownTimer - (float)delta
            );
        }

        // Detect player
        bool playerDetected = DetectPlayer();

        // Start firing when player is detected
        if (playerDetected && fireCooldownTimer <= 0 && !isFiring)
        {
            StartFiring();
        }

        // Shooting sequence
        if (isFiring)
        {
            if (fireDelayTimer > 0)
            {
                fireDelayTimer -= (float)delta;

                if (fireDelayTimer <= 0)
                {
                    weapon.Play("default");
                }
            }
        }
    }


    private bool DetectPlayer()
    {
        if (player == null || !GodotObject.IsInstanceValid(player))
            return false;

        if (player.isHidden())
            return false;

        detection.ForceRaycastUpdate();

        return detection.GetCollider() is Player;
    }


    private void ApplyFacingDirection()
    {
        // Flip main sprite
        sprite.FlipH = facingDirection < 0;

        // Flip weapon and ShootPoint together
        gunflip.Scale = new Vector2(facingDirection, 1);

        // Mirror detection ray
        detection.Position = new Vector2(
            Mathf.Abs(detection.Position.X) * facingDirection,
            detection.Position.Y
        );

        detection.TargetPosition = new Vector2(
            Mathf.Abs(detection.TargetPosition.X) * facingDirection,
            detection.TargetPosition.Y
        );
    }


    // SHOOTING

    private void StartFiring()
    {
        if (isFiring)
            return;

        isFiring = true;

        // Start the windup
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

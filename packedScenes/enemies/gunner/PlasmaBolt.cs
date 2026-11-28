using Godot;

public partial class PlasmaBolt : Area2D
{
    [Export] public float Speed = 300.0f;
    [Export] public float MaxDistance = 1000.0f;

    private Vector2 direction = Vector2.Right;
    private Vector2 startPosition;

    public override void _Ready()
    {
        startPosition = GlobalPosition;
        BodyEntered += OnBodyEntered;
    }

    public void SetDirection(int facingDirection)
    {
        direction = new Vector2(Mathf.Sign(facingDirection), 0);

        if (GetNodeOrNull<Sprite2D>("Sprite2D") is Sprite2D sprite)
            sprite.FlipH = facingDirection < 0;
    }

    public override void _PhysicsProcess(double delta)
    {
        GlobalPosition += direction * Speed * (float)delta;

        if (startPosition.DistanceTo(GlobalPosition) >= MaxDistance)
            QueueFree();
    }

    private void OnBodyEntered(Node2D body)
    {
        // Don't hit other enemies
        if (body.IsInGroup("enemy"))
            return;

        if (body is Player player)
        {
            player.killCheck();
            QueueFree();
            return;
        }

        HitTiles();
    }

    private void HitTiles()
    {
        QueueFree();
    }
}
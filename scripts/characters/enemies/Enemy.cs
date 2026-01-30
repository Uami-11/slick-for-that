using Godot;

/// <summary>
/// Basic enemy - extend this for different enemy types
/// </summary>
public partial class Enemy : CharacterBody2D
{
	[ExportGroup("Stats")]
	[Export] public int MaxHealth = 3;
	[Export] public float MoveSpeed = 100f;
	[Export] public int Damage = 1;
	
	[ExportGroup("AI")]
	[Export] public float DetectionRange = 200f;
	[Export] public float AttackRange = 50f;
	
	[ExportGroup("References")]
	[Export] public AnimatedSprite2D AnimatedSprite;
	[Export] public Area2D Hurtbox;
	[Export] public Area2D AttackHitbox;
	
	private int currentHealth;
	private PlayerController player;
	private bool isDead = false;
	
	public override void _Ready()
	{
		currentHealth = MaxHealth;
		
		// Find player
		player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		
		// Add to enemy group
		AddToGroup("enemy");
		
		// Setup hurtbox
		if (Hurtbox != null)
		{
			Hurtbox.AddToGroup("enemy_hurtbox");
		}
		
		// Setup attack hitbox
		if (AttackHitbox != null)
		{
			AttackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (isDead || player == null)
			return;
		
		// Simple AI: move toward player if in range
		float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
		
		if (distanceToPlayer <= DetectionRange)
		{
			// Move toward player
			Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
			
			if (distanceToPlayer > AttackRange)
			{
				Velocity = direction * MoveSpeed;
				
				// Flip sprite
				if (AnimatedSprite != null)
				{
					AnimatedSprite.FlipH = direction.X < 0;
				}
			}
			else
			{
				// In attack range - stop moving
				Velocity = Vector2.Zero;
				// Trigger attack animation/logic here
			}
		}
		else
		{
			// Out of range - idle or patrol
			Velocity = Vector2.Zero;
		}
		
		// Apply gravity
		Velocity = new Vector2(Velocity.X, Velocity.Y + 980 * (float)delta);
		
		MoveAndSlide();
	}
	
	public void TakeDamage(int damage)
	{
		if (isDead)
			return;
		
		currentHealth -= damage;
		GD.Print($"Enemy took {damage} damage. Health: {currentHealth}/{MaxHealth}");
		
		// Play hit animation/effect
		if (AnimatedSprite != null)
		{
			AnimatedSprite.Modulate = new Color(1, 0.5f, 0.5f); // Flash red
			GetTree().CreateTimer(0.1).Timeout += () => AnimatedSprite.Modulate = new Color(1, 1, 1);
		}
		
		if (currentHealth <= 0)
		{
			Die();
		}
	}
	
	private void Die()
	{
		isDead = true;
		GD.Print("Enemy died!");
		
		// Notify player
		if (player != null)
		{
			player.OnEnemyKilled();
		}
		
		// Play death animation, spawn particles, etc.
		// For now, just remove
		QueueFree();
	}
	
	private void OnAttackHitboxBodyEntered(Node2D body)
	{
		if (body is PlayerController playerHit)
		{
			playerHit.TakeDamage(Damage);
		}
	}
}

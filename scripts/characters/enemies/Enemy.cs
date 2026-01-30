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
	[Export] public float DetectionRange = 400f;  // INCREASED: Was 200f - easier to test
	[Export] public float AttackRange = 50f;
	[Export] public float PatrolSpeed = 60f;  // NEW: Patrol when far/no player
	
	[ExportGroup("Physics")]
	[Export] public float Gravity = 980f;
	
	[ExportGroup("References")]
	[Export] public AnimatedSprite2D AnimatedSprite;
	[Export] public Area2D Hurtbox;
	[Export] public Area2D AttackHitbox;
	
	private int currentHealth;
	private PlayerController player;
	private bool isDead = false;
	private float patrolTimer = 0f;
	private float patrolDirection = 1f;
	private double attackTimer = 0;

	public override void _Ready()
	{
		currentHealth = MaxHealth;
		
		// Find player (will retry if fails)
		FindPlayer();
		
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
			AttackHitbox.Monitoring = false;  // NEW: Enable only during attack
		}
		
		// Default animation
		if (AnimatedSprite != null)
		{
			AnimatedSprite.Play("idle");  // Change to your idle anim name
		}
	}
	
	private void FindPlayer()
	{
		player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		if (player == null)
		{
			GD.Print("Enemy WARNING: Player not found! Add AddToGroup(\"player\"); to PlayerController._Ready()");
		}
		else
		{
			GD.Print("Enemy found player!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;  // FIXED: Only dead skips - ALWAYS apply gravity/patrol even if no player!

		// ALWAYS: Use LOCAL velocity var (Godot 4 rule - fixes CS1612)
		Vector2 velocity = Velocity;

		// FIXED GRAVITY: Only if not on floor (no jitter/sliding in Grounded mode)
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}

		// LAZY: Retry find player every 2 sec if missing
		if (player == null)
		{
			patrolTimer += (float)delta;
			if (patrolTimer > 2f)
			{
				FindPlayer();
				patrolTimer = 0f;
			}
			Patrol(ref velocity, (float)delta);
			Velocity = velocity;
			MoveAndSlide();
			return;
		}

		// AI: Detect player
		float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
		//GD.Print($"Enemy Distance to Player: {distanceToPlayer:F1} / Range: {DetectionRange}");  // DEBUG: Watch console!

		if (distanceToPlayer <= DetectionRange)
		{
			//GD.Print("Enemy DETECTED player! Chasing...");  // DEBUG
			Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
			
			if (distanceToPlayer > AttackRange)
			{
				// Chase
				velocity.X = direction.X * MoveSpeed;
				FlipAnim("walk", direction.X < 0);  // Assume "walk" anim
			}
			else
			{
				// Attack (stops moving)
				velocity.X = 0;
				Attack((float)delta);
				FlipAnim("attack", patrolDirection < 0);
			}
		}
		else
		{
			// Patrol (no more full idle)
			Patrol(ref velocity, (float)delta);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
	
	private void Patrol(ref Vector2 velocity, float delta)
	{
		patrolTimer += delta * 1.5f;
		float targetDir = Mathf.Sign(Mathf.Sin(patrolTimer));
		if (targetDir != patrolDirection)
		{
			patrolDirection = targetDir;
		}
		velocity.X = patrolDirection * PatrolSpeed;
		FlipAnim("walk", patrolDirection < 0);
	}
	
	private void Attack(float delta)
	{
		attackTimer += delta;
		if (attackTimer >= 1.2f)
		{
			if (AttackHitbox != null)
			{
				AttackHitbox.Monitoring = true;
				GetTree().CreateTimer(0.3f).Timeout += () =>
				{
					if (AttackHitbox != null) AttackHitbox.Monitoring = false;
				};
			}
			attackTimer = 0;
			//GD.Print("Enemy ATTACKING!");
		}
	}
	
	private void FlipAnim(string anim, bool flipH)
	{
		if (AnimatedSprite != null)
		{
			AnimatedSprite.FlipH = flipH;
			if (AnimatedSprite.Animation != anim)
			{
				AnimatedSprite.Play(anim);
			}
		}
	}

	public void TakeDamage(int damage)
	{
		if (isDead) return;
		
		currentHealth -= damage;
		//GD.Print($"Enemy took {damage} damage. Health: {currentHealth}/{MaxHealth}");
		
		if (AnimatedSprite != null)
		{
			AnimatedSprite.Modulate = Colors.Red;
			GetTree().CreateTimer(0.1f).Timeout += () => AnimatedSprite.Modulate = Colors.White;
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
		
		if (player != null)
		{
			player.OnEnemyKilled();  // Ensure PlayerController has this method
		}
		
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

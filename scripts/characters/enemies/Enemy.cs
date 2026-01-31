using Godot;

/// <summary>
/// Basic enemy with knockback and stun mechanics
/// </summary>
public partial class Enemy : CharacterBody2D
{
	[ExportGroup("Stats")]
	[Export] public int MaxHealth = 3;
	[Export] public float MoveSpeed = 100f;
	[Export] public int Damage = 1;
	
	[ExportGroup("AI")]
	[Export] public float DetectionRange = 400f;
	[Export] public float AttackRange = 50f;
	[Export] public float PatrolSpeed = 60f;
	
	[ExportGroup("Physics")]
	[Export] public float Gravity = 980f;
	[Export] public float KnockbackForce = 200f;
	[Export] public float KnockbackDuration = 0.2f;
	
	[ExportGroup("References")]
	[Export] public AnimatedSprite2D AnimatedSprite;
	[Export] public Area2D Hurtbox;
	[Export] public Area2D AttackHitbox;
	
	private int currentHealth;
	private PlayerController player;
	private bool isDead = false;
	private bool isStunned = false;
	private bool isKnockedBack = false;
	private float patrolTimer = 0f;
	private float patrolDirection = 1f;
	private float attackTimer = 0f;
	private float stunTimer = 0f;
	private float knockbackTimer = 0f;
	private Vector2 knockbackVelocity = Vector2.Zero;

	public override void _Ready()
	{
		currentHealth = MaxHealth;
		FindPlayer();
		AddToGroup("enemy");
		
		if (Hurtbox != null)
		{
			Hurtbox.AddToGroup("enemy_hurtbox");
		}
		
		if (AttackHitbox != null)
		{
			AttackHitbox.BodyEntered += OnAttackHitboxBodyEntered;
			AttackHitbox.Monitoring = false;
		}
		
		if (AnimatedSprite != null)
		{
			AnimatedSprite.Play("idle");
		}
	}
	
	private void FindPlayer()
	{
		player = GetTree().GetFirstNodeInGroup("player") as PlayerController;
		if (player == null)
		{
			GD.Print("Enemy WARNING: Player not found!");
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isDead) return;

		Vector2 velocity = Velocity;

		// Apply gravity
		if (!IsOnFloor())
		{
			velocity.Y += Gravity * (float)delta;
		}
		
		// Handle stun timer
		if (isStunned)
		{
			stunTimer -= (float)delta;
			if (stunTimer <= 0)
			{
				isStunned = false;
				if (AnimatedSprite != null)
					AnimatedSprite.Modulate = Colors.White;
			}
			else
			{
				// Flash blue while stunned
				float flash = Mathf.Sin(stunTimer * 20f) > 0 ? 1f : 0.5f;
				if (AnimatedSprite != null)
					AnimatedSprite.Modulate = new Color(0.5f, 0.5f, 1f, flash);
			}
			
			velocity.X = 0; // Can't move while stunned
			Velocity = velocity;
			MoveAndSlide();
			return;
		}
		
		// Handle knockback
		if (isKnockedBack)
		{
			knockbackTimer -= (float)delta;
			if (knockbackTimer <= 0)
			{
				isKnockedBack = false;
			}
			else
			{
				velocity = knockbackVelocity;
				knockbackVelocity.Y += Gravity * (float)delta;
				Velocity = velocity;
				MoveAndSlide();
				return;
			}
		}

		// Retry find player if missing
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

		if (distanceToPlayer <= DetectionRange)
		{
			Vector2 direction = (player.GlobalPosition - GlobalPosition).Normalized();
			
			if (distanceToPlayer > AttackRange)
			{
				// Chase
				velocity.X = direction.X * MoveSpeed;
				FlipAnim("walk", direction.X < 0);
			}
			else
			{
				// Attack
				velocity.X = 0;
				Attack((float)delta);
				FlipAnim("attack", patrolDirection < 0);
			}
		}
		else
		{
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
		}
	}
	
	private void FlipAnim(string anim, bool flipH)
	{
		if (AnimatedSprite != null)
		{
			AnimatedSprite.FlipH = flipH;
			if (AnimatedSprite.Animation != anim && !isStunned)
			{
				AnimatedSprite.Play(anim);
			}
		}
	}

	public void TakeDamage(int damage, Vector2 attackerPosition)
	{
		if (isDead) return;
		
		currentHealth -= damage;
		GD.Print($"Enemy took {damage} damage. Health: {currentHealth}/{MaxHealth}");
		
		// Flash red
		if (AnimatedSprite != null && !isStunned)
		{
			AnimatedSprite.Modulate = Colors.Red;
			GetTree().CreateTimer(0.1f).Timeout += () => 
			{
				if (AnimatedSprite != null && !isStunned)
					AnimatedSprite.Modulate = Colors.White;
			};
		}
		
		// Apply knockback
		ApplyKnockback(attackerPosition);
		
		if (currentHealth <= 0)
		{
			Die();
		}
	}
	
	private void ApplyKnockback(Vector2 attackerPosition)
	{
		isKnockedBack = true;
		knockbackTimer = KnockbackDuration;
		
		Vector2 knockbackDir = (GlobalPosition - attackerPosition).Normalized();
		knockbackVelocity = new Vector2(knockbackDir.X * KnockbackForce, -100f);
	}
	
	public void Stun(float duration)
	{
		isStunned = true;
		stunTimer = duration;
		GD.Print($"Enemy stunned for {duration} seconds!");
	}
	
	private void Die()
	{
		isDead = true;
		GD.Print("Enemy died!");
		
		if (player != null)
		{
			player.OnEnemyKilled();
		}
		
		QueueFree();
	}
	
	private void OnAttackHitboxBodyEntered(Node2D body)
	{
		if (body is PlayerController playerHit)
		{
			playerHit.TakeDamage(Damage, GlobalPosition);
		}
	}
}

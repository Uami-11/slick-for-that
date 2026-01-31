using Godot;

/// <summary>
/// Main player controller - holds all player data and references
/// </summary>
public partial class PlayerController : CharacterBody2D
{
	// Movement
	[ExportGroup("Movement")]
	[Export] public float BaseSpeed = 200f;
	[Export] public float Acceleration = 800f;
	[Export] public float Friction = 1000f;
	[Export] public float JumpVelocity = -400f;
	[Export] public float Gravity = 980f;
	[Export] public float MaxFallSpeed = 500f;
	[Export] public int MaxJumps = 1;
	[Export] public float CoyoteTime = 0.15f;
	[Export] public float JumpBufferTime = 0.1f;
	
	// Dash Attack
	[ExportGroup("Dash Attack")]
	[Export] public float DashSpeed = 250f;
	[Export] public float DashDuration = 0.1f;
	[Export] public float DashCooldown = 0.5f;
	[Export] public float DashBounceVelocity = -300f; // Upward bounce when hitting enemy
	
	// Slide Attack
	[ExportGroup("Slide")]
	[Export] public float SlideSpeed = 400f;
	[Export] public float SlideDuration = 0.5f;
	[Export] public float SlideCooldown = 1f;
	[Export] public int SlideDamage = 1;
	[Export] public float SlideStunDuration = 1f; // How long enemies are stunned
	
	// Speed Scaling
	[ExportGroup("Speed Scaling")]
	[Export] public float SpeedMultiplierPerKill = 0.1f;
	[Export] public float MaxSpeedMultiplier = 2f;
	
	// Health & Damage
	[ExportGroup("Health")]
	[Export] public int MaxHealth = 3;
	[Export] public float InvincibilityDuration = 1f;
	[Export] public float KnockbackForce = 300f;
	[Export] public float KnockbackDuration = 0.3f;
	
	// References
	[ExportGroup("References")]
	[Export] public AnimatedSprite2D AnimatedSprite;
	[Export] public Area2D DashHitbox;
	[Export] public Area2D SlideHitbox;
	[Export] public CollisionShape2D HurtboxCollision;
	
	// State
	public StateMachine StateMachine { get; private set; }
	public int KillCount { get; set; } = 0;
	public float CurrentSpeed => BaseSpeed * (1 + Mathf.Min(KillCount * SpeedMultiplierPerKill, MaxSpeedMultiplier));
	public int CurrentHealth { get; set; }
	public bool IsInvincible { get; set; } = false;
	public bool IsKnockedBack { get; set; } = false;
	
	// Jump state
	public int JumpsRemaining { get; set; } = 1;
	public float CoyoteTimer { get; set; } = 0f;
	public float JumpBufferTimer { get; set; } = 0f;
	public bool WasOnFloor { get; set; } = false;
	
	// Timers
	public float DashTimer { get; set; } = 0f;
	public float DashCooldownTimer { get; set; } = 0f;
	public float SlideTimer { get; set; } = 0f;
	public float SlideCooldownTimer { get; set; } = 0f;
	public float InvincibilityTimer { get; set; } = 0f;
	public float KnockbackTimer { get; set; } = 0f;
	
	// Input direction
	public Vector2 InputDirection { get; set; } = Vector2.Zero;
	public int FacingDirection { get; set; } = 1;
	private Vector2 knockbackVelocity = Vector2.Zero;
	
	public override void _Ready()
	{
		AddToGroup("player");
		StateMachine = GetNode<StateMachine>("StateMachine");
		CurrentHealth = MaxHealth;
		
		// Disable hitboxes by default
		if (DashHitbox != null)
			DashHitbox.Monitoring = false;
		if (SlideHitbox != null)
			SlideHitbox.Monitoring = false;
	}
	
	public override void _Process(double delta)
	{
		// Update timers
		if (DashCooldownTimer > 0)
			DashCooldownTimer -= (float)delta;
			
		if (SlideCooldownTimer > 0)
			SlideCooldownTimer -= (float)delta;
		
		if (CoyoteTimer > 0)
			CoyoteTimer -= (float)delta;
			
		if (JumpBufferTimer > 0)
			JumpBufferTimer -= (float)delta;
		
		// Invincibility timer
		if (InvincibilityTimer > 0)
		{
			InvincibilityTimer -= (float)delta;
			if (InvincibilityTimer <= 0)
			{
				IsInvincible = false;
				if (AnimatedSprite != null)
					AnimatedSprite.Modulate = new Color(1, 1, 1, 1);
			}
			else
			{
				// Flash effect
				float flash = Mathf.Sin(InvincibilityTimer * 30f) > 0 ? 1f : 0.3f;
				if (AnimatedSprite != null)
					AnimatedSprite.Modulate = new Color(1, 1, 1, flash);
			}
		}
		
		// Knockback timer
		if (KnockbackTimer > 0)
		{
			KnockbackTimer -= (float)delta;
			if (KnockbackTimer <= 0)
			{
				IsKnockedBack = false;
			}
		}
		
		// Track floor state for coyote time
		if (IsOnFloor())
		{
			JumpsRemaining = MaxJumps;
			CoyoteTimer = CoyoteTime;
			WasOnFloor = true;
		}
		else if (WasOnFloor)
		{
			WasOnFloor = false;
		}
		
		// Jump buffer
		if (Input.IsActionJustPressed("jump"))
		{
			JumpBufferTimer = JumpBufferTime;
		}
		
		// Update input direction
		InputDirection = Input.GetVector("move_left", "move_right", "move_up", "move_down");
		
		// Update facing direction
		if (InputDirection.X != 0)
		{
			FacingDirection = InputDirection.X > 0 ? -1 : 1;
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		// Apply knockback if active
		if (IsKnockedBack)
		{
			Velocity = knockbackVelocity;
			knockbackVelocity.Y += Gravity * (float)delta;
			MoveAndSlide();
		}
	}
	
	public void OnEnemyKilled()
	{
		KillCount++;
		GD.Print($"Kill count: {KillCount}, Speed: {CurrentSpeed}");
	}
	
	public void TakeDamage(int damage, Vector2 enemyPosition)
	{
		if (IsInvincible || IsKnockedBack)
			return;
		
		CurrentHealth -= damage;
		GD.Print($"Player took {damage} damage! Health: {CurrentHealth}/{MaxHealth}");
		
		if (CurrentHealth <= 0)
		{
			Die();
			return;
		}
		
		// Start invincibility
		IsInvincible = true;
		InvincibilityTimer = InvincibilityDuration;
		
		// Apply knockback
		IsKnockedBack = true;
		KnockbackTimer = KnockbackDuration;
		Vector2 knockbackDir = (GlobalPosition - enemyPosition).Normalized();
		knockbackVelocity = new Vector2(knockbackDir.X * KnockbackForce, -150f); // Slight upward
		
		// Play hit animation
		if (AnimatedSprite != null)
			AnimatedSprite.Play("hit");
		
		// Transition to hit state (we'll create this)
		if (StateMachine != null)
			StateMachine.TransitionTo("HitState");
	}
	
	private void Die()
	{
		GD.Print("Player died! Restarting scene...");
		
		// Play death animation if you have one
		if (AnimatedSprite != null)
			AnimatedSprite.Play("death");
		
		// Restart scene after short delay
		GetTree().CreateTimer(1f).Timeout += () =>
		{
			GetTree().ReloadCurrentScene();
		};
	}
	
	/// <summary>
	/// Called when dash attack hits an enemy - bounces player upward
	/// </summary>
	public void OnDashHitEnemy()
	{
		Velocity = new Vector2(Velocity.X, DashBounceVelocity);
		GD.Print("Dash bounce!");
	}
	
	public bool TryJump()
	{
		if (CoyoteTimer > 0 || JumpsRemaining > 0)
		{
			Velocity = new Vector2(Velocity.X, JumpVelocity);
			
			if (CoyoteTimer > 0)
			{
				CoyoteTimer = 0;
			}
			else
			{
				JumpsRemaining--;
			}
			
			return true;
		}
		
		return false;
	}
	
	public void BufferJump()
	{
		JumpBufferTimer = JumpBufferTime;
	}
}

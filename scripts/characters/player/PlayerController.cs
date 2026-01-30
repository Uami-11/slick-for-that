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
	[Export] public int MaxJumps = 1; // 1 = single jump, 2 = double jump
	[Export] public float CoyoteTime = 0.15f; // Grace period after leaving platform
	[Export] public float JumpBufferTime = 0.1f; // Grace period for early jump input
	
	// Dash Attack
	[ExportGroup("Dash Attack")]
	[Export] public float DashSpeed = 250f;
	[Export] public float DashDuration = 0.1f;
	[Export] public float DashCooldown = 0.5f;
	
	// Slide Attack
	[ExportGroup("Slide")]
	[Export] public float SlideSpeed = 400f;
	[Export] public float SlideDuration = 0.5f;
	[Export] public float SlideCooldown = 1f;
	[Export] public int SlideDamage = 1;
	
	// Speed Scaling
	[ExportGroup("Speed Scaling")]
	[Export] public float SpeedMultiplierPerKill = 0.1f;
	[Export] public float MaxSpeedMultiplier = 2f;
	
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
	
	// Jump state
	
	// Input direction
	public Vector2 InputDirection { get; set; } = Vector2.Zero;
	public int FacingDirection { get; set; } = 1; // 1 = right, -1 = left
	
	public override void _Ready()
	{
		AddToGroup("player");
		StateMachine = GetNode<StateMachine>("StateMachine");
		GD.Print("Player Controlled");
		
		// Disable hitboxes by default
		if (DashHitbox != null)
		{
			DashHitbox.Monitoring = false;
		}
		if (SlideHitbox != null)
		{
			SlideHitbox.Monitoring = false;
		}
	}
	
	public override void _Process(double delta)
	{
		// Update timers
		if (DashCooldownTimer > 0)
			DashCooldownTimer -= (float)delta;
			
		if (SlideCooldownTimer > 0)
			SlideCooldownTimer -= (float)delta;
		
		// Update jump timers
		if (CoyoteTimer > 0)
			CoyoteTimer -= (float)delta;
			
		if (JumpBufferTimer > 0)
			JumpBufferTimer -= (float)delta;
		
		// Track floor state for coyote time
		if (IsOnFloor())
		{
			JumpsRemaining = MaxJumps;
			CoyoteTimer = CoyoteTime;
			WasOnFloor = true;
		}
		else if (WasOnFloor)
		{
			// Just left the ground
			WasOnFloor = false;
		}
		
		// Jump buffer - store jump input for a short time
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
	
	public void OnEnemyKilled()
	{
		KillCount++;
		GD.Print($"Kill count: {KillCount}, Speed: {CurrentSpeed}");
		
		// Add visual/audio feedback here
		// E.g., particle effect, sound, screen shake
	}
	
	public void TakeDamage(int damage = 1)
	{
		GD.Print($"Player took {damage} damage!");
		
		// Add health system here
		// Add knockback, damage animation, etc.
	}
	
	/// <summary>
	/// Attempt to jump - handles coyote time and jump buffering
	/// </summary>
	public bool TryJump()
	{
		// Can jump if on ground (with coyote time) or have jumps remaining
		if (CoyoteTimer > 0 || JumpsRemaining > 0)
		{
			Velocity = new Vector2(Velocity.X, JumpVelocity);
			
			// Consume coyote time if used
			if (CoyoteTimer > 0)
			{
				CoyoteTimer = 0;
			}
			else
			{
				JumpsRemaining--;
			}
			
			GD.Print($"Jumped! Jumps remaining: {JumpsRemaining}");
			return true;
		}
		
		return false;
	}
	
	/// <summary>
	/// Buffer a jump input for a short time
	/// </summary>
	public void BufferJump()
	{
		JumpBufferTimer = JumpBufferTime;
	}
}

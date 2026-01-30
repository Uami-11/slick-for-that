using Godot;

/// <summary>
/// Jump/aerial state - handles movement while in the air
/// </summary>
public partial class JumpState : State
{
	private bool hasReleasedJump = false;
	
	public override void Enter()
	{
		hasReleasedJump = false;
		
		// Play jump animation
		if (player.AnimatedSprite != null)
		{
			// Use different animations based on vertical velocity
			if (player.Velocity.Y < 0)
				player.AnimatedSprite.Play("jump"); // Rising
			else
				player.AnimatedSprite.Play("fall"); // Falling
		}
	}
	
	public override void PhysicsUpdate(double delta)
	{
		// Apply gravity
		player.Velocity = new Vector2(
			player.Velocity.X,
			player.Velocity.Y + player.Gravity * (float)delta
		);
		
		// Variable jump height - cut jump short if button released
		if (Input.IsActionJustReleased("jump") && player.Velocity.Y < 0)
		{
			hasReleasedJump = true;
			player.Velocity = new Vector2(
				player.Velocity.X,
				player.Velocity.Y * 0.5f // Cut jump momentum
			);
		}
		
		// Air control - horizontal movement
		if (player.InputDirection.X != 0)
		{
			// Slightly reduced acceleration in air for better control
			float airAcceleration = player.Acceleration * 0.7f;
			
			player.Velocity = new Vector2(
				Mathf.MoveToward(player.Velocity.X, player.InputDirection.X * player.CurrentSpeed, airAcceleration * (float)delta),
				player.Velocity.Y
			);
			
			// Flip sprite based on direction
			if (player.AnimatedSprite != null)
			{
				player.AnimatedSprite.FlipH = player.FacingDirection < 0;
			}
		}
		else
		{
			// Apply slight air friction when no input
			player.Velocity = new Vector2(
				Mathf.MoveToward(player.Velocity.X, 0, player.Friction * 0.3f * (float)delta),
				player.Velocity.Y
			);
		}
		
		// Update animation based on velocity
		if (player.AnimatedSprite != null)
		{
			if (player.Velocity.Y < -50)
			{
				if (player.AnimatedSprite.Animation != "jump")
					player.AnimatedSprite.Play("jump");
			}
			else if (player.Velocity.Y > 50)
			{
				if (player.AnimatedSprite.Animation != "fall")
					player.AnimatedSprite.Play("fall");
			}
		}
		
		player.MoveAndSlide();
		
		// Handle jump buffering - if player pressed jump just before landing
		if (player.JumpBufferTimer > 0 && player.IsOnFloor())
		{
			player.JumpBufferTimer = 0;
			player.TryJump();
			return; // Stay in jump state
		}
		
		// Check for landing
		if (player.IsOnFloor())
		{
			if (player.InputDirection.X != 0)
			{
				player.StateMachine.TransitionTo("RunState");
			}
			else
			{
				player.StateMachine.TransitionTo("IdleState");
			}
			return;
		}
		
		// Allow double jump (or more if MaxJumps > 2)
		if (Input.IsActionJustPressed("jump"))
		{
			if (player.TryJump())
			{
				hasReleasedJump = false;
				// Stay in jump state but reset animation
				if (player.AnimatedSprite != null)
					player.AnimatedSprite.Play("jump");
			}
		}
		
		// Can still dash attack in air
		if (Input.IsActionJustPressed("dash") && player.DashCooldownTimer <= 0)
		{
			player.StateMachine.TransitionTo("DashAttackState");
		}
		
		// Can still slide in air (will start when landing)
		if (Input.IsActionJustPressed("slide") && player.SlideCooldownTimer <= 0)
		{
			player.StateMachine.TransitionTo("SlideState");
		}
	}
}

using Godot;

/// <summary>
/// Slide attack state - low sliding attack that damages enemies
/// </summary>
public partial class SlideState : State
{
	public override void Enter()
	{
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("slide");
		
		// Start slide timer
		player.SlideTimer = player.SlideDuration;
		player.SlideCooldownTimer = player.SlideCooldown;
		
		// Enable slide hitbox
		if (player.SlideHitbox != null)
		{
			player.SlideHitbox.Monitoring = true;
			// Disconnect first to avoid duplicate connections, then connect
			player.SlideHitbox.AreaEntered -= OnHitboxAreaEntered;
			player.SlideHitbox.AreaEntered += OnHitboxAreaEntered;
			
			player.SlideHitbox.BodyEntered -= OnHitboxBodyEntered;
			player.SlideHitbox.BodyEntered += OnHitboxBodyEntered;
		}
		
		GD.Print("Sliding!");
	}
	
	public override void Exit()
	{
		// Disable slide hitbox
		if (player.SlideHitbox != null)
		{
			player.SlideHitbox.Monitoring = false;
		}
	}
	
	public override void PhysicsUpdate(double delta)
	{
		player.SlideTimer -= (float)delta;
		
		// Slide in the facing direction with slight deceleration
		float slideProgress = player.SlideTimer / player.SlideDuration;
		float currentSlideSpeed = player.SlideSpeed * slideProgress; // Decelerates as slide progresses
		
		player.Velocity = new Vector2(
			player.FacingDirection * -1 * currentSlideSpeed,
			player.Velocity.Y + player.Gravity * (float)delta // Still apply gravity
		);
		
		player.MoveAndSlide();
		
		// Return to appropriate state when slide finishes
		if (player.SlideTimer <= 0)
		{
			if (player.InputDirection.X != 0)
			{
				player.StateMachine.TransitionTo("RunState");
			}
			else
			{
				player.StateMachine.TransitionTo("IdleState");
			}
		}
	}
	
	private void OnHitboxAreaEntered(Area2D area)
	{
		// Check if it's an enemy hurtbox
		if (area.IsInGroup("enemy_hurtbox"))
		{
			// Deal damage to enemy
			if (area.Owner is Enemy enemy)
			{
				enemy.TakeDamage(player.SlideDamage);
				GD.Print("Hit enemy with slide attack!");
				
				// Add hit effect here
			}
		}
	}
	
	private void OnHitboxBodyEntered(Node2D body)
	{
		// Alternative: Check if body is an enemy
		if (body.IsInGroup("enemy"))
		{
			if (body is Enemy enemy)
			{
				enemy.TakeDamage(player.SlideDamage);
				GD.Print("Hit enemy with slide attack!");
			}
		}
	}
}

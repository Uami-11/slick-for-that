using Godot;

/// <summary>
/// Slide attack state - low sliding attack that damages and stuns enemies
/// Only works when on the ground
/// </summary>
public partial class SlideState : State
{
	public override void Enter()
	{
		// Only allow slide if on floor
		if (!player.IsOnFloor())
		{
			GD.Print("Can't slide in air!");
			player.StateMachine.TransitionTo("JumpState");
			return;
		}
		
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("slide");
		
		player.SlideTimer = player.SlideDuration;
		player.SlideCooldownTimer = player.SlideCooldown;
		
		if (player.SlideHitbox != null)
		{
			player.SlideHitbox.Monitoring = true;
			player.SlideHitbox.AreaEntered -= OnHitboxAreaEntered;
			player.SlideHitbox.AreaEntered += OnHitboxAreaEntered;
			
			player.SlideHitbox.BodyEntered -= OnHitboxBodyEntered;
			player.SlideHitbox.BodyEntered += OnHitboxBodyEntered;
		}
		
		GD.Print("Sliding!");
	}
	
	public override void Exit()
	{
		if (player.SlideHitbox != null)
		{
			player.SlideHitbox.Monitoring = false;
		}
	}
	
	public override void PhysicsUpdate(double delta)
	{
		player.SlideTimer -= (float)delta;
		
		// Slide in the facing direction with deceleration
		float slideProgress = player.SlideTimer / player.SlideDuration;
		float currentSlideSpeed = player.SlideSpeed * slideProgress;
		
		player.Velocity = new Vector2(
			player.FacingDirection * -1 * currentSlideSpeed,
			player.Velocity.Y + player.Gravity * (float)delta
		);
		
		player.MoveAndSlide();
		
		// If we leave the ground during slide, go to jump state
		if (!player.IsOnFloor())
		{
			player.StateMachine.TransitionTo("JumpState");
			return;
		}
		
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
		if (area.IsInGroup("enemy_hurtbox"))
		{
			if (area.Owner is Enemy enemy)
			{
				enemy.TakeDamage(player.SlideDamage, player.GlobalPosition);
				enemy.Stun(player.SlideStunDuration);
				GD.Print("Hit enemy with slide attack! Enemy stunned!");
			}
		}
	}
	
	private void OnHitboxBodyEntered(Node2D body)
	{
		if (body.IsInGroup("enemy"))
		{
			if (body is Enemy enemy)
			{
				enemy.TakeDamage(player.SlideDamage, player.GlobalPosition);
				enemy.Stun(player.SlideStunDuration);
				GD.Print("Hit enemy with slide attack! Enemy stunned!");
			}
		}
	}
}

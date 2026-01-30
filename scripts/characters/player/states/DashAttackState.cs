using Godot;

/// <summary>
/// Dash attack state - King's Scepter style forward dash with hitbox
/// </summary>
public partial class DashAttackState : State
{
	public override void Enter()
	{
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("dash");
		
		// Start dash timer
		player.DashTimer = player.DashDuration;
		player.DashCooldownTimer = player.DashCooldown;
		
		// Enable dash hitbox
		if (player.DashHitbox != null)
		{
			player.DashHitbox.Monitoring = true;
			// Disconnect first to avoid duplicate connections, then connect
			player.DashHitbox.AreaEntered -= OnHitboxAreaEntered;
			player.DashHitbox.AreaEntered += OnHitboxAreaEntered;
			
			player.DashHitbox.BodyEntered -= OnHitboxBodyEntered;
			player.DashHitbox.BodyEntered += OnHitboxBodyEntered;
		}
		
		// Add screen shake or particle effect here
	}
	
	public override void Exit()
	{
		// Disable dash hitbox
		if (player.DashHitbox != null)
		{
			player.DashHitbox.Monitoring = false;
		}
	}
	
	public override void PhysicsUpdate(double delta)
	{
		player.DashTimer -= (float)delta;
		
		// Dash forward in facing direction
		player.Velocity = new Vector2(
			player.FacingDirection * -1 * player.DashSpeed,
			0 // No vertical movement during dash
		);
		
		player.MoveAndSlide();
		
		// Return to idle/run when dash finishes
		if (player.DashTimer <= 0)
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
				enemy.TakeDamage(1);
				GD.Print("Hit enemy with dash attack!");
				
				// Add hit effect, freeze frame, etc.
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
				enemy.TakeDamage(1);
				GD.Print("Hit enemy with dash attack!");
			}
		}
	}
}

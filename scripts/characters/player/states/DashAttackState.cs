using Godot;

/// <summary>
/// Dash attack state - King's Scepter style forward dash with hitbox
/// Bounces upward when hitting an enemy
/// </summary>
public partial class DashAttackState : State
{
	private bool hasHitEnemy = false;
	
	public override void Enter()
	{
		hasHitEnemy = false;
		
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("dash");
		
		player.DashTimer = player.DashDuration;
		player.DashCooldownTimer = player.DashCooldown;
		
		if (player.DashHitbox != null)
		{
			player.DashHitbox.Monitoring = true;
			player.DashHitbox.AreaEntered -= OnHitboxAreaEntered;
			player.DashHitbox.AreaEntered += OnHitboxAreaEntered;
			
			player.DashHitbox.BodyEntered -= OnHitboxBodyEntered;
			player.DashHitbox.BodyEntered += OnHitboxBodyEntered;
		}
	}
	
	public override void Exit()
	{
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
			player.Velocity.Y // Keep Y velocity for bounce
		);
		
		// Apply gravity
		if (!hasHitEnemy)
		{
			player.Velocity = new Vector2(
				player.Velocity.X,
				player.Velocity.Y + player.Gravity * (float)delta
			);
		}
		
		player.MoveAndSlide();
		
		// If hit enemy, transition to jump state to handle air control
		if (hasHitEnemy)
		{
			player.StateMachine.TransitionTo("JumpState");
			return;
		}
		
		// Return to idle/run when dash finishes
		if (player.DashTimer <= 0)
		{
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
			}
			else
			{
				player.StateMachine.TransitionTo("JumpState");
			}
		}
	}
	
	private void OnHitboxAreaEntered(Area2D area)
	{
		if (area.IsInGroup("enemy_hurtbox"))
		{
			if (area.Owner is Enemy enemy)
			{
				enemy.TakeDamage(1, player.GlobalPosition);
				GD.Print("Hit enemy with dash attack!");
				
				// Bounce player upward
				player.OnDashHitEnemy();
				hasHitEnemy = true;
				
				// Add hit effect, freeze frame, etc.
				HitFreeze();
			}
		}
	}
	
	private void OnHitboxBodyEntered(Node2D body)
	{
		if (body.IsInGroup("enemy"))
		{
			if (body is Enemy enemy)
			{
				enemy.TakeDamage(1, player.GlobalPosition);
				GD.Print("Hit enemy with dash attack!");
				
				player.OnDashHitEnemy();
				hasHitEnemy = true;
				
				HitFreeze();
			}
		}
	}
	
	private void HitFreeze()
	{
		// Brief freeze frame for impact
		Engine.TimeScale = 0.1f;
		player.GetTree().CreateTimer(0.05f, true, false, true).Timeout += () =>
		{
			Engine.TimeScale = 1.0f;
		};
	}
}

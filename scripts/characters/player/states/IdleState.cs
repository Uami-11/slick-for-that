using Godot;

/// <summary>
/// Player idle state - standing still or slight movement
/// </summary>
public partial class IdleState : State
{
	public override void Enter()
	{
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("idle");
		
		GD.Print("Entered Idle State");
	}
	
	public override void PhysicsUpdate(double delta)
	{
		// DEBUG: Print input direction every frame
		GD.Print($"InputDirection: {player.InputDirection}, X: {player.InputDirection.X}");
		
		// Apply gravity
		if (!player.IsOnFloor())
		{
			player.Velocity = new Vector2(
				player.Velocity.X,
				player.Velocity.Y + player.Gravity * (float)delta
			);
		}
		
		// Apply friction
		player.Velocity = new Vector2(
			Mathf.MoveToward(player.Velocity.X, 0, player.Friction * (float)delta),
			player.Velocity.Y
		);
		
		player.MoveAndSlide();
		
		// Check for state transitions
		if (player.InputDirection.X != 0)
		{
			GD.Print("Trying to transition to RunState!");
			player.StateMachine.TransitionTo("RunState");
		}
		
		// Handle jump input
		if (Input.IsActionJustPressed("jump"))
		{
			GD.Print("Jump pressed!");
			if (player.TryJump())
			{
				player.StateMachine.TransitionTo("JumpState");
			}
			else
			{
				player.BufferJump();
			}
		}
		
		if (Input.IsActionJustPressed("dash") && player.DashCooldownTimer <= 0)
		{
			GD.Print("Dash pressed!");
			player.StateMachine.TransitionTo("DashAttackState");
		}
		
		if (Input.IsActionJustPressed("slide") && player.SlideCooldownTimer <= 0)
		{
			GD.Print("Slide pressed!");
			player.StateMachine.TransitionTo("SlideState");
		}
	}
}

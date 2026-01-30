using Godot;

/// <summary>
/// Player running state
/// </summary>
public partial class RunState : State
{
	public override void Enter()
	{
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("run");
		
		GD.Print("Entered Run State");
	}
	
	public override void PhysicsUpdate(double delta)
	{
		// Apply gravity
		if (!player.IsOnFloor())
		{
			player.Velocity = new Vector2(
				player.Velocity.X,
				player.Velocity.Y + player.Gravity * (float)delta
			);
		}
		
		// Apply horizontal movement
		if (player.InputDirection.X != 0)
		{
			player.Velocity = new Vector2(
				Mathf.MoveToward(player.Velocity.X, player.InputDirection.X * player.CurrentSpeed, player.Acceleration * (float)delta),
				player.Velocity.Y
			);
			
			// DEBUG: Print velocity
			//GD.Print($"Setting velocity to: {player.Velocity}, CurrentSpeed: {player.CurrentSpeed}");
			
			// Flip sprite based on direction
			if (player.AnimatedSprite != null)
			{
				player.AnimatedSprite.FlipH = player.FacingDirection < 0;
			}
		}
		else
		{
			// Apply friction if no input
			player.Velocity = new Vector2(
				Mathf.MoveToward(player.Velocity.X, 0, player.Friction * (float)delta),
				player.Velocity.Y
			);
		}
		
		// DEBUG: Print before and after MoveAndSlide
		var posBefore = player.Position;
		player.MoveAndSlide();
		var posAfter = player.Position;
		
		//GD.Print($"Position before: {posBefore}, after: {posAfter}, IsOnFloor: {player.IsOnFloor()}");
		
		// Check for state transitions
		if (player.InputDirection.X == 0 && Mathf.Abs(player.Velocity.X) < 10f)
		{
			player.StateMachine.TransitionTo("IdleState");
		}
		
		// Handle jump input
		if (Input.IsActionJustPressed("jump"))
		{
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
			player.StateMachine.TransitionTo("DashAttackState");
		}
		
		if (Input.IsActionJustPressed("slide") && player.SlideCooldownTimer <= 0)
		{
			player.StateMachine.TransitionTo("SlideState");
		}
	}
}

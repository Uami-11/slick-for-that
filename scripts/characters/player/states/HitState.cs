using Godot;

/// <summary>
/// Hit state - player is knocked back after taking damage
/// </summary>
public partial class HitState : State
{
	public override void Enter()
	{
		if (player.AnimatedSprite != null)
			player.AnimatedSprite.Play("hit");
		
		GD.Print("Player hit!");
	}
	
	public override void PhysicsUpdate(double delta)
	{
		// Knockback is handled in PlayerController._PhysicsProcess
		// This state just waits for knockback to finish
		
		if (!player.IsKnockedBack)
		{
			// Knockback finished, return to normal state
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
}

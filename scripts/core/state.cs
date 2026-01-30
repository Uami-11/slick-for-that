using Godot;

/// <summary>
/// Base class for all player states
/// </summary>
public abstract partial class State : Node
{
	protected PlayerController player;
	
	public virtual void Enter()
	{
		// Called when entering this state
	}
	
	public virtual void Exit()
	{
		// Called when exiting this state
	}
	
	public virtual void Update(double delta)
	{
		// Called every frame
	}
	
	public virtual void PhysicsUpdate(double delta)
	{
		// Called every physics frame
	}
	
	public virtual void HandleInput(InputEvent @event)
	{
		// Called when input events occur
	}
	
	public void SetPlayer(PlayerController playerController)
	{
		player = playerController;
	}
}

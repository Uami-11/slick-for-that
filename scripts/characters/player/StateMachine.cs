using Godot;
using System.Collections.Generic;

/// <summary>
/// Manages state transitions for the player
/// </summary>
public partial class StateMachine : Node
{
	[Export] public State InitialState;
	
	private Dictionary<string, State> states = new Dictionary<string, State>();
	private State currentState;
	
	public override void _Ready()
	{
		// Collect all State children
		foreach (Node child in GetChildren())
		{
			if (child is State state)
			{
				states[child.Name] = state;
				state.SetPlayer(GetParent<PlayerController>());
			}
		}
		
		// Enter initial state
		if (InitialState != null)
		{
			currentState = InitialState;
			currentState.Enter();
		}
	}
	
	public override void _Process(double delta)
	{
		if (currentState != null)
		{
			currentState.Update(delta);
		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (currentState != null)
		{
			currentState.PhysicsUpdate(delta);
		}
	}
	
	public override void _Input(InputEvent @event)
	{
		if (currentState != null)
		{
			currentState.HandleInput(@event);
		}
	}
	
	/// <summary>
	/// Transition to a new state by name
	/// </summary>
	public void TransitionTo(string stateName)
	{
		if (!states.ContainsKey(stateName))
		{
			GD.PrintErr($"State '{stateName}' not found!");
			return;
		}
		
		if (currentState != null)
		{
			currentState.Exit();
		}
		
		currentState = states[stateName];
		currentState.Enter();
		
		GD.Print($"Transitioned to: {stateName}");
	}
	
	public string GetCurrentStateName()
	{
		return currentState?.Name ?? "None";
	}
}

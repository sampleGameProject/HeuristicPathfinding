using System;
using System.Collections.Generic;
using UnityEngine;

public class AStar
{
	public interface IState
	{
		void foo();
	}
	
	class State : IState, IComparable<State>
	{
		public int id, g;
		public float f;
		public IState prevState;
		
		#region IState implementation
		
		public void foo ()
		{
			throw new NotImplementedException ();
		}
		
		#endregion
		
		#region IComparable implementation
		public int CompareTo(State other)
		{
			if (other.f > this.f)
				return -1;
			else if (other.f == this.f)
				return 0;
			else
				return 1;
		}
		#endregion
	}
	
	public event EventHandler OnSearchFail;
	public event EventHandler OnSearchCompleted;
	public event EventHandler OnNextStepProcessed;
	
	NodeManager nodeManager;
	int goalId;
	Func<int,int,float> heuristic;
	
	List<State> openSet = new List<State>();
	List<State> closedSet = new List<State>();
	State current;
	
	public int[] GetCurrentPath()
	{
		List<int> path = new List<int>();
		
		for (var state = current; state != null; state = (State)state.prevState)
			path.Add(state.id);
		
		path.Reverse();
		return path.ToArray();
	}
	
	public void Start(NodeManager nodeManager, Func<int,int,float> heuristic)
	{
		this.nodeManager = nodeManager;
		this.goalId = nodeManager.FinishNodeId;
		this.heuristic = heuristic;
		
		var startState = new State(){id = nodeManager.StartNodeId};
		openSet.Add(startState);
	}
	
	public void NextStep()
	{
		Debug.Log("Astar : NextStep()");
		
		current = GetMinState();
		Debug.Log(string.Format("Current state :  id = {0} , f = {1}, g = {2}",current.id, current.f, current.g) );
		
		if(current.id == goalId)
		{
			OnSearchCompleted.Notify();
			return;
		}
		
		openSet.Remove(current);
		closedSet.Add(current);
		
		RoadData[] roads = nodeManager.GetRoadsForNode(current.id);
		
		var nextStates = new List<State>();
		
		foreach(var road in roads)
		{
			nextStates.Add(new State() {id = road.To.Id, g = road.Weight + current.g });
		}
		
		foreach(var next in nextStates)
		{
			if (ContainsState(closedSet,next))
				continue;
			
			int tentativeG = current.g + nodeManager.GetRoadWeight(NodeManager.GetRoadHash(current.id,next.id));
			
			bool tentativeIsBetter = false;
			
			var nextFromOpenSet = GetStateFromOpen(next);
			
			if(nextFromOpenSet == null)
			{
				openSet.Add(next);
				tentativeIsBetter = true;
				nextFromOpenSet = next;
			}
			else
			{
				if(tentativeG < nextFromOpenSet.g)
					tentativeIsBetter = true;
			}
			
			if(tentativeIsBetter)
			{
				nextFromOpenSet.prevState = current;
				nextFromOpenSet.g = tentativeG;
				nextFromOpenSet.f = tentativeG + heuristic(next.id, goalId);					
			}
		}
		
		if(openSet.Count == 0)
		{
			OnSearchFail.Notify();
			return;
		}
		
		openSet.Sort();
		OnNextStepProcessed.Notify();
	}
	
	State GetMinState ()
	{
		State min = openSet[0];
		
		foreach(var s in openSet)
		{
			if(min.f > s.f)
				min = s;
		}
		
		return min;
	}
	
	bool ContainsState(List<State> states, State state)
	{
		foreach(var s in states)
		{
			if(s.id == state.id)
				return true;
		}
		return false;
	}
	
	State GetStateFromOpen(State s)
	{
		foreach(var state in openSet)
		{
			if(state.id == s.id)
				return state;
		}
		return null;
	}
}
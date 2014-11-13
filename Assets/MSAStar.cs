using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class Problem
{
	public int Start {get; private set;}
	public int Goal {get; private set;}

	public Problem(int start, int goal) // теперь можно создавать проблемы на пустом месте :trf:
	{
		Start = start;
		Goal = goal;
	}

	public bool IsGoal(int nodeId)
	{
		return Goal == nodeId;
	}
}

public class SMAStar
{
	struct SuccessorData
	{
		public int nodeId;
		public bool isRevealed;
	}

	const float INFINITY = float.MaxValue;

	List<State> queue;

	Dictionary<int,int> forgottenSuccessorsValues = new Dictionary<int, int>();
	Dictionary<int,int> fValues = new Dictionary<int, int>();

	Dictionary<int,SuccessorData[]> successors = new Dictionary<int, SuccessorData[]>();

	object Search(Problem problem)
	{
		queue.Insert(problem.Start);

		while(true)
		{
			if(queue.Count == 0)
				return null;//failture
		

			var node = queue.First();

			if(problem.IsGoal(node))
				return ReconstructPath(); //success


			if(!successors.ContainsKey(node.Id))
			{
				FindAllSuccessorsAndAddToDictionaty(node.Id);
			}

			var s = GetNextSuccessor(node);

			if(!problem.IsGoal(s) && s.Depth == MAX_DEPTH)
			{
				s.F = INFINITY;
			}
			else
			{
				s.F = Mathf.Max(node.F, s.G + s.H);
			}

			if(NoMoreSuccessors(node))
			{
				UpdateNodesFCost();
			}

			if(AllNodeSuccessorsAreEnqueued(node))
			{
				queue.Remove(node);
			}

			if(MemoryIsFull())
			{
				var badNode = GetShallowestNodeWithHightestFCost();

				var badNodeParents = GetNodeParents(badNode);

				foreach(var parent in badNodeParents)
				{
					parent.successors.Remove(badNode);
					if(needed)
						queue.Insert(parent);
				}
			}

			queue.Insert(parent);
		}

	}

	object GetNextSuccessor (object node)
	{
		throw new System.NotImplementedException ();
	}

	void FindAllSuccessorsAndAddToDictionaty (int id)
	{
		throw new System.NotImplementedException ();
	}

}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;


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
	const float INFINITY = float.MaxValue;
	const int NO_PARENT = -1;

	class State
	{
		public int depth, fs;
		public float f, g;
		public int nodeId, prevId;
	
		public bool IsEqual(State other)
		{
			return nodeId == other.nodeId && prevId == other.prevId;
		}
	}

	List<State> nodes = new List<State>();

	int maxDepth;
	int maxSize;
	NodeManager manager;
	Problem problem;

	public static void Test ()
	{
		NodeData[] nodes = new NodeData[]
		{
			new NodeData(){Id = 0, Position = new Vector3(0,0,0)/5},

			new NodeData(){Id = 1, Position = new Vector3(30,40,0)/5},

			new NodeData(){Id = 2, Position = new Vector3(50,60,0)/5},
			new NodeData(){Id = 3, Position = new Vector3(80,60,0)/5},

			new NodeData(){Id = 4, Position = new Vector3(130,0,0)/5},

			new NodeData(){Id = 5, Position = new Vector3(30,-40,0)/5},

			new NodeData(){Id = 6, Position = new Vector3(50,-60,0)/5},
			new NodeData(){Id = 7, Position = new Vector3(80,-60,0)/5}
		};
		
		int size = nodes.GetLength(0);
		
		int[,] incs = new int[size,size];
		
		for(int i = 0; i < size; i++)
			for(int j = 0; j < size; j++)
				incs[i,j] = -1;
		
		incs[0,1] = 10;
		incs[0,5] = 8;

		incs[1,2] = 10;
		incs[1,4] = 10;
		incs[2,3] = 10;
		incs[2,4] = 10;

		incs[5,4] = 16;
		incs[5,6] = 8;
		incs[6,7] = 8;
		incs[6,4] = 8;
		
		NodeManager nodeManager = new NodeManager(nodes,incs);

		var problem = new Problem(0,4);

		var sma = new SMAStar(nodeManager,problem,3,3,(a,b) =>
      {
			var posA = nodeManager.nodes[a].Position;
			var posB = nodeManager.nodes[b].Position;
			return Vector3.Distance(posA,posB);
		});

		while(sma.NextStep() != null)
		{
			Debug.Log("Do one more step...");
		}

	}

	Func<int,int,float> heuristic;

	public SMAStar(NodeManager manager, Problem problem , int maxSize, int maxDepth,Func<int,int,float> h)
	{
		this.heuristic = h;
		this.manager = manager;
		this.maxDepth = maxDepth;
		this.maxSize = maxSize;
		this.problem = problem;

		nodes.Add(new State()
		{ 	
			nodeId 	= problem.Start, 
			prevId = -1,
			f 		= H(problem.Start),
			depth = 0
		});
	}

	object NextStep()
	{
		if(QueueIsEmpty())
		{
			return null;//failture
		}
			
		State nodeState = GetDeepestLowestFCostNode();

		if(problem.IsGoal(nodeState.nodeId))
			return ReconstructPath(); //success

		var s = GetNextSuccessor(nodeState);

		if(!problem.IsGoal(s.nodeId) && s.depth == maxDepth - 1)
		{
			s.f = INFINITY;
		}
		else
		{
			s.f = Mathf.Max(nodeState.f, G(s) + H(s.nodeId));
		}

		if(NoMoreSuccessors(nodeState))
		{
			UpdateNodeFCost(nodeState);
		}

//		if(AllNodeSuccessorsAreEnqueued(nodeState))
//		{
//			nodes.Remove(nodeState);
//		}

		if(MemoryIsFull())
		{
			var badNode = GetShallowestNodeWithHightestFCost();
			nodes.Remove(badNode);

//			State[] badNodeParents = GetNodeParents(badNode);
//
//			foreach(var parent in badNodeParents)
//			{
//				parent.successors.Remove(badNode);
//
//				if(parent.fs > badNode.f)
//				{
//					parent.fs = badNode.f;
//				}
//
//				if(needed)
//					nodes.Add(parent);
//			}
		}

		nodes.Add(s);

		return nodes;
	}

	bool MemoryIsFull()
	{
		return nodes.Count == maxSize;
	}

	float H (int nodeId)
	{
		return heuristic(nodeId,problem.Goal);
	}

	float G (State s)
	{
		return manager.GetRoadWeight(NodeManager.GetRoadHash(s.nodeId,s.prevId));
	}

	bool QueueIsEmpty ()
	{
		return nodes.Count == 0;
	}

	State GetDeepestLowestFCostNode ()
	{
		var ret = nodes[0];
		for(int i = 1; i < nodes.Count; i++)
		{
			var cur = nodes[i];

			if(ret.depth < cur.depth)
			{
				ret = cur;
			}
			else if(ret.depth == cur.depth && ret.f >= cur.f)
			{
				ret = cur;
			}
		}

//		nodes.Remove(ret);
		return ret;
	}

	State GetNextSuccessor (State nodeState)
	{
		int stateNodeId = nodeState.nodeId;

		var roads = manager.GetRoadsForNode(stateNodeId);

		foreach(var road in roads)
		{
			if(nodeState.prevId == road.To.Id)
				continue;

			if(!AlreadyInNodes(stateNodeId,road.To.Id))
			{
				return new State()
				{
					depth = nodeState.depth + 1,
					prevId = stateNodeId,
					nodeId = road.To.Id
				};
			}
		}

		return null;
	}

	bool AlreadyInNodes (int nodeId, int nodeToId)
	{
		foreach(var node in nodes)
		{
			if(node.prevId == nodeId && node.nodeId == nodeToId)
				return true;
		}

		return false;
	}

	bool NoMoreSuccessors (State nodeState)
	{
		return GetNextSuccessor(nodeState) == null;
	}

	void UpdateNodeFCost (State nodeState)
	{
		float minF = float.MaxValue;

		foreach(var node in nodes)
		{
			if(node == nodeState)
				continue;

			if(node.f < minF)
				node.f = minF;
		}

		nodeState.f = minF;
	}

	bool AllNodeSuccessorsAreEnqueued (object node)
	{
		throw new System.NotImplementedException ();
	}

	State GetShallowestNodeWithHightestFCost ()
	{
		var ret = nodes[0];
		for(int i = 1; i < nodes.Count; i++)
		{
			var cur = nodes[i];
			
			if(ret.depth>= cur.depth&& ret.f <= cur.f)
				ret = cur;
		}
		
		return ret;
	}

//	State[] GetNodeParents (State badNode)
//	{
//		throw new System.NotImplementedException ();
//	}

	object ReconstructPath ()
	{
		Debug.Log("ReconstructPath()");
		throw new System.NotImplementedException ();
	}

}

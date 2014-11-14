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
	const float INFINITY = float.MaxValue;
	const int NO_PARENT = -1;

	class State
	{
		public int depth, f, g, fs, id, prevId;
		public int nodeId;
	}

//	List<int> nodesList = new List<int>();
//
//	Dictionary<int,int> fs = new Dictionary<int, int>();
//	Dictionary<int,int> f = new Dictionary<int, int>();
//	Dictionary<int,int> depth = new Dictionary<int, int>();
//
//	Dictionary<int,List<int>> successors = new Dictionary<int,List<int>>();


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
	}

	public SMAStar(NodeManager manager, Problem problem , int maxSize, int maxDepth)
	{
		this.manager = manager;
		this.maxDepth = maxDepth;
		this.maxSize = maxSize;
		this.problem = problem;

//		PrepareData();

		nodes.Add(new State()
		{ 	
			id 		= 0, 
			nodeId 	= problem.Start, 
			f 		= H(problem.Start)
		});
	}

//	void PrepareData ()
//	{
//		//TODO: setup all dictionaries
//	}

	object NextStep()
	{
		if(QueueIsEmpty())
			return null;//failture

		State nodeState = PopDeepestLowestFCostNode();

		if(problem.IsGoal(nodeState.nodeId))
			return ReconstructPath(); //success

		var s = GetNextSuccessor(nodeState);

		if(!problem.IsGoal(s) && s.depth == maxDepth - 1)
		{
			s.f = INFINITY;
		}
		else
		{
			s.f = Mathf.Max(nodeState.f, G(s) + H(s));
		}

		if(NoMoreSuccessors(nodeState))
		{
			UpdateNodeFCost(nodeState);
		}

		if(AllNodeSuccessorsAreEnqueued(nodeState))
		{
			nodes.Remove(nodeState);
		}

		if(MemoryIsFull())
		{
			var badNode = GetShallowestNodeWithHightestFCost();

			State[] badNodeParents = GetNodeParents(badNode);

			foreach(var parent in badNodeParents)
			{
				parent.successors.Remove(badNode);

				if(parent.fs > badNode.f)
				{
					parent.fs = badNode.f;
				}

				if(needed)
					nodes.Add(parent);
			}
		}

		nodes.Add(s);
	}

//	object NextStep()
//	{
//		
//
//		while(true)
//		{
//			if(QueueIsEmpty())
//				return null;//failture
//		
//			int node = PopDeepestLowestFCostNode();
//
//			if(problem.IsGoal(node))
//				return ReconstructPath(); //success
//
//			int s = GetNextSuccessor(node);
//
//			if(!problem.IsGoal(s) && depth[s] == maxDepth-1)
//			{
//				f[s] = INFINITY;
//			}
//			else
//			{
//				f[s] = Mathf.Max(f[node], G(s) + H(s));
//			}
//
//			if(NoMoreSuccessors(node))
//			{
//				UpdateNodeFCost(node);
//			}
//
//			if(AllNodeSuccessorsAreEnqueued(node))
//			{
//				nodesList.Remove(node);
//			}
//
//			if(MemoryIsFull())
//			{
//				var badNode = GetShallowestNodeWithHightestFCost();
//
//				int[] badNodeParents = GetNodeParents(badNode);
//
//				foreach(var parent in badNodeParents)
//				{
//					successors[parent].Remove(badNode);
//
//					if(fs[parent] > f[badNode])
//					{
//						fs[parent] = f[badNode];
//					}
//
//					if(needed)
//						nodesList.Add(parent);
//				}
//			}
//
//			nodesList.Add(s);
//		}
//
//	}
//
	bool MemoryIsFull()
	{
		return nodes.Count == maxSize;
	}
//
//
//	int GetNextSuccessor (int node)
//	{
//		for(int i = 0; i < successors[node].Count; i++)
//		{
//			var s = successors[node];
//			if(!nodesList.Contains(s))
//			{
//				// setup deep
//				depth[s] = depth[node] + 1;
//				return s;
//			}
//		}
//		
//		return -1;
//	}
//
//	bool NoMoreSuccessors (int node)
//	{
//		for(int i = 0; i < successors[node].Count; i++)
//		{
//			if(!nodesList.Contains(successors[node]))
//				return false;
//		}
//
//		return true;
//	}
//
//	
//	bool AllNodeSuccessorsAreEnqueued (int node)
//	{
//		return !NoMoreSuccessors(node);
//	}
//
//	int PopDeepestLowestFCostNode ()
//	{
//		int ret = nodesList[0];
//		for(int i = 1; i < nodesList.Count; i++)
//		{
//			var cur = nodesList[i];
//
//			if(depth[ret] <= depth[cur] && f[ret] >= f[cur])
//				ret = cur;
//		}
//
//		nodesList.Remove(ret);
//		return ret;
//	}
//
//	bool QueueIsEmpty ()
//	{
//		return nodesList.Count == 0;
//	}
//
//	object ReconstructPath ()
//	{
//		throw new System.NotImplementedException ();
//	}
//
//
//	int GetShallowestNodeWithHightestFCost()
//	{
//		int ret = nodesList[0];
//		for(int i = 1; i < nodesList.Count; i++)
//		{
//			var cur = nodesList[i];
//			
//			if(depth[ret] >= depth[cur] && f[ret] <= f[cur])
//				ret = cur;
//		}
//		
//		return ret;
//	}
//
//	int[] GetNodeParents (int badNode)
//	{
//		List<int> parents = new List<int>();
//
//		foreach(var node in manager.nodes)
//		{
//			if(successors[node].Contains(badNode))
//				parents.Add(node);
//		}
//
//		return parents.ToArray();
//	}
//
//	void UpdateNodeFCost (int node)
//	{
//		float minSuccessorF = float.MaxValue;
//
//		foreach(var s in successors[node])
//		{
//			if(f[s] < minSuccessorF)
//			{
//				minSuccessorF = f[s];
//			}
//		}
//		f[node] = minSuccessorF;
//	}
//
//	float G (int s)
//	{
//		throw new System.NotImplementedException ();
//	}
//
//	float H (int s)
//	{
//		throw new System.NotImplementedException ();
//	}

	float H (int nodeId)
	{
		throw new System.NotImplementedException ();
	}

	float G (State s)
	{
		throw new System.NotImplementedException ();
	}

	bool QueueIsEmpty ()
	{
		return nodes.Count == 0;
	}

	State PopDeepestLowestFCostNode ()
	{
		var ret = nodes[0];
		for(int i = 1; i < nodes.Count; i++)
		{
			var cur = nodes[i];

			if(ret.depth <= cur.depth && ret.f >= cur.f)
				ret = cur;
		}

		nodes.Remove(ret);
		return ret;
	}

	State GetNextSuccessor (State nodeState)
	{
		int stateNodeId = nodeState.nodeId;

		var roads = manager.GetRoadsForNode(stateNodeId);

		foreach(var road in roads)
		{
			if(!AlreadyInNodes(road.To.Id,))
			{
				return new State()
				{
					depth = nodeState.depth + 1,
					nodeId = road.To
				};
			}
		}
	}

	bool NoMoreSuccessors (State nodeState)
	{
		throw new System.NotImplementedException ();
	}

	void UpdateNodeFCost (State nodeState)
	{
		throw new System.NotImplementedException ();
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

	State[] GetNodeParents (State badNode)
	{
		throw new System.NotImplementedException ();
	}

}

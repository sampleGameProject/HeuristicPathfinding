using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public static class ColorHelper
{
	public static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
	{
		// ######################################################################
		// T. Nathan Mundhenk
		// mundhenk@usc.edu
		// C/C++ Macro HSV to RGB
		
		double H = h;
		while (H < 0) { H += 360; };
		while (H >= 360) { H -= 360; };
		double R, G, B;
		if (V <= 0)
		{ R = G = B = 0; }
		else if (S <= 0)
		{
			R = G = B = V;
		}
		else
		{
			float hf = (float)(H / 60.0);
			int i = (int)Mathf.Floor(hf);
			double f = hf - i;
			double pv = V * (1 - S);
			double qv = V * (1 - S * f);
			double tv = V * (1 - S * (1 - f));
			switch (i)
			{
				
				// Red is the dominant color
				
			case 0:
				R = V;
				G = tv;
				B = pv;
				break;
				
				// Green is the dominant color
				
			case 1:
				R = qv;
				G = V;
				B = pv;
				break;
			case 2:
				R = pv;
				G = V;
				B = tv;
				break;
				
				// Blue is the dominant color
				
			case 3:
				R = pv;
				G = qv;
				B = V;
				break;
			case 4:
				R = tv;
				G = pv;
				B = V;
				break;
				
				// Red is the dominant color
				
			case 5:
				R = V;
				G = pv;
				B = qv;
				break;
				
				// Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.
				
			case 6:
				R = V;
				G = tv;
				B = pv;
				break;
			case -1:
				R = V;
				G = pv;
				B = qv;
				break;
				
				// The color is not defined, we should throw an error.
				
			default:
				//LFATAL("i Value error in Pixel conversion, Value is %d", i);
				R = G = B = V; // Just pretend its black/white
				break;
			}
		}
		r = Clamp((int)(R * 255.0));
		g = Clamp((int)(G * 255.0));
		b = Clamp((int)(B * 255.0));
	}
	
	/// <summary>
	/// Clamp a value to 0-255
	/// </summary>
	static int Clamp(int i)
	{
		if (i < 0) return 0;
		if (i > 255) return 255;
		return i;
	}
}

public struct NodeData
{
	public Vector3 Position {get;set;}
	public int Id {get;set;}
}

public struct RoadData
{
	public NodeData To {get;set;}
	public NodeData From {get;set;}
	public int Weight {get;set;}
}

public class NodeManager
{
	public const int MaxWeight = 100;

	public delegate void RoadWeightChangeDelegate(long roadHash, int weight);
	public event RoadWeightChangeDelegate OnRoadWeightChanged;

	public Dictionary<int,NodeData> nodes; 
	public int[,] incidenceMatrix;

	public NodeManager(NodeData[] nodes,int[,] incidenceMatrix)
	{
		this.nodes = new Dictionary<int,NodeData>();

		foreach(var node in nodes)
			this.nodes.Add(node.Id,node);

		this.incidenceMatrix = incidenceMatrix;
	}

	
	public RoadData[] GetRoadsForNode(NodeData node)
	{
		return GetRoadsForNode(node.Id);
	}

	public RoadData[] GetRoadsForNode(int nodeId)
	{
		List<RoadData> ret = new List<RoadData>();
		
		for(int i = 0; i < incidenceMatrix.GetLength(0); i++)
		{
			if(incidenceMatrix[nodeId,i] != -1) // road is exists
			{
				ret.Add(new RoadData()
		        {
					From = nodes[nodeId],
					To = nodes[i],
					Weight = incidenceMatrix[nodeId,i]
				});
			}
			else
			{
				if(incidenceMatrix[i,nodeId] != -1) // road is exists
				{
					ret.Add(new RoadData()
					        {
						From = nodes[nodeId],
						To = nodes[i],
						Weight = incidenceMatrix[i,nodeId]
					});
				}
			}
		}
		
		return ret.ToArray();
	}

	public RoadData[] GetAllRoads()
	{
		List<RoadData> ret = new List<RoadData>();

		for(int i = 0; i < incidenceMatrix.GetLength(0); i++)
			for(int j = i+1; j < incidenceMatrix.GetLength(0); j++)
			{
				if(incidenceMatrix[i,j] == -1)
					continue;

				ret.Add(new RoadData()
		        {
					From = nodes[i],
					To = nodes[j],
					Weight = incidenceMatrix[i,j]
				});
			}		
		return ret.ToArray();
	}

	public static long GetRoadHash(int i, int j)
	{
		return (long)(((ulong)i) << 32 | ((ulong)j));
	}

	public int GetRoadWeight (long roadHash)
	{
		int i = (int)(roadHash >> 32);
		int j = (int)roadHash;

		var weight = incidenceMatrix[i,j];

		if(weight == -1)
			weight = incidenceMatrix[j,i];

		return weight;
	}

	public void SetRoadWeight (long roadHash, float val)
	{
		int i = (int)(roadHash >> 32);
		int j = (int)roadHash;
		
		incidenceMatrix[i,j] = (int)val;

		if(OnRoadWeightChanged != null)
			OnRoadWeightChanged(roadHash, incidenceMatrix[i,j]);
	}

	public static float RelativeWeigth(int weight)
	{
		return (float)weight / (float)MaxWeight;
	}
}

static class EventHandlerExtenstions
{
	public static void Notify(this EventHandler handler)
	{
		if(handler != null)
			handler(null,EventArgs.Empty);	
	}
}

class AStar
{
	public interface IState
	{
		void foo();
	}

	class State : IState
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

	public void Start(NodeManager nodeManager, int startId, int goalId, Func<int,int,float> heuristic)
	{
		this.nodeManager = nodeManager;
		this.goalId = goalId;
		this.heuristic = heuristic;

		var startState = new State(){id = startId, f = heuristic(startId,goalId), g = 0};
		openSet.Add(startState);
	}

	public void NextStep()
	{
		current = GetMinState();

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
			if(IsAvailableState(current, road.To))
				nextStates.Add(new State() {id = road.To.Id, g = road.Weight + current.g, prevState = current });
		}

		foreach(var next in nextStates)
		{
			if (ContainsState(closedSet,next))
				continue;

			int g = current.g + nodeManager.GetRoadWeight(NodeManager.GetRoadHash(current.id,next.id));

			bool inOpenSet = ContainsState(openSet, next);

			if (!inOpenSet || g < next.g)
			{
				next.prevState = current;
				next.g = g;
				next.f = g + heuristic(next.id, goalId);
				
				if (!inOpenSet)
					openSet.Add(next);
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

	bool IsAvailableState (State current, NodeData to)
	{
		if (current.prevState == null)
			return true;
		
		for (State state = current.prevState as State; state != null; state = (State)state.prevState)
		{
			if (state.id == to.Id)
				return false;
		}
		return true;
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

}

public class World : MonoBehaviour 
{
	public GameObject nodePrefab;
	public GameObject roadPrefab;

	public GUIManager guiManager;
	public NodeManager nodeManager;

	Dictionary<int,GameObject> nodesObjects;
	Dictionary<long,LineRenderer> lineRenderers;

	public delegate void WorldModeChangedDelegate(bool isPresetMode);
	public event WorldModeChangedDelegate OnWorldModeChanged;

	bool isPresetMode = true;

	void Awake () 
	{
		Load();		
		CreateNodesGameObjects();
		CreateRoadsGameObjects();
	}

	void Load ()
	{
		//TODO: read from file code here

		NodeData[] nodes = new NodeData[]
		{
			new NodeData(){Id = 0, Position = new Vector3(-100,30,0)},
			new NodeData(){Id = 1, Position = new Vector3(50,10,0)},
			new NodeData(){Id = 2, Position = new Vector3(90,60,0)},
			new NodeData(){Id = 3, Position = new Vector3(50,200,0)},
			new NodeData(){Id = 4, Position = new Vector3(60,60,0)},
			new NodeData(){Id = 5, Position = new Vector3(10,90,0)},
			new NodeData(){Id = 6, Position = new Vector3(160,150,0)}
		};

		int size = nodes.GetLength(0);

		int[,] incs = new int[size,size];

		for(int i = 0; i < size; i++)
			for(int j = 0; j < size; j++)
				incs[i,j] = -1;

		incs[0,1] = 10;
		incs[0,2] = 20;
		incs[0,3] = 50;

		incs[1,2] = 30;
		incs[1,3] = 90;

		incs[3,5] = 80;
		incs[2,4] = 5;
		incs[4,6] = 20;	
		incs[2,6] = 50;

		nodeManager = new NodeManager(nodes,incs);

		nodeManager.OnRoadWeightChanged += (roadHash, weight) =>
		{
			var color = GetRoadColor(weight);
			lineRenderers[roadHash].SetColors(color,color);
		};
	}

	void CreateNodesGameObjects()
	{
		//TODO: instatinate GO and init scripts:
		nodesObjects = new Dictionary<int,GameObject>();

		var nodesArray = nodeManager.nodes.Values.ToArray();

		float width = guiManager.worldWidth;
		float height = guiManager.worldHeight - 1;

		Vector3 min = new Vector3(){x = float.MaxValue, y = float.MaxValue};
		Vector3 max = new Vector3(){x = float.MinValue, y = float.MinValue};

		foreach(var nodeData in nodesArray)
		{
			float x = nodeData.Position.x;
			float y = nodeData.Position.y;

			if(x > max.x)
				max.x = x;
			if(y > max.y)
				max.y = y;

			if(x < min.x)
				min.x = x;
			if(y < min.y)
				min.y = y;
		}

		float scaleX = width/(max.x - min.x);
		float scaleY = height/(max.y - min.y);

		foreach(var nodeData in nodesArray)
		{
			var go = (GameObject)GameObject.Instantiate(nodePrefab);
			nodesObjects.Add(nodeData.Id,go);
			go.transform.parent = transform;

			var pos = new Vector3();
			pos.x = 0.9f * ((nodeData.Position.x - min.x) * scaleX - width/2); 
			pos.y = 0.9f * ((nodeData.Position.y - min.y) * scaleY - height/2);
			go.transform.position = pos;
		}
	}


	void CreateRoadsGameObjects ()
	{
		lineRenderers = new Dictionary<long,LineRenderer>();
		var allRoads = nodeManager.GetAllRoads();

		foreach(var wayData in allRoads)
		{
			var go = (GameObject)GameObject.Instantiate(roadPrefab);
			go.transform.parent = transform;
			var lineRenderer = go.GetComponent<LineRenderer>();

			var idFrom = wayData.From.Id;
			var idTo = wayData.To.Id;

			var posFrom = nodesObjects[idFrom].transform.position;
			var posTo = nodesObjects[idTo].transform.position;

			lineRenderer.SetVertexCount(2);
			lineRenderer.SetWidth(0.1f,0.1f);
			lineRenderer.SetPosition(0, posFrom);
			lineRenderer.SetPosition(1, posTo);

			var color = GetRoadColor(wayData.Weight);
			lineRenderer.SetColors(color, color);

			var buttonWorldPos = (posFrom + posTo) / 2;

			var roadHash = NodeManager.GetRoadHash(idFrom, idTo);				
			lineRenderers.Add(roadHash,lineRenderer);

			guiManager.AddRoadButton(buttonWorldPos, wayData.Weight, roadHash);
		}
	}


	Color GetRoadColor (int weight)
	{
		if(weight == -1)
			return new Color(0.4f,0.4f,0.4f,0.1f); 
		else
		{
			var relativeWeight = 1.0f - NodeManager.RelativeWeigth(weight);
			int r,g,b;
			ColorHelper.HsvToRgb(120 * relativeWeight, 1, 1, out r, out g, out b);

			float rVal = ((float)r)/255;
			float gVal = ((float)g)/255;
			float bVal = ((float)b)/255;

			return new Color(rVal,gVal,bVal);
		}
	}

	// preset mode actions
	
	public void SelectStartNode()
	{
		
	}	
	
	public void SelectFinishNode()
	{
		
	}
	
	public void SelectHeuristic()
	{
		
	}

	public void StartDemonstration()
	{
		
	}
	
	
	// demonstration mode actions

	public void TogglePause()
	{

	}

	public void StopDemonstration()
	{

	}

	public void ProcessNextStep()
	{

	}
}





















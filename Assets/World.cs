using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;



static class EventHandlerExtenstions
{
	public static void Notify(this EventHandler handler)
	{
		if(handler != null)
			handler(null,EventArgs.Empty);	
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

	Color defaultColor;
	Color startColor = Color.blue;
	Color finishColor = Color.red;

	public delegate void WorldModeChangedDelegate(bool isPresetMode);
	public event WorldModeChangedDelegate OnWorldModeChanged;

	public event EventHandler OnPauseToggled;

	bool _isPresetMode = true;

	public bool IsPresetMode 
	{
		get{ return _isPresetMode;}
		set
		{
			_isPresetMode = value;

			if(OnWorldModeChanged != null)
				OnWorldModeChanged(_isPresetMode);
		}
	}

	bool _isPause;

	public bool IsPause 
	{
		get {return _isPause;}
		set
		{
			_isPause = value;
			OnPauseToggled.Notify();
		}
	}

	AStar astar;

	enum SelectionState
	{
		NONE,
		START,
		FINISH
	};

	SelectionState state = SelectionState.NONE;

	void Awake () 
	{
		defaultColor = nodePrefab.GetComponent<SpriteRenderer>().color;
		Load();		
		CreateNodesGameObjects();
		CreateRoadsGameObjects();

	}

	void Start()
	{
		TestSelectStartAndFinish();
//		StartDemonstration();
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
			new NodeData(){Id = 4, Position = new Vector3(60,100,0)},
			new NodeData(){Id = 5, Position = new Vector3(10,90,0)},
			new NodeData(){Id = 6, Position = new Vector3(160,150,0)},
			new NodeData(){Id = 7, Position = new Vector3(150,50,0)},
			new NodeData(){Id = 8, Position = new Vector3(160,30,0)}
		};

		int size = nodes.GetLength(0);

		int[,] incs = new int[size,size];

		for(int i = 0; i < size; i++)
			for(int j = 0; j < size; j++)
				incs[i,j] = -1;

		incs[0,1] = 10;
		incs[0,2] = 20;
		incs[0,3] = 50;
		incs[0,5] = 10;

		incs[1,2] = 30;
		
		incs[2,4] = 35;	
		incs[2,6] = 50;

		incs[3,5] = 80;
		incs[3,6] = 60;

		incs[4,5] = 20;	
		incs[4,6] = 20;

		incs[6,7] = 30;
		incs[6,8] = 70;
		incs[7,8] = 50;

		nodeManager = new NodeManager(nodes,incs);

		nodeManager.OnRoadWeightChanged += (roadHash, weight) =>
		{
			var color = GetRoadColor(weight);
			lineRenderers[roadHash].SetColors(color,color);
		};

		nodeManager.OnStartNodeChanged += (sender, e) => UpdateNodes(null);
		nodeManager.OnFinishNodeChanged += (sender, e) => UpdateNodes(null);
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

			var controller = go.GetComponent<NodeController>();
			controller.id = nodeData.Id;
			controller.clickCallback = OnNodeClicked;
		}
	}

	void OnNodeClicked(int id)
	{
		switch(state)
		{
			case SelectionState.START:
				nodeManager.StartNodeId = id;
				break;

			case SelectionState.FINISH:
				nodeManager.FinishNodeId = id;
				break;
		}
		
		state = SelectionState.NONE;
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

	void TestSelectStartAndFinish ()
	{
		nodeManager.StartNodeId = 0;
		nodeManager.FinishNodeId = 8;
	}

	void SetNodeColor(int nodeId, Color color)
	{
		nodesObjects[nodeId].GetComponent<SpriteRenderer>().color = color;
	}

	public void SelectStartNode()
	{
		state = SelectionState.START;
	}

	public void SelectFinishNode()
	{
		state = SelectionState.FINISH;
	}

//	public void SelectHeuristic()
//	{
//		// show selection buttons
//	}

	public void StartDemonstration()
	{
		IsPresetMode = false;
		time = 0;

		astar = new AStar();

		astar.OnNextStepProcessed += (sender, e) => 
		{
			Debug.Log("OnNextStepProcessed");
			UpdateWorldView();
		};

		astar.OnSearchCompleted += (sender, e) => 
		{
			Debug.Log("OnSearchCompleted");
			UpdateWorldView ();
			IsPresetMode = true;
			IsPause = false;
		};

		astar.OnSearchFail += (sender, e) => 
		{
			Debug.Log("OnSearchFail");
			IsPresetMode = true;
			IsPause = false;
		};

		astar.Start(nodeManager, (a,b) =>
		{
			var posA = nodeManager.nodes[a].Position;
			var posB = nodeManager.nodes[b].Position;
			return Vector3.Distance(posA,posB);
		});
	}

	void UpdateWorldView ()
	{
		// select roads at current path

		var currentPath = astar.GetCurrentPath();

		var roads = new List<long>();

		for(int i = 0; i < currentPath.Count() - 1; i++)
		{
			long hash = NodeManager.GetRoadHash(currentPath[i],currentPath[i+1]);
			roads.Add(hash);
		}

		foreach(var pair in lineRenderers)
		{
			const float defaultWidth = 0.07f;
			const float roadWidth = 0.15f;
			var color = GetRoadColor(nodeManager.GetRoadWeight(pair.Key));

			if(!roads.Contains(pair.Key))
			{
				color.a = 0.3f;
				pair.Value.SetWidth(defaultWidth,defaultWidth);
			}
			else
			{
				pair.Value.SetWidth(roadWidth,roadWidth);
			}
				
			pair.Value.SetColors(color,color);
		}

		// update nodes

		UpdateNodes(currentPath);

	}

	void UpdateNodes(int[] currentPath)
	{
		foreach(var pair in nodesObjects)
		{
			if(pair.Key == nodeManager.StartNodeId)
				SetNodeColor(pair.Key,startColor);
			else if (pair.Key == nodeManager.FinishNodeId)
				SetNodeColor(pair.Key,finishColor);
			else if(currentPath != null && currentPath.Contains(pair.Key))
			{
				SetNodeColor(pair.Key,defaultColor);
			}
			else
			{
				var color = defaultColor;
				color.a = 0.5f;
				SetNodeColor(pair.Key,color);
			}
		}

	}

	float time = 0;
	const float STEP_TIME = 1;

	void Update()
	{
		if(IsPresetMode)
			return;

		if(IsPause)
			return;

		time += Time.deltaTime;
		if(time > STEP_TIME)
		{
			astar.NextStep();
			time -= STEP_TIME;
		}
	}

	public void TogglePause()
	{
		if(!IsPresetMode)
			IsPause = !IsPause;
	}

	public void StopDemonstration()
	{
		IsPresetMode = true;
		astar = null;
	}

	public void ProcessNextStep()
	{
		astar.NextStep();
	}
}





















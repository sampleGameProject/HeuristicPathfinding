using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

struct NodeData
{
	public Vector3 Position {get;set;}
	public int Id {get;set;}
}

struct RoadData
{
	public NodeData To {get;set;}
	public NodeData From {get;set;}
	public int Weight {get;set;}
}

class NodeManager
{
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
		List<RoadData> ret = new List<RoadData>();
		
		for(int i = 0; i < incidenceMatrix.GetLength(0); i++)
		{
			if(incidenceMatrix[node.Id,i] != -1) // road is exists
			{
				ret.Add(new RoadData()
		        {
					From = nodes[node.Id],
					To = nodes[i],
					Weight = incidenceMatrix[node.Id,i]
				});
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
				ret.Add(new RoadData()
		        {
					From = nodes[i],
					To = nodes[j],
					Weight = incidenceMatrix[i,j]
				});
			}		
		return ret.ToArray();
	}

}

public class World : MonoBehaviour 
{
	public GameObject nodePrefab;
	public GameObject roadPrefab;

	NodeManager nodeManager;

	Dictionary<int,GameObject> nodesObjects;
	Dictionary<int,GameObject> roadsObjects;
	Dictionary<int,GameObject> textViews;

	void Start () 
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
			new NodeData(){Id = 0, Position = new Vector3(10,30,0)},
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
		incs[0,3] = 20;

		incs[1,2] = 30;
		incs[1,3] = 10;

//		incs[2,3] = 10;
//		incs[2,4] = 4;
//
//		incs[4,5] = 20;
//	
//		incs[2,6] = 50;

		nodeManager = new NodeManager(nodes,incs);
	}

	void CreateNodesGameObjects()
	{
		//TODO: instatinate GO and init scripts:
		nodesObjects = new Dictionary<int,GameObject>();

		var nodesArray = nodeManager.nodes.Values.ToArray();

		float width = 17;
		float height = 10;

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
		var allRoads = nodeManager.GetAllRoads();

		foreach(var wayData in allRoads)
		{
			var go = (GameObject)GameObject.Instantiate(roadPrefab);
			go.transform.parent = transform;
			var lineRenderer = go.GetComponent<LineRenderer>();

			lineRenderer.SetVertexCount(2);
			lineRenderer.SetWidth(0.1f,0.1f);
			lineRenderer.SetPosition(0, nodesObjects[wayData.From.Id].transform.position);
			lineRenderer.SetPosition(1, nodesObjects[wayData.To.Id].transform.position);

			var color = GetRoadColor(wayData.Weight);
			lineRenderer.SetColors(color, color);


		}
	}

	int GetRoadHash(int i, int j)
	{
		return i * 10000 + j;
	}

	Color GetRoadColor (int weight)
	{
		if(weight == -1)
		{
			return new Color(0.4f,0.4f,0.4f,0.1f); 
		}
		return Color.white;
	}
}





















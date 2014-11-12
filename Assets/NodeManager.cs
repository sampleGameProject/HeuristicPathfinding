using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

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
	
	int _startNodeId = 0, _finihNodeId = 0;
	
	public event EventHandler OnStartNodeChanged;
	public event EventHandler OnFinishNodeChanged;
	
	
	public int StartNodeId 
	{
		get {return _startNodeId;}
		set
		{
			_startNodeId = value;
			OnStartNodeChanged.Notify();
		}
	}
	
	public int FinishNodeId 
	{
		get {return _finihNodeId;}
		set
		{
			_finihNodeId = value;
			OnFinishNodeChanged.Notify();
		}
	}
	
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
		return (long)((((ulong)(Mathf.Min (i,j)))) << 32 | ((ulong)(Mathf.Max (i,j))));
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
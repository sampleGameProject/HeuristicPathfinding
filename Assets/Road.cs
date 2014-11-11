using UnityEngine;
using System.Collections;

public class Road : MonoBehaviour {

	public Node first, second;

	void Start () 
	{
	
	}
	
	void Update () 
	{
		if(first != null && second != null && first != second)
		{
			Debug.DrawLine (first.transform.position, second.transform.position, Color.red);
		}

	}

}

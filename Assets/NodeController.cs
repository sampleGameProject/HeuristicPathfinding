using UnityEngine;
using System.Collections;
using System;

public class NodeController : MonoBehaviour 
{
	static Vector3 hoverScale = new Vector3(2.3f, 2.3f, 2.3f);
	static Vector3 defaultScale = new Vector3(2,2,2);

	public int id;
	public Action<int> clickCallback;

	void OnMouseEnter()
	{
		transform.localScale = hoverScale;
	}

	void OnMouseExit()
	{
		transform.localScale = defaultScale;
	}

	void OnMouseDown()
	{
		Debug.Log(string.Format("Click at node : {0}",id));
		clickCallback(id);
	}
}

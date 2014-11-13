using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

public class GUIManager : MonoBehaviour 
{
	public float canvasWidth, canvasHeight, worldWidth, worldHeight;

	public GameObject buttonPrefab;
	public Slider slider;

	public long currentRoadHash;
	public World world;

	Dictionary<long, GameObject> buttons = new Dictionary<long, GameObject>();

	public Button startButton;

	public Button pauseButton;
	public Button nextStepButton;

	public GameObject presetPanel;
	public GameObject demonstationPanel;

	void Start()
	{
		slider.gameObject.SetActive(false);
		slider.onValueChanged.AddListener(OnSliderValueChanged);

		world.nodeManager.OnRoadWeightChanged += (roadHash, weight) => 
		{
			buttons[roadHash].GetComponentInChildren<Text>().text = weight.ToString();
		};

		world.OnWorldModeChanged += (isPresetMode) => 
		{
			if(!isPresetMode)
			{
				HideSlider();
				currentRoadHash = 0;
			}
				
			presetPanel.gameObject.SetActive(isPresetMode);
			demonstationPanel.gameObject.SetActive(!isPresetMode);
		};

		world.OnPauseToggled += (sender, e) => 
		{
			var text = pauseButton.GetComponentInChildren<Text>();
			if(text != null)
				text.text = world.IsPause ? PAUSE_ACTIVE : PAUSE_NOT_ACTIVE;
			nextStepButton.interactable = world.IsPause;
		};
	}

	const string PAUSE_ACTIVE = "Продолжить";
	const string PAUSE_NOT_ACTIVE = "Пауза";


	void OnSliderValueChanged(float val)
	{
		if(currentRoadHash != 0)
		{
			world.nodeManager.SetRoadWeight(currentRoadHash,val);
		}
	}

	public void AddRoadButton (Vector3 buttonWorldPos, int weigth, long roadHash)
	{
		var go = (GameObject)GameObject.Instantiate (buttonPrefab);
		go.transform.SetParent(transform);
		go.transform.localPosition = WorldToCanvasPosition (buttonWorldPos);
		go.transform.localScale = new Vector3 (1, 1, 1);

		var buttonComp = go.GetComponent<Button>();
		buttonComp.onClick.AddListener (() => ShowSlider(roadHash));
		buttons.Add(roadHash,go);

		var text = go.GetComponentInChildren<Text> ();
		text.text = weigth.ToString();
	}

	private Vector3 WorldToCanvasPosition(Vector3 worldPos)
	{
		var pos = new Vector3 ();
		pos.x = (worldPos.x / (worldWidth * 0.5f )) * canvasWidth * 0.5f;
		pos.y = (worldPos.y / (worldHeight * 0.5f )) * canvasHeight * 0.5f;
		return pos;
	}

	void HideSlider()
	{
		slider.gameObject.SetActive(false);
	}

	void ShowSlider (long roadHash)
	{
		if(currentRoadHash == roadHash)
		{
			slider.gameObject.SetActive(false);
			currentRoadHash = 0;
		}
		else
		{
			currentRoadHash = roadHash;
			slider.gameObject.SetActive(true);
			slider.gameObject.transform.localPosition = GetSliderPosition(roadHash);
			slider.minValue = 0;
			slider.maxValue = (float)NodeManager.MaxWeight;
			slider.value = (float) world.nodeManager.GetRoadWeight(roadHash);
		}
	}

	Vector3 GetSliderPosition (long roadHash)
	{
		var pos = buttons[roadHash].transform.localPosition;
		pos.y -= 20;
		return pos;
	}

}

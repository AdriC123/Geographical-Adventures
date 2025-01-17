using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileCaptureTest : MonoBehaviour
{
	Camera cam;
	public Transform display;

	void Start()
	{
		float height = Mathf.PI;
		cam = Camera.main;
		cam.orthographicSize = height / 4;

		SetDisplaySize();
	}

	[ContextMenu("Set Display Size")]
	void SetDisplaySize()
	{
		if (display)
		{
			display.transform.position = Vector3.zero;
			display.transform.localScale = new Vector3(Mathf.PI * 2, Mathf.PI, 1);
		}
	}

	[ContextMenu("Capture Tiles")]
	void StartCapture()
	{
		StartCoroutine(CaptureAllTiles());
	}

	[ContextMenu("Capture Single")]
	void SingleCapture()
	{
		cam = Camera.main;
		float height = Mathf.PI;
		cam.orthographicSize = height / 2;
		transform.position = Vector3.zero;
		ScreenCapture.CaptureScreenshot($"capture.png", 1);
		Debug.Log("Single Capture Complete");
	}

	IEnumerator CaptureAllTiles()
	{
		if (Application.isPlaying)
		{
			Debug.Log("Starting capture. Note: game window should be set to 8192x8192.");
			for (int y = 0; y < 2; y++)
			{
				for (int x = 0; x < 4; x++)
				{
					int i = y * 4 + x;
					Debug.Log($"Capturing tile {i + 1} of 8 ({name})");
					float left = -Mathf.PI;
					float top = Mathf.PI / 2;
					float tileSize = Mathf.PI / 2;

					float posX = left + tileSize * (x + 0.5f);
					float posY = top - tileSize * (y + 0.5f);
					cam.transform.position = new Vector3(posX, posY, -10);
					yield return null;

					ScreenCapture.CaptureScreenshot($"tile_{i}.png", 1);
					yield return null;
				}
			}
			Debug.Log("Capture Complete");
		}
		else
		{
			Debug.Log("Enter game mode first");
		}
	}

	// Update is called once per frame
	void Update()
	{


	}
}

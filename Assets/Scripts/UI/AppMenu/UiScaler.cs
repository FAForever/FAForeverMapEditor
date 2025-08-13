using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiScaler : MonoBehaviour
{

	public static UiScaler instance { get; private set; }
	public CanvasScaler canvasScaler;

	private void Awake()
	{
		instance = this;
		UpdateUiScale();
	}

	public static void UpdateUiScale()
	{
		float value = Mathf.Clamp(FafEditorSettings.GetUiScale(), 1f, 2.5f);
		instance.canvasScaler.scaleFactor = value;
		instance.canvasScaler.referencePixelsPerUnit = 100f / value;
	}

	public static void TempChangeUiScale(float value)
	{
		value = Mathf.Clamp(value, 1f, 2.5f);
		instance.canvasScaler.scaleFactor = value;
		instance.canvasScaler.referencePixelsPerUnit = 100f / value;
	}
}

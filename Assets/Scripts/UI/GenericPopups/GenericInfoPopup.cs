﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GenericInfoPopup : MonoBehaviour {

	public		Text		InfoText;

	#region ErrorMessage
	public void Show(bool on, string text = ""){
		gameObject.SetActive(on);
		InfoText.text = text;
	}

	public void Hide(){
		gameObject.SetActive(false);
	}

	public void InvokeHide(){
		Invoke(nameof(Hide), 4);
	}
	#endregion


	#region Generic Info

	public static GenericInfoPopup Current;
	public CanvasGroup grp;
	public AnimationCurve Visibility;
	public float Length;
	float lerp;

	static List<string> InfoStrings = new List<string>();

	private void Awake()
	{
		if (grp)
			Current = this;

		if (!Started && InfoStrings.Count > 0)
			StartNextInfoPopup();
	}

	public static void ShowInfo(string Text)
	{
		InfoStrings.Add(Text);

		if (Current == null)
			return;

		if (!Current.Started)
			Current.StartNextInfoPopup();
	}

	public static bool HasAnyInfo()
	{
		return Current != null && (Current.Started || InfoStrings.Count > 0);
	}

	private void Update()
	{
		if (grp == null)
			return;

		if (!Started)
			return;

		lerp += Mathf.Clamp(Time.unscaledDeltaTime, 0.016f, 0.0333f);
		grp.alpha = Visibility.Evaluate(lerp);

		if(lerp > Length)
		{
			FinishInfoPopup();
		}

	}

	bool Started = false;
	void StartNextInfoPopup()
	{
		InfoText.text = InfoStrings[0];
		grp.alpha = 0;
		lerp = 0;
		Started = true;
	}

	void FinishInfoPopup()
	{
		Started = false;
		InfoStrings.RemoveAt(0);

		if (InfoStrings.Count > 0)
			StartNextInfoPopup();
	}

	#endregion
}

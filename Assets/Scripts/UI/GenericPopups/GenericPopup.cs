﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A configurable confirmation popup modal that supports a global buffer for sequences of registered popups.
/// </summary>
public class GenericPopup : MonoBehaviour {

	public static GenericPopup Current;

	public GameObject Pivot;
	public CanvasGroup Cg;
	public Text TitleText;
	public Text DescriptionText;
	public GameObject CancelBtn;
	public GameObject NoBtn;
	public GameObject YesBtn;

	public Text CancelText;
	public Text NoText;
	public Text YesText;

	public Image TitleBar;
	public Color StandardColor;
	public Color WarningColor;
	public Color ErrorColor;

	public const string key_yes = "Yes";
	public const string key_no = "No";
	public const string key_cancel = "Cancel";

	public enum PopupTypes{
		OneButton, TwoButton, TriButton, Error, Warning
	}

	public class Popup
	{
		public PopupTypes PopupType;
		public string Title;
		public string Description;
		public string Yes;
		public string No;
		public string Cancel;

		public System.Action CancelAction;
		public System.Action NoAction;
		public System.Action YesAction;

	}

	private void Awake()
	{
		Current = this;
	}


	static List<Popup> PopupBufor = new List<Popup>();

	public static void RemoveAll()
	{
		PopupBufor = new List<Popup>();
		Current.PopupDisplayed = false;
		Current.Pivot.SetActive(false);
	}

	public static void ShowPopup(PopupTypes PopupType, string Title, string Description, string Yes, System.Action YesAction, string No = "", System.Action NoAction = null, string Cancel = "", System.Action CancelAction = null)
	{
		Popup NewPopup = new Popup();
		NewPopup.PopupType = PopupType;
		NewPopup.Title = Title;
		NewPopup.Description = Description;
		NewPopup.Yes = Yes;
		NewPopup.No = No;
		NewPopup.Cancel = Cancel;

		NewPopup.YesAction = YesAction;
		NewPopup.NoAction = NoAction;
		NewPopup.CancelAction = CancelAction;

		PopupBufor.Add(NewPopup);
		Current.StartPopup();
	}

	void StartPopup()
	{
		if (!PopupDisplayed && PopupBufor.Count > 0)
		{
			PopupDisplayed = true;
			ShowPupup();
		}
	}
	bool PopupDisplayed;
	void ShowPupup()
	{
		if (PopupBufor[0].PopupType == PopupTypes.OneButton || PopupBufor[0].PopupType == PopupTypes.Warning || PopupBufor[0].PopupType == PopupTypes.Error)
		{
			YesBtn.SetActive(true);
			NoBtn.SetActive(false);
			CancelBtn.SetActive(false);
		}
		else if (PopupBufor[0].PopupType == PopupTypes.TwoButton)
		{
			YesBtn.SetActive(true);
			NoBtn.SetActive(true);
			CancelBtn.SetActive(false);
		}
		else if (PopupBufor[0].PopupType == PopupTypes.TriButton)
		{
			YesBtn.SetActive(true);
			NoBtn.SetActive(true);
			CancelBtn.SetActive(true);
		}

		if (PopupBufor[0].PopupType == PopupTypes.Warning)
			TitleBar.color = WarningColor;
		else if (PopupBufor[0].PopupType == PopupTypes.Error)
			TitleBar.color = ErrorColor;
		else
			TitleBar.color = StandardColor;

		TitleText.text = PopupBufor[0].Title;
		DescriptionText.text = PopupBufor[0].Description;

		YesText.text = PopupBufor[0].Yes;
		NoText.text = PopupBufor[0].No;
		CancelText.text = PopupBufor[0].Cancel;
		Pivot.SetActive(true);
		//Debug.Log("Show popup");
	}

	void HidePopup()
	{
		//Debug.Log("Hide popup");

		PopupDisplayed = false;
		Pivot.SetActive(false);
		PopupBufor.RemoveAt(0);
		StartPopup();
	}


	public void PressYes()
	{
		PopupBufor[0].YesAction?.Invoke();
		HidePopup();
	}

	public void PressNo()
	{
		PopupBufor[0].NoAction?.Invoke();
		HidePopup();
	}

	public void PressCancel()
	{
		PopupBufor[0].CancelAction?.Invoke();
		HidePopup();
	}



}

﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using OzoneDecals;
using Selection;

namespace EditMap
{
	public partial class DecalsInfo : MonoBehaviour
	{

		private void OnEnable()
		{
			Selection.SelectionManager.Current.DisableLayer = 14;
			Selection.SelectionManager.Current.SetRemoveAction(DestroyDetails);
			Selection.SelectionManager.Current.SetSelectionChangeAction(SelectDetails);

			GoToSelection();
		}

		public void GoToSelection()
		{
			/*
			if (!MarkersInfo.MarkerPageChange)
			{
				Selection.SelectionManager.Current.CleanSelection();
			}
			*/

			SelectionManager.Current.SetAffectedGameObjects(DecalsControler.GetAllDecalsGo(), SelectionManager.SelectionControlTypes.Decal);
			//SelectionManager.Current.SetCustomSettings(true, true, true);


			PlacementManager.Clear();
			//if (ChangeControlerType.Current)
			//	ChangeControlerType.Current.UpdateButtons();

			//MarkerSelectionOptions.UpdateOptions();
		}


		public void DestroyDetails(List<GameObject> MarkerObjects, bool RegisterUndo = true)
		{

		}

		public void SelectDetails()
		{


		}

		public void Place(Vector3[] Positions, Quaternion[] Rotations)
		{
		}



		}
}
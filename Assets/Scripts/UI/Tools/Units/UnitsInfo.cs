using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Selection;
using Ozone.UI;

namespace EditMap
{
	public partial class UnitsInfo : ToolPage
	{
		public static UnitsInfo Current;

		[Header("Units Info")]
		[Header("UI")]
		public Transform Pivot;
		public GameObject GroupPrefab;
		public GameObject UnitPrefab;
		public RectTransform RenameField;
		public UiTextField NameInputField;
		public UiTextField PrefixInputField;
		public Text SelectedUnitsGroup;
		public Text UnitStats;
		public GameObject SelectionSection;
		public UiTextField UnitName;

		[Header("Objects")]
		public GameObject UnitInstancePrefab;
		public Material UnitMaterial;
		public Mesh NoUnitMesh;
		public Material NoUnitMaterial;
		public Mesh StrategicMesh;
		public Material StrategicIcon;

		private void OnEnable()
		{
			ForceExitCreate();
			PlacementManager.OnDropOnGameplay += DropAtGameplay;
			SelectionManager.Current.DisableLayer = 15;
			SelectionManager.Current.SetRemoveAction(DestroyUnits);
			SelectionManager.Current.SetSelectionChangeAction(SelectUnit);
			SelectionManager.Current.SetCustomSnapAction(SnapAction);
			SelectionManager.Current.SetCopyActionAction(CopyAction);
			SelectionManager.Current.SetPasteActionAction(PasteAction);
			SelectionManager.Current.SetDuplicateActionAction(DuplicateAction);

			GoToSelection();

			Generate();
		}

		private void OnDisable()
		{
			ClearRename();
			ForceExitCreate();
			PlacementManager.OnDropOnGameplay -= DropAtGameplay;
			SelectionManager.Current.ClearAffectedGameObjects();
			RenderUnitRanges.Clear();
		}

		public static void UnloadUnits()
		{
			UnitsControler.StopUnitsUpdate();

			foreach (UnitInstance unit in FindAllMapUnits())
			{
				Destroy(unit.gameObject);
			}
		}

		private void Update()
		{
			if (UpdateSelectedMatrixes)
			{
				UpdateSelectedMatrixes = false;

				for(int i = 0; i < SelectionManager.Current.Selection.Ids.Count; i++)
				{
					AllObjects[SelectionManager.Current.Selection.Ids[i]].GetComponent<UnitInstance>().UpdateMatrixTranslated();
				}

				for(int s = 0; s < SelectionManager.Current.SymetrySelection.Length; s++)
				{
					for (int i = 0; i < SelectionManager.Current.SymetrySelection[s].Ids.Count; i++)
					{
						AllObjects[SelectionManager.Current.SymetrySelection[s].Ids[i]].GetComponent<UnitInstance>().UpdateMatrixTranslated();
					}
				}
			}

			if (Input.GetKeyDown(KeyCode.P) && FirstSelected != null && !CameraControler.IsInputFieldFocused())
			{
				// Parent all groups to selected
				ReparrentGroups();

				// Parent all units to selected
				ReparrentUnits();

				Generate();
				RefreshVisibility();
			}

			if (Input.GetKeyDown(KeyCode.B) && !CameraControler.IsInputFieldFocused())
			{
				SwitchCreate();
			}
		}

		GameObject[] AllObjects;
		public void GoToSelection()
		{
			PlacementManager.Clear();
			SelectionManager.Current.CleanSelection();

			int[] AllTypes;
			AllObjects = UnitInstance.GetAllUnitGo(out AllTypes);
			SelectionManager.Current.SetAffectedGameObjects(AllObjects, SelectionManager.SelectionControlTypes.Units);
			SelectionManager.Current.SetAffectedTypes(AllTypes);

			OnChangeFreeRotation();

			if (ChangeControlerType.Current)
				ChangeControlerType.Current.UpdateButtons();

		}

		static List<UnitInstance> FindAllMapUnits()
		{
			List<UnitInstance> units = new();

			foreach (UnitInstance unit in FindObjectsOfType<UnitInstance>(true))
			{
				if (unit.Owner != null && unit.UnitRenderer != null)
				{
					units.Add(unit);
				}
			}

			return units;
		}

		public static void GetTotalUnitsReclaim(out float Mass, out float Energy)
		{
			Mass = 0;
			Energy = 0;

			foreach(UnitInstance UI in FindAllMapUnits())
			{
				Mass += UI.UnitRenderer.BP.BuildCostMass * UI.UnitRenderer.BP.Wreckage_MassMult;
				Energy += UI.UnitRenderer.BP.BuildCostEnergy * UI.UnitRenderer.BP.Wreckage_EnergyMult;
			}
		}

	}
}

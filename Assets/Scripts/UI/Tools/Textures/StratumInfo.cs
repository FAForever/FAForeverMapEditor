﻿using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Diagnostics;
using Ozone.UI;
using System.IO;
using B83.Image.BMP;
using SFB;
using FAF.MapEditor;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Image = UnityEngine.UI.Image;
using Toggle = UnityEngine.UI.Toggle;

namespace EditMap
{
	public partial class StratumInfo : ToolPage
	{
		public int layerPageIndex = 0;
		public int paintPageIndex = 1;
		public int settingsPageIndex = 2;
		
		[Header("Stratum Info")]
		public StratumSettingsUi StratumSettings;
		public Editing Edit;
		//public ScmapEditor Map;

		public int Selected = 0;
		public GameObject[] Stratum_Selections;
		public bool[] StratumHide = new bool[10];

		public RawImage Stratum_Albedo;
		public RawImage Stratum_Normal;
		public Text Stratum_Albedo_Name;
		public Text Stratum_Normal_Name;

		public UiTextField Stratum_Albedo_Input;
		public UiTextField Stratum_Normal_Input;

		// Brush
		[Header("Brush")]
		//public Slider BrushSizeSlider;
		public UiTextField BrushSize;
		//public Slider BrushStrengthSlider;
		public UiTextField BrushStrength;
		//public Slider BrushRotationSlider;
		public UiTextField BrushRotation;

		public UiTextField TargetValue;

		public UiTextField BrushMini;
		public UiTextField BrushMax;

		public UiTextField Scatter;

		[FormerlySerializedAs("LinearBrush")] public Toggle ScaledRange;

        public LayerMask TerrainMask;
		public List<Toggle> BrushToggles;
		public ToggleGroup ToogleGroup;

		public GameObject BrushListObject;
		public Transform BrushListPivot;
		public Material TerrainMaterial;

		public CanvasGroup ShaderSettings;
		public UiTextField Blurriness;
		public CanvasGroup ShaderTools;
		public InputField JavaPathField;
		public InputField ImagePathField;
		public OutputWindow OutputWindow;
		private ConcurrentQueue<string> outputQueue = new ();

		[Header("State")]
		public bool Invert;
		public bool Smooth;

		#region Classes
		[System.Serializable]
		public class StratumSettingsUi
		{
			[Header("Textures")]
			// Stratum 0 and Stratum 9 are the lower and upper layer
			public RawImage Stratum9_Albedo;
			// Upper layer has no normal texture
			public RawImage Stratum8_Albedo;
			public RawImage Stratum8_Normal;
			public RawImage Stratum7_Albedo;
			public RawImage Stratum7_Normal;
			public RawImage Stratum6_Albedo;
			public RawImage Stratum6_Normal;
			public RawImage Stratum5_Albedo;
			public RawImage Stratum5_Normal;
			public RawImage Stratum4_Albedo;
			public RawImage Stratum4_Normal;
			public RawImage Stratum3_Albedo;
			public RawImage Stratum3_Normal;
			public RawImage Stratum2_Albedo;
			public RawImage Stratum2_Normal;
			public RawImage Stratum1_Albedo;
			public RawImage Stratum1_Normal;
			public RawImage Stratum0_Albedo;
			public RawImage Stratum0_Normal;

			[Header("Mask")]
			public RawImage Stratum8_Mask;
			public RawImage Stratum7_Mask;
			public RawImage Stratum6_Mask;
			public RawImage Stratum5_Mask;
			public RawImage Stratum4_Mask;
			public RawImage Stratum3_Mask;
			public RawImage Stratum2_Mask;
			public RawImage Stratum1_Mask;

			[Header("Visibility")]
			public Text Stratum9_Visible;
			public Text Stratum8_Visible;
			public Text Stratum7_Visible;
			public Text Stratum6_Visible;
			public Text Stratum5_Visible;
			public Text Stratum4_Visible;
			public Text Stratum3_Visible;
			public Text Stratum2_Visible;
			public Text Stratum1_Visible;

			public Image[] XpShaderLayers;
		}
		#endregion

		void OnEnable()
		{
			JavaPathField.text = EnvPaths.GetJavaPath();
			ImagePathField.text = EnvPaths.GetImagePath();
			Blurriness.SetValue(ScmapEditor.Current.map.SpecularColor.x);
			BrushGenerator.Current.LoadBrushes();
			ReloadStratums();

			if (Pages[layerPageIndex].gameObject.activeSelf)
			{
				ChangePage(layerPageIndex);
			}
			else if (Pages[paintPageIndex].gameObject.activeSelf)
			{
				ChangePage(paintPageIndex);
			}
			else
			{
				ChangePage(settingsPageIndex);
			}
		}

		void OnDisable()
		{
			TerrainMaterial.SetFloat("_BrushSize", 0);
		}

		void Start()
		{
			ChangePage(layerPageIndex);
			SelectStratum(0);
		}


		bool TerainChanged = false;
		Color[] beginColors;

		Vector3 BeginMousePos;
		float StrengthBeginValue;
		bool ChangingStrength;
		float SizeBeginValue;
		bool ChangingSize;
		void Update()
		{
			// Process queued messages from Java CLI output
			if (outputQueue.TryDequeue(out string output))
			{
				OutputWindow.WriteOutput(output);
			}
			
			if (StratumChangeCheck)
				if (Input.GetMouseButtonUp(0))
					StratumChangeCheck = false;

			if (Pages[paintPageIndex].gameObject.activeSelf)
			{
				Invert = Input.GetKey(KeyCode.LeftAlt);
				Smooth = Input.GetKey(KeyCode.LeftShift);



				if (Edit.MauseOnGameplay || ChangingStrength || ChangingSize)
				{
					if (!ChangingSize && (KeyboardManager.BrushStrengthHold() || ChangingStrength))
					{
						// Change Strength
						if (Input.GetMouseButtonDown(0))
						{
							ChangingStrength = true;
							BeginMousePos = Input.mousePosition;
							StrengthBeginValue = BrushStrength.value;
						}
						else if (Input.GetMouseButtonUp(0))
						{
							ChangingStrength = false;
						}
						if (ChangingStrength)
						{
							//BrushStrengthSlider.value = Mathf.Clamp(StrengthBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 0.1f), 0, 100);
							BrushStrength.SetValue(Mathf.Clamp(StrengthBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 0.1f), 0, 100));
							UpdateStratumMenu(true);
							//UpdateBrushPosition(true);

						}
					}
					else if (KeyboardManager.BrushSizeHold() || ChangingSize)
					{
						// Change Size
						if (Input.GetMouseButtonDown(0))
						{
							ChangingSize = true;
							BeginMousePos = Input.mousePosition;
							SizeBeginValue = BrushSize.value;
						}
						else if (Input.GetMouseButtonUp(0))
						{
							ChangingSize = false;
						}
						if (ChangingSize)
						{
							BrushSize.SetValue(Mathf.Clamp(SizeBeginValue - (int)((BeginMousePos.x - Input.mousePosition.x) * 0.4f), 1, 256));
							UpdateStratumMenu(true);
							UpdateBrushPosition(true);

						}
					}
					else
					{
						if (Edit.MauseOnGameplay && Input.GetMouseButtonDown(0))
						{
							if (CameraControler.Current.DragStartedGameplay && UpdateBrushPosition(true))
							{
								SymmetryPaint();
							}
						}
						else if (Input.GetMouseButton(0))
						{
							if (CameraControler.Current.DragStartedGameplay)
							{
								if (UpdateBrushPosition(false))
								{
									SymmetryPaint();
								}
							}
						}
						else
						{
							UpdateBrushPosition(true);
						}
					}
				}

				if (!CameraControler.IsInputFieldFocused())// Ignore all unput
				{
					if (Input.GetMouseButton(0))
					{
					}
					else if (KeyboardManager.SwitchTypeNext())
					{
						ScaledRange.isOn = !ScaledRange.isOn;
					}
					else if (KeyboardManager.SwitchType1())
					{
						ScaledRange.isOn = false;
					}
					else if (KeyboardManager.SwitchType2())
					{
						ScaledRange.isOn = true;
					}

					if (KeyboardManager.IncreaseTarget())
					{
						if (TargetValue.value < 1)
							TargetValue.SetValue(TargetValue.value + 0.05f);
					}
					else if (KeyboardManager.DecreaseTarget())
					{
						if (TargetValue.value > 0)
							TargetValue.SetValue(TargetValue.value - 0.05f);
					}
				}

				if (Input.GetMouseButtonUp(0))
				{
					if (Painting)
					{
						Painting = false;
					}

					if (TerainChanged)
					{
						if (Selected > 0 && Selected < 5)
							Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(0, beginColors));
						else if (Selected > 4 && Selected < 9)
							Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(1, beginColors));
						TerainChanged = false;
					}
				}

				BrushGenerator.RegeneratePaintBrushIfNeeded();
			}
		}

		#region Stratums
		public void VisibleStratums()
		{
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum0", StratumHide[1] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum1", StratumHide[2] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum2", StratumHide[3] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum3", StratumHide[4] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum4", StratumHide[5] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum5", StratumHide[6] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum6", StratumHide[7] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum7", StratumHide[8] ? 1 : 0);
			ScmapEditor.Current.TerrainMaterial.SetInteger("_HideStratum8", StratumHide[9] ? 1 : 0);


			const string TextVisible = "V";
			const string TextHiden = "H";

			StratumSettings.Stratum1_Visible.text = StratumHide[1] ? TextHiden : TextVisible;
			StratumSettings.Stratum2_Visible.text = StratumHide[2] ? TextHiden : TextVisible;
			StratumSettings.Stratum3_Visible.text = StratumHide[3] ? TextHiden : TextVisible;
			StratumSettings.Stratum4_Visible.text = StratumHide[4] ? TextHiden : TextVisible;
			StratumSettings.Stratum5_Visible.text = StratumHide[5] ? TextHiden : TextVisible;
			StratumSettings.Stratum6_Visible.text = StratumHide[6] ? TextHiden : TextVisible;
			StratumSettings.Stratum7_Visible.text = StratumHide[7] ? TextHiden : TextVisible;
			StratumSettings.Stratum8_Visible.text = StratumHide[8] ? TextHiden : TextVisible;
			StratumSettings.Stratum9_Visible.text = StratumHide[9] ? TextHiden : TextVisible;
		}

		public void ReloadStratums()
		{
			StratumSettings.Stratum0_Albedo.texture = ScmapEditor.Current.Textures[0].Albedo;
			StratumSettings.Stratum0_Normal.texture = ScmapEditor.Current.Textures[0].Normal;

			StratumSettings.Stratum1_Albedo.texture = ScmapEditor.Current.Textures[1].Albedo;
			StratumSettings.Stratum1_Normal.texture = ScmapEditor.Current.Textures[1].Normal;

			StratumSettings.Stratum2_Albedo.texture = ScmapEditor.Current.Textures[2].Albedo;
			StratumSettings.Stratum2_Normal.texture = ScmapEditor.Current.Textures[2].Normal;

			StratumSettings.Stratum3_Albedo.texture = ScmapEditor.Current.Textures[3].Albedo;
			StratumSettings.Stratum3_Normal.texture = ScmapEditor.Current.Textures[3].Normal;

			StratumSettings.Stratum4_Albedo.texture = ScmapEditor.Current.Textures[4].Albedo;
			StratumSettings.Stratum4_Normal.texture = ScmapEditor.Current.Textures[4].Normal;

			StratumSettings.Stratum5_Albedo.texture = ScmapEditor.Current.Textures[5].Albedo;
			StratumSettings.Stratum5_Normal.texture = ScmapEditor.Current.Textures[5].Normal;

			StratumSettings.Stratum6_Albedo.texture = ScmapEditor.Current.Textures[6].Albedo;
			StratumSettings.Stratum6_Normal.texture = ScmapEditor.Current.Textures[6].Normal;

			StratumSettings.Stratum7_Albedo.texture = ScmapEditor.Current.Textures[7].Albedo;
			StratumSettings.Stratum7_Normal.texture = ScmapEditor.Current.Textures[7].Normal;

			StratumSettings.Stratum8_Albedo.texture = ScmapEditor.Current.Textures[8].Albedo;
			StratumSettings.Stratum8_Normal.texture = ScmapEditor.Current.Textures[8].Normal;

			StratumSettings.Stratum9_Albedo.texture = ScmapEditor.Current.Textures[9].Albedo;


			StratumSettings.Stratum1_Mask.texture = ScmapEditor.Current.map.TexturemapTex;
			StratumSettings.Stratum2_Mask.texture = ScmapEditor.Current.map.TexturemapTex;
			StratumSettings.Stratum3_Mask.texture = ScmapEditor.Current.map.TexturemapTex;
			StratumSettings.Stratum4_Mask.texture = ScmapEditor.Current.map.TexturemapTex;

			StratumSettings.Stratum5_Mask.texture = ScmapEditor.Current.map.TexturemapTex2;
			StratumSettings.Stratum6_Mask.texture = ScmapEditor.Current.map.TexturemapTex2;
			StratumSettings.Stratum7_Mask.texture = ScmapEditor.Current.map.TexturemapTex2;
			StratumSettings.Stratum8_Mask.texture = ScmapEditor.Current.map.TexturemapTex2;

			RefreshLayerUI();

		}

		bool LoadingStratum = false;
		public void SelectStratum(int newid)
		{
			LoadingStratum = true;
			Selected = newid;

			foreach (GameObject obj in Stratum_Selections) obj.SetActive(false);

			Stratum_Selections[Selected].SetActive(true);

			Stratum_Albedo.texture = ScmapEditor.Current.Textures[Selected].Albedo;
			Stratum_Albedo_Name.text = ScmapEditor.Current.Textures[Selected].AlbedoPath;
			Stratum_Albedo_Input.SetValue(ScmapEditor.Current.Textures[Selected].AlbedoScale);

			if (Selected != 9)
			{
				Stratum_Normal.texture = ScmapEditor.Current.Textures[Selected].Normal;
				Stratum_Normal_Name.text = ScmapEditor.Current.Textures[Selected].NormalPath;
				Stratum_Normal_Input.SetValue(ScmapEditor.Current.Textures[Selected].NormalScale);
                Stratum_Normal.gameObject.SetActive(true);
                Stratum_Normal_Name.gameObject.SetActive(true);
                Stratum_Normal_Input.gameObject.SetActive(true);
            }
			else
			{
				Stratum_Normal.gameObject.SetActive(false);
				Stratum_Normal_Name.gameObject.SetActive(false);
				Stratum_Normal_Input.gameObject.SetActive(false);
			}

			if (Shader.GetGlobalInt("_ShaderID") >= 200)
			{
				if (Selected == 9)
				{
                    Stratum_Albedo_Input.gameObject.SetActive(false);
                }
				else if (Selected == 8)
				{
                    Stratum_Albedo_Input.gameObject.SetActive(false);
                    Stratum_Normal_Input.gameObject.SetActive(false);
                }
				else
				{
					Stratum_Albedo_Input.SetTitle("Scale");
					Stratum_Normal_Input.SetTitle("Secondary Blending Scale");
					Stratum_Albedo_Input.gameObject.SetActive(true);
                    Stratum_Normal_Input.gameObject.SetActive(true);
                }
            }
			else
			{
                Stratum_Albedo_Input.SetTitle("Albedo Scale");
                Stratum_Normal_Input.SetTitle("Normal Scale");
                Stratum_Albedo_Input.gameObject.SetActive(true);
            }
			LoadingStratum = false;
		}

		public void ResetVisibility()
		{
			for (int i = 0; i < StratumHide.Length; i++)
				StratumHide[i] = false;

			VisibleStratums();
		}

		public void ToggleLayerVisibility(int id)
		{
			StratumHide[id] = !StratumHide[id];
			// TODO Update Terrain Shader To Hide Stratum

			VisibleStratums();
		}

		#endregion

		#region Update Menu

		public override bool ChangePage(int newPageID)
		{
			bool pageChanged = base.ChangePage(newPageID);
			
			if (newPageID == layerPageIndex)
			{
				ConfigureForLayersPage();
			}
			else if (newPageID == paintPageIndex)
			{
				ConfigureForPaintPage();
			}
			else if (newPageID == settingsPageIndex)
			{
				ConfigureForSettingsPage();
			}

			return pageChanged;
		}
		
		private void ConfigureForLayersPage()
		{
			TerrainMaterial.SetFloat("_BrushSize", 0);
		}

		private void ConfigureForPaintPage()
		{
			BrushGenerator.Current.LoadBrushes();

			if (!BrusheshLoaded) LoadBrushesh();
			UpdateStratumMenu();

			TerrainMaterial.SetInteger("_Brush", 1);
			BrushGenerator.SetFalloff(SelectedFalloff, LastRotation);
			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.Brushes[SelectedFalloff]);
		}

		private void ConfigureForSettingsPage()
		{
			TerrainMaterial.SetFloat("_BrushSize", 0);
		}

		static readonly Color DisabledLayerColor = new Color(1f, 1f, 1f, 0.5f);

		public void RefreshLayerUI()
		{
			bool allLayers = MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.text != "TTerrain";
			for(int i = 0; i < StratumSettings.XpShaderLayers.Length; i++)
			{
				StratumSettings.XpShaderLayers[i].color = allLayers ? Color.white : DisabledLayerColor;
			}

			StratumSettings.Stratum8_Mask.gameObject.SetActive(allLayers);
			StratumSettings.Stratum7_Mask.gameObject.SetActive(allLayers);
			StratumSettings.Stratum6_Mask.gameObject.SetActive(allLayers);
			StratumSettings.Stratum5_Mask.gameObject.SetActive(allLayers);
		}

		public float Min = 0;
		public float Max = 512;
		int LastRotation = 0;
		bool StratumChangeCheck = false;

		bool StratumMenuUndoRegistered = false;

		public void UpdateStratumMenu(bool Slider = false)
		{
			if (!gameObject.activeSelf)
				return;

			if (Pages[layerPageIndex].gameObject.activeSelf)
			{
				if (Slider)
				{
					if (!StratumChangeCheck)
					{
						StratumChangeCheck = true;
						if (!LoadingStratum && !StratumMenuUndoRegistered)
						{
							Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(Selected));
							StratumMenuUndoRegistered = true;
						}
					}
					if (!LoadingStratum)
					{
					}
				}
				else
				{
					if (!LoadingStratum && !StratumMenuUndoRegistered)
					{
						Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(Selected));
					}
					StratumMenuUndoRegistered = false;
				}
				if (!LoadingStratum)
				{
					ScmapEditor.Current.Textures[Selected].AlbedoScale = Stratum_Albedo_Input.value;
					ScmapEditor.Current.Textures[Selected].NormalScale = Stratum_Normal_Input.value;
				}

				ScmapEditor.Current.UpdateScales(Selected);

			}
			else if (Pages[paintPageIndex].gameObject.activeSelf)
			{
				if (Slider)
				{

				}
				else
				{

				}


				Min = BrushMini.intValue;
				Max = BrushMax.intValue;

				Min = Mathf.Clamp(Min, 0, Max);
				Max = Mathf.Clamp(Max, Min, 90);

				BrushMini.SetValue(Min);
				BrushMax.SetValue(Max);


				if (TargetValue.value < 0)
					TargetValue.SetValue(0);
				else if (TargetValue.value > 1)
					TargetValue.SetValue(1);

				//BrushMini.text = Min.ToString("0");
				//BrushMax.text = Max.ToString("0");

				if (LastRotation != BrushRotation.intValue)
				{
					LastRotation = BrushRotation.intValue;
					if (LastRotation == 0)
					{
						BrushGenerator.Current.RotatedBrush = BrushGenerator.Current.Brushes[SelectedFalloff];
					}
					else
					{
						BrushGenerator.Current.RotatedBrush = BrushGenerator.rotateTexture(BrushGenerator.Current.Brushes[SelectedFalloff], LastRotation);
					}

					TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.RotatedBrush);
					BrushGenerator.RegeneratePaintBrushIfNeeded(true);
				}
				//TerrainMaterial.SetFloat("_BrushSize", BrushSize.value);
			}
		}

		public void updateShaderSettings()
		{
			Vector4 oldSpecular = ScmapEditor.Current.map.SpecularColor;
			ScmapEditor.Current.map.SpecularColor = new Vector4(Blurriness.value, oldSpecular.y, oldSpecular.z, oldSpecular.w);
			Shader.SetGlobalVector("SpecularColor", ScmapEditor.Current.map.SpecularColor);
		}
        #endregion

        #region Load all brushes
        bool BrusheshLoaded = false;
		public void LoadBrushesh()
		{
			Clean();

			string StructurePath = MapLuaParser.GetDataPath() + "/Structure/"; ;
			StructurePath += "brush";

			if (!Directory.Exists(StructurePath))
			{
				Debug.LogError("Cant find brush folder");
				return;
			}

			BrushToggles = new List<Toggle>();

			for (int i = 0; i < BrushGenerator.Current.Brushes.Count; i++)
			{
				GameObject NewBrush = Instantiate(BrushListObject) as GameObject;
				NewBrush.transform.SetParent(BrushListPivot, false);
				NewBrush.transform.localScale = Vector3.one;
				string ThisName = BrushGenerator.Current.BrushesNames[i];
				BrushToggles.Add(NewBrush.GetComponent<BrushListId>().SetBrushList(ThisName, BrushGenerator.Current.Brushes[i], i));
				NewBrush.GetComponent<BrushListId>().Controler2 = this;
			}

			foreach (Toggle tog in BrushToggles)
			{
				tog.isOn = false;
				//tog.group = ToogleGroup;
			}
			BrushToggles[0].isOn = true;
			SelectedFalloff = 0;

			BrusheshLoaded = true;
		}

		void Clean()
		{
			BrusheshLoaded = false;
			foreach (Transform child in BrushListPivot) Destroy(child.gameObject);
		}

		#endregion


		#region Brush Update
		int SelectedBrush = 0;
		public void ChangeBrush(int id)
		{
			SelectedBrush = id;
		}

		int SelectedFalloff = 0;
		public void ChangeFalloff(int id)
		{
			SelectedFalloff = id;

			for (int i = 0; i < BrushToggles.Count; i++)
			{
				if (i == SelectedFalloff)
					continue;
				BrushToggles[i].isOn = false;
			}

			LastRotation = BrushRotation.intValue;

			BrushGenerator.SetFalloff(SelectedFalloff, LastRotation);

			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.RotatedBrush);
		}


		Vector3 BrushPos;
		Vector3 MouseBeginClick;
		bool UpdateBrushPosition(bool Forced = false)
		{
			//Debug.Log(Vector3.Distance(MouseBeginClick, Input.mousePosition));
			if (Forced || Vector3.Distance(MouseBeginClick, Input.mousePosition) > 1) { }
			else
			{
				return false;
			}

			float SizeXprop = MapLuaParser.GetMapSizeX() / 512f;
			float SizeZprop = MapLuaParser.GetMapSizeY() / 512f;
			float BrushSizeValue = BrushSize.value;

			MouseBeginClick = Input.mousePosition;
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit, 2000, TerrainMask))
			{
				BrushPos = hit.point;
				BrushPos.y = ScmapEditor.Current.Teren.SampleHeight(BrushPos);

				Vector3 tempCoord = ScmapEditor.Current.Teren.gameObject.transform.InverseTransformPoint(BrushPos);
				Vector3 coord = Vector3.zero;
				float SizeX = (int)((BrushSizeValue / SizeXprop) * 100) * 0.01f;
				float SizeZ = (int)((BrushSizeValue / SizeZprop) * 100) * 0.01f;
				coord.x = (tempCoord.x - SizeX * MapLuaParser.GetMapSizeX() * 0.0001f) / ScmapEditor.Current.Teren.terrainData.size.x;
				coord.z = (tempCoord.z - SizeZ * MapLuaParser.GetMapSizeY() * 0.0001f) / ScmapEditor.Current.Teren.terrainData.size.z;

				TerrainMaterial.SetFloat("_BrushSize", BrushSizeValue / ((SizeXprop + SizeZprop) / 2f));
				TerrainMaterial.SetFloat("_BrushUvX", coord.x);
				TerrainMaterial.SetFloat("_BrushUvY", coord.z);

				return true;
			}
			return false;
		}
		#endregion
		bool _Painting = false;
		bool Painting
		{
			set
			{
				_Painting = value;
				TerrainMaterial.SetInteger("_BrushPainting", _Painting ? (1) : (0));

			}
			get
			{
				return _Painting;
			}
		}
		float ScatterValue = 0;
		static float TargetPaintValue = 1;
		void SymmetryPaint()
		{
			Painting = true;
			float SizeProportion = (float)ScmapEditor.Current.map.TexturemapTex.width / (float)ScmapEditor.Current.map.Width;
			size = (int)(BrushSize.value * SizeProportion);
			ScatterValue = Scatter.value;

			if (ScaledRange.isOn)
				TargetPaintValue = Mathf.Clamp01((TargetValue.value + 1f) / 2f);
			else
				TargetPaintValue = TargetValue.value;

			BrushGenerator.Current.GenerateSymmetry(BrushPos, 0, ScatterValue, size * 0.03f);

			if (Selected == 1 || Selected == 5)
				PaintChannel = 0;
			else if (Selected == 2 || Selected == 6)
				PaintChannel = 1;
			else if (Selected == 3 || Selected == 7)
				PaintChannel = 2;
			else if (Selected == 4 || Selected == 8)
				PaintChannel = 3;

			for (int i = 0; i < BrushGenerator.Current.PaintPositions.Length; i++)
			{
				Paint(BrushGenerator.Current.PaintPositions[i], i);
			}

			if (Selected > 0 && Selected < 5)
			{
				ScmapEditor.Current.map.TexturemapTex.Apply();
			}
			else if (Selected > 4 && Selected < 9)
			{
				ScmapEditor.Current.map.TexturemapTex2.Apply();
			}
		}


		static Color[] StratumData;
		static int PaintChannel = 0;
		int size = 0;
		void Paint(Vector3 AtPosition, int id = 0)
		{

			int hmWidth = ScmapEditor.Current.map.TexturemapTex.width;
			int hmHeight = ScmapEditor.Current.map.TexturemapTex.height;

			Vector3 tempCoord = ScmapEditor.Current.Teren.gameObject.transform.InverseTransformPoint(AtPosition);
			Vector3 coord = Vector3.zero;
			coord.x = tempCoord.x / ScmapEditor.Current.Teren.terrainData.size.x;
			//coord.y = tempCoord.y / Map.Teren.terrainData.size.y;
			coord.z = 1 - tempCoord.z / ScmapEditor.Current.Teren.terrainData.size.z;

			if (coord.x > 1) return;
			if (coord.x < 0) return;
			if (coord.z > 1) return;
			if (coord.z < 0) return;

			// get the position of the terrain heightmap where this game object is
			int posXInTerrain = (int)(coord.x * hmWidth);
			int posYInTerrain = (int)(coord.z * hmHeight);
			// we set an offset so that all the raising terrain is under this game object
			int offset = size / 2;
			// get the heights of the terrain under this game object

			// Horizontal Brush Offsets
			int OffsetLeft = 0;
			if (posXInTerrain - offset < 0) OffsetLeft = Mathf.Abs(posXInTerrain - offset);
			int OffsetRight = 0;
			if (posXInTerrain - offset + size > hmWidth) OffsetRight = posXInTerrain - offset + size - hmWidth;

			// Vertical Brush Offsets
			int OffsetDown = 0;
			if (posYInTerrain - offset < 0) OffsetDown = Mathf.Abs(posYInTerrain - offset);
			int OffsetTop = 0;
			if (posYInTerrain - offset + size > hmHeight) OffsetTop = posYInTerrain - offset + size - hmHeight;

			//float CenterHeight = 0;
			float LocalBrushStrength = Mathf.Pow(BrushStrength.value * 0.01f, 1.5f) * 0.6f;
			float inverted = (Invert ? (-1) : 1);
			LocalBrushStrength *= inverted;
			float SampleBrush = 0;
			//Color BrushValue;
			int x = 0;
			int y = 0;
			int i = 0;
			int j = 0;

			int SizeDown = (size - OffsetDown) - OffsetTop;
			int SizeLeft = (size - OffsetLeft) - OffsetRight;

			if (Selected > 0 && Selected < 5)
			{
				StratumData = ScmapEditor.Current.map.TexturemapTex.GetPixels(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, SizeLeft, SizeDown);
			}
			else if (Selected > 4 && Selected < 9)
			{
				StratumData = ScmapEditor.Current.map.TexturemapTex2.GetPixels(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, SizeLeft, SizeDown);
			}
			else
				return;

			for (i = 0; i < SizeDown; i++)
			{
				for (j = 0; j < SizeLeft; j++)
				{

					if (Min > 0 || Max < 90)
					{
						float angle = Vector3.Angle(Vector3.up, ScmapEditor.Current.Teren.terrainData.GetInterpolatedNormal((posXInTerrain - offset + OffsetLeft + i) / (float)hmWidth, 1 - (posYInTerrain - offset + OffsetDown + j) / (float)hmHeight));
						if ((angle < Min && Min > 0) || (angle > Max && Max < 90))
							continue;
					}

					// Brush strength
					x = BrushGenerator.Current.PaintImageWidths[id] - 
						(int)(((i + OffsetDown) / (float)size) * BrushGenerator.Current.PaintImageWidths[id]);
					y = (int)(((j + OffsetLeft) / (float)size) * BrushGenerator.Current.PaintImageHeights[id]);

					if (x < 0 || y < 0 || x >= BrushGenerator.Current.PaintImageWidths[id] || y >= BrushGenerator.Current.PaintImageHeights[id])
						continue;
					SampleBrush = Mathf.Clamp01(BrushGenerator.Current.Values[id][y + BrushGenerator.Current.PaintImageHeights[id] * x] - 0.0255f);
					SampleBrush = Mathf.GammaToLinearSpace(SampleBrush); // , 0.454545f

					if (SampleBrush >= 0.003f)
					{
						if (Smooth || SelectedBrush == 2)
						{
							//float PixelPower = Mathf.Abs( heights[i,j] - CenterHeight);
							//heights[i,j] = Mathf.Lerp(heights[i,j], CenterHeight, BrushStrengthSlider.value * 0.4f * Mathf.Pow(SambleBrush, 2) * PixelPower);
						}
						else if (SelectedBrush == 3)
						{
							//float PixelPower = heights[i,j] - CenterHeight;
							//heights[i,j] += Mathf.Lerp(PixelPower, 0, PixelPower * 10) * BrushStrengthSlider.value * 0.01f * Mathf.Pow(SambleBrush, 2);
						}
						else
						{
							int XY = j + i * SizeLeft;

							switch (PaintChannel)
							{
								case 0:
										StratumData[XY].r = ToTarget(StratumData[XY].r, SampleBrush * LocalBrushStrength);
									break;
								case 1:
										StratumData[XY].g = ToTarget(StratumData[XY].g, SampleBrush * LocalBrushStrength);
									break;
								case 2:
										StratumData[XY].b = ToTarget(StratumData[XY].b, SampleBrush * LocalBrushStrength);
									break;
								case 3:
										StratumData[XY].a = ToTarget(StratumData[XY].a, SampleBrush * LocalBrushStrength);
									break;
							}
						}
					}
				}
			}
			// set the new height
			if (!TerainChanged)
			{
				if (Selected > 0 && Selected < 5)
				{
					beginColors = ScmapEditor.Current.map.TexturemapTex.GetPixels();
				}
				else if (Selected > 4 && Selected < 9)
				{
					beginColors = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
				}

				TerainChanged = true;
			}
			if (Selected > 0 && Selected < 5)
			{
				ScmapEditor.Current.map.TexturemapTex.SetPixels(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, (size - OffsetLeft) - OffsetRight, (size - OffsetDown) - OffsetTop, StratumData);
			}
			else
			{
				ScmapEditor.Current.map.TexturemapTex2.SetPixels(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, (size - OffsetLeft) - OffsetRight, (size - OffsetDown) - OffsetTop, StratumData);
			}
			//Map.map.TexturemapTex.SetPixels(StratumData);
		}

		static float ToTarget(float source, float addValue)
		{
			if(addValue < 0)
			{
				return Mathf.Clamp(source + addValue, 0, 1);
			}
			else if(source < TargetPaintValue)
			{
				return Mathf.Clamp(source + addValue, 0, TargetPaintValue);
			}
			else
			{
				return Mathf.Clamp(source - addValue, TargetPaintValue, 1);
			}
		}

		#region Select Texture
		public void SelectAlbedo()
		{
			if(ResourceBrowser.DragedObject == null || ResourceBrowser.DragedObject.ContentType != ResourceObject.ContentTypes.Texture)
				return;

			if (!ResourceBrowser.Current.gameObject.activeSelf)
				return;
			if (ResourceBrowser.SelectedCategory == 0 || ResourceBrowser.SelectedCategory == 1)
			{
				Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(Selected));
				//Debug.Log(ResourceBrowser.Current.LoadedPaths[ResourceBrowser.DragedObject.InstanceId]);

				ScmapEditor.Current.Textures[Selected].Albedo = ResourceBrowser.Current.LoadedTextures[ResourceBrowser.DragedObject.InstanceId];
				ScmapEditor.Current.Textures[Selected].AlbedoPath = ResourceBrowser.Current.LoadedPaths[ResourceBrowser.DragedObject.InstanceId];

				//Map.map.Layers [Selected].PathTexture = Map.Textures [Selected].AlbedoPath;

				ResourceBrowser.ClearDrag();
				ScmapEditor.Current.SetTextures(Selected);
				ReloadStratums();
				SelectStratum(Selected);
			}
		}

		public void SelectNormal()
		{
			if (ResourceBrowser.DragedObject == null || ResourceBrowser.DragedObject.ContentType != ResourceObject.ContentTypes.Texture)
				return;

			if (!ResourceBrowser.Current.gameObject.activeSelf)
				return;
			if (ResourceBrowser.SelectedCategory == 0 || ResourceBrowser.SelectedCategory == 1)
			{
				Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(Selected));
				Debug.Log(ResourceBrowser.Current.LoadedPaths[ResourceBrowser.DragedObject.InstanceId]);

				//Map.Textures [Selected].Normal = ResourceBrowser.Current.LoadedTextures [ResourceBrowser.DragedObject.InstanceId];
				ScmapEditor.Current.Textures[Selected].NormalPath = ResourceBrowser.Current.LoadedPaths[ResourceBrowser.DragedObject.InstanceId];

				GetGamedataFile.LoadTextureFromGamedata(ScmapEditor.Current.Textures[Selected].NormalPath, Selected, true);

				//Map.map.Layers [Selected].PathNormalmap = Map.Textures [Selected].NormalPath;

				ResourceBrowser.ClearDrag();
				ScmapEditor.Current.SetTextures(Selected);
				ReloadStratums();
				SelectStratum(Selected);
			}
		}

		public void ClickAlbedo()
		{
			ResourceBrowser.Current.LoadStratumTexture(ScmapEditor.Current.Textures[Selected].AlbedoPath);
		}

		public void ClickNormal()
		{
			ResourceBrowser.Current.LoadStratumTexture(ScmapEditor.Current.Textures[Selected].NormalPath);
		}
		#endregion


#region ColorTransfer
		Color[] GetPixels(int layer)
		{
			if (layer > 4)
			{
				return ScmapEditor.Current.map.TexturemapTex2.GetPixels();
			}
			else
			{
				return ScmapEditor.Current.map.TexturemapTex.GetPixels();
			}
		}

		void SetPixels(int layer, Color[] Colors)
		{
			if (layer > 4)
			{
				ScmapEditor.Current.map.TexturemapTex2.SetPixels(Colors);
				ScmapEditor.Current.map.TexturemapTex2.Apply(false);
			}
			else
			{
				ScmapEditor.Current.map.TexturemapTex.SetPixels(Colors);
				ScmapEditor.Current.map.TexturemapTex.Apply(false);
			}
		}

		float GetChannelByLayer(int layer, Color color)
		{
			if (layer == 1 || layer == 5)
				return color.r;
			else if (layer == 2 || layer == 6)
				return color.g;
			else if (layer == 3 || layer == 7)
				return color.b;
			else if (layer == 4 || layer == 8)
				return color.a;
			else
				return 0;
		}

		void SetChannelByLayer(int layer, ref Color color, float channel)
		{
			if (layer == 1 || layer == 5)
				color.r = channel;
			else if (layer == 2 || layer == 6)
				color.g = channel;
			else if (layer == 3 || layer == 7)
				color.b = channel;
			else if (layer == 4 || layer == 8)
				color.a = channel;
		}

        #endregion

        #region TextureGeneration
        public void BrowseJavaPath()
        {

            var paths = StandaloneFileBrowser.OpenFolderPanel("Select folder containing java.exe.", EnvPaths.GetJavaPath(), false);

            if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
            {
                JavaPathField.text = paths[0];
                EnvPaths.SetJavaPath(paths[0]);
            }
        }
        
        public void UpdateJavaPath()
        {
	        if (!string.IsNullOrEmpty(JavaPathField.text))
	        {
		        EnvPaths.SetJavaPath(JavaPathField.text);
	        }
        }
		
        public void BrowseImagePath()
        {

	        var paths = StandaloneFileBrowser.OpenFolderPanel("Select folder for height and roughness images.", "C:\\", false);

	        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
	        {
		        ImagePathField.text = paths[0];
		        EnvPaths.SetImagePath(paths[0]);
	        }
        }
        
        public void UpdateImagePath()
        {
	        if (!string.IsNullOrEmpty(ImagePathField.text))
	        {
		        EnvPaths.SetImagePath(ImagePathField.text);
	        }
        }

        private int invokeToolsuite(string arguments)
        {
	        OutputWindow.Initialize();
	        Process neroxisToolsuite = new Process();
            neroxisToolsuite.StartInfo.FileName = EnvPaths.GetJavaPath() + "/java.exe";
            var jarPath = MapLuaParser.StructurePath + "Neroxis/toolsuite-all.jar";
            neroxisToolsuite.StartInfo.Arguments = "-jar \"" + jarPath + "\" " + arguments;
            outputQueue.Enqueue("Starting Java process: " + neroxisToolsuite.StartInfo.FileName + neroxisToolsuite.StartInfo.Arguments);
            
            neroxisToolsuite.StartInfo.CreateNoWindow = true;
            neroxisToolsuite.StartInfo.UseShellExecute = false;
            neroxisToolsuite.StartInfo.RedirectStandardOutput = true;
            neroxisToolsuite.StartInfo.RedirectStandardError = true;
            neroxisToolsuite.OutputDataReceived += (sender, args) => outputQueue.Enqueue(args.Data);
            neroxisToolsuite.ErrorDataReceived += (sender, args) => outputQueue.Enqueue(args.Data);

            neroxisToolsuite.Start();
            neroxisToolsuite.BeginOutputReadLine();
            neroxisToolsuite.BeginErrorReadLine();
            neroxisToolsuite.WaitForExit();
            
            outputQueue.Enqueue("Java process exited with code: " + neroxisToolsuite.ExitCode);
            OutputWindow.Close();
            if (neroxisToolsuite.ExitCode != 0) GenericInfoPopup.ShowInfo("Command failed! Check the log for more information.");
            return neroxisToolsuite.ExitCode;
        }

        public void GenerateMapInfoTexture()
        {
	        string toolsuiteArguments = "export-map-info --map-path=\"" + MapLuaParser.LoadedMapFolderPath + "\"";
	        int exitcode = invokeToolsuite(toolsuiteArguments);
	        if (exitcode != 0) return;
	        
	        Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(9));
                
	        string texturePath = MapLuaParser.RelativeLoadedMapFolderPath + "env/layers/mapInfo.dds";
	        ScmapEditor.Current.Textures[8].Albedo = GetGamedataFile.LoadTexture2D(texturePath, false, false, false);
	        ScmapEditor.Current.Textures[8].AlbedoPath = texturePath;
	        ScmapEditor.Current.Textures[8].AlbedoScale = MapLuaParser.Current.ScenarioLuaFile.Data.Size[0] + 1;
            ScmapEditor.Current.SetTextures(8);
            ReloadStratums();
            SelectStratum(8);
        }
        
        public void GenerateMapNormalTexture()
        {
	        string toolsuiteArguments = "export-map-normals --map-path=\"" + MapLuaParser.LoadedMapFolderPath + "\"";
	        int exitcode = invokeToolsuite(toolsuiteArguments);
	        if (exitcode != 0) return;
	        
	        Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(9));
                
	        string texturePath = MapLuaParser.RelativeLoadedMapFolderPath + "env/layers/mapNormal.dds";
	        ScmapEditor.Current.Textures[8].Normal = GetGamedataFile.LoadTexture2D(texturePath, true, false, false);
	        ScmapEditor.Current.Textures[8].NormalPath = texturePath;
	        ScmapEditor.Current.Textures[8].NormalScale = MapLuaParser.Current.ScenarioLuaFile.Data.Size[0] + 1;
	        ScmapEditor.Current.SetTextures(8);
	        ReloadStratums();
	        SelectStratum(8);
        }
        
        public void GenerateHeightRoughnessTexture()
        {
	        if (ImagePathField.text == "")
	        {
		        GenericInfoPopup.ShowInfo("You need to specify the directory that contains your source images.");
		        return;
	        }
	        string toolsuiteArguments = "generate-pbr --in-path=\"" + ImagePathField.text + "\"" +
	                                    " --out-path=\"" + MapLuaParser.LoadedMapFolderPath + "/env/layers/\"";
	        int exitcode = invokeToolsuite(toolsuiteArguments);
	        if (exitcode != 0) return;
	        
	        Undo.RegisterUndo(new UndoHistory.HistoryStratumChange(), new UndoHistory.HistoryStratumChange.StratumChangeHistoryParameter(9));
                
	        string texturePath = MapLuaParser.RelativeLoadedMapFolderPath + "env/layers/roughnessAndHeight.dds";
	        ScmapEditor.Current.Textures[9].Albedo = GetGamedataFile.LoadTexture2D(texturePath, false, false, false);
            ScmapEditor.Current.Textures[9].AlbedoPath = texturePath;
            ScmapEditor.Current.Textures[9].AlbedoScale = MapLuaParser.Current.ScenarioLuaFile.Data.Size[0] + 1;
            ScmapEditor.Current.SetTextures(9);
            ReloadStratums();
            SelectStratum(9);
        }
        
        public void ResetRoughnessMask()
        {
	        Color[] data = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
	        beginColors = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
	        Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(1, beginColors));
	        
	        for (int i = 0; i < data.Length; i++)
	        {
		        data[i].a = 0.5f;
	        }

	        ScmapEditor.Current.map.TexturemapTex2.SetPixels(data);
	        ScmapEditor.Current.map.TexturemapTex2.Apply(false);
        }

        #endregion

		#region Import/Export


		public void ClearStratumMask()
		{
			if (Selected == 0 || Selected == 9)
				return;


			Color[] StratumData;
			if (Selected > 4)
			{
				StratumData = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
				beginColors = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
			}
			else
			{
				StratumData = ScmapEditor.Current.map.TexturemapTex.GetPixels();
				beginColors = ScmapEditor.Current.map.TexturemapTex.GetPixels();
			}

			if (Selected > 0 && Selected < 5)
				Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(0, beginColors));
			else if (Selected > 4 && Selected < 9)
				Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(1, beginColors));


			for (int i = 0; i < StratumData.Length; i++)
			{
				if (Selected == 1 || Selected == 5)
					StratumData[i].r = 0;
				else if (Selected == 2 || Selected == 6)
					StratumData[i].g = 0;
				else if (Selected == 3 || Selected == 7)
					StratumData[i].b = 0;
				else if (Selected == 4 || Selected == 8)
					StratumData[i].a = 0;
			}


			if (Selected > 4)
			{
				ScmapEditor.Current.map.TexturemapTex2.SetPixels(StratumData);
				ScmapEditor.Current.map.TexturemapTex2.Apply(false);
			}
			else
			{
				ScmapEditor.Current.map.TexturemapTex.SetPixels(StratumData);
				ScmapEditor.Current.map.TexturemapTex.Apply(false);
			}

			GenericInfoPopup.ShowInfo("Cleared stratum mask for layer " + Selected);
		}

		public void ImportStratumMask()
		{
			if (Selected == 0 || Selected == 9)
				return;

			var extensions = new[]
			{
				new ExtensionFilter("Stratum mask", new string[]{"png", "raw" })
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import stratum mask", DefaultPath, extensions, false);
			
			if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
			{
				if (paths[0].ToLower().EndsWith("png"))
				{
					Color[] data;
					if (Selected > 4)
					{
						data = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
						beginColors = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
					}
					else
					{
						data = ScmapEditor.Current.map.TexturemapTex.GetPixels();
						beginColors = ScmapEditor.Current.map.TexturemapTex.GetPixels();
					}

					if (Selected > 0 && Selected < 5)
						Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(0, beginColors));
					else if (Selected > 4 && Selected < 9)
						Undo.RegisterUndo(new UndoHistory.HistoryStratumPaint(), new UndoHistory.HistoryStratumPaint.StratumPaintHistoryParameter(1, beginColors));

					byte[] binaryImageData = File.ReadAllBytes(paths[0]);
					Texture2D ImportedImage = new Texture2D(ScmapEditor.Current.map.TexturemapTex.width,
						ScmapEditor.Current.map.TexturemapTex.height);
					ImportedImage.LoadImage(binaryImageData);

					ImportedImage = TextureFlip.FlipTextureVertical(ImportedImage, false);

					Color[] ImportedColors = ImportedImage.GetPixels();

					for (int i = 0; i < data.Length; i++)
					{
						if (Selected == 1 || Selected == 5)
							data[i].r = ImportedColors[i].r;
						else if (Selected == 2 || Selected == 6)
							data[i].g = ImportedColors[i].r;
						else if (Selected == 3 || Selected == 7)
							data[i].b = ImportedColors[i].r;
						else if (Selected == 4 || Selected == 8)
							data[i].a = ImportedColors[i].r;
					}


					if (Selected > 4)
					{
						ScmapEditor.Current.map.TexturemapTex2.SetPixels(data);
						ScmapEditor.Current.map.TexturemapTex2.Apply(false);
					}
					else
					{
						ScmapEditor.Current.map.TexturemapTex.SetPixels(data);
						ScmapEditor.Current.map.TexturemapTex.Apply(false);
					}
					GenericInfoPopup.ShowInfo("Stratum mask import success!\n" + System.IO.Path.GetFileName(paths[0]));

				}
				else if(paths[0].ToLower().EndsWith("raw"))
				{
					int h = ScmapEditor.Current.map.TexturemapTex.width;
					int w = ScmapEditor.Current.map.TexturemapTex.height;
					int i = 0;

					Color[] data;

					if (Selected > 4)
						data = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
					else
						data = ScmapEditor.Current.map.TexturemapTex.GetPixels();


					//byte[,] data = new byte[h, w];
					using (var file = System.IO.File.OpenRead(paths[0]))
					using (var reader = new System.IO.BinaryReader(file))
					{
						for (int y = 0; y < h; y++)
						{
							for (int x = 0; x < w; x++)
							{
								i = x + y * w;

								//data[y, x] = reader.ReadByte();

								if (Selected == 1 || Selected == 5)
									data[i].r = reader.ReadByte() / 255f;
								else if (Selected == 2 || Selected == 6)
									data[i].g = reader.ReadByte() / 255f;
								else if (Selected == 3 || Selected == 7)
									data[i].b = reader.ReadByte() / 255f;
								else if (Selected == 4 || Selected == 8)
									data[i].a = reader.ReadByte() / 255f;

							}
						}
					}

					if (Selected > 4)
					{
						ScmapEditor.Current.map.TexturemapTex2.SetPixels(data);
						ScmapEditor.Current.map.TexturemapTex2.Apply(false);
					}
					else
					{
						ScmapEditor.Current.map.TexturemapTex.SetPixels(data);
						ScmapEditor.Current.map.TexturemapTex.Apply(false);
					}


					GenericInfoPopup.ShowInfo("Stratum mask import success!\n" + System.IO.Path.GetFileName(paths[0]));

				}
				else
				{
					// Wrong file type
					GenericInfoPopup.ShowInfo("Wrong file type!");

				}

				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths[0]));

				ScmapEditor.Current.SetTextures(Selected);

				ReloadStratums();

			}
		}

		const string ExportPathKey = "TexturesStratumMaskExport";
		static string DefaultPath
		{
			get
			{
				return EnvPaths.GetLastPath(ExportPathKey, EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName);
			}
		}

		public void ExportStratumMask()
		{
			if (Selected <= 0 || Selected > 8)
				return;


			var extensions = new[]
			{
				new ExtensionFilter("Stratum mask", "raw")
			};

			var path = StandaloneFileBrowser.SaveFilePanel("Export stratum mask", DefaultPath, "stratum_" + Selected, extensions);

			if (!string.IsNullOrEmpty(path))
			{

				int h = ScmapEditor.Current.map.TexturemapTex.width;
				int w = ScmapEditor.Current.map.TexturemapTex.height;
				int x = 0;
				int y = 0;
				int i = 0;

				//float[,] data = Map.Teren.terrainData.GetHeights(0, 0, w, h);
				Color[] data;

				if (Selected > 4)
					data = ScmapEditor.Current.map.TexturemapTex2.GetPixels();
				else
					data = ScmapEditor.Current.map.TexturemapTex.GetPixels();

				using (BinaryWriter writer = new BinaryWriter(new System.IO.FileStream(path, System.IO.FileMode.Create)))
				{
					for (y = 0; y < h; y++)
					{
						for (x = 0; x < w; x++)
						{
							i = x + y * w;

							//uint ThisPixel = (uint)(data[y, x] * 0xFFFF);
							byte ThisPixel = 0;

							if (Selected == 1 || Selected == 5)
								ThisPixel = (byte)(data[i].r * 255);
							else if (Selected == 2 || Selected == 6)
								ThisPixel = (byte)(data[i].g * 255);
							else if (Selected == 3 || Selected == 7)
								ThisPixel = (byte)(data[i].b * 255);
							else if (Selected == 4 || Selected == 8)
								ThisPixel = (byte)(data[i].a * 255);

							

							if (Selected == 1 || Selected == 5)
								writer.Write(ThisPixel);
							else if (Selected == 2 || Selected == 6)
								writer.Write(ThisPixel);
							else if (Selected == 3 || Selected == 7)
								writer.Write(ThisPixel);
							else if (Selected == 4 || Selected == 8)
								writer.Write(ThisPixel);
						}
					}
					writer.Close();
				}
				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(path));
				GenericInfoPopup.ShowInfo("Stratum mask export success!\n" + System.IO.Path.GetFileName(path));
			}

		}


		public void ExportStratum()
		{

			

			var extensions = new[]
{
				new ExtensionFilter("Stratum setting file", "scmsl")
			};

			var paths = StandaloneFileBrowser.SaveFilePanel("Import stratum mask", DefaultPath, "", extensions);


			if(!string.IsNullOrEmpty(paths))
			{
				string data = UnityEngine.JsonUtility.ToJson(ScmapEditor.Current.Textures[Selected], true);

				File.WriteAllText(paths, data);

				GenericInfoPopup.ShowInfo("Stratum settings export success!\n" + System.IO.Path.GetFileName(paths));
				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths));
			}
		}

		public void ImportStratum()
		{

			var extensions = new[]
{
				new ExtensionFilter("Stratum setting file", "scmsl")
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import stratum mask", DefaultPath, extensions, false);

			if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
			{
				string data = File.ReadAllText(paths[0]);

				ScmapEditor.TerrainTexture NewTexture = UnityEngine.JsonUtility.FromJson<ScmapEditor.TerrainTexture>(data);

				ScmapEditor.Current.Textures[Selected] = NewTexture;

				GetGamedataFile.LoadTextureFromGamedata(ScmapEditor.Current.Textures[Selected].AlbedoPath, Selected, false);
				GetGamedataFile.LoadTextureFromGamedata(ScmapEditor.Current.Textures[Selected].NormalPath, Selected, true);

				ScmapEditor.Current.SetTextures(Selected);

				ReloadStratums();

				GenericInfoPopup.ShowInfo("Stratum settings import success!\n" + System.IO.Path.GetFileName(paths[0]));
				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths[0]));
			}
		}

		class StratumTemplate
		{
			public string ShaderName;
			public ScmapEditor.TerrainTexture Stratum0;
			public ScmapEditor.TerrainTexture Stratum1;
			public ScmapEditor.TerrainTexture Stratum2;
			public ScmapEditor.TerrainTexture Stratum3;
			public ScmapEditor.TerrainTexture Stratum4;
			public ScmapEditor.TerrainTexture Stratum5;
			public ScmapEditor.TerrainTexture Stratum6;
			public ScmapEditor.TerrainTexture Stratum7;
			public ScmapEditor.TerrainTexture Stratum8;
			public ScmapEditor.TerrainTexture Stratum9;
		}

		public void ExportStratumTemplate()
		{
			var extensions = new[]
{
				new ExtensionFilter("Stratum template", "scmst")
			};

			var paths = StandaloneFileBrowser.SaveFilePanel("Export stratum mask", DefaultPath, "", extensions);


			if (!string.IsNullOrEmpty(paths))
			{

				StratumTemplate NewTemplate = new StratumTemplate();
				NewTemplate.ShaderName = MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.text;
				NewTemplate.Stratum0 = ScmapEditor.Current.Textures[0];
				NewTemplate.Stratum1 = ScmapEditor.Current.Textures[1];
				NewTemplate.Stratum2 = ScmapEditor.Current.Textures[2];
				NewTemplate.Stratum3 = ScmapEditor.Current.Textures[3];
				NewTemplate.Stratum4 = ScmapEditor.Current.Textures[4];
				NewTemplate.Stratum5 = ScmapEditor.Current.Textures[5];
				NewTemplate.Stratum6 = ScmapEditor.Current.Textures[6];
				NewTemplate.Stratum7 = ScmapEditor.Current.Textures[7];
				NewTemplate.Stratum8 = ScmapEditor.Current.Textures[8];
				NewTemplate.Stratum9 = ScmapEditor.Current.Textures[9];

				string data = UnityEngine.JsonUtility.ToJson(NewTemplate, true);

				File.WriteAllText(paths, data);
				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths));
			}
		}

		public void ImportStratumTemplate()
		{

			var extensions = new[]
{
				new ExtensionFilter("Stratum setting file", "scmst")
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import stratum mask", DefaultPath, extensions, false);

			if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
			{
				string data = File.ReadAllText(paths[0]);

				StratumTemplate NewTemplate = UnityEngine.JsonUtility.FromJson<StratumTemplate>(data);


                MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.SetValue(NewTemplate.ShaderName);
				ScmapEditor.Current.Textures[0] = NewTemplate.Stratum0;
				ScmapEditor.Current.Textures[1] = NewTemplate.Stratum1;
				ScmapEditor.Current.Textures[2] = NewTemplate.Stratum2;
				ScmapEditor.Current.Textures[3] = NewTemplate.Stratum3;
				ScmapEditor.Current.Textures[4] = NewTemplate.Stratum4;
				ScmapEditor.Current.Textures[5] = NewTemplate.Stratum5;
				ScmapEditor.Current.Textures[6] = NewTemplate.Stratum6;
				ScmapEditor.Current.Textures[7] = NewTemplate.Stratum7;
				ScmapEditor.Current.Textures[8] = NewTemplate.Stratum8;
				ScmapEditor.Current.Textures[9] = NewTemplate.Stratum9;



				ScmapEditor.Current.LoadStratumScdTextures(false);

				ScmapEditor.Current.SetTextures();
				EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths[0]));

				ReloadStratums();
			}
		}
		#endregion
	}
}
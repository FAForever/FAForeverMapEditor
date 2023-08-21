﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using B83.Image.BMP;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using SFB;
using Ozone.UI;

namespace EditMap
{
	public class TerrainInfo : MonoBehaviour
	{

		public GameObject[] Selection;
		public GameObject[] Page;

		public Editing Edit;
		public Camera GameplayCamera;

		public UiTextField BrushSize;
		public UiTextField BrushStrength;

		//public Slider BrushSizeSlider;
		//public InputField BrushSize;
		//public Slider BrushStrengthSlider;
		//public InputField BrushStrength;
		//public Slider BrushRotationSlider;
		public UiTextField BrushRotation;

		public UiTextField BrushTarget;
		public UiTextField BrushMini;
		public UiTextField BrushMax;

		public InputField TerrainSet;
		public InputField TerrainAdd;
		public InputField TerrainScale;
		public Toggle TerrainScale_Height;
		public InputField TerrainScale_HeightValue;

		public GameObject BrushListObject;
		public Transform BrushListPivot;
		public Material TerrainMaterial;

		public Toggle[] BrushTypes;

		public LayerMask TerrainMask;
		public List<Toggle> BrushToggles;
		public ToggleGroup ToogleGroup;

		[Header("State")]
		public bool Invert;
		public bool Smooth;


		//PaintWithBrush.BrushData TerrainBrush = new PaintWithBrush.BrushData();

		void OnEnable()
		{
			BrushGenerator.Current.LoadBrushes();

			if (!BrusheshLoaded) LoadBrushes();
			UpdateMenu();
			ResetBrushes();
			SetBrush();
		}

		void ResetBrushes()
		{
			LastRotation = BrushRotation.intValue;
		}

		void SetBrush()
		{
			TerrainMaterial.SetInt("_Brush", 1);
			BrushGenerator.SetFallof(SelectedFalloff, LastRotation);
			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.Brushes[SelectedFalloff]);

			for (int i = 0; i < BrushTypes.Length; i++)
				if (BrushTypes[i].isOn) {
					ChangeBrush(i);
					break;
				}
		}

		void OnDisable()
		{
			TerrainMaterial.SetInt("_Brush", 0);
		}

		//int PreviousPage = 0;
		int CurrentPage = 0;
		public static bool TerrainPageChange = false;
		public void ChangePage(int PageId)
		{
			if (CurrentPage == PageId && Page[CurrentPage].activeSelf && Selection[CurrentPage].activeSelf)
				return;
			TerrainPageChange = true;

			//PreviousPage = CurrentPage;
			CurrentPage = PageId;

			for (int i = 0; i < Page.Length; i++)
			{
				Page[i].SetActive(false);
				Selection[i].SetActive(false);
			}

			if (CurrentPage == 0)
			{
				SetBrush();
			}
			else if (CurrentPage == 2)
			{
				TerrainMaterial.SetInt("_Brush", 1);
			}
			else
			{
				TerrainMaterial.SetInt("_Brush", 0);
			}

			Page[CurrentPage].SetActive(true);
			Selection[CurrentPage].SetActive(true);
			TerrainPageChange = false;
		}


		#region Load all brushesh
		bool BrusheshLoaded = false;
		string StructurePath;
		public void LoadBrushes()
		{
			Clean();

			StructurePath = MapLuaParser.GetDataPath() + "/Structure/"; ;

			StructurePath += "brush";

			if (!Directory.Exists(StructurePath))
			{
				Debug.LogError("Cant find brush folder at: " + StructurePath);
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
				NewBrush.GetComponent<BrushListId>().Controler = this;
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

		#region Update tool
		bool PaintStarted = false;
		bool TerainChanged = false;
		float[,] beginHeights;

		Vector3 BeginMousePos;
		float StrengthBeginValue;
		bool ChangingStrength;
		float SizeBeginValue;
		bool ChangingSize;
		bool ChangingRotation;
		void Update()
		{
			Invert = Input.GetKey(KeyCode.LeftAlt);
			Smooth = Input.GetKey(KeyCode.LeftShift);

			if (CurrentPage != 0)
				return;

			if (PaintStarted && Input.GetMouseButtonUp(0))
			{
				ScmapEditor.SyncHeightmap(true);
				
			}

			if (Edit.MauseOnGameplay || ChangingStrength || ChangingSize || ChangingRotation)
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
						BrushStrength.SetValue((int)Mathf.Clamp(StrengthBeginValue - (BeginMousePos.x - Input.mousePosition.x), 0, 100));
						//BrushStrengthSlider.value = Mathf.Clamp(StrengthBeginValue - (BeginMousePos.x - Input.mousePosition.x), 0, 100);
						UpdateMenu(true);
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
						BrushSize.SetValue(Mathf.Clamp(SizeBeginValue - (BeginMousePos.x - Input.mousePosition.x), 1, 256));
						//BrushSizeSlider.value = Mathf.Clamp(SizeBeginValue - (BeginMousePos.x - Input.mousePosition.x), 1, 256);
						UpdateMenu(true);
						UpdateBrushPosition(true);

					}
				}
				else if (KeyboardManager.BrushRotate() || ChangingRotation)
				{
					if (Input.GetMouseButtonDown(0))
					{
						ChangingRotation = true;
						BeginMousePos = Input.mousePosition;
						SizeBeginValue = BrushSize.value;
					}
					else if (Input.GetMouseButtonUp(0))
					{
						ChangingRotation = false;
					}
					if (ChangingRotation)
					{
						BrushRotation.SetValue((SizeBeginValue - (BeginMousePos.x - Input.mousePosition.x)) % 360);
						UpdateMenu(true);
						UpdateBrushPosition(true);
					}
				}
				else
				{
					if (!PaintStarted && Input.GetKey(KeyCode.LeftControl))
					{
						if (CameraControler.Current.DragStartedGameplay)
						{
							if (UpdateBrushPosition(false))
							{
							}
						}

						if (Input.GetMouseButton(0))
						{
							BrushTarget.SetValue(CameraControler.GetLastScmHeight());
							UpdateMenu(true);
						}
						else if (Input.GetMouseButton(1))
						{
							BrushMini.SetValue(CameraControler.GetLastScmHeight());
							UpdateMenu(true);
						}
						else if (Input.GetMouseButton(2))
						{
							BrushMax.SetValue(CameraControler.GetLastScmHeight());
							UpdateMenu(true);
						}
					}
					else if (Edit.MauseOnGameplay && Input.GetMouseButtonDown(0))
					{
						if (UpdateBrushPosition(true))
						{
							ScmapEditor.Current.Teren.heightmapPixelError = 20;
							GenerateControlTex.StopGenerateNormal();
							ScmapEditor.Current.TerrainMaterial.SetFloat("_GeneratingNormal", 1);
							PaintStarted = true;
							RecalcTerrainClamp();
							SymmetryPaint();
						}
					}
					else if (Input.GetMouseButton(0))
					{
						if (CameraControler.Current.DragStartedGameplay)
						{
							if (UpdateBrushPosition(false))
							{
							}
							RecalcTerrainClamp();
							SymmetryPaint();
						}
					}
					else if (Input.GetMouseButtonUp(0))
					{

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
					int SelectedBrush = 0;
					for (int i = 0; i < BrushTypes.Length; i++)
					{
						if (BrushTypes[i].isOn)
						{
							SelectedBrush = i;
							//BrushTypes[i].isOn = false;
						}
					}

					SelectedBrush++;
					if (SelectedBrush >= BrushTypes.Length)
						SelectedBrush = 0;

					BrushTypes[SelectedBrush].isOn = true;
				}
				else if (KeyboardManager.SwitchType1())
				{
					BrushTypes[0].isOn = true;
				}
				else if (KeyboardManager.SwitchType2())
				{
					BrushTypes[1].isOn = true;
				}
				else if (KeyboardManager.SwitchType3())
				{
					BrushTypes[2].isOn = true;
				}
				else if (KeyboardManager.SwitchType4())
				{
					BrushTypes[3].isOn = true;
				}

				if (KeyboardManager.IncreaseTarget())
				{
					if (BrushTarget.value < 256)
						BrushTarget.SetValue((int)BrushTarget.value + 1);
				}
				else if (KeyboardManager.DecreaseTarget())
				{
					if (BrushTarget.value > 0)
						BrushTarget.SetValue((int)BrushTarget.value - 1);
				}
			}


			if (Input.GetMouseButtonUp(0))
			{
				if (TerainChanged)
				{
					Undo.RegisterUndo(new UndoHistory.HistoryTerrainHeight(), new UndoHistory.HistoryTerrainHeight.TerrainHeightHistoryParameter(beginHeights));
					TerainChanged = false;
				}

				if (Painting)
				{
					Painting = false;
					RegenerateMaps();
				}

				PaintStarted = false;
				ScmapEditor.Current.Teren.heightmapPixelError = 4;
			}



			BrushGenerator.RegeneratePaintBrushIfNeeded();
		}
		public float Min = 0;
		public float Max = 512;

		public float HeightmapMin = 0;
		public float HeightmapMax = 512;
		public float HeightmapDistance = 50;

		int LastRotation = 0;
		public void UpdateMenu(bool Slider = false)
		{
			if (Slider)
			{

			}
			else
			{

			}



			Min = BrushMini.value / ScmapEditor.Current.Data.size.y;
			Max = BrushMax.value / ScmapEditor.Current.Data.size.y;

			Min /= 10f;
			Max /= 10f;

			RecalcTerrainClamp();

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
		}
		#endregion

		#region Set Heightmap

		public void SetTerrainHeight()
		{
			int h = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			int w = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			ScmapEditor.GetAllHeights(ref beginHeights);

			float Value = float.Parse(TerrainSet.text) * 0.1f;
			Value /= ScmapEditor.TerrainHeight;

			float[,] heights = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, ScmapEditor.Current.Teren.terrainData.heightmapResolution, ScmapEditor.Current.Teren.terrainData.heightmapResolution);

			for (int i = 0; i < ScmapEditor.Current.Teren.terrainData.heightmapResolution; i++)
			{
				for (int j = 0; j < ScmapEditor.Current.Teren.terrainData.heightmapResolution; j++)
				{
					heights[i, j] = Value;
				}
			}

			Undo.RegisterUndo(new UndoHistory.HistoryTerrainHeight(), new UndoHistory.HistoryTerrainHeight.TerrainHeightHistoryParameter(beginHeights));

			ScmapEditor.SetAllHeights(heights);

			//ScmapEditor.Current.Teren.terrainData.SetHeights(0, 0, heights);
			RegenerateMaps();
			OnTerrainChanged();

		}

		public void AddTerrainHeight()
		{
			int h = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			int w = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			ScmapEditor.GetAllHeights(ref beginHeights);
			Undo.RegisterUndo(new UndoHistory.HistoryTerrainHeight(), new UndoHistory.HistoryTerrainHeight.TerrainHeightHistoryParameter(beginHeights));

			float Value = float.Parse(TerrainAdd.text) * 0.1f;
			Value /= ScmapEditor.TerrainHeight;

			float[,] heights = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, ScmapEditor.Current.Teren.terrainData.heightmapResolution, ScmapEditor.Current.Teren.terrainData.heightmapResolution);

			for (int i = 0; i < ScmapEditor.Current.Teren.terrainData.heightmapResolution; i++)
			{
				for (int j = 0; j < ScmapEditor.Current.Teren.terrainData.heightmapResolution; j++)
				{
					heights[i, j] += Value;
				}
			}
			ScmapEditor.SetAllHeights(heights);
			//ScmapEditor.Current.Teren.terrainData.SetHeights(0, 0, heights);
			RegenerateMaps();
			OnTerrainChanged();
		}

		public void ExportHeightmap()
		{
			//string Filename = EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName + "/heightmap.raw";
			//string Filename = EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName + "/heightmap.raw";

			var extensions = new[]
			{
				new ExtensionFilter("Heightmap", new string[]{"raw" }),
				new ExtensionFilter("WM Heightmap", new string[]{"r16" })
				//new ExtensionFilter("Stratum mask", "raw, bmp")
			};

			var paths = StandaloneFileBrowser.SaveFilePanel("Export heightmap", DefaultPath, "heightmap", extensions);


			if (paths == null || string.IsNullOrEmpty(paths))
				return;


			int h = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			int w = ScmapEditor.Current.Teren.terrainData.heightmapResolution;

			float[,] data = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, w, h);

			using (BinaryWriter writer = new BinaryWriter(new System.IO.FileStream(paths, System.IO.FileMode.Create)))
			{
				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						// float v = (float)(((double)reader.ReadUInt16() - 0.5) / (double)HeightConversion); << Import

						ushort ThisPixel = (ushort)(data[h - (y + 1), x] * ScmapEditor.HeightResize);
						writer.Write(System.BitConverter.GetBytes(ThisPixel));
					}
				}
				writer.Close();
			}
			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths));
			GenericInfoPopup.ShowInfo("Heightmap export success!\n" + System.IO.Path.GetFileName(paths));
		}

		public Texture2D ExportAs;
		const string ExportPathKey = "TerrainHeightmapExport";
		static string DefaultPath
		{
			get
			{
				string ToReturn = EnvPaths.GetLastPath(ExportPathKey, EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName);
				Debug.Log("Default path: " + EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName);
				Debug.Log("Last path: " + ToReturn);

				if (string.IsNullOrEmpty(ToReturn) || !Directory.Exists(ToReturn))
				{
					Debug.Log("Send my documents " + EnvPaths.MyDocuments);
					return EnvPaths.MyDocuments;
				}

				return ToReturn;
			}
		}

		public void ExportWithSizeHeightmap()
		{

			int scale = int.Parse(TerrainScale.text);
			scale = Mathf.Clamp(scale, 129, 2049);

			//string Filename = EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName + "/heightmap.raw";

			var extensions = new[]
{
				new ExtensionFilter("Heightmap", new string[]{"raw" })
				//new ExtensionFilter("Stratum mask", "raw, bmp")
			};

			var paths = StandaloneFileBrowser.SaveFilePanel("Export heightmap scalled", DefaultPath, "heightmap", extensions);


			if (paths == null || string.IsNullOrEmpty(paths))
				return;

			float h = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			float w = ScmapEditor.Current.Teren.terrainData.heightmapResolution;

			float[,] data = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, (int)w, (int)h);

			ExportAs = new Texture2D((int)w, (int)h, TextureFormat.RGB24, false, true);

			Debug.Log(w + ", " + h);

			float HeightValue = 1;
			HeightValue = float.Parse(TerrainScale_HeightValue.text);
			if (HeightValue < 0) HeightValue = 1;

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					float Value = data[y, x];// / HeightConversion;

					if (TerrainScale_Height.isOn)
					{
						Value *= HeightValue;
					}
					//float ColorR = (Mathf.Floor(Value) * (1f / 255f));
					//float ColorG = (Value - Mathf.Floor(Value));

					ExportAs.SetPixel((int)(h - (y + 1)), x, new Color(Value, 0, 0));
				}
			}
			ExportAs.Apply();

			bool differentSize = w != scale || h != scale;

			h = scale;
			w = scale;

			using (BinaryWriter writer = new BinaryWriter(new System.IO.FileStream(paths, System.IO.FileMode.Create)))
			{
				Color pixel = Color.black;

				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						if (differentSize)
							pixel = ExportAs.GetPixelBilinear(y / h, x / w);
						else
							pixel = ExportAs.GetPixel(y, x);
						//float value = (pixel.r + pixel.g * (1f / 255f));
						float value = pixel.r;
						//uint ThisPixel = (uint)(value * HeightConversion);
						ushort ThisPixel = (ushort)(value * ScmapEditor.HeightResize);
						writer.Write(System.BitConverter.GetBytes(ThisPixel));
					}
				}
				writer.Close();
			}
			//ExportAs = null;
			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths));

			GenericInfoPopup.ShowInfo("Rescaled Heightmap export success!\n" + System.IO.Path.GetFileName(paths));
		}

		public void ImportHeightmap()
		{
			var extensions = new[]
			{
				new ExtensionFilter("Heightmap", new string[]{"raw", "r16", "bmp" })
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import heightmap", DefaultPath, extensions, false);


			if (paths == null || paths.Length == 0 || string.IsNullOrEmpty(paths[0]))
				return;

			int h = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			int w = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			ScmapEditor.GetAllHeights(ref beginHeights);
			Undo.RegisterUndo(new UndoHistory.HistoryTerrainHeight(), new UndoHistory.HistoryTerrainHeight.TerrainHeightHistoryParameter(beginHeights));


			float[,] data = new float[h, w];
			//float[,] old = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, w, h);

			if (paths[0].ToLower().EndsWith("bmp"))
			{
				BMPLoader loader = new BMPLoader();
				BMPImage img = loader.LoadBMP(paths[0]);
				Debug.Log(img.info.compressionMethod + ", " + img.info.nBitsPerPixel + ", " + img.rMask + ", " + img.imageData[0].r);
				Texture2D ImportedImage = img.ToTexture2D();


				if (ImportedImage.width != h || ImportedImage.height != w)
				{
					Debug.Log("Wrong size");
					TextureScale.Bilinear(ImportedImage, h, w);
					ImportedImage.Apply(false);
				}

				Color[] ImportedColors = ImportedImage.GetPixels();

				for (int y = 0; y < h; y++)
				{
					for (int x = 0; x < w; x++)
					{
						data[y, x] = (float)ImportedColors[x + y * w].r / 0.567f; // 0.58
					}
				}
			}
			else
			{
				using (var file = System.IO.File.OpenRead(paths[0]))
				using (var reader = new System.IO.BinaryReader(file))
				{
					long CheckValue = 2;
					CheckValue *= (long)(w);
					CheckValue *= (long)(h);
					long FileLength = file.Length;

					if (FileLength != CheckValue)
					{
						reader.Dispose();
						file.Dispose();
						GenericPopup.ShowPopup(GenericPopup.PopupTypes.Error, "Error", "Selected heightmap is in wrong size.\nIs: " + FileLength + "B, should be: " + CheckValue + "B", "OK", null);
						return;
					}

					for (int y = 0; y < h; y++)
					{
						for (int x = 0; x < w; x++)
						{
							float v = (float)(reader.ReadUInt16() / ScmapEditor.HeightResize);
							data[h - (y + 1), x] = v;
						}
					}
				}
			}

			//ScmapEditor.Current.Teren.terrainData.SetHeights(0, 0, data);
			ScmapEditor.SetAllHeights(data);
			RegenerateMaps();
			OnTerrainChanged();
			EnvPaths.SetLastPath(ExportPathKey, Path.GetDirectoryName(paths[0]));
			GenericInfoPopup.ShowInfo("Heightmap import success!\n" + Path.GetFileName(paths[0]));


			if (ScmapEditor.IsOverMinMaxDistance())
			{
				GenericPopup.ShowPopup(GenericPopup.PopupTypes.TriButton, "Importing heightmap", "Distance between lowest and highest point is higher than 50.\nClamp it?", "Clamp Top", ClampTop, "Clamp Bottom", ClampBottom, "Ignore", null);
			}

		}
		#endregion

		#region Neroxis

		public void GenerateRandomHeightmap()
		{
			var values = new neroxis.MapGeneratorSettings() { width = ScmapEditor.Current.map.Width, mexCount = 0, reclaimDensity = 0 };

			neroxis.GenerateMapTask.GenerateSCMP(values, OnGeneratorHeightmapFinished);
		}

		public void GenerateRandomHeightmapStratum()
		{
			var values = new neroxis.MapGeneratorSettings() { width = ScmapEditor.Current.map.Width, mexCount = 0, reclaimDensity = 0 };

			neroxis.GenerateMapTask.GenerateSCMP(values, OnGeneratorHeightmapStratumFinished);
		}

		public void OnGeneratorHeightmapFinished()
		{
			LoadNeroxisHeightmap();

			RegenerateMaps();
			OnTerrainChanged();
		}

		public void OnGeneratorHeightmapStratumFinished()
		{
			LoadNeroxisHeightmap();

			RegenerateMaps();
			OnTerrainChanged();
		}

		void LoadNeroxisHeightmap()
		{
			ScmapEditor.GetAllHeights(ref beginHeights);

			float[,] heights = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, ScmapEditor.Current.Teren.terrainData.heightmapResolution, ScmapEditor.Current.Teren.terrainData.heightmapResolution);
			float[,] generatedHeights = neroxis.GenerateMapTask.GetGeneratedHeightmap();

			for (int i = 0; i < ScmapEditor.Current.Teren.terrainData.heightmapResolution; i++)
			{
				for (int j = 0; j < ScmapEditor.Current.Teren.terrainData.heightmapResolution; j++)
				{
					heights[i, j] = generatedHeights[i, j];
				}
			}

			Undo.RegisterUndo(new UndoHistory.HistoryTerrainHeight(), new UndoHistory.HistoryTerrainHeight.TerrainHeightHistoryParameter(beginHeights));

			ScmapEditor.SetAllHeights(heights);
		}

		#endregion

		void ClampTop()
		{
			//RecalcTerrainClamp();

			ScmapEditor.ClampTop((ScmapEditor.Current.Data.bounds.min.y + 5) / ScmapEditor.Current.Data.size.y);

			RegenerateMaps();
			OnTerrainChanged();
		}

		void ClampBottom()
		{
			//RecalcTerrainClamp();

			ScmapEditor.ClampBottom((ScmapEditor.Current.Data.bounds.max.y - 5) / ScmapEditor.Current.Data.size.y);

			RegenerateMaps();
			OnTerrainChanged();
		}

		public void RegenerateMaps()
		{
			GenerateControlTex.GenerateWater();
			GenerateControlTex.GenerateNormal();
			WavesRenderer.ClearShoreLine();

		}

		#region Brush Update
		int SelectedBrush = 0;
		public void ChangeBrush(int id)
		{
			if (gameObject.activeSelf && BrushTypes[id].isOn)
			{
				SelectedBrush = id;
			}
		}

		int SelectedFalloff = 0;
		public void ChangeFalloff(int id)
		{
			if (!gameObject.activeSelf)
				return;

			SelectedFalloff = id;

			for (int i = 0; i < BrushToggles.Count; i++)
			{
				if (i == SelectedFalloff)
					continue;
				BrushToggles[i].isOn = false;
			}
			LastRotation = BrushRotation.intValue;

			BrushGenerator.SetFallof(SelectedFalloff, LastRotation);

			TerrainMaterial.SetTexture("_BrushTex", (Texture)BrushGenerator.Current.RotatedBrush);
			//BrushGenerator.RegeneratePaintBrushIfNeeded(true);
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


		#region Painting
		bool _Painting = false;
		bool Painting
		{
			set
			{
				_Painting = value;
				TerrainMaterial.SetInt("_BrushPainting", _Painting ? (1) : (0));
			}
			get
			{
				return _Painting;
			}
		}

		void RecalcTerrainClamp()
		{
			FafEditorSettings.GetHeightmapClamp();

			float bmin = ScmapEditor.Current.Data.bounds.min.y;
			float bmax = ScmapEditor.Current.Data.bounds.max.y;

			HeightmapMin = (bmax - 5f) / ScmapEditor.Current.Data.size.y;
			HeightmapMax = (bmin + 5f) / ScmapEditor.Current.Data.size.y;

			if (HeightmapMin < 0f)
				HeightmapMin = 0f;
			if (HeightmapMax > 1f)
				HeightmapMax = 1f;

			//Debug.Log(bmin + " > " + bmax);
		}

		void SymmetryPaint()
		{
			Painting = true;
			BrushGenerator.Current.GenerateSymmetry(BrushPos);

			for (int i = 0; i < BrushGenerator.Current.PaintPositions.Length; i++)
			{
				Paint(BrushGenerator.Current.PaintPositions[i], i);
			}
		}

		void Paint(Vector3 AtPosition, int id = 0)
		{
			int BrushPaintType = SelectedBrush;


			if (Smooth)
				BrushPaintType = 2; // Smooth



			int hmWidth = ScmapEditor.Current.Teren.terrainData.heightmapResolution;
			int hmHeight = ScmapEditor.Current.Teren.terrainData.heightmapResolution;

			Vector3 tempCoord = ScmapEditor.Current.Teren.gameObject.transform.InverseTransformPoint(AtPosition);
			Vector3 coord = Vector3.zero;
			coord.x = tempCoord.x / ScmapEditor.Current.Teren.terrainData.size.x;
			//coord.y = tempCoord.y / ScmapEditor.Current.Teren.terrainData.size.y;
			coord.z = tempCoord.z / ScmapEditor.Current.Teren.terrainData.size.z;

			if (coord.x > 1) return;
			if (coord.x < 0) return;
			if (coord.z > 1) return;
			if (coord.z < 0) return;


			// get the position of the terrain heightmap where this game object is
			int posXInTerrain = (int)(coord.x * hmWidth);
			int posYInTerrain = (int)(coord.z * hmHeight);
			// we set an offset so that all the raising terrain is under this game object
			int size = BrushSize.intValue;

			if (BrushPaintType == 2 && size < 8)
				size = 8;


			int offset = size / 2;
			// get the heights of the terrain under this game object

			// Horizontal Brush Offsets
			int OffsetLeft = 0;
			if (posXInTerrain - offset < 0) OffsetLeft = Mathf.Abs(posXInTerrain - offset);
			int OffsetRight = 0;
			if (posXInTerrain - offset + size > ScmapEditor.Current.Teren.terrainData.heightmapResolution) OffsetRight = posXInTerrain - offset + size - ScmapEditor.Current.Teren.terrainData.heightmapResolution;

			// Vertical Brush Offsets
			int OffsetDown = 0;
			if (posYInTerrain - offset < 0) OffsetDown = Mathf.Abs(posYInTerrain - offset);
			int OffsetTop = 0;
			if (posYInTerrain - offset + size > ScmapEditor.Current.Teren.terrainData.heightmapResolution) OffsetTop = posYInTerrain - offset + size - ScmapEditor.Current.Teren.terrainData.heightmapResolution;

			//float[,] heights = ScmapEditor.Current.Teren.terrainData.GetHeights(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, (size - OffsetLeft) - OffsetRight, (size - OffsetDown) - OffsetTop);

			int GetX = posXInTerrain - offset + OffsetLeft;
			int GetY = posYInTerrain - offset + OffsetDown;


			int SizeDown = (size - OffsetDown) - OffsetTop;
			int SizeLeft = (size - OffsetLeft) - OffsetRight;

			ScmapEditor.GetValues(GetX, GetY, SizeDown, SizeLeft);


			float CenterHeight = 0;

			int i = 0;
			int j = 0;
			int x = 0;
			int y = 0;
			float SampleBrush = 0;
			float PixelPower = 0;

			if (SelectedBrush == 1)
			{
				float Count = 0;
				for (i = 0; i < ScmapEditor.LastGetWidth; i++)
				{
					for (j = 0; j < ScmapEditor.LastGetHeight; j++)
					{
						// Brush strength
						x = (int)(((i + OffsetDown) / (float)size) * BrushGenerator.Current.PaintImageWidths[id]);
						y = (int)(((j + OffsetLeft) / (float)size) * BrushGenerator.Current.PaintImageHeights[id]);

						if (x < 0 || y < 0 || x >= BrushGenerator.Current.PaintImageWidths[id] || y >= BrushGenerator.Current.PaintImageHeights[id])
							continue;
						SampleBrush = Mathf.Clamp01(BrushGenerator.Current.Values[id][y + BrushGenerator.Current.PaintImageHeights[id] * x] - 0.0255f);
						CenterHeight += ScmapEditor.ReturnValues[i, j] * SampleBrush;
						Count += SampleBrush;
					}
				}
				CenterHeight /= Count;
			}
			else if (SelectedBrush == 3)
			{
				float Count = 0;
				for (i = 0; i < ScmapEditor.LastGetWidth; i++)
				{
					for (j = 0; j < ScmapEditor.LastGetHeight; j++)
					{
						// Brush strength
						x = (int)(((i + OffsetDown) / (float)size) * BrushGenerator.Current.PaintImageWidths[id]);
						y = (int)(((j + OffsetLeft) / (float)size) * BrushGenerator.Current.PaintImageHeights[id]);

						if (x < 0 || y < 0 || x >= BrushGenerator.Current.PaintImageWidths[id] || y >= BrushGenerator.Current.PaintImageHeights[id])
							continue;
						SampleBrush = Mathf.Clamp01(BrushGenerator.Current.Values[id][y + BrushGenerator.Current.PaintImageHeights[id] * x] - 0.0255f);
						CenterHeight += ScmapEditor.ReturnValues[i, j] * SampleBrush;
						Count += SampleBrush;
					}
				}
				CenterHeight /= Count;
			}


			float TargetHeight = Mathf.Clamp(((Invert ? (256) : (BrushTarget.value)) / ScmapEditor.Current.Data.size.y) / 10f, Min, Max);

			float BrushStrenghtValue = BrushStrength.value;
			//float PaintStrength = BrushStrenghtValue * 0.00005f * (Invert ? (-1) : 1);

			float StrengthMultiplier = 1;
			switch (BrushPaintType)
			{
				case 1: // Flatten
					StrengthMultiplier = BrushGenerator.Current.HeightmapBlurStrength.Evaluate(BrushStrenghtValue) * 0.00008f;
					break;
				case 2: // Smooth
					StrengthMultiplier = BrushGenerator.Current.HeightmapBlurStrength.Evaluate(BrushStrenghtValue) * Mathf.Clamp(size / 10f, 0, 20) * 0.01f;
					break;
				case 3: // Sharp
					StrengthMultiplier = BrushGenerator.Current.HeightmapBlurStrength.Evaluate(BrushStrenghtValue) * 0.00008f;
					break;
				default:
					StrengthMultiplier = BrushGenerator.Current.HeightmapPaintStrength.Evaluate(BrushStrenghtValue) * 0.00008f * (Invert ? (-1) : 1);
					break;
			}

			//float SizeSmooth = Mathf.Clamp01(size / 10f) * 15;

			for (i = 0; i < ScmapEditor.LastGetWidth; i++)
			{
				for (j = 0; j < ScmapEditor.LastGetHeight; j++)
				{
					// Brush strength
					x = (int)(((i + OffsetDown) / (float)size) * BrushGenerator.Current.PaintImageWidths[id]);
					y = (int)(((j + OffsetLeft) / (float)size) * BrushGenerator.Current.PaintImageHeights[id]);

					if (x < 0 || y < 0 || x >= BrushGenerator.Current.PaintImageWidths[id] || y >= BrushGenerator.Current.PaintImageHeights[id])
						continue;
					SampleBrush = Mathf.Clamp01(BrushGenerator.Current.Values[id][y + BrushGenerator.Current.PaintImageHeights[id] * x] - 0.0255f);


					if (SampleBrush > 0)
					{
						//if (i < 0 || j < 0 || i >= ScmapEditor.LastGetWidth || j >= ScmapEditor.LastGetHeight)
						//	continue;

						switch (BrushPaintType)
						{
							case 1: // Flatten
								PixelPower = Mathf.Pow(Mathf.Abs(ScmapEditor.ReturnValues[i, j] - CenterHeight), 0.454545f) + 1;
								PixelPower /= 2f;
								ScmapEditor.ReturnValues[i, j] = MoveToValue(ScmapEditor.ReturnValues[i, j], CenterHeight, StrengthMultiplier * SampleBrush * PixelPower, 0, ScmapEditor.MaxElevation);
								break;
							case 2: // Smooth
								CenterHeight = GetNearValues(ref ScmapEditor.ReturnValues, i, j);
								PixelPower = Mathf.Pow(Mathf.Abs(ScmapEditor.ReturnValues[i, j] - CenterHeight), 0.454545f) + 1;
								PixelPower /= 2f;
								ScmapEditor.ReturnValues[i, j] = Mathf.Lerp(ScmapEditor.ReturnValues[i, j], CenterHeight, PixelPower * StrengthMultiplier * Mathf.Pow(SampleBrush, 2));
								break;
							case 3: // Sharp
								PixelPower = Mathf.Pow(Mathf.Abs(ScmapEditor.ReturnValues[i, j] - CenterHeight), 0.454545f) + 1;
								PixelPower /= 2f;
								ScmapEditor.ReturnValues[i, j] = MoveToValue(ScmapEditor.ReturnValues[i, j], CenterHeight, -StrengthMultiplier * SampleBrush * PixelPower, Min, Max);
								break;
							default:
								ScmapEditor.ReturnValues[i, j] = MoveToValue(ScmapEditor.ReturnValues[i, j], TargetHeight, SampleBrush * StrengthMultiplier, Min, Max);
								break;
						}

						/*if (FafEditorSettings.IsHeightmapClamp)
						{
							if (ScmapEditor.ReturnValues[i, j] > HeightmapMax)
								ScmapEditor.ReturnValues[i, j] = HeightmapMax;
							else if (ScmapEditor.ReturnValues[i, j] < HeightmapMin)
								ScmapEditor.ReturnValues[i, j] = HeightmapMin;
						}*/

					}
				}
			}

			// set the new height
			if (!TerainChanged)
			{
				ScmapEditor.GetAllHeights(ref beginHeights);

				TerainChanged = true;
			}

			//ScmapEditor.Current.Teren.terrainData.SetHeightsDelayLOD(posXInTerrain - offset + OffsetLeft, posYInTerrain - offset + OffsetDown, heights);

			ScmapEditor.ApplyChanges(GetX, GetY);

			//Markers.MarkersControler.UpdateMarkersHeights();

			OnTerrainChanged();
		}

		float MoveToValue(float current, float target, float speed, float min, float max)
		{
			current = Mathf.Clamp(current, min, max);

			if (current > target)
			{
				if (FafEditorSettings.IsHeightmapClamp)
				{
					if (speed > 0)
					{
						target = Mathf.Max(target, HeightmapMin);
					}
					else
					{
						max = Mathf.Min(max, HeightmapMax);
					}
				}

				return Mathf.Clamp(current - speed, target, max);
			}
			else
			{
				if (FafEditorSettings.IsHeightmapClamp)
				{
					if (speed > 0)
					{
						target = Mathf.Min(target, HeightmapMax);
					}
					else
					{
						min = Mathf.Max(min, HeightmapMin);
					}
				}

				return Mathf.Clamp(current + speed, min, target);
			}
		}


		public void OnTerrainChanged()
		{
			Markers.MarkersControler.UpdateMarkersHeights();
			PropsRenderer.Current.UpdatePropsHeights();
			UnitsControler.UpdateUnitsHeights();
			DecalsControler.UpdateDecals();

			if (ScmapEditor.Current.Slope)
				GenerateControlTex.Current.GenerateSlopeTexture();
		}

		float GetNearValues(ref float[,] heigths, int x, int y, int range = 1)
		{
			float ToReturn = 0f;
			float Count = 0;

			if (x > 0)
			{
				ToReturn += heigths[x - 1, y];
				Count++;
			}
			if (x < heigths.GetLength(0) - 1)
			{
				ToReturn += heigths[x + 1, y];
				Count++;
			}

			if (y > 0)
			{
				ToReturn += heigths[x, y - 1];
				Count++;
			}
			if (y < heigths.GetLength(1) - 1)
			{
				ToReturn += heigths[x, y + 1];
				Count++;
			}

			return ToReturn / Count;
		}
		#endregion
	}
}
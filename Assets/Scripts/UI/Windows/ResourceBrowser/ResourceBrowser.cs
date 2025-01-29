﻿//********************************
// 
// * Resource browser
// * Copyright ozonexo3 2017
//
//********************************

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
//using System.IO.Compression;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.BZip2;
using System.Linq;

namespace FAF.MapEditor
{
	public partial class ResourceBrowser : MonoBehaviour
	{

		public static ResourceBrowser Current;

		// Drag data
		public static ResourceObject DragedObject;
		public static int SelectedCategory = 0;

		[Header("UI")]
		public GameObject Prefab_Texture;
		public GameObject Prefab_Decal;
		public GameObject Prefab_Prop;
		public Material PropMaterial;
		public Transform Pivot;
		public ScrollRect SRect;
		public Dropdown EnvType;
		public Dropdown Category;
		public GameObject Loading;
		public Texture2D CursorImage;
		public Texture2D CursorImage_Prop;
		public Texture2D CursorImage_Decal;
		public LayoutGroup Layout;
		public ContentSizeFitter SizeFitter;

		public Texture2D GetCursorImage()
		{
			if (Category.value == 1 || Category.value == 2)
				return CursorImage_Decal;
			else if (Category.value == 3)
				return CursorImage_Prop;
			else
				return CursorImage;
		}

		public static bool IsDecal()
		{
			return Current.Category.value == 1 || Current.Category.value == 2;
		}

		public static bool IsProp()
		{
			return Current.Category.value == 3;
		}

		[Header("Loaded assets")]
		public List<Texture2D> LoadedTextures = new List<Texture2D>();
		public List<string> LoadedPaths = new List<string>();
		public List<GetGamedataFile.PropObject> LoadedProps = new List<GetGamedataFile.PropObject>();
		public int LastLoadedType;

		//Local
		List<string> LoadedEnvPaths = new List<string>(24);
		const string LocalPath = "env/";
		static string[] CategoryPaths = new string[] { "layers/", "splats/", "decals/", "Props/" };
		string SelectedObject = "";
		bool CustomLoading = false;

		const string CurrentMapPath = "current";
		const string CurrentMapFolderPath = "maps/";
		const string SkyCubesFolderPathName = "SkyCubes";
		const string WavesFolderPathName = "Waves";
		const string WaterRampFolderPathName = "WaterRamps";
		const int PauseEveryLoadedAsset = 1;


		#region UI
		void LateUpdate()
		{
			if (Input.GetMouseButtonUp(0))
			{
				if (DragedObject)
				{
					ResourceBrowser.ClearDrag();
				}
			}
		}

		public static void ClearDrag()
		{
			Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			DragedObject = null;
		}

		public void OnDropdownChanged()
		{
			if (!gameObject.activeSelf || CustomLoading)
				return;
			SelectedObject = "";
			if (IsGenerating)
				StopCoroutine(GeneratingList);
			DontReload = false;
			Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
			GeneratingList = StartCoroutine(GenerateList());
		}
		#endregion


		#region Init
		public void Instantiate()
		{
			Current = this;
			ReadAllFolders();
		}

		bool Initialised = false;
		void ReadAllFolders()
		{
			if (Initialised)
				return;

			EnvType.ClearOptions();

			LoadedEnvPaths.Clear();
			List<Dropdown.OptionData> NewOptions = new List<Dropdown.OptionData>();

			if (!EnvPaths.GamedataExist)
			{
				Debug.LogWarning("Gamedata path not exist!");
				return;
			}

			string[] files = GetGamedataFile.GetFilesInPath("env/");

			char Separator = '/';
			for (int f = 0; f < files.Length; f++)
			{
				string LocalName = files[f].Replace("env/", "");

				if (string.IsNullOrEmpty(LocalName) || LocalName.StartsWith("."))
					continue;

				LocalName = LocalName.Split(Separator)[0];

				if (LoadedEnvPaths.Contains(LocalName))
					continue;

				LoadedEnvPaths.Add(LocalName);
				Dropdown.OptionData NewOptionInstance = new Dropdown.OptionData(LocalName);
				NewOptions.Add(NewOptionInstance);
			}

			LoadedEnvPaths.Add(SkyCubesFolderPathName);
			Dropdown.OptionData NewOptionInstance2 = new Dropdown.OptionData(SkyCubesFolderPathName);
			NewOptions.Add(NewOptionInstance2);

			LoadedEnvPaths.Add(WavesFolderPathName);
			Dropdown.OptionData NewOptionInstance3 = new Dropdown.OptionData(WavesFolderPathName);
			NewOptions.Add(NewOptionInstance3);
			
			LoadedEnvPaths.Add(WaterRampFolderPathName);
			Dropdown.OptionData NewOptionInstance4 = new Dropdown.OptionData(WaterRampFolderPathName);
			NewOptions.Add(NewOptionInstance4);

			LoadedEnvPaths.Add(CurrentMapFolderPath);
			Dropdown.OptionData NewOptionInstance5 = new Dropdown.OptionData("Map folder");
			NewOptions.Add(NewOptionInstance5);

			LoadedEnvPaths.Add(CurrentMapPath);
			Dropdown.OptionData NewOptionInstance6 = new Dropdown.OptionData("On map");
			NewOptions.Add(NewOptionInstance6);

			EnvType.AddOptions(NewOptions);

			Initialised = true;
		}
		#endregion


		bool DontReload = false;

		public void ShowBrowser()
		{
			ReadAllFolders();
			gameObject.SetActive(true);
			if (LoadedEnvPaths[EnvType.value] == CurrentMapFolderPath)
			{
				
				DontReload = false;
				if (IsGenerating)
					StopCoroutine(GeneratingList);
				CustomLoading = false;
				GeneratingList = StartCoroutine(GenerateList());
			}
		}

		public void LoadStratumTexture(string path)
		{
			ReadAllFolders();

			int LastCategory = Category.value;
			int LastEnvType = EnvType.value;

			CustomLoading = true;
			SelectedObject = path;

			path = path.Replace("env/", "");
			path = SelectEnvType(path);
			SelectCategory(path);

			gameObject.SetActive(true);

			if(LoadedEnvPaths.Count <= EnvType.value)
			{
				Debug.LogError("Env paths count is lower than selected Env type!");
			}
			else if(LoadedEnvPaths[EnvType.value] != CurrentMapFolderPath)
				DontReload = LastCategory == Category.value && LastEnvType == EnvType.value && !IsGenerating;

			if (!DontReload)
				Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

			if (IsGenerating)
				StopCoroutine(GeneratingList);
			CustomLoading = false;
			GeneratingList = StartCoroutine(GenerateList());
		}

		private string SelectEnvType(string path)
		{
			string EnvTypeFolder = "";

			while (EnvTypeFolder.Length < path.Length)
			{
				if (path[EnvTypeFolder.Length] == "/"[0])
				{
					path = path.Replace(EnvTypeFolder + "/", "");
					break;
				}
				EnvTypeFolder += path[EnvTypeFolder.Length];
			}

			for (int i = 0; i < EnvType.options.Count; i++)
			{
				if (EnvType.options[i].text.ToLower() == EnvTypeFolder.ToLower())
				{
					EnvType.value = i;
					break;
				}
			}

			return path;
		}

		private void SelectCategory(string path)
		{
			string CategoryFolder = "";
			while (CategoryFolder.Length < path.Length)
			{
				if (path[CategoryFolder.Length] == "/"[0])
				{
					CategoryFolder += "/";
					break;
				}
				CategoryFolder += path[CategoryFolder.Length];
			}

			for (int i = 0; i < Category.options.Count; i++)
			{
				if (CategoryPaths[i].ToLower() == CategoryFolder.ToLower())
				{
					Category.value = i;
					break;
				}
			}
		}

		public void LoadPropBlueprint()
		{
			ReadAllFolders();
			if (Category.value != 3)
			{
				Category.value = 3;

				gameObject.SetActive(true);
				if (IsGenerating)
					StopCoroutine(GeneratingList);
				CustomLoading = false;
				GeneratingList = StartCoroutine(GenerateList());
			}
			else
				gameObject.SetActive(true);
		}

		public void LoadDecalTexture(string path)
		{
			ReadAllFolders();
			int LastCategory = Category.value;
			int LastEnvType = EnvType.value;

			CustomLoading = true;

			if (string.IsNullOrEmpty(path))
			{
				DontReload = false;
				Category.value = 2;
			}
			else
			{
				SelectedObject = path;

				path = path.Replace("env/", "");
				path = SelectEnvType(path);
				SelectCategory(path);

				if (LoadedEnvPaths[EnvType.value] != CurrentMapFolderPath)
					DontReload = LastCategory == Category.value && LastEnvType == EnvType.value && !IsGenerating;

			}

			gameObject.SetActive(true);

			if (!DontReload)
				Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

			if (IsGenerating)
				StopCoroutine(GeneratingList);
			CustomLoading = false;
			GeneratingList = StartCoroutine(GenerateList());
		}

		#region Generate List of Assets

		void Clean()
		{
			CleanAssetsMemory();

			foreach (Transform child in Pivot)
			{
				Destroy(child.gameObject);
			}

			//LoadedTextures = new List<Texture2D>();
			//LoadedPaths = new List<string>();
			//LoadedProps = new List<GetGamedataFile.PropObject>();
		}

		bool IsGenerating
		{
			get
			{
				return GeneratingList != null;
			}
		}

		int GeneratedId = 0;
		string SelectedDirectory = "";
		Coroutine GeneratingList;
		IEnumerator GenerateList()
		{
			SelectedCategory = Category.value;
			if (!DontReload)
				Clean();
			else if (LastSelection)
				LastSelection.SetActive(false);
			Loading.SetActive(true);

			GeneratedId = 0;
			int Counter = 0;

			Layout.enabled = true;
			SizeFitter.enabled = true;

			LastLoadedType = Category.value;
			if (LoadedEnvPaths[EnvType.value] == CurrentMapFolderPath)
			{
				if (MapLuaParser.IsMapLoaded)
				{
					string LoadPath = MapLuaParser.LoadedMapFolderPath + "env/" + CategoryPaths[LastLoadedType];
					if (Directory.Exists(LoadPath))
					{
						string[] AllFiles = Directory.GetFiles(LoadPath, "*", SearchOption.AllDirectories).OrderBy(filePath => filePath).ToArray();
						foreach (string path in AllFiles)
						{
							switch (LastLoadedType)
							{
								case 0:
									if (path.ToLower().EndsWith(".dds"))
										Counter += GenerateMapTextureButton(LoadPath, path, Prefab_Texture);
									break;
								case 1:
									if (path.ToLower().EndsWith(".dds"))
										Counter += GenerateMapTextureButton(LoadPath, path, Prefab_Decal);
									break;
								case 2:
									if (path.ToLower().EndsWith(".dds"))
										Counter += GenerateMapTextureButton(LoadPath, path, Prefab_Decal);
									break;
								case 3:
									if (path.ToLower().EndsWith(".bp"))
										Counter += GenerateMapPropButton(LoadPath, path, Prefab_Prop);
									break;
							}

							if (Counter >= PauseEveryLoadedAsset)
							{
								Counter = 0;
								yield return null;
							}
						}
						yield return null;
						yield return null;
					}
				}
			}
			else if (LoadedEnvPaths[EnvType.value] == CurrentMapPath)
			{
				if (LastLoadedType == 3)
				{
					int Count = EditMap.PropsInfo.AllPropsTypes.Count;
					//Debug.Log("Found props: " + Count);

					for (int i = 0; i < Count; i++)
					{
						LoadAtPath(GetGamedataFile.LocalBlueprintPath(EditMap.PropsInfo.AllPropsTypes[i].Blueprint), EditMap.PropsInfo.AllPropsTypes[i].PropObject.BP.Name);

						Counter++;
						if (Counter >= PauseEveryLoadedAsset)
						{
							Counter = 0;
							yield return null;
						}
					}
				}
				else
				{
					yield return null;
					yield return null;
				}

			}
			else if (LoadedEnvPaths[EnvType.value] == SkyCubesFolderPathName)
			{
				SelectedDirectory = ("textures/environment/");
				string[] files = GetGamedataFile.GetFilesInPath(SelectedDirectory).OrderBy(filePath => filePath).ToArray();
				foreach (string path in files)
				{
					if (path.ToLower().EndsWith(".dds") && path.ToLower().Contains("cube"))
						Counter += GenerateMapTextureButton(path, path, Prefab_Texture);
					
					if (Counter >= PauseEveryLoadedAsset)
					{
						Counter = 0;
						yield return null;
					}
				}
				yield return null;
			}
			else if (LoadedEnvPaths[EnvType.value] == WavesFolderPathName)
			{
				SelectedDirectory = ("textures/engine/");
				string[] files = GetGamedataFile.GetFilesInPath(SelectedDirectory).OrderBy(filePath => filePath).ToArray();
				foreach (string path in files)
				{
					if (path.ToLower().EndsWith(".dds") && path.ToLower().Contains("wave"))
						Counter += GenerateMapTextureButton(path, path, Prefab_Texture);
					
					if (Counter >= PauseEveryLoadedAsset)
					{
						Counter = 0;
						yield return null;
					}
				}
				yield return null;
			}
			else if (LoadedEnvPaths[EnvType.value] == WaterRampFolderPathName)
			{
				SelectedDirectory = ("textures/engine/");
				string[] files = GetGamedataFile.GetFilesInPath(SelectedDirectory).OrderBy(filePath => filePath).ToArray();
				foreach (string path in files)
				{
					if (path.ToLower().EndsWith(".dds") && path.ToLower().Contains("waterramp"))
						Counter += GenerateMapTextureButton(path, path, Prefab_Texture);
					
					if (Counter >= PauseEveryLoadedAsset)
					{
						Counter = 0;
						yield return null;
					}
				}
				yield return null;
			}
			else
			{
				SelectedDirectory = ("env/" + EnvType.options[EnvType.value].text + "/" + CategoryPaths[LastLoadedType]).ToLower();

				string[] files = GetGamedataFile.GetFilesInPath(SelectedDirectory).OrderBy(filePath => filePath).ToArray();
				bool Breaked = false;

				foreach (string LocalName in files)
				{
					if (!IsProperFile(LocalName))
						continue;

					if (LoadZipEntry(LocalName, out Breaked))
					{
						continue;
					}

					if (Breaked)
						break;

					GeneratedId++;
					Counter++;
					if (Counter >= PauseEveryLoadedAsset)
					{
						Counter = 0;
						yield return null;
					}
				}
			}

			yield return null;
			Layout.enabled = false;
			SizeFitter.enabled = false;

			Loading.SetActive(false);
			GeneratingList = null;
		}

		bool LoadZipEntry(string localPath, out bool Breaked)
		{
			Breaked = false;

			if (DontReload)
			{
				if (localPath == SelectedObject.ToLower())
				{
					LastSelection = Pivot.GetChild(GeneratedId).GetComponent<ResourceObject>().Selected;
					LastSelection.SetActive(true);
					Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.up * 250 * Mathf.FloorToInt(GeneratedId / 5f);
					Breaked = true;
					return false;
				}
			}
			else
			{
				string LocalName = localPath.Remove(0, LocalPath.Length);

				LoadAtPath(localPath, LocalName);
			}
			return false;
		}

		bool IsProperFile(string LocalName)
		{
			LocalName = LocalName.ToLower();
			if (!LocalName.StartsWith(SelectedDirectory))
				return false;

			switch (Category.value)
			{
				case 0:
					return LocalName.EndsWith(".dds");
				case 1:
					return LocalName.EndsWith(".dds");
				case 2:
					return LocalName.EndsWith(".dds");
				case 3:
					return LocalName.EndsWith(".bp");
			}
			return false;
		}

		void LoadAtPath(string localPath, string LocalName)
		{
			switch (Category.value)
			{
				case 0:
					if (GenerateTextureButton(localPath, LocalName, Prefab_Texture))
					{ }
					break;
				case 1:
					if (GenerateTextureButton(localPath, LocalName, Prefab_Decal))
					{ }
					break;
				case 2:
					if (GenerateTextureButton(localPath, LocalName, Prefab_Decal))
					{ }
					break;
				case 3:
					if (GeneratePropButton(localPath, LocalName, Prefab_Prop))
					{ }
					break;
			}
		}

		#endregion

		#region Buttons
		GameObject LastSelection;
		bool GenerateTextureButton(string localpath, string LocalName, GameObject Prefab)
		{
			if (!LocalName.ToLower().EndsWith(".dds"))
				return true;
			Texture2D LoadedTex;

			try
			{
				LoadedTex = GetGamedataFile.LoadTexture2D(localpath, false, false);
			}
			catch (System.Exception e)
			{
				LoadedTex = new Texture2D(128, 128);
				Debug.LogWarning("Can't load DDS texture: " + e);
			}

			string TexPath = "";
			if (localpath.EndsWith(".dds"))
				TexPath = LocalName.Replace(".dds", "");
			else if (localpath.EndsWith(".DDS"))
				TexPath = LocalName.Replace(".DDS", "");


			GameObject NewButton = Instantiate(Prefab) as GameObject;
			NewButton.transform.SetParent(Pivot, false);
			NewButton.GetComponent<ResourceObject>().SetImages(LoadedTex);
			NewButton.GetComponent<ResourceObject>().InstanceId = LoadedTextures.Count;
			NewButton.GetComponent<ResourceObject>().NameField.text = TexPath;
			LoadedTextures.Add(LoadedTex);
			LoadedPaths.Add(localpath);

			if (localpath.ToLower() == SelectedObject.ToLower())
			{
				LastSelection = NewButton.GetComponent<ResourceObject>().Selected;
				LastSelection.SetActive(true);
				Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.up * 250 * Mathf.FloorToInt(LoadedPaths.Count / 5f);
			}
			return false;
		}

		const int MaxPropMass = 300;
		const int MaxPropEnergy = 200;

		bool GeneratePropButton(string localpath, string LocalName, GameObject Prefab)
		{
			if (!localpath.ToLower().EndsWith(".bp"))
				return false;

			string PropPath = "";
			if (localpath.EndsWith(".bp"))
				PropPath = LocalName.Replace(".bp", "");
			else if (localpath.EndsWith(".BP"))
				PropPath = LocalName.Replace(".BP", "");

			GetGamedataFile.PropObject LoadedProp = null;

			if (localpath.ToLower().StartsWith("maps"))
			{
				localpath = "/" + localpath;
				LoadedProp = GetGamedataFile.LoadProp(localpath, true);
			}
			else
				LoadedProp = GetGamedataFile.LoadProp(localpath, true);

			GameObject NewButton = Instantiate(Prefab) as GameObject;
			NewButton.transform.SetParent(Pivot, false);

			if (LoadedProp.BP.LODs.Length > 0 && LoadedProp.BP.LODs[0].Albedo)
			{
				NewButton.GetComponent<RawImage>().texture = LoadedProp.BP.LODs[0].Albedo;

			}

			ResourceObject Ro = NewButton.GetComponent<ResourceObject>();

			Ro.InstanceId = LoadedPaths.Count;
			Ro.NameField.text = LoadedProp.BP.Name;
			PropPath = PropPath.Replace(LoadedProp.BP.Name, "");
			Ro.CustomTexts[2].text = PropPath;

			Ro.CustomTexts[0].text = LoadedProp.BP.ReclaimMassMax.ToString();
			Ro.CustomTexts[1].text = LoadedProp.BP.ReclaimEnergyMax.ToString();

			if (LoadedProp.BP.RECLAIMABLE)
			{
				Ro.CustomTexts[0].color = ReclaimColor(LoadedProp.BP.ReclaimMassMax, MaxPropMass);
				Ro.CustomTexts[1].color = ReclaimColor(LoadedProp.BP.ReclaimEnergyMax, MaxPropEnergy);
				Ro.CustomTexts[3].gameObject.SetActive(false);
			}
			else
			{
				Ro.CustomTexts[0].color = ReclaimEmpty;
				Ro.CustomTexts[1].color = ReclaimEmpty;
				Ro.CustomTexts[3].gameObject.SetActive(true);
			}


			LoadedPaths.Add(localpath);
			LoadedProps.Add(LoadedProp);

			if (localpath.ToLower() == SelectedObject.ToLower())
			{
				Ro.Selected.SetActive(true);
				Pivot.GetComponent<RectTransform>().anchoredPosition = Vector2.up * 250 * Mathf.FloorToInt(LoadedPaths.Count / 5f);
			}
			return true;
		}
		#endregion

		HashSet<Texture2D> UsedTerrainTexturesMemory = new HashSet<Texture2D>();
		void CleanAssetsMemory()
		{
			if (LastLoadedType == 0)
			{
				//Textures
				UsedTerrainTexturesMemory.Clear();

				for (int i = 0; i < ScmapEditor.Current.Textures.Length; i++)
				{
					if (ScmapEditor.Current.Textures[i].Albedo != null)
						UsedTerrainTexturesMemory.Add(ScmapEditor.Current.Textures[i].Albedo);
					if (ScmapEditor.Current.Textures[i].Normal != null)
						UsedTerrainTexturesMemory.Add(ScmapEditor.Current.Textures[i].Normal);
				}


				int count = LoadedTextures.Count;
				for (int i = 0; i < count; i++)
				{
					if(LoadedTextures[i] != null && !UsedTerrainTexturesMemory.Contains(LoadedTextures[i]))
						Destroy(LoadedTextures[i]);
				}
				UsedTerrainTexturesMemory.Clear();
			}
			else if(LastLoadedType == 1 || LastLoadedType == 2)
			{
				// Decals
				UsedTerrainTexturesMemory.Clear();

				HashSet<Decal.DecalSharedSettings>.Enumerator ListEnum = Decal.AllDecalsShared.GetEnumerator();
				while (ListEnum.MoveNext())
				{
					Decal.DecalSharedSettings Current = ListEnum.Current;
					if (Current != null)
					{
						if(Current.Texture1)
						UsedTerrainTexturesMemory.Add(Current.Texture1);
						if (Current.Texture2)
							UsedTerrainTexturesMemory.Add(Current.Texture2);
					}
				}

				int count = LoadedTextures.Count;
				for (int i = 0; i < count; i++)
				{
					if (LoadedTextures[i] != null && !UsedTerrainTexturesMemory.Contains(LoadedTextures[i]))
						Destroy(LoadedTextures[i]);
				}
				UsedTerrainTexturesMemory.Clear();
			}
			else if (LastLoadedType == 3)
			{
				int count = LoadedProps.Count;
				for (int i = 0; i < count; i++)
				{
					for (int l = 0; l < LoadedProps[i].BP.LODs.Length; l++)
					{
						if(!GetGamedataFile.IsStoredInMemory(LoadedProps[i].BP.LODs[l].Albedo) && LoadedProps[i].BP.LODs[l].Albedo != Texture2D.whiteTexture)
							Destroy(LoadedProps[i].BP.LODs[l].Albedo);
						if (!GetGamedataFile.IsStoredInMemory(LoadedProps[i].BP.LODs[l].Normal) && LoadedProps[i].BP.LODs[l].Normal != GetGamedataFile.emptyNormalTexture)
							Destroy(LoadedProps[i].BP.LODs[l].Normal);
					}
				}
			}

			LoadedTextures.Clear();
			LoadedPaths.Clear();
			LoadedProps.Clear();
		}

	}
}
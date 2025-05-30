﻿// ***************************************************************************************
// * SCmap editor
// * Set Unity objects and scripts using data loaded from Scm
// ***************************************************************************************

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.ImageEffects;
//using UnityEngine.PostProcessing;
using FAF.MapEditor;

public partial class ScmapEditor : MonoBehaviour
{

	public static ScmapEditor Current;

	[Header("Connections")]
	public Camera Cam;
	public Terrain Teren;
	[System.NonSerialized]
	public TerrainData Data;
	public Transform WaterLevel;
	public ResourceBrowser ResBrowser;
	public UnitBrowser UnBrowser;
	public Light Sun;
	public ProceduralSkybox Skybox;
	public Material TerrainMaterial;
	public Material WaterMaterial;
	public Cubemap DefaultWaterSky;
	//public PostProcessingProfile PostProcessing;
	public BloomOptimized BloomOpt;
	public BloomOptimized BloomOptPreview;
	public PreviewTex PreviewRenderer;
	public TextAsset DefaultSkybox;

	[Header("Loaded variables")]
	public TerrainTexture[] Textures; // Loaded textures
	public Map map; // Loaded Scmap data
	//public SkyboxData.SkyboxValues DefaultSkyboxData;
	public Cubemap CurrentEnvironmentCubemap;



	public const float MapHeightScale = 2048;


	// Stratum Layer
	[System.Serializable]
	public class TerrainTexture
	{
		public Texture2D Albedo;
		public Texture2D Normal;
		public Vector2 Tilling = Vector2.one;
		//Scmap Data
		public string AlbedoPath;
		public string NormalPath;
		public float AlbedoScale;
		public float NormalScale;
	}


	void Awake()
	{
		Current = this;
		ResBrowser.Instantiate();
		Data = Teren.terrainData;
		EnvPaths.CurrentGamedataPath = EnvPaths.GamedataPath;
	}


	void Start()
	{
		UnBrowser.Instantiate();

		Grid = false;
		UpdateGrid();
		heightsLength = 10;
		heights = new float[10, 10];
		RestartTerrainAsset();
	}

	public void UpdateLighting()
	{
		Vector3 SunDIr = new Vector3(-map.SunDirection.x, -map.SunDirection.y, map.SunDirection.z);
		Sun.transform.rotation = Quaternion.LookRotation(SunDIr);

		/*BloomModel.Settings Bs = PostProcessing.bloom.settings;
		Bs.bloom.intensity = map.Bloom * 10;
		PostProcessing.bloom.settings = Bs;*/

		BloomOpt.intensity = map.Bloom * 4;
		BloomOptPreview.intensity = map.Bloom * 4;

		RenderSettings.fogColor = new Color(map.FogColor.x, map.FogColor.y, map.FogColor.z, 1);
		RenderSettings.fogStartDistance = map.FogStart * 4f;
		RenderSettings.fogEndDistance = map.FogEnd * 4f;

        Shader.SetGlobalVector("ShadowFillColor", map.ShadowFillColor);
        Shader.SetGlobalFloat("LightingMultiplier", map.LightingMultiplier);
        Shader.SetGlobalVector("SunDirection", map.SunDirection);
        Shader.SetGlobalVector("SunAmbience", map.SunAmbience);
        Shader.SetGlobalVector("SunColor", map.SunColor);
        Shader.SetGlobalVector("SpecularColor", map.SpecularColor);
	}


	public IEnumerator LoadScmapFile()
	{
		map = new Map();

		//string MapPath = EnvPaths.GetMapsPath();
		string path = MapLuaParser.MapRelativePath(MapLuaParser.Current.ScenarioLuaFile.Data.map);

		if (map.Load(path))
		{
			UpdateLighting();
		}
		else
		{
			Debug.LogWarning("File not found!\n" + path);
			yield break;
		}


		if (map.VersionMinor >= 60)
		{
			map.AdditionalSkyboxData.Data.UpdateSize();
		}
		else
		{
			LoadDefaultSkybox();
		}


		EnvPaths.CurrentGamedataPath = EnvPaths.GamedataPath;

		//Shader
        MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.SetValue(map.TerrainShader);
        ToogleShader();
	
		// Set Variables
		int xRes = MapLuaParser.Current.ScenarioLuaFile.Data.Size[0];
		int zRes = MapLuaParser.Current.ScenarioLuaFile.Data.Size[1];
		float HalfxRes = xRes / 10f;
		float HalfzRes = zRes / 10f;


		TerrainMaterial.SetTexture("_TerrainNormal", map.UncompressedNormalmapTex);
		WaterMaterial.SetTexture("UtilitySamplerC", map.UncompressedWatermapTex);
		Shader.SetGlobalFloat("_WaterScaleX", xRes);
		Shader.SetGlobalFloat("_WaterScaleZ", xRes);

		//*****************************************
		// ***** Set Terrain proportives
		//*****************************************

		LoadHeights();


		// Load Stratum Textures Paths
		LoadStratumScdTextures();
		MapLuaParser.Current.InfoPopup.Show(true, "Loading map...\n( Assing scmap data )");


		WaterLevel.transform.localScale = new Vector3(HalfxRes, 1, HalfzRes);
		TerrainMaterial.SetFloat("TerrainScale", (float) 1.0 / xRes);
        Shader.SetGlobalFloat("_GridScale", HalfxRes);
		TerrainMaterial.SetTexture("UtilitySamplerC", map.UncompressedWatermapTex);


		for (int i = 0; i < map.EnvCubemapsFile.Length; i++)
		{
			if (map.EnvCubemapsName[i] == "<default>")
			{

				try
				{
					CurrentEnvironmentCubemap = GetGamedataFile.GetGamedataCubemap(map.EnvCubemapsFile[i]);
					Shader.SetGlobalTexture("environmentSampler", CurrentEnvironmentCubemap);
				}
				catch
				{
					WaterMaterial.SetTexture("environmentSampler", DefaultWaterSky);
				}
			}

		}

		SetWaterTextures();

		SetWater();

		Teren.gameObject.layer = 8;

		SetTextures();

		EditMap.WavesRenderer.ReloadWaves();

		if (Slope)
		{
			ToogleSlope(Slope);
		}

		yield return null;
	}

	public void LoadHeights()
	{
		if (Teren) DestroyImmediate(Teren.gameObject);

		Teren = Terrain.CreateTerrainGameObject(Data).GetComponent<Terrain>();
		Teren.gameObject.name = "TERRAIN";
#if UNITY_2019_2_OR_NEWER

#else
		Teren.materialType = Terrain.MaterialType.Custom;
#endif
		Teren.materialTemplate = TerrainMaterial;
		Teren.heightmapPixelError = 1f;
		Teren.basemapDistance = 10000;
		Teren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		Teren.drawTreesAndFoliage = false;
		Teren.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

		int xRes = MapLuaParser.Current.ScenarioLuaFile.Data.Size[0];
		int zRes = MapLuaParser.Current.ScenarioLuaFile.Data.Size[1];
		float yRes = (float)map.HeightScale;

		float HalfxRes = xRes / 10f;
		float HalfzRes = zRes / 10f;

		Data.heightmapResolution = (int)(xRes + 1);
		TerrainHeight = 1f / yRes;
		TerrainHeight *= 0.1f;
		TerrainHeight *= 2;

		Data.size = new Vector3(
			HalfxRes,
			TerrainHeight,
			HalfzRes
			);

		Data.RefreshPrototypes();
		Teren.Flush();
		Teren.UpdateGIMaterials();
		SyncHeightmap();

		Teren.transform.localPosition = new Vector3(0, 0, -HalfzRes);

		heightsLength = (int)Mathf.Max((map.Height + 1), (map.Width + 1));
		heights = new float[heightsLength, heightsLength];

		float HeightWidthMultiply = (map.Height / (float)map.Width);

		int y = 0;
		int x = 0;
		int localY = 0;

		for (y = 0; y < heightsLength; y++)
		{
			for (x = 0; x < heightsLength; x++)
			{
				localY = (int)(((heightsLength - 1) - y) * HeightWidthMultiply);

				//heights[y, x] = (float)((((double)map.GetHeight(x, localY)) / HeightResize));
				heights[y, x] = (float)(map.GetHeight(x, localY) / HeightResize); // 65536.0 / 2.0 // 32768.0

				if (HeightWidthMultiply == 0.5f && y > 0 && y % 2f == 0)
				{
					heights[y - 1, x] = Mathf.Lerp(heights[y, x], heights[y - 2, x], 0.5f);
				}
			}
		}

		// Set terrain heights from heights array
		ApplyHeightmap(false);

		GenerateControlTex.StopAllTasks();
		GenerateControlTex.GenerateNormal();
		GenerateControlTex.GenerateWater();
	}


	public void LoadStratumScdTextures(bool Loading = true)
	{
		// Load Stratum Textures Paths


		bool NormalMapFix = false;

		for (int i = 0; i < Textures.Length; i++)
		{
			if (Loading)
			{
				MapLuaParser.Current.InfoPopup.Show(true, "Loading map...\n( Stratum textures " + (i + 1) + " )");

				if (i >= map.Layers.Count)
					map.Layers.Add(new Layer());

				Textures[i].AlbedoPath = GetGamedataFile.FixMapsPath(map.Layers[i].PathTexture);
				if (Textures[i].AlbedoPath.StartsWith("/"))
				{
					Textures[i].AlbedoPath = Textures[i].AlbedoPath.Remove(0, 1);
				}		
				Textures[i].AlbedoScale = map.Layers[i].ScaleTexture;


				if (string.IsNullOrEmpty(map.Layers[i].PathNormalmap))
				{
					if (i == 9)
					{
						// Upper stratum normal should be empty!
						Textures[i].NormalPath = "";
						Debug.Log("Clear Upper stratum normal map");
					}
					else
					{
						Textures[i].NormalPath = "env/tundra/layers/tund_none_normal.dds";
						Debug.Log("Add missing normalmap on stratum " + i);
						NormalMapFix = true;
					}
				}
				else
				{
					if (i == 9)
					{
						// Upper stratum normal should be empty!
						Textures[i].NormalPath = "";
						NormalMapFix = true;
					}
					else
					{
						Textures[i].NormalPath = GetGamedataFile.FixMapsPath(map.Layers[i].PathNormalmap);
						if (Textures[i].NormalPath.StartsWith("/"))
						{
							Textures[i].NormalPath = Textures[i].NormalPath.Remove(0, 1);
						}
					}
				}
				Textures[i].NormalScale = map.Layers[i].ScaleNormalmap;
			}


			/*string Env = GetGamedataFile.EnvScd;
			if (GetGamedataFile.IsMapPath(Textures[i].AlbedoPath))
				Env = GetGamedataFile.MapScd;*/

			try
			{
				Textures[i].AlbedoPath = GetGamedataFile.FindFile(Textures[i].AlbedoPath);
				//Debug.Log("Found: " + Textures[i].AlbedoPath);
				GetGamedataFile.LoadTextureFromGamedata(Textures[i].AlbedoPath, i, false);
			}
			catch (System.Exception e)
			{
				Debug.LogError(i + ", Albedo tex: " + Textures[i].AlbedoPath);
				Debug.LogError(e);
			}
			/*Env = GetGamedataFile.EnvScd;
			if (GetGamedataFile.IsMapPath(Textures[i].NormalPath))
				Env = GetGamedataFile.MapScd;*/

			try
			{
				Textures[i].NormalPath = GetGamedataFile.FindFile(Textures[i].NormalPath);
				//Debug.Log("Found: " + Textures[i].NormalPath);
				GetGamedataFile.LoadTextureFromGamedata(Textures[i].NormalPath, i, true);
			}
			catch (System.Exception e)
			{
				Debug.LogError(i + ", Normal tex: " + Textures[i].NormalPath);
				Debug.LogError(e);
			}
		}


		if (NormalMapFix)
			GenericInfoPopup.ShowInfo("Fixed wrong or missing normalmap textures on stratum layers");
	}


#region Water
	public void SetWater()
	{
		WaterLevel.gameObject.SetActive(map.Water.HasWater);
		WaterLevel.transform.position = Vector3.up * (map.Water.Elevation / 10.0f);

		WaterMaterial.SetVector("waterColor", map.Water.SurfaceColor);
		WaterMaterial.SetVector("SunColor", map.Water.SunColor);
        WaterMaterial.SetVector("waterLerp", map.Water.ColorLerp);
		WaterMaterial.SetVector("SunDirection", map.Water.SunDirection);
		WaterMaterial.SetFloat("SunReflectionAmount", map.Water.SunReflection);
		WaterMaterial.SetFloat("SunShininess", map.Water.SunShininess);
		WaterMaterial.SetFloat("skyreflectionAmount", map.Water.SkyReflection);
        WaterMaterial.SetFloat("refractionScale", map.Water.RefractionScale);

		/*
		for (int w = 0; w < map.WaveGenerators.Count; w++)
		{
			map.WaveGenerators[w].Position.y = map.Water.Elevation;
		}
		*/

		Shader.SetGlobalFloat("WaterElevation", map.Water.Elevation / 10.0f);
		//TerrainMaterial.SetFloat("_DepthLevel", map.Water.ElevationDeep / 10.0f);
		//TerrainMaterial.SetFloat("_AbyssLevel", map.Water.ElevationAbyss / 10.0f);
		//TerrainMaterial.SetInt("_Water", map.Water.HasWater ? 1 : 0);
		Shader.SetGlobalInt("_Water", map.Water.HasWater ? 1 : 0);
	}

	public void SetWaterTextures()
	{
		Texture2D WaterRamp = GetGamedataFile.LoadTexture2D(map.Water.TexPathWaterRamp, false, true, true);
		WaterRamp.wrapMode = TextureWrapMode.Clamp;
		Shader.SetGlobalTexture("WaterRampSampler", WaterRamp);

		try
		{
			Cubemap WaterReflection = GetGamedataFile.GetGamedataCubemap(map.Water.TexPathCubemap);
			WaterMaterial.SetTexture("SkySampler", WaterReflection);
		}
		catch
		{
			WaterMaterial.SetTexture("SkySampler", DefaultWaterSky);
		}

		const int WaterAnisoLevel = 4;

		Texture2D WaterNormal = GetGamedataFile.LoadTexture2D(map.Water.WaveTextures[0].TexPath, false, true, true);
		WaterNormal.anisoLevel = WaterAnisoLevel;
		WaterMaterial.SetTexture("NormalSampler0", WaterNormal);
		WaterNormal = GetGamedataFile.LoadTexture2D(map.Water.WaveTextures[1].TexPath, false, true, true);
		WaterNormal.anisoLevel = WaterAnisoLevel;
		WaterMaterial.SetTexture("NormalSampler1", WaterNormal);
		WaterNormal = GetGamedataFile.LoadTexture2D(map.Water.WaveTextures[2].TexPath, false, true, true);
		WaterNormal.anisoLevel = WaterAnisoLevel;
		WaterMaterial.SetTexture("NormalSampler2", WaterNormal);
		WaterNormal = GetGamedataFile.LoadTexture2D(map.Water.WaveTextures[3].TexPath, false, true, true);
		WaterNormal.anisoLevel = WaterAnisoLevel;
		WaterMaterial.SetTexture("NormalSampler3", WaterNormal);

		WaterMaterial.SetVector("normal1Movement", map.Water.WaveTextures[0].NormalMovement);
		WaterMaterial.SetVector("normal2Movement", map.Water.WaveTextures[1].NormalMovement);
		WaterMaterial.SetVector("normal3Movement", map.Water.WaveTextures[2].NormalMovement);
		WaterMaterial.SetVector("normal4Movement", map.Water.WaveTextures[3].NormalMovement);
        WaterMaterial.SetVector("normalRepeatRate", new Vector4(map.Water.WaveTextures[0].NormalRepeat, map.Water.WaveTextures[1].NormalRepeat, map.Water.WaveTextures[2].NormalRepeat, map.Water.WaveTextures[3].NormalRepeat));
	}

#endregion

#region Textures
	public void SetTextures(int Layer = -1)
	{
		if (Layer < 0)
		{
			TerrainMaterial.SetTexture("UtilitySamplerA", map.TexturemapTex);
			if (Textures[5].Albedo || Textures[6].Albedo || Textures[7].Albedo || Textures[8].Albedo)
				TerrainMaterial.SetTexture("UtilitySamplerB", map.TexturemapTex2);
			
			TerrainMaterial.SetFloat("Stratum0AlbedoTile", map.Width / Textures[1].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum1AlbedoTile", map.Width / Textures[2].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum2AlbedoTile", map.Width / Textures[3].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum3AlbedoTile", map.Width / Textures[4].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum4AlbedoTile", map.Width / Textures[5].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum5AlbedoTile", map.Width / Textures[6].AlbedoScale);
            TerrainMaterial.SetFloat("Stratum6AlbedoTile", map.Width / Textures[7].AlbedoScale);

            TerrainMaterial.SetFloat("Stratum0NormalTile", map.Width / Textures[1].NormalScale);
            TerrainMaterial.SetFloat("Stratum1NormalTile", map.Width / Textures[2].NormalScale);
            TerrainMaterial.SetFloat("Stratum2NormalTile", map.Width / Textures[3].NormalScale);
            TerrainMaterial.SetFloat("Stratum3NormalTile", map.Width / Textures[4].NormalScale);
            TerrainMaterial.SetFloat("Stratum4NormalTile", map.Width / Textures[5].NormalScale);
            TerrainMaterial.SetFloat("Stratum5NormalTile", map.Width / Textures[6].NormalScale);
            TerrainMaterial.SetFloat("Stratum6NormalTile", map.Width / Textures[7].NormalScale);
            
            GenerateArrays();
		}

		if (Layer <= 0)
		{
			TerrainMaterial.SetFloat("LowerAlbedoTile", map.Width / Textures[0].AlbedoScale);
			TerrainMaterial.SetFloat("LowerNormalTile", map.Width / Textures[0].NormalScale);
			TerrainMaterial.SetTexture("LowerAlbedoSampler", Textures[0].Albedo);
			TerrainMaterial.SetTexture("LowerNormalSampler", Textures[0].Normal);
		}

		if (Layer >= 1 && Layer <= 7)
		{
			string IdStrig = (Layer - 1).ToString();
			TerrainMaterial.SetFloat("Stratum" + IdStrig + "AlbedoTile", map.Width / Textures[Layer].AlbedoScale);
			TerrainMaterial.SetFloat("Stratum" + IdStrig + "NormalTile", map.Width / Textures[Layer].NormalScale);
			GenerateArrays();
		}
		
		if (Layer == 8 || Layer < 0)
		{
			TerrainMaterial.SetFloat("Stratum7AlbedoTile", map.Width / Textures[8].AlbedoScale);
			TerrainMaterial.SetFloat("Stratum7NormalTile", map.Width / Textures[8].NormalScale);
			TerrainMaterial.SetTexture("Stratum7AlbedoSampler", Textures[8].Albedo);
			TerrainMaterial.SetTexture("Stratum7NormalSampler", Textures[8].Normal);
		}

		if (Layer == 9 || Layer < 0)
		{
			TerrainMaterial.SetFloat("UpperAlbedoTile", map.Width / Textures[9].AlbedoScale);
			TerrainMaterial.SetTexture("UpperAlbedoSampler", Textures[9].Albedo);
		}
	}

	// We need an array texture to reduce the amount of samplers in the shader,
	// but we exclude stratum 7 as it might hold very large textures.
	void GenerateArrays()
	{
		int AlbedoSize = 256;
		int MipMapCount = 10;

		for (int i = 0; i < 7; i++)
		{
			if (Textures[i + 1].Albedo.width > AlbedoSize)
			{
				AlbedoSize = Textures[i + 1].Albedo.width;
				MipMapCount = Textures[i + 1].Albedo.mipmapCount;
			}
			if (Textures[i + 1].Albedo.height > AlbedoSize)
			{
				AlbedoSize = Textures[i + 1].Albedo.height;
				MipMapCount = Textures[i + 1].Albedo.mipmapCount;
			}
		}

		Texture2DArray AlbedoArray = new Texture2DArray(AlbedoSize, AlbedoSize, 7, TextureFormat.RGBA32, true);

		for (int i = 0; i < 7; i++)
		{
			if(!Textures[i + 1].Albedo.isReadable)
			{
				if(Textures[i + 1].Albedo != Texture2D.whiteTexture)
					Debug.LogWarning("Assigned albedo texture is not readable! " + Textures[i + 1].AlbedoPath);
				continue;
			}

			if (Textures[i + 1].Albedo.width <= 4 && Textures[i + 1].Albedo.height <= 4)
				continue;

			if (Textures[i + 1].Albedo.width != AlbedoSize || Textures[i + 1].Albedo.height != AlbedoSize)
			{
				//Debug.Log("Rescale texture from" + Textures[i + 1].Albedo.width + "x" + Textures[i + 1].Albedo.height + " to: " + AlbedoSize);
				Textures[i + 1].Albedo = TextureScale.Bilinear(Textures[i + 1].Albedo, AlbedoSize, AlbedoSize);
			}

			if (MipMapCount != Textures[i + 1].Albedo.mipmapCount)
				Debug.LogWarning("Wrong mipmap Count: " + Textures[i + 1].Albedo.mipmapCount + " for texture" + Textures[i + 1].AlbedoPath);
			for (int m = 0; m < MipMapCount; m++)
			{
				AlbedoArray.SetPixels(Textures[i + 1].Albedo.GetPixels(m), i, m);
			}
		}

		//AlbedoArray.mipMapBias = 0.5f;
		AlbedoArray.filterMode = FilterMode.Bilinear;
		AlbedoArray.anisoLevel = 4;
		AlbedoArray.mipMapBias = 0.0f;

		AlbedoArray.Apply(false);
		TerrainMaterial.SetTexture("_StratumAlbedoArray", AlbedoArray);

		AlbedoSize = 256;

		for (int i = 0; i < 7; i++)
		{
			if (Textures[i + 1].Normal == null)
				continue;
			if (Textures[i + 1].Normal.width > AlbedoSize)
			{
				AlbedoSize = Textures[i + 1].Normal.width;
			}
			if (Textures[i + 1].Normal.height > AlbedoSize)
			{
				AlbedoSize = Textures[i + 1].Normal.height;
			}
		}

		Texture2DArray NormalArray = new Texture2DArray(AlbedoSize, AlbedoSize, 7, TextureFormat.RGBA32, true);

		for (int i = 0; i < 7; i++)
		{
			if (!Textures[i + 1].Normal.isReadable)
			{
				Debug.LogWarning("Assigned normal texture is not readable! " + Textures[i + 1].NormalPath);
				continue;
			}

			if (Textures[i + 1].Normal == null)
				continue;

			if (Textures[i + 1].Normal.width != AlbedoSize || Textures[i + 1].Normal.height != AlbedoSize)
			{
				Textures[i + 1].Normal = TextureScale.Bilinear(Textures[i + 1].Normal, AlbedoSize, AlbedoSize);
			}

			for (int m = 0; m < Textures[i + 1].Normal.mipmapCount; m++)
			{
				NormalArray.SetPixels(Textures[i + 1].Normal.GetPixels(m), i, m);
			}
		}

		//NormalArray.mipMapBias = -0.5f;
		NormalArray.filterMode = FilterMode.Bilinear;
		NormalArray.anisoLevel = 2;
		NormalArray.Apply(false);

		TerrainMaterial.SetTexture("_StratumNormalArray", NormalArray);
	}

	public void UpdateScales(int id)
	{
		if (id == 0)
		{
			TerrainMaterial.SetFloat("LowerAlbedoTile", map.Width / Textures[0].AlbedoScale);
			TerrainMaterial.SetFloat("LowerNormalTile", map.Width / Textures[0].NormalScale);
		}
		else if (id == 9)
		{
			TerrainMaterial.SetFloat("UpperAlbedoTile", map.Width / Textures[9].AlbedoScale);
			TerrainMaterial.SetFloat("UpperNormalTile", map.Width / Textures[9].NormalScale);
		}
		else
		{
			string IdStrig = (id - 1).ToString();
			TerrainMaterial.SetFloat("Stratum" + IdStrig + "AlbedoTile", map.Width / Textures[id].AlbedoScale);
			TerrainMaterial.SetFloat("Stratum" + IdStrig + "NormalTile", map.Width / Textures[id].NormalScale);
		}
	}
#endregion

#region Saving
	public static float TerrainHeight = 12.5f;
	//const double HeightResize = 128.0 * 256.0; //512 * 40;
	public const double HeightResize = 32768.0; //512 * 40;
	//public const double RoundingError = 0.5;
	public const float MaxElevation = 256;

	public void SaveScmapFile()
	{
		float LowestElevation = ScmapEditor.MaxElevation;
		float HighestElevation = 0;

		if (Teren)
		{
			heights = Teren.terrainData.GetHeights(0, 0, Teren.terrainData.heightmapResolution, Teren.terrainData.heightmapResolution);
			heightsLength = heights.GetLength(0);

			int y = 0;
			int x = 0;
			for (y = 0; y < map.Width + 1; y++)
			{
				for (x = 0; x < map.Height + 1; x++)
				{
					float Height = heights[x, y];

					LowestElevation = Mathf.Min(LowestElevation, Height);
					HighestElevation = Mathf.Max(HighestElevation, Height);

					//double HeightValue = ((double)Height) * HeightResize;
					//map.SetHeight(y, map.Height - x, (short)(HeightValue + RoundingError));
					map.SetHeight(y, map.Height - x, (ushort)(Height * HeightResize));
				}
			}
		}

		LowestElevation = (LowestElevation * TerrainHeight) / 0.1f;
		HighestElevation = (HighestElevation * TerrainHeight) / 0.1f;


		if (HighestElevation - LowestElevation > 49.9)
		{
			Debug.Log("Lowest point: " + LowestElevation);
			Debug.Log("Highest point: " + HighestElevation);

			Debug.LogWarning("Height difference is too high! it might couse rendering issues! Height difference is: " + (HighestElevation - LowestElevation));
			GenericInfoPopup.ShowInfo("Height difference " + (HighestElevation - LowestElevation) + " is too high!\nIt might cause rendering issues!");
		}


		if (MapLuaParser.Current.EditMenu.MapInfoMenu.SaveAsFa.isOn)
		{
			if(map.AdditionalSkyboxData == null || map.AdditionalSkyboxData.Data == null || map.AdditionalSkyboxData.Data.Position.x == 0)
			{ // Convert to v60
				LoadDefaultSkybox();
			}

			map.VersionMinor = 60;
			map.AdditionalSkyboxData.Data.UpdateSize();
		}
		else if(map.VersionMinor >= 60) // Convert to v56
		{
			LoadDefaultSkybox();
			map.AdditionalSkyboxData.Data.UpdateSize();
			map.VersionMinor = 56;
		}

		//Debug.Log("Set Heightmap to map " + map.Width + ", " + map.Height);

		//string MapPath = EnvPaths.GetMapsPath();
		string path = MapLuaParser.MapRelativePath(MapLuaParser.Current.ScenarioLuaFile.Data.map);

		map.TerrainShader = MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.text;

		map.MinimapContourColor = new Color32(0, 0, 0, 255);
		map.MinimapDeepWaterColor = new Color32(71, 140, 181, 255);
		map.MinimapShoreColor = new Color32(141, 200, 225, 255);
		map.MinimapLandStartColor = new Color32(119, 101, 108, 255);
		map.MinimapLandEndColor = new Color32(206, 206, 176, 255);
		//map.MinimapLandEndColor = new Color32 (255, 255, 215, 255);
		map.MinimapContourInterval = 10;

		map.WatermapTex = new Texture2D(map.UncompressedWatermapTex.width, map.UncompressedWatermapTex.height, map.UncompressedWatermapTex.format, false);
		map.WatermapTex.SetPixels(map.UncompressedWatermapTex.GetPixels());
		map.WatermapTex.Apply();
		map.WatermapTex.Compress(true);
		map.WatermapTex.Apply();

		map.NormalmapTex = new Texture2D(map.UncompressedNormalmapTex.width, map.UncompressedNormalmapTex.height, map.UncompressedNormalmapTex.format, false);
		map.NormalmapTex.SetPixels(map.UncompressedNormalmapTex.GetPixels());
		map.NormalmapTex.Apply();
		map.NormalmapTex.Compress(true);
		map.NormalmapTex.Apply();

		map.PreviewTex = PreviewRenderer.RenderPreview(((LowestElevation + HighestElevation) / 2) * 0.1f);


		for (int i = 0; i < map.Layers.Count; i++)
		{
			Textures[i].AlbedoPath = GetGamedataFile.FixMapsPath(Textures[i].AlbedoPath);
			Textures[i].NormalPath = GetGamedataFile.FixMapsPath(Textures[i].NormalPath);

			map.Layers[i].PathTexture = Textures[i].AlbedoPath;
			map.Layers[i].PathNormalmap = Textures[i].NormalPath;

			map.Layers[i].ScaleTexture = Textures[i].AlbedoScale;
			map.Layers[i].ScaleNormalmap = Textures[i].NormalScale;
		}



		List<Prop> AllProps = new List<Prop>();
		if (EditMap.PropsInfo.AllPropsTypes != null)
		{
			int Count = EditMap.PropsInfo.AllPropsTypes.Count;
			for (int i = 0; i < EditMap.PropsInfo.AllPropsTypes.Count; i++)
			{
				AllProps.AddRange(EditMap.PropsInfo.AllPropsTypes[i].GenerateSupComProps());
			}
		}
		map.Props = AllProps;


		if(map.VersionMinor < 56)
		{
			map.ConvertToV56();
		}

		map.Save(path,  map.VersionMinor);
	}

#endregion

#region Clean
	public void RestartTerrainAsset()
	{
		int xRes = (int)(256 + 1);
		int zRes = (int)(256 + 1);
		int yRes = (int)(ScmapEditor.MaxElevation);
		heightsLength = xRes;
		heights = new float[xRes, zRes];

		// Set Terrain proportives
		Data.heightmapResolution = xRes;
		Data.size = new Vector3(
			256 / 10.0f,
			yRes / 10.0f,
			256 / 10.0f
			);

		if (map != null) WaterLevel.transform.localScale = new Vector3(map.Width * 0.1f, 1f, map.Height * 0.1f);
		if (Teren) Teren.transform.localPosition = new Vector3(-xRes / 20.0f, 1, -zRes / 20.0f);

		// Modify heights array data
		for (int y = 0; y < zRes; y++)
		{
			for (int x = 0; x < xRes; x++)
			{
				heights[x, y] = 0;
			}
		}

		// Set terrain heights from heights array
		Data.SetHeights(0, 0, heights);
	}

	public void LoadDefaultSkybox()
	{
		map.AdditionalSkyboxData = UnityEngine.JsonUtility.FromJson<SkyboxData>(DefaultSkybox.text);
		map.AdditionalSkyboxData.Data.UpdateSize();
	}

	public void UnloadMap()
	{
		EditMap.PropsInfo.UnloadProps();
		EditMap.UnitsInfo.UnloadUnits();
		Markers.MarkersControler.UnloadMarkers();
		DecalsControler.Current.UnloadDecals();
		GenerateControlTex.StopAllTasks();
	}
#endregion

#region Converters
	/// <summary>
	/// Convert Scmap position to editor world position
	/// </summary>
	/// <param name="MapPos"></param>
	/// <returns></returns>
	public static Vector3 ScmapPosToWorld(Vector3 MapPos)
	{
		Vector3 ToReturn = MapPos;

		// Position
		ToReturn.x = MapPos.x / 10f;
		ToReturn.z = -MapPos.z / 10f;

		// Height
		ToReturn.y = 1 * (MapPos.y / 10);

		return ToReturn;
	}

	public static float ScmapPosToWorld(float val)
	{
		return val / 10f;
	}

	/// <summary>
	/// Convert Editor world position to Scmap position
	/// </summary>
	/// <param name="MapPos"></param>
	/// <returns></returns>
	public static Vector3 WorldPosToScmap(Vector3 MapPos)
	{
		Vector3 ToReturn = MapPos;

		//Position
		ToReturn.x = MapPos.x * 10;
		ToReturn.z = MapPos.z * -10f;

		// Height
		ToReturn.y = MapPos.y * 10;

		return ToReturn;
	}

	public static Vector3 SnapToSmallGridCenter(Vector3 Pos)
	{

		Pos.x += 0.05f;
		Pos.z -= 0.05f;

		Pos.x *= 20;
		Pos.x = (int)(Pos.x + 0.0f);
		Pos.x /= 20.0f;

		Pos.z *= 20;
		Pos.z = (int)(Pos.z + 0.0f);
		Pos.z /= 20.0f;

		return Pos;
	}

	public static Vector3 SnapToSmallGrid(Vector3 Pos)
	{
		Pos.x *= 20;
		Pos.x = (int)(Pos.x + 0.0f);
		Pos.x /= 20.0f;

		Pos.z *= 20;
		Pos.z = (int)(Pos.z + 0.0f);
		Pos.z /= 20.0f;

		return Pos;
	}

	public static Vector3 SnapMarker(Vector3 Pos, int ID)
	{
		bool Water = ID == 1 || ID == 2 || ID == 3;

		return SnapToGridCenter(Pos, true, !Water);

	}

	public static Vector3 SnapToGridCenter(Vector3 Pos, bool SampleHeight = false, bool MinimumWaterLevel = false)
	{
		Pos.x += 0.1f;
		//Pos.z += 0.1f;

		Pos.x *= 10;
		Pos.x = (int)(Pos.x);
		Pos.x /= 10.0f;

		Pos.z *= 10;
		Pos.z = (int)(Pos.z);
		Pos.z /= 10.0f;

		Pos.x -= 0.05f;
		Pos.z -= 0.05f;

		if (SampleHeight)
			Pos.y = Current.Teren.SampleHeight(Pos);
		if (MinimumWaterLevel)
			Pos.y = Mathf.Clamp(Pos.y, GetWaterLevel(), 10000);
		return Pos;
	}

	public static Vector3 SnapToTerrain(Vector3 Pos, bool MinimumWaterLevel = false)
	{
		Pos.y = Current.Teren.SampleHeight(Pos);
		if (MinimumWaterLevel)
			Pos.y = Mathf.Clamp(Pos.y, GetWaterLevel(), 10000);
		return Pos;
	}

	public static Vector3 ClampToWater(Vector3 Pos)
	{
		if (Current.map.Water.HasWater)
			Pos.y = Mathf.Clamp(Pos.y, GetWaterLevel(), 10000);
		return Pos;
	}

	public static Vector3 SnapToGrid(Vector3 Pos)
	{
		Pos.x *= 10;
		Pos.x = (int)(Pos.x);
		Pos.x /= 10.0f;

		Pos.z *= 10;
		Pos.z = (int)(Pos.z);
		Pos.z /= 10.0f;

		return Pos;
	}

	public static float GetWaterLevel()
	{
		if (!Current.map.Water.HasWater)
			return 0;
		return Current.WaterLevel.localPosition.y;
	}

#endregion

#region Rendering
	[HideInInspector]
	public bool Grid;
	[HideInInspector]
	public GridTypes GridType;
	[HideInInspector]
	public bool Slope;

	public Texture[] GridTextures;

	public enum GridTypes
	{
		Standard, Build, General, AI
	}

	public bool ToogleCurrent()
	{
		Grid = !Grid;
		UpdateGrid();
		return Grid;
	}

	public void ToogleGrid(bool To)
	{
		if(To)
			GridType = GridTypes.Standard;
		Grid = To;
		UpdateGrid();
	}

	public void ToogleBuildGrid(bool To)
	{
		if (To)
			GridType = GridTypes.Build;
		Grid = To;
		UpdateGrid();
	}

	public void ToogleGeneraldGrid(bool To)
	{
		if (To)
			GridType = GridTypes.General;
		Grid = To;
		UpdateGrid();
	}

	public void ToogleAIGrid(bool To)
	{
		if (To)
			GridType = GridTypes.AI;
		Grid = To;
		UpdateGrid();
	}

	void UpdateGrid()
	{
		TerrainMaterial.SetTexture("_GridTexture", GridTextures[(int) GridType]);
		TerrainMaterial.SetInteger("_Grid", Grid ? 1 : 0);
		TerrainMaterial.SetInteger("_GridType", (int) GridType);
	}

	public void ToogleSlope(bool To)
	{
		Slope = To;
		TerrainMaterial.SetInteger("_Slope", Slope ? 1 : 0);
		if (To)
		{
			GenerateControlTex.Current.GenerateSlopeTexture();

		}

	}

	public void ToogleShader()
	{
		foreach (var localKeyword in TerrainMaterial.shader.keywordSpace.keywords)
		{
			TerrainMaterial.DisableKeyword(localKeyword);
		}
		TerrainMaterial.EnableKeyword(MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.text);
		
		int shaderId;
		switch (MapLuaParser.Current.EditMenu.MapInfoMenu.ShaderName.text)
		{
			case "TTerrain":
				shaderId = -10;
				break;
			case "TTerrainXP":
				shaderId = -20;
				break;
			case "TTerrainXPExt":
				shaderId = 20;
				break;
			case "Terrain000":
				shaderId = 0;
				break;
			case "Terrain050":
				shaderId = 50;
				break;
			case "Terrain100":
				shaderId = 100;
				break;
			case "Terrain150":
				shaderId = 150;
				break;
			case "Terrain101":
				shaderId = 101;
				break;
			case "Terrain151":
				shaderId = 151;
				break;
			case "Terrain102":
				shaderId = 102;
				break;
			case "Terrain152":
				shaderId = 152;
				break;
			case "Terrain100B":
				shaderId = 110;
				break;
			case "Terrain150B":
				shaderId = 160;
				break;
			case "Terrain101B":
				shaderId = 111;
				break;
			case "Terrain151B":
				shaderId = 161;
				break;
			case "Terrain102B":
				shaderId = 112;
				break;
			case "Terrain152B":
				shaderId = 162;
				break;
			case "Terrain200":
				shaderId = 200;
				break;
			case "Terrain250":
				shaderId = 250;
				break;
			case "Terrain201":
				shaderId = 201;
				break;
			case "Terrain251":
				shaderId = 251;
				break;
			case "Terrain202":
				shaderId = 202;
				break;
			case "Terrain252":
				shaderId = 252;
				break;
			case "Terrain200B":
				shaderId = 210;
				break;
			case "Terrain250B":
				shaderId = 260;
				break;
			case "Terrain201B":
				shaderId = 211;
				break;
			case "Terrain251B":
				shaderId = 261;
				break;
			case "Terrain202B":
				shaderId = 212;
				break;
			case "Terrain252B":
				shaderId = 262;
				break;
			default:
				shaderId = -1;
				break;
		}

		if (shaderId >= 0)
        {
            Textures[8].AlbedoScale = 10000;  // Use terrain info texture
            Textures[8].NormalScale = 10000;  // Use terrain normal texture
            MapLuaParser.Current.EditMenu.TexturesMenu.ShaderTools.interactable = true;
            MapLuaParser.Current.EditMenu.TexturesMenu.ShaderTools.alpha = 1;
            if (shaderId >= 100)
            {
                Textures[9].AlbedoScale = 10000;  // Use PBR rendering on decals
            }
        }
		else
		{
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderTools.interactable = false;
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderTools.alpha = 0.7f;
		}
		
		if (shaderId >= 200)
		{
			MapLuaParser.Current.EditMenu.LightingMenu.Specular.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.LightingMenu.SpecularRed.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderSettings.gameObject.SetActive(true);
			// arguments are: blurriness, contrast increase, cos(30), sin(30)
			map.SpecularColor = new Vector4(map.SpecularColor.x, 0.6f, 0.866f, 0.5f);
			Shader.SetGlobalVector("SpecularColor", map.SpecularColor);
		}
		else if (shaderId >= 100)
		{
			MapLuaParser.Current.EditMenu.LightingMenu.Specular.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.LightingMenu.SpecularRed.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderSettings.gameObject.SetActive(false);
		}
		else if (shaderId == -10)  //TTerrain
		{
			MapLuaParser.Current.EditMenu.LightingMenu.Specular.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.LightingMenu.SpecularRed.gameObject.SetActive(true);
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderSettings.gameObject.SetActive(false);
		}
		else
		{
			MapLuaParser.Current.EditMenu.LightingMenu.Specular.gameObject.SetActive(true);
			MapLuaParser.Current.EditMenu.LightingMenu.SpecularRed.gameObject.SetActive(false);
			MapLuaParser.Current.EditMenu.TexturesMenu.ShaderSettings.gameObject.SetActive(false);
		}
		
		Shader.SetGlobalInt("_ShaderID", shaderId);
	}
#endregion
}

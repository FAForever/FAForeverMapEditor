﻿using UnityEngine;
using System.Collections;
using CielaSpike;
using System.Threading;

public class GenerateControlTex : MonoBehaviour
{

	public static GenerateControlTex Current;

	void Awake()
	{
		Current = this;
	}

	public static void StopAllTasks()
	{
		if (GeneratingNormalTex)
			Current.StopCoroutine(NormalCoroutine);
		if (GeneratingWaterTex)
			Current.StopCoroutine(WaterCoroutine);

		NormalCoroutine = null;
		WaterCoroutine = null;

		EditMap.WavesRenderer.ClearShoreLine();
	}

	#region Water

	static bool GeneratingWaterTex
	{
		get
		{
			return WaterCoroutine != null;
		}
	}
	static bool BufforWaterTex = false;
	static Coroutine WaterCoroutine;
	public static void GenerateWater()
	{
		if (GeneratingWaterTex)
		{
			BufforWaterTex = true;
		}
		else
			WaterCoroutine = Current.StartCoroutine(Current.GeneratingWaterTask());
	}

	public IEnumerator GeneratingWater()
	{
		//Task task;
		//this.StartCoroutineAsync(GeneratingWaterTask(), out task);
		//yield return StartCoroutine(task.Wait());

		yield return StartCoroutine(GeneratingWaterTask());

		WaterCoroutine = null;
		yield return null;

		if (BufforWaterTex)
		{
			BufforWaterTex = false;
			WaterCoroutine = Current.StartCoroutine(Current.GeneratingWater());
		}
	}

	public IEnumerator GeneratingWaterTask()
	{

		float WaterHeight = ScmapEditor.Current.map.Water.Elevation * 0.1f;
		//float WaterDeep = ScmapEditor.Current.map.Water.ElevationDeep * 0.1f;
		if (WaterHeight == 0)
			WaterHeight = 1;
		float WaterAbyss = ScmapEditor.Current.map.Water.ElevationAbyss * 0.1f;

		float DeepDifference = (WaterHeight - WaterAbyss) / WaterHeight;
		//float AbyssDifference = (WaterDeep - WaterAbyss) / WaterDeep;

		int i = 0;
		int x = 0;
		int y = 0;
		float WaterDepth = 0;
		//float WaterDepthDeep = 0;
		//float WaterDepthAbyss = 0;

		yield return Ninja.JumpToUnity;
		int Width = ScmapEditor.Current.map.UncompressedWatermapTex.width;
		int Height = ScmapEditor.Current.map.UncompressedWatermapTex.height;
		Color[] AllColors = ScmapEditor.Current.map.UncompressedWatermapTex.GetPixels();
		float[,] HeightmapPixels = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, ScmapEditor.Current.Teren.terrainData.heightmapResolution, ScmapEditor.Current.Teren.terrainData.heightmapResolution);
		int HeightmapWidth = ScmapEditor.Current.Teren.terrainData.heightmapResolution - 1;
		int HeightmapHeight = ScmapEditor.Current.Teren.terrainData.heightmapResolution - 1;

		//yield return Ninja.JumpBack;

		for (x = 0; x < Width; x++)
		{
			for (y = 0; y < Height; y++)
			{
				i = y + x * Height;

				//WaterDepth = ScmapEditor.Current.Data.GetInterpolatedHeight((x + 0.5f) / (Width + 1f), 1f - (y + 0.5f) / (Height + 1f));
				int LerpX = (int)(HeightmapWidth * (1f - (x) / ((float)Width)));
				int LerpY = (int)(HeightmapHeight * ((y) / ((float)Height)));

				if (LerpX < 0) LerpX = 0;
				else if (LerpX >= HeightmapWidth)
					LerpX = HeightmapWidth - 1;

				if (LerpY < 0) LerpY = 0;
				else if (LerpY >= HeightmapHeight)
					LerpY = HeightmapHeight - 1;

				WaterDepth = HeightmapPixels[LerpX, LerpY] + HeightmapPixels[LerpX + 1, LerpY] + HeightmapPixels[LerpX, LerpY + 1] + HeightmapPixels[LerpX + 1, LerpY + 1];
				WaterDepth /= 4f;
				WaterDepth *= ScmapEditor.TerrainHeight; //16
														 //WaterDepth /= 0.1f;


				WaterDepth = (WaterHeight - WaterDepth) / WaterHeight;
				WaterDepth /= DeepDifference;

				//WaterDepthAbyss = (WaterDeep - WaterDepth) / WaterDeep;
				//WaterDepthAbyss /= AbyssDifference;

				//WaterDepth = Mathf.Lerp(WaterDepthDeep * 0.5f, 1, WaterDepthAbyss);


				AllColors[i] = new Color(AllColors[i].r, WaterDepth, (1f - Mathf.Clamp01(WaterDepth * 100f)), 0);
			}
		}

		//yield return Ninja.JumpToUnity;

		ScmapEditor.Current.map.UncompressedWatermapTex.SetPixels(AllColors);
		ScmapEditor.Current.map.UncompressedWatermapTex.Apply(false);

		WaterCoroutine = null;

		yield return null;

		if (BufforWaterTex)
		{
			BufforWaterTex = false;
			WaterCoroutine = Current.StartCoroutine(Current.GeneratingWaterTask());
		}
	}

	#endregion

	#region Normal

	static Coroutine NormalCoroutine;
	public static void GenerateNormal()
	{
		if (GeneratingNormalTex)
		{
			//Debug.Log("Buffor Generate Normals");

			BufforNormalTex = true;
		}
		else
		{
			//Debug.Log("Start Generate Normals");
			NormalCoroutine = Current.StartCoroutine(Current.GeneratingNormal());
		}
	}

	public static void StopGenerateNormal()
	{
		if (GeneratingNormalTex)
		{
			//Debug.Log("Stop Generate Normals");
			BufforNormalTex = false;
			Current.StopCoroutine(NormalCoroutine);
			NormalCoroutine = null;
			ScmapEditor.Current.TerrainMaterial.SetInteger("_GeneratingNormal", 0);

		}
	}


	static bool GeneratingNormalTex
	{
		get
		{
			return NormalCoroutine != null;
		}
	}
	static bool BufforNormalTex = false;

	public IEnumerator GeneratingNormal()
	{
		//Debug.Log("Begin Generate Normals");
		ScmapEditor.Current.TerrainMaterial.SetInteger("_GeneratingNormal", 1);
		Color[] AllColors = ScmapEditor.Current.map.UncompressedNormalmapTex.GetPixels();

		float Width = ScmapEditor.Current.map.UncompressedNormalmapTex.width;
		float Height = ScmapEditor.Current.map.UncompressedNormalmapTex.height;
		int i = 0;
		int x = 0;
		int y = 0;
		Vector3 Normal;

		//int counter = 0;
		float Realtime = Time.realtimeSinceStartup;
		const float MaxAllowedOverhead = 0.02f;

		for (x = 0; x < Width; x++)
		{
			for (y = 0; y < Height; y++)
			{
				i = x + y * ScmapEditor.Current.map.UncompressedNormalmapTex.width;
				Normal = ScmapEditor.Current.Data.GetInterpolatedNormal((x + 0.5f) / (Width), 1f - (y + 0.5f) / (Height));
				AllColors[i] = new Color(0, 1f - (Normal.z * 0.5f + 0.5f), 0, Normal.x * 0.5f + 0.5f);

				if (Time.realtimeSinceStartup - Realtime > MaxAllowedOverhead)
				{
					yield return null;
					Realtime = Time.realtimeSinceStartup;
				}
				/*
				counter++;
				if (counter > 40000)
				{
					counter = 0;
					yield return null;
				}
				*/
			}
		}

		//Debug.Log("Success Generate Normals");
		ScmapEditor.Current.map.UncompressedNormalmapTex.SetPixels(AllColors);
		ScmapEditor.Current.map.UncompressedNormalmapTex.Apply(false);
		ScmapEditor.Current.TerrainMaterial.SetInteger("_GeneratingNormal", 0);
		ScmapEditor.Current.TerrainMaterial.SetTexture("_TerrainNormal", ScmapEditor.Current.map.UncompressedNormalmapTex);

		yield return null;
		//Debug.Log("Finalize Generate Normals");
		NormalCoroutine = null;



		if (BufforNormalTex)
		{
			//Debug.Log("Start buffor Generate Normals");
			BufforNormalTex = false;
			GenerateNormal();
		}
	}



	#endregion

	public Texture2D SlopeData;

	public void GenerateSlopeTexture()
	{
		if (SlopeData == null || SlopeData.width != ScmapEditor.Current.map.Width || SlopeData.height != ScmapEditor.Current.map.Height)
		{
			SlopeData = new Texture2D(ScmapEditor.Current.map.Width, ScmapEditor.Current.map.Height, TextureFormat.RGB24, false);
			SlopeData.filterMode = FilterMode.Point;
			SlopeData.wrapMode = TextureWrapMode.Clamp;
		}

		if (GeneratingSlopeTex)
		{
			BufforSlopeTex = true;
		}
		else
		{
			SlopeTask = StartCoroutine(GeneratingSlope());
		}

	}

	static Coroutine SlopeTask;
	static bool GeneratingSlopeTex
	{
		get
		{
			return SlopeTask != null;
		}
	}
	static bool BufforSlopeTex = false;



	static float[,] SlopeHeightmapPixels;

	IEnumerator GeneratingSlope()
	{
		//ScmapEditor.Current.TerrainMaterial.SetFloat("_UseSlopeTex", 0);
		

		//SlopeHeightmapPixels = ScmapEditor.Current.Teren.terrainData.GetHeights(0, 0, ScmapEditor.Current.Teren.terrainData.heightmapResolution, ScmapEditor.Current.Teren.terrainData.heightmapResolution);
		Task task;
		this.StartCoroutineAsync(GeneratingSlopeTask(), out task);
		yield return StartCoroutine(task.Wait());

		SlopeTask = null;

		if (BufforSlopeTex)
		{
			BufforSlopeTex = false;
			SlopeTask = StartCoroutine(GeneratingSlope());
		}
	}

	/* Old FAF way
	const float FlatHeight = 0.995f;
	const float NonFlatHeight = 0.88f;
	const float AlmostUnpassableHeight = 0.74f;
	const float UnpassableHeight = 0.541f;
	*/

	//SupComSlope
	const float FlatHeight = 0.002f;
	const float NonFlatHeight = 0.30f;
	const float AlmostUnpassableHeight = 0.75f;
	const float UnpassableHeight = 0.75f;
	const float ScaleHeight = 256;

	public Color Flat;
	public Color LowAngle;
	public Color HighAngle;
	public Color AlmostUnpassable;
	public Color Unpassable;


	IEnumerator GeneratingSlopeTask()
	{
		Color[] Pixels = new Color[ScmapEditor.Current.map.Width * ScmapEditor.Current.map.Height];
		
		int x = 0;
		int y = 0;
		int i = 0;

		Vector3 Vert0 = Vector3.zero;
		Vector3 Vert1 = Vector3.zero;
		Vector3 Vert2 = Vector3.zero;
		Vector3 Vert3 = Vector3.zero;


		for (x = 0; x < ScmapEditor.Current.map.Width; x++)
		{
			for (y = 0; y < ScmapEditor.Current.map.Height; y++)
			{
				i = y + x * ScmapEditor.Current.map.Height;

				Vert0.x = x;
				Vert1.x = x + 1;
				Vert2.x = x;
				Vert3.x = x + 1;

				Vert0.z = y;
				Vert1.z = y;
				Vert2.z = y + 1;
				Vert3.z = y + 1;

				/* Old FAF way
				Vert0.y = SlopeHeightmapPixels[x, y] * 512f;
				Vert1.y = SlopeHeightmapPixels[x + 1, y] * 512f;
				Vert2.y = SlopeHeightmapPixels[x, y + 1] * 512f;
				Vert3.y = SlopeHeightmapPixels[x + 1, y + 1] * 512f;

				// Triangle 1: 0, 2, 1
				// 0, 3, 1
				float Dot = Vector3.Dot(GetTriangleVector(Vert0, Vert2, Vert1), Vector3.up);

				// Triangle 2: 3, 1, 2
				// 3, 0, 2
				//if (Dot > 0.5f)
				Dot = Mathf.Min(Dot, Vector3.Dot(GetTriangleVector(Vert3, Vert1, Vert2), Vector3.up));

				if (Dot > FlatHeight)
					Pixels[i] = Flat;
				else if (Dot > NonFlatHeight)
					Pixels[i] = LowAngle;
				else if (Dot > AlmostUnpassableHeight)
					Pixels[i] = HighAngle;
				else if (Dot > UnpassableHeight)
					Pixels[i] = AlmostUnpassable;
				else
					Pixels[i] = Unpassable;
				*/


				Vert0.y = ScmapEditor.GetHeight(x, y) * ScaleHeight;
				Vert1.y = ScmapEditor.GetHeight(x + 1, y) * ScaleHeight;
				Vert2.y = ScmapEditor.GetHeight(x + 1, y + 1) * ScaleHeight;
				Vert3.y = ScmapEditor.GetHeight(x, y + 1) * ScaleHeight;

				float Dot = getSupComSlope(Vert0.y, Vert1.y, Vert2.y, Vert3.y);

				if (Dot <= FlatHeight)
					Pixels[i] = Flat;
				else if (Dot <= NonFlatHeight)
					Pixels[i] = LowAngle;
				else if (Dot <= AlmostUnpassableHeight)
					Pixels[i] = HighAngle;
				else if (Dot <= UnpassableHeight)
					Pixels[i] = AlmostUnpassable;
				else
					Pixels[i] = Unpassable;

				/*
				float Min = Mathf.Min(Slope0, Slope1, Slope2, Slope3);
				float Max = Mathf.Max(Slope0, Slope1, Slope2, Slope3);

				float Slope = Mathf.Abs(Max - Min) * 2;

				if (Slope < FlatHeight)
					Pixels[i] = Flat;
				else if (Slope < NonFlatHeight)
					Pixels[i] = LowAngle;
				else if (Slope < AlmostUnpassableHeight)
					Pixels[i] = HighAngle;
				else if (Slope < UnpassableHeight)
					Pixels[i] = AlmostUnpassable;
				else
					Pixels[i] = Unpassable;
					*/
			}
		}


		yield return Ninja.JumpToUnity;
		SlopeData.SetPixels(Pixels);
		SlopeData.Apply(false);

		ScmapEditor.Current.TerrainMaterial.SetTexture("_SlopeTex", SlopeData);
		//yield return null;
	}

	static Vector3 GetTriangleVector(Vector3 Vert0, Vector3 Vert1, Vector3 Vert2)
	{
		return Vector3.Cross(Vert1 - Vert0, Vert2 - Vert0).normalized;
	}

	static float getSupComSlope(float a, float b, float c, float d)
	{
		return Mathf.Max(Mathf.Abs(a - b), Mathf.Abs(b - c), Mathf.Abs(c - d), Mathf.Abs(d - a));
	}

}

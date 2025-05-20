using UnityEngine;
using UnityEngine.UI;
using Ozone.UI;
using System.IO;
using FAF.MapEditor;
using SFB;

namespace EditMap
{
	public partial class WaterInfo : MonoBehaviour
	{

		public static WaterInfo Current;

		public TerrainInfo TerrainMenu;

		public Toggle HasWater;
		public CanvasGroup WaterSettings;
		public CanvasGroup LightSettings;

		public UiTextField WaterElevation;
		public UiTextField DepthElevation;
		public UiTextField AbyssElevation;
		public Toggle AdvancedWaterToggle;

		public UiTextField ColorLerpXElevation;
		public UiTextField ColorLerpYElevation;
		public UiColor WaterColor;
		public UiColor SunColor;
		public Toggle UseLightingSettings;
		public Vector3 SunDirection;

		public UiTextField SunShininess;
		public UiTextField UnitReflection;
		public UiTextField SkyReflection;
		public UiTextField SunReflection;
		public UiTextField RefractionScale;

		public InputField WaterRamp;
		public InputField Cubemap;

		public UiWaves Waves0;
		public UiWaves Waves1;
		public UiWaves Waves2;
		public UiWaves Waves3;

		bool Loading = false;
		private void OnEnable()
		{
			Current = this;
			ReloadValues();
			//WavesRenderer.GenerateShoreline();
		}

		private void Start()
		{
			LoadWavesUI();
		}

		public void ResetUi()
		{
			AdvancedWaterToggle.isOn = false;
			UseLightingSettings.isOn = false;
		}

		public void OnWaterTogglePressed()
		{
			if (AdvancedWaterToggle.isOn)
			{
				MapLuaParser.Current.EditMenu.LightingMenu.RecalculateLightSettings(2.2f);
            } 
			else
			{
                MapLuaParser.Current.EditMenu.LightingMenu.RecalculateLightSettings(1.8f);
            }
		}

		public void ReloadValues(bool Undo = false)
		{
			Loading = true;
			HasWater.isOn = ScmapEditor.Current.map.Water.HasWater;

			WaterElevation.SetValue(ScmapEditor.Current.map.Water.Elevation);
			DepthElevation.SetValue(ScmapEditor.Current.map.Water.ElevationDeep);
			AbyssElevation.SetValue(ScmapEditor.Current.map.Water.ElevationAbyss);

			ColorLerpXElevation.SetValue(ScmapEditor.Current.map.Water.ColorLerp.x);
			ColorLerpYElevation.SetValue(ScmapEditor.Current.map.Water.ColorLerp.y);

			WaterColor.SetColorField(ScmapEditor.Current.map.Water.SurfaceColor.x, ScmapEditor.Current.map.Water.SurfaceColor.y, ScmapEditor.Current.map.Water.SurfaceColor.z);
            SunColor.SetColorField(ScmapEditor.Current.map.Water.SunColor.x, ScmapEditor.Current.map.Water.SunColor.y, ScmapEditor.Current.map.Water.SunColor.z);
            SunDirection.Set(ScmapEditor.Current.map.Water.SunDirection.x, ScmapEditor.Current.map.Water.SunDirection.y, ScmapEditor.Current.map.Water.SunDirection.z);

			SunShininess.SetValue(ScmapEditor.Current.map.Water.SunShininess);
			UnitReflection.SetValue(ScmapEditor.Current.map.Water.UnitReflection);
			SkyReflection.SetValue(ScmapEditor.Current.map.Water.SkyReflection);
			SunReflection.SetValue(ScmapEditor.Current.map.Water.SunReflection);
			RefractionScale.SetValue(ScmapEditor.Current.map.Water.RefractionScale);

			WaterRamp.text = ScmapEditor.Current.map.Water.TexPathWaterRamp;
			Cubemap.text = ScmapEditor.Current.map.Water.TexPathCubemap;

			WaterSettings.interactable = HasWater.isOn;
			LightSettings.interactable = !UseLightingSettings.isOn;
			
			SetWaves();

			Loading = false;

			if (Undo)
			{
				UpdateScmap(true);
			}
		}

		private void SetWaves()
		{
			Waves0.SetTexPath(ScmapEditor.Current.map.Water.WaveTextures[0].TexPath);
			Waves0.SetFrequency(ScmapEditor.Current.map.Water.WaveTextures[0].NormalRepeat);
			Waves0.SetMovement(ScmapEditor.Current.map.Water.WaveTextures[0].NormalMovement);
			Waves1.SetTexPath(ScmapEditor.Current.map.Water.WaveTextures[1].TexPath);
			Waves1.SetFrequency(ScmapEditor.Current.map.Water.WaveTextures[1].NormalRepeat);
			Waves1.SetMovement(ScmapEditor.Current.map.Water.WaveTextures[1].NormalMovement);
			Waves2.SetTexPath(ScmapEditor.Current.map.Water.WaveTextures[2].TexPath);
			Waves2.SetFrequency(ScmapEditor.Current.map.Water.WaveTextures[2].NormalRepeat);
			Waves2.SetMovement(ScmapEditor.Current.map.Water.WaveTextures[2].NormalMovement);
			Waves3.SetTexPath(ScmapEditor.Current.map.Water.WaveTextures[3].TexPath);
			Waves3.SetFrequency(ScmapEditor.Current.map.Water.WaveTextures[3].NormalRepeat);
			Waves3.SetMovement(ScmapEditor.Current.map.Water.WaveTextures[3].NormalMovement);
		}

		void UpdateScmap(bool Maps)
		{
			ScmapEditor.Current.SetWaterTextures();
			ScmapEditor.Current.SetWater();

			if (Maps)
			{
				GenerateControlTex.StopAllTasks();
				TerrainMenu.RegenerateMaps();
			}
		}

		bool UndoRegistered = false;
		public void ElevationChangeBegin()
		{
			if (Loading || UndoRegistered)
				return;
			//Undo.Current.RegisterWaterElevationChange();
			//UndoRegistered = true;
			//Debug.Log("Begin");
		}

		public void ElevationChanged()
		{
			if (Loading)
				return;



			float water = WaterElevation.value;
			float depth = DepthElevation.value;
			float abyss = AbyssElevation.value;

			if (water < 1)
				water = 1;
			else if (water > 256)
				water = 256;

			if (depth > water)
				depth = water;
			else if (depth < 0)
				depth = 0;

			if (abyss > depth)
				abyss = depth;
			else if (abyss < 0)
				abyss = 0;

			bool AnyChanged = ScmapEditor.Current.map.Water.HasWater != HasWater.isOn
				|| !Mathf.Approximately(ScmapEditor.Current.map.Water.Elevation, water)
				|| !Mathf.Approximately(ScmapEditor.Current.map.Water.ElevationDeep, depth)
				|| !Mathf.Approximately(ScmapEditor.Current.map.Water.ElevationAbyss, abyss)
				;

			if (!AnyChanged)
			{
				return;
			}

			Undo.RegisterUndo(new UndoHistory.HistoryWaterElevation());
			if (!UndoRegistered)
				ElevationChangeBegin();
			UndoRegistered = false;

			ScmapEditor.Current.map.Water.HasWater = HasWater.isOn;
			ScmapEditor.Current.map.Water.Elevation = water;
			ScmapEditor.Current.map.Water.ElevationDeep = depth;
			ScmapEditor.Current.map.Water.ElevationAbyss = abyss;

			Loading = true;
			WaterElevation.SetValue(water);
			DepthElevation.SetValue(depth);
			AbyssElevation.SetValue(abyss);
			WaterSettings.interactable = HasWater.isOn;
			Loading = false;

			UpdateScmap(true);

		}

		public void WaterSettingsChangedBegin()
		{
			if (Loading || UndoRegistered)
				return;
			//Undo.Current.RegisterWaterSettingsChange();
			//UndoRegistered = true;
		}

		public void WaterSettingsChanged(bool Slider)
		{
			if (Loading)
				return;

			if (!AnyChanged())
				return;

			if (!UndoRegistered)
			{
				Undo.RegisterUndo(new UndoHistory.HistoryWaterSettings());
				UndoRegistered = true;
				WaterSettingsChangedBegin();

			}

			if(!Slider)
				UndoRegistered = false;

			ScmapEditor.Current.map.Water.ColorLerp.x = ColorLerpXElevation.value;
			ScmapEditor.Current.map.Water.ColorLerp.y = ColorLerpYElevation.value;

			ScmapEditor.Current.map.Water.SurfaceColor = WaterColor.GetVectorValue();
			ScmapEditor.Current.map.Water.SunColor = SunColor.GetVectorValue();
			ScmapEditor.Current.map.Water.SunDirection = SunDirection;

			ScmapEditor.Current.map.Water.SunShininess = SunShininess.value;
			ScmapEditor.Current.map.Water.UnitReflection = UnitReflection.value;
			ScmapEditor.Current.map.Water.SkyReflection = SkyReflection.value;
			ScmapEditor.Current.map.Water.SunReflection = SunReflection.value;
			ScmapEditor.Current.map.Water.RefractionScale = RefractionScale.value;
			
			ScmapEditor.Current.map.Water.TexPathWaterRamp = WaterRamp.text;
			ScmapEditor.Current.map.Water.TexPathCubemap = Cubemap.text;
			
			ScmapEditor.Current.map.Water.WaveTextures[0].TexPath = Waves0.GetTexPath();
			ScmapEditor.Current.map.Water.WaveTextures[0].NormalRepeat = Waves0.GetScale() ;
			ScmapEditor.Current.map.Water.WaveTextures[0].NormalMovement = Waves0.GetMovement();
			ScmapEditor.Current.map.Water.WaveTextures[1].TexPath = Waves1.GetTexPath() ;
			ScmapEditor.Current.map.Water.WaveTextures[1].NormalRepeat = Waves1.GetScale() ;
			ScmapEditor.Current.map.Water.WaveTextures[1].NormalMovement = Waves1.GetMovement();
			ScmapEditor.Current.map.Water.WaveTextures[2].TexPath = Waves2.GetTexPath() ;
			ScmapEditor.Current.map.Water.WaveTextures[2].NormalRepeat = Waves2.GetScale() ;
			ScmapEditor.Current.map.Water.WaveTextures[2].NormalMovement = Waves2.GetMovement();
			ScmapEditor.Current.map.Water.WaveTextures[3].TexPath = Waves3.GetTexPath() ;
			ScmapEditor.Current.map.Water.WaveTextures[3].NormalRepeat = Waves3.GetScale() ;
			ScmapEditor.Current.map.Water.WaveTextures[3].NormalMovement = Waves3.GetMovement();
			
			LightSettings.interactable = !UseLightingSettings.isOn;
			
			UpdateScmap(false);
		}

		private bool AnyChanged()
		{
			return !Mathf.Approximately(ScmapEditor.Current.map.Water.ColorLerp.x, ColorLerpXElevation.value)
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.ColorLerp.y, ColorLerpYElevation.value)
		        || ScmapEditor.Current.map.Water.SurfaceColor != WaterColor.GetVectorValue()
		        || ScmapEditor.Current.map.Water.SunColor != SunColor.GetVectorValue()
		        || ScmapEditor.Current.map.Water.SunDirection != SunDirection
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.SunShininess, SunShininess.value)
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.UnitReflection, UnitReflection.value)
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.SkyReflection, SkyReflection.value)
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.SunReflection, SunReflection.value)
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.RefractionScale, RefractionScale.value)
		        || ScmapEditor.Current.map.Water.TexPathWaterRamp != WaterRamp.text
		        || ScmapEditor.Current.map.Water.TexPathCubemap != Cubemap.text
		        || ScmapEditor.Current.map.Water.WaveTextures[0].TexPath != Waves0.GetTexPath() 
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.WaveTextures[0].NormalRepeat, Waves0.GetScale()) 
		        || ScmapEditor.Current.map.Water.WaveTextures[0].NormalMovement != Waves0.GetMovement()
		        || ScmapEditor.Current.map.Water.WaveTextures[1].TexPath != Waves1.GetTexPath() 
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.WaveTextures[1].NormalRepeat, Waves1.GetScale()) 
		        || ScmapEditor.Current.map.Water.WaveTextures[1].NormalMovement != Waves1.GetMovement()
		        || ScmapEditor.Current.map.Water.WaveTextures[2].TexPath != Waves2.GetTexPath() 
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.WaveTextures[2].NormalRepeat, Waves2.GetScale()) 
		        || ScmapEditor.Current.map.Water.WaveTextures[2].NormalMovement != Waves2.GetMovement()
		        || ScmapEditor.Current.map.Water.WaveTextures[3].TexPath != Waves3.GetTexPath() 
		        || !Mathf.Approximately(ScmapEditor.Current.map.Water.WaveTextures[3].NormalRepeat, Waves3.GetScale()) 
		        || ScmapEditor.Current.map.Water.WaveTextures[3].NormalMovement != Waves3.GetMovement()
				;
		}

		public void selectTexture(InputField inputField)
		{
			if(ResourceBrowser.DragedObject == null || ResourceBrowser.DragedObject.ContentType != ResourceObject.ContentTypes.Texture)
				return;
			if (!ResourceBrowser.Current.gameObject.activeSelf)
				return;
			inputField.text = ResourceBrowser.Current.LoadedPaths[ResourceBrowser.DragedObject.InstanceId];
			ResourceBrowser.ClearDrag();
			WaterSettingsChanged(false);
		}
		
		public void ClickWaterRampButton()
		{
			ResourceBrowser.Current.LoadWaterRampTexture(WaterRamp.text);
		}
		
		public void ClickSkyCubeButton()
		{
			ResourceBrowser.Current.LoadSkyCube(Cubemap.text);
		}
		
		public void ClickWave0Button()
		{
			ResourceBrowser.Current.LoadWaveTexture(Waves0.TexPath.text);
		}
		
		public void ClickWave1Button()
		{
			ResourceBrowser.Current.LoadWaveTexture(Waves1.TexPath.text);
		}
		
		public void ClickWave2Button()
		{
			ResourceBrowser.Current.LoadWaveTexture(Waves2.TexPath.text);
		}
		
		public void ClickWave3Button()
		{
			ResourceBrowser.Current.LoadWaveTexture(Waves3.TexPath.text);
		}
		
		public void ClickLightSettingsToggle()
		{
			if (UseLightingSettings.isOn)
			{
				Map map = ScmapEditor.Current.map;
				SunColor.SetColorField(map.SunColor.x * (map.LightingMultiplier - map.ShadowFillColor.x),
					map.SunColor.y * (map.LightingMultiplier - map.ShadowFillColor.y), 
					map.SunColor.z * (map.LightingMultiplier - map.ShadowFillColor.z));
				SunDirection = map.SunDirection;
				Cubemap.text = MapLuaParser.Current.EditMenu.LightingMenu.EnvCube.text;
			} else {
				SunDirection = new Vector3(0.09954818f, -0.9626309f, 0.2518569f);
			}
			WaterSettingsChanged(false);
		}
		
		public void ResetWaves()
		{
			Waves0.SetTexPath("/textures/engine/waves1_400m.dds");
			Waves0.SetFrequency(0.05f);
			Waves0.SetMovement(new Vector2(0f, -0.012f));
			Waves1.SetTexPath("/textures/engine/waves1_120m.dds");
			Waves1.SetFrequency(0.167f);
			Waves1.SetMovement(new Vector2(-0.00130236f, 0.00738606f));
			Waves2.SetTexPath("/textures/engine/waves1_40m.dds");
			Waves2.SetFrequency(0.5f);
			Waves2.SetMovement(new Vector2(-0.003f, 0.00519615f));
			Waves3.SetTexPath("/textures/engine/waves1_120m.dds");
			Waves3.SetFrequency(0.8f);
			Waves3.SetMovement(new Vector2(-0.00105f, -0.006f));
			
			WaterSettingsChanged(false);
		}

		#region Import/Export
		const string ExportPathKey = "WaterExport";
		static string DefaultPath
		{
			get
			{
				return EnvPaths.GetLastPath(ExportPathKey, EnvPaths.GetMapsPath() + MapLuaParser.Current.FolderName);
			}
		}
		public void ExportWater()
		{
			var extensions = new[]
			{
				new ExtensionFilter("Water settings", "scmwtr")
			};

			var path = StandaloneFileBrowser.SaveFilePanel("Export water", DefaultPath, "WaterSettings", extensions);

			if (string.IsNullOrEmpty(path))
				return;


			string data = JsonUtility.ToJson(ScmapEditor.Current.map.Water, true);

			File.WriteAllText(path, data);
			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(path));
		}

		public void ImportWater()
		{
			var extensions = new[]
			{
				new ExtensionFilter("Water settings", "scmwtr")
			};

			var paths = StandaloneFileBrowser.OpenFilePanel("Import water", DefaultPath, extensions, false);


			if (paths.Length <= 0 || string.IsNullOrEmpty(paths[0]))
				return;


			string data = File.ReadAllText(paths[0]);

			float WaterLevel = ScmapEditor.Current.map.Water.Elevation;
			float AbyssLevel = ScmapEditor.Current.map.Water.ElevationAbyss;
			float DeepLevel = ScmapEditor.Current.map.Water.ElevationDeep;

			ScmapEditor.Current.map.Water = JsonUtility.FromJson<WaterShader>(data);

			ScmapEditor.Current.map.Water.Elevation = WaterLevel;
			ScmapEditor.Current.map.Water.ElevationAbyss = AbyssLevel;
			ScmapEditor.Current.map.Water.ElevationDeep = DeepLevel;

			ReloadValues();

			UpdateScmap(true);
			EnvPaths.SetLastPath(ExportPathKey, System.IO.Path.GetDirectoryName(paths[0]));

		}
		#endregion

	}
}

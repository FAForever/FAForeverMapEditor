using UnityEngine;
using System.Collections;
using Microsoft.Win32;

public class EnvPaths : MonoBehaviour {

	public static string DefaultMapPath;
	public static string DefaultGamedataPath;
	public static string DefaultFafDataPath;
	public static string DefaultJavaPath;

	static Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
	//static RegistryKey regKey = Registry. .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");


	const string InstallationPath = "InstallationPath";
	const string FafDataPath = "FafDataPath";
	const string InstallationGamedata = "gamedata/";
	const string InstallationMods = "mods/";
	const string MapsPath = "MapsPath";
	const string BackupPath = "BackupPath";
	const string JavaPath = "JavaPath";
	const string ImagePath = "ImagePath";

	public static string GetInstallationPath() {
		return PlayerPrefs.GetString(InstallationPath, EnvPaths.DefaultGamedataPath);
	}

	public static void SetInstallationPath(string value) {
		value = value.Replace("\\", "/");
		if (value[value.Length - 1].ToString() != "/") value += "/";
		if (value[0].ToString() == "/") value = value.Remove(0, 1);

		if (value.ToLower().EndsWith(InstallationGamedata))
		{
			value = value.Remove(value.Length - InstallationGamedata.Length);
		}

		PlayerPrefs.SetString(InstallationPath, value);

		if (!System.IO.Directory.Exists(value))
		{
			GenericInfoPopup.ShowInfo("Wrong game installation path!\nCheck preferences.");
		}
    }

    public static string GetFafDataPath()
    {
        return PlayerPrefs.GetString(FafDataPath, EnvPaths.DefaultFafDataPath);
    }

    public static void SetFafDataPath(string value)
    {
        value = value.Replace("\\", "/");
        if (value[value.Length - 1].ToString() != "/") value += "/";
        if (value[0].ToString() == "/") value = value.Remove(0, 1);

        if (value.ToLower().EndsWith(InstallationGamedata))
        {
            value = value.Remove(value.Length - InstallationGamedata.Length);
        }

        PlayerPrefs.SetString(FafDataPath, value);

        if (!System.IO.Directory.Exists(value))
        {
            GenericInfoPopup.ShowInfo("Wrong faf installation path!\nCheck preferences.");
        }
    }
    
    public static string GetJavaPath()
    {
	    return PlayerPrefs.GetString(JavaPath, DefaultJavaPath);
    }

    public static void SetJavaPath(string value)
    {
	    value = value.Replace("\\", "/");
	    if (value[value.Length - 1].ToString() != "/") value += "/";
	    if (value[0].ToString() == "/") value = value.Remove(0, 1);

	    PlayerPrefs.SetString(JavaPath, value);

	    if (!System.IO.Directory.Exists(value))
	    {
		    GenericInfoPopup.ShowInfo("This directory does not exist!");
	    }
    }
        
    public static string GetImagePath()
    {
	    return PlayerPrefs.GetString(ImagePath, "");
    }

    public static void SetImagePath(string value)
    {
	    value = value.Replace("\\", "/");
	    if (value[value.Length - 1].ToString() != "/") value += "/";
	    if (value[0].ToString() == "/") value = value.Remove(0, 1);

	    PlayerPrefs.SetString(ImagePath, value);

	    if (!System.IO.Directory.Exists(value))
	    {
		    GenericInfoPopup.ShowInfo("This directory does not exist!");
	    }
    }

    /// <summary>
    /// All directorys from where editor will load *.scd and *.nx2 files
    /// </summary>
    public static string[] LoadGamedataPaths
	{
		get
		{
			if(AllowMods)
				return new string[] { GamedataPath, FAFDataPath, GamedataModsPath, UserModsPath };

			return new string[] { GamedataPath, FAFDataPath };
		}
	}

	public static bool GamedataExist
	{
		get
		{
			return System.IO.Directory.Exists(GamedataPath);
		}
	}

	public static bool AllowMods
	{
		get
		{
			return true;
		}
	}

	public static string GamedataPath{
		get
		{
			return GetInstallationPath() + InstallationGamedata;
		}
	}

	public static string GamedataModsPath
	{
		get
		{
			return GetInstallationPath() + InstallationMods;
		}
	}

	public static string UserModsPath
	{
		get
		{
			return MyDocuments.Replace("\\", "/") + "/My Games/Gas Powered Games/Supreme Commander Forged Alliance/Mods/";
		}
	}

	public static string FAFDataPath
	{
		get
		{
            return GetFafDataPath() + InstallationGamedata;
        }
	}

	public static string CurrentGamedataPath = "";

	public static void SetMapsPath(string value) {
		value = value.Replace("\\", "/");
		if (value[value.Length - 1].ToString() != "/") value += "/";
		if (value[0].ToString() == "/") value = value.Remove(0, 1);

		PlayerPrefs.SetString(MapsPath, value);

		if (!System.IO.Directory.Exists(value))
		{
			GenericInfoPopup.ShowInfo("Wrong maps path!\nCheck preferences.");
		}
	}

	public static string GetMapsPath() {
		return PlayerPrefs.GetString(MapsPath, EnvPaths.DefaultMapPath);
	}

	public static void SetBackupPath(string value)
	{

		if (!string.IsNullOrEmpty(value))
		{
			value = value.Replace("\\", "/");
			if (value[value.Length - 1].ToString() != "/") value += "/";
			if (value.Length > 0 && value[0].ToString() == "/") value = value.Remove(0, 1);
		}

		PlayerPrefs.SetString(BackupPath, value);
	}

	public static string GetBackupPath()
	{
		return PlayerPrefs.GetString(BackupPath, "");
	}

	#region Auto Generate
	public static void GenerateDefaultPaths() {
		GenerateMapPath();
		GenerateFafDataPath();
		GenerateGamedataPath();
		GenerateJavaPath();
	}

	public static void GenerateMapPath() {
		DefaultMapPath = MyDocuments.Replace("\\", "/") + "/My Games/Gas Powered Games/Supreme Commander Forged Alliance/Maps/";
		if (!System.IO.Directory.Exists(DefaultMapPath)) {
			Debug.LogWarning("Default map directory does not exist: " + DefaultMapPath);
			DefaultMapPath = "maps/";
		}
	}
    public static void GenerateFafDataPath()
    {
        DefaultFafDataPath = EnvPaths.ProgramData + "/FAForever/";
        if (!System.IO.Directory.Exists(DefaultFafDataPath))
        {
            Debug.LogWarning("Default FAF directory does not exist: " + DefaultFafDataPath);
            DefaultFafDataPath = "";
        }
    }

    public static void GenerateGamedataPath() {
		DefaultGamedataPath = FindByDisplayName(regKey, "Supreme Commander: Forged Alliance").Replace("\\", "/");



		if (!string.IsNullOrEmpty(DefaultGamedataPath)) {
			if (!DefaultGamedataPath.EndsWith("/"))
				DefaultGamedataPath += "/";

			if (!System.IO.Directory.Exists(DefaultGamedataPath)) {
				Debug.LogWarning("Installation directory does not exist: " + DefaultGamedataPath);
				DefaultGamedataPath = "";
			}
		}

		//Debug.Log ("Found: " + DefaultGamedataPath);

		if (string.IsNullOrEmpty(DefaultGamedataPath))
			DefaultGamedataPath = "gamedata/";
	}
    
	public static void GenerateJavaPath()
	{
		DefaultJavaPath = EnvPaths.Programs + "/FAF Client/jre/bin";
		if (!System.IO.Directory.Exists(DefaultJavaPath))
		{
			Debug.LogWarning("Default Java directory does not exist: " + DefaultJavaPath);
			DefaultJavaPath = "";
		}
	}


	private static string FindByDisplayName(Microsoft.Win32.RegistryKey parentKey, string name)
	{

		string[] nameList = parentKey.GetSubKeyNames();
		for (int i = 0; i < nameList.Length; i++)
		{
			Microsoft.Win32.RegistryKey regKey = parentKey.OpenSubKey(nameList[i]);
			try
			{

				if (regKey.GetValue("DisplayName").ToString() == name)
				{
					return regKey.GetValue("InstallLocation").ToString();
				}
				else {
					//Debug.Log(nameList[i] + ", " + regKey.Name + " : " + regKey.GetValue("InstallLocation").ToString());
				}
			}
			catch {
				//Debug.LogError ("AAA");
			}
		}
		return "";
	}
	#endregion

	#region SystemPaths
	public static string MyDocuments
	{
		get {
			return System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments).Replace("\\", "/");
		}
	}

	public static string ProgramData
	{
		get
		{
			return System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData);
		}
	}
	
	public static string Programs
	{
		get
		{
			return System.Environment.GetFolderPath(System.Environment.SpecialFolder.Programs);
		}
	}


	#endregion

	#region Save
	public static string GetLastPath(string key, string defaultPath)
	{
		if (string.IsNullOrEmpty(defaultPath))
			defaultPath = MyDocuments;

		string SavedPath = PlayerPrefs.GetString(key, defaultPath);

		if (System.IO.Directory.Exists(SavedPath))
			return SavedPath;

		SavedPath = MyDocuments;

		if (System.IO.Directory.Exists(SavedPath))
			return SavedPath;

		return "";
	}

	public static void SetLastPath(string key, string path)
	{
		PlayerPrefs.SetString(key, path);
	}

	#endregion
}

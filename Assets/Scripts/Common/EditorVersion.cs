using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System;

public class EditorVersion : MonoBehaviour
{
	private const string EditorBuildVersion = "v1.0";
	private const string EditorPrereleaseTag = "";  // add rcX here for release candidates
	private static string FoundUrl;
	public bool SearchForNew = true;

	string VersionString
	{
		get
		{
			if (EditorPrereleaseTag.Length == 0)
				return EditorBuildVersion;

			return EditorBuildVersion + "-" + EditorPrereleaseTag;
		}
	}

	void Start()
	{
		GetComponent<Text>().text = VersionString;
		if(SearchForNew)
			StartCoroutine(FindLatest());
	}

	public string url = "https://github.com/FAForever/FAForeverMapEditor/releases/latest";
	IEnumerator FindLatest()
	{
		DownloadHandler dh = new DownloadHandlerBuffer();
        using UnityWebRequest www = new(url, "GET", dh, null);
        yield return www.SendWebRequest();
        string[] Tags = www.url.Replace("\\", "/").Split("/".ToCharArray());
        if (Tags.Length > 0)
        {
	        string LatestRelease = Tags[^1];
            FoundUrl = www.url;
            Debug.Log("Editor version: " + VersionString);
            if (shouldUpdate(LatestRelease))
            {
                Debug.Log("New version available: " + LatestRelease);
                GenericPopup.ShowPopup(GenericPopup.PopupTypes.TwoButton, "New version",
                    "New version of Map Editor is avaiable.\nCurrent: " + VersionString + "\t\tNew: " + LatestRelease + "\nDo you want to download it now?",
                    "Download", DownloadLatest,
                    "Cancel", CancelDownload
                    );
            }
        }
    }

	static bool shouldUpdate(string LatestRelease)
	{
		double Latest = Math.Round(BuildFloat(LatestRelease), 3);
		double Current = Math.Round(BuildFloat(EditorBuildVersion), 3);
		if (Latest == 0 || Current == 0) return false;
		if (EditorPrereleaseTag.Length > 0) Current -= 0.0001;
                    
		return Current < Latest;
	}

	static string CleanBuildVersion(string tag)
	{
		return tag.ToLower().Replace(" ", "").Replace("v", "");
	}

	static float BuildFloat(string tag)
	{
        string ToParse = CleanBuildVersion(tag);

        if (float.TryParse(ToParse, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture.NumberFormat, out float Found))
        {
            return Found;
        }
        else
        {
            Debug.LogWarning("Wrong tag! Cant parse build version to float! Tag: " + ToParse);
            return 0;
        }
    }

	public void DownloadLatest()
	{
		Application.OpenURL(FoundUrl);
	}

	public void CancelDownload()
	{

	}
}

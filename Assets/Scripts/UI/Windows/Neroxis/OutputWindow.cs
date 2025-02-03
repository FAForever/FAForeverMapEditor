using UnityEngine;
using UnityEngine.UI;

namespace Ozone.UI
{
	public class OutputWindow : MonoBehaviour
	{
		
		public GameObject Window;
		public Text JavaOutput;
		public Image Spinner;

		public void Initialize()
		{
			JavaOutput.text = "";
			Window.SetActive(true);
			Spinner.enabled = true;
		}

		public void WriteOutput(string text)
		{
			JavaOutput.text += text + "\n";
			Debug.Log(text);
		}

		public void Close(int exitCode)
		{
			HideSpinner();
			float timeout = 2;
			if (exitCode != 0) timeout = 5;
			Invoke(nameof(HideWindow), timeout);
		}

		private void HideSpinner()
		{
			Spinner.enabled = false;
		}

		private void HideWindow()
		{
			Window.SetActive(false);
		}
	}
}
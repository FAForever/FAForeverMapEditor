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

		public void Close()
		{
			HideSpinner();
			Invoke(nameof(HideWindow), 1);
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
using System;
using UnityEngine;
using UnityEngine.UI;

namespace FAF.MapEditor
{
	public class UnitFilterButton : MonoBehaviour
	{
		public string Key;
		public string IconPath;

		public Image Background;
		public Text Label;
		public RawImage Icon;
		public Tooltip IconTooltip;

		public Color OffColor = new(1, 1, 1, 0.06f);
		public Color OnColor = new(0.2f, 0.45f, 0.85f, 1f);

		public bool On { get; private set; }
		public Action Toggled;

		public void Toggle()
		{
			On = !On;
			Background.color = On ? OnColor : OffColor;
			Toggled?.Invoke();
		}

		public void SetIcon(Texture2D texture)
		{
			Icon.texture = texture;
			Icon.rectTransform.sizeDelta = new Vector2(texture.width, texture.height);
			Icon.gameObject.SetActive(true);
			Label.gameObject.SetActive(false);
			IconTooltip.enabled = true;
		}
	}
}

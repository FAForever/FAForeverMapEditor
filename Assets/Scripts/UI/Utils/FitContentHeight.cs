using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// I love love love love decade-old problems with no solution
// ref: https://discussions.unity.com/t/resizing-scrollrects/568240
namespace Ozone.UI
{
	[RequireComponent(typeof(ScrollRect))]
	public class FitContentHeight : UIBehaviour, ILayoutElement
	{
		public float MinHeight = 24;
		public float MaxHeight = 250;

		ScrollRect Scroll;
		float LastContentHeight = -1;

		public int layoutPriority => 2;
		public float minWidth => -1;
		public float preferredWidth => -1;
		public float flexibleWidth => -1;
		public float minHeight => MinHeight;
		public float flexibleHeight => -1;

		public float preferredHeight
		{
			get
			{
				if (!FindRefs())
				{
					return -1;
				}

				RectTransform self = (RectTransform)transform;
				float chrome = self.rect.height - Scroll.viewport.rect.height;
				return Mathf.Clamp(Scroll.content.rect.height + chrome, MinHeight, MaxHeight);
			}
		}

		public void CalculateLayoutInputHorizontal() { }
		public void CalculateLayoutInputVertical() { }

		void Update()
		{
			if (FindRefs() && !Mathf.Approximately(Scroll.content.rect.height, LastContentHeight))
			{
				LastContentHeight = Scroll.content.rect.height;
				LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
			}
		}

		bool FindRefs()
		{
			if (Scroll == null)
			{
				Scroll = GetComponent<ScrollRect>();
			}

			return Scroll != null && Scroll.content != null && Scroll.viewport != null;
		}
	}
}

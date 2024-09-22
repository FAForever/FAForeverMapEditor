﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UndoHistory;
using EditMap;
using MapLua;

namespace UndoHistory
{
	public class HistoryAreaChange : HistoryObject
	{

		private AreaChangeParam parameter;
		public class AreaChangeParam : HistoryParameter
		{
			public MapLua.SaveLua.Areas Area;

			public AreaChangeParam(MapLua.SaveLua.Areas Area)
			{
				this.Area = Area;
			}
		}


		SaveLua.Areas Area;
		public string Name;
		public float X;
		public float Y;
		public float Width;
		public float Height;

		public override void Register(HistoryParameter Param)
		{
			parameter = Param as AreaChangeParam;
			Area = parameter.Area;
			Name = Area.Name;
			X = Area.rectangle.x;
			Y = Area.rectangle.y;
			Width = Area.rectangle.width;
			Height = Area.rectangle.height;

		}


		public override void DoUndo()
		{
			if (!RedoGenerated)
				Undo.RegisterRedo(new HistoryAreaChange(), new AreaChangeParam(Area));
			RedoGenerated = true;
			DoRedo();
		}

		public override void DoRedo()
		{

			Area.Name = Name;
			Area.rectangle = new Rect(X, Y, Width, Height);


			Undo.Current.EditMenu.ChangeCategory(0);
			Undo.Current.EditMenu.MapInfoMenu.ChangePage(2);
			AreaInfo.Current.ToggleSelected();
			AreaInfo.Current.UpdateList();
		}
	}
}
#region ================= Namespaces

using System;
using System.Windows.Media;
using mxd.SQL2.DataReaders;

#endregion

namespace mxd.SQL2.Items
{
	public class MapItem : AbstractItem
	{
		#region ================= Default items

		public static readonly MapItem Default = new MapItem(NAME_NONE, ResourceType.NONE);
		public static readonly MapItem Random = new MapItem(NAME_RANDOM, ResourceType.NONE);

		#endregion

		#region ================= Variables

		private readonly string maptitle; // "The Introduction"
		private readonly ResourceType restype;

		#endregion

		#region ================= Properties

		public string MapTitle => maptitle;
		public ResourceType ResourceType => restype;
		public override ItemType Type => ItemType.MAP;

		#endregion

		#region ================= Constructors

		// Map title, e1m1 
		public MapItem(string title, string mapname, ResourceType restype) : base(mapname + " | " + title, mapname)
		{
			this.maptitle = title;
			this.restype = restype;
			SetColor();
		}

		// e1m1
		public MapItem(string mapname, ResourceType restype) : base(mapname, mapname)
		{
			this.maptitle = mapname;
			this.restype = restype;
			SetColor();
		}

		#endregion

		#region ================= Methods

		private void SetColor()
		{
			switch(restype)
			{
				case ResourceType.NONE: break; // Already set in AbstractItem
				case ResourceType.FOLDER: foreground = Brushes.Black; break;
				case ResourceType.PAK: foreground = Brushes.DarkSlateGray; break;
				case ResourceType.PK3: foreground = Brushes.MidnightBlue; break;
				default: throw new NotImplementedException("Unknown ResourceType!");
			}
		}

		#endregion
	}
}

namespace mxd.SQL2.Items
{
	public class MapItem : AbstractItem
	{
		#region ================= Default items

		public static readonly MapItem Default = new MapItem(NAME_NONE);
		public static readonly MapItem Random = new MapItem(NAME_RANDOM);

		#endregion

		#region ================= Variables

		private readonly string maptitle; // "The Introduction"

		#endregion

		#region ================= Properties

		public string MapTitle => maptitle;
		public override ItemType Type => ItemType.MAP;

		#endregion

		#region ================= Constructors

		// Map title, e1m1 
		public MapItem(string title, string mapname) : base(mapname + " | " + title, mapname)
		{
			maptitle = title;
		}

		// e1m1
		public MapItem(string mapname) : base(mapname, mapname)
		{
			maptitle = mapname;
		}

		#endregion
	}
}

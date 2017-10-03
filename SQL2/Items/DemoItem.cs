#region ================= Namespaces

using System.IO;

#endregion

namespace mxd.SQL2.Items
{
	public class DemoItem : AbstractItem
	{
		#region ================= Default items

		public static readonly DemoItem None = new DemoItem(NAME_NONE);

		#endregion

		#region ================= Variables

		private readonly string mapfilename;
		private readonly string maptitle;
        private readonly bool isinvalid;

        #endregion

        #region ================= Properties

        // Value: demos\somedemo.dem
        // Title: demos\somedemo.dem | map: Benis Devastation
        public string MapFileName => mapfilename; // maps/somemap.bsp
        public string MapTitle => maptitle; // Benis Devastation
        public bool IsInvalid => isinvalid;

	    public override ItemType Type => ItemType.DEMO;
        private new bool IsRandom; // No random demos

		#endregion

		#region ================= Constructors

		private DemoItem(string name) : base(name, "")
		{
			this.maptitle = name;
		}

		public DemoItem(string filename, string mapfilename, string maptitle) : base(filename + " | map: " + maptitle, filename)
		{
			this.mapfilename = mapfilename;
			this.maptitle = maptitle;
		}

		public DemoItem(string filename, string message) : base(filename + " | " + message, filename)
		{
            this.isinvalid = true;
			this.maptitle = Path.GetFileName(filename);
		}

		#endregion
	}
}
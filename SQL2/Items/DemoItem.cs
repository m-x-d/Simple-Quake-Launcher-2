#region ================= Namespaces

using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using mxd.SQL2.DataReaders;

#endregion

namespace mxd.SQL2.Items
{
	public class DemoItem : AbstractItem
	{
		#region ================= Default items

		public static readonly DemoItem None = new DemoItem(NAME_NONE);

		#endregion

		#region ================= Variables

		private readonly string modname; // Stored only on QWD demos, so can be empty...
		private readonly string mapfilepath;
		private readonly string maptitle;
		private readonly bool isinvalid;
		private readonly ResourceType restype;

		protected override ItemType type => ItemType.DEMO;

		#endregion

		#region ================= Properties

		// Value: demos\somedemo.dem
		// Title: demos\somedemo.dem | map: Benis Devastation
		public string ModName => modname; // xatrix / id1 etc.
		public string MapFilePath => mapfilepath; // maps/somemap.bsp
		public string MapTitle => maptitle; // Benis Devastation
		public bool IsInvalid => isinvalid;
		public ResourceType ResourceType => restype;

		private new bool IsRandom; // No random demos

		#endregion

		#region ================= Constructors

		private DemoItem(string name) : base(name, "")
		{
			this.maptitle = name;
		}

		// "demos\dm3_demo.dem", "maps\dm3.bsp", "Whatever Title DM3 Has"
		public DemoItem(string filename, string mapfilepath, string maptitle, ResourceType restype) : base(filename + " | map: " + maptitle, filename)
		{
			this.modname = string.Empty;
			this.mapfilepath = mapfilepath;
			this.maptitle = maptitle;
			this.restype = restype;
			SetColor();
		}

		// "qw", "demos\dm3_demo.dem", "maps\dm3.bsp", "Whatever Title DM3 Has"
		public DemoItem(string modname, string filename, string mapfilepath, string maptitle, ResourceType restype) : base(filename + " | map: " + maptitle, filename)
		{
			this.modname = modname;
			this.mapfilepath = mapfilepath;
			this.maptitle = maptitle;
			this.restype = restype;
			SetColor();
		}

		public DemoItem(string filename, string message, ResourceType restype) : base((string.IsNullOrEmpty(message) ? filename : filename + " | " + message), filename)
		{
			this.isinvalid = true;
			this.maptitle = Path.GetFileName(filename);
			this.restype = restype;
			SetColor();
		}

		#endregion

		#region ================= Methods

		private void SetColor()
		{
			if(isinvalid)
			{
				foreground = Brushes.DarkRed;
				return;
			}

			switch(restype)
			{
				case ResourceType.NONE: break; // Already set in AbstractItem
				case ResourceType.FOLDER: foreground = SystemColors.ActiveCaptionTextBrush; break;
				case ResourceType.PAK: foreground = Brushes.DarkGreen; break;
				case ResourceType.PK3: foreground = Brushes.DarkBlue; break;
				default: throw new NotImplementedException("Unknown ResourceType!");
			}
		}

		#endregion
	}
}
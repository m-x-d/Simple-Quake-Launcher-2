#region ================= Namespaces

using System.IO;
using System.Windows.Media;

#endregion

namespace mxd.SQL2.Items
{
	public class EngineItem : AbstractItem
	{
		#region ================= Variables

		private string filename;
		private ImageSource icon;

		protected override ItemType type => ItemType.ENGINE;

		#endregion

		#region ================= Properties

		// Value: quake.exe
		// Title: quake
		public string FileName => filename; // c:\games\quake\quake.exe
		public ImageSource Icon => icon;

		private new bool IsRandom; // No random engines

		#endregion

		#region ================= Constructor

		public EngineItem(ImageSource icon, string path) : base(Path.GetFileNameWithoutExtension(path), Path.GetFileName(path))
		{
			this.icon = icon;
			this.filename = path;
		}

		#endregion
	}
}

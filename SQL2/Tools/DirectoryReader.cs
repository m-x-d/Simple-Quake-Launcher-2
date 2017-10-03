#region ================= Namespaces

using System.Collections.Generic;
using System.IO;
using System.Text;
using mxd.SQL2.Games;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	public static class DirectoryReader
	{
        #region ================= Maps

        public static void GetMaps(string modpath, Dictionary<string, MapItem> mapslist, GameHandler.GetMapInfoDelegate getmapinfo)
		{
			DirectoryInfo mapdir = new DirectoryInfo(Path.Combine(modpath, "maps"));
			if(!mapdir.Exists) return;

			// Get the map files
			string[] mapnames = Directory.GetFiles(mapdir.FullName, "*.bsp");
			foreach(string file in mapnames)
			{
				if(!GameHandler.Current.EntryIsMap(file, mapslist)) continue;
				string mapname = Path.GetFileNameWithoutExtension(file);

				using(FileStream stream = File.OpenRead(file))
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
						mapslist.Add(mapname, getmapinfo(mapname, reader));
			}
		}

		public static bool ContainsMaps(string modpath)
		{
			DirectoryInfo mapdir = new DirectoryInfo(Path.Combine(modpath, "maps"));
			if(!mapdir.Exists) return false;

			// Get map files
			string prefix = GameHandler.Current.IgnoredMapPrefix;
			string[] mapnames = Directory.GetFiles(mapdir.FullName, "*.bsp");
			if(string.IsNullOrEmpty(prefix) && mapnames.Length > 0) return true;

			foreach(string file in mapnames)
			{
				string mapname = Path.GetFileNameWithoutExtension(file);
				if(!mapname.StartsWith(prefix)) return true;
			}

			return false;
		}

        #endregion

        #region ================= Demos

        public static List<DemoItem> GetDemos(string modpath)
        {
            var result = new List<DemoItem>();

            // Get demo files. Can be in subfolders
            string[] demofiles = Directory.GetFiles(modpath, "*.dem", SearchOption.AllDirectories);

            // Try to get data from demo files...
            foreach(string demofile in demofiles)
            {
                using(Stream stream = File.OpenRead(demofile))
                {
                    if(stream.Length > 67)
                    {
                        using(BinaryReader br = new BinaryReader(stream, Encoding.ASCII))
                        {
                            string relativedemopath = demofile.Substring(modpath.Length + 1);
                            GameHandler.Current.AddDemoItem(relativedemopath, result, br);
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}

#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using mxd.SQL2.Games;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.DataReaders
{
	public static class DirectoryReader
	{
		#region ================= Variables

		private const ResourceType restype = ResourceType.FOLDER;

		#endregion

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
				MapItem mapitem;

				if(getmapinfo != null)
				{
					using(FileStream stream = File.OpenRead(file))
						using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
							mapitem = getmapinfo(mapname, reader, restype);
				}
				else
				{
					mapitem = new MapItem(mapname, restype);
				}

				// Add to collection
				mapslist.Add(mapname, mapitem);
			}
		}

		public static bool ContainsMaps(string modpath)
		{
			DirectoryInfo mapdir = new DirectoryInfo(Path.Combine(modpath, "maps"));
			if(!mapdir.Exists) return false;

			// Get map files
			string prefix = GameHandler.Current.IgnoredMapPrefix;
			string[] mapnames = Directory.GetFiles(mapdir.FullName, "*.bsp");

			foreach(string file in mapnames)
			{
				if(!file.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase)) continue;
				if(string.IsNullOrEmpty(prefix) || !Path.GetFileName(file).StartsWith(prefix))
					return true;
			}

			return false;
		}

		#endregion

		#region ================= Demos

		public static List<DemoItem> GetDemos(string modpath, string demosfolder)
		{
			var result = new List<DemoItem>();
			if(!string.IsNullOrEmpty(demosfolder))
			{
				modpath = Path.Combine(modpath, demosfolder);
				if(!Directory.Exists(modpath)) return result;
			}

			// Get demo files. Can be in subfolders
			foreach(string ext in GameHandler.Current.SupportedDemoExtensions) // .dem, etc
			{
				// Try to get data from demo files...
				foreach(string file in Directory.GetFiles(modpath, "*" + ext, SearchOption.AllDirectories))
				{
					if(!file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) continue; // A searchPattern with a file extension (for example *.txt) of exactly three characters returns files 
																						  // having an extension of three or more characters, where the first three characters match the file extension specified in the searchPattern.
					string relativedemopath = file.Substring(modpath.Length + 1);

					using(var stream = File.OpenRead(file))
						using(var br = new BinaryReader(stream, Encoding.ASCII))
							GameHandler.Current.AddDemoItem(relativedemopath, result, br, restype);
				}
			}

			return result;
		}

		#endregion
	}
}

#region ================= Namespaces

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using mxd.SQL2.Games;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	public static class PK3Reader
	{
        #region ================= Maps

        public static void GetMaps(string modpath, Dictionary<string, MapItem> mapslist, GameHandler.GetMapInfoDelegate getmapinfo)
		{
			string[] zipfiles = Directory.GetFiles(modpath, "*.pk3");
			foreach(string file in zipfiles)
			{
			    using(var arc = ZipFile.OpenRead(file))
			    {
			        foreach(var entry in arc.Entries)
			        {
			            if(!GameHandler.Current.EntryIsMap(entry.FullName, mapslist)) continue;

			            string mapname = Path.GetFileNameWithoutExtension(entry.Name);
			            using(var stream = entry.Open())
			            {
                            using(var copy = new MemoryStream())
			                {
                                stream.CopyTo(copy);
                                copy.Position = 0;

                                using(var reader = new BinaryReader(copy))
                                {
                                    mapslist.Add(mapname, getmapinfo(mapname, reader));
                                }
                            }
                        }
			        }
			    }
            }
        }

		public static bool ContainsMaps(string modpath)
		{
			string[] zipfiles = Directory.GetFiles(modpath, "*.pk3");
			string prefix = GameHandler.Current.IgnoredMapPrefix;

            foreach(string file in zipfiles)
            {
                using(FileStream stream = File.OpenRead(file))
                {
                    using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        // Traverse file entries
                        while(reader.ReadString(4) == "PK\x03\x04")
                        {
                            reader.BaseStream.Position += 14;
                            int compressedsize = reader.ReadInt32();
                            reader.BaseStream.Position += 4;
                            short filenamelength = reader.ReadInt16();
                            short extralength = reader.ReadInt16();
                            string entry = reader.ReadString(filenamelength);

                            if(Path.GetDirectoryName(entry.ToLower()) == "maps" && Path.GetExtension(entry).ToLower() == ".bsp")
                            {
                                string mapname = Path.GetFileNameWithoutExtension(entry);
                                if(string.IsNullOrEmpty(prefix) || !mapname.StartsWith(prefix))
                                    return true;
                            }

                            reader.BaseStream.Position += extralength + compressedsize;
                        }
                    }
                }
            }

            // 2 SLOW 4 US
            /*foreach(string file in zipfiles)
		    {
		        using(var arc = ZipFile.OpenRead(file))
		        {
		            foreach(var entry in arc.Entries)
		            {
                        if(Path.GetDirectoryName(entry.FullName.ToLower()) == "maps" && Path.GetExtension(entry.FullName).ToLower() == ".bsp")
                        {
                            string mapname = Path.GetFileNameWithoutExtension(entry.Name);
                            if(string.IsNullOrEmpty(prefix) || !mapname.StartsWith(prefix))
                                return true;
                        }
		            }
		        }
		    }*/

            return false;
		}

        #endregion

        #region ================= Demos

	    public static List<DemoItem> GetDemos(string modpath)
	    {
            string[] zipfiles = Directory.GetFiles(modpath, "*.pk3");
            var result = new List<DemoItem>();

            foreach(string file in zipfiles)
            {
                using(var arc = ZipFile.OpenRead(file))
                {
                    foreach(var entry in arc.Entries)
                    {
                        if(!GameHandler.Current.EntryIsDemo(entry.Name)) continue;

                        using(var stream = entry.Open())
                        {
                            using(var copy = new MemoryStream())
                            {
                                stream.CopyTo(copy);
                                copy.Position = 0;

                                using(var reader = new BinaryReader(copy))
                                    GameHandler.Current.AddDemoItem(entry.FullName, result, reader);
                            }
                        }
                    }
                }
            }

	        return result;
	    }

	    #endregion
    }
}

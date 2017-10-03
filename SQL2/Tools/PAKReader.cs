#region ================= Namespaces

using System.Collections.Generic;
using System.IO;
using System.Text;
using mxd.SQL2.Games;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	public static class PAKReader
	{
        #region ================= Maps

        public static void GetMaps(string modpath, Dictionary<string, MapItem> mapslist, GameHandler.GetMapInfoDelegate getmapinfo)
		{
			string[] pakfiles = Directory.GetFiles(modpath, "*.pak");
			foreach(string file in pakfiles)
			{
				using(FileStream stream = File.OpenRead(file))
				{
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadString(4);
						if(id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for(int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadString(56).Trim(); // Read entry name
							int offset = reader.ReadInt32();
							reader.BaseStream.Position += 4; //skip unrelated stuff

							if(!GameHandler.Current.EntryIsMap(entry, mapslist)) continue;
							string mapname = Path.GetFileNameWithoutExtension(entry);

							// Store position
							long curpos = reader.BaseStream.Position;

							// Go to data location
							reader.BaseStream.Position = offset;
							mapslist.Add(mapname, getmapinfo(mapname, reader));

							// Restore position
							reader.BaseStream.Position = curpos;
						}
					}
				}
			}
		}

		public static bool ContainsMaps(string modpath)
		{
			string[] pakfiles = Directory.GetFiles(modpath, "*.pak");
			string prefix = GameHandler.Current.IgnoredMapPrefix;
			foreach(string file in pakfiles)
			{
				using(FileStream stream = File.OpenRead(file))
				{
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadString(4);
						if(id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for(int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadString(56).Trim(); // Read entry name
							reader.BaseStream.Position += 8; // Skip unrelated stuff

							if(Path.GetDirectoryName(entry.ToLower()) == "maps" && Path.GetExtension(entry).ToLower() == ".bsp")
							{
								string mapname = Path.GetFileNameWithoutExtension(entry);
								if(string.IsNullOrEmpty(prefix) || !mapname.StartsWith(prefix))
									return true;
							}
						}
					}
				}
			}

			return false;
		}

        #endregion

        #region ================= Demos

        public static List<DemoItem> GetDemos(string modpath)
        {
            string[] pakfiles = Directory.GetFiles(modpath, "*.pak");
            var result = new List<DemoItem>();

            // Get demo files
            foreach(string file in pakfiles)
            {
                using(FileStream stream = File.OpenRead(file))
                {
                    using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
                    {
                        // Read header
                        string id = reader.ReadString(4);
                        if(id != "PACK") continue;

                        int ftoffset = reader.ReadInt32();
                        int ftsize = reader.ReadInt32() / 64;

                        // Read file table
                        reader.BaseStream.Position = ftoffset;
                        for(int i = 0; i < ftsize; i++)
                        {
                            string entry = reader.ReadString(56).Trim(); // Read entry name
                            int offset = reader.ReadInt32();
                            reader.BaseStream.Position += 4; //skip unrelated stuff

                            if(!GameHandler.Current.EntryIsDemo(entry)) continue;
                            
                            // Store position
                            long curpos = reader.BaseStream.Position;

                            // Go to data location
                            reader.BaseStream.Position = offset;

                            // Add demo data
                            GameHandler.Current.AddDemoItem(entry, result, reader);

                            // Restore position
                            reader.BaseStream.Position = curpos;
                        }
                    }
                }
            }

            return result;
        }

        #endregion
    }
}

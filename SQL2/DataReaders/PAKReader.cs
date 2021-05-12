#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using mxd.SQL2.Games;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.DataReaders
{
	public static class PAKReader
	{
		#region ================= Variables

		private const ResourceType restype = ResourceType.PAK;

		#endregion

		#region ================= Maps

		public static void GetMaps(string modpath, Dictionary<string, MapItem> mapslist, GameHandler.GetMapInfoDelegate getmapinfo)
		{
			string[] pakfiles = Directory.GetFiles(modpath, "*.pak");
			foreach(string file in pakfiles)
			{
				if(!file.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;
				using(FileStream stream = File.OpenRead(file))
				{
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadStringExactLength(4);
						if(id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for(int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadStringExactLength(56).Trim(); // Read entry name
							int offset = reader.ReadInt32();
							reader.BaseStream.Position += 4; // Skip unrelated stuff

							if(!GameHandler.Current.EntryIsMap(entry, mapslist)) continue;
							string mapname = Path.GetFileNameWithoutExtension(entry);
							MapItem mapitem;

							if(getmapinfo != null)
							{
								// Store position
								long curpos = reader.BaseStream.Position;

								// Go to data location
								reader.BaseStream.Position = offset;
								mapitem = getmapinfo(mapname, reader, restype);
								
								// Restore position
								reader.BaseStream.Position = curpos;
							}
							else
							{
								mapitem = new MapItem(mapname, restype);
							}

							// Add to collection
							mapslist.Add(mapname, mapitem);
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
				if(!file.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;
				using(FileStream stream = File.OpenRead(file))
				{
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadStringExactLength(4);
						if(id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for(int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadStringExactLength(56).Trim(); // Read entry name
							reader.BaseStream.Position += 8; // Skip unrelated stuff

							if(Path.GetDirectoryName(entry.ToLower()) == "maps" && Path.GetExtension(entry).ToLower() == ".bsp"
								&& (string.IsNullOrEmpty(prefix) || !Path.GetFileName(entry).StartsWith(prefix)) )
							{
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

		public static List<DemoItem> GetDemos(string modpath, string demosfolder)
		{
			string[] pakfiles = Directory.GetFiles(modpath, "*.pak");
			var result = new List<DemoItem>();

			// Get demo files
			foreach(string file in pakfiles)
			{
				if(!file.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;
				using(FileStream stream = File.OpenRead(file))
				{
					using(BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadStringExactLength(4);
						if(id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for(int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadStringExactLength(56).Trim(); // Read entry name
							int offset = reader.ReadInt32();
							reader.BaseStream.Position += 4; //skip unrelated stuff

							// Skip unrelated files...
							if(!GameHandler.Current.SupportedDemoExtensions.Contains(Path.GetExtension(entry)))
								continue;

							if(!string.IsNullOrEmpty(demosfolder))
							{
								// If demosfolder is given, skip items not within said folder...
								if(!entry.StartsWith(demosfolder, StringComparison.OrdinalIgnoreCase))
									continue;

								// Strip "demos" from the entry name (Q2 expects path relative to "demos" folder)
								entry = entry.Substring(demosfolder.Length + 1);
							}

							// Store position
							long curpos = reader.BaseStream.Position;

							// Go to data location
							reader.BaseStream.Position = offset;

							// Add demo data
							GameHandler.Current.AddDemoItem(entry, result, reader, restype);

							// Restore position
							reader.BaseStream.Position = curpos;
						}
					}
				}
			}

			return result;
		}

		#endregion

		#region ================= Files

		public static bool ContainsFile(string modpath, string filename)
		{
			string[] pakfiles = Directory.GetFiles(modpath, "*.pak");

			foreach (string file in pakfiles)
			{
				if (!file.EndsWith(".pak", StringComparison.OrdinalIgnoreCase)) continue;
				using (FileStream stream = File.OpenRead(file))
				{
					using (BinaryReader reader = new BinaryReader(stream, Encoding.ASCII))
					{
						// Read header
						string id = reader.ReadStringExactLength(4);
						if (id != "PACK") continue;

						int ftoffset = reader.ReadInt32();
						int ftsize = reader.ReadInt32() / 64;

						// Read file table
						reader.BaseStream.Position = ftoffset;
						for (int i = 0; i < ftsize; i++)
						{
							string entry = reader.ReadStringExactLength(56).Trim(); // Read entry name
							reader.BaseStream.Position += 8; // Skip unrelated stuff

							if (string.CompareOrdinal(entry, filename) == 0)
								return true;
						}
					}
				}
			}

			return false;
		}

		#endregion
	}
}

#region ================= Namespaces

using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Hexen2
{
	public static class Hexen2DemoReader
	{
		#region ================= Constants

		private const int PROTOCOL_HEXEN2 = 19;

		private const int GAME_COOP = 0;
		private const int GAME_DEATHMATCH = 1;

		private const int SVC_PRINT = 8;
		private const int SVC_SERVERINFO = 11;

		#endregion

		#region ================= GetDemoInfo

		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader, ResourceType restype)
		{
			// CD track (string terminated by '\n' (0x0A in ASCII))
			if(!reader.SkipString(13, '\n')) return null;

			string maptitle = string.Empty;
			string mapfilepath = string.Empty;
			int protocol = 0;
			bool alldatafound = false;

			// Read blocks...
			while(reader.BaseStream.Position < reader.BaseStream.Length)
			{
				if(alldatafound) break;

				// Block header:
				// Block size (int32)
				// Camera angles (int32 x 3)

				int blocklength = reader.ReadInt32();
				long blockend = reader.BaseStream.Position + blocklength;
				if(blockend >= reader.BaseStream.Length) return null;
				reader.BaseStream.Position += 12; // Skip camera angles

				// Read messages...
				while(reader.BaseStream.Position < blockend)
				{
					if(alldatafound) break;

					int message = reader.ReadByte();
					switch(message)
					{
						// SVC_SERVERINFO (byte, 0x0B)
						// protocol (int32)  -> 19
						// maxclients (byte) should be in 1 .. 16 range
						// gametype (byte) - 0 -> coop, 1 -> deathmatch
						// map title (null-terminated string)
						// map filename (null-terminated string) "maps/mymap.bsp"

						case SVC_SERVERINFO:
							protocol = reader.ReadInt32();
							if(protocol != PROTOCOL_HEXEN2) return null;

							int maxclients = reader.ReadByte();
							if(maxclients < 1 || maxclients > 16) return null;

							int gametype = reader.ReadByte();
							if(gametype != GAME_COOP && gametype != GAME_DEATHMATCH) return null;

							maptitle = reader.ReadMapTitle(blocklength, QuakeFont.CharMap); // Map title can contain bogus chars...
							mapfilepath = reader.ReadString(blocklength);
							if(string.IsNullOrEmpty(mapfilepath)) return null;
							if(string.IsNullOrEmpty(maptitle)) maptitle = Path.GetFileName(mapfilepath);
							alldatafound = true;
							break;

						case SVC_PRINT:
							// The string reading stops at '\0' or after 0x7FF bytes. The internal buffer has only 0x800 bytes available.
							if(!reader.SkipString(2048)) return null;
							break;

						default:
							return null;
					}
				}
			}

			// Done
			return (alldatafound ? new DemoItem(demoname, mapfilepath, maptitle, restype) : null);
		}

		#endregion
	}
}

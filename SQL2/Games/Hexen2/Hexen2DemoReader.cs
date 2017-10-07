#region ================= Namespaces

using System.IO;
using mxd.SQL2.Data;
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

		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader)
		{
			// CD track (string terminated by '\n' (0x0A in ASCII))

			// Block header:
			// Block size (int32)
			// Camera angles (int32 x 3)

			// SVC_SERVERINFO
			// Null-terminated string

			// SVC_SERVERINFO (byte, 0x0B)
			// protocol (int32) -> 19
			// maxclients (byte) should be in 1 .. 16 range
			// gametype (byte) - 0 -> coop, 1 -> deathmatch
			// map title (null-terminated string)
			// map filename (null-terminated string) "maps/mymap.bsp"

			// CD-track: skip a decimal integer possibly with a leading '-', followed by a '\n'...
			if(!reader.SkipString(13, '\n')) return null;

			// Read block header...
			int blocklength = reader.ReadInt32();
			if(reader.BaseStream.Position + blocklength >= reader.BaseStream.Length) return null;
			reader.BaseStream.Position += 12; // Skip camera angles

			// Next should be SVC_PRINT...
			byte messagetype = reader.ReadByte();
			if(messagetype != SVC_PRINT) return null;

			// The string reading stops at '\0' or after 0x7FF bytes. The internal buffer has only 0x800 bytes available.
			if(!reader.SkipString(2048)) return null;
			messagetype = reader.ReadByte();

			// Next should be SVC_SERVERINFO...
			if(messagetype != SVC_SERVERINFO) return null;

			int protocol = reader.ReadInt32();
			if(protocol != PROTOCOL_HEXEN2) return null;

			int maxclients = reader.ReadByte();
			if(maxclients < 1 || maxclients > 16) return null;

			int gametype = reader.ReadByte();
			if(gametype != GAME_COOP && gametype != GAME_DEATHMATCH) return null;

			string maptitle = reader.ReadMapTitle(blocklength, QuakeFont.CharMap); // Map title can contain bogus chars...
			string mapfilepath = reader.ReadString(blocklength);
			if(string.IsNullOrEmpty(mapfilepath)) return null;
			if(string.IsNullOrEmpty(maptitle)) maptitle = Path.GetFileName(mapfilepath);

			// Done
			return new DemoItem(demoname, mapfilepath, maptitle);
		}

		#endregion
	}
}

#region ================= Namespaces

using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Games.Quake
{
	public static class QuakeDemoReader
	{
		#region ================= Constants

		private const int PROTOCOL_NETQUAKE = 15;
		private const int PROTOCOL_HEXEN2 = 19; // TODO: this should be it's own thing...
		private const int PROTOCOL_FITZQUAKE = 666;
		private const int PROTOCOL_RMQ = 999;

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

			// SVC_SERVERINFO (byte, 0x0B)
			// protocol (int32)  -> 666, 999 or 15
			// protocolflags (int32) - only when protocol == 999
			// maxclients (byte) should be in 1 .. 16 range
			// gametype (byte) - 0 -> coop, 1 -> deathmatch
			// map title (null-terminated string)
			// map filename (null-terminated string) "maps/mymap.bsp"

			// CD-track: skip a decimal integer possibly with a leading '-', followed by a '\n'...
			if(!SkipString(reader, 13, '\n')) return null;

			// Read block header...
			long blockend = reader.ReadUInt32() + reader.BaseStream.Position;
			reader.BaseStream.Position += 12; // Skip camera angles

			// Next should be SVC_SERVERINFO or SVC_PRINT...
			byte messagetype = reader.ReadByte();

			// Skip SVC_PRINT?..
			if(messagetype == SVC_PRINT)
			{
				// The string reading stops at '\0' or after 0x7FF bytes. The internal buffer has only 0x800 bytes available.
				if(!SkipString(reader, 2048)) return null;
				messagetype = reader.ReadByte();
			}

			// Must be SVC_SERVERINFO, right?..
			if(messagetype != SVC_SERVERINFO) return null;

			int protocol = reader.ReadInt32();
			if(protocol != PROTOCOL_NETQUAKE && protocol != PROTOCOL_FITZQUAKE && protocol != PROTOCOL_RMQ && protocol != PROTOCOL_HEXEN2) return null;
			if(protocol == PROTOCOL_RMQ) reader.BaseStream.Position += 4; // Skip RMQ protocolflags (int32)

			int maxclients = reader.ReadByte();
			if(maxclients < 1 || maxclients > 16) return null;

			int gametype = reader.ReadByte();
			if(gametype != GAME_COOP && gametype != GAME_DEATHMATCH) return null;

			string maptitle = ReadMapTitle(reader, blockend); // Map title can contain bogus chars...
			string mapfilepath = string.Empty;
			if(!ReadMapPath(reader, blockend, ref mapfilepath) || string.IsNullOrEmpty(mapfilepath)) return null;
			if(string.IsNullOrEmpty(maptitle)) maptitle = Path.GetFileName(mapfilepath);

			// Done
			return new DemoItem(demoname, mapfilepath, maptitle);
		}

		// Try to read null-terminated string of printable ASCII chars...
		private static bool ReadMapPath(BinaryReader reader, long maxoffset, ref string result)
		{
			while(reader.BaseStream.Position < maxoffset)
			{
				var c = reader.ReadChar();
				if(c == 0) break;
				if(c < 32 || c > 126) return false; // Not a printable char...
				result += c;
			}

			return true;
		}

		private static string ReadMapTitle(BinaryReader reader, long maxoffset)
		{
			string result = string.Empty;

			byte prevchar = 0;
			while(reader.BaseStream.Position < maxoffset)
			{
				var b = reader.ReadByte();

				// Stop on null char
				if(b == 0) break;

				// Replace newline with space
				if(b == 'n' && prevchar == '\\')
				{
					prevchar = b;
					result = result.Remove(result.Length - 1, 1) + ' ';
					continue;
				}

				// Trim extra spaces...
				if(!(prevchar == 32 && prevchar == b)) result += QuakeFont.CharMap[b];
				prevchar = b;
			}

			return result;
		}

		private static bool SkipString(BinaryReader reader, int maxlength, char terminator = '\0')
		{
			char c = '0';
			for(int i = 0; i < maxlength; i++)
			{
				c = reader.ReadChar();
				if(c == terminator) break;
			}

			return (c == terminator);
		}

		#endregion
	}
}

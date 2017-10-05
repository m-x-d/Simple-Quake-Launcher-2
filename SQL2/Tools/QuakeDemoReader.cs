#region ================= Namespaces

using System;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	public static class QuakeDemoReader
	{
		#region ================= Constants

		private const int PROTOCOL_NETQUAKE = 15;
		private const int PROTOCOL_FITZQUAKE = 666;
		private const int PROTOCOL_RMQ = 999;

		private const int GAME_COOP = 0;
		private const int GAME_DEATHMATCH = 1;

		//private const int SVC_PRINT = 8;
		private const int SVC_SERVERINFO = 11;

		#endregion

		#region ================= GetDemoInfo

		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader)
		{
			// SVC_SERVERINFO (byte)
			// protocol (int32)  -> 666, 999 or 15
			// protocolflags (int32) - only when protocol == 999
			// maxclients (byte) should be in 1 .. 16 range
			// gametype (byte) - 0 -> coop, 1 -> deathmatch
			// map title (null-terminated string)
			// map filename (null-terminated string) "maps/mymap.bsp"

			// CD-track: read a decimal integer possibly with a leading '-', followed by a '\n':
			char c = '0';
			for(int i = 0; i < 13; i++)
			{
				c = reader.ReadChar();
				if(c == '\n') break;
			}
			if(c != '\n') return null;

			// Now try to skip to SVC_SERVERINFO...
			long maxoffset = reader.BaseStream.Position + Math.Min(4096, reader.BaseStream.Length / 2);
			while(reader.BaseStream.Position < maxoffset)
			{
				byte b = reader.ReadByte();
				if(b == SVC_SERVERINFO)
				{
					long curoffset = reader.BaseStream.Position;

					// Try to read data...
					int protocol = reader.ReadInt32();
					if(protocol != PROTOCOL_NETQUAKE && protocol != PROTOCOL_FITZQUAKE && protocol != PROTOCOL_RMQ)
					{
						reader.BaseStream.Position = curoffset;
						continue;
					}

					if(protocol == PROTOCOL_RMQ) { int protocolflags = reader.ReadInt32(); }

					int maxclients = reader.ReadByte();
					if(maxclients < 1 || maxclients > 16)
					{
						reader.BaseStream.Position = curoffset;
						continue;
					}

					int gametype = reader.ReadByte();
					if(gametype != GAME_COOP && gametype != GAME_DEATHMATCH)
					{
						reader.BaseStream.Position = curoffset;
						continue;
					}

					string maptitle = ReadMapTitle(reader, maxoffset); // Map title can contain bogus chars...

					string mapfilepath = string.Empty;
					if(!ReadMapPath(reader, maxoffset, ref mapfilepath))
					{
						reader.BaseStream.Position = curoffset;
						continue;
					}

					if(string.IsNullOrEmpty(mapfilepath))
					{
						reader.BaseStream.Position = curoffset;
						continue;
					}

					return new DemoItem(demoname, mapfilepath, maptitle);
				}
			}

			// No dice...
			return null;
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

		#endregion
	}
}

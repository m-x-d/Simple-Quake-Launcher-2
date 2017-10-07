#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Quake2
{
	public static class Quake2DemoReader
	{
		#region ================= Constants

		private const int PROTOCOL_KMQ = 56;
		private const int PROTOCOL_R1Q2 = 35;
		private static readonly HashSet<int> ProtocolsQ2 = new HashSet<int> { 25, 26, 27, 28, 30, 31, 32, 33, 34 }; // The many Q2 PROTOCOL_VERSIONs...

		private const int SERVERINFO = 12; // 0x0C
		private const int CONFIGSTRING = 13; // 0x0D

		#endregion

		#region ================= GetDemoInfo

		// https://www.quakewiki.net/archives/demospecs/dm2/dm2.html
		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader)
		{
			// uint blocklength
			// SERVERINFO
			// CONFIGSTRINGS, one of them is .bsp path

			int blocklength = reader.ReadInt32();
			if(reader.BaseStream.Position + blocklength >= reader.BaseStream.Length) return null;
			long blockend = reader.ReadUInt32() + reader.BaseStream.Position;

			int messagetype = reader.ReadByte();
			if(messagetype != SERVERINFO) return null;

			// Read ServerInfo
			int serverversion = reader.ReadInt32();
			if(serverversion != PROTOCOL_KMQ && serverversion != PROTOCOL_R1Q2 && !ProtocolsQ2.Contains(serverversion)) return null;
			int key = reader.ReadInt32();
			if(reader.ReadByte() != 1) return null; // Not a RECORD_CLIENT demo...
			string gamedir = reader.ReadString('\0'); // Game directory (may be empty, which means "baseq2").
			int playernum = reader.ReadInt16();
			string maptitle = reader.ReadMapTitle(blocklength, Quake2Font.CharMap);

			// Read configstrings
			string mapfilepath = string.Empty;
			while(reader.BaseStream.Position < blockend)
			{
				messagetype = reader.ReadByte();
				if(messagetype != CONFIGSTRING) return null;

				int configstringtype = reader.ReadInt16();
				string data = reader.ReadString('\0');

				if(data.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
				{
					mapfilepath = data;
					break;
				}

				// Block end reached?..
				if(reader.BaseStream.Position == blockend)
				{
					blockend = reader.ReadUInt32() + reader.BaseStream.Position;
					if(blockend >= reader.BaseStream.Length) return null;
				}
			}

			if(!string.IsNullOrEmpty(maptitle) && !string.IsNullOrEmpty(mapfilepath))
				return new DemoItem(demoname, mapfilepath, maptitle);

			// No dice...
			return null;
		}

		#endregion
	}
}

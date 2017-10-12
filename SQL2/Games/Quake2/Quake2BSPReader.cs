#region ================= Namespaces

using System;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Quake2
{
	public static class Quake2BSPReader
	{
		#region ================= Constants

		private const string BSP_MAGIC = "IBSP";
		private const int BSP_VERSION = 38;

		#endregion

		#region ================= GetMapInfo

		public static MapItem GetMapInfo(string name, BinaryReader reader, ResourceType restype)
		{
			long offset = reader.BaseStream.Position;

			// Check header
			string magic = reader.ReadStringExactLength(4);
			int version = reader.ReadInt32();

			if(magic != BSP_MAGIC || version != BSP_VERSION)
				return new MapItem(name, restype);

			// Next is lump directory. We are interested in the first one
			long entdatastart = reader.ReadUInt32() + offset;
			long entdataend = entdatastart + reader.ReadUInt32();

			if(entdatastart >= reader.BaseStream.Length || entdataend >= reader.BaseStream.Length)
				return new MapItem(name, restype);

			// Get entities data. Worldspawn should be the first entry
			reader.BaseStream.Position = entdatastart + 1; // Skip the first "{"
			string data = reader.ReadString(' ');

			while(!data.EndsWith("\"message\"", StringComparison.OrdinalIgnoreCase) && !data.Contains("}") && reader.BaseStream.Position < entdataend)
			{
				data = reader.ReadString(' ');
			}

			// Next quoted string is map name
			string title = string.Empty;
			if(data.EndsWith("\"message\"", StringComparison.OrdinalIgnoreCase))
			{
				byte b = reader.ReadByte();

				// Skip opening quote...
				while((char)b != '\"') b = reader.ReadByte();

				// Continue till closing quote...
				b = 0;
				byte prevchar = b;
				while(true)
				{
					b = reader.ReadByte();

					// Stop on closing quote, EOF or closing brace...
					if((char)b == '\"' || (char)b == '\0' || (char)b == '}') break;

					// Replace newline with space
					if(b == 'n' && prevchar == '\\')
					{
						prevchar = b;
						title = title.Remove(title.Length - 1, 1) + ' ';
						continue;
					}

					// Trim extra spaces...
					if(!(prevchar == 32 && prevchar == b)) title += Quake2Font.CharMap[b];
					prevchar = b;
				}
			}

			// Return MapItem with title, if we have one
			title = GameHandler.Current.CheckMapTitle(title);
			return (!string.IsNullOrEmpty(title) ? new MapItem(title, name, restype) : new MapItem(name, restype));
		}

		#endregion
	}
}

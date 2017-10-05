#region ================= Namespaces

using System;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	// Quake BSP reader
	public static class QuakeBSPReader
	{
	    #region ================= Constants

        private const int BSPVERSION = 29;
		private const int BSP2VERSION_2PSB = (('B' << 24) | ('S' << 16) | ('P' << 8) | '2');
		private const int BSP2VERSION_BSP2 = (('B' << 0) | ('S' << 8) | ('P' << 16) | ('2' << 24));

        #endregion

        #region ================= GetMapInfo

        public static MapItem GetMapInfo(string name, BinaryReader reader)
		{
			long offset = reader.BaseStream.Position;

			// Get version and offset to entities 
			int version = reader.ReadInt32();
			long entdatastart = reader.ReadInt32() + offset;
			long entdataend = entdatastart + reader.ReadInt32();

            // Time to bail out?
            if((version != BSPVERSION && version != BSP2VERSION_BSP2 && version != BSP2VERSION_2PSB)
				|| entdatastart >= reader.BaseStream.Length || entdataend >= reader.BaseStream.Length)
				return new MapItem(name);

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
					if(!(prevchar == 32 && prevchar == b)) title += QuakeFont.CharMap[b];
					prevchar = b;
				}
			}

            // Return MapItem with title, if we have one
            title = title.Trim();
            return (!string.IsNullOrEmpty(title) ? new MapItem(title, name) : new MapItem(name));
		}

        #endregion
    }
}

#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
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

        public static DemoItem GetDemoInfo(string demoname, BinaryReader reader)
		{
            // uint blocklength
            // SERVERINFO
            // CONFIGSTRINGS, one of them is .bsp path

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
		    string maptitle = ReadMapTitle(reader, blockend);

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
				}
		    }

            if(!string.IsNullOrEmpty(maptitle) && !string.IsNullOrEmpty(mapfilepath))
                return new DemoItem(demoname, mapfilepath, maptitle);

            // No dice...
            return null;
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
	            if(!(prevchar == 32 && prevchar == b)) result += Quake2Font.CharMap[b];
	            prevchar = b;
	        }

	        return result;
	    }

        #endregion
    }
}

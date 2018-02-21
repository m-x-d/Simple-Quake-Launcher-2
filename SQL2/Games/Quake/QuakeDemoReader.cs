#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Quake
{
	public static class QuakeDemoReader
	{
		#region ================= Constants

		// DEM protocols
		private const int PROTOCOL_NETQUAKE = 15;
		private const int PROTOCOL_FITZQUAKE = 666;
		private const int PROTOCOL_RMQ = 999;

		// "Special" protocols...
		private const int PROTOCOL_FTE  = ('F' << 0) + ('T' << 8) + ('E' << 16) + ('X' << 24);
		private const int PROTOCOL_FTE2 = ('F' << 0) + ('T' << 8) + ('E' << 16) + ('2' << 24);

		// QW protocols
		private static readonly HashSet<int> ProtocolsQW = new HashSet<int> { 24, 25, 26, 27, 28 }; // The not so many QW PROTOCOL_VERSIONs...

		private const int GAME_COOP = 0;
		private const int GAME_DEATHMATCH = 1;

		private const int BLOCK_CLIENT = 0;
		private const int BLOCK_SERVER = 1;
		private const int BLOCK_FRAME = 2;

		private const int SVC_PRINT = 8;
		private const int SVC_STUFFTEXT = 9;
		private const int SVC_SERVERINFO = 11;
		private const int SVC_CDTRACK = 32;
		private const int SVC_MODELLIST = 45;
		private const int SVC_SOUNDLIST = 46;

		#endregion

		#region ================= GetDemoInfo

		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader, ResourceType restype)
		{
			string ext = Path.GetExtension(demoname);
			if(string.IsNullOrEmpty(ext)) return null;

			switch(ext.ToUpperInvariant())
			{
				case ".DEM": return GetDEMInfo(demoname, reader, restype);
				case ".MVD": return GetMVDInfo(demoname, reader, restype);
				case ".QWD": return GetQWDInfo(demoname, reader, restype);
				default: throw new NotImplementedException("Unsupported demo type: " + ext);
			}
		}

		// https://www.quakewiki.net/archives/demospecs/dem/dem.html
		private static DemoItem GetDEMInfo(string demoname, BinaryReader reader, ResourceType restype)
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
						// protocol (int32)  -> 666, 999 or 15
						// protocolflags (int32) - only when protocol == 999
						// maxclients (byte) should be in 1 .. 16 range
						// gametype (byte) - 0 -> coop, 1 -> deathmatch
						// map title (null-terminated string)
						// map filename (null-terminated string) "maps/mymap.bsp"

						case SVC_SERVERINFO:
							protocol = reader.ReadInt32();

							// FTE2 shenanigans...
							if(protocol == PROTOCOL_FTE || protocol == PROTOCOL_FTE2)
							{
								reader.BaseStream.Position += 4; // Skip fteprotocolextensions or fteprotocolextensions2 (?)
								protocol = reader.ReadInt32();

								if(protocol == PROTOCOL_FTE2)
								{
									reader.BaseStream.Position += 4; // Skip fteprotocolextensions2 (?)
									protocol = reader.ReadInt32();
								}

								reader.SkipString(1024); // Skip mod folder...
							}

							if(protocol != PROTOCOL_NETQUAKE && protocol != PROTOCOL_FITZQUAKE && protocol != PROTOCOL_RMQ) return null;
							if(protocol == PROTOCOL_RMQ) reader.BaseStream.Position += 4; // Skip RMQ protocolflags (int32)

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

		// https://www.quakewiki.net/archives/demospecs/qwd/qwd.html
		private static DemoItem GetQWDInfo(string demoname, BinaryReader reader, ResourceType restype)
		{
			// Block header:
			// float time;                 
			// char code; // QWDBlockType.SERVER for server block

			// Server block:
			// long blocksize;
			// unsigned long seq_rel_1; // (!= 0xFFFFFFFF) for Game block

			// Game block:
			// unsigned long seq_rel_2;
			// char messages[blocksize - 8];

			string game = string.Empty;
			string maptitle = string.Empty;
			string mapfilepath = string.Empty;
			int protocol = 0;
			bool alldatafound = false;

			// Read blocks...
			while(reader.BaseStream.Position < reader.BaseStream.Length)
			{
				if(alldatafound) break;

				// Read block header...
				reader.BaseStream.Position += 4; // Skip time
				int code = reader.ReadByte();
				if(code != BLOCK_SERVER) return null;

				// Read as Server block...
				int blocklength = reader.ReadInt32();
				long blockend = reader.BaseStream.Position + blocklength;
				if(blockend >= reader.BaseStream.Length) return null;
				uint serverblocktype = reader.ReadUInt32(); // 13 // connectionless block (== 0xFFFFFFFF) or a game block (!= 0xFFFFFFFF).
				if(serverblocktype == uint.MaxValue) return null;

				// Read as Game block...
				reader.BaseStream.Position += 4; // Skip seq_rel_2

				while(reader.BaseStream.Position < blockend)
				{
					if(alldatafound) break;

					// Read messages...
					int message = reader.ReadByte();
					switch(message)
					{
						// SVC_SERVERINFO
						// long serverversion; // the protocol version coming from the server.
						// long age; // the number of levels analysed since the existence of the server process. Starts with 1.
						// char* game; // the QuakeWorld game directory. It has usually the value "qw";
						// byte client; // the client id.
						// char* mapname; // the name of the level.
						// 10 unrelated floats

						case SVC_SERVERINFO:
							protocol = reader.ReadInt32();
							if(!ProtocolsQW.Contains(protocol)) return null;
							reader.BaseStream.Position += 4; // Skip age
							game = reader.ReadString('\0');
							reader.BaseStream.Position += 1; // Skip client
							maptitle = reader.ReadMapTitle(blocklength, QuakeFont.CharMap); // Map title can contain bogus chars...
							if(protocol > 24) reader.BaseStream.Position += 40; // Skip 10 floats...
							break;

						case SVC_CDTRACK:
							reader.BaseStream.Position += 1; // Skip CD track number
							break;

						case SVC_STUFFTEXT:
							reader.SkipString(2048);
							break;

						case SVC_MODELLIST: // First model should be the map name
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip first model index...
							for(int i = 0; i < 256; i++)
							{
								string mdlname = reader.ReadString('\0');
								if(string.IsNullOrEmpty(mdlname)) break;
								if(mdlname.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
								{
									mapfilepath = mdlname;
									alldatafound = true;
									break;
								}
							}
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip next model index...
							break;

						case SVC_SOUNDLIST:
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip first sound index...
							for(int i = 0; i < 256; i++)
							{
								string sndname = reader.ReadString('\0');
								if(string.IsNullOrEmpty(sndname)) break;
							}
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip next sound index...
							break;

						default:
							return null;
					}
				}
			}

			// Done
			return (alldatafound ? new DemoItem(game, demoname, mapfilepath, maptitle, restype) : null);
		}

		//TODO: Hacked in, needs more testing or proper format spec...
		private static DemoItem GetMVDInfo(string demoname, BinaryReader reader, ResourceType restype)
		{
			string game = string.Empty;
			string maptitle = string.Empty;
			string mapfilepath = string.Empty;
			int protocol = 0;
			bool alldatafound = false;

			// Read blocks...
			while(reader.BaseStream.Position < reader.BaseStream.Length)
			{
				if(alldatafound) break;

				// Read block header...
				reader.BaseStream.Position += 2; // Skip ??? (0x00 0x01 or 0x00 0x06)
				int blocklength = reader.ReadInt32();
				long blockend = reader.BaseStream.Position + blocklength;
				if(blockend >= reader.BaseStream.Length) return null;

				while(reader.BaseStream.Position < blockend)
				{
					if(alldatafound) break;

					// Read messages...
					int message = reader.ReadByte();
					switch(message)
					{
						// SVC_SERVERINFO
						// long serverversion; // the protocol version coming from the server.
						// long age; // the number of levels analysed since the existence of the server process. Starts with 1.
						// char* game; // the QuakeWorld game directory. It has usually the value "qw";
						// long client; // the client id.
						// char* mapname; // the name of the level.
						// 10 unrelated floats

						case SVC_SERVERINFO:
							protocol = reader.ReadInt32();
							if(!ProtocolsQW.Contains(protocol)) return null;
							reader.BaseStream.Position += 4; // Skip age
							game = reader.ReadString('\0');
							reader.BaseStream.Position += 4; // Skip ???
							maptitle = reader.ReadMapTitle(blocklength, QuakeFont.CharMap); // Map title can contain bogus chars...
							if(protocol > 24) reader.BaseStream.Position += 40; // Skip 10 floats...
							break;

						case SVC_CDTRACK:
							reader.BaseStream.Position += 1; // Skip CD track number
							break;

						case SVC_STUFFTEXT:
							reader.SkipString(2048);
							break;

						case SVC_MODELLIST: // First model should be the map name
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip first model index...
							for(int i = 0; i < 256; i++)
							{
								string mdlname = reader.ReadString('\0');
								if(string.IsNullOrEmpty(mdlname)) break;
								if(mdlname.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
								{
									mapfilepath = mdlname;
									alldatafound = true;
									break;
								}
							}
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip next model index...
							break;

						case SVC_SOUNDLIST:
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip first sound index...
							for(int i = 0; i < 256; i++)
							{
								string sndname = reader.ReadString('\0');
								if(string.IsNullOrEmpty(sndname)) break;
							}
							if(protocol > 25) reader.BaseStream.Position += 1; // Skip next sound index...
							break;

						default:
							return null;
					}
				}
			}

			// Done
			return (alldatafound ? new DemoItem(game, demoname, mapfilepath, maptitle, restype) : null);
		}

		#endregion
	}
}

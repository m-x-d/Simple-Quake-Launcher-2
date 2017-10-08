#region ================= Namespaces

using System.IO;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.HalfLife
{
	public static class HalfLifeDemoReader
	{
		#region ================= Constants

		private const string MAGIC = "HLDEMO";

		#endregion

		#region ================= GetDemoInfo

		// https://sourceforge.net/p/lmpc/git/ci/master/tree/spec/dem-hl.spec
		public static DemoItem GetDemoInfo(string demoname, BinaryReader reader)
		{
			/*  0 char[8] magic;			/* == "HLDEMO\0\0" */
			/*  8 uint32 demo_version;      /* == 5 (HL 1.1.0.1) */
			/*  c uint32 network_version;	/* == 42 (HL 1.1.0.1) */
			/* 10 char[0x104] map_name;     /* eg "c0a0e" */
			/*114 char[0x108] game_dll;     /* eg "valve" */

			// Header is 544 bytes
			if(reader.BaseStream.Length - reader.BaseStream.Position < 544) return null;

			// Read header
			if(reader.ReadStringExactLength(8) != MAGIC) return null;
			reader.BaseStream.Position += 8; // Skip demo_version and network_version
			string mapname = reader.ReadStringExactLength(260);
			string modname = reader.ReadString('\0');

			// Done. Easiest of them all :)
			return new DemoItem(modname, demoname, mapname, mapname);
		}

		#endregion
	}
}

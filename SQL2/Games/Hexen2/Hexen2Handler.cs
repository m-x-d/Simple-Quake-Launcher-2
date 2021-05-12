#region ================= Namespaces

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Games.Quake;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Games.Hexen2
{
	public class Hexen2Handler : GameHandler
	{
		#region ================= Variables

		private List<string> strings; // Contents of strings.txt in the current moddir

		#endregion

		#region ================= Properties

		public override string GameTitle => "Hexen II";

		#endregion

		#region ================= Setup

		// Valid Hexen 2 path if "data1\pak0.pak" and "data1\pak1.pak" exist, I guess...
		protected override bool CanHandle(string gamepath)
		{
			foreach(var p in new[] { "data1\\pak0.pak", "data1\\pak1.pak" })
				if(!File.Exists(Path.Combine(gamepath, p))) return false;

			return true;
		}

		// Data initialization order matters (horrible, I know...)!
		protected override void Setup(string gamepath)
		{
			// Default mod path
			defaultmodpath = Path.Combine(gamepath, "DATA1").ToLowerInvariant();

			// Nothing to ignore
			ignoredmapprefix = string.Empty;

			// Demo extensions
			supporteddemoextensions.Add(".dem");

			// Setup map delegates
			getfoldermaps = DirectoryReader.GetMaps;
			getpakmaps = PAKReader.GetMaps;
			getpk3maps = PK3Reader.GetMaps;

			foldercontainsmaps = DirectoryReader.ContainsMaps;
			pakscontainmaps = PAKReader.ContainsMaps;
			pk3scontainmaps = PK3Reader.ContainsMaps;

			getmapinfo = QuakeBSPReader.GetMapInfo;

			// Setup demo delegates
			getfolderdemos = DirectoryReader.GetDemos;
			getpakdemos = PAKReader.GetDemos;
			getpk3demos = PK3Reader.GetDemos;

			getdemoinfo = Hexen2DemoReader.GetDemoInfo;

			// Setup file checking delegates
			pakscontainfile = PAKReader.ContainsFile;
			pk3scontainfile = PK3Reader.ContainsFile;

			// Setup fullscreen args...
			fullscreenarg[true] = string.Empty;
			fullscreenarg[false] = "-window ";

			// Setup launch params
			launchparams[ItemType.ENGINE] = string.Empty;
			launchparams[ItemType.RESOLUTION] = "{2}-width {0} -height {1}";
			launchparams[ItemType.GAME] = string.Empty;
			launchparams[ItemType.MOD] = "-game {0}";
			launchparams[ItemType.MAP] = "+map {0}";
			launchparams[ItemType.SKILL] = "+skill {0}";
			launchparams[ItemType.CLASS] = "+playerclass {0}";
			launchparams[ItemType.DEMO] = "+playdemo {0}";

			// Setup skills (requires launchparams)
			skills.AddRange(new[]
			{
				new SkillItem("Easy", "0"),
				new SkillItem("Medium", "1", true),
				new SkillItem("Hard", "2"),
				new SkillItem("Very Hard", "3", false, true)
			});

			// Setup classes (requires launchparams)
			classes.AddRange(new[]
			{
				// Hexen 2 stores last used playerclass, so no defaults here...
				new ClassItem("Paladin", "1"),
				new ClassItem("Crusader", "2"),
				new ClassItem("Necromancer", "3"),
				new ClassItem("Assassin", "4"),
				new ClassItem("Demoness", "5")
			});

			// Setup basegames (requires defaultmodpath)
			basegames["DATA1"] = new GameItem("Hexen II", "data1", "");
			basegames["PORTALS"] = new GameItem("H2MP: Portal of Praevus", "portals", "-portals");

			// Initialize collections
			strings = new List<string>();

			// Pass on to base...
			base.Setup(gamepath);
		}

		#endregion

		#region ================= Methods

		public override List<ModItem> GetMods()
		{
			var result = new List<ModItem>();

			foreach(string folder in Directory.GetDirectories(gamepath))
			{
				if(!Directory.Exists(folder)) continue;

				string name = folder.Substring(gamepath.Length + 1);
				if(basegames.ContainsKey(name))
				{
					result.Add(new ModItem(name, folder, true));
					continue;
				}

				// Count folder as a mod when it contains "progs.dat"...
				if(File.Exists(Path.Combine(folder, "progs.dat")) || pakscontainfile(folder, "progs.dat") || pk3scontainfile(folder, "progs.dat"))
				{
					result.Add(new ModItem(name, folder));
					continue;
				}

				// Skip folder if it has no maps
				if(!foldercontainsmaps(folder) && !pakscontainmaps(folder) && !pk3scontainmaps(folder))
					continue;

				result.Add(new ModItem(name, folder));
			}

			return result;
		}

		public override List<MapItem> GetMaps(string modpath)
		{
			// Safety foist...
			if(!Directory.Exists(modpath)) return new List<MapItem>();

			// Get contents of strings.txt...
			strings = new List<string>(); // Actually faster than Clear()
			var stringspath = Path.Combine(modpath, "strings.txt");
			if(File.Exists(stringspath)) strings.AddRange(File.ReadAllLines(stringspath));

			// Pass on to base...
			return base.GetMaps(modpath);
		}

		public override string CheckMapTitle(string title)
		{
			// Title is a number?
			int index;
			if(int.TryParse(title, NumberStyles.Integer, CultureInfo.InvariantCulture, out index))
			{
				index--; // Make it 0-based...
				if(index >= strings.Count || index < 0)
					return "[Invalid string index: " + index + "]";

				if(string.IsNullOrEmpty(strings[index]))
					return "[Empty string index: " + index + "]";

				title = strings[index];
				if(title.Length > 64) title = title.Substring(0, 64); // What's the actual limit, BTW?..
			}

			return base.CheckMapTitle(title);
		}

		#endregion
	}
}

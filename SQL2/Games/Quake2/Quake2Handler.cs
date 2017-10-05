#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Quake2
{
	public class Quake2Handler : GameHandler
	{
		#region ================= Properties

		private Dictionary<string, string> knowngamefolders; // Folder names and titles for official expansions, <rogue, MP2: Ground Zero>

		#endregion

		#region ================= Properties

		public override string GameTitle => "Quake II";

		#endregion

		#region ================= Setup

		// Valid Quake 2 path if "baseq2\pak0.pak" and "baseq2\pak1.pak" exist, I guess...
		protected override bool CanHandle(string gamepath)
		{
			foreach(var p in new[] { "baseq2\\pak0.pak", "baseq2\\pak1.pak" })
				if(!File.Exists(Path.Combine(gamepath, p))) return false;

			return true;
		}

		// Data initialization order matters (horrible, I know...)!
		protected override void Setup(string gamepath)
		{
			// Default mod path
			defaultmodpath = Path.Combine(gamepath, "baseq2").ToLowerInvariant();

			// Nothing to ignore
			ignoredmapprefix = string.Empty;

			// Demo extensions
			supporteddemoextensions.Add(".dm2");

			// Setup map delegates
			getfoldermaps = DirectoryReader.GetMaps;
			getpakmaps = PAKReader.GetMaps;
			getpk3maps = PK3Reader.GetMaps;

			foldercontainsmaps = DirectoryReader.ContainsMaps;
			pakscontainmaps = PAKReader.ContainsMaps;
			pk3scontainmaps = PK3Reader.ContainsMaps;

			getmapinfo = Quake2BSPReader.GetMapInfo;

			// Setup demo delegates
			getfolderdemos = DirectoryReader.GetDemos;
			getpakdemos = PAKReader.GetDemos;
			getpk3demos = PK3Reader.GetDemos;

			getdemoinfo = Quake2DemoReader.GetDemoInfo;

			// Setup launch params
			launchparams[ItemType.ENGINE] = string.Empty;
			launchparams[ItemType.RESOLUTION] = "+vid_fullscreen 0 +set r_mode -1 +r_customwidth {0} +r_customheight {1}";
			launchparams[ItemType.GAME] = string.Empty;
			launchparams[ItemType.MOD] = "+set game {0}";
			launchparams[ItemType.MAP] = "+map {0}";
			launchparams[ItemType.SKILL] = "+set skill {0}";
			launchparams[ItemType.CLASS] = string.Empty;
			launchparams[ItemType.DEMO] = "+map {0}";

			// Setup skills (requires launchparams)
			skills.AddRange(new[]
			{
				new SkillItem("Easy", "0"),
				new SkillItem("Medium", "1", true),
				new SkillItem("Hard", "2"),
				new SkillItem("Nightmare", "3")
			});

			// Setup known folders
			knowngamefolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "ROGUE", "MP1: The Reckoning" },
				{ "XATRIX", "MP2: Ground Zero" },
			};

			// Pass on to base...
			base.Setup(gamepath);
		}

		#endregion

		#region ================= Methods

		public override List<DemoItem> GetDemos(string modpath)
		{
			return GetDemos(modpath, "DEMOS");
		}

		public override List<ModItem> GetMods()
		{
			var result = new List<ModItem>();

			foreach(string folder in Directory.GetDirectories(gamepath))
			{
				// Skip folder if it has no maps
				if(!foldercontainsmaps(folder) && !pakscontainmaps(folder) && !pk3scontainmaps(folder))
					continue;

				string name = folder.Substring(gamepath.Length + 1);
				bool isbuiltin = (string.Compare(folder, defaultmodpath, StringComparison.OrdinalIgnoreCase) == 0);
				string title = (knowngamefolders.ContainsKey(name) ? knowngamefolders[name] : name);

				result.Add(new ModItem(title, name, folder, isbuiltin));
			}

			// Push known mods above regular ones
			result.Sort((i1, i2) =>
			{
				bool firstknown = (i1.Title != i1.Value);
				bool secondknown = (i2.Title != i2.Value);

				if(firstknown == secondknown) return string.Compare(i1.Title, i2.Title, StringComparison.Ordinal);
				return (firstknown ? -1 : 1);
			});

			return result;
		}

		#endregion
	}
}

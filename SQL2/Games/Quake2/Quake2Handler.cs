#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using mxd.SQL2.Data;
using mxd.SQL2.DataReaders;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;

#endregion

namespace mxd.SQL2.Games.Quake2
{
	public class Quake2Handler : GameHandler
	{
		#region ================= Variables

		private Dictionary<string, string> knowngamefolders; // Folder names and titles for official expansions, <rogue, MP2: Ground Zero>
		private List<VideoModeInfo> rmodes;

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

			// Setup fullscreen args...
			fullscreenarg[true]  = "1";
			fullscreenarg[false] = "0";

			// Setup launch params
			launchparams[ItemType.ENGINE] = string.Empty;
			launchparams[ItemType.RESOLUTION] = "+vid_fullscreen {1} +set r_mode {0}"; // "+vid_fullscreen 0 +r_customwidth {0} +r_customheight {1}" -> works unreliably in KMQuake2, doesn't work in Q2 v3.24
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
				new SkillItem("Nightmare", "3", false, true)
			});

			// Setup known folders
			knowngamefolders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "XATRIX", "MP1: The Reckoning" },
				{ "ROGUE", "MP2: Ground Zero" },
			};

			// Setup fixed r_modes... Taken from qcommon\vid_modes.h (KMQ2)
			int c = 3; // The first two r_modes are ignored by KMQ2
			rmodes = new List<VideoModeInfo>
			{
				new VideoModeInfo(640, 480, c++), 
				new VideoModeInfo(800, 600, c++),
				new VideoModeInfo(960, 720, c++),
				new VideoModeInfo(1024, 768, c++),
				new VideoModeInfo(1152, 864, c++),
				new VideoModeInfo(1280, 960, c++),
				new VideoModeInfo(1280, 1024, c++),
				new VideoModeInfo(1400, 1050, c++),
				new VideoModeInfo(1600, 1200, c++),
				new VideoModeInfo(1920, 1440, c++),
				new VideoModeInfo(2048, 1536, c++),

				new VideoModeInfo(800, 480, c++),
				new VideoModeInfo(856, 480, c++),
				new VideoModeInfo(1024, 600, c++),
				new VideoModeInfo(1280, 720, c++),
				new VideoModeInfo(1280, 768, c++),
				new VideoModeInfo(1280, 800, c++),
				new VideoModeInfo(1360, 768, c++),
				new VideoModeInfo(1366, 768, c++),
				new VideoModeInfo(1440, 900, c++),
				new VideoModeInfo(1600, 900, c++),
				new VideoModeInfo(1600, 1024, c++),
				new VideoModeInfo(1680, 1050, c++),
				new VideoModeInfo(1920, 1080, c++),
				new VideoModeInfo(1920, 1200, c++),
				new VideoModeInfo(2560, 1080, c++),
				new VideoModeInfo(2560, 1440, c++),
				new VideoModeInfo(2560, 1600, c++),
				new VideoModeInfo(3200, 1800, c++),
				new VideoModeInfo(3440, 1440, c++),
				new VideoModeInfo(3840, 2160, c++),
				new VideoModeInfo(3840, 2400, c++),
				new VideoModeInfo(5120, 2880, c++),
			};

			// Pass on to base...
			base.Setup(gamepath);
		}

		#endregion

		#region ================= Methods

		public override List<ResolutionItem> GetVideoModes()
		{
			return DisplayTools.GetFixedVideoModes(rmodes);
		}

		public override List<DemoItem> GetDemos(string modpath)
		{
			return GetDemos(modpath, "DEMOS");
		}

		public override List<ModItem> GetMods()
		{
			var result = new List<ModItem>();

			foreach(string folder in Directory.GetDirectories(gamepath))
			{
				// Skip folder if it has no maps or a variant of "gamex86.dll"
				if(!foldercontainsmaps(folder) && !pakscontainmaps(folder) && !pk3scontainmaps(folder) && !ContainsGameDll(folder))
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

		// Check for a variant of "gamex86.dll". Can be named differently depending on source port...
		// Vanilla, UQE Quake2: gamex86.dll
		// KMQuake2: kmq2gamex86.dll
		// Yamagi Quake2: game.dll
		// Quake 2 Evolved: q2e_gamex86.dll
		// Quake 2 XP: gamex86xp.dll
		private bool ContainsGameDll(string folder)
		{
			//TODO? Ideally, we should check for game.dll specific to selected game engine, but that would require too much work, including updating mod list when game engine selection changes. 
			//TODO? So, for now just look for anything resembling game.dll...
			var dlls = Directory.GetFiles(folder, "*.dll");
			foreach (var dll in dlls)
				if (Path.GetFileName(dll).Contains("game"))
					return true;

			return false;
		}

		#endregion
	}
}

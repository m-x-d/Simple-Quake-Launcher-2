#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Games
{
	public abstract class GameHandler
	{
		#region ================= Variables

		protected string defaultmodpath; // id1 / baseq2 / data1 etc.
		protected string gamepath; // c:\games\Quake, c:\games\Quake2 etc.
		protected string ignoredmapprefix; // map filenames starting with this will be ignored
		protected HashSet<string> supporteddemoextensions; // .dem, .mvd, .qvd etc.
		protected Dictionary<string, GameItem> basegames; // Quake-specific game flags; <folder name, BaseGameItem>
		protected List<SkillItem> skills; // Easy, Medium, Hard, Nightmare!
		protected List<ClassItem> classes; // Cleric, Paladin, Necromancer [default], Assassin, Demoness [Hexen 2 only]

		private HashSet<string> mapnames;
		private List<string> skillnames;
		private List<string> classnames; // [Hexen 2 only]

		private static string supportedgames; // "Quake / Quake II / Hexen 2 / Half-Life"
		private static GameHandler current;

		// Command line
		protected Dictionary<ItemType, string> launchparams; 

		#endregion

		#region ================= Properties

		public abstract string GameTitle { get; } // Quake / Quake II / Hexen 2 / Half-Life etc.
		public Dictionary<ItemType, string> LaunchParameters => launchparams;
		public string DefaultModPath => defaultmodpath; // c:\games\Quake\ID1, c:\games\Quake2\baseq2 etc.
		public string GamePath => gamepath;
		public string IgnoredMapPrefix => ignoredmapprefix;
		public HashSet<string> SupportedDemoExtensions => supporteddemoextensions;
		public ICollection<GameItem> BaseGames => basegames.Values;
		public List<SkillItem> Skills => skills;
		public List<ClassItem> Classes => classes;

		#endregion

		#region ================= Static properties

		public static string SupportedGames => supportedgames;
		public static GameHandler Current => current;

		#endregion

		#region ================= Delegates

		// Map title retrieval
		public delegate MapItem GetMapInfoDelegate(string mapname, BinaryReader reader);

		// Maps gathering
		protected delegate void GetFolderMapsDelegate(string modpath, Dictionary<string, MapItem> mapslist, GetMapInfoDelegate getmapinfo);
		protected delegate void    GetPakMapsDelegate(string modpath, Dictionary<string, MapItem> mapslist, GetMapInfoDelegate getmapinfo);
		protected delegate void    GetPK3MapsDelegate(string modpath, Dictionary<string, MapItem> mapslist, GetMapInfoDelegate getmapinfo);

		// Maps checking
		protected delegate bool FolderContainsMapsDelegate(string modpath);
		protected delegate bool PakContainsMapsDelegate(string modpath);
		protected delegate bool PK3ContainsMapsDelegate(string modpath);

		// Demo info retrieval
		public delegate DemoItem GetDemoInfoDelegate(string demoname, BinaryReader reader);

		// Demos gathering
		protected delegate List<DemoItem> GetFolderDemosDelegate(string modpath, string demosfolder);
		protected delegate List<DemoItem>    GetPakDemosDelegate(string modpath, string demosfolder);
		protected delegate List<DemoItem>    GetPK3DemosDelegate(string modpath, string demosfolder);

		// Map title retrieval instance
		protected GetMapInfoDelegate getmapinfo;

		// Maps gathering instances
		protected GetFolderMapsDelegate getfoldermaps;
		protected GetPakMapsDelegate getpakmaps;
		protected GetPK3MapsDelegate getpk3maps;

		// Maps checking instances
		protected FolderContainsMapsDelegate foldercontainsmaps;
		protected PakContainsMapsDelegate pakscontainmaps;
		protected PK3ContainsMapsDelegate pk3scontainmaps;

		// Demo info retrieval instance
		protected GetDemoInfoDelegate getdemoinfo;

		// Demo gathering instances
		protected GetFolderDemosDelegate getfolderdemos;
		protected GetPakDemosDelegate getpakdemos;
		protected GetPK3DemosDelegate getpk3demos;

		#endregion

		#region ================= Constructor / Setup

		protected GameHandler()
		{
			launchparams = new Dictionary<ItemType, string>();
			basegames = new Dictionary<string, GameItem>(StringComparer.OrdinalIgnoreCase);
			mapnames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			skills = new List<SkillItem>();
			classes = new List<ClassItem>();
			skillnames = new List<string>();
			classnames = new List<string>();
			supporteddemoextensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		}

		protected abstract bool CanHandle(string gamepath);

		protected virtual void Setup(string gamepath) // c:\games\Quake
		{
#if DEBUG
			CheckItems(basegames.Values.ToArray());
			CheckItems(skills.ToArray());
			CheckItems(classes.ToArray());

			SkillItem defaultskill = null;
			foreach(var skill in skills)
			{
				skillnames.Add(skill.Value);
				if(skill.IsDefault)
				{
					defaultskill = skill;
					break;
				}
			}

			ClassItem defaultclass = null;
			foreach(var pclass in classes)
			{
				classnames.Add(pclass.Value);
				if(pclass.IsDefault)
				{
					defaultclass = pclass;
					break;
				}
			}

			if(skills.Count > 0 && defaultskill == null) throw new InvalidDataException("No default skill specified!");
			if(classes.Count > 0 && defaultclass == null) throw new InvalidDataException("No default class specified!");
#endif

			// Add random skill and class
			if(skills.Count > 1) skills.Insert(0, SkillItem.Random);
			if(skills.Count > 0) skills.Insert(0, SkillItem.Default);

			if(classes.Count > 1) classes.Insert(0, ClassItem.Random);
			if(classes.Count > 0) classes.Insert(0, ClassItem.Default);

			this.gamepath = gamepath;
		}

		#endregion

		#region ================= Data gathering

		public abstract List<ModItem> GetMods();

		public virtual List<DemoItem> GetDemos(string modpath) // c:\Quake\MyMod
		{
			return GetDemos(modpath, string.Empty);
		}

		protected virtual List<DemoItem> GetDemos(string modpath, string modfolder)
		{
			var demos = new List<DemoItem>();
			if(!Directory.Exists(modpath)) return demos;

			// Get demos from all supported sources
			var nameshash = new HashSet<string>();
			if(getfolderdemos != null) AddDemos(demos, getfolderdemos(modpath, modfolder), nameshash);
			if(getpakdemos != null) AddDemos(demos, getpakdemos(modpath, modfolder), nameshash);
			if(getpk3demos != null) AddDemos(demos, getpk3demos(modpath, modfolder), nameshash);

			// Sort and return the List
			demos.Sort((s1, s2) => string.Compare(s1.Value, s2.Value, StringComparison.OrdinalIgnoreCase));
			return demos;
		}

		protected virtual void AddDemos(List<DemoItem> demos, List<DemoItem> newdemos, HashSet<string> nameshash)
		{
			foreach(DemoItem di in newdemos)
			{
				string hash = Path.GetFileName(di.MapFilePath) + di.Title;
				if(!nameshash.Contains(hash))
				{
					nameshash.Add(hash);
					demos.Add(di);
				}
			}
		}

		public virtual List<MapItem> GetMaps(string modpath) // c:\Quake\MyMod
		{
			if(!Directory.Exists(modpath)) return new List<MapItem>();
			Dictionary<string, MapItem> maplist = new Dictionary<string, MapItem>(StringComparer.OrdinalIgnoreCase);

			// Get maps from all supported sources
			if(getfoldermaps != null) getfoldermaps(modpath, maplist, getmapinfo);
			if(getpakmaps != null) getpakmaps(modpath, maplist, getmapinfo);
			if(getpk3maps != null) getpk3maps(modpath, maplist, getmapinfo);

			// Store map names...
			mapnames = new HashSet<string>(maplist.Keys, StringComparer.OrdinalIgnoreCase);

			// Sort and return the List
			List<MapItem> mapitems = new List<MapItem>(maplist.Values.Count);
			foreach(MapItem mi in maplist.Values) mapitems.Add(mi);
			mapitems.Sort((s1, s2) => string.Compare(s1.Value, s2.Value, StringComparison.OrdinalIgnoreCase));
			return mapitems;
		} 

		public virtual List<EngineItem> GetEngines()
		{
			string[] enginenames = Directory.GetFiles(gamepath, "*.exe");
			var result = new List<EngineItem>();
			foreach(string engine in enginenames)
			{
				if(Path.GetFileNameWithoutExtension(engine) == App.AppName) continue; // We is not engine. We is cat!

				ImageSource img = null;
				using(var i = Icon.ExtractAssociatedIcon(engine))
				{
					if(i != null)
						img = Imaging.CreateBitmapSourceFromHIcon(i.Handle, new Int32Rect(0, 0, i.Width, i.Height), BitmapSizeOptions.FromEmptyOptions());
				}

				result.Add(new EngineItem(img, engine));
			}

			return result;
		}

		public virtual string GetRandomItem(ItemType type)
		{
			switch(type)
			{
				case ItemType.CLASS: return (classnames.Count > 0 ? classnames[App.Random.Next(0, classnames.Count)] : "0");
				case ItemType.SKILL: return (skillnames.Count > 0 ? skillnames[App.Random.Next(0, skillnames.Count)] : "0");
				case ItemType.MAP: return mapnames.ElementAt(App.Random.Next(0, mapnames.Count));
				default: throw new Exception("GetRandomItem: unsupported ItemType!");
			}
		}

		#endregion

		#region ================= Utility methods

		// "maps/mymap4.bsp", <mymap1, mymap2, mymap3...>
		public virtual bool EntryIsMap(string path, Dictionary<string, MapItem> mapslist)
		{
			path = path.ToLowerInvariant();
			if(Path.GetDirectoryName(path).EndsWith("maps") && Path.GetExtension(path) == ".bsp")
			{
				string mapname = Path.GetFileNameWithoutExtension(path);
				if((string.IsNullOrEmpty(ignoredmapprefix) || !mapname.StartsWith(ignoredmapprefix)) && !mapslist.ContainsKey(mapname))
					return true;
			}

			return false;
		}

		public virtual void AddDemoItem(string relativedemopath, List<DemoItem> demos, BinaryReader reader)
		{
			relativedemopath = relativedemopath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			DemoItem di = getdemoinfo(relativedemopath, reader);
			if(di != null)
			{
				// Check if we have a matching map...
				if(!mapnames.Contains(Path.GetFileNameWithoutExtension(di.MapFilePath)))
					demos.Add(new DemoItem(relativedemopath, "Missing map file: '" + di.MapFilePath + "'")); // Add anyway, but with a warning...
				else
					demos.Add(di);
			}
			else
			{
				// Add anyway, I guess...
				demos.Add(new DemoItem(relativedemopath, "Unknown demo format"));
			}
		}

		// Debug checks...
#if DEBUG
		private static void CheckItems(ICollection<AbstractItem> items)
		{
			if(items.Count == 0) return;

			bool founddefault = false;
			foreach(var item in items)
			{
				if(item.IsDefault)
				{
					if(founddefault) throw new InvalidDataException("Multiple default items!");
					founddefault = true;
				}
			}

			if(!founddefault) throw new InvalidDataException("No default items!");
		}
#endif

#endregion

		#region ================= Instancing

		public static bool Create(string gamepath) // c:\games\Quake
		{
			// Try to get appropriate game handler
			List<string> gametitles = new List<string>();
			foreach(Type type in Assembly.GetAssembly(typeof(GameHandler)).GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(GameHandler))))
			{
				var gh = (GameHandler)Activator.CreateInstance(type);
				gametitles.Add(gh.GameTitle);
				if(current == null && gh.CanHandle(gamepath))
				{
					current = gh;
					gh.Setup(gamepath); // GameItems created in Setup() reference GameHandler.Current...
				}
			}

			// Store all titles
			supportedgames = string.Join(" / ", gametitles.ToArray());

			return current != null;
		}

		#endregion
	}
}

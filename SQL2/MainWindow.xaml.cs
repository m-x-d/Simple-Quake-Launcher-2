#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using IWshRuntimeLibrary;
using mxd.SQL2.Data;
using mxd.SQL2.Games;
using mxd.SQL2.Items;
using mxd.SQL2.Tools;
using Brushes = System.Windows.Media.Brushes;
using File = System.IO.File;

#endregion

namespace mxd.SQL2
{

	public partial class MainWindow : Window
	{
		#region ================= Enums

		private enum FocusState
		{
			DEFAULT,
			FOCUSED,
			UNFOCUSED
		}

		#endregion

		#region ================= Variables

		private Point storedlocation;
		private bool enginelaunched;
		private bool blockupdate;
		private List<ModItem> allmods;
		private FocusState focusstate;


		#endregion

		#region ================= Constructor / Setup

		public MainWindow()
		{
			InitializeComponent();
			allmods = new List<ModItem>();
			createshortcut.ContextMenu.PlacementTarget = createshortcut;
		}

		private void Setup()
		{
			blockupdate = true;

			// Set title and version
			this.Title = "Simple " + GameHandler.Current.GameTitle + " Launcher";
			labelversion.Content = "v" + App.Version;

			// Load configuration
			Configuration.Load(App.IniPath);

			// Setup Engines
			UpdateEngines();

			// Fill video modes list
			var videomodes = DisplayTools.GetVideoModes();
			resolutions.Items.Add(ResolutionItem.Default);
			foreach(ResolutionItem mode in videomodes)
			{
				resolutions.Items.Add(mode);
				if(mode.ToString() == Configuration.WindowSize.ToString())
					resolutions.SelectedIndex = resolutions.Items.Count - 1;
			}

			if(resolutions.SelectedIndex == -1) resolutions.SelectedIndex = 0;

			// Setup base game
			if(GameHandler.Current.BaseGames.Count > 0)
			{
				foreach(var bgi in GameHandler.Current.BaseGames)
					games.Items.Add(bgi);

				if(Configuration.Game > -1 && Configuration.Game < GameHandler.Current.BaseGames.Count)
					games.SelectedIndex = Configuration.Game;
				else
					games.SelectedIndex = 0;
			}
			else
			{
				// No base game support
				rowgame.Height = new GridLength(0);
			}

			// Setup mod folders
			UpdateModsList();

			// Setup maps
			UpdateMapsList();

			// Setup demos list
			UpdateDemosList();

			// Setup skills
			foreach(var skill in GameHandler.Current.Skills)
			{
				skills.Items.Add(skill);
				if(skill.Value == Configuration.Skill)
					skills.SelectedItem = skill;
			}

			// Safety measures...
			if(skills.SelectedIndex == -1)
			{
				if(skills.Items.Count > 0) skills.SelectedIndex = 0;
				else rowskill.Height = new GridLength(0);
			}

			// Setup classes
			foreach(var pclass in GameHandler.Current.Classes)
			{
				classes.Items.Add(pclass);
				if(pclass.Value == Configuration.Class)
					classes.SelectedItem = pclass;
			}

			// Safety measures...
			if(classes.SelectedIndex == -1)
			{
				if(classes.Items.Count > 0) classes.SelectedIndex = 0;
				else rowclass.Height = new GridLength(0);
			}

			// Update UI and preview
			UpdateInterface();
			UpdateCommandLinePreview();

			blockupdate = false;
		}

		#endregion

		#region ================= Utility

		private void ReloadMods()
		{
			blockupdate = true;

			UpdateModsList();
			UpdateMapsList();
			UpdateDemosList();
			UpdateInterface();
			UpdateCommandLinePreview();
 
			blockupdate = false;
		}

		private void UpdateModsList()
		{
			mods.Items.Clear();
			mods.Items.Add(ModItem.Default);

			// Select stored item?
			allmods = GameHandler.Current.GetMods();
			foreach(ModItem mi in allmods)
			{
				if(mi.IsBuiltIn) continue; // Skip mods enabled by cmdline param
				mods.Items.Add(mi);
				if(mi.Value == Configuration.Mod)
					mods.SelectedIndex = mods.Items.Count - 1;
			}

			// Select the Default item...
			if(mods.SelectedIndex == -1) mods.SelectedIndex = 0;
		}

		private void UpdateMapsList()
		{
			maps.Items.Clear();
			maps.Items.Add(MapItem.Default);

			ModItem curmod = GetCurrentMod((ModItem)mods.SelectedItem);
			List<MapItem> mapslist = GameHandler.Current.GetMaps(curmod.ModPath);
			foreach(MapItem mi in mapslist) maps.Items.Add(mi);

			//Add "[Random]" item
			if(maps.Items.Count > 2) maps.Items.Insert(1, MapItem.Random);

			// Select map when both mod name and map name match
			if(!string.IsNullOrEmpty(Configuration.Map))
			{
				foreach(MapItem mi in maps.Items)
				{
					// Select stored map?
					if(mi.Value == Configuration.Map)
					{
						maps.SelectedItem = mi;
						break;
					}
				}
			}

			//Select start map?
			if(maps.SelectedIndex == -1)
			{
				foreach(MapItem mi in mapslist)
				{
					if(mi.Value.Contains("start"))
					{
						maps.SelectedItem = mi;
						break;
					}
				}
			}

			//Select the first map if no "start"/stored map was found
			if(maps.SelectedIndex == -1)
			{
				foreach(MapItem mi in mapslist)
				{
					if(!mi.IsDefault && !mi.IsRandom)
					{
						maps.SelectedItem = mi;
						break;
					}
				}
			}

			// No maps. Select the Default item...
			if(maps.SelectedIndex == -1) maps.SelectedIndex = 0;
		}

		// c:\quake\mymod
		private void UpdateDemosList()
		{
			demos.Items.Clear();
			ModItem curmod = GetCurrentMod((ModItem)mods.SelectedItem);

#if DEBUG
			if(!Directory.Exists(curmod.ModPath))
				throw new InvalidOperationException("Expected existing absolute path!");
#endif

			var demoitems = GameHandler.Current.GetDemos(curmod.ModPath);
			if(demoitems.Count == 0) return;

			demos.Items.Add(new ComboBoxItem { Content = DemoItem.None });

			foreach(var di in demoitems)
			{
				var cbi = new ComboBoxItem { Content = di };
				if(di.IsInvalid) cbi.Foreground = Brushes.DarkRed;
				demos.Items.Add(cbi);

				if(di.Value == Configuration.Demo)
					demos.SelectedItem = cbi;
			}

			// Select the first item...
			if(demos.SelectedIndex == -1) demos.SelectedIndex = 0;
		}

		private void UpdateEngines()
		{
			engines.Items.Clear();

			// Store current engine...
			string currentengine = (engines.SelectedItem != null ? ((EngineItem)engines.SelectedItem).Title : string.Empty);
			var engineitems = GameHandler.Current.GetEngines();
			if(engineitems.Count == 0)
			{
				MessageBox.Show(this, "No executable files detected in the game directory (" + GameHandler.Current.GamePath
					+ ")\n\nMake sure you are running this program from your " + GameHandler.SupportedGames + " directory!", App.ErrorMessageTitle);
				Application.Current.Shutdown();
				return;
			}

			// Refill the list...
			foreach(EngineItem ei in engineitems) engines.Items.Add(ei);

			// Select last used engine
			if(!string.IsNullOrEmpty(currentengine))
			{
				foreach(EngineItem ei in engines.Items)
				{
					if(ei.Title != currentengine) continue;
					engines.SelectedItem = ei;
					break;
				}
			}

			// Select last stored engine
			if(engines.SelectedIndex == -1)
			{
				foreach(EngineItem ei in engines.Items)
				{
					if(ei.Title != Configuration.Engine) continue;
					engines.SelectedItem = ei;
					break;
				}
			}

			// Select... something
			if(engines.SelectedIndex == -1) engines.SelectedIndex = 0;

			// Set engine icon
			engineicon.Source = ((EngineItem)engines.SelectedItem).Icon;
		}

		private void UpdateCommandLinePreview(bool clearcustomargs = false)
		{
			var lp = GetLaunchParams();

			// Update the shortcut button...
			createshortcut.IsEnabled = (lp[ItemType.CLASS] == null || !lp[ItemType.CLASS].IsRandom) 
									&& (lp[ItemType.MAP] == null || !lp[ItemType.MAP].IsRandom)
									&& (lp[ItemType.SKILL] == null || !lp[ItemType.SKILL].IsRandom);

			// Update command line
			cmdline.SetArguments(lp, clearcustomargs);
		}

		// Enable/disable controls based on currently selected items
		private void UpdateInterface()
		{
			// Disable map and skill dropdowns when a demo is selected...
			var demo = GetCurrentDemo();
			bool enable = (demo == null || demo.IsDefault);
			maps.IsEnabled = enable;
			labelmaps.IsEnabled = enable;
			skills.IsEnabled = enable;
			labelskills.IsEnabled = enable;

			// Disable demo controls if no demos were found
			bool havedemos = (demos.Items.Count > 0);
			demos.IsEnabled = havedemos;
			labeldemos.IsEnabled = havedemos;
		}

		private ModItem GetCurrentMod(ModItem mi)
		{
			var gi = (GameItem)games.SelectedItem;

			// When Default ModItem and non-default GameItem is selected, return ModItem to GameItem location
			if(!mi.IsDefault || !games.IsEnabled || gi == null || gi.IsDefault)
				return mi;

			foreach(ModItem mod in allmods)
				if(string.Equals(mod.ModPath, gi.ModFolder, StringComparison.InvariantCultureIgnoreCase)) return mod;

			// GameItem without corresponding game data selected
			return mi;
		}

		// Return only when exists and non-default
		private DemoItem GetCurrentDemo()
		{
			return (DemoItem)((ComboBoxItem)demos.SelectedItem)?.Content;
		}

		private Dictionary<ItemType, AbstractItem> GetLaunchParams()
		{
			var result = new Dictionary<ItemType, AbstractItem>(8);

			result[ItemType.ENGINE] = (EngineItem)engines.SelectedItem;
			result[ItemType.RESOLUTION] = (ResolutionItem)resolutions.SelectedItem;
			result[ItemType.GAME] = ((games.IsVisible && games.IsEnabled) ? (GameItem)games.SelectedItem : null);
			result[ItemType.MOD] = ((mods.IsVisible && mods.IsEnabled) ? (ModItem)mods.SelectedItem : null);
			result[ItemType.SKILL] = ((skills.IsVisible && skills.IsEnabled) ? (SkillItem)skills.SelectedItem : null);
			result[ItemType.CLASS] = ((classes.IsVisible && classes.IsEnabled) ? (ClassItem)classes.SelectedItem : null);
			result[ItemType.DEMO] = ((demos.IsVisible && demos.IsEnabled) ? GetCurrentDemo() : null);
			result[ItemType.MAP] = ((maps.IsVisible && maps.IsEnabled && (result[ItemType.DEMO] == null || result[ItemType.DEMO].IsDefault)) ? (MapItem)maps.SelectedItem : null);

			return result;
		}
		
		// Doesn't handle random items!
		private void CreateShortcut(string shortcutpath)
		{
			var lp = GetLaunchParams();

			// Determine shortcut name
			string shortcutname;
			if(lp[ItemType.DEMO] != null && !lp[ItemType.DEMO].IsDefault)
			{
				var demo = (DemoItem)lp[ItemType.DEMO];
				string map = RemoveInvalidFilenameChars(demo.MapTitle);
				if(string.IsNullOrEmpty(map)) map = Path.GetFileName(demo.MapFilePath);
				shortcutname = "Watch '" + map.UppercaseFirst() + "' Demo";
			}
			else
			{
				// Determine game/map title
				string map;
				if(lp[ItemType.MAP] != null && !lp[ItemType.MAP].IsDefault)
				{
					var mi = (MapItem)lp[ItemType.MAP];
					map = RemoveInvalidFilenameChars(mi.MapTitle);
					if(string.IsNullOrEmpty(map)) map = mi.Value;
				}
				else if(lp[ItemType.MOD] != null && !lp[ItemType.MOD].IsDefault)
				{
					map = Path.GetFileName(lp[ItemType.MOD].Title);
				}
				else if(lp[ItemType.GAME] != null && !lp[ItemType.GAME].IsDefault)
				{
					map = RemoveInvalidFilenameChars(lp[ItemType.GAME].Title);
				}
				else
				{
					map = GameHandler.Current.GameTitle;
				}

				shortcutname = "Play '" + map.UppercaseFirst() + "'";

				// Add class/skill if available/non-default
				var extrainfo = new List<string>();
				if(lp[ItemType.CLASS] != null && !lp[ItemType.CLASS].IsDefault)
					extrainfo.Add(lp[ItemType.CLASS].Title);

				if(lp[ItemType.SKILL] != null && !lp[ItemType.SKILL].IsDefault)
					extrainfo.Add(lp[ItemType.SKILL].Title);

				if(extrainfo.Count > 0)
					shortcutname += " (" + string.Join(", ", extrainfo) + ")";
			}

			// Assemble shortcut path
			shortcutpath = Path.Combine(shortcutpath, shortcutname + ".lnk");

			// Check if we already have a shortcut with that name...
			if(File.Exists(shortcutpath) && MessageBox.Show("Shortcut '" + shortcutname + "' already exists." + Environment.NewLine 
				+ "Do you want to replace it?", "Serious Question", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
				return;

			// Create shortcut
			string enginepath = ((EngineItem)lp[ItemType.ENGINE]).FileName;
			var shell = new WshShell();
			var shortcut = (IWshShortcut)shell.CreateShortcut(shortcutpath);
			shortcut.TargetPath = enginepath;
			shortcut.WorkingDirectory = Path.GetDirectoryName(enginepath);
			shortcut.Arguments = cmdline.GetCommandLine();
			shortcut.Save();
		}

		private static string RemoveInvalidFilenameChars(string filename)
		{
			foreach(char c in Path.GetInvalidFileNameChars())
			{
				if(filename.Contains(c.ToString()))
					filename = filename.Replace(c.ToString(), (c == ':' ? " - " : ""));
			}

			return filename;
		}

		#endregion

		#region ================= Events

		private void launch_Click(object sender, EventArgs e)
		{
			var lp = GetLaunchParams();
			var engine = (EngineItem)lp[ItemType.ENGINE];
			var mod = (ModItem)lp[ItemType.MOD];
			string argsstr = cmdline.GetCommandLine();

			// Some sanity checks
			bool reloadengines = false;
			bool reloadmods = false;
			List<string> reasons = new List<string>();

			if(!File.Exists(engine.FileName))
			{
				reasons.Add("- Selected game engine does not exist!");
				reloadengines = true;
			}

			if(mod != null && !Directory.Exists(mod.ModPath))
			{
				reasons.Add("- Selected game folder not exist!");
				reloadmods = true;
			}

			if(reasons.Count > 0)
			{
				MessageBox.Show(this, "Unable to launch:\n" + string.Join("\n", reasons.ToArray()) + "\n\nAffected data will be updated.", App.ErrorMessageTitle);
				if(reloadengines) UpdateEngines();
				if(reloadmods) ReloadMods();
				return;
			}

			// Proceed with launch
			storedlocation = new Point(this.Left, this.Top);
			enginelaunched = true;

#if DEBUG
			string result = engine.FileName + " " + argsstr;
			if(MessageBox.Show(this, "Launch parameters:\n" + result + "\n\nProceed?", "Launch Preview", MessageBoxButton.YesNo) == MessageBoxResult.No) return;
#endif

			// Setup process info
			ProcessStartInfo processinfo = new ProcessStartInfo
			{
				Arguments = argsstr,
				FileName = engine.FileName,
				CreateNoWindow = false,
				ErrorDialog = false,
				UseShellExecute = true,
				WindowStyle = ProcessWindowStyle.Normal,
			};

			processinfo.WorkingDirectory = Path.GetDirectoryName(processinfo.FileName);

			try
			{
				// Start the program
				var process = Process.Start(processinfo);
				if(process != null)
				{
					process.EnableRaisingEvents = true;
					process.Exited += ProcessOnExited;
				}
			}
			catch(Exception ex)
			{
				// Unable to start the program
				MessageBox.Show(this, "Unable to start game engine, " + ex.GetType().Name + ": " + ex.Message, App.ErrorMessageTitle);
			}
		}

		private void engines_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || engines.Items.Count == 0) return;
			var ei = (EngineItem)engines.SelectedItem;
			Configuration.Engine = ei.Title;
			engineicon.Source = ei.Icon;

			UpdateCommandLinePreview();
		}

		private void resolutions_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || resolutions.Items.Count == 0) return;
			Configuration.WindowSize = (resolutions.SelectedIndex > 0 ? (ResolutionItem)resolutions.SelectedItem : ResolutionItem.Default);

			UpdateCommandLinePreview();
		}

		private void games_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || games.Items.Count == 0) return;
			Configuration.Game = games.SelectedIndex;
			var mod = GetCurrentMod((ModItem)mods.SelectedItem);
			Configuration.Mod = (mod != null && !mod.IsDefault ? mod.Value : string.Empty);

			UpdateMapsList();
			UpdateDemosList();
			UpdateCommandLinePreview();
		}

		private void mods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || mods.Items.Count == 0) return;
			var mod = (ModItem)mods.SelectedItem;
			Configuration.Mod = (mod != null ? mod.Value : string.Empty);

			UpdateMapsList();
			UpdateDemosList();
			UpdateInterface();
			UpdateCommandLinePreview();
		}

		private void maps_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || maps.Items.Count == 0) return;
			var map = (MapItem)maps.SelectedItem;
			Configuration.Map = (map != null ? map.Value : string.Empty);

			UpdateCommandLinePreview();
		}

		private void skills_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || skills.Items.Count == 0) return;
			var skill = (SkillItem)skills.SelectedItem;
			Configuration.Skill = skill.Value;

			UpdateCommandLinePreview();
		}

		private void classes_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || classes.Items.Count == 0) return;
			var pclass = (ClassItem)classes.SelectedItem;
			Configuration.Class = pclass.Value;

			UpdateCommandLinePreview();
		}

		private void demos_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if(blockupdate || demos.Items.Count == 0) return;
			var demo = GetCurrentDemo();
			Configuration.Demo = (demo != null ? demo.Value : string.Empty);

			UpdateInterface();
			UpdateCommandLinePreview();
		}

		private void clearcustomargs_Click(object sender, RoutedEventArgs e)
		{
			UpdateCommandLinePreview(true);
		}

		private void copyargs_Click(object sender, RoutedEventArgs e)
		{
			// Clipboard.SetText() is borked on many levels...
			Clipboard.SetDataObject(cmdline.GetCommandLine());
			SystemSounds.Asterisk.Play();
		}

		private void createshortcut_OnClick(object sender, RoutedEventArgs e)
		{
			createshortcut.ContextMenu.IsOpen = true;
		}

		private void createdesktopshortcut_OnClick(object sender, RoutedEventArgs e)
		{
			CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
		}

		private void createfoldershortcut_OnClick(object sender, RoutedEventArgs e)
		{
			var engine = (EngineItem)engines.SelectedItem;
			CreateShortcut(Path.GetDirectoryName(engine.FileName));
		}

		// Restore window location (fitzquake messes this up when launching in fullscreen)
		private void ProcessOnExited(object sender, EventArgs e)
		{
			// Cross-thread call required...
			this.Dispatcher.Invoke(() =>
			{
				this.Left = storedlocation.X;
				this.Top = storedlocation.Y;
			});
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			Setup();
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
#if DEBUG
			Configuration.Save();
#else
			if(enginelaunched) Configuration.Save();
#endif
		}

		private void MainWindow_Activated(object sender, EventArgs e)
		{
#if DEBUG
			return;
#endif

			// Reload data after regaining focus
			if(focusstate == FocusState.UNFOCUSED)
			{
				focusstate = FocusState.FOCUSED;
				ReloadMods();
			}
		}

		private void MainWindow_Deactivated(object sender, EventArgs e)
		{
			focusstate = FocusState.UNFOCUSED;
		}

		#endregion
	}
}

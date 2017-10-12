#region ================= Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Data
{
	static class Configuration
	{
		#region ================= Properties

		public static string Engine = string.Empty;
		public static ResolutionItem WindowSize = ResolutionItem.Default;
		public static string Mod = string.Empty;
		public static string Map = string.Empty;
		public static string Demo = string.Empty;
		public static string Skill = string.Empty;
		public static string Class = string.Empty; // [Hexen 2]
		public static int Game; 
		public static Dictionary<ItemType, string> ExtraArguments = new Dictionary<ItemType, string>(); // LaunchParameterType, custom arg

		#endregion

		#region ================= Variables

		// Local stuff
		private static string configpath;
		private static readonly string[] separator = { " = " };
		private static readonly string[] extraargsseparator = { "|" };

		#endregion

		#region ================= Save/Load

		public static void Load(string path)
		{
			configpath = path;
			if(!File.Exists(configpath)) return;
			
			// Read values
			string[] lines = File.ReadAllLines(configpath);
			foreach (string line in lines)
			{
				string[] bits = line.Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if(bits.Length != 2) continue;

				string key = bits[0].ToLower().Trim();
				string value = bits[1].Trim();
				if(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value)) continue;

				switch(key)
				{
					case "resolution":
						int w, h;
						string[] pieces = value.Split(new[] {"x"}, StringSplitOptions.None);
						if(pieces.Length == 2 && int.TryParse(pieces[0], out w) && int.TryParse(pieces[1], out h))
							WindowSize = new ResolutionItem(w, h);
						break;

					case "extraargs":
						ExtraArguments = new Dictionary<ItemType, string>();
						string[] args = value.Split(extraargsseparator, StringSplitOptions.RemoveEmptyEntries);
						foreach(string arg in args)
						{
							string typestr = arg.Substring(0, 2);
							ItemType type = ItemTypes.Markers.ContainsKey(typestr) ? ItemTypes.Markers[typestr] : ItemType.ENGINE;
							string trimmedarg = arg.Substring(2).Trim();
							if(string.IsNullOrEmpty(trimmedarg)) continue;

							if(ExtraArguments.ContainsKey(type))
								ExtraArguments[type] += " " + trimmedarg;
							else
								ExtraArguments[type] = trimmedarg;
						}
						break;

					case "engine": Engine = value; break;
					case "game": Mod = value; break;
					case "map": Map = value; break;
					case "demo": Demo = value; break;
					case "skill": Skill = value; break;
					case "class": Class = value; break;
					case "basegame": int.TryParse(value, out Game);	break;

					default:
						System.Windows.MessageBox.Show("Got unknown configuration parameter:\n'" + line + "'", App.ErrorMessageTitle);
						break;
				}
			}
		}

		public static void Save()
		{
			var sb = new StringBuilder(100);

			if(!string.IsNullOrEmpty(Engine)) sb.AppendLine("engine" + separator[0] + Engine);
			if(!WindowSize.IsDefault) sb.AppendLine("resolution" + separator[0] + WindowSize);
			if(!string.IsNullOrEmpty(Mod)) sb.AppendLine("game" + separator[0] + Mod);
			if(!string.IsNullOrEmpty(Map)) sb.AppendLine("map" + separator[0] + Map);
			if(!string.IsNullOrEmpty(Demo)) sb.AppendLine("demo" + separator[0] + Demo);
			if(!string.IsNullOrEmpty(Skill) && Skill != SkillItem.Default.Value) sb.AppendLine("skill" + separator[0] + Skill);
			if(!string.IsNullOrEmpty(Class) && Class != ClassItem.Default.Value) sb.AppendLine("class" + separator[0] + Class);
			if(Game > 0) sb.AppendLine("basegame" + separator[0] + Game);
			if(ExtraArguments.Count > 0)
			{
				var args = new List<string>();
				foreach(var group in ExtraArguments)
				{
					if(!string.IsNullOrEmpty(group.Value))
						args.Add(ItemTypes.Types[group.Key] + " " + group.Value);
				}

				if(args.Count > 0) sb.AppendLine("extraargs" + separator[0] + string.Join(extraargsseparator[0], args.ToArray()));
			}

			try
			{
				using(StreamWriter writer = File.CreateText(configpath)) writer.Write(sb);
			}
			catch(Exception ex)
			{
				System.Windows.MessageBox.Show("Unable to save configuration file '" + configpath + "'!\n" + ex.GetType().Name + ": " + ex.Message, App.ErrorMessageTitle);
			}
		}

		#endregion
	}
}

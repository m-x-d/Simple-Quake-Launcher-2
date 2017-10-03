#region ================= Namespaces

using System;
using System.IO;
using mxd.SQL2.Games;

#endregion

namespace mxd.SQL2.Items
{
	public class ModItem : AbstractItem
	{
		#region ================= Default items

		public static readonly ModItem Default = new ModItem(NAME_DEFAULT, GameHandler.Current.DefaultModPath);

		#endregion

		#region ================= Variables

		private string modpath; // c:\quake\mymod
		private bool isbuiltin; // true for mods enabled by special cmdline params, like -rogue

        #endregion

        #region ================= Properties

        // Value: "Arcane Dimensions"
        // Title: Arcane Dimensions
        public string ModPath => modpath; // c:\quake\Arcane Dimensions
        public bool IsBuiltIn => isbuiltin;

        public override ItemType Type => ItemType.MOD;
        private new bool IsRandom; // No random mods

        #endregion

        #region ================= Constructors

        // mods\Arcane Dimensions, "c:\Quake\mods\Arcane Dimensions"
        public ModItem(string modname, string modpath, bool isbuiltin = false) : base(modname, modname)
        {
#if DEBUG
			if(!Directory.Exists(modpath)) throw new Exception("Invalid modpath!");
#endif
            this.modpath = modpath;
            this.isbuiltin = isbuiltin;
		}

		#endregion
	}
}

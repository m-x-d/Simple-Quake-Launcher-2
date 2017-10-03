using System;
using System.IO;
using mxd.SQL2.Games;

namespace mxd.SQL2.Items
{
	public class GameItem : AbstractItem
	{
		#region ================= Variables

		private readonly string modfolder;

        #endregion

        #region ================= Properties
        
        // Value: -hipnotic
        // Title: EP1: Scourge of Armagon
        public string ModFolder => modfolder; // c:\Quake\mymod

        public override ItemType Type => ItemType.GAME;
        private new bool IsRandom; // No random base games

		#endregion

		#region ================= Constructor

		// "EP1: Scourge of Armagon", "HIPNOTIC", -hipnotic
		public GameItem(string name, string modfolder, string arg) : base(name, arg)
		{
			this.modfolder = Path.Combine(App.GamePath, modfolder);
		    this.isdefault = (string.Equals(GameHandler.Current.DefaultModPath, this.modfolder, StringComparison.OrdinalIgnoreCase));
		}

		#endregion
	}
}

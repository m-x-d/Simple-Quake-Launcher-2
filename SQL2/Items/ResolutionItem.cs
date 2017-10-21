#region ================= Namespaces

using System;
using System.IO;
using mxd.SQL2.Games;

#endregion

namespace mxd.SQL2.Items
{
	public class ResolutionItem : AbstractItem
	{
		#region ================= Default items

		public static readonly ResolutionItem Default = new ResolutionItem();

		#endregion

		#region ================= Variables

		protected override ItemType type => ItemType.RESOLUTION;

		#endregion

		#region ================= Properties

		public readonly int Width;
		public readonly int Height;

		private new bool IsRandom; // No random resolutions

		#endregion

		#region ================= Constructors

		private ResolutionItem() : base(NAME_DEFAULT, string.Empty) { }

		public ResolutionItem(int width, int height, int index = -1, bool fullscreen = false) 
			: base(width + "x" + height + (fullscreen ? " (fullscreen)" : ""), (index == -1 ? width + "x" + height : index.ToString()) + "x" + fullscreen)
		{
			Width = width;
			Height = height;
		}

		protected override string GetArgument(string val)
		{
			// Skip parsing shenanigans for the default item...
			if(title == NAME_DEFAULT) return val;
			
			// val is either WIDTHxHEIGHTxFULLSCREEN or INDEXxFULLSCREEN...
			int w, h;
			bool fullscreen;
			string[] pieces = value.Split(new[] { "x" }, StringSplitOptions.None);

			if(pieces.Length == 3 && int.TryParse(pieces[0], out w) && int.TryParse(pieces[1], out h) && bool.TryParse(pieces[2], out fullscreen))
				return string.Format(param, w, h, GameHandler.Current.FullScreenArg[fullscreen]);

			if(pieces.Length == 2 && int.TryParse(pieces[0], out w) && bool.TryParse(pieces[1], out fullscreen))
				return string.Format(param, w, GameHandler.Current.FullScreenArg[fullscreen]);

			// Should never happen
			throw new InvalidDataException("Unexpected screen resolution: " + val);
		}

		#endregion
	}
}

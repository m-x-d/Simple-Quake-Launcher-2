#region ================= Namespaces

using System;
using System.IO;

#endregion

namespace mxd.SQL2.Items
{
	public class ResolutionItem : AbstractItem
	{
		#region ================= Default items

		public static readonly ResolutionItem Default = new ResolutionItem();

		#endregion
		
		#region ================= Properties

		public readonly int Width;
		public readonly int Height;

		public override ItemType Type => ItemType.RESOLUTION;
		private new bool IsRandom; // No random resolutions

		#endregion

		#region ================= Constructors

		private ResolutionItem() : base(NAME_DEFAULT, "0x0") { }

		public ResolutionItem(string title, int index) : base(title, index.ToString()) { }

		public ResolutionItem(int width, int height) : base(width + "x" + height, width + "x" + height)
		{
			Width = width;
			Height = height;
		}

		protected override string GetArgument(string val)
		{
			// val is Either WIDTHxHEIGHT or INDEX...
			int w, h;
			string[] pieces = value.Split(new[] { "x" }, StringSplitOptions.None);
			if(pieces.Length == 2 && int.TryParse(pieces[0], out w) && int.TryParse(pieces[1], out h))
				return string.Format(param, w, h);

			if(int.TryParse(val, out w))
				return string.Format(param, w);

			// Should never happen
			throw new InvalidDataException("Unexpected screen resolution: " + val);
		}

		#endregion
	}
}

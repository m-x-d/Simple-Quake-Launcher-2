namespace mxd.SQL2.Data
{
	// Hardcoded video mode used by older engines...
	public struct VideoModeInfo
	{
		private readonly int width;
		private readonly int height;
		private readonly int index;

		public int Width => width;
		public int Height => height;
		public int Index => index;

		public VideoModeInfo(int width, int height, int index)
		{
			this.width = width;
			this.height = height;
			this.index = index;
		}

		public override string ToString()
		{
			return width + "x" + height;
		}
	}
}

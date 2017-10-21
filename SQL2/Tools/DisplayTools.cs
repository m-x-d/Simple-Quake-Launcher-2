#region ================= Namespaces

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using mxd.SQL2.Data;
using mxd.SQL2.Items;

#endregion

namespace mxd.SQL2.Tools
{
	internal static class DisplayTools
	{
		#region ================= Imports/consts

		[DllImport("user32.dll")]
		private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DeviceMode devMode);

		#endregion

		#region ================= Structs

		[StructLayout(LayoutKind.Sequential)]
		private struct DeviceMode
		{
			private const int CCHDEVICENAME = 0x20;
			private const int CCHFORMNAME = 0x20;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
			public string dmDeviceName;
			public short dmSpecVersion;
			public short dmDriverVersion;
			public short dmSize;
			public short dmDriverExtra;
			public int dmFields;
			public int dmPositionX;
			public int dmPositionY;
			public ScreenOrientation dmDisplayOrientation;
			public int dmDisplayFixedOutput;
			public short dmColor;
			public short dmDuplex;
			public short dmYResolution;
			public short dmTTOption;
			public short dmCollate;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
			public string dmFormName;
			public short dmLogPixels;
			public int dmBitsPerPel;
			public int dmPelsWidth;
			public int dmPelsHeight;
			public int dmDisplayFlags;
			public int dmDisplayFrequency;
			public int dmICMMethod;
			public int dmICMIntent;
			public int dmMediaType;
			public int dmDitherType;
			public int dmReserved1;
			public int dmReserved2;
			public int dmPanningWidth;
			public int dmPanningHeight;
		}

		#endregion

		#region ================= Methods

		public static List<ResolutionItem> GetVideoModes()
		{
			var dm = new DeviceMode();
			var modes = new Dictionary<string, ResolutionItem>(1);
			var screenarea = Screen.PrimaryScreen.WorkingArea;
			var screenres = Screen.PrimaryScreen.Bounds;
			int i = 0;
			
			while(EnumDisplaySettings(null, i++, ref dm))
			{
				string key = dm.dmPelsWidth + "x" + dm.dmPelsHeight;
				if(!modes.ContainsKey(key))
				{
					if(dm.dmPelsWidth < screenarea.Width && dm.dmPelsHeight < screenarea.Height)
						modes.Add(key, new ResolutionItem(dm.dmPelsWidth, dm.dmPelsHeight));
					else if(dm.dmPelsWidth == screenres.Width && dm.dmPelsHeight == screenres.Height)
						modes.Add(key, new ResolutionItem(dm.dmPelsWidth, dm.dmPelsHeight, -1, true));
				}
			}

			// Sort in descending order...
			var result = modes.Values.ToList();
			result.Sort((i1, i2) => (i1.Width == i2.Width ? i1.Height.CompareTo(i2.Height) : i1.Width.CompareTo(i2.Width)) * -1);
			return result;
		}

		public static List<ResolutionItem> GetFixedVideoModes(List<VideoModeInfo> rmodes)
		{
			var screenarea = Screen.PrimaryScreen.WorkingArea;
			var screenres = Screen.PrimaryScreen.Bounds;
			var result = new List<ResolutionItem>();

			// Pick all the modes smaller than screenarea or equal to screenres...
			foreach(var vmi in rmodes)
			{
				if(vmi.Width < screenarea.Width && vmi.Height < screenarea.Height)
					result.Add(new ResolutionItem(vmi.Width, vmi.Height, vmi.Index));
				else if(vmi.Width == screenres.Width && vmi.Height == screenres.Height)
					result.Add(new ResolutionItem(vmi.Width, vmi.Height, vmi.Index, true));
			}
				
			// Sort in descending order...
			result.Sort((i1, i2) => (i1.Width == i2.Width ? i1.Height.CompareTo(i2.Height) : i1.Width.CompareTo(i2.Width)) * -1);
			return result;
		}

		#endregion
	}
}

#region ================= Namespaces

using System.IO;
using System.Text;

#endregion

namespace mxd.SQL2.Tools
{
	// Extension methods for binary reader
	public static class BinaryReaderEx
	{
		#region ================= String reading

		// Reads given length of bytes as a string 
		public static string ReadStringExactLength(this BinaryReader br, int len)
		{
			char[] arr = new char[len];
			int i;

			for(i = 0; i < len; ++i)
			{
				var c = br.ReadChar();
				if(c == '\0') break;
				arr[i] = c;
			}

			if(i < len) br.BaseStream.Position += (len - i - 1);
			return new string(arr, 0, i);
		}

		// Reads a string until either maxlength chars are read or terminator char is encountered
		public static string ReadString(this BinaryReader reader, int maxlength, char terminator = '\0')
		{
			char[] arr = new char[maxlength];
			int i;

			for(i = 0; i < maxlength; i++)
			{
				var c = reader.ReadChar();
				if(c == terminator) break;
				arr[i] = c;
			}

			return new string(arr, 0, i);
		}

		// Reads bytes as a string until given char, null or EOF is encountered
		public static string ReadString(this BinaryReader br, char stopper)
		{
			var sb = new StringBuilder();

			if(stopper == '\0')
			{
				while(br.BaseStream.Position < br.BaseStream.Length)
				{
					var c = br.ReadChar();
					if(c == '\0') break;
					sb.Append(c);
				}
			}
			else
			{
				while(br.BaseStream.Position < br.BaseStream.Length)
				{
					var c = br.ReadChar();
					if(c == '\0' || c == stopper) break;
					sb.Append(c);
				}
			}
			
			return sb.ToString();
		}

		public static bool SkipString(this BinaryReader reader, int maxlength, char terminator = '\0')
		{
			char c = '0';
			for(int i = 0; i < maxlength; i++)
			{
				c = reader.ReadChar();
				if(c == terminator) break;
			}

			return (c == terminator);
		}

		#endregion

		#region ================= Special string reading

		public static string ReadMapTitle(this BinaryReader reader, int maxlength, string[] charmap)
		{
			string result = string.Empty;

			byte prevchar = 0;
			for(int i = 0; i < maxlength; i++)
			{
				var b = reader.ReadByte();

				// Stop on null char
				if(b == 0) break;

				// Replace newline with space
				if(b == 'n' && prevchar == '\\')
				{
					prevchar = b;
					result = result.Remove(result.Length - 1, 1) + ' ';
					continue;
				}

				// Trim extra spaces...
				if(!(prevchar == 32 && prevchar == b)) result += charmap[b];
				prevchar = b;
			}

			return result;
		}

		#endregion
	}
}

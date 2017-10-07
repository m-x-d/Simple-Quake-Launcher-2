#region ================= Namespaces

using System.IO;

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
			string result = string.Empty;
			int i;

			for(i = 0; i < len; ++i)
			{
				var c = br.ReadChar();
				if(c == '\0')
				{
					++i;
					break;
				}
				result += c;
			}

			for(; i < len; ++i) br.ReadChar();
			return result;
		}

		// Reads a string until either maxlength chars are read or terminator char is encountered
		public static string ReadString(this BinaryReader reader, int maxlength, char terminator = '\0')
		{
			string result = string.Empty;

			for(int i = 0; i < maxlength; i++)
			{
				var c = reader.ReadChar();
				if(c == terminator) break;
				result += c;
			}

			return result;
		}

		// Reads bytes as a string until given char, null or EOF is encountered
		public static string ReadString(this BinaryReader br, char stopper)
		{
			string name = string.Empty;

			if(stopper == '\0')
			{
				while(br.BaseStream.Position < br.BaseStream.Length)
				{
					var c = br.ReadChar();
					if(c == '\0') break;
					name += c;
				}
			}
			else
			{
				while(br.BaseStream.Position < br.BaseStream.Length)
				{
					var c = br.ReadChar();
					if(c == '\0' || c == stopper) break;
					name += c;
				}
			}
			
			return name;
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

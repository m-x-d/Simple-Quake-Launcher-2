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
        public static string ReadString(this BinaryReader br, int len)
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

		// Reads bytes as a string untill given char, null or EOF is encountered 
		public static string ReadString(this BinaryReader br, char stopper)
		{
			string name = string.Empty;
			while(br.BaseStream.Position < br.BaseStream.Length)
			{
				var c = br.ReadChar();
				if(c == '\0' || c == stopper) break;
				name += c;
			}
			return name;
		}

		#endregion
	}
}

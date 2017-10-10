#region ================= Namespaces

using System;
using System.IO;
using System.IO.Compression;

#endregion

namespace mxd.SQL2.DataReaders
{
	public class DeflateStreamWrapper : Stream
	{
		#region ================= Variables

		private Stream stream;
		private long position;
		private long length;

		#endregion

		#region ================= Properties

		// Obligatory abstract property overrides
		public override bool CanRead => stream.CanRead;
		public override bool CanSeek => stream.CanSeek;
		public override bool CanWrite => stream.CanWrite;

		// Emulated stream properties
		public override long Length => length;
		public override long Position { get { return position; } set { SkipTo(value - position); } }

		#endregion

		#region ================= Constructor

		// DeflateStream cannot return Position or Length 
		public DeflateStreamWrapper(DeflateStream stream, long length)
		{
			if(stream == null) throw new NullReferenceException("Stream is null!");

			this.stream = stream;
			this.length = length;
			this.position = 0;
		}

		#endregion

		#region ================= Methods

		public override long Seek(long offset, SeekOrigin origin)
		{
			switch(origin)
			{
				case SeekOrigin.Current: SkipTo(offset); break;
				case SeekOrigin.Begin: SkipTo(offset - position); break;
				case SeekOrigin.End: SkipTo(length + offset - position); break;
			}

			return position;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			int result = stream.Read(buffer, offset, count);
			position += result;
			return result;
		}

		private void SkipTo(long offset)
		{
			if(offset < 0 || length - position < offset) throw new Exception("Stream cannot be rewinded!");
			if(offset == 0) return;

			byte[] unused = new byte[offset];
			int result = stream.Read(unused, 0, (int)offset);
			position += result;
		}

		// Obligatory abstract method overrides
		public override void Flush() { stream.Flush(); }
		public override void SetLength(long value) { stream.SetLength(value); }
		public override void Write(byte[] buffer, int offset, int count) { stream.Write(buffer, offset, count); }

		#endregion
	}
}

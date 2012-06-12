//***************************************************
// Uncloseable Stream
//  Copyright © Rylogic Ltd 2009
//***************************************************

using System;
using System.IO;
using System.Text;
using pr.stream;

namespace pr.stream
{
	// Prevents a reader/writer class closing a stream when closed.
	// Usage:
	//	using (BinaryWriter wr = new BinaryWriter(new UncloseableStream(strm))) {}
	public sealed class UncloseableStream :StreamWrapper
	{
		private bool m_closed = false; // True when the wrapped stream has been closed
		
		public UncloseableStream(Stream stream) :base(stream) {}
		public override bool CanRead            { get { return m_closed ? false : base.CanRead; } }
		public override bool CanSeek            { get { return m_closed ? false : base.CanSeek; } }
		public override bool CanWrite           { get { return m_closed ? false : base.CanWrite; } }
		public override long Length             { get { CheckClosed(); return base.Length; } }
		public override long Position           { get { CheckClosed(); return base.Position; } set { CheckClosed(); base.Position = value; } }

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			CheckClosed();
			return base.BeginRead(buffer, offset, count, callback, state);
		}
		public override int EndRead(IAsyncResult asyncResult)
		{
			CheckClosed();
			return base.EndRead(asyncResult);
		}
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			CheckClosed();
			return base.BeginWrite(buffer, offset, count, callback, state);
		}
		public override void EndWrite(IAsyncResult asyncResult)
		{
			CheckClosed();
			base.EndWrite(asyncResult);
		}
		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckClosed();
			return base.Read(buffer, offset, count);
		}
		public override int ReadByte()
		{
			CheckClosed();
			return base.ReadByte();
		}
		public override long Seek(long offset, SeekOrigin origin)
		{
			CheckClosed();
			return base.Seek(offset, origin);
		}
		public override void SetLength(long value)
		{
			CheckClosed();
			base.SetLength(value);
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			CheckClosed();
			base.Write(buffer, offset, count);
		}
		public override void WriteByte(byte value)
		{
			CheckClosed();
			base.WriteByte(value);
		}
		public override void Flush()
		{
			CheckClosed();
			base.Flush();
		}

		// This method is not proxied to the underlying stream; instead, the wrapper
		// is marked as unusable for other (non-close/Dispose) operations. The underlying
		// stream is flushed if the wrapper wasn't m_closed before this call.
		public override void Close()
		{
			if (!m_closed) { base.Flush(); }
			m_closed = true;
		}
		private void CheckClosed()
		{
			if (m_closed) throw new InvalidOperationException("Stream has been closed or disposed");
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	// ReSharper disable PossibleNullReferenceException, AccessToModifiedClosure
	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestUnclosableStream()
		{
			const string str = "The quick brown fox jumped over the lazy dog";
			
			// First check the normal case where StreamReader closes the stream
			// Then check that the uncloseable stream wrapper works.
			for (int i = 0; i != 2; ++i)
			{
				MemoryStream s = null;
				TestDelegate func = () => { Assert.AreEqual(str.Length, s.Capacity); };
				
				using (s = new MemoryStream(Encoding.ASCII.GetBytes(str)))
				{
					Assert.DoesNotThrow(()=>{Assert.True(s.CanRead);});
					using (StreamReader r = new StreamReader(i == 0 ? (Stream)s : new UncloseableStream(s)))
					{
						Assert.AreEqual(str, r.ReadToEnd());
					}
					
					if (i == 0) Assert.Throws<ObjectDisposedException>(func);
					else        Assert.DoesNotThrow                   (func);
				}
				Assert.Throws<ObjectDisposedException>(func);
			}
		}
	}
	// ReSharper restore PossibleNullReferenceException, AccessToModifiedClosure
}
#endif
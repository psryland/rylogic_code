using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using pr.stream;

namespace pr.stream
{
	/// <summary>An IO stream for linking sources to sinks. Basically a std::deque</summary>
	public class LinkStream
	{
		private class Block
		{
			/// <summary>The block data buffer</summary>
			public readonly byte[] Data;
			
			/// <summary>The number of used bytes in 'Data'</summary>
			public int Size;
			
			public Block(int size) { Data = new byte[size]; Size = 0; }
		}
		private const int DefaultBlockBufferSize = 4096;
		private readonly EventWaitHandle m_data_available = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly EventWaitHandle m_data_consumed  = new EventWaitHandle(false, EventResetMode.AutoReset);
		private readonly List<Block>     m_data           = new List<Block>();  // blocks in use, containing data
		private readonly Queue<Block>    m_avail          = new Queue<Block>(); // blocks available for recycling
		private int m_capacity;        // The current allocated memory
		
		/// <summary>Controls how big each allocation unit is</summary>
		public int BlockBufferSize { get; set; }
		
		/// <summary>Limits the maximum size that the link buffer can grow to. Once at or above MaxCapacity writes will block</summary>
		public int MaxCapacity { get; set; }
		
		/// <summary>The memory currently in use</summary>
		public int CommitCharge { get; private set; }
		
		/// <summary>The current capacity of the link buffer</summary>
		public int Capacity
		{
			get { return m_capacity; }
			set
			{
				if (value > MaxCapacity)  throw new ArgumentException("Cannot exceed MaxCapacity");
				if (value < CommitCharge) throw new ArgumentException("Cannot reduce Capacity below what is currently in use");
				lock (m_avail)
				{
					for (;m_avail.Count != 0 && m_capacity - m_avail.Peek().Data.Length > value;)
					{
						var b = m_avail.Dequeue();
						m_capacity -= b.Data.Length;
						// drop 'b'
					}
					for (;m_capacity < value;)
					{
						var b = new Block(BlockBufferSize);
						m_avail.Enqueue(b);
						m_capacity += b.Data.Length;
					}
				}
				GC.Collect();
			}
		}
		
		/// <summary>Access to the read interface of the stream</summary>
		public Stream IStream { get; private set; }
		
		/// <summary>Access to the write interface of the stream</summary>
		public Stream OStream { get; private set; }
		
		public LinkStream() :this(DefaultBlockBufferSize * 8) {}
		public LinkStream(int capacity)
		{
			m_capacity = 0;
			CommitCharge = 0;
			BlockBufferSize = DefaultBlockBufferSize;
			MaxCapacity  = 0x7FFFFFFF;
			Capacity = capacity;
			IStream = new InputStream(this);
			OStream = new OutputStream(this);
		}
		
		/// <summary>Get a recycled block</summary>
		private Block Recycle() // don't make into a property as watching in the debugger causes blocks to be used
		{
			Block b;
			lock (m_avail) b = m_avail.Count != 0 ? m_avail.Dequeue() : null;
			if (b != null) CommitCharge += b.Data.Length;
			return b;
		}
		
		/// <summary>Return a block to be recycled</summary>
		private void Recycle(Block block)
		{
			block.Size = 0;
			CommitCharge -= block.Data.Length;
			lock (m_avail) m_avail.Enqueue(block);
		}
		
		private bool Empty
		{
			get { return m_data.Count == 0; }
		}
		private Block Front
		{
			get { if (Empty) throw new ApplicationException("No data available"); return m_data[0]; }
		}
		private Block Back
		{
			get { if (Empty) throw new ApplicationException("No data available"); return m_data[m_data.Count - 1]; }
		}
		private Block PopFront()
		{
			Block b = Front; m_data.RemoveAt(0); return b;
		}
		private Block PopBack()
		{
			Block b = Back; m_data.RemoveAt(m_data.Count-1); return b;
		}
		private void PushFront(Block b)
		{
			m_data.Insert(0, b);
		}
		private void PushBack(Block b)
		{
			m_data.Add(b);
		}
		private void WaitForDataAvailable()
		{
			if (m_data_available.WaitOne(IStream.ReadTimeout) || ((OutputStream)OStream).Closed) return;
			throw new TimeoutException("read timeout");
		}
		private void WaitForDataConsumed()
		{
			if (m_data_consumed.WaitOne(OStream.WriteTimeout) || ((InputStream)IStream).Closed) return;
			throw new TimeoutException("read timeout");
		}
		
		/// <summary>A read interface to the internal buffer</summary>
		private class InputStream :Stream
		{
			private readonly LinkStream m_parent;
			private int m_read_timeout_ms;
			private int m_block_pos;
			
			public InputStream(LinkStream parent)                            { m_parent = parent; m_read_timeout_ms = Timeout.Infinite; }
			public override bool CanRead                                     { get { return true;  } }
			public override bool CanSeek                                     { get { return false; } }
			public override bool CanWrite                                    { get { return false; } }
			public override void Flush()                                     { } // Always flushed.
			public override long Length                                      { get { throw new NotSupportedException(); } }
			public override long Position                                    { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
			public override bool CanTimeout                                  { get { return true; } }
			public override int  ReadTimeout                                 { get { return m_read_timeout_ms; } set { m_read_timeout_ms = value; } }
			public override long Seek(long offset, SeekOrigin origin)        { throw new NotSupportedException(); }
			public override void SetLength(long value)                       { throw new NotSupportedException(); }
			public override void Write(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
			public override void Close()                                     { try { Closed = true; base.Close(); m_parent.m_data_consumed.Set(); } catch {} }
			public bool Closed                                               { get; private set; }
			
			/// <summary>Read bytes from the stream</summary>
			public override int Read(byte[] buffer, int offset, int count)
			{
				int total_read = 0;
				while (count != 0)
				{
					// Get the block at the front of the queue
					Block b;
					lock (m_parent.m_data)
						b = !m_parent.Empty ? m_parent.PopFront() : null;
						
					// None available? block until same is added
					if (b == null)
					{
						// If we've read something, or the src stream is closed, return what we have
						if (total_read != 0 || ((OutputStream)m_parent.OStream).Closed) break;
						m_parent.WaitForDataAvailable();
						continue;
					}
					
					// Read as much from it as possible
					int read = Math.Min(b.Size - m_block_pos, count);
					Array.Copy(b.Data, m_block_pos, buffer, offset, read);
					m_block_pos += read;
					total_read += read;
					offset += read;
					count -= read;
					
					// If the block is all used up, return it to the recycler
					if (m_block_pos == b.Size)
					{
						m_parent.Recycle(b);
						m_block_pos = 0;
						m_parent.m_data_consumed.Set();
					}
					// If not used up, return it to the data queue
					else
					{
						lock (m_parent.m_data)
							m_parent.PushFront(b);
					}
				}
				return total_read;
			}
		}
		
		/// <summary>A write interface to the internal buffer</summary>
		private class OutputStream :Stream
		{
			private readonly LinkStream m_parent;
			private int m_write_timeout_ms;
			
			public OutputStream(LinkStream parent)                           { m_parent = parent; m_write_timeout_ms = Timeout.Infinite; }
			public override bool CanRead                                     { get { return false; } }
			public override bool CanSeek                                     { get { return false; } }
			public override bool CanWrite                                    { get { return true; } }
			public override void Flush()                                     { } // Always flushed.
			public override long Length                                      { get { throw new NotSupportedException(); } }
			public override long Position                                    { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
			public override bool CanTimeout                                  { get { return true; } }
			public override int  WriteTimeout                                { get { return m_write_timeout_ms; } set { m_write_timeout_ms = value; } }
			public override int  Read(byte[] buffer, int offset, int count)  { throw new NotSupportedException(); }
			public override long Seek(long offset, SeekOrigin origin)        { throw new NotSupportedException(); }
			public override void SetLength(long value)                       { throw new NotSupportedException(); }
			public override void Close()                                     { try { Closed = true; base.Close(); m_parent.m_data_available.Set(); } catch {} }
			public bool Closed                                               { get; private set; }
			
			/// <summary>Write bytes to the stream</summary>
			public override void Write(byte[] buffer, int offset, int count)
			{
				while (count != 0)
				{
					// If there is more than one block in use, we can add our data
					// to the last block because it hasn't been read from yet. Otherwise
					// get a new block from the recycler so that we don't block reads
					Block b;
					lock (m_parent.m_data)
						b = m_parent.m_data.Count > 1 ? m_parent.PopBack() : m_parent.Recycle();
					
					// None available? block until some is consumed, freeing up blocks
					if (b == null)
					{
						m_parent.WaitForDataConsumed();
						continue;
					}
					
					// Fill the block with data
					int writ = Math.Min(b.Data.Length - b.Size, count);
					Array.Copy(buffer, offset, b.Data, b.Size, writ);
					b.Size += writ;
					offset += writ;
					count -= writ;

					// Add the block
					lock (m_parent.m_data)
					{
						m_parent.PushBack(b);
						m_parent.m_data_available.Set();
					}
				}
			}
		}
	}
}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestLinkStream()
		{
			const string src = "This is a longest message to test blocking and asynchronous communication using the link stream";
			string msg = string.Empty;

			EventWaitHandle wait = new EventWaitHandle(false, EventResetMode.ManualReset);
			Exception write_ex = null, read_ex = null;
			
			LinkStream link = new LinkStream{BlockBufferSize = 4, MaxCapacity = 8};
			ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						using (var sw = new StreamWriter(link.OStream))
							sw.Write(src);
					}
					catch (Exception ex) { write_ex = ex; wait.Set(); }
				});
			ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						
						using (var sr = new StreamReader(link.IStream))
							msg = sr.ReadToEnd();
						
						wait.Set();
					}
					catch (Exception ex) { read_ex = ex; wait.Set(); }
				});

			wait.WaitOne();
			Assert.IsNull(write_ex);
			Assert.IsNull(read_ex);
			Assert.AreEqual(src, msg);
		}
	}
}
#endif

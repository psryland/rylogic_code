//***************************************************
// AggregateFileStream Stream
//  Copyright (c) Rylogic Ltd 2013
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Rylogic.Extn;

namespace Rylogic.Streams
{
	/// <summary>Combines multiple files into a stream that looks like a single file</summary>
	public sealed class AggregateFileStream :Stream
	{
		public AggregateFileStream(IEnumerable<string> filepaths, FileShare file_share = FileShare.None, int buffer_size = 0x1000, FileOptions file_options = FileOptions.None)
		{
			FileShare = file_share;
			FileBufferSize = buffer_size;
			FileOptions = file_options;
			Files = new List<FileInfo>();
			m_boundary = new List<long>();
			m_fs = null;

			SetFiles(filepaths);
		}

		/// <summary>The byte offset boundaries between each file</summary>
		private readonly List<long> m_boundary;

		/// <summary>The combined length of all files</summary>
		private long m_total_length;

		/// <summary>The current aggregated file position</summary>
		private long m_position;

		/// <summary>A file stream that reads from the one of the referenced files</summary>
		private FileStream? m_fs;

		/// <summary>The index of the file that m_fs currently represents</summary>
		private int m_fs_index;

		/// <summary>The sharing mode to use when opening the files</summary>
		public FileShare FileShare { get; set; }

		/// <summary>The internal file buffering size</summary>
		public int FileBufferSize { get; set; }

		/// <summary>Additional options when opening files</summary>
		public FileOptions FileOptions { get; set; }

		/// <summary>Get the files that make up this aggregate file stream</summary>
		public List<FileInfo> Files { get; private set; }

		/// <summary>Updates the collection of files</summary>
		public void SetFiles(IEnumerable<string> filepaths)
		{
			Files.Clear();
			foreach (var f in filepaths)
				Files.Add(new FileInfo(f));

			// Determine the combined file length
			m_total_length = 0;
			m_boundary.Clear();
			foreach (var f in Files)
			{
				m_total_length += f.Length;
				m_boundary.Add(m_total_length);
			}
			m_boundary.Add(m_total_length); // Add an extra one to avoid extra ifs

			// Reset the file position
			m_position = 0;
			m_fs = null;
			m_fs_index = -1;
			
			// Force 'FileStream' to be created so that a lock is held from construction
			var _ = FileStream;
		}

		/// <summary>Returns the file stream appropriate for the current 'Position' value</summary>
		public FileStream? FileStream
		{
			get { return FileStreamAtOffset(Position); }
		}

		/// <summary>Returns the file stream appropriate for byte offset 'offset'</summary>
		public FileStream? FileStreamAtOffset(long offset)
		{
			var fidx = FileIndexAtOffset(offset);
			if (m_fs != null && fidx == m_fs_index) return m_fs;
			if (m_fs != null) m_fs.Dispose();

			m_fs_index = fidx;
			System.Diagnostics.Debug.Assert(m_fs_index >= -1 && m_fs_index <= Files.Count);

			if (m_fs_index < 0 || Files.Count == 0)
			{
				m_fs = null;
			}
			else if (m_fs_index >= Files.Count)
			{
				m_fs = new FileStream(Files[Files.Count-1].FullName, FileMode.Open, FileAccess.Read, FileShare, FileBufferSize, FileOptions);
				m_fs.Position = m_fs.Length;
			}
			else
			{
				m_fs = new FileStream(Files[m_fs_index].FullName, FileMode.Open, FileAccess.Read, FileShare, FileBufferSize, FileOptions);
				Position = Position;
			}
			return m_fs;
		}

		/// <summary>Returns the index of the file that contains byte offset 'Position'</summary>
		public int FileIndex
		{
			get { return FileIndexAtOffset(Position); }
		}

		/// <summary>Returns the index of the file that contains byte 'offset'</summary>
		public int FileIndexAtOffset(long offset)
		{
			var idx = m_boundary.BinarySearch(x => x.CompareTo(offset));
			return idx < 0 ? ~idx : idx + 1;
		}

		public override bool CanRead  { get { return true; } }
		public override bool CanSeek  { get { return true; } }
		public override bool CanWrite { get { return false; } }
		public override long Length   { get { return m_total_length; } }
		public override long Position
		{
			get { return m_position; }
			set
			{
				m_position = Math.Max(Math.Min(value, Length), 0);
				var fs = FileStream;
				if (fs != null) fs.Position = m_position - (m_fs_index > 0 ? m_boundary[m_fs_index - 1] : 0);
			}
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotImplementedException();
			//var fs = FileStream(FileIndex(offset));
			//return base.BeginRead(buffer, offset, count, callback, state);
		}
		public override int EndRead(IAsyncResult asyncResult)
		{
			throw new NotImplementedException();
			//return base.EndRead(asyncResult);
		}
		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
		{
			throw new NotSupportedException();
		}
		public override void EndWrite(IAsyncResult asyncResult)
		{
			throw new NotSupportedException();
		}

		/// <summary>Reads a block of bytes from the stream and writes the data into the given buffer</summary>
		/// <param name="buffer">The buffer to write read data into</param>
		/// <param name="offset">The offset into 'buffer' to start writing</param>
		/// <param name="count">The number of bytes to attempt to read from the stream</param>
		/// <returns>The number of bytes written to 'buffer'</returns>
		public override int Read(byte[] buffer, int offset, int count)
		{
			long initial_position = Position;
			while (count != 0)
			{
				var read = FileStream!.Read(buffer, offset, (int)Math.Min(count, m_boundary[FileIndex] - Position));
				Position += read;
				offset += read;
				count -= read;
				if (read == 0)
					break;
			}
			return (int)(Position - initial_position);
		}

		/// <summary>Reads a byte from the file and advances the read position one byte</summary>
		public override int ReadByte()
		{
			if (Position >= Length) return -1;
			var b = FileStream!.ReadByte();
			++Position;
			return b;
		}

		/// <summary>Sets the current position of this stream to the given value</summary>
		/// <param name="offset">The byte offset from 'origin'</param>
		/// <param name="origin">The reference point from which to seek</param>
		/// <returns>Returns the new position of the file stream</returns>
		public override long Seek(long offset, SeekOrigin origin)
		{
			switch (origin)
			{
			case SeekOrigin.Begin:   Position = offset;          break;
			case SeekOrigin.Current: Position += offset;         break;
			case SeekOrigin.End:     Position = Length - offset; break;
			}
			return Position;
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}
		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotSupportedException();
		}
		public override void WriteByte(byte value)
		{
			throw new NotSupportedException();
		}
		public override void Flush() {}

		/// <summary>Closes the stream and releases any resources</summary>
		public override void Close()
		{
			if (m_fs != null) m_fs.Close();
			m_fs = null;
			m_fs_index = -1;
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Text;
	using Streams;

	[TestFixture] public class TestAggregateFileStream
	{
		private readonly string file0 = Path.GetTempFileName();
		private readonly string file1 = Path.GetTempFileName();
		private readonly string file2 = Path.GetTempFileName();
		private readonly string fileN = Path.GetTempFileName();
		private string Content0 => "Line0\n";
		private string Content1 => "Line1\n";
		private string Content2 => "Line2\n";
		private string CombinedContent => Content0 + Content1 + Content2;
		private IEnumerable<string> Files => new[] { file0, file1, file2 };

		[TestFixtureSetUp] public void Setup()
		{
			File.WriteAllText(file0, Content0);
			File.WriteAllText(file1, Content1);
			File.WriteAllText(file2, Content2);
		}
		[TestFixtureTearDown] public void CleanUp()
		{
			File.Delete(file0);
			File.Delete(file1);
			File.Delete(file2);
			File.Delete(fileN);
		}
		[Test] public void TestGeneral()
		{
			using var f = new AggregateFileStream(Files);
			Assert.True(f.CanRead);
			Assert.True(f.CanSeek);
			Assert.False(f.CanWrite);
			Assert.Equal((long)CombinedContent.Length, f.Length);
		}
		[Test] public void TestFileIndex()
		{
			using var f = new AggregateFileStream(Files);
			Assert.Equal(0, f.FileIndexAtOffset(0));
			Assert.Equal(0, f.FileIndexAtOffset(Content0.Length - 1));
			Assert.Equal(1, f.FileIndexAtOffset(Content0.Length));
			Assert.Equal(1, f.FileIndexAtOffset(Content0.Length + Content1.Length - 1));
			Assert.Equal(2, f.FileIndexAtOffset(Content0.Length + Content1.Length));
			Assert.Equal(2, f.FileIndexAtOffset(Content0.Length + Content1.Length + Content2.Length - 1));
		}
		[Test] public void TestStream()
		{
			using (var f = new AggregateFileStream(Files))
			using (var s = new FileStream(fileN, FileMode.Create, FileAccess.Write, FileShare.Read))
				f.CopyTo(s);

			var combined = File.ReadAllText(fileN);
			Assert.Equal(CombinedContent, combined);
		}
		[Test] public void TestRead()
		{
			using var f = new AggregateFileStream(Files);
			var s = "abcdefghijklmnopqrstuvwxyz";
			var bytes = Encoding.UTF8.GetBytes(s);
			var offset = 5; // into 'bytes'
			int pos = 0;    // file pos

			Assert.Equal((long)pos, f.Position);
			var read = f.Read(bytes, offset, Content0.Length / 2);
			Assert.Equal(Content0.Length / 2, read);
			Assert.Equal((long)(pos + read), f.Position);
			Assert.Equal(s.Substring(0, offset) + CombinedContent.Substring(pos, read) + s.Substring(offset + read), Encoding.UTF8.GetString(bytes));

			s = Encoding.UTF8.GetString(bytes);

			pos = CombinedContent.Length / 3;
			f.Seek(pos, SeekOrigin.Begin);
			Assert.Equal((long)pos, f.Position);

			offset = 11;
			read = f.Read(bytes, offset, Content1.Length);
			Assert.Equal(Content1.Length, read);
			Assert.Equal((long)(pos + read), f.Position);
			Assert.Equal(s.Substring(0, offset) + CombinedContent.Substring(pos, read) + s.Substring(offset + read), Encoding.UTF8.GetString(bytes));
		}
		[Test] public void TestReadByte()
		{
			var bytes = new List<byte>();
			using (var f = new AggregateFileStream(Files))
			{
				for (;;)
				{
					var b = f.ReadByte();
					if (b == -1) break;
					bytes.Add((byte)b);
				}
			}
			Assert.Equal(CombinedContent, Encoding.UTF8.GetString(bytes.ToArray()));
		}
		[Test] public void TestPosition()
		{
			using var f = new AggregateFileStream(Files);
			var bytes = Encoding.UTF8.GetBytes(CombinedContent);
			var rnd = new Random(0);
			for (int i = 0; i != bytes.Length; ++i)
			{
				var pos = rnd.Next(bytes.Length);
				f.Position = pos;
				Assert.Equal((int)bytes[pos], f.ReadByte());
			}
		}
	}
}
#endif
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Rylogic.Maths;

namespace Rylogic.Script
{
	/// <summary>Base class for a source of script characters</summary>
	[DebuggerDisplay("{Description,nq}")]
	public abstract class Src :IDisposable
	{
		// Notes:
		//  - The source is exhausted when 'Peek' returns 0
		//  - This class supports local buffering.
		//  - Don't make 'Read' public, that would by-pass the local buffering.

		protected Src()
		{
			Buffer = new StringBuilder(256);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{}

		/// <summary>
		/// The current position within the source.
		/// Note: when buffering is used the might be past the current 'Peek' position</summary>
		public abstract Loc Location { get; }

		/// <summary>A local cache of characters read from the source</summary>
		public StringBuilder Buffer { get; }

		/// <summary>Returns the next character in the character stream without advancing the stream position</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public char Peek => this[0];

		/// <summary>Read ahead buffering</summary>
		public char this[int i]
		{
			get
			{
				ReadAhead(i + 1);
				return i < Buffer.Length ? Buffer[i] : '\0';
			}
		}

		/// <summary>
		/// Attempt to buffer 'n' characters locally. Less than 'n' characters can be buffered if EOF is hit.
		/// Returns the number of characters actually buffered (a value in [0, n])</summary>
		public int ReadAhead(int n)
		{
			for (; n > Buffer.Length;)
			{
				// Ensure 'Buffer's length grows with each loop
				var count = Buffer.Length;

				// Don't add '\0' to the buffer. 'TextBuffer' should never contain '\0' characters.
				// Read() returns 0 when the source is exhausted, or if the Buffer has been modified
				// directly. If the latter, we still need to check 'n' chars have been buffered.
				var ch = Read();
				if (ch != 0) { Buffer.Append(ch); continue; }
				if (Buffer.Length > count) continue;
				break;
			}
			return Math.Min(n, Buffer.Length);
		}

		/// <summary>
		/// Buffer until 'pred' returns false. Starts from src[start].
		/// Returns the number of characters buffered.</summary>
		public int ReadAhead(Func<char, bool> pred, int start = 0)
		{
			int i = start;
			for (; ; ++i)
			{
				var ch = this[i];
				if (ch == 0 || !pred(ch)) break;
			}
			return i;
		}

		/// <summary>Increment to the next character</summary>
		public void Next(int n = 1)
		{
			if (n < 0)
				throw new Exception("Cannot seek backwards");

			// Consume from the buffered characters first
			var remove = Math.Min(n, Buffer.Length);
			Buffer.Remove(0, remove);
			n -= remove;

			// Consume any remaining from the underlying source
			for (; n != 0 && Read() != 0; --n) {}
		}

		/// <summary>
		/// Return the next valid character from the underlying stream or '\0' for the end of stream.
		/// If you want to inject characters into the stream, modify 'Buffer' and return '\0' from Read().
		/// The stream is only considered empty when 'Buffer' is empty and Read returns 0.</summary>
		protected abstract char Read();

		/// <summary>Pointer-like interface</summary>
		public static implicit operator char(Src src) { return src.Peek; }
		public static Src operator ++(Src src) { src.Next(); return src; }
		public static Src operator +(Src src, int n) { src.Next(n); return src; }

		/// <summary>Reads all characters from the src and returns them as one string.</summary>
		public string ReadToEnd()
		{
			var sb = new StringBuilder(4096);
			for (char ch; (ch = Peek) != 0; Next()) sb.Append(ch);
			return sb.ToString();
		}

		///<summary>
		/// Reads a line. A line is defined as a sequence of characters followed by
		/// a carriage return ('\r'), a line feed ('\n'), or a carriage return
		/// immediately followed by a line feed. The resulting string does not
		/// contain the terminating carriage return and/or line feed. The returned
		/// value is null if the end of the input stream has been reached with no
		/// characters being read (not even the new lines characters).</summary>
		public string? ReadLine()
		{
			var sb = new StringBuilder(16);
			for (char ch; (ch = Peek) != 0; Next())
			{
				if (ch != '\r' && ch != '\n')
				{
					sb.Append(ch);
					continue;
				}

				Next();
				if (ch == '\r' && Peek == '\n') Next();
				return sb.ToString();
			}
			return sb.Length != 0 ? sb.ToString() : null;
		}

		/// <summary>String compare. Note: asymmetric, i.e. src="abcd", str="ab", src.Match(str) == true</summary>
		public bool Match(string str) => Match(str, str.Length);
		public bool Match(string str, int count)
		{
			ReadAhead(count);
			if (Buffer.Length < count)
				return false;

			int i = 0;
			for (; i != count && str[i] == Buffer[i]; ++i) { }
			return i == count;
		}

		/// <summary>Debugger description. Don't use Peek because that has side effects that make debugging confusing</summary>
		public virtual string Description => Buffer.ToString();
	}

	/// <summary>A script source that always returns 0</summary>
	public class NullSrc :Src
	{
		public override Loc Location => new Loc();
		protected override char Read() { return '\0'; }
		public override string Description => "NullSrc";
	}

	/// <summary>A script character sequence from a string</summary>
	public class StringSrc :Src
	{
		private readonly string m_str;
		private int m_position;

		public StringSrc(string str, int position = 0)
		{
			m_str = str;
			m_position = position;
			Location = new Loc();
		}
		public StringSrc(StringSrc rhs)
			: this(rhs.m_str, rhs.Position)
		{}

		/// <summary>The current position in the string</summary>
		public int Position
		{
			get => m_position;
			set => m_position = Math_.Clamp(value, 0, m_str.Length);
		}

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location { get; }

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override char Read()
		{
			var ch = m_position != m_str.Length ? m_str[m_position++] : '\0';
			return Location.inc(ch);
		}
	}

	/// <summary>A script character sequence from a file</summary>
	public class FileSrc :Src
	{
		private readonly string m_filepath;
		private StreamReader m_stream;

		public FileSrc(string filepath, long position = 0)
		{
			m_filepath = filepath;
			m_stream = new StreamReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
			m_stream.BaseStream.Position = position;
			Location = new Loc();
		}
		public FileSrc(FileSrc rhs)
			: this(rhs.m_filepath, rhs.m_stream.BaseStream.Position)
		{ }
		protected override void Dispose(bool _)
		{
			m_stream.Dispose();
			base.Dispose(_);
		}

		/// <summary>The current position in the file</summary>
		public long Position
		{
			get => m_stream.BaseStream.Position;
			set => m_stream.BaseStream.Position = value;
		}

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location { get; }

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override char Read()
		{
			var ch = m_stream.Read();
			if (ch == -1) return '\0';
			return Location.inc((char)ch);
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture]
	public partial class TestScript
	{
		[Test]
		public void NullSrc()
		{
			var src = new NullSrc();
			Assert.Equal('\0', src.Peek);
			src.Next();
			Assert.Equal('\0', src.Peek);
			src.Next(10);
			Assert.Equal('\0', src.Peek);
			src.ReadAhead(10);
			Assert.Equal('\0', src.Peek);
		}
		[Test]
		public void StringSrc()
		{
			const string str = "This is a stream of characters\n";
			var src = new StringSrc(str);
			for (int i = 0; i != str.Length; ++i, src.Next())
				Assert.Equal(str[i], src.Peek);
			Assert.Equal((char)0, src.Peek);
		}
		[Test]
		public void BufferingSrc()
		{
			const string str0 =
				"This is \n" +
				"a stream of \n" +
				"characters\n";

			var src = new StringSrc(str0);
			for (int i = 0; i != 5; ++i)
				Assert.Equal(str0[i], src[i]);
			for (int i = 0; i != 5; ++i, src.Next())
				Assert.Equal(str0[i], src.Peek);
			src.ReadAhead(5);
			for (int i = 0; i != 5; ++i)
				Assert.Equal(str0[i + 5], src[i]);
			for (int i = 5; i != str0.Length; ++i, src.Next())
				Assert.Equal(str0[i], src.Peek);
		}
		[Test]
		public void Match()
		{
			const string str0 = "1234567890";
			var src = new StringSrc(str0);

			Assert.Equal(true, src.Match(""));

			Assert.Equal(true, src.Match("1234"));
			Assert.Equal(true, src.Match("12"));
			Assert.Equal(false, src.Match("1235"));

			src.Next();
			Assert.Equal(true, src.Match("234567"));
			Assert.Equal(true, src.Match("2"));
			Assert.Equal(false, src.Match("123"));

			src.Next(4);
			Assert.Equal(true, src.Match("67"));
			Assert.Equal(true, src.Match("67890"));
			Assert.Equal(false, src.Match("678901"));
		}
	}
}
#endif

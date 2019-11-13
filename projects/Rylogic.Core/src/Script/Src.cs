using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Rylogic.Maths;
using Rylogic.Str;

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
		//  - The 'Read()' method returns int and EOS to indicate end of stream.
		//    'Peek' however returns characters and returns '\0' to indicate end of stream.
		//    (This is for consistency with the native implementation)
		//  - The C# implementation does not need 'EOS' because the character sources always
		//    return decoded characters from 'Read()'

		protected Src(Loc location)
		{
			m_src = this;
			m_loc = location;
			Buffer = new StringBuilder(256);
		}
		protected Src(Src wrapped)
		{
			m_src = wrapped;
			m_loc = new Loc();
			Buffer = new StringBuilder(256);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{
			if (m_src != this)
				m_src.Dispose();
		}

		/// <summary>A reference to a wrapped source (or this)</summary>
		protected Src m_src;

		/// <summary>The current position in the root source.</summary>
		public Loc Location
		{
			get
			{
				var s = this;
				for (; s != s.m_src; s = s.m_src) { }
				return s.m_loc;
			}
		}
		private Loc m_loc;

		/// <summary>A local cache of characters read from the source</summary>
		public StringBuilder Buffer { get; }

		/// <summary>
		/// Returns the next character in the character stream without advancing the stream position.
		/// '\0' indicates the end of the stream.</summary>
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

		/// <summary>Increment to the next character</summary>
		public void Next(int n = 1)
		{
			if (n < 0)
				throw new Exception("Cannot seek backwards");

			for (; ; )
			{
				// Consume from the buffered characters first
				var remove = Math.Min(n, Buffer.Length);
				for (int i = 0; i != remove; ++i)
				{
					m_loc.inc(Buffer[i]);
				}

				Buffer.Remove(0, remove);
				n -= remove;
				if (n == 0)
					break;

				// Buffer and dump, since we need to read whole characters from the underlying source
				if (ReadAhead(Math.Min(n, 4096)) == 0)
					break;
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

				// Read the next complete character from the underlying stream
				var ch = Read();

				// Buffer the read character
				if (ch != '\0')
				{
					Buffer.Append((char)ch);
					continue;
				}

				// Stop reading if the buffer isn't growing
				if (Buffer.Length <= count)
					break;
			}
			return Math.Min(n, Buffer.Length);
		}

		/// <summary>
		/// Return the next valid character from the underlying stream or 'EOS' for the end of stream.
		/// The stream is only considered empty when 'Buffer' is empty and Read returns 0.</summary>
		protected abstract int Read();

		/// <summary>Pointer-like interface</summary>
		public static implicit operator char(Src src) { return src.Peek; }
		public static Src operator ++(Src src) { src.Next(); return src; }
		public static Src operator +(Src src, int n) { src.Next(n); return src; }

		/// <summary>String compare. Note: asymmetric, i.e. src="abcd", str="ab", src.Match(str) == true</summary>
		public bool Match(string str) => Match(str, 0);
		public bool Match(string str, int start) => Match(str, start, str.Length);
		public bool Match(string str, int start, int count)
		{
			ReadAhead(start + count);
			if (Buffer.Length < start + count)
				return false;

			int i = start, iend = start + count;
			for (; i != iend && str[i] == Buffer[i]; ++i) { }
			return i == iend;
		}

		/// <summary>Read 'count' characters from the string and return them as a string</summary>
		public string Read(int count)
		{
			var sb = new StringBuilder(count); int i = 0;
			for (char ch; i != count && (ch = Peek) != '\0'; ++i, Next()) sb.Append(ch);
			if (i != count) throw new ScriptException(EResult.UnexpectedEndOfFile, Location, $"Could not read {count} characters. End of stream reached");
			return sb.ToString();
		}

		/// <summary>Reads all characters from the src and returns them as one string.</summary>
		public string ReadToEnd()
		{
			var sb = new StringBuilder(4096);
			for (char ch; (ch = Peek) != '\0'; Next()) sb.Append(ch);
			return sb.ToString();
		}

		///<summary>
		/// Reads a line. A line is defined as a sequence of characters followed by a carriage return ('\r'),
		/// a line feed ('\n'), or a carriage return immediately followed by a line feed. The resulting string
		/// does not contain the terminating carriage return and/or line feed.</summary>
		public string ReadLine()
		{
			var sb = new StringBuilder(256);
			for (char ch; (ch = Peek) != '\0'; Next())
			{
				if (ch != '\r' && ch != '\n')
				{
					sb.Append(ch);
					continue;
				}

				Next();
				if (ch == '\r' && Peek == '\n') Next();
				break;
			}
			return sb.ToString();
		}

		/// <summary>Debugger description. Don't use Peek because that has side effects that make debugging confusing</summary>
		public virtual string Description => Buffer.ToString();
	}

	/// <summary>A script source that always returns 0</summary>
	public class NullSrc :Src
	{
		public NullSrc() :base(new Loc()) { }
		protected override int Read() { return '\0'; }
		public override string Description => "NullSrc";
	}

	/// <summary>A script character sequence from a string</summary>
	public class StringSrc :Src, IStringView
	{
		// Notes:
		//  - Don't add copy constructors, the 'rhs' source may have buffered text
		//    which will not be buffered in the copy. Instead create new instances.
		private readonly IString m_str;
		private readonly long m_end;
		private long m_position;

		public StringSrc(IString str, long? start = null, long? count = null, Loc? loc = null)
			:base(loc ?? new Loc(string.Empty, start ?? 0, 1, 1))
		{
			start ??= 0;
			count ??= str.Length - start.Value;

			if (start < 0 || start > str.Length)
				throw new ArgumentOutOfRangeException(nameof(start), "String source offset is out of range");
			if (count < 0 || start + count > str.Length)
				throw new ArgumentOutOfRangeException(nameof(count), "String source count is out of range");

			m_str = str;
			m_end = (start + count).Value;
			m_position = start.Value;
		}

		/// <summary>The current position in the string</summary>
		public long Position
		{
			get => m_position;
			set => m_position = Math_.Clamp(value, 0, m_end);
		}

		/// <summary>The remaining characters in this string source</summary>
		public int Length => (int)(m_end - m_position);

		/// <summary>Return the next valid character from the underlying stream or 'EOS' for the end of stream.</summary>
		protected override int Read()
		{
			return m_position != m_end ? m_str[(int)m_position++] : '\0';
		}
	}

	/// <summary>A script character sequence from a file</summary>
	public class FileSrc :Src
	{
		// Notes:
		//  - Don't add copy constructors, the 'rhs' source may have buffered text
		//    which will not be buffered in the copy. Instead create new instances.

		private StreamReader m_stream;
		public FileSrc(string filepath, long position = 0, int line = 1, int column = 1)
			:base(new Loc(filepath, position, line, column))
		{
			Filepath = filepath;
			m_stream = new StreamReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read));
			m_stream.BaseStream.Position = position;
		}
		protected override void Dispose(bool _)
		{
			m_stream.Dispose();
			base.Dispose(_);
		}

		/// <summary>The file path of the source</summary>
		public string Filepath { get; }

		/// <summary>The current position in the file</summary>
		public long Position
		{
			get => m_stream.BaseStream.Position;
			set => m_stream.BaseStream.Position = value;
		}

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override int Read()
		{
			var ch = m_stream.Read();
			if (ch == -1) return '\0';
			return ch;
		}
	}

	/// <summary>A wrapped source with truncated length</summary>
	public class WrapSrc :Src
	{
		// Notes:
		//  - This source type is useful for creating a subrange of an existing source
		
		private long m_count;

		public WrapSrc(Src src, long count = long.MaxValue)
			:base(src)
		{
			m_count = count;
		}

		protected override int Read()
		{
			if (m_count == 0)
				return '\0';

			var ch = m_src.Peek;
			m_src.Next();
			--m_count;
			return ch;
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

using System;
using System.IO;
using System.Text;

namespace Rylogic.Script
{
	/// <summary>Base class for a source of script characters</summary>
	public abstract class Src :IDisposable
	{
		/// <summary>Clean up resources</summary>
		protected Src()
		{
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool _)
		{}

		/// <summary>A cached copy of the last character read from 'Peek'. Only used internally</summary>
		private char m_char;

		/// <summary>The type of source this is</summary>
		public abstract SrcType SrcType { get; }

		/// <summary>The 'file position' within the source</summary>
		public abstract Loc Location { get; }

		/// <summary>Returns the next character in the character stream without advancing the stream position</summary>
		public char Peek
		{
			get
			{
				var ch = PeekInternal();
				if (ch != m_char) { Advance(0); m_char = PeekInternal(); }
				return m_char;
			}
		}

		/// <summary>Increment to the next character</summary>
		public void Next(int n = 1)
		{
			if (Peek == 0) return;
			Advance(n);
		}

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
				if (ch == '\r' || ch == '\n')
				{
					Next();
					if (ch == '\r' && Peek == '\n') Next();
					return sb.ToString();
				}
				sb.Append(ch);
			}
			return sb.Length != 0 ? sb.ToString() : null;
		}

		/// <summary>Returns the character at the current source position or 0 when the source is exhausted</summary>
		protected abstract char PeekInternal();

		/// <summary>
		/// Advances the internal position a minimum of 'n' positions.
		/// If the character at the new position is not a valid character to be return keep
		/// advancing to the next valid character</summary>
		protected abstract void Advance(int n);

		public override string ToString() => Peek.ToString();
	}

	/// <summary>A script character sequence from a text reader</summary>
	public class TextReaderSrc :Src
	{
		private readonly TextReader m_reader;
		private readonly Loc m_loc = new Loc();

		public TextReaderSrc(TextReader reader)
		{
			m_reader = reader;
		}
		protected override void Dispose(bool _)
		{
			m_reader.Dispose();
			base.Dispose(_);
		}

		/// <summary>The type of source this is</summary>
		public override SrcType SrcType { get { return SrcType.String; } }

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location { get { return m_loc; } }

		/// <summary>Returns the character at the current source position or 0 when the source is exhausted</summary>
		protected override char PeekInternal()
		{
			int ch = m_reader.Peek();
			return ch != -1 ? (char)ch : (char)0;
		}

		/// <summary>
		/// Advances the internal position a minimum of 'n' positions.
		/// If the character at the new position is not a valid character to be return keep
		/// advancing to the next valid character</summary>
		protected override void Advance(int n)
		{
			for (;n-- != 0;)
			{
				int ch = m_reader.Read();
				m_loc.inc((char)ch);
			}
		}

		public override string ToString() { return Peek.ToString(); }
	}

	/// <summary>A script character sequence from a string</summary>
	public class StringSrc :TextReaderSrc
	{
		public StringSrc(string str)
			:base(new StringReader(str))
		{}
	}

	/// <summary>A script character sequence from a file</summary>
	public class FileSrc :TextReaderSrc
	{
		public FileSrc(string filepath)
			:base(new StreamReader(filepath))
		{}
	}

	/// <summary>A script character source that inserts indenting on new lines</summary>
	public class IndentSrc :Src
	{
		private readonly Buffer m_buf;
		private readonly string m_indent;

		public IndentSrc(Src src, string indent, bool indent_first = true, string line_end = "\n")
		{
			m_buf = new Buffer(src);
			m_indent = indent;
			LineEnd = line_end;
			if (indent_first)
				m_buf.TextBuffer.Append(m_indent);
		}
		protected override void Dispose(bool _)
		{
			m_buf.Dispose();
			base.Dispose(_);
		}

		public string LineEnd { get; private set; }

		/// <summary>The type of source this is</summary>
		public override SrcType SrcType => m_buf.SrcType;

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location => m_buf.Location;

		/// <summary>Returns the character at the current source position or 0 when the source is exhausted</summary>
		protected override char PeekInternal()
		{
			return m_buf.Peek;
		}

		/// <summary>
		/// Advances the internal position a minimum of 'n' positions.
		/// If the character at the new position is not a valid character to be return keep
		/// advancing to the next valid character</summary>
		protected override void Advance(int n)
		{
			for (; n-- != 0;)
			{
				m_buf.Next();
				if (m_buf.Match(LineEnd) && m_buf.Src.Peek != 0)
					m_buf.TextBuffer.Append(m_indent);
			}
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public partial class TestScript
	{
		[Test] public void StringSrc()
		{
			const string str = "This is a stream of characters\n";
			var src = new StringSrc(str);
			for (int i = 0; i != str.Length; ++i, src.Next())
				Assert.Equal(str[i], src.Peek);
			Assert.Equal((char)0, src.Peek);
		}
		[Test] public void IndentSrc()
		{
			const string str_in =
				"This is \n" +
				"a stream of \n" +
				"characters\n";
			const string str_out =
				"indent This is \n" +
				"indent a stream of \n" +
				"indent characters\n";

			var src = new IndentSrc(new StringSrc(str_in), "indent ");
			for (int i = 0; i != str_out.Length; ++i, src.Next())
				Assert.Equal(str_out[i], src.Peek);
			Assert.Equal((char)0, src.Peek);
		}
	}
}
#endif

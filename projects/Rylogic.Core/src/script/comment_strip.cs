using System.Diagnostics;

namespace Rylogic.Script
{
	/// <summary>Strips comments from a character stream</summary>
	public class CommentStrip :Src
	{
		private readonly Buffer m_buf;

		public CommentStrip(Src src, string line_end = "\n", string line_comment = "//", string block_start = "/*", string block_end = "*/")
		{
			m_buf = new Buffer(src);
			LineEnd           = line_end ?? string.Empty;
			LineComment       = line_comment ?? string.Empty;
			BlockCommentStart = block_start ?? string.Empty;
			BlockCommentEnd   = block_end ?? string.Empty;
		}
		public override void Dispose()
		{
			m_buf.Dispose();
		}

		// These code only works if line and block comments both start with the same initial character
		public string LineEnd { get; private set; }
		public string LineComment { get; private set; }
		public string BlockCommentStart { get; private set; }
		public string BlockCommentEnd { get; private set; }

		/// <summary>The type of source this is</summary>
		public override SrcType SrcType { get { return m_buf.SrcType; } }

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location { get { return m_buf.Location; } }

		/// <summary>Returns the character at the current source position or 0 when the source is exhausted</summary>
		protected override char PeekInternal() { return m_buf.Peek; }

		/// <summary>
		/// Advances the internal position a minimum of 'n' positions.
		/// If the character at the new position is not a valid character to be return keep
		/// advancing to the next valid character</summary>
		protected override void Advance(int n)
		{
			for (;;)
			{
				// If there are characters buffered, then they're ones we need to return
				if (m_buf.Empty)
				{
					// Read through literal strings
					if (m_buf.Peek == '\"')
					{
						BufferLiteralString();
						continue;
					}

					// Read through literal characters
					if (m_buf.Peek == '\'')
					{
						BufferLiteralChar();
						continue;
					}

					// Skip comments
					// Check that the next character in the stream matches the first character in the comment sequence
					// before adding characters to the buffer. This only works if line and block comments start with the same character

					if (LineComment.Length != 0 && m_buf.Peek == LineComment[0] && m_buf.Match(LineComment))
					{
						EatLineComment();
						continue;
					}
					if (BlockCommentStart.Length != 0 && m_buf.Peek == BlockCommentStart[0] && m_buf.Match(BlockCommentStart))
					{
						EatBlockComment();
						continue;
					}
				}

				if (n-- == 0) break;
				m_buf.Next();
			}
		}

		private void BufferLiteralString()
		{
			m_buf.Cache(); Debug.Assert(m_buf[0] == '\"');
			for (bool esc = true; m_buf.Src.Peek != 0 && (m_buf.Src.Peek != '\"' || esc); esc = (m_buf.Src.Peek == '\\'), m_buf.Cache(m_buf.Length+1)) {}
			if (m_buf.Src.Peek != 0) m_buf.Cache(m_buf.Length+1);
		}
		private void BufferLiteralChar()
		{
			m_buf.Cache(); Debug.Assert(m_buf[0] == '\'');
			for (bool esc = true; m_buf.Src.Peek != 0 && (m_buf.Src.Peek != '\'' || esc); esc = (m_buf.Src.Peek == '\\'), m_buf.Cache(m_buf.Length+1)) {}
			if (m_buf.Src.Peek != 0) m_buf.Cache(m_buf.Length+1);
		}
		private void EatLineComment()
		{
			for (; m_buf.Peek != 0 && !m_buf.Match(LineEnd); m_buf.Next()) {}
		}
		private void EatBlockComment()
		{
			for (; m_buf.Peek != 0 && !m_buf.Match(BlockCommentEnd); m_buf.Next()) {}
			m_buf.Clear();
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public partial class TestScript
	{
		[Test] public void StripCppComments()
		{
			const string str_in =
				"123// comment         \n"+
				"456/* block */789     \n"+
				"// many               \n"+
				"// lines              \n"+
				"// \"string\"         \n"+
				"/* \"string\" */      \n"+
				"\"string \\\" /*a*/ //b\"  \n"+
				"/not a comment\n"+
				"/*\n"+
				"  more lines\n"+
				"*/\n"+
				"/*back to*//*back*/ comment\n";
			const string str_out =
				"123\n"+
				"456789     \n"+
				"\n"+
				"\n"+
				"\n"+
				"      \n"+
				"\"string \\\" /*a*/ //b\"  \n"+
				"/not a comment\n"+
				"\n"+
				" comment\n";

			var src = new StringSrc(str_in);
			var strip = new CommentStrip(src);
			var result = strip.ReadToEnd();
			Assert.Equal(str_out, result);
			Assert.Equal((char)0, strip.Peek);
		}
		[Test] public void StripAsmComments()
		{
			const string str_in =
				"; asm comments start with a ; character\r\n"+
				"mov 43 2\r\n"+
				"ldr $a 2 ; imaginary asm";
			const string str_out =
				"\r\n"+
				"mov 43 2\r\n"+
				"ldr $a 2 ";

			var src = new StringSrc(str_in);
			var strip = new CommentStrip(src, "\r\n", ";", null, null);
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
				Assert.Equal(str_out[i], strip.Peek);
			Assert.Equal((char)0, strip.Peek);
		}
	}
}
#endif

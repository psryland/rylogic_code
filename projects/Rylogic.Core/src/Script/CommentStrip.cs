namespace Rylogic.Script
{
	/// <summary>Strips comments from a character stream</summary>
	public class CommentStrip :Src
	{
		public CommentStrip(Src src, string? line_comment = "//", string? block_beg = "/*", string? block_end = "*/")
			:base(src)
		{
			LineComment = line_comment ?? string.Empty;
			BlockBeg = block_beg ?? string.Empty;
			BlockEnd = block_end ?? string.Empty;
			m_literal_string = new InLiteral();
		}

		/// <summary>Comment patterns</summary>
		public string LineComment { get; }
		public string BlockBeg { get; }
		public string BlockEnd { get; }

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override int Read()
		{
			for (; ; )
			{
				// Read through literal strings or characters
				if (m_literal_string.WithinLiteralString(m_src))
					break;

				// Skip comments
				if (LineComment.Length != 0 && m_src == LineComment[0] && m_src.Match(LineComment))
				{
					Extract.EatLineComment(m_src, LineComment);
					continue;
				}
				if (BlockBeg.Length != 0 && BlockEnd.Length != 0 && m_src == BlockBeg[0] && m_src.Match(BlockBeg))
				{
					Extract.EatBlockComment(m_src, BlockBeg, BlockEnd);
					continue;
				}

				break;
			}

			var ch = m_src.Peek;
			if (ch != '\0') m_src.Next();
			return ch;
		}
		private InLiteral m_literal_string;
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
		public void StripCppComments()
		{
			const string str_in =
				"123// comment         \n" +
				"456/* block */789     \n" +
				"// many               \n" +
				"// lines              \n" +
				"// \"string\"         \n" +
				"/* \"string\" */      \n" +
				"\"string \\\" /*a*/ //b\"  \n" +
				"/not a comment\n" +
				"/*\n" +
				"  more lines\n" +
				"*/\n" +
				"/*back to*//*back*/ comment\n";
			const string str_out =
				"123\n" +
				"456789     \n" +
				"\n" +
				"\n" +
				"\n" +
				"      \n" +
				"\"string \\\" /*a*/ //b\"  \n" +
				"/not a comment\n" +
				"\n" +
				" comment\n";

			var src = new StringSrc(str_in);
			var strip = new CommentStrip(src);
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
				Assert.Equal(str_out[i], strip.Peek);
			Assert.Equal('\0', strip.Peek);
		}
		[Test]
		public void StripAsmComments()
		{
			const string str_in =
				"; asm comments start with a ; character\r\n" +
				"mov 43 2\r\n" +
				"ldr $a 2 ; imaginary asm";
			const string str_out =
				"\r\n" +
				"mov 43 2\r\n" +
				"ldr $a 2 ";

			var src = new StringSrc(str_in);
			var strip = new CommentStrip(src, ";", null, null);
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
				Assert.Equal(str_out[i], strip.Peek);
			Assert.Equal('\0', strip.Peek);
		}
	}
}
#endif

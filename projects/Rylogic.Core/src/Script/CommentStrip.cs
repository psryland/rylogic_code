namespace Rylogic.Script
{
	/// <summary>Strips comments from a character stream</summary>
	public class CommentStrip :Src
	{
		public CommentStrip(Src src, string? line_end = "\n", string? line_comment = "//", string? block_beg = "/*", string? block_end = "*/")
		{
			Src = src;
			LineEnd = line_end ?? string.Empty;
			LineComment = line_comment ?? string.Empty;
			BlockCommentBeg = block_beg ?? string.Empty;
			BlockCommentEnd = block_end ?? string.Empty;
			m_literal_string = new InLiteralString();
		}
		protected override void Dispose(bool _)
		{
			Src.Dispose();
			base.Dispose(_);
		}

		/// <summary>The input stream</summary>
		private Src Src { get; }

		// These code only works if line and block comments both start with the same initial character
		public string LineEnd { get; }
		public string LineComment { get; }
		public string BlockCommentBeg { get; }
		public string BlockCommentEnd { get; }

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location => Src.Location;

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override char Read()
		{
			for (; ; )
			{
				// Read through literal strings or characters
				if (m_literal_string.WithinLiteralString(Src))
					break;

				// Skip comments
				if (LineComment.Length != 0 && Src == LineComment[0] && Src.Match(LineComment))
				{
					Extract.EatLineComment(Src, LineComment);
					continue;
				}
				if (BlockCommentBeg.Length != 0 && Src == BlockCommentBeg[0] && Src.Match(BlockCommentBeg))
				{
					Extract.EatBlockComment(Src, BlockCommentBeg, BlockCommentEnd);
					continue;
				}

				break;
			}

			var ch = Src.Peek;
			if (ch != 0) Src.Next();
			return ch;
		}
		private InLiteralString m_literal_string;
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
			Assert.Equal((char)0, strip.Peek);
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
			var strip = new CommentStrip(src, "\r\n", ";", null, null);
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
				Assert.Equal(str_out[i], strip.Peek);
			Assert.Equal((char)0, strip.Peek);
		}
	}
}
#endif

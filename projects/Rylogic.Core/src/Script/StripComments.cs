using Rylogic.Str;

namespace Rylogic.Script
{
	/// <summary>Strips comments from a character stream</summary>
	public class StripComments :Src
	{
		private InComment m_com;

		public StripComments(Src src, InLiteral.EFlags literal_flags = InLiteral.EFlags.Escaped | InLiteral.EFlags.SingleLineStrings, InComment.Patterns? comment_patterns = null)
			:base(src)
		{
			m_com = new InComment(comment_patterns ?? new InComment.Patterns(), literal_flags);
		}

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override int Read()
		{
			for (; m_src != '\0'; ++m_src)
			{
				// Eat comments
				var len = m_src.ReadAhead(8);
				if (m_com.WithinComment(m_src.Buffer.ToString(0,len), 0))
					continue;

				break;
			}

			var ch = m_src.Peek;
			if (ch != '\0') m_src.Next();
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
			var strip = new StripComments(src);
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
			{
				if (str_out[i] == strip.Peek) continue;
				Assert.Equal(str_out[i], strip.Peek);
			}
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
			var strip = new StripComments(src, comment_patterns:new InComment.Patterns(";", "\r\n", "", ""));
			for (int i = 0; i != str_out.Length; ++i, strip.Next())
			{
				if (str_out[i] == strip.Peek) continue;
				Assert.Equal(str_out[i], strip.Peek);
			}
			Assert.Equal('\0', strip.Peek);
		}
	}
}
#endif

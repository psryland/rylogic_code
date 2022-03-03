using System;

namespace Rylogic.Str
{
	/// <summary>A helper class for recognising line or block comments in a stream of characters.</summary>
	public class InComment
	{
		public enum EType { None = 0, Line = 1, Block = 2 };
		private const char LineContinuation = '\\';

		private Patterns m_pat;
		private InLiteral m_lit;
		private EType m_comment;
		private bool m_escape;
		private int m_emit;

		public InComment(Patterns? pat = null, InLiteral.EFlags literal_flags = InLiteral.EFlags.Escaped | InLiteral.EFlags.SingleLineStrings)
		{
			m_pat = pat ?? new Patterns();
			m_lit = new InLiteral(literal_flags);
			m_comment = EType.None;
			m_escape = false;
			m_emit = 0;
		}

		/// <summary>True if the current state is "within comment"</summary>
		public bool IsWithinComment { get; private set; }

		/// <summary>
		/// Processes the 'ith' character in 'src'.
		/// Returns true if currently within a string/character literal</summary>
		public bool WithinComment(IString src, int i) => WithinComment((IStringView)src, i);
		public bool WithinComment(IStringView src, int i)
		{
			// This function requires 'src' because we need to look ahead
			// to say whether we're in a comment.

			switch (m_comment)
			{
			case EType.None:
				{
					if (m_lit.WithinLiteral(src[i]))
					{
					}
					else if (m_emit == 0 && m_pat.m_line_comment.Length != 0 && Match(src, i, m_pat.m_line_comment))
					{
						m_comment = EType.Line;
						m_emit = m_pat.m_line_comment.Length;
						m_escape = false;
					}
					else if (m_emit == 0 && m_pat.m_block_beg.Length != 0 && Match(src, i, m_pat.m_block_beg))
					{
						m_comment = EType.Block;
						m_emit = m_pat.m_block_beg.Length;
					}
					break;
				}
			case EType.Line:
				{
					if (src[i] == '\0')
					{
						m_comment = EType.None;
						m_emit = 0;
					}
					else if (m_emit == 0 && !m_escape && Match(src, i, m_pat.m_line_end))
					{
						m_comment = EType.None;
						m_emit = 0; // line comments don't include the line end
					}
					m_escape = src[i] == LineContinuation;
					break;
				}
			case EType.Block:
				{
					if (src[i] == '\0')
					{
						m_comment = EType.Block;
						m_emit = 0;
					}
					else if (m_emit == 0 && Match(src, i, m_pat.m_block_end))
					{
						m_comment = EType.None;
						m_emit = m_pat.m_block_end.Length;
					}
					break;
				}
			default:
				{
					throw new Exception("Unknown comment state");
				}
			}

			IsWithinComment = m_comment != EType.None || m_emit != 0;
			m_emit -= m_emit != 0 ? 1 : 0;
			return IsWithinComment;
		}

		// True if 'src' starts with 'pattern'
		private static bool Match(IStringView src, int start, string pattern)
		{
			int i = 0, iend = pattern.Length;
			if (start + pattern.Length > src.Length) return false;
			for (; i != iend && src[start + i] == pattern[i]; ++i) { }
			return i == iend;
		}

		/// <summary></summary>
		public class Patterns
		{
			public Patterns(
				string line_comment = "//",
				string line_end = "\n",
				string block_beg = "/*",
				string block_end = "*/")
			{
				if ((line_comment.Length == 0) != (line_end.Length == 0))
					throw new Exception("If line comments are detected, both start and end markers are required");
				if ((block_beg.Length == 0) != (block_end.Length == 0))
					throw new Exception("If block comments are detected, both start and end markers are required");

				m_line_comment = line_comment;
				m_line_end = line_end;
				m_block_beg = block_beg;
				m_block_end = block_end;
			}

			public string m_line_comment;
			public string m_line_end;
			public string m_block_beg;
			public string m_block_end;
		}
	}
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Str;

	[TestFixture]
	public partial class TestInComment
	{
		[Test]
		public void SimpleBlockComment()
		{
			// Simple block comment
			string src = " /**/ ";
			int[] exp = { 0, 1, 1, 1, 1, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
		[Test]
		public void NoSubstringMatchingWithinBlockCommentMarkers()
		{
			// No substring matching within block comment markers
			string src= "/*/*/ /**/*/";
			int[] exp= { 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
		[Test]
		public void LineCommentEndsAtUnescapedNewLine()
		{
			// Line comment ends at unescaped new line (exclusive)
			string src = " // \\\n \n ";
			int[] exp = { 0, 1, 1, 1, 1, 1, 1, 0, 0, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
		[Test]
		public void LineCommentEndsAsEoS()
		{
			// Line comment ends at EOS
			string src = " // ";
			int[] exp = { 0, 1, 1, 1, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
		[Test]
		public void NoCommentsWithinLiteralStrings()
		{
			// No comments within literal strings
			string src = " \"// /* */\" ";
			int[] exp = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
		[Test]
		public void IgnoreLiteralStringsWithinComments()
		{
			// Ignore literal strings within comments
			string src = " /* \" */ // \" \n ";
			int[] exp = { 0, 1, 1, 1, 1, 1, 1, 1, 0, 1, 1, 1, 1, 1, 0, 0 };

			var com = new InComment();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], com.WithinComment(src, i) ? 1 : 0);
		}
	}
}
#endif
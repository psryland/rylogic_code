using System;

namespace Rylogic.Str
{
	/// <summary>A helper class for recognising literal strings in a stream of characters.</summary>
	public class InLiteral
	{
		// Notes:
		//  - Literal strings are closed automatically by newline characters. Higher level
		//    logic handles the unmatched quote character. This is needed for parsing inactive
		//    code blocks in the preprocessor, which ignores unclosed literal strings/characters.
		//  - Escape sequences don't have to be for single characters (i.e. unicode sequences) but
		//    that doesn't matter here because we only care about escaped quote characters.

		private readonly EFlags m_flags;
		private readonly char m_escape_character;
		private char m_quote_character;
		private bool m_in_literal_state;
		private bool m_in_literal;
		private bool m_escape;

		public InLiteral(EFlags flags = EFlags.Escaped, char escape_character = '\\')
		{
			m_flags = flags;
			m_escape_character = escape_character;
			m_quote_character = '\0';
			m_in_literal_state = false;
			m_in_literal = false;
			m_escape = false;
		}

		/// <summary>True if the last reported state was 'within literal'</summary>
		public bool IsWithinLiteral => m_in_literal;

		/// <summary>True while within an escape sequence</summary>
		public bool IsWithinEscape => m_escape;

		/// <summary>
		/// Consider the next character in the stream 'ch'.
		/// Returns true if currently within a string/character literal.</summary>
		public bool WithinLiteral(char ch)
		{
			if (m_in_literal_state)
			{
				if (m_escape)
				{
					// If escaped, then still within the literal
					m_escape = false;
					return m_in_literal = true;
				}
				else if (ch == m_quote_character)
				{
					m_in_literal_state = false;
					return m_in_literal = !m_flags.HasFlag(EFlags.ExcludeQuotes); // terminating quote can be part of the literal
				}
				else if (ch == '\n' && m_flags.HasFlag(EFlags.SingleLineStrings))
				{
					m_in_literal_state = false;
					return m_in_literal = false; // terminating '\n' is not part of the literal
				}
				else
				{
					m_escape = (ch == m_escape_character) && m_flags.HasFlag(EFlags.Escaped);
					return m_in_literal = true;
				}
			}
			else if (ch == '\"' || ch == '\'')
			{
				m_quote_character = ch;
				m_in_literal_state = true;
				m_escape = false;
				return m_in_literal = !m_flags.HasFlag(EFlags.ExcludeQuotes); // first quote can be part of the literal
			}
			else
			{
				return m_in_literal = false;
			}
		}

		/// <summary>Flags for controlling the behaviour of the InLiteral class</summary>
		[Flags]
		public enum EFlags
		{
			None = 0,

			// Expected escape sequences in the string
			Escaped = 1 << 0,

			// 'WithinLiteralString' returns false for the initial and final quote characters
			ExcludeQuotes = 1 << 1,

			// New line characters end literal strings
			SingleLineStrings = 1 << 2,
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Str;

	[TestFixture]
	public partial class TestInLiteral
	{
		[Test]
		public void EscapedQuotesIgnored()
		{
			// Escaped quotes are ignored
			string src = " \"\\\"\" ";
			int[] exp = { 0, 1, 1, 1, 1, 0 };

			var lit = new InLiteral();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], lit.WithinLiteral(src[i]) ? 1 : 0);
		}
		[Test]
		public void ExcludeQuotes()
		{
			// Escape sequences are not always 1 character, but it doesn't
			// matter because we only care about escaped quotes.
			string src = " \"\\xB1\" ";
			int[] exp = { 0, 0, 1, 1, 1, 1, 0, 0 };

			// Don't include the quotes
			var lit = new InLiteral(InLiteral.EFlags.Escaped | InLiteral.EFlags.ExcludeQuotes);
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], lit.WithinLiteral(src[i]) ? 1 : 0);
		}
		[Test]
		public void MatchSameQuoteMark()
		{
			// Literals must match " to " and ' to '
			string src = "\"'\" '\"' ";
			int[] exp = { 1, 1, 1, 0, 1, 1, 1, 0 };

			var lit = new InLiteral();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], lit.WithinLiteral(src[i]) ? 1 : 0);
		}
		[Test]
		public void ClosedByNewLine()
		{
			// Literals *are* closed by '\n'
			string src = "\" '\n ";
			int[] exp = { 1, 1, 1, 0, 0 };

			var lit = new InLiteral(InLiteral.EFlags.Escaped | InLiteral.EFlags.SingleLineStrings);
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], lit.WithinLiteral(src[i]) ? 1 : 0);
		}
		[Test]
		public void NotClosedByEoS()
		{
			// Literals are not closed by EOS
			string src = "\" ";
			int[] exp = { 1, 1 };

			var lit = new InLiteral();
			for (int i = 0; i != src.Length; ++i)
				Assert.Equal(exp[i], lit.WithinLiteral(src[i]) ? 1 : 0);

			Assert.Equal(1, lit.WithinLiteral('\0') ? 1 : 0);
		}
	}
}
#endif

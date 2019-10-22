namespace Rylogic.Script
{
	public class InLiteralString
	{
		// Notes:
		//  - A helper class for recognising literal strings in a stream of characters.

		private readonly char m_escape_character;
		private bool m_in_literal_string;
		private bool m_escape;

		public InLiteralString(char escape_character = '\\')
		{
			m_escape_character = escape_character;
		}

		/// <summary>Returns true if currently in a literal string</summary>
		public bool WithinLiteralString(char ch, Loc? loc = null)
		{
			// Read through literal strings or characters
			if (m_in_literal_string)
			{
				if (ch == 0) throw new ScriptException(EResult.InvalidString, loc ?? new Loc(), "Incomplete literal string");
				m_in_literal_string = m_escape || !(ch == '\"' || ch == '\'');
				m_escape = ch == m_escape_character;
				return true;
			}
			if (ch == '\"' || ch == '\'')
			{
				m_in_literal_string = true;
				m_escape = false;
				return true;
			}
			return false;
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
		public void InLiteralString()
		{
			const string src = "00\"11\"00";
			var lit = new InLiteralString();
			foreach (var ch in src)
			{
				if (lit.WithinLiteralString(ch))
					Assert.True(ch == '\"' || ch == '1');
				else
					Assert.True(ch == '0');
			}
		}
	}
}
#endif
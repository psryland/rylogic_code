namespace Rylogic.Script
{
	/// <summary>A script character source that inserts indenting on new lines</summary>
	public class AddIndents :Src
	{
		public AddIndents(Src src, string indent, bool indent_first = true, string line_end = "\n")
			:base(src)
		{
			Indent = indent;
			LineEnd = line_end;

			if (indent_first)
				m_src.Buffer.Insert(0, Indent);
		}

		/// <summary>The string to indent with, typically tab characters</summary>
		private string Indent { get; }

		/// <summary>The sequence that identifies the end of a line</summary>
		public string LineEnd { get; private set; }

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override int Read()
		{
			// If the next characters are a line end,
			// insert the indent text into the src's buffer
			if (m_src.Match(LineEnd) && m_src[LineEnd.Length+1] != '\0')
				m_src.Buffer.Insert(LineEnd.Length, Indent);

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
		public void Indenting()
		{
			const string str_in =
				"This is \n" +
				"a stream of \n" +
				"characters\n";
			const string str_out =
				"indent This is \n" +
				"indent a stream of \n" +
				"indent characters\n";

			using var src = new AddIndents(new StringSrc(str_in), "indent ");
			for (int i = 0; i != str_out.Length; ++i, src.Next())
				Assert.Equal(str_out[i], src.Peek);
			Assert.Equal('\0', src.Peek);
		}
	}
}
#endif

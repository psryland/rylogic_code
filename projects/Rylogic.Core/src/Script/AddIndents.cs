namespace Rylogic.Script
{
	/// <summary>A script character source that inserts indenting on new lines</summary>
	public class AddIndents :Src
	{
		public AddIndents(Src src, string indent, bool indent_first = true, string line_end = "\n")
		{
			Src = src;
			Indent = indent;
			LineEnd = line_end;

			if (indent_first)
			{
				foreach (var x in Indent)
					Buffer.Append(x);
			}
		}
		protected override void Dispose(bool _)
		{
			Src.Dispose();
			base.Dispose(_);
		}

		/// <summary>The input source stream</summary>
		private Src Src { get; }

		/// <summary>The string to indent with, typically tab characters</summary>
		private string Indent { get; }

		/// <summary>The sequence that identifies the end of a line</summary>
		public string LineEnd { get; private set; }

		/// <summary>The 'file position' within the source</summary>
		public override Loc Location => Src.Location;

		/// <summary>Return the next valid character from the underlying stream or '\0' for the end of stream.</summary>
		protected override char Read()
		{
			// If the next characters are a line end..
			if (Src.Match(LineEnd))
			{
				// Buffer the line end..
				foreach (var x in LineEnd)
					Buffer.Append(x);

				// Skip the line end in the source
				Src.Next(LineEnd.Length);
				if (Src.Peek != 0)
				{
					// If not the end of the source, insert the indent
					foreach (var x in Indent)
						Buffer.Append(x);
				}

				// Return 0 because we've manually modified the Buffer
				return '\0';
			}
			else
			{
				var ch = Src.Peek;
				if (ch != 0) Src.Next();
				return ch;
			}
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
			Assert.Equal((char)0, src.Peek);
		}
	}
}
#endif

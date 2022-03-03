using System.Diagnostics;

namespace Rylogic.Script
{
	/// <summary>A location within a script source</summary>
	public class Loc
	{
		// Notes:
		//  - Don't use this class for save/restore of a stream position.
		//    It's meant to just be for error messages.

		public Loc()
			:this(string.Empty, 0, 1, 1)
		{}
		public Loc(string filepath, long pos, int line, int column, int tab_size = 1)
		{
			Debug.Assert(line >= 1, "Line number should be a natural number (i.e. >= 1)");
			Debug.Assert(column >= 1, "Column number should be a natural number (i.e. >= 1)");

			Uri = filepath;
			Pos = pos;
			Line = line;
			Column  = column;
			TabSize = tab_size;
		}

		/// <summary>The filepath, resource address, etc</summary>
		public string Uri;

		/// <summary>The character position in the stream (0-based)</summary>
		public long Pos;

		/// <summary>The line number within the source (natural number i.e. 1-based)</summary>
		public int Line;

		/// <summary>The column number within the source (natural number i.e. 1-based)</summary>
		public int Column;

		/// <summary>The number of columns that a tab character corresponds to</summary>
		public int TabSize;

		/// <summary>Increment the location based on 'ch'</summary>
		public char inc(char ch)
		{
			if (ch != 0)
			{
				++Pos;
			}
			if (ch == '\n')
			{
				++Line;
				Column = 1;
			}
			else if (ch == '\t')
			{
				Column += TabSize;
			}
			else if (ch != '\0')
			{
				++Column;
			}
			return ch;
		}

		/// <summary></summary>
		public override string ToString() => $"File:{Uri} Line:{Line} Col:{Column}";
	};
}


#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture]
	public partial class TestScript
	{
		[Test]
		public static void Loc()
		{
			const string str =
				"123\n"+
				"abc\n"+
				"\tx";

			var loc = new Loc() { TabSize = 4 };
			foreach (var ch in str)
				loc.inc(ch);

			Assert.Equal(10, loc.Pos);
			Assert.Equal(3, loc.Line);
			Assert.Equal(6, loc.Column);
		}
	}
}
#endif

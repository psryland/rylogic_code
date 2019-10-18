namespace Rylogic.Script
{
	/// <summary>A location within a script source</summary>
	public class Loc
	{
		public Loc()
			:this(string.Empty, 0, 0)
		{}
		public Loc(string filepath, int line, int column)
		{
			Uri = filepath;
			Line = line;
			Column  = column;
		}

		/// <summary>The filepath, resource address, etc</summary>
		public string Uri;

		/// <summary>The line number within the source</summary>
		public int Line;

		/// <summary>The column number within the source</summary>
		public int Column;

		/// <summary>Increment the location based on 'ch'</summary>
		public char inc(char ch)
		{
			if (ch == '\n')
			{
				++Line;
				Column = 0;
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
	[TestFixture] public partial class TestScript
	{
		[Test] public static void Loc()
		{}
	}
}
#endif

using pr.extn;

namespace pr.script
{
	// A location within a script source
	public class Loc
	{
		public string Filepath;
		public int Line;
		public int Column;
		
		public Loc()
		{
			Filepath = string.Empty;
			Line = 0;
			Column = 0;
		}
		public Loc(string filepath, int line, int column)
		{
			Filepath = filepath;
			Line = line;
			Column  = column;
		}
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

		public override string ToString() { return "File:{0} Line:{1} Col:{2}".Fmt(Filepath, Line, Column); }
	};
}


#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestScript
		{
			[Test] public static void TestLoc()
			{}
		}
	}
}
#endif

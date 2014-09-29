namespace pr.script
{
	public static class CodeUtil
	{
		/// <summary>Determines whether 'position' is within a C-like code string</summary>
		public static bool IsWithinString(string line, int position)
		{
			var in_string = false;

			bool esc = false;
			for (var i = 0; i != position; ++i)
			{
				if (!esc && (line[i] == '\"' || line[i] == '\''))
				{
					in_string = true;
					var match = line[i];
					for (++i; i != position; ++i)
					{
						if (!esc && line[i] == match) break; // Stop when the matching quote is found
						esc = !esc && line[i] == '\\';
					}
					if (i == position) break;
					in_string = false;
				}
				esc = !esc && line[i] == '\\';
			}
			return in_string;
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using script;

	[TestFixture] public partial class TestScript
	{
		[Test] public void TestCodeUtil()
		{
			Assert.False(CodeUtil.IsWithinString(@"0'23\'67\\'12", 0 ));
			Assert.False(CodeUtil.IsWithinString(@"0'23\'67\\'12", 1 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 2 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 3 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 4 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 5 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 6 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 7 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 8 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 9 ));
			Assert.True (CodeUtil.IsWithinString(@"0'23\'67\\'12", 10));
			Assert.False(CodeUtil.IsWithinString(@"0'23\'67\\'12", 11));
			Assert.False(CodeUtil.IsWithinString(@"0'23\'67\\'12", 12));
		}
	}
}

#endif
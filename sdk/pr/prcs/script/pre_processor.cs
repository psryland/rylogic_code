using System.IO;

namespace pr.script
{
	/// <summary>Base class for a source of script characters</summary>
	public class PreProcessor
	{}

}

#if PR_UNITTESTS
namespace pr
{
	using NUnit.Framework;
	using script;

	[TestFixture] internal static partial class UnitTests
	{
		internal static partial class TestScript
		{
			[Test] public static void TestPreProcessor()
			{}
		}
	}
}
#endif

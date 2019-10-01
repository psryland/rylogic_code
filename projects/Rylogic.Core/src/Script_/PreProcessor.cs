using System.IO;

namespace Rylogic.Script
{
	/// <summary>Base class for a source of script characters</summary>
	public class PreProcessor
	{}

}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Script;

	[TestFixture] public partial class TestScript
	{
		[Test] public static void PreProcessor()
		{}
	}
}
#endif

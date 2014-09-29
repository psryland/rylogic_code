using System.IO;

namespace pr.script
{
	/// <summary>Base class for a source of script characters</summary>
	public class PreProcessor
	{}

}

#if PR_UNITTESTS
namespace pr.unittests
{
	using script;

	[TestFixture] public partial class TestScript
	{
		[Test] public static void PreProcessor()
		{}
	}
}
#endif

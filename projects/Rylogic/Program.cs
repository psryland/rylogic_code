using System;
using System.Reflection;
using System.Windows.Forms;

namespace pr
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread] public static int Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			return Environment.ExitCode = pr.unittests.UnitTest.RunLocalTests() ? 0 : 1;
		}

		public static void RunTestsOrThrow()
		{
			if (!pr.unittests.UnitTest.RunLocalTests())
				throw new Exception("Unit tests failed");
		}
	}
}
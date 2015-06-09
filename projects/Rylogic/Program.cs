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
			// PowerShell CommandLine:
			// powershell -noninteractive -noprofile -sta -nologo -command "[Reflection.Assembly]::LoadFile('P:\projects\Rylogic\bin\Debug\Rylogic.dll')|Out-Null;exit [pr.Program]::Main();"
			// Note: You can't use $(TargetPath) in debugging command line options, it doesn't get expanded
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
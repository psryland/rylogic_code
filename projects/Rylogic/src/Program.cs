using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Rylogic
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread] public static int Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// PowerShell CommandLine:
			// PowerShell -NonInteractive -NoProfile -STA -NoLogo -Command "[Reflection.Assembly]::LoadFile('P:\projects\Rylogic\bin\Debug\Rylogic.dll')|Out-Null;exit [Rylogic.Program]::Main();"
			// Note: You can't use $(TargetPath) in debugging command line options, it doesn't get expanded
			var ass = Assembly.GetExecutingAssembly();
			Debug.WriteLine($"{ass.GetName().Name} running as a {(Environment.Is64BitProcess ? "64bit" : "32bit")} process");
			return Environment.ExitCode = UnitTests.UnitTest.RunTests(ass) ? 0 : 1;
		}
	}
}
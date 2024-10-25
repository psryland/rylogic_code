using System;
using System.Diagnostics;
using System.Reflection;
using Rylogic.UnitTests;

namespace Rylogic.Scintilla
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread]
		public static int Main()
		{
			var ass = Assembly.GetExecutingAssembly();
			Debug.WriteLine($"{ass.GetName().FullName} running as a {(Environment.Is64BitProcess ? "64bit" : "32bit")} process");
			return Environment.ExitCode = UnitTest.RunTests(ass) ? 0 : 1;
		}
	}
}
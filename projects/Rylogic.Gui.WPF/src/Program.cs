using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Rylogic.UnitTests;

[assembly: ThemeInfo(
	//where theme specific resource dictionaries are located used if a resource is not found in the page, or application resource dictionaries)
	ResourceDictionaryLocation.None, 
	//where the generic resource dictionary is located (used if a resource is not found in the page, app, or any theme specific resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly
)]

//[assembly: System.Windows.Markup.XmlnsDefinition("http://rylogic.co.nz/guiwpf", "Rylogic.Gui.WPF")]

namespace Rylogic.Gui.WPF
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread] public static int Main()
		{
			var ass = Assembly.GetExecutingAssembly();
			Debug.WriteLine($"{ass.GetName().Name} running as a {(Environment.Is64BitProcess ? "64bit" : "32bit")} process");
			return Environment.ExitCode = UnitTest.RunTests(ass) ? 0 : 1;
		}
	}
}
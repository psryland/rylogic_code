using System;
using System.Reflection;
using System.Windows.Forms;

namespace pr
{
	public static class Program
	{
		/// <summary>The main entry point for the application.</summary>
		[STAThread] public static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			pr.unittest.UnitTest.RunTests(Assembly.GetExecutingAssembly());
		}
	}
}
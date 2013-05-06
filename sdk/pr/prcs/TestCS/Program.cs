using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.util;

namespace TestCS
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			// Copy the dlls to the current directory if not currently there
			Util.LibCopy(@"view3d.{platform}.{config}.dll", "view3d.dll", true);
			Util.LibCopy(@"sqlite3.{platform}.{config}.dll", "sqlite3.dll", true);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormTestApp());
		}
	}
}

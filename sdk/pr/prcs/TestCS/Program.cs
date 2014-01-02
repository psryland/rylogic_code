using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
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
			// Note! Running this in the debugger causes this to be run as a 32bit
			// process regardless of the selected solution platform
			Debug.WriteLine("\n    {0} is a {1}bit process\n".Fmt(Application.ExecutablePath, Environment.Is64BitProcess ? "64" : "32"));

			// Copy the dlls to the current directory if not currently there
			Util.LibCopy(@"{platform}\{config}\view3d.dll", "view3d.dll", true);
			Util.LibCopy(@"{platform}\{config}\sqlite3.dll", "sqlite3.dll", true);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new FormTestApp());
		}
	}
}

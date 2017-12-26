using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Utility;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App :Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Note! Running this in the debugger causes this to be run as a 32bit
			// process regardless of the selected solution platform
			Debug.WriteLine("\n    {0} is a {1}bit process\n".Fmt(Assembly.GetEntryAssembly().Location, Environment.Is64BitProcess ? "64" : "32"));

			//// Copy the dlls to the current directory if not currently there
			//Util.LibCopy(@"{platform}\{config}\view3d.dll", "view3d.dll", true);
			//Util.LibCopy(@"{platform}\{config}\sqlite3.dll", "sqlite3.dll", true);

		}
	}
}

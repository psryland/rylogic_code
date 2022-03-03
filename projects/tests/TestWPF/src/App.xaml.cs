using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using Rylogic.Gfx;

namespace TestWPF
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App :Application
	{
		public App()
		{
			View3d.LoadDll();
		}
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Note! Running this in the debugger causes this to be run as a 32bit
			// process regardless of the selected solution platform
			Debug.WriteLine($"\n    {Assembly.GetEntryAssembly()!.Location} is a {(Environment.Is64BitProcess?"64":"32")}bit process\n");

			//// Copy the dlls to the current directory if not currently there
			//Util.LibCopy(@"{platform}\{config}\view3d.dll", "view3d.dll", true);
			//Util.LibCopy(@"{platform}\{config}\sqlite3.dll", "sqlite3.dll", true);

		}
	}
}

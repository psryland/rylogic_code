using System;
using System.Diagnostics;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Utility;
using SolarHotWater.Common;

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located (used if a resource is not found in the page, or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located (used if a resource is not found in the page, app, or any theme specific resource dictionaries)
)]

namespace SolarHotWater.UI
{
	public partial class App :Application
	{
		static App()
		{
			WPFUtil.WaitForDebugger();
		}
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				View3d.LoadDll();
				MainWindow = new MainWindow();
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				if (Debugger.IsAttached) throw;
				Log.Write(ELogLevel.Fatal, ex, "Application startup failure");
				MsgBox.Show(null, $"Application startup failure: {ex.MessageFull()}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				Shutdown();
			}
		}
	}
}

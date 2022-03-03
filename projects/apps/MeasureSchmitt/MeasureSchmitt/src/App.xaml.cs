using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
									 //(used if a resource is not found in the page,
									 // or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
											  //(used if a resource is not found in the page,
											  // app, or any theme specific resource dictionaries)
)]

namespace MeasureSchmitt
{
	public partial class App :Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				//new Model(e.Args)
				MainWindow = new MainWindow();
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				if (Debugger.IsAttached) throw;
				//Log.Write(ELogLevel.Fatal, ex, Util.AppProductName, string.Empty, 0);
				MsgBox.Show(null, $"Application startup failure: {ex.MessageFull()}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				Shutdown();
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Gui.WPF;

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
									 //(used if a resource is not found in the page,
									 // or application resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
											  //(used if a resource is not found in the page,
											  // app, or any theme specific resource dictionaries)
)]

namespace Fishomatic
{
	public partial class App :Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				Xml_.Config
					.SupportSystemDrawingPrimitiveTypes()
					.SupportWPFTypes();

				MainWindow = new MainWindow();
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				MsgBox.Show(null, $"Application startup failure: {ex.MessageFull()}", "Solar Hot Water", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
			}
		}
	}
}

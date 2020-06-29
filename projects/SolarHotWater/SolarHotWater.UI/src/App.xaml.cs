using System;
using System.Diagnostics;
using System.Windows;

namespace SolarHotWater.UI
{
	public partial class App :Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				MainWindow = new MainWindow();
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex.Message);
				#if DEBUG
				throw;
				#endif
			}
		}
	}
}

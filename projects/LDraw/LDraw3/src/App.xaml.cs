using System;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Extn.Windows;
using Rylogic.Gfx;
using Rylogic.Maths;

namespace LDraw
{
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				View3d.LoadDll();
				Xml_.Config
					.SupportRylogicMathsTypes()
					.SupportRylogicGraphicsTypes()
					.SupportSystemDrawingPrimitiveTypes()
					.SupportSystemDrawingCommonTypes();

				MainWindow = new MainWindow(new Model(e.Args));
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				Log.Write(Rylogic.Utility.ELogLevel.Fatal, ex, $"LDraw failed to start");
				#if DEBUG
				throw;
				#endif
			}
		}
	}
}

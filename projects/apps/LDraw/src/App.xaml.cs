using System;
using System.Diagnostics;
using System.Windows;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Windows.Extn;

namespace LDraw
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
				Xml_.Config
					.SupportRylogicMathsTypes()
					.SupportRylogicGraphicsTypes()
					.SupportSystemDrawingPrimitiveTypes()
					.SupportSystemDrawingCommonTypes()
					.SupportWPFTypes()
					;

				var options = new StartupOptions(e.Args);
				MainWindow = new MainWindow(new Model(options));
				MainWindow.Show();
			}
			catch (Exception ex)
			{
				if (Debugger.IsAttached) throw;
				Log.Write(ELogLevel.Fatal, ex, Util.AppProductName, string.Empty, 0);
				MsgBox.Show(null, $"Application startup failure: {ex.MessageFull()}", Util.AppProductName, MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				Shutdown();
			}
		}
	}
}

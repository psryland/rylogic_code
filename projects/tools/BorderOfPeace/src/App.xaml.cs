using System;
using System.Diagnostics;
using System.Windows;
using BorderOfPeace.Model;
using BorderOfPeace.UI;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

[assembly: ThemeInfo(
	ResourceDictionaryLocation.None,
	ResourceDictionaryLocation.SourceAssembly
)]

namespace BorderOfPeace
{
	public partial class App : Application
	{
		private TrayHost? m_tray_host;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			try
			{
				var settings = Settings.Load();
				m_tray_host = new TrayHost(settings);
			}
			catch (Exception ex)
			{
				if (Debugger.IsAttached) throw;
				MsgBox.Show(null, $"Application startup failure: {ex.MessageFull()}", "Border Of Peace", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
				Shutdown();
			}
		}
	}
}

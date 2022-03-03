using System;
using System.Diagnostics;
using System.Windows;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;

namespace CoinFlip.UI
{
	public partial class App : Application
	{
		public App()
		{
			View3d.LoadDll();
			Xml_.Config.SupportWPFTypes();
		}
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Record the startup info to allow restart
			m_psi = new ProcessStartInfo
			{
				FileName = Process.GetCurrentProcess().MainModule?.FileName ?? throw new NullReferenceException("Current process is null"),
				Arguments = string.Join(" ", e.Args),
			};
		}
		private ProcessStartInfo m_psi = null!;

		/// <summary>Singleton access</summary>
		public static new App Current => (App)Application.Current;

		/// <summary>Restart this process</summary>
		public void Restart()
		{
			MainWindow.Closed += Start;
			MainWindow.Close();
			
			void Start(object? sender, EventArgs args)
			{
				MainWindow.Closed -= Start;
				Process.Start(m_psi);
			}
		}

		/// <summary>Selected app skin</summary>
		public static ESkin Skin
		{
			get => SettingsData.Settings.Skin;
			set
			{
				SettingsData.Settings.Skin = value;

				// A restart is needed to reload StaticResources
				var res = MessageBox.Show(Current.MainWindow, "Restart now to apply new skin settings?", "Change Skin", MessageBoxButton.YesNo, MessageBoxImage.Question);
				if (res == MessageBoxResult.Yes)
					Current.Restart();
			}
		}
	}
}

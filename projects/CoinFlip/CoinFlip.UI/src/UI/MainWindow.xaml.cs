using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
			m_root.DataContext = this;
			Model = new Model();
			SetupUI();
		}
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			LogOn();
		}
		protected override void OnClosing(CancelEventArgs e)
		{
			// Save the layout and window position
			SettingsData.Settings.UI.UILayout = m_dc.SaveLayout();

			// Shutdown is a PITA when using async methods.
			// Disable the form while we wait for shutdown to be allowed
			IsEnabled = false;
			if (!Model.Shutdown.IsCancellationRequested)
			{
				e.Cancel = true;
				Model.Shutdown.Cancel();
				Dispatcher.BeginInvoke(new Action(Close));
				return;
			}
			base.OnClosing(e);
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			Util.DisposeRange(m_dc.AllContent.OfType<IDisposable>().ToList());
			Util.Dispose(m_dc);
			Model = null;
		}

		/// <summary>App logic</summary>
		private Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			set
			{
				if (m_model == value) return;
				Util.Dispose(ref m_model);
				m_model = value;
			}
		}
		private Model m_model;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Menu
			m_menu_file_skin_toggle.Click += (s, a) =>
			{
				App.Skin = Enum<ESkin>.Cycle(App.Skin);
				Application.Current.Shutdown();
				Process.Start(Application.ResourceAssembly.Location);
			};
			m_menu.Items.Add(m_dc.WindowsMenu());
			#endregion

			#region Dockables
			m_dc.Add(new CandleChart(Model), EDockSite.Centre);
			m_dc.Add(new GridCoins(Model), EDockSite.Left);
			m_dc.Add(new GridExchanges(Model), EDockSite.Left, EDockSite.Bottom);
			#endregion
		}

		/// <summary>Change the logged on user</summary>
		private void LogOn()
		{
			// Log off first
			Model.User = null;

			// Prompt for a user log-on
			var ui = new LogOnUI { Username = SettingsData.Settings.LastUser };
			#if DEBUG
			ui.Username = "Paul";
			ui.Password = "UltraSecurePasswordWotIMade";
			#endif

			// If log on fails, close the application
			if (ui.ShowDialog() != true)
			{
				Close();
				return;
			}

			// Create the user instance from the log-on details
			Model.User = ui.User;
			Title = $"Coin Flip - {Model.User.Name}";
		}
	}
}

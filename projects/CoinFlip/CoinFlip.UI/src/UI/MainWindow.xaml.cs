using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Settings;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class MainWindow : Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			Model = new Model();

			// Commands
			LogOn = Command.Create(this, LogOnInternal);
			ToggleLiveTrading = Command.Create(this, () => Model.AllowTrades = !Model.AllowTrades);
			ToggleBackTesting = Command.Create(this, () => Model.BackTesting = !Model.BackTesting);

			// Dock container windows
			m_dc.Add(new GridExchanges(Model), EDockSite.Left);
			m_dc.Add(new GridCoins(Model), EDockSite.Left, EDockSite.Bottom);
			m_dc.Add(new GridTradeOrders(Model), 0, EDockSite.Bottom);
			m_dc.Add(new GridTradeHistory(Model), 1, EDockSite.Bottom);
			m_dc.Add(new CandleChart(Model), 0, EDockSite.Centre);
			m_dc.Add(new CandleChart(Model), 1, EDockSite.Centre);
			m_dc.Add(new LogView(), 2, EDockSite.Centre);
			m_menu.Items.Add(m_dc.WindowsMenu());

			((CandleChart)Model.Charts[0]).DockControl.IsActiveContent = true;
			DataContext = this;
		}
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			LogOnInternal();
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
				if (m_model != null)
				{
					Model.AllowTradesChanged -= HandleAllowTradesChanged;
					Model.BackTestingChanged -= HandleBackTestingChanged;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					Model.BackTestingChanged += HandleBackTestingChanged;
					Model.AllowTradesChanged += HandleAllowTradesChanged;
				}

				// Handler
				void HandleAllowTradesChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AllowTrades)));
				}
				void HandleBackTestingChanged(object sender, EventArgs e)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(BackTesting)));
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Simulation)));
				}
			}
		}
		private Model m_model;

		/// <summary>The current state of live trading</summary>
		public bool AllowTrades
		{
			get => Model.AllowTrades;
			set => Model.AllowTrades = value;
		}

		/// <summary>The current state of back testing mode</summary>
		public bool BackTesting
		{
			get => Model.BackTesting;
			set => Model.BackTesting = value;
		}

		/// <summary>Access the main simulation model</summary>
		public Simulation Simulation => Model.Simulation;

		/// <summary></summary>
		public decimal NettWorth => 0m;//todo Model.NettWorth;

		/// <summary>Toggle the live trading switch</summary>
		public Command ToggleLiveTrading { get; }

		/// <summary>Toggle the state of back testing</summary>
		public Command ToggleBackTesting { get; }

		/// <summary>Change the logged on user</summary>
		public Command LogOn { get; }
		private void LogOnInternal()
		{
			// Log off first
			Model.User = null;

			// Prompt for a user log-on
			var ui = new LogOnUI { Username = SettingsData.Settings.LastUser };
			#if DEBUG
			ui.Username = "Paul";
			ui.Password = "UltraSecurePasswordWotIMade";
			#else
			// If log on fails, close the application
			if (ui.ShowDialog() != true)
			{
				Close();
				return;
			}
			#endif

			// Create the user instance from the log-on details
			Model.User = ui.User;
			Title = $"Coin Flip - {Model.User.Name}";
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}

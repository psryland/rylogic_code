using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using CoinFlip.Settings;
using CoinFlip.UI.Dialogs;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class MainWindow :Window, INotifyPropertyChanged
	{
		public MainWindow()
		{
			InitializeComponent();
			Model = new Model(CreateNewChart);

			// Commands
			CloseApp = Command.Create(this, CloseAppInternal);
			ShowOptions = Command.Create(this, ShowOptionsInternal);
			ToggleLiveTrading = Command.Create(this, ToggleLiveTradingInternal);
			ToggleBackTesting = Command.Create(this, ToggleBackTestingInternal);
			LogOn = Command.Create(this, LogOnInternal);
			NewChart = Command.Create(this, NewChartInternal);

			DataContext = this;
		}
		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);
			LogOnInternal();

			// Create the grids after log on so that exchanges have been initialised
			m_dc.Add(new GridExchanges(Model), EDockSite.Left);
			m_dc.Add(new GridBots(Model), EDockSite.Left);
			m_dc.Add(new GridCoins(Model), EDockSite.Left, EDockSite.Bottom);
			m_dc.Add(new GridFunds(Model), EDockSite.Left, EDockSite.Bottom);
			m_dc.Add(new GridTradeOrders(Model), 0, EDockSite.Bottom);
			m_dc.Add(new GridTradeHistory(Model), 1, EDockSite.Bottom);
			m_dc.Add(new GridTransfers(Model), 2, EDockSite.Bottom);
			m_dc.Add(new CandleChart(Model), 0, EDockSite.Centre);
			m_dc.Add(new EquityChart(Model), 1, EDockSite.Centre);
			m_dc.Add(new LogView(), 2, EDockSite.Right).IsAutoHide = true;
			m_menu.Items.Add(m_dc.WindowsMenu());

			// Bring panes to the front
			m_dc.FindAndShow(typeof(GridExchanges));
			m_dc.FindAndShow(typeof(GridCoins));
			m_dc.FindAndShow(typeof(GridTradeOrders));
			m_dc.FindAndShow(typeof(CandleChart));

			// Move the "current" position in the default binding to the last selected exchange
			var last_exchange = Model.Exchanges.FirstOrDefault(x => x.Name == SettingsData.Settings.LastExchange);
			CollectionViewSource.GetDefaultView(Model.Exchanges).MoveCurrentTo(last_exchange);

			// Display the last viewed instrument
			ShowLastChart();
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
					Model.BackTestingChange -= HandleBackTestingChange;
					m_model.NettWorthChanged -= HandleNettWorthChanged;
					m_model.EditingTrade -= HandleEditingTrade;
					Simulation = null;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					Simulation = new SimulationView(GetWindow(this), m_model);
					m_model.EditingTrade += HandleEditingTrade;
					m_model.NettWorthChanged += HandleNettWorthChanged;
					Model.BackTestingChange += HandleBackTestingChange;
					Model.AllowTradesChanged += HandleAllowTradesChanged;
				}

				// Handler
				void HandleEditingTrade(object sender, EditTradeContext e)
				{
					// Create an editor window for the trade
					var ui = new EditTradeUI(GetWindow(this), Model, e.Trade, e.Original is Order);
					ui.Closed += async (s, a) =>
					{
						// Signal the end of editing
						e.EndEdit();

						// Create or Update trade on the exchange
						if (ui.Result == true)
						{
							try { await e.Commit(); }
							catch (OperationCanceledException) { }
							catch (Exception ex)
							{
								Model.Log.Write(ELogLevel.Error, ex, "Creating/Updating trade failed");
								await Misc.RunOnMainThread(() =>
								{
									MsgBox.Show(GetWindow(this), ex.MessageFull(), "Create/Modify Order", MsgBox.EButtons.OK, MsgBox.EIcon.Error);
								});
							}
						}
					};
					ui.Show();

					// If the EditTrade instance is closed externally, close 'ui'
					e.Closed += delegate { ui.Close(); };
				}
				void HandleNettWorthChanged(object sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(NettWorth));
				}
				void HandleAllowTradesChanged(object sender, EventArgs e)
				{
					NotifyPropertyChanged(nameof(AllowTrades));
				}
				void HandleBackTestingChange(object sender, PrePostEventArgs e)
				{
					if (e.Before) return;
					NotifyPropertyChanged(nameof(BackTesting));
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
		public SimulationView Simulation
		{
			get => m_simulation_view;
			set
			{
				if (m_simulation_view == value) return;
				Util.Dispose(ref m_simulation_view);
				m_simulation_view = value;
				NotifyPropertyChanged(nameof(Simulation));
			}
		}
		private SimulationView m_simulation_view;

		/// <summary>Total holdings value across all exchanges and all currencies</summary>
		public decimal NettWorth => Model.NettWorth;
		public string ValuationCurrency => SettingsData.Settings.ValuationCurrency;

		/// <summary>Command to close the app</summary>
		public Command CloseApp { get; }
		private void CloseAppInternal()
		{
			Close();
		}

		/// <summary>Show the application settings dialog</summary>
		public Command ShowOptions { get; }
		private void ShowOptionsInternal()
		{
			if (m_settings_ui == null)
			{
				m_settings_ui = new MainSettingsUI(GetWindow(this));
				m_settings_ui.Closed += delegate { m_settings_ui = null; };
				m_settings_ui.Show();
			}
			m_settings_ui.Focus();
		}
		private MainSettingsUI m_settings_ui;

		/// <summary>Toggle the live trading switch</summary>
		public Command ToggleLiveTrading { get; }
		private void ToggleLiveTradingInternal()
		{
			Model.AllowTrades = !Model.AllowTrades;
		}

		/// <summary>Toggle the state of back testing</summary>
		public Command ToggleBackTesting { get; }
		private void ToggleBackTestingInternal()
		{
			Model.BackTesting = !Model.BackTesting;
		}

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

		/// <summary>Add a new chart window</summary>
		public Command NewChart { get; }
		private void NewChartInternal()
		{
			CreateNewChart();
		}

		/// <summary>Try to display the last chart</summary>
		private void ShowLastChart()
		{
			var parts = SettingsData.Settings.LastChart.Split('-');
			if (parts.Length != 3)
				return;

			// Find the specified exchange
			var exch = Model.Exchanges.FirstOrDefault(x => x.Name == parts[0]);
			if (exch == null)
				return;

			var pair_name = parts[1];
			var tf = Enum<ETimeFrame>.TryParse(parts[2]) ?? ETimeFrame.None;

			// Wait for the exchange to load its pairs, and select the chart when available
			exch.Pairs.CollectionChanged += WaitForPairs;
			void WaitForPairs(object sender, NotifyCollectionChangedEventArgs e)
			{
				if (e.Action != NotifyCollectionChangedAction.Add)
					return;

				for (; ; )
				{
					// Give up if the chart has instrument, pair, and time frame selected already
					var chart0 = Model.Charts[0];
					if (chart0.Exchange != null && chart0.Pair != null && chart0.TimeFrame != ETimeFrame.None)
						break;

					// See if the pair is available. If not, keep waiting
					var pair = exch.Pairs[parts[1]];
					if (pair == null)
						return;

					// Set the chart
					chart0.Exchange = exch;
					chart0.Pair = pair;
					chart0.TimeFrame = tf;
					break;
				}
				exch.Pairs.CollectionChanged -= WaitForPairs;
			}
		}

		/// <summary>Callback function for creating new charts</summary>
		private IChartView CreateNewChart()
		{
			var chart = m_dc.Add2(new CandleChart(Model), EDockSite.Centre);
			return chart;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

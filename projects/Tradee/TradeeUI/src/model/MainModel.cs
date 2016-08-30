using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	/// <summary>Container object for the main app logic</summary>
	public class MainModel :IDisposable
	{
		public MainModel(MainUI owner, Settings settings)
		{
			Owner           = owner;
			Settings        = settings;
			TradeDataSource = new TradeeClient(DispatchMsg);
			Acct            = new AccountStatus(this, new Account());
			History         = new History(this);
			MarketData      = new MarketData(this);
			Favourites      = new BindingSource<Instrument> { DataSource = new BindingListEx<Instrument>() };
			Positions       = new Positions(this);
			Sessions        = new Sessions(this);
			Alarms          = new Alarms();
			Simulation      = new Simulation(this);
		}
		public void Dispose()
		{
			Simulation      = null;
			Alarms          = null;
			Sessions        = null;
			Positions       = null;
			Favourites      = null;
			MarketData      = null;
			History         = null;
			Acct            = null;
			TradeDataSource = null;
		}

		/// <summary>Owner window</summary>
		public MainUI Owner
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Trade account info</summary>
		public AccountStatus Acct
		{
			[DebuggerStepThrough] get { return m_acct; }
			private set
			{
				if (m_acct == value) return;
				Util.Dispose(ref m_acct);
				m_acct = value;
			}
		}
		private AccountStatus m_acct;

		/// <summary>The history of trades</summary>
		public History History
		{
			[DebuggerStepThrough] get { return m_history; }
			private set
			{
				if (m_history == value) return;
				Util.Dispose(ref m_history);
				m_history = value;
			}
		}
		private History m_history;

		/// <summary>The store of market data</summary>
		public MarketData MarketData
		{
			[DebuggerStepThrough] get { return m_market_data; }
			private set
			{
				if (m_market_data == value) return;
				Util.Dispose(ref m_market_data);
				m_market_data = value;
			}
		}
		private MarketData m_market_data;

		/// <summary>The collection of favourite instruments</summary>
		public BindingSource<Instrument> Favourites
		{
			get { return m_favs; }
			private set
			{
				if (m_favs == value) return;
				if (m_favs != null)
				{
					m_favs.ListChanging -= HandleFavouritesListChanging;
				}
				m_favs = value;
				if (m_favs != null)
				{
					// Populate the favourites list from the settings
					foreach (var sym in Settings.General.FavouriteInstruments)
						Favourites.Add(MarketData[sym]);

					m_favs.ListChanging += HandleFavouritesListChanging;
				}
			}
		}
		private BindingSource<Instrument> m_favs;

		/// <summary>The store of all orders (visualising/pending/active/closed)</summary>
		public Positions Positions
		{
			[DebuggerStepThrough] get { return m_positions; }
			private set
			{
				if (m_positions == value) return;
				Util.Dispose(ref m_positions);
				m_positions = value;
			}
		}
		private Positions m_positions;

		/// <summary>The Forex trading sessions</summary>
		public Sessions Sessions
		{
			[DebuggerStepThrough] get { return m_sessions; }
			private set
			{
				if (m_sessions == value) return;
				Util.Dispose(ref m_sessions);
				m_sessions = value;
			}
		}
		private Sessions m_sessions;

		/// <summary>Alarm app logic</summary>
		public Alarms Alarms
		{
			[DebuggerStepThrough] get { return m_alarms; }
			private set
			{
				if (m_alarms == value) return;
				Util.Dispose(ref m_alarms);
				m_alarms = value;
			}
		}
		private Alarms m_alarms;

		/// <summary>The connection to the trade data source</summary>
		private TradeeClient TradeDataSource
		{
			[DebuggerStepThrough] get { return m_client; }
			set
			{
				if (m_client == value) return;
				if (m_client != null)
				{
					m_client.Disconnect();
					m_client.ConnectionChanged -= HandleTradeeConnectionChanged;
					Util.Dispose(ref m_client);
				}
				m_client = value;
				if (m_client != null)
				{
					m_client.ConnectionChanged += HandleTradeeConnectionChanged;
				}
			}
		}
		private TradeeClient m_client;

		/// <summary>Trading simulation</summary>
		public Simulation Simulation
		{
			[DebuggerStepThrough] get { return m_sim; }
			set
			{
				if (m_sim == value) return;
				if (m_sim != null)
				{
					//m_sim.SimReset       -= HandleSimReset;
					//m_sim.SimTimeChanged -= HandleSimSimTimeChanged;
					Util.Dispose(ref m_sim);
				}
				m_sim = value;
				if (m_sim != null)
				{
					//m_sim.SimTimeChanged += HandleSimSimTimeChanged;
					//m_sim.SimReset       += HandleSimReset;
				}
			}
		}
		private Simulation m_sim;

		/// <summary>True if the link to the trade data source is available</summary>
		public bool IsConnected
		{
			get
			{
				if (m_sim_active) return true;
				return TradeDataSource?.IsConnected ?? false;
			}
		}

		/// <summary>True if simulating, false if live</summary>
		public bool SimActive
		{
			[DebuggerStepThrough] get { return m_sim_active; }
			set
			{
				if (m_sim_active == value) return;
				m_sim_active = value;

				// Disable the live connection, enable the sim
				if (value)
				{
					// Disconnect from the trade data source
					TradeDataSource = null;

					// Reset the sim
					Simulation.Reset();

					// Turning on the simulation is like connecting to a different trade data source
					OnConnectionChanged();
				}
				// Disable the sim, enable the live connection
				else
				{
					// Reconnect to the live trade data source
					TradeDataSource = new TradeeClient(DispatchMsg);
				}
			}
		}
		private bool m_sim_active;

		/// <summary>The current time (in UTC). Note: for simulations, this is a time in the past</summary>
		public DateTimeOffset UtcNow
		{
			get { return SimActive ? Simulation.UtcNow : DateTimeOffset.UtcNow; }
		}

		/// <summary>Send a message to the trade data source</summary>
		public bool Post<T>(T msg) where T:ITradeeMsg
		{
			if (!IsConnected)
				return false;

			// The Simulation acts as the trade data source while a simulation is running
			var res = m_sim_active
				? Simulation.Post(msg)
				: TradeDataSource.Post(msg);

			Debug.WriteLine("{0}: {1}".Fmt(res ? "Posted" : "Post Failed", msg.GetType().Name));
			return res;
		}

		/// <summary>Raised when the trade data source connection changes</summary>
		public event EventHandler ConnectionChanged;
		private void OnConnectionChanged()
		{
			ConnectionChanged.Raise(this);

			// On connection, request an update of the account status
			if (IsConnected)
				RefreshAcctStatus();
		}

		/// <summary>Handle messages received from the trade data source</summary>
		public void DispatchMsg(ITradeeMsg msg)
		{
			// Ignore messages after dispose
			if (!IsConnected)
				return;

			// Dispatch received messages
			switch (msg.ToMsgType())
			{
			default:
				#region
				{
					Debug.WriteLine("Unknown Message Type {0} received".Fmt(msg.GetType().Name));
					Owner.Status.SetStatusMessage(msg:"Unknown Message Type {0} received".Fmt(msg.GetType().Name), fr_color:Color.Red, display_time:TimeSpan.FromSeconds(5));
					break;
				}
				#endregion
			case EMsgType.HelloTradee:
				#region
				{
					var m = (InMsg.HelloTradee)msg;
					MsgBox.Show(Owner, m.Text, "Test Msg Received");
					break;
				}
				#endregion
			case EMsgType.CandleData:
				#region
				{
					var cd = (InMsg.CandleData)msg;
					if (cd.Candle  != null) MarketData[cd.SymbolCode].Add(cd.TimeFrame, cd.Candle);
					if (cd.Candles != null) MarketData[cd.SymbolCode].Add(cd.TimeFrame, cd.Candles);
					break;
				}
				#endregion
			case EMsgType.AccountUpdate:
				#region
				{
					var acct = (InMsg.AccountUpdate)msg;
					Acct.Update(acct.Acct);
					break;
				}
				#endregion
			case EMsgType.PositionsUpdate:
				#region
				{
					var pos = (InMsg.PositionsUpdate)msg;
					Acct.Update(pos.Positions);
					Positions.Update(pos.Positions);
					break;
				}
				#endregion
			case EMsgType.PendingOrdersUpdate:
				#region
				{
					var orders = (InMsg.PendingOrdersUpdate)msg;
					Acct.Update(orders.PendingOrders);
					Positions.Update(orders.PendingOrders);
					break;
				}
				#endregion
			case EMsgType.SymbolData:
				#region
				{
					var sd = (InMsg.SymbolData)msg;
					MarketData[sd.Symbol].PriceData = sd.Data;
					break;
				}
				#endregion
			case EMsgType.HistoryData:
				#region
				{
					var hd = (InMsg.HistoryData)msg;
					Positions.Update(hd.Orders);
					break;
				}
				#endregion
			}
		}

		/// <summary>Add a new, or show an existing, chart in the UI</summary>
		public void ShowChart(string sym)
		{
			Owner.ShowChart(sym);
		}

		/// <summary></summary>
		public PerChartSettings FindChartSettings(string symbol_code)
		{
			// If there aren't any for this symbol, create some
			var chart_settings = Settings.Chart.PerChart.FirstOrDefault(x => x.SymbolCode == symbol_code);
			if (chart_settings == null)
			{
				chart_settings = new PerChartSettings { SymbolCode = symbol_code };
				Settings.Chart.PerChart = Settings.Chart.PerChart.Concat(chart_settings).ToArray();
			}
			return chart_settings;
		}

		/// <summary>All existing charts</summary>
		public IEnumerable<ChartUI> Charts
		{
			get { return Owner.Charts; }
		}

		/// <summary>Request an update of the account status</summary>
		public void RefreshAcctStatus()
		{
			Post(new OutMsg.RequestAccountStatus(Acct?.AccountId));
		}

		/// <summary>Request the trade history for the current account</summary>
		public void RefreshTradeHistory()
		{
			Post(new OutMsg.RequestTradeHistory());
		}

		/// <summary>Determine the stop loss level for a potential trade on 'instrument'</summary>
		public double CalculateStopLoss(Instrument instrument)
		{
			return 0.01;
		}

		/// <summary>Choose a volume such that the 'distance' (in base currency) has a value approximately equal to 'balance_pc' percent of the current balance</summary>
		public long CalculateVolume(Instrument instrument, double distance)
		{
			var pd = instrument.PriceData;

			// The amount of base currency to base the volume on
			var amount = Misc.AcctToBaseCurrency(Acct.Balance * Settings.Trade.RiskFracPerTrade, pd) * Acct.Leverage;

			// The value is the change of 'distance' * volume.
			// We want to choose volume such that abs(value - amount) is minimised

			//volume = amount / distance;

			//var pd = instrument.PriceData;
			//var d = pd.PipValue * distance;
			return 0;
		}

		/// <summary>Place an immediate market order</summary>
		public void NewMarketImmediateOrder(Instrument instr, ETradeType trade_type, double? volume = null, double? stop_loss = null, double? take_profit = null)
		{
			// Choose a volume for the position based on the current balance and the stop loss distance
			if (volume == null && stop_loss != null)
				volume = CalculateVolume(instr, stop_loss.Value);
			if (stop_loss == null && volume != null)
				stop_loss = CalculateStopLoss(instr);

			// Create the position we want to hold
			var position = new Position
			{
				SymbolCode  = instr.SymbolCode,
				TradeType   = trade_type,
				EntryTime   = DateTimeOffset.UtcNow.Ticks,
				EntryPrice  = trade_type == ETradeType.Long ? instr.PriceData.BidPrice : instr.PriceData.AskPrice,
				StopLossRel = stop_loss == null ? 0 : 0,//todo
				Volume      = 0,
				Comment     = "Auto Trade test",
			};

			// Place the order
			Post(new OutMsg.PlaceMarketOrder(position, Settings.Trade.MarketRangePips));
		}

		/// <summary>Handle connection/disconnection of the trade data source</summary>
		private void HandleTradeeConnectionChanged(object sender, EventArgs e)
		{
			// Notify connection changed
			OnConnectionChanged();
		}

		/// <summary>Handle the list of favourite instruments changing</summary>
		private void HandleFavouritesListChanging(object sender, ListChgEventArgs<Instrument> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemAdded:
			case ListChg.ItemRemoved:
				Settings.General.FavouriteInstruments = Favourites.Select(x => x.SymbolCode).ToArray();
				break;
			}
		}
	}
}


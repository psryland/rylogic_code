using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.container;
using pr.extn;
using pr.util;
using Tradee;

namespace cAlgo
{
	public class TradeeBotModel :IDisposable
	{
		/// <summary>The main thread dispatcher</summary>
		private Dispatcher m_main_thread;

		/// <summary>A cache of symbol information interfaces</summary>
		private Cache<string,Symbol> m_sym_cache;


		public TradeeBotModel(Robot calgo, Settings settings)
		{
			CAlgo = calgo;
			Settings = settings;
			m_main_thread = Dispatcher.CurrentDispatcher;
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Connect to 'Tradee'
			Tradee = new TradeeProxy(DispatchMsg);

			// The collection of transmitters
			Transmitters = new BindingSource<Transmitter> { DataSource = new BindingListEx<Transmitter>() };
			foreach (var trans in Settings.Transmitters)
				Transmitters.Add(new Transmitter(this, trans.Pair, trans));

			// Initiate the connection by sending account data
			SendAccountStatus();
		}
		public virtual void Dispose()
		{
			// Don't save the removed transmitters
			Settings.AutoSaveOnChanges = false;

			// Shutdown all transmitters
			Util.DisposeAll(Transmitters);

			// Kill the connection to tradee
			Tradee = null;

			Util.Dispose(ref m_sym_cache);
			GC.SuppressFinalize(this);
		}

		/// <summary>The cAlgo connection</summary>
		public Robot CAlgo { get; private set; }

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The connection to the Tradee application</summary>
		private TradeeProxy Tradee
		{
			[DebuggerStepThrough] get { return m_tradee; }
			set
			{
				if (m_tradee == value) return;
				if (m_tradee != null)
				{
					m_tradee.ConnectionChanged -= HandleConnectionChanged;
					m_tradee.Posted -= HandleDataPosted;
					Util.Dispose(ref m_tradee);
				}
				m_tradee = value;
				if (m_tradee != null)
				{
					m_tradee.Posted += HandleDataPosted;
					m_tradee.ConnectionChanged += HandleConnectionChanged;
				}
			}
		}
		private TradeeProxy m_tradee;

		/// <summary>Worker threads for transmitting symbol data to Tradee</summary>
		public BindingSource<Transmitter> Transmitters
		{
			[DebuggerStepThrough] get { return m_bs_transmitters; }
			private set
			{
				if (m_bs_transmitters == value) return;
				if (m_bs_transmitters != null)
				{
					m_bs_transmitters.ListChanging -= HandleTransmitterListChanging;
				}
				m_bs_transmitters = value;
				if (m_bs_transmitters != null)
				{
					m_bs_transmitters.ListChanging += HandleTransmitterListChanging;
				}
			}
		}
		private BindingSource<Transmitter> m_bs_transmitters;

		/// <summary>Raised when data is sent to Tradee</summary>
		public event EventHandler DataPosted;

		/// <summary>Step each of the transmitters</summary>
		public void Step()
		{
			// Send account information
			SendAccountStatus();
			SendCurrentPositions();
			SendPendingPositions();

			// Send price data
			foreach (var trans in Transmitters.Where(x => x.Enabled))
			{
				try
				{
					trans.Step();
				}
				catch (Exception ex)
				{
					Debug.WriteLine(ex.Message);
				}
			}
		}

		/// <summary>Add a new transmitter (unless it already exists)</summary>
		public Transmitter AddTransmitter(ETradePairs pair)
		{
			// If the pair is already in the list, set it as the current pair
			var idx = Transmitters.IndexOf(x => x.Pair == pair);
			if (idx != -1)
			{
				Transmitters.Position = idx;
				return Transmitters.Current;
			}

			// Look for settings for this pair
			var s_idx = Settings.Transmitters.IndexOf(x => x.Pair == pair);
			if (s_idx == -1)
			{
				// If no settings exist, create some
				var list = Settings.Transmitters.ToList();
				list.Add(new TransmitterSettings(pair) { TimeFrames = Settings.DefaultTimeFrames });
				Settings.Transmitters = list.ToArray();
				s_idx = list.Count - 1;
			}

			var trans = new Transmitter(this, pair, Settings.Transmitters[s_idx]);
			return Transmitters.Add2(trans);
		}

		/// <summary>Get the latest account information</summary>
		public IAccount GetAccount()
		{
			return CAlgo.Account;
		}

		/// <summary>Return the symbol for a given code</summary>
		public Symbol GetSymbol(string code)
		{
			return m_sym_cache.Get(code, c =>
				{
					Symbol res = null;
					using (var wait = new ManualResetEvent(false))
					{
						CAlgo.BeginInvokeOnMainThread(() =>
						{
							res = CAlgo.MarketData.GetSymbol(code);
							wait.Set();
						});
						wait.WaitOne();
						return res;
					}
				});
		}

		/// <summary>Return the series for a given symbol and time frame</summary>
		public MarketSeries GetSeries(Symbol sym, TimeFrame tf)
		{
			MarketSeries res = null;
			using (var wait = new ManualResetEvent(false))
			{
				CAlgo.BeginInvokeOnMainThread(() =>
				{
					res = CAlgo.MarketData.GetSeries(sym, tf);
					wait.Set();
				});
				wait.WaitOne();
				return res;
			}
		}

		/// <summary>True if the pipe is connected</summary>
		public bool IsConnected
		{
			get { return Tradee.IsConnected; }
		}

		/// <summary>Raised when the pipe connection is established/broken</summary>
		public event EventHandler ConnectionChanged;
		private void HandleConnectionChanged(object sender, EventArgs e)
		{
			RunOnMainThread(() => ConnectionChanged.Raise(this));
		}

		/// <summary>Called when data is sent to Tradee</summary>
		private void HandleDataPosted(object sender, EventArgs e)
		{
			DataPosted.Raise(sender, e);
		}

		/// <summary>Handle changes to the list of transmitters</summary>
		private void HandleTransmitterListChanging(object sender, ListChgEventArgs<Transmitter> e)
		{
			switch (e.ChangeType)
			{
			case ListChg.ItemRemoved:
				{
					// Remove the settings for this transmitter
					var list = Settings.Transmitters.ToList();
					list.Remove(e.Item.Settings);
					Settings.Transmitters = list.ToArray();

					// Dispose the transmitter
					e.Item.Dispose();
					break;
				}
			}
		}

		/// <summary>Handle messages from Tradee</summary>
		private void DispatchMsg(object msg)
		{
			if (msg is OutMsg.HelloClient)
			#region
			{
			}
			#endregion
			else if (msg is OutMsg.RequestAccountStatus)
			#region
			{
				// A request to send the account information
				var req = (OutMsg.RequestAccountStatus)msg;

				// Send the account info if the id matches.
				var acct = GetAccount();
				if (!req.AccountId.HasValue() || req.AccountId == acct.Number.ToString())
				{
					SendAccountStatus();
					SendCurrentPositions();
					SendPendingPositions();
				}
			}
			#endregion
			else if (msg is OutMsg.RequestInstrument)
			#region
			{
				var req = (OutMsg.RequestInstrument)msg;

				// Convert the symbol code to a known trading pair
				var pair = Enum<ETradePairs>.TryParse(req.SymbolCode);
				if (pair == null)
					return; // Not a pair we know about

				// Add or select the associated transmitter, and ensure the time frames are being sent
				var trans = AddTransmitter(pair.Value);
				trans.TimeFrames = trans.TimeFrames.Concat(req.TimeFrames).Distinct().ToArray();

				// Ensure data is sent, even if it hasn't changed
				trans.ForcePost();
			}
			#endregion
			else
			{
				Debug.WriteLine("Unknown Message Type {0} received".Fmt(msg.GetType().Name));
			}
		}

		/// <summary>Send details of the current account</summary>
		private void SendAccountStatus()
		{
			var data = GetAccount();
			var acct = new Account();
			acct.AccountId             = data.Number.ToString();
			acct.BrokerName            = data.BrokerName;
			acct.Currency              = data.Currency;
			acct.Balance               = data.Balance;
			acct.Equity                = data.Equity;
			acct.FreeMargin            = data.FreeMargin;
			acct.IsLive                = data.IsLive;
			acct.Leverage              = data.Leverage;
			acct.Margin                = data.Margin;
			acct.UnrealizedGrossProfit = data.UnrealizedGrossProfit;
			acct.UnrealizedNetProfit   = data.UnrealizedNetProfit;

			// Only send differences
			if (!acct.Equals(m_last_pending_orders))
			{
				if (Tradee.Post(new InMsg.AccountUpdate(acct)))
					m_last_account = acct;
			}
		}
		private Tradee.Account m_last_account = new Account();

		/// <summary>Send details about the currently active positions held</summary>
		private void SendCurrentPositions()
		{
			// Collect the currently active positions
			var list = CAlgo.Positions.Select(x => new Tradee.Position
			{
				Id            = x.Id,
				SymbolCode    = x.SymbolCode,
				TradeType     = x.TradeType.ToTradeeTradeType(),
				EntryTime     = x.EntryTime.ToUniversalTime().Ticks,
				EntryPrice    = x.EntryPrice,
				Pips          = x.Pips,
				StopLossRel   = x.StopLossRel(),
				TakeProfitRel = x.TakeProfitRel(),
				Quantity      = x.Quantity,
				Volume        = x.Volume,
				GrossProfit   = x.GrossProfit,
				NetProfit     = x.NetProfit,
				Commissions   = x.Commissions,
				Swap          = x.Swap,
				Label         = x.Label,
				Comment       = x.Comment,
			}).ToArray();

			// Only send differences
			if (!list.SequenceEqual(m_last_positions))
			{
				if (Tradee.Post(new InMsg.PositionsUpdate(list)))
					m_last_positions = list;
			}
		}
		private Tradee.Position[] m_last_positions = new Tradee.Position[0];

		/// <summary>Send details about pending orders</summary>
		private void SendPendingPositions()
		{
			// Collect the pending orders
			var list = CAlgo.PendingOrders.Select(x => new Tradee.PendingOrder
			{
				Id                         = x.Id,
				SymbolCode                 = x.SymbolCode,
				TradeType                  = x.TradeType.ToTradeeTradeType(),
				ExpirationTime             = x.ExpirationTime != null ? x.ExpirationTime.Value.ToUniversalTime().Ticks : 0,
				EntryPrice                 = x.TargetPrice,
				StopLossRel                = x.StopLossRel(),
				TakeProfitRel              = x.TakeProfitRel(),
				Quantity                   = x.Quantity,
				Volume                     = x.Volume,
				Label                      = x.Label,
				Comment                    = x.Comment,
			}).ToArray();

			// Only send differences
			if (!list.SequenceEqual(m_last_pending_orders))
			{
				if (Tradee.Post(new InMsg.PendingOrdersUpdate(list)))
					m_last_pending_orders = list;
			}
		}
		private Tradee.PendingOrder[] m_last_pending_orders = new Tradee.PendingOrder[0];

		/// <summary>Marshal a function call to the main thread</summary>
		public void RunOnMainThread(Action action)
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
				action();
			else
				m_main_thread.BeginInvoke(action);
		}
		public void RunOnMainThread(Action<object> action, object arg)
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
				action(arg);
			else
				m_main_thread.BeginInvoke(action, arg);
		}

		/// <summary>Send a message to Tradee</summary>
		public bool Post<T>(T msg) where T:ITradeeMsg
		{
			return Tradee != null && Tradee.Post(msg);
		}

		/// <summary>Return a scope that measures time until disposed</summary>
		public Scope TimingScope(string msg)
		{
			var sw = new Stopwatch();
			return Scope.Create(
				() => sw.Start(),
				() =>
				{
					sw.Stop();
					Debug.WriteLine("{0}: {1}".Fmt(msg, TimeSpan.FromMilliseconds(sw.ElapsedMilliseconds).ToPrettyString()));
				});
		}
	}
}

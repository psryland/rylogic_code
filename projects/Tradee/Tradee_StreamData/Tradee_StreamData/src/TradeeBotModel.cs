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
using pr.container;
using pr.extn;
using pr.util;
using Tradee;

namespace cAlgo
{
	public class TradeeBotModel :IDisposable
	{
		/// <summary>The cAlgo connection</summary>
		private readonly Robot m_calgo;

		/// <summary>The main thread dispatcher</summary>
		private Dispatcher m_main_thread;

		public TradeeBotModel(Robot calgo, Settings settings)
		{
			m_calgo = calgo;
			m_main_thread = Dispatcher.CurrentDispatcher;
			Settings = settings;

			// Connect to 'Tradee'
			Tradee = new TradeeProxy(DispatchMsg);

			// The collection of transmitters
			Transmitters = new BindingSource<Transmitter> { DataSource = new BindingListEx<Transmitter>() };
			foreach (var trans in Settings.Transmitters)
				Transmitters.Add(new Transmitter(this, trans.Pair, trans));

			//try
			//{
			//	// Initial information
			//	SendAccountStatus();
			//}
			//catch {}
		}
		public virtual void Dispose()
		{
			// Don't save the removed transmitters
			Settings.AutoSaveOnChanges = false;
			Util.DisposeAll(Transmitters);
			Tradee = null;
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			get;
			private set;
		}

		/// <summary>The connection to the Tradee application</summary>
		public TradeeProxy Tradee
		{
			get { return m_tradee; }
			private set
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
			get { return m_bs_transmitters; }
			set
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

		/// <summary>Enumerate all time frames</summary>
		public IEnumerable<TimeFrame> AllTimeFrames
		{
			get
			{
				foreach (var tf in Enum<ETimeFrame>.Values)
				{
					var time_frame = tf.ToCAlgoTimeframe();
					if (time_frame == null) continue;
					yield return time_frame;
				}
			}
		}

		/// <summary>Raised when data is sent to Tradee</summary>
		public event EventHandler DataPosted;

		/// <summary>Add a new transmitter (unless it already exists)</summary>
		public void AddTransmitter(ETradePairs pair)
		{
			// If the pair is already in the list, set it as the current pair
			var idx = Transmitters.IndexOf(x => x.Pair == pair);
			if (idx != -1)
			{
				Transmitters.Position = idx;
				return;
			}

			// Look for settings for this pair
			var s_idx = Settings.Transmitters.IndexOf(x => x.Pair == pair);
			if (s_idx == -1)
			{
				// If no settings exist, create some
				var arr = Settings.Transmitters;
				s_idx = Settings.Transmitters.Length;
				Array.Resize(ref arr, s_idx + 1);
				arr[s_idx] = new TransmitterSettings(pair) { TimeFrames = Settings.DefaultTimeFrames };
				Settings.Transmitters = arr;
			}

			Transmitters.Add(new Transmitter(this, pair, Settings.Transmitters[s_idx]));
		}

		/// <summary>Return the symbol for a given code</summary>
		public Symbol GetSymbol(string code)
		{
			return m_calgo.MarketData.GetSymbol(code);
		}

		/// <summary>Return the series for a given symbol and time frame</summary>
		public MarketSeries GetSeries(Symbol sym, TimeFrame tf)
		{
			using (Scope.Create(() => Environment.TickCount, s => Trace.WriteLine("Get Series {0},{1} took {2}".Fmt(sym.Code, tf, TimeSpan.FromMilliseconds(Environment.TickCount - s)))))
				return m_calgo.MarketData.GetSeries(sym, tf);
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
					e.Item.Dispose();
					break;
				}
			}
		}



		/// <summary>Enumerate all trading pairs</summary>
		private IEnumerable<Symbol> AllTradePairs
		{
			get
			{
				foreach (var pair in Enum<ETradePairs>.Values)
					yield return m_calgo.MarketData.GetSymbol(pair.ToString());
			}
		}

		/// <summary>Send details of the current account</summary>
		private void SendAccountStatus()
		{
			var acct = new InMsg.AccountStatus();
			acct.Number                = m_calgo.Account.Number                ;
			acct.Balance               = m_calgo.Account.Balance               ;
			acct.BrokerName            = m_calgo.Account.BrokerName            ;
			acct.Currency              = m_calgo.Account.Currency              ;
			acct.Equity                = m_calgo.Account.Equity                ;
			acct.FreeMargin            = m_calgo.Account.FreeMargin            ;
			acct.IsLive                = m_calgo.Account.IsLive                ;
			acct.Leverage              = m_calgo.Account.Leverage              ;
			acct.Margin                = m_calgo.Account.Margin                ;
			acct.MarginLevel           = m_calgo.Account.MarginLevel           ;
			acct.UnrealizedGrossProfit = m_calgo.Account.UnrealizedGrossProfit ;
			acct.UnrealizedNetProfit   = m_calgo.Account.UnrealizedNetProfit   ;

			// Add up all the potential losses from current positions
			foreach (var position in m_calgo.Positions)
			{
				var symbol = m_calgo.MarketData.GetSymbol(position.SymbolCode);
				if (position.StopLoss == null)
					continue;

				var pips = (position.EntryPrice - position.StopLoss.Value) * position.Volume;
				acct.PositionRisk += pips * symbol.PipValue / symbol.PipSize;
			}

			// Add up all potential losses from orders
			foreach (var order in m_calgo.PendingOrders)
			{
				var symbol = m_calgo.MarketData.GetSymbol(order.SymbolCode);
				if (order.StopLoss == null)
					continue;

				var pips = order.TradeType == TradeType.Buy ? (order.TargetPrice - order.StopLoss.Value) * order.Volume : (order.StopLoss.Value - order.TargetPrice) * order.Volume;
				acct.OrderRisk += pips * symbol.PipValue / symbol.PipSize;
			}

			Tradee.Post(acct);
		}


		/// <summary>Handle messages from Tradee</summary>
		private void DispatchMsg(object msg)
		{
			switch (msg.GetType().Name)
			{
			default:
				{
					Trace.WriteLine("Unknown Message Type {0} received".Fmt(msg.GetType().Name));
					break;
				}
			case "OutMsg.TestMsg":
				{
					break;
				}
			}
		}

		/// <summary>Marshal a function call to the main thread</summary>
		public void RunOnMainThread(Action action)
		{
			if (m_main_thread.Thread == Thread.CurrentThread)
				action();
			else
				m_main_thread.BeginInvoke(() => RunOnMainThread(action));
		}
	}
}

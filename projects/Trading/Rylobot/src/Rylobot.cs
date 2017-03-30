using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.extn;
using pr.util;

namespace Rylobot
{
	/// <summary>Base class for a Rylobot</summary>
	public abstract class Rylobot :Robot
	{
		// Notes:
		// Each bot uses a different time frame, has different optimisation parameters, etc.
		// This is a base class providing common functionality to all 'Rylobots'.

		/// <summary></summary>
		protected override void OnStart()
		{
			Debug.WriteLine("Rylobot is a {0} bit process".Fmt(Environment.Is64BitProcess?"64":"32"));
			base.OnStart();

			Risk = 0.1;
			TickNumber = 0;
			BalanceMinimum = 0;
			Label = GetType().Name;
			PositionManagers = new List<PositionManager>();

			// Load the global bot settings
			Settings = new Settings(Settings.DefaultFilepath);

			// Create the cache of symbol data
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Create the main instrument
			Instrument = new Instrument(this);

			// The sets of positions/orders created by this bot
			PositionIds = new HashSet<int>();
			OrderIds = new HashSet<int>();

			// Create the account manager and trade creator
			Broker = new Broker(this, Account);

			// Create a debugging helper
			Debugging = new Debugging(this);

			base.Positions.Opened += HandlePositionOpened;
			base.Positions.Closed += HandlePositionClosed;
		}
		protected override void OnStop()
		{
			// Notify stopping
			Stopping.Raise(this);

			base.Positions.Closed -= HandlePositionClosed;
			base.Positions.Opened -= HandlePositionOpened;

			// Output the edge for this strategy
			Debugging.ReportEdge(100);

			Util.DisposeAll(PositionManagers);
			Debugging = null;
			Broker = null;
			Instrument = null;
			Util.Dispose(ref m_sym_cache);

			base.OnStop();
		}
		protected sealed override void OnTick()
		{
			++TickNumber;
			Debugging.BreakOnPointOfInterest();

			// Emergency stop on large draw-down
			var max_loss_ratio = (1.0 - 0.01*Settings.MaxRiskPC);
			BalanceMinimum = Math.Max((double)BalanceMinimum, Account.Balance * max_loss_ratio);
			if (Account.Equity < BalanceMinimum)
			{
				Debugging.Trace("Account equity (${0}) dropped below the balance minimum (${1}). Stopping".Fmt(Account.Equity, BalanceMinimum));
				Print("Account equity (${0}) dropped below the balance minimum (${1}). Stopping".Fmt(Account.Equity, BalanceMinimum));
				CloseAllPositions();
				Stop();
				return;
			}

			// Raise the Bot.Tick event before stepping the bot
			// Instruments are signed up to the Tick event so they will be updated first
			base.OnTick();
			Tick.Raise(this);

			// Update the account info
			Broker.Update();

			// Step active position managers
			foreach (var pm in PositionManagers)
				pm.Step();

			// Step the bot
			Step();
		}

		/// <summary>Step the strategy</summary>
		protected abstract void Step();

		/// <summary>Debugging output</summary>
		public abstract void Dump();

		/// <summary>Debugging helper</summary>
		public Debugging Debugging
		{
			[DebuggerStepThrough] get { return m_debugging; }
			private set
			{
				if (m_debugging == value) return;
				Util.Dispose(ref m_debugging);
				m_debugging = value;
			}
		}
		private Debugging m_debugging;

		/// <summary>New data arriving</summary>
		public event EventHandler Tick;

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The current server time</summary>
		public DateTimeOffset UtcNow
		{
			get { return Server.Time; }
		}

		/// <summary>Incremented whenever OnTick is called</summary>
		public int TickNumber
		{
			get;
			private set;
		}

		/// <summary>Manages creating trades and managing the risk level</summary>
		public Broker Broker
		{
			[DebuggerStepThrough] get { return m_broker; }
			private set
			{
				if (m_broker == value) return;
				Util.Dispose(ref m_broker);
				m_broker = value;
			}
		}
		private Broker m_broker;

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			private set
			{
				if (m_instr == value) return;
				Util.Dispose(ref m_instr);
				m_instr = value;
			}
		}
		private Instrument m_instr;

		/// <summary>The positions created by 'Bot'</summary>
		public HashSet<int> PositionIds { get; private set; }

		/// <summary>The orders created by 'Bot'</summary>
		public HashSet<int> OrderIds { get; private set; }

		/// <summary>Active position managers</summary>
		public List<PositionManager> PositionManagers
		{
			get;
			private set;
		}

		/// <summary>The account balance used for emergency stop</summary>
		public AcctCurrency BalanceMinimum
		{
			get;
			private set;
		}

		/// <summary>All open positions on the account</summary>
		public Positions AllPositions
		{
			get { return base.Positions; }
		}

		/// <summary>All pending orders on the account</summary>
		public PendingOrders AllPendingOrders
		{
			get { return base.PendingOrders; }
		}

		/// <summary>All positions and pending orders on the account</summary>
		public IEnumerable<Order> AllOrders
		{
			get
			{
				foreach (var pos in AllPositions)
					yield return new Order(pos);
				foreach (var ord in AllPendingOrders)
					yield return new Order(ord);
			}
		}

		/// <summary>Return the positions created by this bot</summary>
		public new IEnumerable<Position> Positions
		{
			get { return AllPositions.Where(x => PositionIds.Contains(x.Id)); }
		}

		/// <summary>Return pending orders created by this bot</summary>
		public new IEnumerable<PendingOrder> PendingOrders
		{
			get { return AllPendingOrders.Where(x => OrderIds.Contains(x.Id)); }
		}

		/// <summary>Positions and pending orders created by this bot</summary>
		public IEnumerable<Order> Orders
		{
			get
			{
				foreach (var pos in Positions)
					yield return new Order(pos);
				foreach (var ord in PendingOrders)
					yield return new Order(ord);
			}
		}

		/// <summary>A label for this bot</summary>
		public string Label
		{
			get;
			private set;
		}

		/// <summary>The current signed net position volume</summary>
		public long NetVolume
		{
			get { return Positions.Sum(x => x.Sign() * x.Volume); }
		}

		/// <summary>The direction of increasing profit</summary>
		public int ProfitSign
		{
			get { return Math.Sign(NetVolume); }
		}

		/// <summary>Return the net profit of positions created by this bot</summary>
		public AcctCurrency NetProfit
		{
			get { return Positions.Sum(x => x.NetProfit); }
		}

		/// <summary>The risk level to use for this bot</summary>
		public virtual double Risk
		{
			get; set;
		}

		/// <summary>Return the symbol for a given symbol code or null if invalid or unavailable</summary>
		public Symbol GetSymbol(string symbol_code)
		{
			// Quick out if the requested symbol is the one this bot is running on
			if (symbol_code == Symbol.Code)
				return Symbol;

			// Otherwise, request the other symbol data
			return m_sym_cache.Get(symbol_code, c =>
			{
				Symbol res = null;
				using (var wait = new ManualResetEvent(false))
				{
					BeginInvokeOnMainThread(() =>
					{
						res = MarketData.GetSymbol(symbol_code);
						wait.Set();
					});
					wait.WaitOne(TimeSpan.FromSeconds(30));
					return res;
				}
			});
		}
		private Cache<string,Symbol> m_sym_cache;

		/// <summary>Return the series for a given symbol and time frame</summary>
		public MarketSeries GetSeries(Symbol sym, TimeFrame tf)
		{
			if (MarketSeries.SymbolCode == sym.Code && MarketSeries.TimeFrame == tf)
				return MarketSeries;

			if (IsBacktesting)
				throw new Exception("This can't be used in back testing");

			MarketSeries res = null;
			using (var wait = new ManualResetEvent(false))
			{
				BeginInvokeOnMainThread(() =>
				{
					res = MarketData.GetSeries(sym, tf);
					wait.Set();
				});
				wait.WaitOne();
				return res;
			}
		}

		/// <summary>Called when a position is opened</summary>
		protected new virtual void OnPositionOpened(Position position)
		{}
		private void HandlePositionOpened(PositionOpenedEventArgs args)
		{
			// Only watch positions created by this bot
			var position = args.Position;
			if (!PositionIds.Contains(position.Id))
				return;

			//Correlator.Track(position, CorrFactor.SL, position.StopLossRel());
			//Correlator.Track(position, CorrFactor.TP, position.TakeProfitRel());
			//Correlator.Track(position, CorrFactor.RtR, position.TakeProfitRel() / position.StopLossRel());
			//Correlator.Track(position, CorrFactor.Volume, position.Volume);

			// Notify position closed
			if (PositionOpened != null) PositionOpened(this, args);
			OnPositionOpened(position);
		}

		/// <summary>Called when a position closes</summary>
		protected new virtual void OnPositionClosed(Position position)
		{}
		private void HandlePositionClosed(PositionClosedEventArgs args)
		{
			// Only watch positions created by this strategy
			var position = args.Position;
			if (!PositionIds.Contains(position.Id))
				return;

			//// Track the trade result
			//Correlator.Result(position);

			// Remove any position managers that are managing 'position'
			PositionManagers.RemoveIf(x => x.Position.Id == position.Id);

			// Notify position closed
			if (PositionClosed != null) PositionClosed(this, args);
			OnPositionClosed(position);

			// Not one of ours now
			PositionIds.Remove(position.Id);
		}

		/// <summary>Position, belonging to this bot, opened</summary>
		public event EventHandler<PositionOpenedEventArgs> PositionOpened;

		/// <summary>Position, belonging to this bot, closed</summary>
		public event EventHandler<PositionClosedEventArgs> PositionClosed;

		/// <summary>Bot shutting down</summary>
		public event EventHandler Stopping;

		#region Position Operations

		/// <summary>Close all positions created by this bot</summary>
		public void CloseAllPositions()
		{
			Broker.ClosePositions(Positions);
		}

		/// <summary>Cancel all pending orders created by this bot</summary>
		public void CancelAllPendingOrders()
		{
			Broker.CancelPendingOrders(PendingOrders);
		}

		/// <summary>Look for positions that cancel out and close them, ensuring a net profit</summary>
		public void CancelOutPositions(int trend_sign)
		{
			// Get the winning and losing positions
			var winners = Positions.Where(x => x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToList();                           // From biggest to smallest
			var losers  = Positions.Where(x => x.Sign() != trend_sign && x.NetProfit < 0).OrderBy(x => +x.NetProfit).ToList(); // From worst to best
			if (winners.Count != 0 && losers.Count != 0)
			{
				// Find the totals of winning and losing positions
				var win_total = winners.Sum(x => x.NetProfit);
				var los_total = losers.Sum(x => -x.NetProfit);

				// More winners than losers, close as few winners as needed
				if (win_total > los_total)
				{
					for (int i = winners.Count; i-- != 0;)
					{
						if (los_total > win_total - winners[i].NetProfit) break;
						win_total -= winners[i].NetProfit;
						winners.RemoveAt(i);
					}
				}

				// More losers than winners, close as many losers as we can
				else if (los_total > win_total)
				{
					los_total = 0;
					for (int i = 0; i != losers.Count; ++i)
					{
						// Find the first loser less than 'win_total'
						for (;losers.Count != i;)
						{
							if (los_total + -losers[i].NetProfit < win_total) break;
							losers.RemoveAt(i);
						}
						if (losers.Count == i) break;
						los_total += -losers[i].NetProfit;
					}
				}

				// Anything cancel out?
				if (winners.Count != 0 && losers.Count != 0)
				{
					var bal = winners.Sum(x => x.NetProfit) - losers.Sum(x => -x.NetProfit);
					Debugging.Trace("Cancelling positions! Net: {0}".Fmt(bal));
					Debug.Assert(bal >= 0.0);

					Broker.ClosePositions(winners);
					Broker.ClosePositions(losers);
				}
			}
		}

		/// <summary>True if there are open positions within a range of 'price'</summary>
		public bool NearbyPositions(string label, QuoteCurrency price, double min_trade_dist, int sign = 0)
		{
			return sign == 0
				? Positions.Any(x => Math.Abs(x.EntryPrice - price) < min_trade_dist)
				: Positions.Any(x => Math.Abs(x.EntryPrice - price) < min_trade_dist && x.Sign() == sign);
		}

		#endregion
	}

	/// <summary>A template for a bot</summary>
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	internal class Rylobot_Template :Rylobot
	{
		#region Parameters
		#endregion

		protected override void OnStart()
		{
			base.OnStart();
		}
		protected override void OnStop()
		{
			base.OnStop();
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument);
		}

		/// <summary>Strategy step</summary>
		protected override void Step()
		{
		}

		/// <summary>Position opened</summary>
		protected override void OnPositionOpened(Position position)
		{}

		/// <summary>Position closed</summary>
		protected override void OnPositionClosed(Position position)
		{}
	}
}




// Displaying a UI
#if false
		private RylobotUI m_ui;
		private Thread m_thread;
		private ManualResetEvent m_running;
		protected override void OnStart()
		{
			try
			{
				m_running = new ManualResetEvent(false);

				// Create a thread and launch the Rylobot UI in it
				m_thread = new Thread(Main) 
				{
					Name = "Rylobot"
				};
				m_thread.SetApartmentState(ApartmentState.STA);
				m_thread.Start();

				m_running.WaitOne(1000);
			} catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				m_thread = null;
				Stop();
			}
		}
		protected override void OnStop()
		{
			base.OnStop();
			try
			{
				if (m_ui != null && !m_ui.IsDisposed && m_ui.IsHandleCreated)
					m_ui.BeginInvoke(() => m_ui.Close());
			} catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
		protected override void OnTick()
		{
			base.OnTick();
			m_ui.Invoke(() => m_ui.Model.OnTick());
		}
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			m_ui.Invoke(() => m_ui.Model.OnPositionClosed(position));
		}

		/// <summary>Thread entry point</summary>
		[STAThread()]
		private void Main()
		{
			try
			{
				Trace.WriteLine("RylobotUI Created");

				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				using (m_ui = new RylobotUI(this))
				{
					m_running.Set();
					Application.Run(m_ui);
				}

				Stop();
			} catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
			}
		}
#endif
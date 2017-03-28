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
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot :Robot
	{
		/// <summary>Singleton access</summary>
		public static Rylobot Instance { get; private set; }

		/// <summary></summary>
		protected override void OnStart()
		{
			Debug.WriteLine("Rylobot is a {0} bit process".Fmt(Environment.Is64BitProcess?"64":"32"));
			base.OnStart();
			Instance = this;
			TickNumber = 0;
			BalanceMinimum = 0;

			// Load the bot settings
			Settings = new Settings(Settings.DefaultFilepath);
			Settings.Save();

			// Create the cache of symbol data
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Create the main instrument
			Instrument = new Instrument(this);

			// Create the account manager and trade creator
			Broker = new Broker(this, Account);

			// Enable capture of trades to Ldr files
			Debugging.LogTrades = true;

			// If capture price data is enabled, just do that
			if (CapturePriceData(ECapturePriceData.Start))
				return;

			// Create the strategies to use
			Strats = new List<Strategy>();
			//Strats.Add(new StrategyMain(this));
			//Strats.Add(new StrategyPriceDistribution(this, 0.2)); // This is nearly working
			//Strats.Add(new StrategyMAExtreme(this, 0.2));
			//Strats.Add(new StrategyBreakOut(this, 0.2));
			//Strats.Add(new StrategyRevenge(this));
			//Strats.Add(new StrategyTrend(this));
			//Strats.Add(new StrategyHedge(this));
			//Strats.Add(new StrategyHedge2(this));
			//Strats.Add(new StrategyEmaCross(this));
			//Strats.Add(new StrategySpike(this));
			Strats.Add(new StrategyPeaks(this, 0.2));

			// Todo:
			// Track the profit of each strategy, if losing, automatically
			// reduce it's risk allocation.
			Debug.Assert(Strats.Sum(x => x.Risk) < 1.001);
		}
		protected override void OnStop()
		{
			// If capture price data is enabled, just do that
			if (CapturePriceData(ECapturePriceData.Stop))
				return;

			Stopping.Raise(this);

			// Stop capturing trades
			Debugging.LogTrades = false;

			Strats = null;
			Broker = null;
			Instrument = null;
			Util.Dispose(ref m_sym_cache);

			Instance = null;
			base.OnStop();
		}
		protected override void OnTick()
		{
			++TickNumber;
			Debugging.BreakOnPointOfInterest();

			// If capture price data is enabled, just do that
			if (CapturePriceData(ECapturePriceData.Tick))
				return;

			// Emergency stop
			var max_loss_ratio = (1.0 - 0.01*Settings.MaxRiskPC);
			BalanceMinimum = Math.Max((double)BalanceMinimum, Account.Balance * max_loss_ratio);
			if (Account.Equity < BalanceMinimum)
			{
				Debugging.Trace("Account equity (${0}) dropped below the balance minimum (${1}). Stopping".Fmt(Account.Equity, BalanceMinimum));
				Print("Account equity (${0}) dropped below the balance minimum (${1}). Stopping".Fmt(Account.Equity, BalanceMinimum));
				Broker.CloseAllPositions();
				Stop();
				return;
			}

			// Raise the Bot.Tick event before stepping the strategy
			// Instruments are signed up to the Tick event so they will be updated first
			base.OnTick();
			Tick.Raise(this);

			// Update the account info
			Broker.Update();

			// Sort the strategies by score.
			Strats.Sort((l,r) => l.SuitabilityScore.CompareTo(r.SuitabilityScore));

			// Step them in order of highest to lowest so that the best strategies
			// get to create positions in preference to less suited strategies.
			foreach (var strat in Strats)
			{
				// If the strategy has not positions or pending orders and is not suitable, skip it
				if (!strat.Positions.Any() &&
					!strat.PendingOrders.Any() &&
					strat.SuitabilityScore == 0)
					continue;

				strat.Step();
			}
		}

		/// <summary>New data arriving</summary>
		public event EventHandler Tick;

		/// <summary>Bot shutting down</summary>
		public event EventHandler Stopping;

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

		/// <summary>All the active strategies</summary>
		public List<Strategy> Strats
		{
			[DebuggerStepThrough] get { return m_strats; }
			private set
			{
				if (m_strats == value) return;
				Util.DisposeAll(m_strats);
				m_strats = value;
			}
		}
		private List<Strategy> m_strats;

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

		/// <summary>The account balance used for emergency stop</summary>
		public AcctCurrency BalanceMinimum
		{
			get;
			private set;
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

		#region Capture Price Data

		/// <summary>Used to log price data to a file</summary>
		private static bool CapturePriceDataEnabled = false;

		/// <summary>States</summary>
		private enum ECapturePriceData { Start, Tick, Stop }

		/// <summary>File of price tick data</summary>
		private StreamWriter m_pd_out;

		/// <summary>File of candle tick data</summary>
		private StreamWriter m_cd_out;

		/// <summary>Log all price data to files</summary>
		private bool CapturePriceData(ECapturePriceData state)
		{
			// Not enabled, return
			if (!CapturePriceDataEnabled)
				return false;

			// Write a line to the candle data file
			Action<int> WriteCandleTick = candle_index =>
			{
				m_cd_out.WriteLine(Str.Build(
					TickNumber,",",
					candle_index,",",
					MarketSeries.OpenTime  [candle_index].Ticks,",",
					MarketSeries.Open      [candle_index],",",
					MarketSeries.High      [candle_index],",",
					MarketSeries.Low       [candle_index],",",
					MarketSeries.Close     [candle_index],",",
					MarketSeries.Median    [candle_index],",",
					MarketSeries.TickVolume[candle_index]));
			};

			// Write a line to the price tick data file
			Action WritePriceTick = () =>
			{
				m_pd_out.WriteLine(Str.Build(
					TickNumber,",",
					UtcNow.Ticks,",",
					Symbol.Ask,",",
					Symbol.Bid));
			};

			switch (state)
			{
			case ECapturePriceData.Start:
				{
					var dir = Debugging.FP("Capture\\{0} - {1}".Fmt(Symbol.Code, TimeFrame.ToString()));
					if (!Path_.DirExists(dir))
						Directory.CreateDirectory(dir);

					// Create a file for candle tick data
					m_cd_out = new StreamWriter(new FileStream(Path_.CombinePath(dir,"candle_data.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));
					m_cd_out.WriteLine("TickNumber,CandleIndex,OpenTime,Open,High,Low,Close,Median,TickVolume");
					
					// Log the initial market series data
					for (int i = 0; i != MarketSeries.OpenTime.Count; ++i)
						WriteCandleTick(i);

					// Create a file for the price tick data
					m_pd_out = new StreamWriter(new FileStream(Path_.CombinePath(dir,"price_data.csv"), FileMode.Create, FileAccess.Write, FileShare.Read));
					m_pd_out.WriteLine("TickNumber,Timestamp,Ask,Bid");

					// Write the initial price
					m_pd_out.WriteLine(Str.Build(TickNumber,",",UtcNow.Ticks,",",Symbol.Ask,",",Symbol.Bid));

					// Create a file with symbol info
					using (var sym = new StreamWriter(new FileStream(Path_.CombinePath(dir,"symbol.csv"), FileMode.Create, FileAccess.Write, FileShare.Read)))
					{
						sym.WriteLine(Str.Build("Code"           ,",", Symbol.Code           ));
						sym.WriteLine(Str.Build("Digits"         ,",", Symbol.Digits         ));
						sym.WriteLine(Str.Build("LotSize"        ,",", Symbol.LotSize        ));
						sym.WriteLine(Str.Build("PipSize"        ,",", Symbol.PipSize        ));
						sym.WriteLine(Str.Build("PipValue"       ,",", Symbol.PipValue       ));
						sym.WriteLine(Str.Build("PreciseLeverage",",", Symbol.PreciseLeverage));
						sym.WriteLine(Str.Build("TickSize"       ,",", Symbol.TickSize       ));
						sym.WriteLine(Str.Build("TickValue"      ,",", Symbol.TickValue      ));
						sym.WriteLine(Str.Build("VolumeMax"      ,",", Symbol.VolumeMax      ));
						sym.WriteLine(Str.Build("VolumeMin"      ,",", Symbol.VolumeMin      ));
						sym.WriteLine(Str.Build("VolumeStep"     ,",", Symbol.VolumeStep     ));
					}
					break;
				}
			case ECapturePriceData.Tick:
				{
					WriteCandleTick(MarketSeries.OpenTime.Count - 1);
					WritePriceTick();
					break;
				}
			case ECapturePriceData.Stop:
				{
					// Close files
					Util.Dispose(ref m_pd_out);
					Util.Dispose(ref m_cd_out);
					break;
				}
			}
			return true;
		}

		#endregion
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

			// Load the bot settings
			var settings_filepath = Util.ResolveAppDataPath("Rylogic","Rylobot","Settings.xml");
			Settings = new Settings(settings_filepath){AutoSaveOnChanges = true};
			Settings.Save();

			// Create the cache of symbol data
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Create the account manager and trade creator
			Broker = new Broker(this, Account);

			// Create the strategies to use
			Strats = new List<Strategy>();
			Strats.Add(new StrategyMain(this));
			//Strats.Add(new StrategyRevenge(this));
			//Strats.Add(new StrategyTrend(this));
			//Strats.Add(new StrategyHedge(this));

			// Enable capture of trades to Ldr files
			Debugging.LogTrades(this, true);
		}
		protected override void OnStop()
		{
			Stopping.Raise(this);

			// Stop capturing trades
			Debugging.LogTrades(this, false);

			// Log the whole instrument
			Debugging.Dump(new Instrument(this), emas:new[]{14, 200});

			Strats = null;
			Broker = null;
			Util.Dispose(ref m_sym_cache);

			Instance = null;
			base.OnStop();
		}
		protected override void OnTick()
		{
			// Raise the Bot.Tick event before stepping the strategy
			// Instruments are signed up to the Tick event so they will be updated first
			base.OnTick();
			Tick.Raise(this);

			// Update the account info
			Broker.Update();

			// Sort the strategies by score and step them in order of highest to lowest so that
			// the best strategies get to create positions in preference to less suited strategies.
			Strats.Sort((l,r) => l.SuitabilityScore.CompareTo(r.SuitabilityScore));
			foreach (var strat in Strats)
				strat.Step();
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
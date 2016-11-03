using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using cAlgo.API;
using cAlgo.API.Internals;
using pr.common;
using pr.extn;
using pr.util;
using pr.win32;

namespace Rylobot
{
	[Robot(TimeZone = TimeZones.UTC, AccessRights = AccessRights.FullAccess)]
	public class Rylobot :Robot
	{
		/// <summary></summary>
		protected override void OnStart()
		{
			Debug.WriteLine("Rylobot is a {0} bit process".Fmt(Environment.Is64BitProcess?"64":"32"));

			base.OnStart();

			// Load the bot settings
			var settings_filepath = Util.ResolveAppDataPath("Rylogic","Rylobot","Settings.xml");
			Settings = new Settings(settings_filepath){AutoSaveOnChanges = true};
			Settings.Save();

			// Create the cache of symbol data
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };

			// Create the guy responsible for selecting the trading strategy
			Selector = new StrategySelector(this);

			// Create the account manager and trade creator
			Broker = new Broker(this, Account);
		}
		protected override void OnStop()
		{
			Stopping.Raise(this);

			Strat = null;
			Selector = null;
			Broker = null;
			Util.Dispose(ref m_sym_cache);
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

			// Is it time to change strategy?
			Strat = Selector.Step(Strat);

			// Step the current strategy
			Strat.Step();
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

		/// <summary>Manages choosing a trading strategy</summary>
		public StrategySelector Selector
		{
			get;
			private set;
		}

		/// <summary>The current trading strategy</summary>
		public Strategy Strat
		{
			get { return m_strat; }
			private set
			{
				if (m_strat == value) return;
				Util.Dispose(ref m_strat);
				m_strat = value;
			}
		}
		private Strategy m_strat;

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
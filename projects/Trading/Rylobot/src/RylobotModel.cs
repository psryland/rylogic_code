using System;
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
	public class RylobotModel :IDisposable
	{
		public RylobotModel(Robot robot)
		{
			m_sym_cache = new Cache<string, Symbol> { ThreadSafe = true , Mode = CacheMode.StandardCache };
			Robot = robot;

			var settings_filepath = Util.ResolveAppDataPath("Rylogic","Rylobot","Settings.xml");
			Settings = new Settings(settings_filepath){AutoSaveOnChanges = true};

			// Ensure the instrument settings directory exists
			if (!Path_.DirExists(Settings.General.InstrumentSettingsDir))
				Directory.CreateDirectory(Settings.General.InstrumentSettingsDir);

			// Create the account
			Acct = new Account(this, Robot.Account);

			// Create the instrument
			Instrument = new Instrument(this);

			// Create a strategy
			Strategy = new StrategyPotLuck(this);

			// ToDo:
			// Restore SnR levels + other indicators
		}
		public void Dispose()
		{
			Strategy = null;
			Instrument = null;
			Acct = null;
			Robot = null;
			Util.Dispose(ref m_sym_cache);
		}

		/// <summary>App settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>Access to the cAlgo API</summary>
		public Robot Robot
		{
			[DebuggerStepThrough] get { return m_impl_robot; }
			private set
			{
				if (m_impl_robot == value) return;
				if (m_impl_robot != null) { }
				m_impl_robot = value;
				if (m_impl_robot != null) { }
			}
		}
		private Robot m_impl_robot;

		/// <summary>The operating account</summary>
		public Account Acct
		{
			[DebuggerStepThrough] get { return m_acct; }
			private set
			{
				if (m_acct == value) return;
				m_acct = value;
			}
		}
		private Account m_acct;

		/// <summary>The instrument being traded</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			private set
			{
				if (m_instr == value) return;
				if (m_instr != null) { }
				m_instr = value;
				if (m_instr != null) { }
			}
		}
		private Instrument m_instr;

		/// <summary>The current active strategy</summary>
		public Strategy Strategy
		{
			[DebuggerStepThrough] get { return m_strategy; }
			set
			{
				if (m_strategy == value) return;
				Util.Dispose(ref m_strategy);
				m_strategy = value;
			}
		}
		private Strategy m_strategy;

		/// <summary>Return the symbol for a given symbol code or null if invalid or unavailable</summary>
		public Symbol GetSymbol(string symbol_code)
		{
			return m_sym_cache.Get(symbol_code, c =>
			{
				Symbol res = null;
				using (var wait = new ManualResetEvent(false))
				{
					Robot.BeginInvokeOnMainThread(() =>
					{
						res = Robot.MarketData.GetSymbol(symbol_code);
						wait.Set();
					});
					wait.WaitOne(TimeSpan.FromSeconds(30));
					return res;
				}
			});
		}
		private Cache<string,Symbol> m_sym_cache;

		/// <summary>Raised when a position is closed</summary>
		public event EventHandler<PositionEventArgs> PositionClosed;
		public void OnPositionClosed(Position position)
		{
			PositionClosed.Raise(new PositionEventArgs(position));
		}

		/// <summary>Update the non-instrument related things</summary>
		public void Step()
		{
			Acct.Update();
		}

		/// <summary>Called when the bot ticks (i.e. update instrument data)</summary>
		public void OnTick()
		{
			// Update the instrument
			Instrument.OnTick();

			// Advance the current strategy
			if (Strategy != null)
				Strategy.Step();
		}
	}

	#region Event Args
	public class PositionEventArgs :EventArgs
	{
		public PositionEventArgs(Position position)
		{
			Position = position;
		}
		public Position Position { get; private set; }
	}
	#endregion
}

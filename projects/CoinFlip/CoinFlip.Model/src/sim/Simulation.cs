using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A container for the data maintained by the back testing simulation</summary>
	public class Simulation :IDisposable
	{
		public Simulation(Model model)
		{
			m_main_loop_sw = new Stopwatch();
			Exch = new Dictionary<string, SimExchange>();
			Model = model;
			Clock = StartTime;
			TimeFrame = Settings.TimeFrame;

			// Create a SimExchange for each exchange
			foreach (var exch in Model.Exchanges)
				Exch[exch.Name] = new SimExchange(this, exch);
		}
		public void Dispose()
		{
			Running = false;
			Exch = null;
			Model = null;
		}

		/// <summary>Simulated Exchanges</summary>
		private Dictionary<string, SimExchange> Exch
		{
			[DebuggerStepThrough] get { return m_exchanges; }
			set
			{
				if (m_exchanges == value) return;
				Util.DisposeRange(m_exchanges?.Values);
				m_exchanges = value;
			}
		}
		private Dictionary<string, SimExchange> m_exchanges;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Settings.BackTesting.SettingChanged -= HandleSettingChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Settings.BackTesting.SettingChanged += HandleSettingChanged;
				}

				// Handlers
				void HandleSettingChanged(object sender, SettingChangedEventArgs args)
				{
					switch (args.Key)
					{
					case nameof(Settings.TimeFrame):
						{
							if (Running)
								throw new Exception("Don't change the time frame while the simulation is running");

							// Update the data sources in each exchange
							foreach (var exch in Exch.Values)
								exch.FindDataSources();

							break;
						}
					}
				}
			}
		}
		private Model m_model;

		/// <summary>Back testing related settings</summary>
		public Settings.BackTestingSettings Settings
		{
			get { return Model.Settings.BackTesting; }
		}

		/// <summary>Get the current simulation time</summary>
		public DateTimeOffset Clock { get; private set; }

		/// <summary>The simulation step resolution</summary>
		public ETimeFrame TimeFrame { get; private set; }

		/// <summary>Get the back testing start time</summary>
		public DateTimeOffset StartTime
		{
			get
			{
				// This can't be relative to the latest candle in the database because the different
				// exchanges may have different amounts of data. Use a fix point in time (relative to
				// the current time)
				var steps = Settings.Steps;
				var tf = Settings.TimeFrame;
				var now = Misc.Round(DateTimeOffset.UtcNow, tf);
				return now - Misc.TimeFrameToTimeSpan(steps, tf);
			}
		}

		/// <summary>Set the back testing start time to 'now - steps * time_frame'</summary>
		public void SetStartTime(ETimeFrame time_frame, int steps)
		{
			Settings.TimeFrame = time_frame;
			Settings.Steps = steps;

			// Reset the sim clock to the start time
			Reset();
		}

		/// <summary>Raised when the simulation is reset to the start time</summary>
		public event EventHandler<SimResetEventArgs> SimReset;

		/// <summary>Raised when the simulation time changes</summary>
		public event EventHandler<SimStepEventArgs> SimStep;

		/// <summary>Access a simulated exchange by name</summary>
		public SimExchange this[Exchange exch]
		{
			[DebuggerStepThrough] get { return Exch[exch.Name]; }
		}

		/// <summary>Reset the sim back to the start time</summary>
		public void Reset()
		{
			// All bots must be stopped and restarted
			var active_bots = Model.Bots.Where(x => x.Active).ToArray();
			foreach (var bot in active_bots)
				bot.Active = false;

			// Reset the sim time back to 'StartTime'
			m_main_loop_sw.Reset();
			RunMode = ERunMode.Stopped;
			m_main_loop_last_step = StartTime.Ticks;
			TimeFrame = Settings.TimeFrame;
			Clock = StartTime;

			// Reset the exchanges
			foreach (var exch in Exch.Values)
				exch.Reset();

			// Restart the bots that were started
			foreach (var bot in active_bots)
				bot.Active = true;

			// Notify of reset
			SimReset.Raise(this, new SimResetEventArgs(Settings.Steps, Settings.TimeFrame, StartTime));
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void StepOne()
		{
			RunMode = ERunMode.StepOne;
			Running = true;
		}

		/// <summary>Advance the simulation to the next submitted or filled trade</summary>
		public void RunToTrade()
		{
			RunMode = ERunMode.RunToTrade;
			Running = true;
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void Run()
		{
			Model.Log.Write(ELogLevel.Debug, "Simulation started");
			RunMode = ERunMode.Continuous;
			Running = true;
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void Pause()
		{
			Running = false;
			Model.Log.Write(ELogLevel.Debug, "Simulation stopped");
		}

		/// <summary>The thread the runs the simulation</summary>
		public bool Running
		{
			get { return m_timer != null; }
			private set
			{
				if (Running == value) return;
				Debug.Assert(Model.AssertMainThread());

				SimRunningChanged.Raise(this, new PrePostEventArgs(after:false));
				if (Running)
				{
					m_main_loop_sw.Stop();
					m_timer.Stop();
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (Running)
				{
					m_timer.Start();
					m_main_loop_sw.Start();
					m_max_ticks_per_step =  Misc.TimeFrameToTicks(1.0 / Settings.StepsPerCandle, Settings.TimeFrame);
				}
				SimRunningChanged.Raise(this, new PrePostEventArgs(after:true));

				// Handlers
				void HandleTick(object sender, EventArgs args)
				{
					// Advance the simulation clock at a rate of 'Settings.StepRate' candles per second.
					// 'm_main_loop_sw' tracks the amount of real world time that has elapsed while 'Running' is true
					switch (RunMode)
					{
					case ERunMode.Continuous:
					case ERunMode.RunToTrade:
						{
							var elapsed_ticks = Math.Max(m_max_ticks_per_step, m_main_loop_sw.ElapsedTicks - m_main_loop_last_ticks);
							m_main_loop_last_ticks = m_main_loop_sw.ElapsedTicks;
							Clock += new TimeSpan(elapsed_ticks);
							break;
						}
					case ERunMode.StepOne:
						{
							// If this is a single step, increment the clock by a fixed amount
							Clock += new TimeSpan(m_max_ticks_per_step);
							break;
						}
					}

					// Stop when the clock has caught up to real time or we've done enough steps
					if (Clock > DateTimeOffset.UtcNow)
					{
						Running = false;
						return;
					}

					// Run the sim exchanges at top speed
					foreach (var exch in Exch.Values)
						exch.Step();

					// See if it's time for a sim step
					var elapsed = Clock.Ticks - m_main_loop_last_step;
					if (elapsed < m_max_ticks_per_step) return;
					m_main_loop_last_step += m_max_ticks_per_step;

					// Notify the sim step. This causes PriceData's to "add" candles
					SimStep.Raise(this, new SimStepEventArgs(Clock));

					// Run the required number of steps
					if (RunMode == ERunMode.StepOne)
						Running = false;
				}
			}
		}
		private DispatcherTimer m_timer;
		private Stopwatch m_main_loop_sw;
		private long m_max_ticks_per_step;
		private long m_main_loop_last_step;
		private long m_main_loop_last_ticks;

		/// <summary>Modes to run the simulation in</summary>
		public ERunMode RunMode { get; private set; }
		public enum ERunMode
		{
			Stopped,
			Continuous,
			StepOne,
			RunToTrade,
		}

		/// <summary>Raised when the simulation starts/stops</summary>
		public event EventHandler<PrePostEventArgs> SimRunningChanged;
	}

	#region Event Args
	public class SimResetEventArgs :EventArgs
	{
		public SimResetEventArgs(int steps_ago, ETimeFrame tf, DateTimeOffset start_time)
		{
			StepsAgo = steps_ago;
			TimeFrame = tf;
			StartTime = start_time;
		}

		/// <summary>The number of 'TimeFrame' steps in the past corresponding to 'StartTime'</summary>
		public int StepsAgo { get; private set; }

		/// <summary>The resolution of the simulation steps</summary>
		public ETimeFrame TimeFrame { get; private set; }

		/// <summary>The start time of the simulation</summary>
		public DateTimeOffset StartTime { get; private set; }
	}
	public class SimStepEventArgs :EventArgs
	{
		public SimStepEventArgs(DateTimeOffset clock)
		{
			Clock = clock;
		}

		/// <summary>The simulation time</summary>
		public DateTimeOffset Clock { get; private set; }
	}
	#endregion
}

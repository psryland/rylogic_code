using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Threading;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>A container for the data maintained by the back testing simulation</summary>
	public class Simulation :IDisposable
	{
		// Notes:
		//  - The simulation has to run on a clock-based step because the bot MainLoop runs on a clock-based loop.
		//    Some bots can be event driven which would allow faster back testing, but that depends on the bot.

		public Simulation(Model model)
		{
			Exch = new Dictionary<string, SimExchange>();
			Model = model;

			// Initialise the clock to valid that will get changed by 'SetStartTime'
			Clock = DateTimeOffset.UtcNow;

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
			get { return m_exchanges; }
			set
			{
				if (m_exchanges == value) return;
				Util.DisposeAll(m_exchanges?.Values);
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
					m_model.BackTestingChanging -= HandleBackTestingChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.BackTestingChanging += HandleBackTestingChanged;
				}

				// Handlers
				void HandleBackTestingChanged(object sender, PrePostEventArgs e)
				{
					// If back-testing has just been enabled, configure the sim exchanges with PriceData sources
					//if (Model.BackTesting && e.After)
					//	foreach (var exch in Exch.Values)
					//		exch.Initials
				}
			}
		}
		private Model m_model;

		/// <summary>Back testing related settings</summary>
		public Settings.BackTestingSettings Settings
		{
			get { return Model.Settings.BackTesting; }
		}

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

		/// <summary>Get the current simulation time</summary>
		public DateTimeOffset Clock
		{
			get { return m_clock; }
			private set
			{
				if (m_clock == value) return;
				Model.UtcNow = m_clock = value;
			}
		}
		private DateTimeOffset m_clock;

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
			get { return Exch[exch.Name]; }
		}

		/// <summary>Reset the sim back to the start time</summary>
		public void Reset()
		{
			m_steps_to_run = 0;
			Clock = StartTime;
			SimReset.Raise(this, new SimResetEventArgs(Settings.Steps, Settings.TimeFrame, StartTime));
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void StepOne()
		{
			m_steps_to_run = 1;
			Running = true;
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void Run()
		{
			m_steps_to_run = -1;
			Running = true;
		}

		/// <summary>Advance the simulation forward by one step</summary>
		public void Pause()
		{
			Running = false;
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
					m_timer.Stop();
					Model.Log.Write(ELogLevel.Debug, "Simulation stopped");
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromSeconds(1.0 / Settings.StepRate), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (Running)
				{
					Model.Log.Write(ELogLevel.Debug, "Simulation started");
					m_timer.Start();
				}
				SimRunningChanged.Raise(this, new PrePostEventArgs(after:true));

				// Handlers
				void HandleTick(object sender, EventArgs args)
				{
					// Stop when the clock has caught up to real time or we've done enough steps
					if (Clock > DateTimeOffset.UtcNow || m_steps_to_run == 0)
					{
						Running = false;
						return;
					}

					// Notify the sim step. This causes PriceData's to "add" candles
					SimStep.Raise(this, new SimStepEventArgs(Clock));

					// Advance the simulation by 0.25 of a time frame since we're adding 4 sub-candles per candle
					Clock += new TimeSpan(Misc.TimeFrameToTicks(1.0 / Settings.SubStepsPerCandle, Settings.TimeFrame));

					// Run the required number of steps
					if (m_steps_to_run > 0)
						--m_steps_to_run;
				}
			}
		}
		private DispatcherTimer m_timer;
		private int m_steps_to_run;

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

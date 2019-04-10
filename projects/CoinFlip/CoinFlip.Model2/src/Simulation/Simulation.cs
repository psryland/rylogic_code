using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A container for the data maintained by the back testing simulation</summary>
	public class Simulation : IDisposable, INotifyPropertyChanged
	{
		public Simulation(IEnumerable<Exchange> exchanges)
		{
			m_sw_main_loop = new Stopwatch();
			Exchanges = new Dictionary<string, SimExchange>();

			Clock = StartTime;

			// Create a SimExchange for each exchange
			foreach (var exch in exchanges)
				Exchanges[exch.Name] = new SimExchange(this, exch);
		}
		public void Dispose()
		{
			Running = false;
		}

		/// <summary>The exchanges involved in the simulation</summary>
		private IDictionary<string, SimExchange> Exchanges { get; }

		/// <summary>Get the current simulation time</summary>
		public DateTimeOffset Clock
		{
			get { return m_clock; }
			private set
			{
				if (m_clock == value) return;
				m_clock = DateTimeOffset_.Clamp(value, StartTime, EndTime);
				NotifyPropertyChanged(nameof(Clock));
				NotifyPropertyChanged(nameof(PercentComplete));
				NotifyPropertyChanged(nameof(CanReset));
				NotifyPropertyChanged(nameof(CanStepOne));
				NotifyPropertyChanged(nameof(CanRunToTrade));
				NotifyPropertyChanged(nameof(CanRun));
			}
		}
		private DateTimeOffset m_clock;

		/// <summary>The simulation step resolution</summary>
		public ETimeFrame TimeFrame
		{
			get { return SettingsData.Settings.BackTesting.TimeFrame; }
			set
			{
				SettingsData.Settings.BackTesting.TimeFrame = value;
				NotifyPropertyChanged(nameof(TimeFrame));
				NotifyPropertyChanged(nameof(StartTime));
				NotifyPropertyChanged(nameof(EndTime));
				NotifyPropertyChanged(nameof(Steps));
			}
		}

		/// <summary>Get the back testing start time</summary>
		public DateTimeOffset StartTime
		{
			get { return SettingsData.Settings.BackTesting.StartTime.RoundDownTo(TimeFrame); }
			set
			{
				SettingsData.Settings.BackTesting.StartTime = value;
				EndTime = DateTimeOffset_.Max(EndTime, StartTime + Misc.TimeFrameToTimeSpan(1.0, TimeFrame));
				Clock = DateTimeOffset_.Clamp(Clock, StartTime, EndTime);
				NotifyPropertyChanged(nameof(StartTime));
				NotifyPropertyChanged(nameof(Steps));
			}
		}

		/// <summary>Get the back testing end time</summary>
		public DateTimeOffset EndTime
		{
			get { return SettingsData.Settings.BackTesting.EndTime.RoundUpTo(TimeFrame); }
			set
			{
				SettingsData.Settings.BackTesting.EndTime = value;
				StartTime = DateTimeOffset_.Min(StartTime, EndTime - Misc.TimeFrameToTimeSpan(1.0, TimeFrame));
				Clock = DateTimeOffset_.Clamp(Clock, StartTime, EndTime);
				NotifyPropertyChanged(nameof(EndTime));
				NotifyPropertyChanged(nameof(Steps));
			}
		}

		/// <summary>Get the back testing end time</summary>
		public double StepsPerCandle
		{
			get { return SettingsData.Settings.BackTesting.StepsPerCandle; }
			set
			{
				SettingsData.Settings.BackTesting.StepsPerCandle = value;
				NotifyPropertyChanged(nameof(StepsPerCandle));
				NotifyPropertyChanged(nameof(CanStepOne));
			}
		}

		/// <summary>The number of time frame steps to simulate</summary>
		public long Steps => (long)Misc.TimeSpanToTimeFrame(EndTime - StartTime, TimeFrame);

		/// <summary>The percentage of the way through the simulation</summary>
		public double PercentComplete
		{
			get { return 100.0 * DateTimeOffset_.Frac(StartTime, Clock, EndTime); }
			set
			{
				value = Math_.Clamp(value * 0.01, 0, 1);
				Clock = DateTimeOffset_.Lerp(StartTime, EndTime, value);
			}
		}

		/// <summary>Raised when the simulation is reset to the start time</summary>
		public event EventHandler<SimResetEventArgs> SimReset;

		/// <summary>Raised when the simulation time changes</summary>
		public event EventHandler<SimStepEventArgs> SimStep;

		/// <summary>Raised when the simulation starts/stops</summary>
		public event EventHandler<PrePostEventArgs> SimRunningChanged;

		/// <summary>Reset the sim back to the start time</summary>
		public void Reset()
		{
			// todo - caller should do this
			//// All bots must be stopped and restarted
			//var active_bots = Model.Bots.Where(x => x.Active).ToArray();
			//foreach (var bot in active_bots)
			//	bot.Active = false;

			// Reset the sim time back to 'StartTime'
			RunMode = ERunMode.Stopped;
			m_main_loop_last_step = StartTime.Ticks;
			Clock = StartTime;

			// Reset the exchange simulations
			foreach (var exch in Exchanges.Values)
				exch.Reset();

			// todo - caller - // Restart the bots that were started
			// todo - caller - foreach (var bot in active_bots)
			// todo - caller - 	bot.Active = true;

			// Notify of reset
			SimReset?.Invoke(this, new SimResetEventArgs(this));
		}
		public bool CanReset => Clock != StartTime;

		/// <summary>Advance the simulation forward by one step</summary>
		public void StepOne()
		{
			RunMode = ERunMode.StepOne;
			Running = true;
		}
		public bool CanStepOne => Misc.TimeSpanToTimeFrame(EndTime - Clock, TimeFrame) >= 1.0 / StepsPerCandle;

		/// <summary>Advance the simulation to the next submitted or filled trade</summary>
		public void RunToTrade()
		{
			RunMode = ERunMode.RunToTrade;
			Running = true;
		}
		public bool CanRunToTrade => CanRun;

		/// <summary>Run the simulation forward continuously</summary>
		public void Run()
		{
			Model.Log.Write(ELogLevel.Debug, "Simulation running");
			RunMode = ERunMode.Continuous;
			Running = true;
		}
		public bool CanRun => Clock < EndTime;

		/// <summary>Pause the simulation</summary>
		public void Pause()
		{
			Running = false;
			Model.Log.Write(ELogLevel.Debug, "Simulation stopped");
		}
		public bool CanPause => Running;

		/// <summary>The thread the runs the simulation</summary>
		public bool Running
		{
			get { return m_timer != null; }
			private set
			{
				if (Running == value) return;
				Debug.Assert(Misc.AssertMainThread());

				SimRunningChanged?.Invoke(this, new PrePostEventArgs(after: false));
				if (Running)
				{
					m_sw_main_loop.Stop();
					m_sw_main_loop.Reset();
					m_timer.Stop();
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (Running)
				{
					m_timer.Start();
					m_sw_main_loop.Start();
					m_max_ticks_per_step = Misc.TimeFrameToTicks(1.0 / StepsPerCandle, TimeFrame);
				}
				SimRunningChanged?.Invoke(this, new PrePostEventArgs(after: true));
				NotifyPropertyChanged(nameof(Running));
				NotifyPropertyChanged(nameof(CanPause));

				// Handlers
				void HandleTick(object sender, EventArgs args)
				{
					// Advance the simulation clock at a rate of 'StepRate' candles per second.
					// 'm_sw_main_loop' tracks the amount of real world time that has elapsed while 'Running' is true
					switch (RunMode)
					{
					case ERunMode.Continuous:
					case ERunMode.RunToTrade:
						{
							var elapsed_ticks = Math.Max(m_max_ticks_per_step, m_sw_main_loop.ElapsedTicks - m_main_loop_last_ticks);
							m_main_loop_last_ticks = m_sw_main_loop.ElapsedTicks;
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

					// Stop when the clock has caught up to real time or if we've done enough steps
					if (Clock > DateTimeOffset.UtcNow)
					{
						Running = false;
						return;
					}

					// Run the sim exchanges at top speed
					foreach (var exch in Exchanges.Values)
						exch.Step();

					// See if it's time for a sim step
					var elapsed = Clock.Ticks - m_main_loop_last_step;
					if (elapsed < m_max_ticks_per_step) return;
					m_main_loop_last_step += m_max_ticks_per_step;

					// Notify the sim step. This causes PriceData's to "add" candles
					SimStep?.Invoke(this, new SimStepEventArgs(Clock));

					// Run the required number of steps
					if (RunMode == ERunMode.StepOne)
						Running = false;
				}
			}
		}
		private DispatcherTimer m_timer;
		private Stopwatch m_sw_main_loop;
		private long m_max_ticks_per_step;
		private long m_main_loop_last_step;
		private long m_main_loop_last_ticks;

		/// <summary>Modes to run the simulation in</summary>
		public ERunMode RunMode { get; private set; }

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>States for the simulation</summary>
		public enum ERunMode
		{
			Stopped,
			Continuous,
			StepOne,
			RunToTrade,
		}
	}

	#region Event Args
	public class SimResetEventArgs : EventArgs
	{
		private readonly Simulation m_sim;
		public SimResetEventArgs(Simulation sim)
		{
			m_sim = sim;
		}

		/// <summary>The resolution of the simulation steps</summary>
		public ETimeFrame TimeFrame => m_sim.TimeFrame;

		/// <summary>The start time of the simulation</summary>
		public DateTimeOffset StartTime => m_sim.StartTime;

		/// <summary>The end time of the simulation</summary>
		public DateTimeOffset EndTime => m_sim.EndTime;

		/// <summary>The end time of the simulation</summary>
		public long Steps => m_sim.Steps;
	}
	public class SimStepEventArgs : EventArgs
	{
		public SimStepEventArgs(DateTimeOffset clock)
		{
			Clock = clock;
		}

		/// <summary>The simulation time</summary>
		public DateTimeOffset Clock { get; }
	}
	#endregion
}

///// <summary>Simulated Exchanges</summary>
//private Dictionary<string, SimExchange> Exch
//{
//	[DebuggerStepThrough]
//	get { return m_exchanges; }
//	set
//	{
//		if (m_exchanges == value) return;
//		Util.DisposeRange(m_exchanges?.Values);
//		m_exchanges = value;
//	}
//}
//private Dictionary<string, SimExchange> m_exchanges;

///// <summary>App logic</summary>
//public Model Model
//{
//	get { return m_model; }
//	private set
//	{
//		if (m_model == value) return;
//		if (m_model != null)
//		{
//			m_model.Settings.BackTesting.SettingChange -= HandleSettingChange;
//		}
//		m_model = value;
//		if (m_model != null)
//		{
//			m_model.Settings.BackTesting.SettingChange += HandleSettingChange;
//		}

//		// Handlers
//		void HandleSettingChange(object sender, SettingChangeEventArgs args)
//		{
//			if (args.Before) return;
//			switch (args.Key)
//			{
//			case nameof(Settings.TimeFrame):
//				{
//					if (Running)
//						throw new Exception("Don't change the time frame while the simulation is running");

//					// Update the data sources in each exchange
//					foreach (var exch in Exch.Values)
//						exch.FindDataSources();

//					break;
//				}
//			}
//		}
//	}
//}
//private Model m_model;

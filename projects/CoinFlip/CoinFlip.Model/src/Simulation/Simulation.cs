using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Threading;
using CoinFlip.Bots;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>A container for the data maintained by the back testing simulation</summary>
	public class Simulation :IDisposable
	{
		public Simulation(IEnumerable<Exchange> exchanges, PriceDataMap price_data_map, BotContainer bots)
		{
			m_sw_main_loop = new Stopwatch();
			Exchanges = new Dictionary<string, SimExchange>();
			PriceData = price_data_map;
			Bots = bots;

			// Create a SimExchange for each exchange
			Clock = StartTime;
			foreach (var exch in exchanges)
				Exchanges[exch.Name] = new SimExchange(this, exch, price_data_map);

			// Ensure the first update to the price data after creating the simulation
			// resets all instrument's cached data
			UpdatePriceData(force_invalidate: true);
		}
		public void Dispose()
		{
			Running = false;
			Util.DisposeRange(Exchanges.Values);
			Exchanges.Clear();
		}

		/// <summary>The exchanges involved in the simulation</summary>
		private IDictionary<string, SimExchange> Exchanges { get; }

		/// <summary>The store of price data instances</summary>
		private PriceDataMap PriceData { get; }

		/// <summary>The collection of bots</summary>
		private BotContainer Bots { get; }

		/// <summary>Get the current simulation time</summary>
		public DateTimeOffset Clock
		{
			get { return Model.SimClock; }
			private set
			{
				if (Model.SimClock == value) return;
				Model.SimClock = DateTimeOffset_.Clamp(value, StartTime, EndTime);
				NotifySimPropertyChanged();
			}
		}

		/// <summary>The simulation step resolution</summary>
		public ETimeFrame TimeFrame
		{
			get { return SettingsData.Settings.BackTesting.TimeFrame; }
			set
			{
				if (SettingsData.Settings.BackTesting.TimeFrame == value) return;
				SettingsData.Settings.BackTesting.TimeFrame = value;
				NotifySimPropertyChanged();
			}
		}

		/// <summary>Get the back testing start time</summary>
		public DateTimeOffset StartTime
		{
			get { return SettingsData.Settings.BackTesting.StartTime.RoundDownTo(TimeFrame); }
			set
			{
				if (SettingsData.Settings.BackTesting.StartTime == value) return;
				SettingsData.Settings.BackTesting.StartTime = value;
				EndTime = DateTimeOffset_.Max(EndTime, StartTime + Misc.TimeFrameToTimeSpan(1.0, TimeFrame));
				Clock = DateTimeOffset_.Clamp(Clock, StartTime, EndTime);
				UpdatePriceData();
				NotifySimPropertyChanged();
			}
		}

		/// <summary>Get the back testing end time</summary>
		public DateTimeOffset EndTime
		{
			get { return SettingsData.Settings.BackTesting.EndTime.RoundUpTo(TimeFrame); }
			set
			{
				if (SettingsData.Settings.BackTesting.EndTime == value) return;
				SettingsData.Settings.BackTesting.EndTime = value;
				StartTime = DateTimeOffset_.Min(StartTime, EndTime - Misc.TimeFrameToTimeSpan(1.0, TimeFrame));
				Clock = DateTimeOffset_.Clamp(Clock, StartTime, EndTime);
				UpdatePriceData();
				NotifySimPropertyChanged();
			}
		}

		/// <summary>Controls the rate that the simulation runs at</summary>
		public double StepsPerCandle
		{
			get { return SettingsData.Settings.BackTesting.StepsPerCandle; }
			set
			{
				if (SettingsData.Settings.BackTesting.StepsPerCandle == value) return;
				SettingsData.Settings.BackTesting.StepsPerCandle = value;
				NotifySimPropertyChanged();
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
				UpdatePriceData();
			}
		}

		/// <summary>Raised when the simulation is reset to the start time</summary>
		public event EventHandler<SimResetEventArgs> SimReset;

		/// <summary>Raised when the simulation time changes</summary>
		public event EventHandler<SimStepEventArgs> SimStep;

		/// <summary>Raised when the simulation starts/stops</summary>
		public event EventHandler<PrePostEventArgs> SimRunningChanged;

		/// <summary>Raised when a property of the sim changes</summary>
		public event EventHandler SimPropertyChanged;
		private void NotifySimPropertyChanged()
		{
			SimPropertyChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Reset the sim back to the start time</summary>
		public void Reset()
		{
			Running = false;

			// Deactivate all back-testing bots (Actually, all of them)
			// Restore the state before deleting the bot so that, by default, 
			// a bot can be constructed in it's initial state.
			foreach (var bot in Bots) bot.RestoreInitialState();
			Bots.Clear();

			// Reset the sim time back to 'StartTime'
			RunMode = ERunMode.Stopped;
			m_main_loop_last_step = StartTime.Ticks;
			Clock = StartTime;

			// Reset the exchange simulations
			foreach (var exch in Exchanges.Values)
				exch.Reset();

			// Reload the testing bots
			Bots.LoadFromSettings();

			// Generate initial price data
			UpdatePriceData();

			// Notify of reset
			SimReset?.Invoke(this, new SimResetEventArgs(this));
		}
		public bool CanReset => true;// So that balances can be update etc even if Clock == StartTime

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

		/// <summary>Get/Set whether the simulation is running</summary>
		public bool Running
		{
			get => m_timer != null;
			private set
			{
				if (Running == value) return;
				Debug.Assert(Misc.AssertMainThread());

				SimRunningChanged?.Invoke(this, new PrePostEventArgs(after: false));
				if (Running)
				{
					m_timer.Stop();
					m_sw_main_loop.Stop();
					m_sw_main_loop.Reset();
				}
				m_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (Running)
				{
					m_max_ticks_per_step = Misc.TimeFrameToTicks(1.0 / StepsPerCandle, TimeFrame);
					m_sw_main_loop.Start();
					m_timer.Start();
				}
				SimRunningChanged?.Invoke(this, new PrePostEventArgs(after: true));
				NotifySimPropertyChanged();

				// Handlers
				async void HandleTick(object sender, EventArgs args)
				{
					Debug.Assert(Misc.AssertMainThread());

					// Returns the next bot to be stepped based on their LastStepTime's and the current sim time
					IBot NextBotToStep() => Bots.Where(x => x.Active).MinByOrDefault(x => x.TimeTillNextStep);

					// On the first step from reset, preserve the initial state
					if (Clock == StartTime)
						foreach (var bot in Bots)
							bot.PreserveInitialState();

					// Advance the simulation clock at a maximum rate of 'StepRate' candles per second.
					// 'm_sw_main_loop' tracks the amount of real world time that has elapsed while 'Running' is true.
					// Determine the time difference to add to the simulation clock
					var next_clock = Clock;
					switch (RunMode)
					{
					case ERunMode.Continuous:
					case ERunMode.RunToTrade:
						{
							var elapsed_ticks = Math.Max(m_max_ticks_per_step, m_sw_main_loop.ElapsedTicks - m_main_loop_last_ticks);
							m_main_loop_last_ticks = m_sw_main_loop.ElapsedTicks;
							next_clock += new TimeSpan(elapsed_ticks);
							break;
						}
					case ERunMode.StepOne:
						{
							// If this is a single step, increment the clock by the step size,
							// or up to the next bot to be stepped, which ever is less.
							var step = TimeSpan_.Min(new TimeSpan(m_max_ticks_per_step), NextBotToStep()?.TimeTillNextStep ?? TimeSpan.MaxValue);
							next_clock += step;
							break;
						}
					}
					next_clock = DateTimeOffset_.Clamp(next_clock, StartTime, EndTime);

					// Make sure bots get stepped at their required rate
					for (; Running;)
					{
						// Find the next bot requiring stepping
						var bot = NextBotToStep();

						// If the next bot to step is within this step, step it.
						if (bot != null && bot.LastStepTime + bot.LoopPeriod < next_clock)
						{
							// Advance to clock to the bot's next step time
							Clock = bot.LastStepTime + bot.LoopPeriod;

							// Update each price data (i.e. "add" candles)
							UpdatePriceData();

							// Step the bot
							await bot.Step();
							continue;
						}

						// Stop when all bots have caught up
						break;
					}

					// Advance the clock to the end of the step and stop if the clock has caught up to real time
					Clock = next_clock;
					if (Clock == EndTime)
					{
						Running = false;
						return;
					}

					// See if it's time for a sim step
					var elapsed = Clock.Ticks - m_main_loop_last_step;
					if (elapsed >= m_max_ticks_per_step)
					{
						m_main_loop_last_step += m_max_ticks_per_step;

						// Update each price data (i.e. "add" candles)
						UpdatePriceData();

						// Notify the sim step.
						SimStep?.Invoke(this, new SimStepEventArgs(Clock));

						// Run the required number of steps
						if (RunMode == ERunMode.StepOne)
							Running = false;
					}
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

		/// <summary>Cause all price data instances to update</summary>
		private void UpdatePriceData(bool force_invalidate = false)
		{
			foreach (var pd in PriceData)
				pd.SimulationUpdate(force_invalidate);

			foreach (var exch in Exchanges.Values)
				exch.Step();
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

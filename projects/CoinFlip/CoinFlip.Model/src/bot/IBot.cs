﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public abstract class IBot :IDisposable ,INotifyPropertyChanged
    {
		// How to create a new Bot:
		// - Create a Class library project called 'Bot.<BotName>'
		// - Add references to 'CoinFlip.Model' and 'Rylogic'
		// - Set the framework version to 4.5.2
		// - Set the post build event to: @"copy $(TargetPath) $(SolutionDir)CoinFlip\bin\$(ConfigurationName)\bots\"
		// - Add a class with the Plugin attribute: @"[Plugin(typeof(IBot))]"
		// - Add a constructor like this: @"public MyBot(Model model, XElement settings_xml) :base("my_bot", model, new SettingsData(settings_xml)) {}"
		//   'SettingsData' is type that inherits 'SettingsBase<SettingsData>'
		//
		// Bots can be clock-based, and run at a fixed rate by overriding the 'Step()' method, or they can be
		// event-driven by subscribing to the 'OnDataChanged' event for the instruments they care about.
		// Event-driven bots should override 'Active' to have it reflect the active status of the bot,
		// and to not start/stop the bot MainLoop function.
		// Clock-based bots should use Model.UtcNow as the "current time". This allows faster than real-time back-testing to work.

		public IBot(string name, Model model, ISettingsData settings)
		{
			Name = name;
			Model = model;
			Settings = settings;
			Shutdown = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown.Token);
			MonitoredTrades = new List<TradeResult>();

			// Initialise the log for this bot instance
			var logpath = Misc.ResolveUserPath($"Logs\\{Name}\\log.txt");
			Path_.CreateDirs(Path_.Directory(logpath));
			Log = new Logger(Name, new LogToFile(logpath, append:false), Model.Log);
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
		}
		public void Dispose()
		{
			Shutdown = null;
			Active = false;
			Dispose(true);
			Log = null;
			Settings = null;
			Model = null;
		}
		protected virtual void Dispose(bool disposing)
		{}

		/// <summary>The cancellation token for shutting down the bot.</summary>
		public CancellationTokenSource Shutdown
		{
			// This is a combination of the Model.Shutdown token and a token created at the last activation.
			// Bots should use this token in their 'await' calls.
			get { return m_shutdown; }
			private set
			{
				if (m_shutdown == value) return;
				m_shutdown?.Cancel();
				m_shutdown = value;
			}
		}
		private CancellationTokenSource m_shutdown;

		/// <summary>Enable/Disable this bot</summary>
		public bool Active
		{
			get { return GetActiveInternal(); }
			set
			{
				if (m_activating != 0) throw new Exception("Reentrancy detected");
				using (Scope.Create(() => ++m_activating, () => --m_activating))
				{
					if (Active == value) return;

					// Note: all bots run in the main thread context. Market data is also
					// updated in the main thread context so there should never be race conditions.
					Debug.Assert(Model.AssertMainThread());

					// Don't allow activation with invalid settings
					if (value && !Settings.Valid)
						return;

					// Start/Stop
					if (!value)
					{
						// Signal exit
						Shutdown.Cancel();

						// Derived deactivation. Call before 'OnStop' so that no 'Step' calls happen after 'OnStop'
						SetActiveInternal(value);

						// Derived bot shutdown
						try { OnStop(); }
						catch (Exception ex) { Log.Write(ELogLevel.Error, ex, "Unhandled Bot.OnStop exception"); }

						// Reset monitored trades
						MonitoredTrades.Clear();

						// The bot is now inactive
						Log.Write(ELogLevel.Debug, $"Bot '{Name}' stopped");
					}
					else
					{
						// The bot is now active
						Log.Write(ELogLevel.Debug, $"Bot '{Name}' started");

						// Derived bot startup
						try { OnStart(); }
						catch (Exception ex) { Log.Write(ELogLevel.Error, ex, "Unhandled Bot.OnStart exception"); }

						// Create a cancel token
						Shutdown = CancellationTokenSource.CreateLinkedTokenSource(Model.Shutdown.Token);

						// Derived activation
						SetActiveInternal(value);
					}
				}
				ActiveChanged.Raise(this);
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Active)));
			}
		}
		private int m_activating;

		/// <summary>Raised on active changed</summary>
		public event EventHandler ActiveChanged;

		/// <summary>Allow sub-classes to specialise the meaning of 'active'</summary>
		protected virtual bool GetActiveInternal()
		{
			return m_main_loop_timer != null;
		}
		protected virtual void SetActiveInternal(bool enabled)
		{
			if (enabled)
			{
				// Start the bot heartbeat timer
				m_main_loop_timer = new DispatcherTimer(TimeSpan.FromMilliseconds(10), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher);
				m_main_loop_last_step = Model.UtcNow;
				m_main_loop_timer.Start();
			}
			else
			{
				// Stop the heartbeat timer
				m_main_loop_timer.Stop();
				m_main_loop_timer = null;
			}

			// Handlers
			void HandleTick(object sender = null, EventArgs e = null)
			{
				// See if it's time to step the bot
				var elapsed = Model.UtcNow - m_main_loop_last_step;
				if (elapsed < TimeSpan.FromMilliseconds(Settings.StepRateMS))
					return;

				// Record the time of the last step
				m_main_loop_last_step = Model.UtcNow;
				if (Shutdown.IsCancellationRequested)
					return;

				// Step the bot
				try
				{
					// Test for filled trades
					UpdateMonitoredTrades();

					// Step the derived bot
					Step();
				}
				catch (Exception ex)
				{
					if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
					if (ex is OperationCanceledException) return;
					else Log.Write(ELogLevel.Error, ex, $"Unhandled exception in Bot '{Name}' step.\r\n{ex.StackTrace}");
				}
			}
		}
		private DispatcherTimer m_main_loop_timer;
		private DateTimeOffset m_main_loop_last_step;

		/// <summary>A name for the type of bot this is</summary>
		public string Name { get; private set; }

		/// <summary>The fraction of funds allocated to this bot</summary>
		public decimal FundAllocation
		{
			get { return Settings.FundAllocation; }
			set
			{
				if (FundAllocation == value) return;
				Settings.FundAllocation = Maths.Clamp(value, 0m, 1m);
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(FundAllocation)));
			}
		}
		public float FundAllocationPC
		{
			get { return (float)FundAllocation * 100f; }
			set { FundAllocation = (decimal)(value * 0.01f); }
		}

		/// <summary>The step rate of the main loop for this bot</summary>
		public float StepRate
		{
			get { return Settings.StepRateMS * 0.001f; }
			set
			{
				if (StepRate == value) return;
				Settings.StepRateMS = (int)(value * 1000);
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(StepRate)));
			}
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.SimStep -= HandleSimStep;
					m_model.SimReset -= HandleSimReset;
					m_model.BackTestingChanging -= HandleBackTestingChanged;
					m_model.AllowTradesChanging -= HandleAllowTradesChanging;
					m_model.PairsUpdated -= HandlePairsUpdated;
					m_model.Exchanges.ListChanging -= HandleExchangeListChanging;
					m_model.Pairs.ListChanging -= HandlePairsListChanging;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Pairs.ListChanging += HandlePairsListChanging;
					m_model.Exchanges.ListChanging += HandleExchangeListChanging;
					m_model.PairsUpdated += HandlePairsUpdated;
					m_model.AllowTradesChanging += HandleAllowTradesChanging;
					m_model.BackTestingChanging += HandleBackTestingChanged;
					m_model.SimReset += HandleSimReset;
					m_model.SimStep += HandleSimStep;
				}
			}
		}
		private Model m_model;
		protected virtual void HandleAllowTradesChanging(object sender, PrePostEventArgs e)
		{}
		protected virtual void HandlePairsUpdated(object sender, EventArgs e)
		{}
		protected virtual void HandleExchangeListChanging(object sender, ListChgEventArgs<Exchange> e)
		{}
		protected virtual void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
		{}
		protected virtual void HandleBackTestingChanged(object sender, PrePostEventArgs e)
		{}
		protected virtual void HandleSimReset(object sender, SimResetEventArgs e)
		{}
		protected virtual void HandleSimStep(object sender, SimStepEventArgs e)
		{}

		/// <summary>Settings object for this instance</summary>
		public ISettingsData Settings
		{
			get { return m_settings; }
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingChanged -= HandleSettingChanged;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChanged += HandleSettingChanged;
				}
			}
		}
		private ISettingsData m_settings;
		protected virtual void HandleSettingChanged(object sender, SettingChangedEventArgs args)
		{
			switch (args.Key) {
			default: RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Settings))); break;
			case nameof(ISettingsData.StepRateMS):     RaisePropertyChanged(new PropertyChangedEventArgs(nameof(StepRate))); break;
			case nameof(ISettingsData.FundAllocation): RaisePropertyChanged(new PropertyChangedEventArgs(nameof(FundAllocation))); break;
			}
		}

		/// <summary>A log for this bot instance</summary>
		public Logger Log
		{
			[DebuggerStepThrough] get { return m_log; }
			private set
			{
				if (m_log == value) return;
				Util.Dispose(ref m_log);
				m_log = value;
			}
		}
		private Logger m_log;

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		protected void RaisePropertyChanged(PropertyChangedEventArgs args)
		{
			PropertyChanged.Raise(this, args);
		}

		/// <summary>Called when the bot is activated</summary>
		public virtual void OnStart()
		{}

		/// <summary>Called when the bot is deactivated</summary>
		public virtual void OnStop()
		{}

		/// <summary>Main loop step</summary>
		public virtual void Step()
		{}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public virtual void CMenuItems(ContextMenuStrip cmenu)
		{}

		/// <summary>Handle the chart about to render</summary>
		public virtual void OnChartRendering(Instrument instrument, Settings.ChartSettings chart_settings, ChartControl.ChartRenderingEventArgs args)
		{}

		/// <summary>Active orders to monitor</summary>
		protected List<TradeResult> MonitoredTrades { get; private set; }

		/// <summary>Update the state of all monitored active trades</summary>
		protected void UpdateMonitoredTrades()
		{
			foreach (var res in MonitoredTrades.ToArray())
			{
				var exch = res.Pair.Exchange;

				// Check that the position still exists on the exchange.
				var pos = exch.Positions[res.OrderId];
				if (pos != null)
					continue;

				// The position is gone, it may have been filled or cancelled.
				if (exch.TradeHistoryUseful)
				{
					// If there is a historic trade matching the order id then the order has been filled.
					var his = exch.History[res.OrderId];
					if (his != null)
						OnPositionFilled(res.OrderId, his);
					else
						OnPositionCancelled(res.OrderId);
				}
				else
				{
					// Trade has gone, assume it was filled
					OnPositionFilled(res.OrderId, null);
				}

				// Stop monitoring it
				MonitoredTrades.Remove(res);
			}
		}
		protected virtual void OnPositionFilled(ulong order_id, PositionFill his)
		{
			Log.Write(ELogLevel.Debug, $"Order filled. id={order_id} {his?.Description ?? string.Empty}");
		}
		protected virtual void OnPositionCancelled(ulong order_id)
		{
			Log.Write(ELogLevel.Debug, $"Order cancelled. id={order_id}");
		}

		/// <summary>Base class for Bot Settings</summary>
		public interface ISettingsData
		{
			/// <summary>The main loop step rate (approx)</summary>
			int StepRateMS { get; set; }

			/// <summary>The percentage of the balance for each currency available to this bot</summary>
			decimal FundAllocation { get; set; }

			/// <summary>True if the settings are valid</summary>
			bool Valid { get; }

			/// <summary>If 'Valid' is false, this is a text description of why</summary>
			string ErrorDescription { get; }

			/// <summary>Notification that a setting has changed</summary>
			event EventHandler<SettingChangedEventArgs> SettingChanged;
		}
		public class SettingsBase<T> :SettingsXml<T> ,ISettingsData where T:SettingsXml<T>, ISettingsData, new()
		{
			public SettingsBase()
			{
				StepRateMS     = 1000;
				FundAllocation = 0m;
			}
			public SettingsBase(SettingsBase<T> rhs)
			{
				StepRateMS     = rhs.StepRateMS;
				FundAllocation = rhs.FundAllocation;
			}
			public SettingsBase(XElement node)
				:base(node)
			{}

			/// <summary>The main loop step rate (approx)</summary>
			public int StepRateMS
			{
				get { return get<int>(nameof(StepRateMS)); }
				set { set(nameof(StepRateMS), value); }
			}

			/// <summary>The percentage of the balance for each currency available to this bot</summary>
			public decimal FundAllocation
			{
				get { return get<decimal>(nameof(FundAllocation)); }
				set { set(nameof(FundAllocation), value); }
			}

			/// <summary></summary>
			public virtual bool Valid
			{
				get { return true; }
			}

			/// <summary>If 'Valid' is false, this is a text description of why</summary>
			public virtual string ErrorDescription
			{
				get { return string.Empty; }
			}
		}
	}
}
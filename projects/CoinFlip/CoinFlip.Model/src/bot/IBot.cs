using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.container;
using pr.extn;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public abstract class IBot :IDisposable ,IShutdownAsync ,INotifyPropertyChanged
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
		// Bots can be clock-based, and run at a fixed rate by override the 'Step()' method, or they can be
		// event-driven by subscribing to the 'OnDataChanged' event for the instruments they care about.
		// Event-driven bots should override 'Active' to have it reflect the active status of the bot,
		// and to not start/stop the bot MainLoop function.
		// Back-testing clock-based bots is tricky however. The rate that the simulation runs through candle data
		// must be controlled so that the clock-based bot doesn't miss candle data.

		public IBot(string name, Model model, ISettingsData settings)
		{
			Name = name;
			Model = model;
			Settings = settings;
			m_main_loop_step = new AutoResetEvent(false);
			m_main_loop_shutdown = new CancellationTokenSource();
			MonitoredTrades = new List<TradeResult>();

			// Initialise the log for this bot instance
			var logpath = Misc.ResolveUserPath($"Logs\\{Name}\\log.txt");
			Path_.CreateDirs(Path_.Directory(logpath));
			Log = new Logger(Name, new LogToFile(logpath, append:false), Model.Log);
			Log.TimeZero = Log.TimeZero - Log.TimeZero.TimeOfDay;
		}
		public void Dispose()
		{
			Debug.Assert(!Active, "Main loop must be shut down before Disposing");
			Dispose(true);
			Log = null;
			Settings = null;
			Model = null;
			Util.Dispose(ref m_main_loop_step);
			Util.Dispose(ref m_main_loop_shutdown);
		}
		protected virtual void Dispose(bool disposing)
		{}

		/// <summary>Async shutdown</summary>
		public Task ShutdownAsync()
		{
			Active = false;
			return Task_.WaitWhile(() => Active);
		}

		/// <summary>The cancellation token for shutting down async tasks</summary>
		public CancellationToken Shutdown { get { return m_main_loop_shutdown.Token; } }
		private CancellationTokenSource m_main_loop_shutdown;

		/// <summary>Enable/Disable the time-based main loop for this bot</summary>
		public virtual bool Active
		{
			get { return m_active != 0; }
			set
			{
				if (Active == value) return;
				if (value)
				{
					if (!Settings.Valid)
						return;

					// Create a new shutdown token for each activation of the bot
					Util.Dispose(ref m_main_loop_shutdown);
					m_main_loop_shutdown = new CancellationTokenSource();
					MainLoop();
				}
				else
				{
					m_main_loop_shutdown.Cancel();
				}
				RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Active)));
			}
		}
		private int m_active;

		/// <summary>Main loop for the bot</summary>
		private async void MainLoop()
		{
			if (m_active != 0) throw new Exception("Main loop already running");
			using (Scope.Create(() => ++m_active, () => --m_active))
			{
				await OnStart();

				// Infinite loop till Shutdown called
				for (;;)
				{
					// Note: all bots run in the main thread context. Market data is also
					// updated in the main thread context so there should never be race conditions.
					Debug.Assert(Model.AssertMainThread());

					try
					{
						var step_rate = Settings.StepRateMS;
						await Task.Run(() => m_main_loop_step.WaitOne(step_rate), Shutdown);
						if (Shutdown.IsCancellationRequested)
							break;

						// Step the bot
						await Step();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) { break; }
						else Log.Write(ELogLevel.Error, ex, $"Unhandled exception in Bot {Name} step.\r\n{ex.StackTrace}");
					}
				}

				await OnStop();
			}
			RaisePropertyChanged(new PropertyChangedEventArgs(nameof(Active)));
		}
		private AutoResetEvent m_main_loop_step;

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
					m_model.AllowTradesChanging -= HandleAllowTradesChanging;
					m_model.PairsInvalidated -= HandlePairsInvalidated;
					m_model.Exchanges.ListChanging -= HandleExchangeListChanging;
					m_model.Pairs.ListChanging -= HandlePairsListChanging;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Pairs.ListChanging += HandlePairsListChanging;
					m_model.Exchanges.ListChanging += HandleExchangeListChanging;
					m_model.PairsInvalidated += HandlePairsInvalidated;
					m_model.AllowTradesChanging += HandleAllowTradesChanging;
				}
			}
		}
		private Model m_model;
		protected virtual void HandleAllowTradesChanging(object sender, PrePostEventArgs e)
		{
			// Disable the bot when enable trading is switched
			// so that trades don't get left behind.
			Active = false;
		}
		protected virtual void HandlePairsInvalidated(object sender, EventArgs e)
		{}
		protected virtual void HandleExchangeListChanging(object sender, ListChgEventArgs<Exchange> e)
		{}
		protected virtual void HandlePairsListChanging(object sender, ListChgEventArgs<TradePair> e)
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
		public virtual Task OnStart()
		{
			Log.Write(ELogLevel.Info, $"Starting Bot: {Name}");
			return Misc.CompletedTask;
		}

		/// <summary>Called when the bot is deactivated</summary>
		public virtual Task OnStop()
		{
			Log.Write(ELogLevel.Info, $"Stopping Bot: {Name}");
			return Misc.CompletedTask;
		}

		/// <summary>Main loop step</summary>
		public virtual Task Step()
		{
			return Misc.CompletedTask;
		}

		/// <summary>Return items to add to the context menu for this bot</summary>
		public virtual void CMenuItems(ContextMenuStrip cmenu)
		{}

		/// <summary>Cause a main loop step sooner than then next scheduled step</summary>
		protected void TriggerMainLoopStep()
		{
			m_main_loop_step.Set();
		}

		/// <summary>Active orders to monitor</summary>
		protected List<TradeResult> MonitoredTrades { get; private set; }

		/// <summary>Update the state of all monitored active trades</summary>
		protected async Task UpdateMonitoredTrades()
		{
			foreach (var res in MonitoredTrades)
			{
				var exch = res.Pair.Exchange;

				// Check that the position still exists on the exchange.
				var pos = exch.Positions[res.OrderId];
				if (pos != null)
					continue;

				// If not, then it may have been filled or cancelled.
				if (exch.TradeHistoryUseful)
				{
					// Ensure the trade history is up to date
					await exch.TradeHistoryUpdated();

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
				get { return get(x => x.StepRateMS); }
				set { set(x => x.StepRateMS, value); }
			}

			/// <summary>The percentage of the balance for each currency available to this bot</summary>
			public decimal FundAllocation
			{
				get { return get(x => x.FundAllocation); }
				set { set(x => x.FundAllocation, value); }
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

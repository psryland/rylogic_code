using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CoinFlip.Settings;
using Rylogic.Plugin;
using Rylogic.Utility;

namespace CoinFlip.Bots
{
	public abstract class IBot : IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//  - Bots must be designed to start and stop at any point. All data must be persisted to
		//    disk immediately.
		//  - Every new bot instance is assigned a GUID to distingush between multiple instances
		//    of the same bot.
		//  - As a safety feature, bots can only be "live" or "backtesting" bots. They can't switch
		//    from one state to the other. This means they can't share settings, order ids, etc and
		//    potentially make incorrect live trades based on simulation data.
		//  - Trades created by a bot can be managed using a 'MonitoredOrders' instance. Monitored
		//    orders are designed to be saved with Settings Data so that they persist across restarts.
		//
		// How to:
		//  - Create a .NET framework 4.7.2 class library project called 'Bot.<BotName>'
		//  - Add references to 'CoinFlip.Model' and 'Rylogic'
		//  - Sub-class 'IBot' and decorate with '[Plugin(typeof(IBot))]'
		//  - Add a project dependency to CoinFlip.UI on this new Bot so that it's built before CoinFlip.UI
		//  - Set the post build event to: @"py $(ProjectDir)..\post_build_bot.py $(TargetPath) $(ProjectDir) $(ConfigurationName)"
		//  - The derived type must have a constructor with the same signature as IBot

		public IBot(BotData bot_data, Model model)
		{
			BotData = bot_data;
			BotData.TypeName = GetType().FullName;
			Model = model;
		}
		public virtual void Dispose()
		{
			Active = false;
		}

		/// <summary>Access the application persisted data for this bot</summary>
		public BotData BotData { get; }

		/// <summary>App logic</summary>
		public Model Model { get; }

		/// <summary>The unique ID for this bot instance</summary>
		public Guid Id => BotData.Id;

		/// <summary>True if this bot is only used in backtesting mode</summary>
		public bool BackTesting => BotData.BackTesting;

		/// <summary>The fund interface</summary>
		public Fund Fund
		{
			get { return Model.Funds[BotData.FundId]; }
			set
			{
				if (Fund?.Id == value?.Id) return;
				BotData.FundId = value?.Id ?? string.Empty;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Fund)));
			}
		}

		/// <summary>User assigned name for the bot</summary>
		public string Name
		{
			get { return BotData.Name; }
			set
			{
				if (Name == value) return;
				BotData.Name = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
			}
		}

		/// <summary>Activate/Deactivate the bot</summary>
		public bool Active
		{
			get { return m_bot_main_timer != null; }
			set
			{
				// Notes:
				//  - Don't persist the active state to settings here. Do it at the point
				//    where the user chooses to activate or deactivate the bot. This solves
				//    issues with shutdown and switching between backtesting and live.
				//  - Don't activate from the constructor of IBot, because derived bots may not
				//    be fully set up yet.
				//  - The bot stepping mechanism is different for backtesting because we want to
				//    run it at faster than real time. The timer is still created in backtesting
				//    mode but it doesn't call Step. Instead, the simulation calls step on bots
				//    as the simulation time advances

				if (Active == value) return;
				if (value && !CanActivate) return;
				Debug.Assert(Misc.AssertMainThread());

				if (Active)
				{
					m_bot_main_timer.Stop();
					Cancel.Cancel();
					Model.Log.Write(ELogLevel.Info, $"Bot '{Name}' stopped");
				}
				m_bot_main_timer = value ? new DispatcherTimer(LoopPeriod, DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) : null;
				if (Active)
				{
					Model.Log.Write(ELogLevel.Info, $"Bot '{Name}' started");
					Cancel = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					LastStepTime = Model.UtcNow - LoopPeriod;
					m_bot_main_timer.Start();
					HandleTick();
				}

				// Notify active changed
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));

				// Handlers
				async void HandleTick(object sender = null, EventArgs e = null)
				{
					try
					{
						// Ignore messages after the timer is stopped
						if (!Active)
							return;

						if (!Model.BackTesting)
							await Step();
					}
					catch (Exception ex)
					{
						if (ex is AggregateException ae) ex = ae.InnerExceptions.First();
						if (ex is OperationCanceledException) return;
						else Model.Log.Write(ELogLevel.Error, ex, $"Unhandled error in bot '{Name}' main loop.");
					}
				}
			}
		}
		private DispatcherTimer m_bot_main_timer;

		/// <summary>How frequently the Bot wants to be stepped</summary>
		public virtual TimeSpan LoopPeriod => TimeSpan.FromMilliseconds(1000);

		/// <summary>Time stamp of when the last occurred</summary>
		public DateTimeOffset LastStepTime { get; private set; }

		/// <summary>Cancel token for deactivating the bot</summary>
		public CancellationTokenSource Cancel { get; private set; }

		/// <summary>Application shutdown signal</summary>
		public CancellationToken Shutdown => Model.Shutdown.Token;

		/// <summary>The filepath to use for settings for this bot</summary>
		public string SettingsFilepath => Misc.ResolveUserPath("Bots", $"settings.{Id}.xml");

		/// <summary>True if the bot is ok to run</summary>
		public bool CanActivate
		{
			get
			{
				try { return Fund != null && CanActivateInternal; }
				catch (Exception ex)
				{
					Model.Log.Write(ELogLevel.Error, ex, $"Bot {Name} 'CanActivate' Error");
					return false;
				}
			}
		}
		protected virtual bool CanActivateInternal => false;

		/// <summary>Configure this bot</summary>
		public async Task Configure(object owner)
		{
			try
			{
				// Notes:
				//  - Configure can be called at any point, even when the bot is running.
				//  - Configure is called in the UI thread, so the bot can display UI. 'owner' is a WPF Window
				await ConfigureInternal(owner);
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Bot {Name} Configure Error");
			}
		}
		protected virtual Task ConfigureInternal(object owner)
		{
			return Task.CompletedTask;
		}

		/// <summary>Step the bot</summary>
		public async Task Step()
		{
			try
			{
				await StepInternal();
				LastStepTime = Model.UtcNow;
			}
			catch (Exception ex)
			{
				Model.Log.Write(ELogLevel.Error, ex, $"Bot {Name} Step Error");
			}
		}
		protected virtual Task StepInternal()
		{
			return Task.CompletedTask;
		}

		/// <summary>Returns a dynamically checked list of the available bots</summary>
		public static IList<PluginFile> AvailableBots()
		{
			// Enumerate the available bots
			var bot_files = Plugins<IBot>.Enumerate(Misc.ResolveBotPath(), regex_filter: Misc.BotFilterRegex).ToArray();
			return bot_files;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}

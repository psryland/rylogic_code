using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Plugin;
using Rylogic.Utility;

namespace CoinFlip.Bots
{
	public abstract class IBot :IDisposable, INotifyPropertyChanged
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
		//  - The main namespace for the project should be 'Bot.<BotName>'
		//  - Add references to 'CoinFlip.Model', 'Rylogic.Core', 'Rylogic.Gui.WPF', 'PresentationCore', 'PresentationFramework', and 'WindowsBase'
		//  - Sub-class 'IBot' and decorate with '[Plugin(typeof(IBot))]'
		//  - Add a project dependency to CoinFlip.UI on this new Bot so that it's built before CoinFlip.UI
		//  - Set the post build event to: @"py $(ProjectDir)..\post_build_bot.py $(TargetPath) $(ProjectDir) $(ConfigurationName)"
		//  - Add constructor: public Bot(CoinFlip.Settings.BotData bot_data, Model model) :base(bot_data, model) {}
		//  - Compile and run
		//  - Go to back testing, and add a new bot instance of the new bot type
		//  - The skeleton bot is now ready
		// Next Steps:
		//  - Add a text description of the bot algorithm to the main IBot class.
		//  - Create a SettingsData class for persisting bot state.
		//  - Add a 'MonitoredOrders' instance to the settings if needed.
		//  - Create a config dialog (ConfigureUI), and overload 'ConfigureInternal' to display it

		public IBot(BotData bot_data, Model model)
		{
			BotData = bot_data;
			BotData.TypeName = GetType().FullName ?? throw new NullReferenceException("Bot type does not have a name");
			Model = model;
			Cancel = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
			Log = new Logger(BotData.Name, Model.Log);
		}
		public virtual void Dispose()
		{
			Active = false;
			Util.Dispose(Log);
		}

		/// <summary>Access the application persisted data for this bot</summary>
		public BotData BotData { get; }

		/// <summary>App logic</summary>
		public Model Model { get; }

		/// <summary>Log context specific to this bot</summary>
		public Logger Log { get; }

		/// <summary>The unique ID for this bot instance</summary>
		public Guid Id => BotData.Id;

		/// <summary>True if this bot is only used in backtesting mode</summary>
		public bool BackTesting => BotData.BackTesting;

		/// <summary>The fund interface</summary>
		public Fund Fund
		{
			get => Model.Funds[BotData.FundId];
			set
			{
				if (Fund?.Id == value?.Id) return;
				BotData.FundId = value?.Id ?? string.Empty;
				NotifyPropertyChanged(nameof(Fund));
				NotifyPropertyChanged(nameof(CanActivate));
				BotData.Save();
			}
		}

		/// <summary>User assigned name for the bot</summary>
		public string Name
		{
			get => BotData.Name;
			set
			{
				if (Name == value) return;
				BotData.Name = value;
				NotifyPropertyChanged(nameof(Name));
			}
		}

		/// <summary>Activate/Deactivate the bot</summary>
		public bool Active
		{
			get => m_bot_main_timer != null;
			set
			{
				// Notes:
				//  - Don't persist the active state to settings here. Do it at the point
				//    where the user chooses to activate or deactivate the bot. This solves
				//    issues with shutdown and switching between backtesting and live.
				//  - Don't activate from the constructor of IBot, because derived bots may not
				//    be fully set up yet.
				//  - The bot stepping mechanism is different for backtesting because I want to
				//    run it at faster than real time. The timer is still created in backtesting
				//    mode but it doesn't call Step. Instead, the simulation calls step on bots
				//    as the simulation time advances

				if (Active == value) return;
				if (value && !CanActivate) return;
				Debug.Assert(Misc.AssertMainThread());

				if (m_bot_main_timer != null)
				{
					m_bot_main_timer.Stop();
					Cancel.Cancel();
					Model.Log.Write(ELogLevel.Info, $"Bot '{Name}' stopped");
				}
				m_bot_main_timer = value ? new DispatcherTimer(TimeSpan.FromMilliseconds(1), DispatcherPriority.Normal, HandleTick, Dispatcher.CurrentDispatcher) { IsEnabled = false } : null;
				if (m_bot_main_timer != null)
				{
					Model.Log.Write(ELogLevel.Info, $"Bot '{Name}' started");
					LastStepTime = Model.UtcNow - LoopPeriod;
					Cancel = CancellationTokenSource.CreateLinkedTokenSource(Shutdown);
					if (!Model.BackTesting)
					{
						m_bot_main_timer.Start();
						HandleTick(null, EventArgs.Empty);
					}
				}

				// Notify active changed
				NotifyPropertyChanged(nameof(Active));

				// Handlers
				async void HandleTick(object? sender, EventArgs args)
				{
					try
					{
						// Ignore messages after the timer is stopped
						if (m_bot_main_timer == null)
							return;

						// Stop the timer until we know when to next step the bot
						m_bot_main_timer.Stop();

						// Step
						Debug.Assert(!Model.BackTesting);
						await Step();

						// Restart the timer
						m_bot_main_timer.Interval = TimeTillNextStep;
						m_bot_main_timer.Start();
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
		private DispatcherTimer? m_bot_main_timer;

		/// <summary>Time stamp of when the last occurred</summary>
		protected DateTimeOffset LastStepTime { get; private set; }

		/// <summary>How frequently the Bot wants to be stepped</summary>
		public virtual TimeSpan LoopPeriod => TimeSpan.FromMilliseconds(1000);

		/// <summary>The time at which to step this bot again</summary>
		public virtual DateTimeOffset NextStepTime => LastStepTime + LoopPeriod;

		/// <summary>The time span until this bot is due to be stepped</summary>
		public TimeSpan TimeTillNextStep => NextStepTime - Model.UtcNow;

		/// <summary>Cancel token for deactivating the bot</summary>
		public CancellationTokenSource Cancel { get; private set; }

		/// <summary>Application shutdown signal</summary>
		public CancellationToken Shutdown => Model.Shutdown.Token;

		/// <summary>The filepath to use for settings for this bot</summary>
		public string SettingsFilepath => Misc.ResolveUserPath(Model.BackTesting ? "Sim" : "", "Bots", $"settings.{Name}.{Id}.xml");

		/// <summary>True if the bot is ok to run</summary>
		public bool CanActivate
		{
			get
			{
				try { return CanActivateInternal; }
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
				PreserveInitialState();
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
				if (!CanActivate)
				{
					Active = false;
					return;
				}

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

		/// <summary>Save/Restore the bot settings</summary>
		public void PreserveInitialState()
		{
			PreserveInitialStateInternal();
		}
		protected virtual void PreserveInitialStateInternal()
		{
			if (Path_.FileExists(SettingsFilepath))
				Shell.Copy(SettingsFilepath, SettingsFilepath + ".initial", overwrite: true);
		}
		public void RestoreInitialState()
		{
			RestoreInitialStateInternal();
		}
		protected virtual void RestoreInitialStateInternal()
		{
			if (Path_.FileExists(SettingsFilepath + ".initial"))
				Shell.Copy(SettingsFilepath + ".initial", SettingsFilepath, overwrite: true);
		}

		/// <summary>Returns a dynamically checked list of the available bots</summary>
		public static IList<PluginFile> AvailableBots()
		{
			// Enumerate the available bots
			var bot_directory = Misc.ResolveBotPath();
			return Path_.DirExists(bot_directory)
				? Plugins<IBot>.Enumerate(bot_directory, regex_filter: Misc.BotFilterRegex).ToArray()
				: Array.Empty<PluginFile>();
		}

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}

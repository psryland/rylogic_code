using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using EweLink;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public sealed class Model :IDisposable, INotifyPropertyChanged
	{
		public Model()
		{
			ConsumersList = new ObservableCollection<Consumer>();
			Settings = new SettingsData(SettingsData.Filepath);
			Shutdown = new CancellationTokenSource();
			History = new History();
			Fronius = new FroniusAPI(Settings.SolarInverterIP, Shutdown.Token);
			Ewe = new EweLinkAPI(Shutdown.Token);
			Solar = new SolarData();
			Sched = new Schedule();

			Sched.Add(new Schedule.Range("Monitor Active", Schedule.ERepeat.Daily,
				new DateTimeOffset(1, 1, 1, 8, 0, 0, TimeSpan.Zero),
				new DateTimeOffset(1, 1, 1, 18, 0, 0, TimeSpan.Zero)));

			Log.Write(ELogLevel.Info, "Model initialised");
			m_settings.NotifyAllSettingsChanged();
		}
		public void Dispose()
		{
			EnableMonitor = false;
			PollSolarData = false;
			Util.DisposeRange(ConsumersList);
			Fronius = null!;
			Ewe = null!;
			History = null!;
			Settings = null!;
		}

		/// <summary></summary>
		public SettingsData Settings
		{
			get => m_settings;
			set
			{
				if (m_settings == value) return;
				if (m_settings != null)
				{
					m_settings.SettingsSaving -= HandleSettingsSaving;
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
					m_settings.SettingsSaving += HandleSettingsSaving;
				}

				// Handlers
				void HandleSettingsSaving(object? sender, SettingsSavingEventArgs e)
				{
					// Save the new order of consumers to the settings
					Settings.Consumers = Consumers.Select(x => x.Settings).ToArray();
				}
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
						case nameof(SettingsData.Username):
						case nameof(SettingsData.Password):
						{
							NotifyPropertyChanged(nameof(CanLogin));
							break;
						}
						case nameof(SettingsData.SolarInverterIP):
						{
							// If the solar inverter IP has a value, start polling for data
							PollSolarData = Settings.SolarInverterIP.HasValue();
							break;
						}
						case nameof(SettingsData.Consumers):
						{
							// Synchronise the consumer list with the settings
							ConsumersList.SyncStable(Settings.Consumers, (l,r) => Equals(l, r.Settings), x =>
							{
								var consumer = new Consumer(x);
								consumer.PropertyChanged += WeakRef.MakeWeak(HandleConsumerPropertyChanged, h => consumer.PropertyChanged -= h);
								return consumer;
							});
							PopulateConsumerDevices(ConsumersList);
							break;
						}
					}
				}
				void HandleConsumerPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
						case nameof(Consumer.On):
						{
							NotifyPropertyChanged(nameof(PowerConsumed));
							NotifyPropertyChanged(nameof(PowerSurplus));
							break;
						}
						case nameof(Consumer.Power):
						{
							NotifyPropertyChanged(nameof(PowerConsumed));
							NotifyPropertyChanged(nameof(PowerSurplus));
							break;
						}
						default:
						{
							//var consumer = (Consumer)sender;
							//Log.Write(ELogLevel.Debug, $"Consumer {consumer.Name}: {e.PropertyName} changed");
							break;
						}
					}
				}
			}
		}
		private SettingsData m_settings = null!;

		/// <summary>Shutdown token</summary>
		public CancellationTokenSource Shutdown { get; }

		/// <summary>A store of historic consumer data</summary>
		public History History
		{
			get => m_history;
			set
			{
				if (m_history == value) return;
				Util.Dispose(ref m_history!);
				m_history = value;
			}
		}
		private History m_history = null!;

		/// <summary>Access to the REST API of eWeLink</summary>
		public EweLinkAPI Ewe
		{
			get => m_ewe;
			private set
			{
				if (m_ewe == value) return;
				if (m_ewe != null)
				{
					m_ewe.Devices.CollectionChanged -= HandleDeviceListChanged;
					Util.Dispose(ref m_ewe!);
				}
				m_ewe = value;
				if (m_ewe != null)
				{
					m_ewe.Devices.CollectionChanged += HandleDeviceListChanged;
				}

				// Handler
				void HandleDeviceListChanged(object sender, NotifyCollectionChangedEventArgs e)
				{
					PopulateConsumerDevices(ConsumersList);
					NotifyPropertyChanged(nameof(EweDevices));
				}
			}
		}
		private EweLinkAPI m_ewe = null!;

		/// <summary>Access to the REST API of the fronius inverter</summary>
		private FroniusAPI Fronius
		{
			get => m_fronius;
			set
			{
				if (m_fronius == value) return;
				Util.Dispose(ref m_fronius!);
				m_fronius = value;
			}
		}
		private FroniusAPI m_fronius = null!;

		/// <summary>EweDevices</summary>
		public EweLinkAPI.IEweDeviceList EweDevices => Ewe.Devices;

		/// <summary>The consumers</summary>
		public IReadOnlyList<Consumer> Consumers => ConsumersList;
		private ObservableCollection<Consumer> ConsumersList { get; }

		/// <summary>The schedule for when the app is active</summary>
		public Schedule Sched { get; }

		/// <summary>Current solar output</summary>
		public SolarData Solar
		{
			get => m_solar;
			private set
			{
				if (m_solar == value) return;
				m_solar = value;
				NotifyPropertyChanged(nameof(Solar));
				NotifyPropertyChanged(nameof(PowerSurplus));
			}
		}
		private SolarData m_solar = null!;

		/// <summary>Enable/Disable controlling the power consumer switches</summary>
		public bool EnableMonitor
		{
			get => m_timer_enable_control != null && m_timer_enable_control.IsEnabled;
			set
			{
				value &= PollSolarData;
				if (EnableMonitor == value) return;
				if (m_timer_enable_control != null)
				{
					m_timer_enable_control.Stop();
				}
				m_timer_enable_control = value ? new DispatcherTimer(Settings.MonitorPeriod, DispatcherPriority.Background, HandleTimer, Dispatcher.CurrentDispatcher) : null;
				if (m_timer_enable_control != null)
				{
					m_timer_enable_control.Start();
				}
				NotifyPropertyChanged(nameof(EnableMonitor));
				NotifyPropertyChanged(nameof(EnableMonitorAvailable));
				LastError = null;

				// Handler
				async void HandleTimer(object? sender, EventArgs e)
				{
					try
					{
						m_timer_enable_control?.Stop();
						await RunMonitor();
						m_timer_enable_control?.Start();
					}
					catch (OperationCanceledException) {}
					catch (Exception ex)
					{
						Log.Write(ELogLevel.Error, ex, "Error during RunMonitor");
						LastError = ex;
					}
				}
				async Task RunMonitor()
				{
					// Perform a check of the solar output to see whether consumers should be enabled.

					// If not within the scheduled active time, do nothing
					//hack if (!Sched.ActiveRanges().Any())
					//hack	return;

					// The current solar output
					var available_power = Solar.CurrentPower;

					// The required power accumulator
					var required_power = Settings.ReservePower;

					// Make a list of the consumers that can be controlled.
					// Accumulate the required power of those that are on but are not controllable
					var consumers = Consumers.Where(c =>
					{
						// No switch? can't turn it off or on
						if (c.EweSwitch == null)
						{
							if (c.On) required_power += c.Power ?? 0.0;
							return false;
						}

						// Not a controllable device...
						switch (c.Settings.ControlMode)
						{
							case EControlMode.Disabled:
							{
								return false;
							}
							case EControlMode.Observed:
							{
								if (c.On) required_power += c.Power ?? 0.0;
								return false;
							}
							case EControlMode.Controlled:
							{
								// Todo: for now, ignore external control.
								//// Consumer is currently on, and wasn't turned on by us
								//return
								//	c.EweSwitch.State == EweSwitch.ESwitchState.On &&
								//	c.LastAppStateChange
								return true;
							}
							default:
							{
								throw new Exception($"Unknown control mode: {c.Settings.ControlMode}");
							}
						}
					}).ToList();

					// Enable/Disable each consumer based on available power
					foreach (var consumer in consumers)
					{
						// If the consumer knows its power consumption, use that value.
						// otherwise fall-back to the required power.
						var req_pwr = consumer.Power ?? consumer.RequiredPower;
						var turn_on = required_power + req_pwr < available_power;
						var state = turn_on ? EweSwitch.ESwitchState.On : EweSwitch.ESwitchState.Off;

						// If the requested state equals the current state, reset the state change pending timer
						if (consumer.On == turn_on)
						{
							if (consumer.StateChangePending != null)
								Log.Write(ELogLevel.Info, $"{consumer.Name}: Cancelling pending stating change");

							consumer.StateChangePending = null;
						}

						// If this is the first time a state change is signalled, record the time.
						else if (consumer.StateChangePending == null)
						{
							Log.Write(ELogLevel.Info, $"{consumer.Name}: Pending state change to {state} started");
							consumer.StateChangePending = DateTimeOffset.Now;
						}

						// If the state change has been pending for more than the cooldown period, change state
						else if (DateTimeOffset.Now - consumer.StateChangePending > consumer.Settings.Cooldown)
						{
							Log.Write(ELogLevel.Info, $"{consumer.Name}: State changed to {state}");
							await Ewe.SwitchState(consumer.EweSwitch!, state, 0, Shutdown.Token);
						}

						// Trigger a refresh of the state change fraction
						else
						{
							consumer.NotifyPropertyChanged(nameof(Consumer.StateChangeFrac));
						}

						// If the device is on (or will be on) accumulate the required power
						if (turn_on)
							required_power += req_pwr;
					}

					// todo: shouldn't be here...
					NotifyPropertyChanged(nameof(PowerConsumed));
					NotifyPropertyChanged(nameof(PowerSurplus));
				}
			}
		}
		public bool EnableMonitorAvailable => PollSolarData || EnableMonitor; // Available if receiving solar data or currently on
		private DispatcherTimer? m_timer_enable_control;

		/// <summary>Enable/Disable polling the solar output data</summary>
		public bool PollSolarData
		{
			get => m_timer_poll_solar != null && m_timer_poll_solar.IsEnabled;
			set
			{
				if (PollSolarData == value) return;
				if (m_timer_poll_solar != null)
				{
					m_timer_poll_solar.Stop();
				}
				m_timer_poll_solar = value ? new DispatcherTimer(Settings.SolarPollPeriod, DispatcherPriority.Background, HandleTimer, Dispatcher.CurrentDispatcher) : null;
				if (m_timer_poll_solar != null)
				{
					m_timer_poll_solar.Start();
				}
				NotifyPropertyChanged(nameof(EnableMonitorAvailable));
				LastError = null;

				// Handler
				async void HandleTimer(object? sender, EventArgs e)
				{
					try
					{
						m_timer_poll_solar?.Stop();
						Solar = await Fronius.RealTimeData(Shutdown.Token);
						History.Add(Solar);
						m_timer_poll_solar?.Start();
					}
					catch (Exception ex)
					{
						LastError = ex;
					}
				}
			}
		}
		private DispatcherTimer? m_timer_poll_solar;

		/// <summary>The total power used by active consumers (in kWatts)</summary>
		public double PowerConsumed => ActiveConsumers.Sum(x => x.Power ?? x.RequiredPower);

		/// <summary>Total unused solar power (in kWatts)</summary>
		public double PowerSurplus => Solar.CurrentPower - PowerConsumed;

		/// <summary>A string description of the consumers that are currently on</summary>
		public IEnumerable<Consumer> ActiveConsumers => Consumers.Where(x => x.On);

		/// <summary>True while a login is being attempted</summary>
		public bool LoginInProgress
		{
			get => m_login_in_progress;
			set
			{
				if (m_login_in_progress == value) return;
				m_login_in_progress = value;
				NotifyPropertyChanged(nameof(LoginInProgress));
			}
		}
		private bool m_login_in_progress;

		/// <summary>True if log on to EweLink was successfull</summary>
		public bool IsLoggedOn => Ewe.Cred != null;

		/// <summary>The last error</summary>
		public Exception? LastError
		{
			get => m_last_error;
			set
			{
				if (m_last_error == value) return;
				m_last_error = value;
				NotifyPropertyChanged(nameof(LastError));
			}
		}
		private Exception? m_last_error;

		/// <summary>Login on the the EweLink service</summary>
		public async Task Login(string username, string password)
		{
			Settings.Username = username;
			Settings.Password = password;

			if (CanLogin)
			{
				// Protect against reentrant login
				using var in_progress = Scope.Create(
					() => LoginInProgress = true,
					() => LoginInProgress = false);

				try
				{
					// Log in to EweLink
					await Ewe.Login(username, password);
					NotifyPropertyChanged(nameof(IsLoggedOn));
					NotifyPropertyChanged(nameof(EweDevices));
				}
				catch (Exception ex)
				{
					LastError = ex;
				}
			}
		}
		public async Task Logout()
		{
			await Ewe.Logout();
			NotifyPropertyChanged(nameof(IsLoggedOn));
		}

		/// <summary>Try if login is possible</summary>
		public bool CanLogin => Settings.Username.HasValue() && !LoginInProgress; // note: password not required for a login attempt

		/// <summary>Create a new Consumer instance</summary>
		public Consumer AddNewConsumer()
		{
			Settings.Consumers = Settings.Consumers.Append(new SettingsData.Consumer());
			return Consumers[Consumers.Count - 1];
		}

		/// <summary>Remove 'consumer'</summary>
		public void RemoveConsumer(Consumer consumer)
		{
			Settings.Consumers = Settings.Consumers.Except(consumer.Settings).ToArray();
		}

		/// <summary>Assign the Device in each Consumer (if found)</summary>
		private void PopulateConsumerDevices(IList<Consumer> consumers)
		{
			var devices = EweDevices.OfType<EweSwitch>().ToDictionary(x => x.Name, x => x);
			foreach (var consumer in consumers)
				consumer.EweSwitch = devices.TryGetValue(consumer.SwitchName, out var sw) ? sw : null;
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}

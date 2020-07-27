using System;
using System.Collections.Generic;
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
			ConsumersList = new List<Consumer>();
			Settings = new SettingsData(SettingsData.Filepath);
			Shutdown = new CancellationTokenSource();
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
					m_settings.SettingChange -= HandleSettingChange;
				}
				m_settings = value;
				if (m_settings != null)
				{
					m_settings.SettingChange += HandleSettingChange;
				}

				// Handlers
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
							var old_list = ConsumersList.ToList();

							// Synchronise the settings list with our list.
							ConsumersList.Assign(Settings.Consumers.Select(x => new Consumer(x)));
							PopulateConsumerDevices(ConsumersList);
							NotifyPropertyChanged(nameof(Consumers));

							// Dispose the old consumers after the notification to allow observers
							// to update their references to the new consumers first.
							Util.DisposeRange(old_list);
							break;
						}
					}
				}
			}
		}
		private SettingsData m_settings = null!;

		/// <summary>Shutdown token</summary>
		public CancellationTokenSource Shutdown { get; }

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
		private List<Consumer> ConsumersList { get; }

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
			}
		}
		private SolarData m_solar = null!;

		/// <summary>Enable/Disable controlling the power consumer switches</summary>
		public bool EnableMonitor
		{
			get => m_timer_enable_control != null && m_timer_enable_control.IsEnabled;
			set
			{
				if (EnableMonitor == value) return;
				if (m_timer_enable_control != null)
				{
					m_timer_enable_control.Stop();
				}
				value &= PollSolarData;
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
					catch (Exception ex)
					{
						LastError = ex;
					}
				}
			}
		}
		public bool EnableMonitorAvailable => PollSolarData || EnableMonitor; // Available if current on, or received solar data
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

		/// <summary>The sum of active consumers</summary>
		public double ConsumedPower
		{
			get => ActiveConsumers.Sum(x => x.Power ?? 0.0);
		}

		/// <summary>A string description of the consumers that are currently on</summary>
		public IEnumerable<Consumer> ActiveConsumers
		{
			get
			{
				// For each consumer...
				foreach (var consumer in Consumers)
				{
					// Find the controlling device...
					var device = EweDevices.FirstOrDefault(x => x.Name == consumer.SwitchName);

					// Return the consumer if the controlling device says it's active
					if (device is EweSwitch sw && sw.State == EweSwitch.ESwitchState.On)
						yield return consumer;
				}
			}
		}

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
		public async Task Logout() => await Ewe.Logout();
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

		/// <summary>Perform a check of the solar output to see whether consumers should be enabled</summary>
		private async Task RunMonitor()
		{
			// If not within the scheduled active time, do nothing
			if (!Sched.ActiveRanges().Any())
				return;

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
					required_power += c.Power ?? 0.0;
					return false;
				}

				// Not a controllable device...
				if (!c.Settings.Controllable)
				{
					required_power += c.Power ?? 0.0;
					return false;
				}

				// Todo: for now, ignore external control.
				//// Consumer is currently on, and wasn't turned on by us
				//return
				//	c.EweSwitch.State == EweSwitch.ESwitchState.On &&
				//	c.LastAppStateChange

				return true;
			}).ToList();

			// Enable/Disable each consumer based on available power
			foreach (var consumer in consumers)
			{
				// If the consumer knows its power consumption, use that otherwise fall-back to the required power
				var req_pwr = consumer.Power ?? consumer.RequiredPower;
				var turn_on = required_power + req_pwr < available_power;

				// If the consumer is not in the desired state, change it
				if (consumer.On != turn_on)
				{
					var state = turn_on ? EweSwitch.ESwitchState.On : EweSwitch.ESwitchState.Off;
					await Ewe.SwitchState(consumer.EweSwitch!, state, 0, Shutdown.Token);
				}

				// If the device is on, accumulate the required power
				if (turn_on)
				{
					required_power += req_pwr;
				}
			}
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
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}
}

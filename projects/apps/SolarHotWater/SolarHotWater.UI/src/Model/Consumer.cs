using System;
using System.ComponentModel;
using System.Diagnostics;
using EweLink;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
using Rylogic.Utility;
using SolarHotWater.Common;

namespace SolarHotWater
{
	[DebuggerDisplay("{Description,nq}")]
	public sealed class Consumer :IDisposable, INotifyPropertyChanged
	{
		public static event Action<Consumer, bool>? Genesis;

		public Consumer(SettingsData.Consumer settings)
		{
			Settings = settings;
			Genesis?.Invoke(this, true);
		}
		public void Dispose()
		{
			Genesis?.Invoke(this, false);
			EweSwitch = null!;
			Settings = null!;
		}

		/// <summary></summary>
		public SettingsData.Consumer Settings
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.SettingChange -= HandleSettingChange;
				}
				field = value;
				if (field != null)
				{
					field.SettingChange += HandleSettingChange;
				}

				// Handler
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
						case nameof(SettingsData.Consumer.ControlMode):
						{
							// Switching out of controlled mode cancels any pending state changes
							if (Settings.ControlMode != EControlMode.Controlled)
								StateChangePending = null;
							break;
						}
						default:
						{
							NotifyPropertyChanged(e.Key);
							break;
						}
					}
				}
			}
		} = null!;

		/// <summary>The switch that controls this consumer</summary>
		public EweSwitch? EweSwitch
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.PropertyChanged -= HandlePropChanged;
					field.Updated -= HandleSwitchUpdated;
				}
				field = value;
				if (field != null)
				{
					DeviceID = field.DeviceID;
					SwitchName = field.Name;
					field.Updated += HandleSwitchUpdated;
					field.PropertyChanged += HandlePropChanged;
				}
				NotifyPropertyChanged(nameof(EweSwitch));
				NotifyPropertyChanged(nameof(On));
				NotifyPropertyChanged(nameof(Voltage));
				NotifyPropertyChanged(nameof(Current));
				NotifyPropertyChanged(nameof(Power));

				// Handler
				void HandlePropChanged(object? sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
						case "switch":
						{
							// Record when the switch changes state, so we know whether
							// it was changed by this app or changed externally.
							LastStateChange = DateTimeOffset.Now;
							StateChangePending = null;
							NotifyPropertyChanged(nameof(On));
							break;
						}
						case "voltage":
						{
							NotifyPropertyChanged(nameof(Voltage));
							break;
						}
						case "current":
						{
							NotifyPropertyChanged(nameof(Current));
							break;
						}
						case "power":
						{
							NotifyPropertyChanged(nameof(Power));
							break;
						}
						case "partnerApikey":
						case "alarmVValue":
						case "alarmCValue":
						case "alarmPValue":
						case "timers":
						case "only_device":
						{
							break;
						}
						default:
						{
							Log.Write(ELogLevel.Debug, $"Consumer '{Name}': {e.PropertyName} state changed");
							break;
						}
					}
				}
				void HandleSwitchUpdated(object? sender, EventArgs e)
				{
					Updated?.Invoke(this, EventArgs.Empty);
				}
			}
		}

		/// <summary>Consumer name</summary>
		public string Name
		{
			get => Settings.Name;
			set => Settings.Name = value;
		}

		/// <summary>Identifying colour for this consumer</summary>
		public Colour32 Colour
		{
			get => Settings.Colour;
			set => Settings.Colour = value;
		}

		/// <summary>The device ID of the controlling switch</summary>
		public string DeviceID
		{
			get => Settings.DeviceID;
			set
			{
				if (value == null || value.Length == 0 || DeviceID == value) return;
				Settings.DeviceID = value;
			}
		}

		/// <summary>The name of the switch that controls this consumer</summary>
		public string SwitchName
		{
			get => Settings.SwitchName;
			set
			{
				if (value == null || value.Length == 0 || SwitchName == value) return;
				Settings.SwitchName = value;
			}
		}

		/// <summary>The power headroom needed for this consumer</summary>
		public double RequiredPower
		{
			get => Settings.RequiredPower;
			set => Settings.RequiredPower = value;
		}

		/// <summary>The cooldown time as a human readable string</summary>
		public string Cooldown
		{
			get => Settings.Cooldown.ToPrettyString();
			set => Settings.Cooldown = TimeSpan_.TryParseExpr(value) is TimeSpan ts ? ts : TimeSpan.Zero;
		}

		/// <summary>The voltage at the switch output</summary>
		public double? Voltage => EweSwitch?.Voltage;

		/// <summary>The current draw of this consumer</summary>
		public double? Current => EweSwitch?.Current;

		/// <summary>The power currently being used by this consumer (in kWatts)</summary>
		public double? Power => EweSwitch?.Power;

		/// <summary>Get/Set whether the consumer is on/off</summary>
		public bool On => EweSwitch?.State == EweSwitch.ESwitchState.On;

		/// <summary>An update for the switch state has been received</summary>
		public event EventHandler? Updated;

		/// <summary>Timestamp of when a state change is first pending</summary>
		public DateTimeOffset? StateChangePending
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(StateChangePending));
				NotifyPropertyChanged(nameof(StateChangeFrac));
			}
		}

		/// <summary>Timestamp of when the switch was last turned on/off</summary>
		public DateTimeOffset LastStateChange { get; private set; }

		/// <summary>Normalised time until state change</summary>
		public double StateChangeFrac => StateChangePending is DateTimeOffset t0 && Settings.Cooldown.TotalSeconds > 0
			? (DateTimeOffset.Now - t0).TotalSeconds / Settings.Cooldown.TotalSeconds :
			0.0;

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary></summary>
		public string Description => $"{Name}";
	}
}

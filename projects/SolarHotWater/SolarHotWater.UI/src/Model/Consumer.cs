﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EweLink;
using Rylogic.Common;
using Rylogic.Extn;

namespace SolarHotWater
{
	[DebuggerDisplay("{Description,nq}")]
	public sealed class Consumer :IDisposable, INotifyPropertyChanged
	{
		public Consumer(SettingsData.Consumer settings)
		{
			Settings = settings;
		}
		public void Dispose()
		{
			EweSwitch = null;
			Settings = null!;
		}

		/// <summary></summary>
		public SettingsData.Consumer Settings
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

				// Handler
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
					if (e.Before) return;
					switch (e.Key)
					{
						case nameof(SettingsData.Consumer.SwitchName):
						{
							NotifyPropertyChanged(nameof(EweSwitch));
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
		}
		private SettingsData.Consumer m_settings = null!;

		/// <summary>The switch that controls this consumer</summary>
		public EweSwitch? EweSwitch
		{
			get => m_ewe_switch;
			set
			{
				if (m_ewe_switch == value) return;
				if (m_ewe_switch != null)
				{
					m_ewe_switch.PropertyChanged -= HandlePropChanged;
				}
				m_ewe_switch = value;
				if (m_ewe_switch != null)
				{
					m_ewe_switch.PropertyChanged += HandlePropChanged;
				}

				// Handler
				void HandlePropChanged(object sender, PropertyChangedEventArgs e)
				{
					switch (e.PropertyName)
					{
						case "State":
						{
							// Record when the switch changes state, so we know whether
							// it was changed by this app or changed externally.
							LastStateChange = DateTimeOffset.Now;
							break;
						}
					}
					NotifyPropertyChanged(string.Empty);
				}
			}
		}
		private EweSwitch? m_ewe_switch;

		/// <summary>Consumer name</summary>
		public string Name
		{
			get => Settings.Name;
			set => Settings.Name = value;
		}

		/// <summary>The name of the switch that controls this consumer</summary>
		public string SwitchName
		{
			get => Settings.SwitchName;
			set => Settings.SwitchName = value;
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

		/// <summary>The priority order of this consumer</summary>
		public int? Priority
		{
			get => Settings.Parent is SettingsData settings ? settings.Consumers.IndexOf(Settings) : (int?)null;
			set
			{
				if (value == null) return;
				if (Settings.Parent is SettingsData settings && settings.Consumers.Length > 1)
				{
					var idx = Math.Clamp(value ?? 0, 0, settings.Consumers.Length);
					var consumers = settings.Consumers.ToList();
					consumers.Remove(Settings);
					consumers.Insert(idx, Settings);
					settings.Consumers = consumers.ToArray();
				}
			}
		}

		/// <summary>The voltage at the switch output</summary>
		public double? Voltage => EweSwitch?.Voltage;

		/// <summary>The current draw of this consumer</summary>
		public double? Current => EweSwitch?.Current;

		/// <summary>The power currently being used by this consumer</summary>
		public double? Power => EweSwitch?.Power;

		/// <summary>Get/Set whether the consumer is on/off</summary>
		public bool On => EweSwitch?.State == EweSwitch.ESwitchState.On;

		/// <summary>Time stamp of when the switch was last turned on/off</summary>
		public DateTimeOffset LastStateChange { get; private set; }

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	
		/// <summary></summary>
		public string Description => $"{Name}";
	}
}
using System;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace SolarHotWater
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			Username = string.Empty;
			Password = string.Empty;
			SolarInverterIP = "ShierlawJ.RyLAN";
			SolarPollPeriod = TimeSpan.FromSeconds(10);
			MonitorPeriod = TimeSpan.FromSeconds(10);
			ReservePower = 0.2;
			Consumers = Array.Empty<Consumer>();

			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.None)
		{
			AutoSaveOnChanges = true;
		}

		/// <summary></summary>
		public static new string Filepath => Util.ResolveUserDocumentsPath("Rylogic", "SolarHotWater", "settings.xml");

		/// <summary>EweLink Login Email</summary>
		public string Username
		{
			get => get<string>(nameof(Username));
			set => set(nameof(Username), value);
		}

		/// <summary>Plain text password...</summary>
		public string Password
		{
			get => get<string>(nameof(Password));
			set => set(nameof(Password), value);
		}

		/// <summary>The IP address of the Solar Inverter</summary>
		public string SolarInverterIP
		{
			get => get<string>(nameof(SolarInverterIP));
			set => set(nameof(SolarInverterIP), value);
		}

		/// <summary>How fast to poll the solar inverter</summary>
		public TimeSpan SolarPollPeriod
		{
			get => get<TimeSpan>(nameof(SolarPollPeriod));
			set => set(nameof(SolarPollPeriod), value);
		}

		/// <summary>The interval between checks for switching the hot water</summary>
		public TimeSpan MonitorPeriod
		{
			get => get<TimeSpan>(nameof(MonitorPeriod));
			set => set(nameof(MonitorPeriod), value);
		}
	
		/// <summary>The power headroom to leave (in kW)</summary>
		public double ReservePower
		{
			get => get<double>(nameof(ReservePower));
			set => set(nameof(ReservePower), value);
		}

		/// <summary>Consumer instances</summary>
		public Consumer[] Consumers
		{
			get => get<Consumer[]>(nameof(Consumers));
			set => set(nameof(Consumers), value);
		}
		public class Consumer :SettingsSet<Consumer>
		{
			public Consumer()
			{
				Name = "New Consumer";
				SwitchName = string.Empty;
				RequiredPower = 0;
				Cooldown = TimeSpan.Zero;
				Controllable = true;
			}

			/// <summary>The name of the consumer</summary>
			public string Name
			{
				get => get<string>(nameof(Name));
				set => set(nameof(Name), value);
			}

			/// <summary>The name of the eWeLink switch that controls the consumer</summary>
			public string SwitchName
			{
				get => get<string>(nameof(SwitchName));
				set => set(nameof(SwitchName), value);
			}

			/// <summary>The required supply power for the consumer (in kW)</summary>
			public double RequiredPower
			{
				get => get<double>(nameof(RequiredPower));
				set => set(nameof(RequiredPower), value);
			}

			/// <summary>The minimum time between changing switch state</summary>
			public TimeSpan Cooldown
			{
				get => get<TimeSpan>(nameof(Cooldown));
				set => set(nameof(Cooldown), value);
			}

			/// <summary>True if the app is allowed to control this device</summary>
			public bool Controllable
			{
				get => get<bool>(nameof(Controllable));
				set => set(nameof(Controllable), value);
			}
		}
	}
}

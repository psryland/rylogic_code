using System;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gfx;
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
			MonitorPeriod = TimeSpan.FromSeconds(1);
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
				Colour = NextColour();
				DeviceID = string.Empty;
				SwitchName = string.Empty;
				RequiredPower = 0;
				Cooldown = TimeSpan.Zero;
				ControlMode = EControlMode.Observed;
			}

			/// <summary>The name of the consumer</summary>
			public string Name
			{
				get => get<string>(nameof(Name));
				set => set(nameof(Name), value);
			}

			/// <summary>Identifying colour</summary>
			public Colour32 Colour
			{
				get => get<Colour32>(nameof(Colour));
				set => set(nameof(Colour), value);
			}

			/// <summary>The Unique ID of the EweLink switch that controls this consumer</summary>
			public string DeviceID
			{
				get => get<string>(nameof(DeviceID));
				set => set(nameof(DeviceID), value);
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

			/// <summary>What to do with this consumer</summary>
			public EControlMode ControlMode
			{
				get => get<EControlMode>(nameof(ControlMode));
				set => set(nameof(ControlMode), value);
			}

			/// <summary>Colour sequence for issuing changing colours to things</summary>
			private static Colour32 NextColour() => m_next_colour[m_next_colour_index++ % m_next_colour.Length];
			private static uint[] m_next_colour = new[] { 0xFF0000FF, 0xFF00FF00, 0xFFFF0000, 0xFFFFFF00, 0xFFFF00FF, 0xFF00FFFF, 0xFF80FF80, 0xFFFF8080, 0xFF8080FF };
			private static int m_next_colour_index;
		}
	}
}

using System;
using Rylogic.Common;

namespace SolarHotWater
{
	public class SettingsData :SettingsBase<SettingsData>
	{
		public SettingsData()
		{
			Username = string.Empty;
			Password = string.Empty;
			SolarInverterIP = "ShierlawJ.RyLAN";
			SolarPollRate = TimeSpan.FromSeconds(10);
			AutoSaveOnChanges = true;
		}
		public SettingsData(string filepath)
			: base(filepath, ESettingsLoadFlags.None)
		{
			AutoSaveOnChanges = true;
		}

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
		public TimeSpan SolarPollRate
		{
			get => get<TimeSpan>(nameof(SolarPollRate));
			set => set(nameof(SolarPollRate), value);
		}
	}
}

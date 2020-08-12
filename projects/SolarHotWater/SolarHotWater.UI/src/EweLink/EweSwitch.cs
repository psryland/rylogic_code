using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace EweLink
{
	/// <summary>Switch device</summary>
	public class EweSwitch :EweDevice
	{
		//"version": 8,
		//"sledOnline": "on",
		//"staMac": "CC:50:E3:68:2F:D6",
		//"rssi": -68,
		//"init": 1,
		//"pulse": "off",
		//"pulseWidth": 500,
		//"oneKwh": "stop",
		//"timeZone": 13,
		//"hundredDaysKwh": "get"

		public EweSwitch(JObject jdevice)
			: base(jdevice)
		{ }

		/// <summary>Switch state</summary>
		public ESwitchState State
		{
			get => Params["switch"]?.Value<string>() switch
			{
				"off" => ESwitchState.Off,
				"on" => ESwitchState.On,
				_ => throw new Exception($"Unknown switch state: {Params["switch"]?.Value<string>()}"),
			};
			set => Params["switch"] = value switch
			{
				ESwitchState.Off => "off",
				ESwitchState.On => "on",
				_ => null
			};
		}
		public enum ESwitchState
		{
			Off,
			On,
			Toggle, // Internal only, Devices never have this state
		}

		/// <summary>What the switch does on power up</summary>
		public EStartupMode? StartupMode
		{
			get => Params["startup"]?.Value<string>() switch
			{
				"off" => EStartupMode.Off,
				"on" => EStartupMode.On,
				"stay" => EStartupMode.Stay,
				_ => throw new Exception($"Unknown startup mode: {Params["startup"]?.Value<string>()}"),
			};
		}
		public enum EStartupMode
		{
			Off,
			On,
			Stay,
		}

		/// <summary>True if the switch is on</summary>
		public bool On => State == ESwitchState.On;

		/// <summary>The number of channels for the switch</summary>
		public int ChannelCount => Manufacturer.UIID.ChannelCount();

		/// <summary>Current power (in W) draw (if known)</summary>
		public double? Power => Params["power"]?.Value<double>();

		/// <summary>Current voltage (if known)</summary>
		public double? Voltage => Params["voltage"]?.Value<double>();

		/// <summary>Current current draw (if known)</summary>
		public double? Current => Params["current"]?.Value<double>();

		/// <summary>Something to do with the LED on the switch I think</summary>
		public int? UIActive => Params["uiActive"]?.Value<int>();

		/// <summary>Device firmware version</summary>
		public string FirmwareVersion => Params["fwVersion"]?.Value<string>() ?? string.Empty;

		/// <summary>Network MAC address</summary>
		public string MACAddress => Params["staMac"]?.Value<string>() ?? string.Empty;

		/// <summary>Timers stored on this switch</summary>
		public IEnumerable<TimerData> Timers
		{
			get
			{
				if (!(Params["timers"] is JArray jtimers))
					yield break;
				foreach (var jtimer in jtimers.Cast<JObject>())
					yield return new TimerData(jtimer);
			}
		}
		public class TimerData
		{
			private readonly JObject m_jtimer;
			public TimerData(JObject jtimer) => m_jtimer = jtimer;

			/// <summary>Timer enabled</summary>
			public bool Enabled => m_jtimer["enabled"]?.Value<int>() == 1;

			/// <summary>Timer type</summary>
			public EType Type => m_jtimer["coolkit_timer_type"]?.Value<string>() switch
			{
				"once" => EType.Once,
				"delay" => EType.Delay,
				"repeat" => EType.Repeat,
				_ => throw new Exception($"Unknown timer type: {m_jtimer["coolkit_timer_type"]}"),
			};
			public enum EType
			{
				Once,
				Delay,
				Repeat,
			}

			/// <summary>What happens when this timer fires</summary>
			public JObject? Do => m_jtimer["do"] as JObject;

			/// <summary>When the timer fires. Formatted string example: "0 11 * * 1,2,3,4,5,6,0" </summary>
			public string At => m_jtimer["at"]?.Value<string>() ?? string.Empty;

			/// <summary>Timer entry UID</summary>
			public Guid ID => m_jtimer["mId"]?.Value<string>() is string guid_string ? new Guid(guid_string) : Guid.Empty;
		}

		/// <summary>Level alarms</summary>
		public IEnumerable<AlarmData> Alarms
		{
			get
			{
				var alarm_type = Params["alarmType"]?.Value<string>() ?? string.Empty;
				if (alarm_type.Contains('v') && Params["alarmVValue"] is JArray jvalarm)
					yield return new AlarmData(AlarmData.EType.Voltage, jvalarm);
				if (alarm_type.Contains('c') && Params["alarmCValue"] is JArray jcalarm)
					yield return new AlarmData(AlarmData.EType.Current, jcalarm);
				if (alarm_type.Contains('p') && Params["alarmPValue"] is JArray jpalarm)
					yield return new AlarmData(AlarmData.EType.Power, jpalarm);
			}
		}
		public class AlarmData
		{
			private readonly JArray m_jdata;
			public AlarmData(EType type, JArray jdata)
			{
				Type = type;
				m_jdata = jdata;
			}

			/// <summary>Alarm type</summary>
			public EType Type { get; }
			public enum EType
			{
				Voltage,
				Current,
				Power,
			}

			/// <summary>Alarm thresholds (i think)</summary>
			public long Min => m_jdata[0].Value<long>();
			public long Max => m_jdata[1].Value<long>();
		}
	}
}

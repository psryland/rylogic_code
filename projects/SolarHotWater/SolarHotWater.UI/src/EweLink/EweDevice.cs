using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rylogic.Script;

namespace EweLink
{
	public class EweDevice :INotifyPropertyChanged
	{
		protected readonly JObject m_jdevice;
		protected EweDevice(JObject jdevice)
		{
			m_jdevice = jdevice;
			Params = m_jdevice["params"] as JObject ?? new JObject();
			Manufacturer = new ManufacturerData(m_jdevice["extra"]?["extra"] as JObject);
		}

		/// <summary>Device name</summary>
		public string Name
		{
			get => m_jdevice["name"]?.Value<string>() ?? string.Empty;
			set => m_jdevice["name"] = value;
		}

		/// <summary>Device ID</summary>
		public string DeviceID
		{
			get => m_jdevice["deviceid"]?.Value<string>() ?? string.Empty;
			set => m_jdevice["deviceid"] = value;
		}

		/// <summary>Extra manufacturer data</summary>
		public ManufacturerData Manufacturer { get; }

		/// <summary>Device parameters</summary>
		protected JObject Params { get; }

		/// <summary>Apply an update to the device</summary>
		public void Update(JObject parms)
		{
			// Merge 'parms' into 'Params'
			foreach (var parm in parms)
			{
				// If the parameter exists and is changing, notify
				if (Params.TryGetValue(parm.Key, out var value) && !Equals(value, parm.Value))
					NotifyPropertyChanged(parm.Key);

				// Set the new state of the parameter
				Params[parm.Key] = parm.Value;
			}

			// Notify that the update occurred
			NotifyPropertyChanged(string.Empty);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}
	}

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

		/// <summary>Current power draw (if known)</summary>
		public double? Power
		{
			get => Params["power"]?.Value<double>();
		}

		/// <summary>Current voltage (if known)</summary>
		public double? Voltage
		{
			get => Params["voltage"]?.Value<double>();
		}

		/// <summary>Current current draw (if known)</summary>
		public double? Current
		{
			get => Params["current"]?.Value<double>();
		}

		/// <summary>Something to do with the LED on the switch I think</summary>
		public int? UIActive
		{
			get => Params["uiActive"]?.Value<int>();
		}

		/// <summary>Device firmware version</summary>
		public string FirmwareVersion
		{
			get => Params["fwVersion"]?.Value<string>() ?? string.Empty;
		}

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

	/// <summary></summary>
	public class ManufacturerData
	{
		private readonly JObject m_jmanu;
		public ManufacturerData(JObject? jmanu)
		{
			m_jmanu = jmanu ?? new JObject();
		}

		/// <summary></summary>
		public int UIID
		{
			get => m_jmanu["uiid"]?.Value<int>() ?? 0;
			set => m_jmanu["uiid"] = value;
		}

		/// <summary></summary>
		public string Description
		{
			get => m_jmanu["description"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["description"] = value;
		}

		/// <summary></summary>
		public string BrandID
		{
			get => m_jmanu["brandId"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["brandId"] = value;
		}

		/// <summary></summary>
		public string Manufacturer
		{
			get => m_jmanu["manufacturer"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["manufacturer"] = value;
		}

		/// <summary></summary>
		public string Model
		{
			get => m_jmanu["model"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["model"] = value;
		}

		/// <summary></summary>
		public string ModelInfo
		{
			get => m_jmanu["modelInfo"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["modelInfo"] = value;
		}

		/// <summary></summary>
		public string MAC
		{
			get => m_jmanu["mac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["mac"] = value;
		}

		/// <summary></summary>
		public string ApMAC
		{
			get => m_jmanu["apmac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["apmac"] = value;
		}

		/// <summary></summary>
		public string StaMAC
		{
			get => m_jmanu["staMac"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["staMac"] = value;
		}

		/// <summary></summary>
		public string UI
		{
			get => m_jmanu["ui"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["ui"] = value;
		}

		/// <summary></summary>
		public string ChipID
		{
			get => m_jmanu["chipid"]?.Value<string>() ?? string.Empty;
			set => m_jmanu["chipid"] = value;
		}
	}




#if false
	public class EweDevice
	{
		public enum EType
		{
			Unknown,
			Switch,
		}

		/// <summary>Device name</summary>
		[JsonProperty("name")]
		public string Name { get; set; } = string.Empty;

		/// <summary>Device type</summary>
		public EType Type { get; set; } = EType.Unknown;
		[JsonProperty("type")] private string TypeInternal
		{
			set
			{
				switch (value)
				{
					case "10":
					{
						Type = EType.Switch;
						break;
					}
					default:
					{
						Type = EType.Unknown;
						break;
					}
				}
			}
		}

		/// <summary>Device ID</summary>
		[JsonProperty("deviceid")]
		public string DeviceID { get; set; } = string.Empty;

	#region Irrelevant

		/// <summary></summary>
		[JsonProperty("_id")]
		private string ID { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("online")]
		private bool Online { get; set; } = false;

		/// <summary></summary>
		[JsonProperty("group")]
		private string Group { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("groups")]
		private object[] Groups { get; set; } = Array.Empty<object>();

		/// <summary></summary>
		[JsonProperty("devGroups")]
		private object[] DevGroups { get; set; } = Array.Empty<object>();

		/// <summary></summary>
		[JsonProperty("shareUsersInfo")]
		private object[] ShareUsersInfo { get; set; } = Array.Empty<object>();

		/// <summary></summary>
		[JsonProperty("apikey")]
		private string ApiKey { get; set; } = string.Empty;
		
		/// <summary></summary>
		[JsonProperty("createdAt")]
		private DateTimeOffset CreateAt { get; set; } = DateTimeOffset.MinValue;

		/// <summary></summary>
		[JsonProperty("__v")]
		private int Version { get; set; } = 0;

		/// <summary></summary>
		[JsonProperty("onlineTime")]
		private DateTimeOffset OnlineTime { get; set; } = DateTimeOffset.MinValue;

		/// <summary></summary>
		[JsonProperty("ip")]
		private string IP { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("location")]
		private string Location { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("offlineTime")]
		private DateTimeOffset OfflineTime { get; set; } = DateTimeOffset.MinValue;

		/// <summary></summary>
		[JsonProperty("deviceStatus")]
		private string DeviceStatus { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("sharedTo")]
		private object[] SharedTo { get; set; } = Array.Empty<object>();

		/// <summary></summary>
		[JsonProperty("devicekey")]
		private string DeviceKey { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("deviceUrl")]
		private string DeviceUrl { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("brandName")]
		private string BrandName { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("showBrand")]
		private bool ShowBrand { get; set; } = false;

		/// <summary></summary>
		[JsonProperty("brandLogoUrl")]
		private string BrandLogoUrl { get; set; } = string.Empty;

		/// <summary>/// </summary>
		[JsonProperty("productModel")]
		private string ProductModel { get; set; } = string.Empty;

		/// <summary></summary>
		[JsonProperty("devConfig")]
		private object DevConfig { get; set; } = new object();

		/// <summary></summary>
		[JsonProperty("uiid")]
		private int UIID { get; set; } = 0;

		/// <summary></summary>
		[JsonProperty("settings")]
		private SettingsData Settings { get; set; } = new SettingsData();
		private class SettingsData
		{
			/// <summary></summary>
			[JsonProperty("opsNotify")]
			public int OpsNotify { get; set; }

			/// <summary></summary>
			[JsonProperty("opsHistory")]
			public int OpsHistory { get; set; }

			/// <summary></summary>
			[JsonProperty("alarmNotify")]
			public int AlarmNotify { get; set; }

			/// <summary></summary>
			[JsonProperty("wxAlarmNotify")]
			public int WxAlarmNotify { get; set; }

			/// <summary></summary>
			[JsonProperty("wxOpsNotify")]
			public int WxOpsNotify { get; set; }

			/// <summary></summary>
			[JsonProperty("wxDoorbellNotify")]
			public int WxDoorbellNotify { get; set; }

			/// <summary></summary>
			[JsonProperty("appDoorbellNotify")]
			public int AppDoorbellNotify { get; set; }
		}

		/// <summary></summary>
		[JsonProperty("family")]
		private FamilyData Family { get; set; } = new FamilyData();
		private class FamilyData
		{
			/// <summary></summary>
			[JsonProperty("id")]
			public string ID { get; set; } = string.Empty;

			/// <summary></summary>
			[JsonProperty("index")]
			public int Index { get; set; } = 0;
		}

		/// <summary></summary>
		[JsonProperty("extra")]
		private DeviceData Extra { get; set; } = new DeviceData();
		private class DeviceData
		{
			/// <summary></summary>
			[JsonProperty("_id")]
			public string ID { get; set; } = string.Empty;

			[JsonProperty("extra")]
			public PropsData Extra { get; set; } = new PropsData();
			public class PropsData
			{
				[JsonProperty("uiid")]
				public int UIID { get; set; } = 0;

				[JsonProperty("description")]
				public string Description { get; set; } = string.Empty;

				[JsonProperty("brandId")]
				public string BrandID { get; set; } = string.Empty;

				[JsonProperty("apmac")]
				public string ApMAC { get; set; } = string.Empty;

				[JsonProperty("mac")]
				public string MAC { get; set; } = string.Empty;

				[JsonProperty("ui")]
				public string UI { get; set; } = string.Empty;

				[JsonProperty("modelInfo")]
				public string ModelInfo { get; set; } = string.Empty;

				[JsonProperty("model")]
				public string Model { get; set; } = string.Empty;

				[JsonProperty("manufacturer")]
				public string Manufacturer { get; set; } = string.Empty;
				
				[JsonProperty("chipid")]
				public string ChipID { get; set; } = string.Empty;

				[JsonProperty("staMac")]
				public string StaMAC { get; set; } = string.Empty;
			}
		}

		/// <summary></summary>
		[JsonProperty("params")]
		private ParamsData Params { get; set; } = new ParamsData();
		private class ParamsData
		{
			[JsonProperty("version")]
			public int Version { get; set; } = 0;

			[JsonProperty("sledOnline")]
			public string SledOnline { get; set; } = string.Empty;

			[JsonProperty("switch")]
			public string Switch { get; set; } = string.Empty;

			[JsonProperty("fwVersion")]
			public string FirmwareVersion { get; set; } = string.Empty;

			[JsonProperty("rssi")]
			public int RSSI { get; set; } = 0;

			[JsonProperty("staMac")]
			public string StaMAC { get; set; } = string.Empty;

			[JsonProperty("startup")]
			public string Startup { get; set; } = string.Empty;

			[JsonProperty("init")]
			public int Init { get; set; } = 0;

			[JsonProperty("pulse")]
			public string Pulse { get; set; } = string.Empty;

			[JsonProperty("pulseWidth")]
			public int PulseWidth { get; set; } = 0;

			[JsonProperty("uiActive")]
			public int UIActive { get; set; } = 0;

			[JsonProperty("timers")]
			public TimerData[] Timers { get; set; } = Array.Empty<TimerData>();
			public class TimerData
			{
				[JsonProperty("enabled")]
				public int Enabled { get; set; } = 0;

				[JsonProperty("coolkit_timer_type")]
				public string CoolKitTimerType { get; set; } = string.Empty;

				[JsonProperty("at")]
				public string At { get; set; } = string.Empty;

				[JsonProperty("type")]
				public string Type { get; set; } = string.Empty;
				
				[JsonProperty("do")]
				public DoData Do { get; set; } = new DoData();

				[JsonProperty("mId")]
				public string mID { get; set; } = string.Empty;

				public class DoData
				{
					[JsonProperty("switch")]
					public string Switch { get; set; } = string.Empty;
				}
			}
		}

#if false
		/// <summary></summary>
		[JsonProperty("tags")]
		private TagsData Tags { get; set; } = new TagsData();
		private class TagsData :Dictionary<string,string>
		{}
#endif

	#endregion
	}
#endif

	//public class EweSwitch
	//{
	//	private readonly EweDevice m_device;
	//	private EweSwitch(EweDevice device) => m_device = device;

	//	public bool SwitchState
	//}
}

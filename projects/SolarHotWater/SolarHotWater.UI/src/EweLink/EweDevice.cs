using System.ComponentModel;
using Newtonsoft.Json.Linq;

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
		public JObject Params { get; }

		/// <summary>Apply an update to the device</summary>
		public void Update(JObject parms)
		{
			// Merge 'parms' into 'Params'
			foreach (var parm in parms)
			{
				// If the parameter exists and is changing, notify
				var notify = Params.TryGetValue(parm.Key, out var value) && !Equals(value, parm.Value);

				// Set the new state of the parameter
				Params[parm.Key] = parm.Value;

				// Notify after changed
				if (notify)
					NotifyPropertyChanged(parm.Key);
			}
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
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
}

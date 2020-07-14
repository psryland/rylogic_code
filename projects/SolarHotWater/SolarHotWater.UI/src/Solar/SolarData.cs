using System;
using System.Collections.Generic;
using System.Windows.Navigation;
using Newtonsoft.Json;

namespace SolarHotWater
{
	public class SolarData
	{
		/// <summary>Current AC power (in kWatts)</summary>
		public double CurrentPower => Body.Data.PowerAC.Values.TryGetValue("1", out var pac) ? pac * 0.001 : 0.0;

		/// <summary>The time when this data was collected</summary>
		public DateTimeOffset Timestamp => Head.Timestamp;

		/// <summary></summary>
		[JsonProperty("Head")]
		private HeadData Head { get; set; } = new HeadData();

		/// <summary></summary>
		[JsonProperty("Body")]
		private BodyData Body { get; set; } = new BodyData();

		public class HeadData
		{
			/// <summary></summary>
			[JsonProperty("RequestArguments")]
			public RequestArgumentsData RequestArguments { get; set; } = new RequestArgumentsData();

			/// <summary></summary>
			[JsonProperty("Status")]
			public StatusData Status { get; set; } = new StatusData();

			/// <summary></summary>
			[JsonProperty("Timestamp")]
			public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.MinValue;

			public class RequestArgumentsData
			{
				/// <summary></summary>
				[JsonProperty("DeviceClass")]
				public string DeviceClass { get; set; } = string.Empty;

				/// <summary></summary>
				[JsonProperty("Scope")]
				public string Scope { get; set; } = string.Empty;
			}
			public class StatusData
			{
				/// <summary></summary>
				[JsonProperty("Code")]
				public int Code { get; set; } = 0;

				/// <summary></summary>
				[JsonProperty("Reason")]
				public string Reason { get; set; } = string.Empty;

				/// <summary></summary>
				[JsonProperty("UserMessage")]
				public string UserMessage { get; set; } = string.Empty;
			}
		}
		public class BodyData
		{
			/// <summary></summary>
			[JsonProperty("Data")]
			public FieldData Data { get; set; } = new FieldData();

			public class FieldData
			{
				/// <summary></summary>
				[JsonProperty("DAY_ENERGY")]
				public Field DayEnergy { get; set; } = new Field();

				/// <summary></summary>
				[JsonProperty("PAC")]
				public Field PowerAC { get; set; } = new Field();

				/// <summary></summary>
				[JsonProperty("TOTAL_ENERGY")]
				public Field TotalEnergy { get; set; } = new Field();

				/// <summary></summary>
				[JsonProperty("YEAR_ENERGY")]
				public Field YearEnergy { get; set; } = new Field();
			}
			public class Field
			{
				/// <summary></summary>
				[JsonProperty("Values")]
				public Dictionary<string, long> Values { get; set; } = new Dictionary<string, long>();

				/// <summary></summary>
				[JsonProperty("Unit")]
				public string Unit { get; set; } = string.Empty;
			}
		}
	}
}



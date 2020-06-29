using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace SolarHotWater
{
	public class SolarData
	{
		public SolarData()
		{
			Head = new HeadData();
			Body = new BodyData();
		}

		/// <summary></summary>
		[JsonProperty("Head")]
		public HeadData Head { get; set; }

		/// <summary></summary>
		[JsonProperty("Body")]
		public BodyData Body { get; set; }

		public class HeadData
		{
			public HeadData()
			{
				RequestArguments = new RequestArgumentsData();
				Status = new StatusData();
				Timestamp = DateTimeOffset.MinValue;
			}

			/// <summary></summary>
			[JsonProperty("RequestArguments")]
			public RequestArgumentsData RequestArguments { get; set; }

			/// <summary></summary>
			[JsonProperty("Status")]
			public StatusData Status { get; set; }

			/// <summary></summary>
			[JsonProperty("Timestamp")]
			public DateTimeOffset Timestamp { get; set; }

			public class RequestArgumentsData
			{
				public RequestArgumentsData()
				{
					DeviceClass = string.Empty;
					Scope = string.Empty;
				}

				/// <summary></summary>
				[JsonProperty("DeviceClass")]
				public string DeviceClass { get; set; }

				/// <summary></summary>
				[JsonProperty("Scope")]
				public string Scope { get; set; }
			}
			public class StatusData
			{
				public StatusData()
				{
					Code = 0;
					Reason = string.Empty;
					UserMessage = string.Empty;
				}

				/// <summary></summary>
				[JsonProperty("Code")]
				public int Code { get; set; }

				/// <summary></summary>
				[JsonProperty("Reason")]
				public string Reason { get; set; }

				/// <summary></summary>
				[JsonProperty("UserMessage")]
				public string UserMessage { get; set; }
			}
		}
		public class BodyData
		{
			public BodyData()
			{
				Data = new FieldData();
			}
			
			/// <summary></summary>
			[JsonProperty("Data")]
			public FieldData Data { get; set; }

			public class FieldData
			{
				public FieldData()
				{
					DayEnergy = new Field();
					PowerAC = new Field();
					TotalEnergy = new Field();
					YearEnergy = new Field();
				}

				/// <summary></summary>
				[JsonProperty("DAY_ENERGY")]
				public Field DayEnergy { get; set; }

				/// <summary></summary>
				[JsonProperty("PAC")]
				public Field PowerAC { get; set; }

				/// <summary></summary>
				[JsonProperty("TOTAL_ENERGY")]
				public Field TotalEnergy { get; set; }

				/// <summary></summary>
				[JsonProperty("YEAR_ENERGY")]
				public Field YearEnergy { get; set; }
			}
			public class Field
			{
				public Field()
				{
					Values = new Dictionary<string, long>();
					Unit = string.Empty;
				}

				/// <summary></summary>
				[JsonProperty("Values")]
				public Dictionary<string, long> Values { get; set; }

				/// <summary></summary>
				[JsonProperty("Unit")]
				public string Unit { get; set; }
			}
		}
	}
}



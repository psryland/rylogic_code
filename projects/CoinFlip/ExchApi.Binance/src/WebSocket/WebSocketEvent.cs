using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API
{
	/// <summary>Event header common to all binary web socket events</summary>
	public class WebSocketEvent
	{
		[JsonProperty("e")]
		public string EventType { get; set; }

		[JsonProperty("E"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset EventTime { get; set; }

		/// <summary>Type of event</summary>
		public static class EventName
		{
			public const string AccountInfo = "outboundAccountInfo";
			public const string AccountPosition = "outboundAccountPosition";
			public const string OrderTrade = "executionReport";
		}
	}
}

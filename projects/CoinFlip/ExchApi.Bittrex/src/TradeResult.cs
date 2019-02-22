using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Bittrex.API
{
	[DebuggerDisplay("{Id}")]
	public class TradeResult
	{
		/// <summary>The order Id for an order added to the order book of the traded pair</summary>
		public Guid Id { get; private set; }
		[JsonProperty("uuid")] internal string UuidInternal
		{
			set { Id = Guid.Parse(value); }
		}
	}

	public class SubmitTradeResponse
	{
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

		[JsonProperty("result")]
		public TradeResult Data { get; internal set; }
	}

	public class CancelTradeResponse
	{
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

		[JsonProperty("result")]
		public TradeResult Data { get; internal set; }
	}
}

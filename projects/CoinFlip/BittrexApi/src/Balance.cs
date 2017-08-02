using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Bittrex.API
{
	[DebuggerDisplay("Avail={Available}")]
	public class Balance
	{
		[JsonProperty("Currency")]
		public string Symbol { get; internal set; }

		[JsonProperty("Balance")]
		public decimal Total { get; internal set; }

		[JsonProperty("Available")]
		public decimal Available { get; internal set; }

		[JsonProperty("Pending")]
		public decimal Pending { get; internal set; }

		[JsonProperty("CryptoAddress")]
		public string CryptoAddress { get; internal set; }

		[JsonProperty("Requested")]
		public bool Requested { get; internal set; }

		[JsonProperty("Uuid")]
		public string Uuid { get; internal set; }
	}

	public class BalanceResponse
	{
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

		[JsonProperty("result")]
		public List<Balance> Data { get; internal set; }
	}
}

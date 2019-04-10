using System.Diagnostics;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	[DebuggerDisplay("Avail={Available}")]
	public class Balance
	{
		public Balance()
		{ }
		public Balance(string symbol)
		{
			Symbol = symbol;
		}

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
}

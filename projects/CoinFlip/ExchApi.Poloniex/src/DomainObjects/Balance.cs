using System.Diagnostics;
using Newtonsoft.Json;

namespace Poloniex.API.DomainObjects
{
	[DebuggerDisplay("Avail={Available}")]
	public class Balance
	{
		[JsonProperty("available")]
		public double Available { get; private set; }

		[JsonProperty("onOrders")]
		public double HeldForTrades { get; private set; }

		[JsonProperty("btcValue")]
		public double BitcoinValue { get; private set; }
	}
}

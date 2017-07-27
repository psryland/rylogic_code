using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Poloniex.API
{
	[DebuggerDisplay("Avail={Available}")]
	public class Balance
	{
		[JsonProperty("available")]
		public decimal Available { get; private set; }

		[JsonProperty("onOrders")]
		public decimal HeldForTrades { get; private set; }

		[JsonProperty("btcValue")]
		public decimal BitcoinValue { get; private set; }
	}
}

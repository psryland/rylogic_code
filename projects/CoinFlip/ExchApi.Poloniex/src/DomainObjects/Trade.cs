using System;
using System.Diagnostics;
using ExchApi.Common;
using Newtonsoft.Json;

namespace Poloniex.API.DomainObjects
{
	[DebuggerDisplay("{Type} Price={PricePerCoin} Vol={VolumeBase}")]
	public class Trade
	{
		/// <summary>The time of the trade (in UTC)</summary>
		public DateTimeOffset Time { get; private set; }
		[JsonProperty("date")] private string TimeInternal
		{
			set { Time = Misc.ParseDateTime(value); }
		}

		/// <summary>The type of trade; Buy or Sell</summary>
		public EOrderSide Type { get; private set; }
		[JsonProperty("type")] private string TypeInternal
		{
			set { Type = Conv.ToOrderType(value); }
		}

		/// <summary>The price of the offer</summary>
		[JsonProperty("rate")]
		public decimal PricePerCoin { get; private set; }

		/// <summary>The trade volume (in quote currency)</summary>
		[JsonProperty("amount")]
		public decimal VolumeQuote { get; private set; }

		/// <summary>The trade volume (in base currency)</summary>
		[JsonProperty("total")]
		public decimal VolumeBase { get; private set; }
	}
}

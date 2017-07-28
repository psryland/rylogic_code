using System;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Poloniex.API
{
	/// <summary>A history trade</summary>
	[DebuggerDisplay("{Type} Price={Price} Vol={VolumeBase}")]
	public class Historic
	{
		internal Historic()
		{}

		/// <summary>The pair that this position is held on</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>The ID of the order that this trade is associated with</summary>
		[JsonProperty("orderNumber")]
		public ulong OrderId { get; internal set; }

		/// <summary>A unique ID for the trade</summary>
		[JsonProperty("tradeID")]
		public ulong TradeId { get; internal set; }

		/// <summary>A Buy or Sell order</summary>
		public EOrderType Type { get; private set; }
		[JsonProperty("type")] private string TypeInternal
		{
			set { Type = Misc.ToOrderType(value); }
		}

		/// <summary></summary>
		[JsonProperty("globalTradeID")]
		public ulong GlobalTradeId { get; internal set; }

		/// <summary>The trade price (in quote currency)</summary>
		[JsonProperty("rate")]
		public decimal Price { get; internal set; }

		/// <summary>The trade volume remaining (in base currency)</summary>
		[JsonProperty("amount")]
		public decimal VolumeBase { get; private set; }

		/// <summary>The value of the trade (equal to Price * VolumeBase) (in quote currency)</summary>
		[JsonProperty("total")]
		public decimal Total { get; private set; }

		/// <summary>The fee charged</summary>
		[JsonProperty("fee")]
		public decimal Fee { get; private set; }

		/// <summary>The order time stamp</summary>
		public DateTimeOffset Timestamp { get; private set; }
		[JsonProperty("date")] private string TimestampInternal
		{
			set { Timestamp = DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); }
		}
	}
}

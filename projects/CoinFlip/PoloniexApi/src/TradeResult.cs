﻿
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;

namespace Poloniex.API
{
	[DebuggerDisplay("{OrderId} Filled={FilledOrders}")]
	public class TradeResult
	{
		internal TradeResult()
		{
			FilledOrders = new List<FilledOrder>();
		}

		/// <summary>The order Id for an order added to the order book of the traded pair</summary>
		[JsonProperty("orderNumber")]
		public ulong OrderId { get; internal set; }

		/// <summary>The trades that filled or partially filled the order</summary>
		[JsonProperty("resultingTrades")]
		public List<FilledOrder> FilledOrders { get; internal set; }

		public class FilledOrder
		{
			/// <summary></summary>
			[JsonProperty("tradeID")]
			public ulong TradeId { get; internal set; }

			/// <summary>A Buy or Sell order</summary>
			public EOrderType Type { get; private set; }
			[JsonProperty("type")] private string TypeInternal
			{
				set { Type = Misc.ToOrderType(value); }
			}

			/// <summary>The trade price (in quote currency)</summary>
			[JsonProperty("rate")]
			public decimal Price { get; internal set; }

			/// <summary>The traded volume (in base currency)</summary>
			[JsonProperty("amount")]
			public decimal VolumeBase { get; private set; }

			/// <summary>The value of the trade (equal to Price * VolumeBase) (in quote currency)</summary>
			[JsonProperty("total")]
			public decimal Total { get; private set; }

			/// <summary>The trade time stamp</summary>
			public DateTimeOffset Timestamp { get; private set; }
			[JsonProperty("date")] private string TimestampInternal
			{
				set { Timestamp = DateTimeOffset.ParseExact(value, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture); }
			}
		}
	}
}
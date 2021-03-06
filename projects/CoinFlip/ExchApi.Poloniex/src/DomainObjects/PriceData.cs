﻿using System.Diagnostics;
using Newtonsoft.Json;

namespace Poloniex.API.DomainObjects
{
	[DebuggerDisplay("Ask={OrderTopSell} Bid={OrderTopBuy}")]
	public class PriceData
	{
		/// <summary>The currency pair that the data is for</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>The trade pair id</summary>
		[JsonProperty("id")]
		public int Id { get; internal set; }

		/// <summary></summary>
		[JsonProperty("last")]
		public decimal PriceLast { get; internal set; }

		/// <summary></summary>
		[JsonProperty("percentChange")]
		public decimal PriceChangePercentage { get; internal set; }

		/// <summary></summary>
		[JsonProperty("baseVolume")]
		public decimal Volume24HourBase { get; internal set; }

		/// <summary></summary>
		[JsonProperty("quoteVolume")]
		public decimal Volume24HourQuote { get; internal set; }

		/// <summary></summary>
		[JsonProperty("highestBid")]
		public decimal OrderTopBuy { get; internal set; }

		/// <summary></summary>
		[JsonProperty("lowestAsk")]
		public decimal OrderTopSell { get; internal set; }

		/// <summary></summary>
		public bool IsFrozen { get; private set; }
		[JsonProperty("isFrozen")] internal byte IsFrozenInternal
		{
			set { IsFrozen = value != 0; }
		}

		/// <summary></summary>
		public decimal OrderSpread
		{
			get { return OrderTopSell - OrderTopBuy; }
		}

		/// <summary></summary>
		public decimal OrderSpreadPercentage
		{
			get { return OrderTopSell / OrderTopBuy - 1; }
		}
	}
}

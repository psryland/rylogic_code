using System;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class OrderFill
	{
		/// <summary>Currency pair</summary>
		public CurrencyPair Pair { get; set; }
		[JsonProperty("symbol")] private string PairInternal { set => Pair = CurrencyPair.Parse(value); }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("id")]
		public long TradeId { get; set; }

		/// <summary></summary>
		public EOrderSide Side => IsBuyer ? EOrderSide.BUY : EOrderSide.SELL;

		/// <summary></summary>
		[JsonProperty("price")]
		public decimal Price { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("qty")]
		public decimal AmountBase { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("quoteQty")]
		public decimal AmountQuote { get; set; }

		/// <summary></summary>
		[JsonProperty("commission")]
		public decimal Commission { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("commissionAsset")]
		public string CommissionAsset { get; set; }

		/// <summary></summary>
		public DateTimeOffset Created { get; set; }
		[JsonProperty("time")] private long CreatedInternal { set => Created = DateTimeOffset.FromUnixTimeMilliseconds(value); }

		/// <summary></summary>
		[JsonProperty("isBuyer")]
		public bool IsBuyer { get; set; }

		/// <summary></summary>
		[JsonProperty("isMaker")]
		public bool IsMaker { get; set; }

		/// <summary></summary>
		[JsonProperty("isBestMatch")]
		public bool IsBestMatch { get; set; }
	}
}

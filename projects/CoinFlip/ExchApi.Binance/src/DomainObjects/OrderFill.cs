using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class OrderFill
	{
		/// <summary>Currency pair</summary>
		[JsonProperty("symbol"), JsonConverter(typeof(ToCurrencyPair))]
		public CurrencyPair Pair { get; set; }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("id")]
		public long TradeId { get; set; }

		/// <summary></summary>
		public EOrderSide Side
		{
			get => IsBuyer ? EOrderSide.BUY : EOrderSide.SELL;
			set => IsBuyer = value == EOrderSide.BUY;
		}

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
		[JsonProperty("time"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset Created { get; set; }

		/// <summary></summary>
		[JsonProperty("isBuyer")]
		public bool IsBuyer { get; set; }

		/// <summary></summary>
		[JsonProperty("isMaker")]
		public bool IsMaker { get; set; }
	}
}

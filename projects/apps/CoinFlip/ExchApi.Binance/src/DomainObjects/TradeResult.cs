using System;
using System.Collections.Generic;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Binance.API.DomainObjects
{
	public class TradeResult
	{
		public TradeResult()
		{
			Fills = new List<Fill>();
		}

		/// <summary>Currency pair</summary>
		[JsonProperty("symbol"), JsonConverter(typeof(ToCurrencyPair))]
		public CurrencyPair Pair { get; set; }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary>Globally unique identifier for the order</summary>
		[JsonProperty("clientOrderId")]
		public string? ClientOrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("transactTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset Created { get; set; }

		/// <summary></summary>
		[JsonProperty("price")]
		public decimal Price { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("origQty")]
		public decimal Amount { get; set; }

		/// <summary>The amount of the order that has been filled so far</summary>
		[JsonProperty("executedQty")]
		public decimal AmountCompleted { get; set; }

		/// <summary></summary>
		[JsonProperty("cummulativeQuoteQty")]
		public decimal CummulativeAmountQuote { get; set; }

		/// <summary>The outstanding amount of the order yet to be filled</summary>
		public decimal Remaining => Amount - AmountCompleted;

		/// <summary></summary>
		[JsonProperty("status"), JsonConverter(typeof(ToEnum<EOrderStatus>))]
		public EOrderStatus Status { get; set; }

		/// <summary></summary>
		[JsonProperty("timeInForce"),JsonConverter(typeof(ToEnum<ETimeInForce>))]
		public ETimeInForce TimeInForce { get; set; }

		/// <summary></summary>
		[JsonProperty("type"), JsonConverter(typeof(ToEnum<EOrderType>))]
		public EOrderType Type { get; set; }

		/// <summary></summary>
		[JsonProperty("side"), JsonConverter(typeof(ToEnum<EOrderSide>))]
		public EOrderSide Side { get; set; }

		/// <summary></summary>
		[JsonProperty("fills")]
		public List<Fill> Fills { get; }

		/// <summary></summary>
		public class Fill
		{
			/// <summary></summary>
			[JsonProperty("price")]
			public decimal Price { get; set; }

			/// <summary></summary>
			[JsonProperty("qty")]
			public decimal Amount { get; set; }

			/// <summary></summary>
			[JsonProperty("commission")]
			public decimal Commission { get; set; }

			/// <summary>The initial amount of the order</summary>
			[JsonProperty("commissionAsset")]
			public decimal CommissionAsset { get; set; }
		}
	}
}

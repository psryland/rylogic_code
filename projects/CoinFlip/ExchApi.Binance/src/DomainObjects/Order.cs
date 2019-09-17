using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;
using Rylogic.Extn;

namespace Binance.API.DomainObjects
{
	public class Order
	{
		/// <summary>Currency pair</summary>
		[JsonProperty("symbol"), JsonConverter(typeof(ToCurrencyPair))]
		public CurrencyPair Pair { get; set; }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary>Unique identifier for the order</summary>
		[JsonProperty("clientOrderId")]
		public string ClientOrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("price")]
		public decimal PriceQ2B { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("origQty")]
		public decimal AmountBase { get; set; }

		/// <summary>The amount of the order that has been filled so far</summary>
		[JsonProperty("executedQty")]
		public decimal AmountCompleted { get; set; }

		/// <summary></summary>
		[JsonProperty("cummulativeQuoteQty")]
		public decimal CummulativeAmountQuote { get; set; }

		/// <summary>The outstanding amount of the order yet to be filled</summary>
		public decimal Remaining => AmountBase - AmountCompleted;

		/// <summary></summary>
		[JsonProperty("status"), JsonConverter(typeof(ToEnum<EOrderStatus>))]
		public EOrderStatus Status { get; set; }

		/// <summary></summary>
		[JsonProperty("timeInForce"), JsonConverter(typeof(ToEnum<ETimeInForce>))]
		public ETimeInForce TimeInForce { get; set; }

		/// <summary></summary>
		[JsonProperty("type"), JsonConverter(typeof(ToEnum<EOrderType>))]
		public EOrderType OrderType { get; set; }

		/// <summary></summary>
		[JsonProperty("side"), JsonConverter(typeof(ToEnum<EOrderSide>))]
		public EOrderSide OrderSide { get; set; }

		/// <summary></summary>
		[JsonProperty("stopPrice")]
		public decimal StopPrice { get; set; }

		/// <summary>
		/// In "iceberg" is a conditional order to buy or sell a large amount of assets in
		/// smaller predetermined quantities in order to conceal the total order quantity.</summary>
		[JsonProperty("icebergQty")]
		public decimal IcebergAmount { get; set; }

		/// <summary></summary>
		[JsonProperty("time"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset Created { get; set; }

		/// <summary></summary>
		[JsonProperty("updateTime"), JsonConverter(typeof(UnixMSToDateTimeOffset))]
		public DateTimeOffset Updated { get; set; }

		/// <summary></summary>
		[JsonProperty("isWorking")]
		public bool IsWorking { get; set; }
	}
}

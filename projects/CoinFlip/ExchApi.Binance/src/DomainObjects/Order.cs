using System;
using Newtonsoft.Json;
using Rylogic.Extn;

namespace Binance.API.DomainObjects
{
	public class Order
	{
		/// <summary>Currency pair</summary>
		public CurrencyPair Pair { get; set; }
		[JsonProperty("symbol")] private string PairInternal { set => Pair = CurrencyPair.Parse(value); }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary>Unique identifier for the order</summary>
		[JsonProperty("clientOrderId")]
		public string ClientOrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("price")]
		public double PriceQ2B { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("origQty")]
		public double AmountBase { get; set; }

		/// <summary>The amount of the order that has been filled so far</summary>
		[JsonProperty("executedQty")]
		public double AmountCompleted { get; set; }

		/// <summary></summary>
		[JsonProperty("cummulativeQuoteQty")]
		public double CummulativeAmountQuote { get; set; }

		/// <summary>The outstanding amount of the order yet to be filled</summary>
		public double Remaining => AmountBase - AmountCompleted;

		/// <summary></summary>
		public EOrderStatus Status { get; set; }
		[JsonProperty("status")] private string StatusInternal { set => Status = Enum<EOrderStatus>.Parse(value); }

		/// <summary></summary>
		public ETimeInForce TimeInForce { get; set; }
		[JsonProperty("timeInForce")] private string TimeInForceInternal { set => TimeInForce = Enum<ETimeInForce>.Parse(value); }

		/// <summary></summary>
		public EOrderType OrderType { get; set; }
		[JsonProperty("type")] private string OrderTypeInternal { set => OrderType = Enum<EOrderType>.Parse(value); }

		/// <summary></summary>
		public EOrderSide OrderSide { get; set; }
		[JsonProperty("side")] private string OrderSideInternal { set => OrderSide = Enum<EOrderSide>.Parse(value); }

		/// <summary></summary>
		[JsonProperty("stopPrice")]
		public double StopPrice { get; set; }

		/// <summary>
		/// In "iceberg" is a conditional order to buy or sell a large amount of assets in
		/// smaller predetermined quantities in order to conceal the total order quantity.</summary>
		[JsonProperty("icebergQty")]
		public double IcebergAmount { get; set; }

		/// <summary></summary>
		public DateTimeOffset Created { get; set; }
		[JsonProperty("time")] private long CreatedInternal { set => Created = DateTimeOffset.FromUnixTimeMilliseconds(value); }

		/// <summary></summary>
		public DateTimeOffset Updated { get; set; }
		[JsonProperty("updateTime")] private long UpdatedTimeInternal { set => Updated = DateTimeOffset.FromUnixTimeMilliseconds(value); }

		/// <summary></summary>
		[JsonProperty("isWorking")]
		public bool IsWorking { get; set; }
	}
}

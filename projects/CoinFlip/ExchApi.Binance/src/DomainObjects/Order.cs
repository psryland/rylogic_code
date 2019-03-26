using System;
using Newtonsoft.Json;
using Rylogic.Extn;

namespace Binance.API.DomainObjects
{
	public class Order
	{
		/// <summary></summary>
		[JsonProperty("symbol")]
		internal string PairInternal { get; set; }
		public CurrencyPair Pair { get; internal set; }

		/// <summary></summary>
		[JsonProperty("orderId")]
		public long OrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("clientOrderId")]
		public string ClientOrderId { get; set; }

		/// <summary></summary>
		[JsonProperty("price")]
		public decimal Price { get; set; }

		/// <summary>The initial amount of the order</summary>
		[JsonProperty("origQty")]
		public decimal Amount { get; set; }

		/// <summary>The amount of the order that has been filled so far</summary>
		[JsonProperty("executedQty")]
		public decimal AmountCompleted { get; set; }

		/// <summary>The outstanding amount of the order yet to be filled</summary>
		public decimal Remaining => Amount - AmountCompleted;

		/// <summary></summary>
		[JsonProperty("cummulativeQuoteQty")]
		public decimal CummulativeAmountQuote { get; set; }

		/// <summary></summary>
		public EOrderStatus Status { get; set; }
		[JsonProperty("status")] private string StatusInternal
		{
			set { Status = Enum<EOrderStatus>.Parse(value); }
		}

		/// <summary></summary>
		public ETimeInForce TimeInForce { get; set; }
		[JsonProperty("timeInForce")] private string TimeInForceInternal
		{
			set { TimeInForce = Enum<ETimeInForce>.Parse(value); }
		}

		/// <summary></summary>
		public EOrderTypes Type { get; set; }
		[JsonProperty("type")] private string TypeInternal
		{
			set { Type = Enum<EOrderTypes>.Parse(value); }
		}

		/// <summary></summary>
		public EOrderSide Side { get; set; }
		[JsonProperty("side")] private string SideInternal
		{
			set { Side = Enum<EOrderSide>.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("stopPrice")]
		public decimal StopPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("icebergQty")]
		public decimal IcebergAmount { get; set; }

		/// <summary></summary>
		public DateTimeOffset Created { get; set; }
		[JsonProperty("time")] private long CreatedInternal
		{
			set { Created = DateTimeOffset.FromUnixTimeMilliseconds(value); }
		}

		/// <summary></summary>
		public DateTimeOffset Updated { get; set; }
		[JsonProperty("updateTime")] private long UpdatedTimeInternal
		{
			set { Updated = DateTimeOffset.FromUnixTimeMilliseconds(value); }
		}

		/// <summary></summary>
		[JsonProperty("isWorking")]
		public bool IsWorking { get; set; }
	}
}

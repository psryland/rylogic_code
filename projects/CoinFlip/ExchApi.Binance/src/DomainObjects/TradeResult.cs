using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rylogic.Extn;

namespace Binance.API.DomainObjects
{
	public class TradeResult
	{
		public TradeResult()
		{
			Fills = new List<Fill>();
		}

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
		public DateTimeOffset Created { get; set; }
		[JsonProperty("transactTime")] private long CreatedInternal { set => Created = DateTimeOffset.FromUnixTimeMilliseconds(value); }

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
		public EOrderStatus Status { get; set; }
		[JsonProperty("status")] private string StatusInternal { set => Status = Enum<EOrderStatus>.Parse(value); }

		/// <summary></summary>
		public ETimeInForce TimeInForce { get; set; }
		[JsonProperty("timeInForce")] private string TimeInForceInternal { set => TimeInForce = Enum<ETimeInForce>.Parse(value); }

		/// <summary></summary>
		public EOrderType Type { get; set; }
		[JsonProperty("type")] private string TypeInternal { set => Type = Enum<EOrderType>.Parse(value); }

		/// <summary></summary>
		public EOrderSide Side { get; set; }
		[JsonProperty("side")] private string SideInternal { set => Side = Enum<EOrderSide>.Parse(value); }

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

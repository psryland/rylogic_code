using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class Historic
	{
		/// <summary>Globally unique identifier for the order</summary>
		public Guid OrderId { get; private set; }
		[JsonProperty("OrderUuid")] internal string OrderIdInternal
		{
			set { OrderId = Guid.Parse(value); }
		}

		/// <summary>The pair that the order is on</summary>
		public CurrencyPair Pair { get; private set; }
		[JsonProperty("Exchange")] internal string PairInternal
		{
			set { Pair = CurrencyPair.Parse(value); }
		}

		/// <summary>The order type</summary>
		public EOrderType Type { get; private set; }
		[JsonProperty("OrderType")] internal string TypeInternal
		{
			set { Type = Misc.ToOrderType(value); }
		}

		/// <summary>The trade price (in quote currency)</summary>
		[JsonProperty("Price")]
		public decimal Price { get; internal set; }

		/// <summary>The trade price (in quote currency)</summary>
		[JsonProperty("PricePerUnit")]
		public decimal PricePerUnit { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Quantity")]
		public decimal QuantityBase { get; internal set; }

		/// <summary></summary>
		[JsonProperty("QuantityRemaining")]
		public decimal RemainingBase { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Limit")]
		public decimal Limit { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Commission")]
		public decimal Commission { get; internal set; }

		/// <summary>The order creation time stamp</summary>
		public DateTimeOffset Created { get; private set; }
		[JsonProperty("TimeStamp")] private string CreatedInternal
		{
			set { Created = DateTimeOffset.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("ImmediateOrCancel")]
		public bool ImmediateOrCancel { get; internal set; }

		/// <summary></summary>
		[JsonProperty("IsConditional")]
		public bool IsConditional { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Condition")]
		public string Condition { get; internal set; }

		/// <summary></summary>
		[JsonProperty("ConditionTarget")]
		public string ConditionTarget { get; internal set; }
	}

	public class PositionHistoryResponse
	{
		/// <summary></summary>
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("message")]
		public string Message { get; internal set; }

		/// <summary></summary>
		[JsonProperty("result")]
		public List<Historic> Data { get; internal set; }
	}
}

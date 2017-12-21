using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class Order
	{
		/// <summary></summary>
		public Guid Id { get; private set; }
		[JsonProperty("Uuid")] internal string UuidInternal
		{
			set { Id = Guid.Parse(value); }
		}

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

		/// <summary>The limit price at which the trade will be filled (in quote currency)</summary>
		[JsonProperty("Limit")]
		public decimal Limit { get; internal set; }

		/// <summary>The price that the trade has been filled at (in quote currency)</summary>
		[JsonProperty("Price")]
		public decimal ActualPrice { get; internal set; }

		/// <summary></summary>
		[JsonProperty("PricePerUnit")]
		public decimal PricePerUnit { get; internal set; }

		/// <summary>The volume to trade (in base currency)</summary>
		[JsonProperty("Quantity")]
		public decimal VolumeBase { get; internal set; }

		/// <summary>The volume that remains to be traded (in base currency)</summary>
		[JsonProperty("QuantityRemaining")]
		public decimal RemainingBase { get; internal set; }

		/// <summary></summary>
		[JsonProperty("CommissionPaid")]
		public decimal CommissionPaid { get; internal set; }

		/// <summary>The order creation time stamp</summary>
		public DateTimeOffset Created { get; private set; }
		[JsonProperty("Opened")] private string CreatedInternal
		{
			set { Created = DateTimeOffset.Parse(value); }
		}

		/// <summary>The order filled time stamp</summary>
		public DateTimeOffset Completed { get; private set; }
		[JsonProperty("Closed")] private string CompletedInternal
		{
			set { Completed = DateTimeOffset.Parse(value); }
		}

		/// <summary></summary>
		[JsonProperty("CancelInitiated")]
		public bool CancelInitiated { get; internal set; }

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

	public class OrdersResponse
	{
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		[JsonProperty("message")]
		public string Message { get; internal set; }

		[JsonProperty("result")]
		public List<Order> Data { get; internal set; }
	}
}

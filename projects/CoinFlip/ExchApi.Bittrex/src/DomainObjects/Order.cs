using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	public class Order
	{
		/// <summary></summary>
		[JsonProperty("Uuid")]
		public Guid? Id { get; set; }

		/// <summary>Globally unique identifier for the order</summary>
		[JsonProperty("OrderUuid")]
		public Guid OrderId { get; set; }

		/// <summary>The pair that the order is on</summary>
		[JsonProperty("Exchange"), JsonConverter(typeof(ParseMethod<CurrencyPair>))]
		public CurrencyPair Pair { get; set; }

		/// <summary>The order type</summary>
		[JsonProperty("OrderType"), JsonConverter(typeof(ToEnum<EOrderSide>))]
		public EOrderSide Type { get; set; }

		/// <summary>The limit price at which the trade will be filled (in quote currency)</summary>
		[JsonProperty("Limit")]
		public double Limit { get; internal set; }

		/// <summary>The price that the trade has been filled at (in quote currency)</summary>
		[JsonProperty("Price")]
		public double ActualPrice { get; set; }

		/// <summary></summary>
		[JsonProperty("PricePerUnit")]
		public double? PricePerUnit { get; set; }

		/// <summary>The volume to trade (in base currency)</summary>
		[JsonProperty("Quantity")]
		public double VolumeBase { get; set; }

		/// <summary>The volume that remains to be traded (in base currency)</summary>
		[JsonProperty("QuantityRemaining")]
		public double RemainingBase { get; set; }

		/// <summary></summary>
		[JsonProperty("CommissionPaid")]
		public double CommissionPaid { get; set; }

		/// <summary>The order creation time stamp</summary>
		[JsonProperty("Opened")]
		public DateTimeOffset Created { get; set; }

		/// <summary>The order filled time stamp</summary>
		[JsonProperty("Closed")]
		public DateTimeOffset? Completed { get; set; }

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
		[JsonProperty("ConditionTarget"), JsonConverter(typeof(NullIsDefault<string>))]
		public string ConditionTarget { get; internal set; }
	}
}

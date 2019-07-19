using System;
using ExchApi.Common.JsonConverter;
using Newtonsoft.Json;

namespace Bittrex.API.DomainObjects
{
	public class Trade
	{
		/// <summary>Globally unique identifier for the order</summary>
		[JsonProperty("OrderUuid")]
		public Guid OrderId { get; set; }

		/// <summary>The pair that the order is on</summary>
		[JsonProperty("Exchange"), JsonConverter(typeof(ParseMethod<CurrencyPair>))]
		public CurrencyPair Pair { get; set; }

		/// <summary>The order type</summary>
		[JsonProperty("OrderType"), JsonConverter(typeof(ToEnum<EOrderSide>))]
		public EOrderSide Type { get; set; }

		/// <summary>The absolute value of the change in the account balance, after fees (in quote currency)</summary>
		[JsonProperty("Price")]
		public double BalanceChange { get; set; }

		/// <summary>The price that the trade was filled at. Typically less than 'Limit' (in quote currency)</summary>
		[JsonProperty("PricePerUnit")]
		public double PricePerUnit { get; set; }

		/// <summary></summary>
		[JsonProperty("Quantity")]
		public double QuantityBase { get; set; }

		/// <summary></summary>
		[JsonProperty("QuantityRemaining")]
		public double RemainingBase { get; set; }

		/// <summary>The amount filled in this trade (in base currency)</summary>
		public double FilledBase => QuantityBase - RemainingBase;

		/// <summary>The amount filled in this trade (in quote currency)</summary>
		public double FilledQuote => FilledBase * PricePerUnit;

		/// <summary></summary>
		[JsonProperty("Limit")]
		public double Limit { get; set; }

		/// <summary></summary>
		[JsonProperty("Commission")]
		public double Commission { get; set; }

		/// <summary>The order creation time stamp</summary>
		[JsonProperty("TimeStamp")]
		public DateTimeOffset Created { get; set; }

		/// <summary></summary>
		[JsonProperty("ImmediateOrCancel")]
		public bool ImmediateOrCancel { get; set; }

		/// <summary></summary>
		[JsonProperty("IsConditional")]
		public bool IsConditional { get; set; }

		/// <summary></summary>
		[JsonProperty("Condition")]
		public string Condition { get; set; }

		/// <summary></summary>
		[JsonProperty("ConditionTarget")]
		public string ConditionTarget { get; set; }
	}
}

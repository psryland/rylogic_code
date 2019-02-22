using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Bittrex.API
{
	public class Trade
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

		/// <summary>The absolute value of the change in the account balance, after fees (in quote currency)</summary>
		[JsonProperty("Price")]
		public decimal BalanceChange { get; internal set; }

		/// <summary>The price that the trade was filled at. Typically less than 'Limit' (in quote currency)</summary>
		[JsonProperty("PricePerUnit")]
		public decimal PricePerUnit { get; internal set; }

		/// <summary></summary>
		[JsonProperty("Quantity")]
		public decimal QuantityBase { get; internal set; }

		/// <summary></summary>
		[JsonProperty("QuantityRemaining")]
		public decimal RemainingBase { get; internal set; }

		/// <summary>The amount filled in this trade (in base currency)</summary>
		public decimal FilledBase
		{
			get { return QuantityBase - RemainingBase; }
		}

		/// <summary>The amount filled in this trade (in quote currency)</summary>
		public decimal FilledQuote
		{
			get { return FilledBase * PricePerUnit; }
		}

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

	public class TradeHistoryResponse
	{
		/// <summary></summary>
		[JsonProperty("success")]
		public bool Success { get; internal set; }

		/// <summary></summary>
		[JsonProperty("message")]
		public string Message { get; internal set; }

		/// <summary></summary>
		[JsonProperty("result")]
		public List<Trade> Data { get; internal set; }
	}
}

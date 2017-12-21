using System;
using System.Diagnostics;

namespace Bitfinex.API
{
	/// <summary>A completed order</summary>
	[DebuggerDisplay("{OrderId}")]
	public class Trade
	{
		/// <summary>The Id of the order that was filled</summary>
		public ulong OrderId { get; internal set; }

		/// <summary>The Id of the trade </summary>
		public ulong TradeId { get; internal set; }

		/// <summary>The pair that was traded</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>The timestamp of when the order was executed</summary>
		public DateTimeOffset Created { get; internal set; }

		/// <summary>The volume traded. Positive means buy, negative means sell</summary>
		public decimal Amount { get; internal set; }

		/// <summary>The price that the trade was made at</summary>
		public decimal Price { get; internal set; }

		/// <summary>The direction of the trade</summary>
		public EOrderType TradeType
		{
			get { return Amount > 0 ? EOrderType.Buy : Amount < 0 ? EOrderType.Sell : throw new Exception("Unknown trade type"); }
		}

		/// <summary>The type of the exchange order</summary>
		public EExchOrderType ExchOrderType { get; internal set; }

		/// <summary>The price that the original order was at</summary>
		public decimal OrderPrice { get; internal set; }

		/// <summary>True if this was a maker trade</summary>
		public bool Maker { get; internal set; }

		/// <summary>The amount charged in commission</summary>
		public decimal Fee { get; internal set; }

		/// <summary>The currency that the Fee was charged in</summary>
		public string FeeCurrency { get; internal set; }
	}
}

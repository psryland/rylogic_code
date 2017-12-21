using System;
using System.Diagnostics;

namespace Bitfinex.API
{
	/// <summary>An order created by the account holder</summary>
	[DebuggerDisplay("{OrderId}")]
	public class Order
	{
		/// <summary>The unique Id of the order</summary>
		public ulong OrderId { get; internal set; }

		/// <summary>The Group Id for the order</summary>
		public ulong? GroupId { get; internal set; }

		/// <summary>Client order Id</summary>
		public ulong? ClientOrderId { get; internal set; }

		/// <summary>Current</summary>
		public CurrencyPair Pair { get; internal set; }

		/// <summary>Timestamp of when the order was created (UTC)</summary>
		public DateTimeOffset Created { get; internal set; }

		/// <summary>Timestamp of when the order was last updated (UTC)</summary>
		public DateTimeOffset Updated { get; internal set; }

		/// <summary>The initial amount of the order. Positive means buy, Negative means sell</summary>
		public decimal AmountInitial { get; internal set; }

		/// <summary>The amount remaining of the order. Positive means buy, Negative means sell</summary>
		public decimal Amount { get; internal set; }

		/// <summary>Order price</summary>
		public decimal Price { get; internal set; }

		/// <summary></summary>
		public decimal PriceAverage { get; internal set; }

		/// <summary></summary>
		public decimal? PriceTrailing { get; internal set; }

		/// <summary>Limit price for stop orders</summary>
		public decimal? PriceLimit { get; internal set; }

		/// <summary>The direction of the trade</summary>
		public EOrderType TradeType
		{
			get { return Amount > 0 ? EOrderType.Buy : Amount < 0 ? EOrderType.Sell : throw new Exception("Unknown trade type"); }
		}

		/// <summary>The order type</summary>
		public EExchOrderType ExchOrderType { get; internal set; }

		/// <summary></summary>
		public EExchOrderType? ExchOrderTypePrev { get; internal set; }

		/// <summary></summary>
		public int Flags { get; internal set; }

		/// <summary></summary>
		public EOrderStatus Status { get; internal set; }

		/// <summary></summary>
		public bool Notify { get; internal set; }

		/// <summary></summary>
		public bool Hidden{ get; internal set; }

		/// <summary>If another order causes this order to be placed (i.e. OCO), this is that order's Id</summary>
		public int PlacedId { get; internal set; }
	}
}

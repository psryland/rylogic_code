using System;
using System.Collections.Generic;
using System.Diagnostics;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Depth of market (both buys and sells)</summary>
	public class MarketDepth
	{
		public MarketDepth(Coin @base, Coin quote)
		{
			Q2B = new OrderBook(@base, quote, ETradeType.Q2B);
			B2Q = new OrderBook(@base, quote, ETradeType.B2Q);
		}

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get; }

		/// <summary>Access the order book for the given trade direction</summary>
		public OrderBook this[ETradeType tt] => tt == ETradeType.B2Q ? B2Q : Q2B;

		/// <summary>Raised when the order book for this pair is updated</summary>
		public event EventHandler OrderBookChanged;

		/// <summary>Update the list of buy/sell orders</summary>
		public void UpdateOrderBook(IEnumerable<Offer> b2q, IEnumerable<Offer> q2b)
		{
			B2Q.Orders.Clear();
			B2Q.Orders.AddRange(b2q);

			Q2B.Orders.Clear();
			Q2B.Orders.AddRange(q2b);

			Debug.Assert(AssertOrdersValid());
			OrderBookChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Check the orders are in the correct order</summary>
		public bool AssertOrdersValid()
		{
			var q2b_price0 = 0m._(Q2B.RateUnits);
			var b2q_price0 = 0m._(B2Q.RateUnits);
			var q2b0 = 0m._(Q2B.Base);
			var b2q0 = 0m._(B2Q.Base);

			// Asking price should increase
			for (int i = 0; i != Q2B.Count; ++i)
			{
				if (Q2B[i].Price < q2b_price0)
					throw new Exception("Q2B order book price is invalid");
				if (Q2B[i].AmountBase < q2b0)
					throw new Exception("Q2B order book volume is invalid");
				if (i > 0 && Q2B[i-1].Price > Q2B[i].Price)
					throw new Exception("Q2B order book prices are out of order");
			}

			// Bid price should decrease
			for (int i = 0; i != B2Q.Count; ++i)
			{
				if (B2Q[i].Price < b2q_price0)
					throw new Exception("B2Q order book price is invalid");
				if (B2Q[i].AmountBase < b2q0)
					throw new Exception("B2Q order book volume is invalid");
				if (i > 0 && B2Q[i-1].Price < B2Q[i].Price)
					throw new Exception("B2Q order book prices are out of order");
			}

			return true;
		}
	}
}

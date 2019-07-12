using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		public MarketDepth(MarketDepth rhs)
		{
			Q2B = new OrderBook(rhs.Q2B);
			B2Q = new OrderBook(rhs.B2Q);
		}

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get; }

		/// <summary>Access the order book for the given trade direction</summary>
		public OrderBook this[ETradeType tt] => tt == ETradeType.B2Q ? B2Q : Q2B;

		/// <summary>True when something is interested in this market data</summary>
		public bool IsNeeded
		{
			get
			{
				var args = new HandledEventArgs();
				Needed?.Invoke(this, args);
				return args.Handled;
			}
		}

		/// <summary>Raised when the order book for this pair is updated</summary>
		public event EventHandler OrderBookChanged;

		/// <summary>
		/// Called by the exchanged to determine if anything is interested in this market data.
		/// Interested parties should attach *Weak* handles, so that we they no longer exist their
		/// references disappear and 'Needed' will return false</summary>
		public event EventHandler<HandledEventArgs> Needed;

		/// <summary>Update the list of buy/sell orders</summary>
		public void UpdateOrderBook(IEnumerable<Offer> b2q, IEnumerable<Offer> q2b)
		{
			B2Q.Offers.Clear();
			B2Q.Offers.AddRange(b2q);

			Q2B.Offers.Clear();
			Q2B.Offers.AddRange(q2b);

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

			// The Q2B prices should increase, i.e. the best offer from a trader's point of view is the lowest price.
			for (int i = 0; i != Q2B.Count; ++i)
			{
				if (Q2B[i].Price < q2b_price0)
					throw new Exception("Q2B order book price is invalid");
				if (Q2B[i].AmountBase < q2b0)
					throw new Exception("Q2B order book volume is invalid");
				if (i > 0 && Q2B[i-1].Price > Q2B[i].Price)
					throw new Exception("Q2B order book prices are out of order");
			}

			// The B2Q prices should decrease, i.e. the best offer from a trader's point of view is the highest price.
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

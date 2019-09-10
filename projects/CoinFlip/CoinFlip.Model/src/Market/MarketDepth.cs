using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Depth of market (both buys and sells)</summary>
	public class MarketDepth
	{
		public MarketDepth(Coin @base, Coin quote)
		{
			RateUnits = @base.Symbol != quote.Symbol ? $"{quote}/{@base}" : string.Empty;
			Q2B = new OrderBook(@base, quote, ETradeType.Q2B);
			B2Q = new OrderBook(@base, quote, ETradeType.B2Q);
		}
		public MarketDepth(MarketDepth rhs)
		{
			RateUnits = rhs.RateUnits;
			Q2B = new OrderBook(rhs.Q2B);
			B2Q = new OrderBook(rhs.B2Q);
		}

		/// <summary>The units of the prices in this market</summary>
		public string RateUnits { get; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get; }

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get; }

		/// <summary>Access the order book for the given trade direction</summary>
		public OrderBook this[ETradeType tt] =>
			tt == ETradeType.B2Q ? B2Q :
			tt == ETradeType.Q2B ? Q2B :
			throw new Exception("Unknown trade type");

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

		/// <summary>
		/// Consume offers up to 'price_q2b' or 'amount_base' (based on order type).
		/// 'pair' is the trade pair that this market depth data is associated with.
		/// Returns the offers that were consumed. 'amount_remaining' is what remains unfilled</summary>
		public IList<Offer> Consume(TradePair pair, ETradeType tt, EOrderType ot, Unit<decimal> price_q2b, Unit<decimal> amount_base, out Unit<decimal> remaining_base)
		{
			// Notes:
			//  - 'remaining_base' should only be non zero if the order book is empty
			//  - Handling 'dust' amounts:
			//     If the amount to fill almost matches an offer, where the difference is an amount too small to trade,
			//     the offer amount is adjusted to exactly match. The small difference is absorbed by the exchange.
			//  - This function cannot use 'amount_in' + 'amount_out' parameters because the price to consume up to
			//    is not necessarity 'amount_out/amount_in'. 'amount_in' may be the partial remaining amount of a trade.

			var order_book = this[tt];
			remaining_base = amount_base;

			// Stop orders become market orders when the price reaches the stop level
			if (ot == EOrderType.Stop)
			{
				if (order_book.Count != 0 && tt.Sign() * price_q2b.CompareTo(order_book[0].PriceQ2B) <= 0)
					ot = EOrderType.Market;
				else
					return new List<Offer>();
			}

			var count = 0;
			var offers = order_book.Offers;
			foreach (var offer in offers)
			{
				// Price is too high/low to fill 'offer', stop.
				if (ot != EOrderType.Market && tt.Sign() * price_q2b.CompareTo(offer.PriceQ2B) < 0)
					break;

				var rem = remaining_base - offer.AmountBase;

				// The remaining amount is large enough to consider the next offer
				if (rem < pair.AmountRangeBase.Beg || rem * offer.PriceQ2B < pair.AmountRangeQuote.Beg)
					break;

				remaining_base = rem;
				++count;
			}

			// Remove the orders that have been filled
			var consumed = offers.GetRange(0, count);
			offers.RemoveRange(0, count);

			// Remove any remaining amount from the top remaining offer (if the price is right)
			if (remaining_base != 0 && offers.Count != 0 && (ot == EOrderType.Market || tt.Sign() * price_q2b.CompareTo(offers[0].PriceQ2B) >= 0))
			{
				var offer = offers[0];

				var rem = offer.AmountBase - remaining_base;
				if (pair.AmountRangeBase.Contains(rem) && pair.AmountRangeQuote.Contains(rem * offer.PriceQ2B))
					offers[0] = new Offer(offers[0].PriceQ2B, rem);
				else
					offers.RemoveAt(0);

				consumed.Add(new Offer(offer.PriceQ2B, remaining_base));
				remaining_base -= remaining_base;
			}
			return consumed;
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex(ETradeType tt, Unit<decimal> price_q2b, out bool beyond_order_book)
		{
			// Check units
			if (price_q2b < 0m._(RateUnits))
				throw new Exception("Invalid price");

			// If a trade cannot be filled by existing orders, it becomes an offer.
			// E.g.
			//  - Want to trade B2Q == Sell our 'B' to get 'Q'.
			//  - If there are no suitable B2Q.Orders (i.e. people wanting to buy 'B') then our trade becomes an offer in the Q2B order book.
			//    i.e. we want to buy 'Q' so our trade is a Q2B offer.
			var idx = -1;
			switch (tt)
			{
			default: throw new Exception($"Unknown trade type:{tt}");
			case ETradeType.B2Q:
				{
					idx = Q2B.Offers.BinarySearch(x => +x.PriceQ2B.CompareTo(price_q2b), find_insert_position: true);
					beyond_order_book = idx == Q2B.Offers.Count;
					break;
				}
			case ETradeType.Q2B:
				{
					idx = B2Q.Offers.BinarySearch(x => -x.PriceQ2B.CompareTo(price_q2b), find_insert_position: true);
					beyond_order_book = idx == B2Q.Offers.Count;
					break;
				}
			}
			return idx;
		}

		/// <summary>The total value of orders with a better price than 'price'</summary>
		public Unit<decimal> OrderBookDepth(ETradeType tt, Unit<decimal> price_q2b, out bool beyond_order_book)
		{
			var index = OrderBookIndex(tt, price_q2b, out beyond_order_book);
			var orders = tt == ETradeType.B2Q ? Q2B.Offers : B2Q.Offers;
			return orders.Take(index).Sum(x => x.AmountBase);
		}

		/// <summary>Update the list of buy/sell orders</summary>
		public void UpdateOrderBooks(IList<Offer> b2q, IList<Offer> q2b)
		{
			B2Q.Offers.Assign(b2q);
			Q2B.Offers.Assign(q2b);

			Debug.Assert(AssertOrdersValid());
			OrderBookChanged?.Invoke(this, EventArgs.Empty);
		}
		public void UpdateOrderBooks(MarketDepth rhs)
		{
			UpdateOrderBooks(rhs.B2Q.Offers, rhs.Q2B.Offers);
		}

		/// <summary>Check the orders are in the correct order</summary>
		public bool AssertOrdersValid()
		{
			var q2b_price0 = 0m;
			var b2q_price0 = 0m;
			var q2b0 = 0m;
			var b2q0 = 0m;

			// The Q2B prices should increase, i.e. the best offer from a trader's point of view is the lowest price.
			for (int i = 0; i != Q2B.Count; ++i)
			{
				if (Q2B[i].PriceQ2B < q2b_price0)
					throw new Exception("Q2B order book price is invalid");
				if (Q2B[i].AmountBase < q2b0)
					throw new Exception("Q2B order book volume is invalid");
				if (i > 0 && Q2B[i-1].PriceQ2B > Q2B[i].PriceQ2B)
					throw new Exception("Q2B order book prices are out of order");
			}

			// The B2Q prices should decrease, i.e. the best offer from a trader's point of view is the highest price.
			for (int i = 0; i != B2Q.Count; ++i)
			{
				if (B2Q[i].PriceQ2B < b2q_price0)
					throw new Exception("B2Q order book price is invalid");
				if (B2Q[i].AmountBase < b2q0)
					throw new Exception("B2Q order book volume is invalid");
				if (i > 0 && B2Q[i-1].PriceQ2B < B2Q[i].PriceQ2B)
					throw new Exception("B2Q order book prices are out of order");
			}

			return true;
		}
	}
}

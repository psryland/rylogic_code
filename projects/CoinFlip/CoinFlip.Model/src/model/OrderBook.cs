using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	/// <summary>Depth of market (both buys and sells)</summary>
	public class MarketDepth
	{
		public MarketDepth(Coin base_, Coin quote)
		{
			Q2B = new OrderBook(base_, quote, ETradeType.Q2B);
			B2Q = new OrderBook(base_, quote, ETradeType.B2Q);
		}

		/// <summary>Access the order book for the given trade direction</summary>
		public OrderBook this[ETradeType tt]
		{
			get { return tt == ETradeType.B2Q ? B2Q : Q2B; }
		}

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get; private set; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get; private set; }

		/// <summary>Update the list of buy/sell orders</summary>
		public void UpdateOrderBook(IEnumerable<OrderBook.Offer> b2q, IEnumerable<OrderBook.Offer> q2b)
		{
			using (B2Q.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				B2Q.Orders.Clear();
				B2Q.Orders.AddRange(b2q);
			}
			using (Q2B.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				Q2B.Orders.Clear();
				Q2B.Orders.AddRange(q2b);
			}
			Debug.Assert(AssertOrdersValid());
			OrderBookChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Raised when the order book for this pair is updated</summary>
		public event EventHandler OrderBookChanged;

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
				if (Q2B[i].VolumeBase < q2b0)
					throw new Exception("Q2B order book volume is invalid");
				if (i > 0 && Q2B[i-1].Price > Q2B[i].Price)
					throw new Exception("Q2B order book prices are out of order");
			}

			// Bid price should decrease
			for (int i = 0; i != B2Q.Count; ++i)
			{
				if (B2Q[i].Price < b2q_price0)
					throw new Exception("B2Q order book price is invalid");
				if (B2Q[i].VolumeBase < b2q0)
					throw new Exception("B2Q order book volume is invalid");
				if (i > 0 && B2Q[i-1].Price < B2Q[i].Price)
					throw new Exception("B2Q order book prices are out of order");
			}

			return true;
		}
	}

	/// <summary>Depth of market (one side, buy or sell). The available trades ordered by price. (Increasing for Ask, Decreasing for Bid).</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class OrderBook :IEnumerable<OrderBook.Offer>
	{
		public OrderBook(Coin base_, Coin quote, ETradeType tt)
		{
			Orders = new BindingListEx<Offer>();
			Base = base_;
			Quote = quote;
			TradeType = tt;
		}
		public OrderBook(OrderBook rhs)
		{
			Base = rhs.Base;
			Quote = rhs.Quote;
			Orders = new BindingListEx<Offer>((IEnumerable<Offer>)rhs.Orders);
			TradeType = rhs.TradeType;
		}

		/// <summary>Base currency</summary>
		public Coin Base { get; private set; }

		/// <summary>Quote currency</summary>
		public Coin Quote { get; private set; }

		/// <summary>The buy/sell offers</summary>
		public BindingListEx<Offer> Orders { get; private set; }

		/// <summary>The trade direction of offer in this order book. E.g. B2Q means offers to convert Base to Quote</summary>
		public ETradeType TradeType { get; private set; }

		/// <summary>If positive, then the first order is a minimum (i.e. Q2B). If negative, then the first order is a maximum (i.e. B2Q)</summary>
		public int Sign { get { return TradeType.Sign(); } }

		/// <summary>The number of orders</summary>
		public int Count
		{
			get { return Orders.Count; }
		}

		/// <summary>Remove all orders</summary>
		public void Clear()
		{
			Orders.Clear();
		}

		/// <summary>Add an offer to the depth of market</summary>
		public void Add(Offer offer, bool validate = true)
		{
			Debug.Assert(!validate || offer.VolumeBase != 0m._(Base));
			Debug.Assert(!validate || offer.VolumeBase * offer.Price != 0m._(Quote));
			Orders.Add(offer);
		}

		/// <summary>Array access</summary>
		public Offer this[int index]
		{
			get { return Orders[index]; }
		}

		/// <summary>
		/// Consume orders up to 'price' or 'volume' (simulating them being filled).
		/// 'pair' is the trade pair that this OrderBook is associated with.
		/// Returns the orders that were consumed. 'volume_remaining' is what remains unfilled</summary>
		public IList<Offer> Consume(TradePair pair, Unit<decimal> price, Unit<decimal> volume, out Unit<decimal> volume_remaining)
		{
			// Note: Have to be careful not to leave behind volumes that are less than the allowable limits for 'pair'
			volume_remaining = volume;

			var count = 0;
			foreach (var order in Orders)
			{
				// Price is too high/low to fill 'order', stop.
				if (Sign * price.CompareTo(order.Price) < 0)
					break;

				// The volume remaining is less than the volume of 'order', stop
				if (volume_remaining <= order.VolumeBase)
					break;

				// 'order' is smaller than the remaining volume so it would be consumed. However, don't consume
				// 'order' if doing so would leave 'volume_remaining' with an invalid trading volume.
				var rem = volume_remaining - order.VolumeBase;
				if (!pair.VolumeRangeBase.Contains(rem) || !pair.VolumeRangeQuote.Contains(rem * price))
					break;
				
				volume_remaining = rem;
				++count;
			}

			// Remove the orders that have been filled
			var consumed = Orders.Take(count).ToList();
			Orders.RemoveRange(0, count);

			// Remove any remaining volume from the top remaining order if doing so don't leave an invalid trading volume
			if (volume_remaining != 0 && Orders.Count != 0 && Sign * price.CompareTo(Orders[0].Price) >= 0)
			{
				var rem = Orders[0].VolumeBase - volume_remaining;
				if (pair.VolumeRangeBase.Contains(rem) && pair.VolumeRangeQuote.Contains(rem * Orders[0].Price))
				{
					consumed.Add(new Offer(Orders[0].Price, volume_remaining));
					Orders[0] = new Offer(Orders[0].Price, rem);
					volume_remaining = 0m._(volume);
				}
			}

			return consumed;
		}

		/// <summary>Return the units for the conversion rate from Base to Quote (i.e. Quote/Base)</summary>
		public string RateUnits
		{
			get { return Base.Symbol != Quote.Symbol ? $"{Quote}/{Base}" : string.Empty; }
		}
		public string RateUnitsInv
		{
			get { return Base.Symbol != Quote.Symbol ? $"{Base}/{Quote}" : string.Empty; }
		}

		/// <summary>A string description of the order book</summary>
		public string Description
		{
			get { return $"{Base}/{Quote} Orders={Count}"; }
		}

		/// <summary>Enumerable Orders</summary>
		public IEnumerator<Offer> GetEnumerator()
		{
			return Orders.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>A single trade offer</summary>
		[DebuggerDisplay("Price={Price} Vol={VolumeBase}")]
		public struct Offer :IComparable<Offer>
		{
			public Offer(Unit<decimal> price, Unit<decimal> volume)
			{
				Price = price;
				VolumeBase = volume;
			}

			/// <summary>The price (to buy or sell) (in Quote/Base)</summary>
			public Unit<decimal> Price { get; set; }

			/// <summary>The volume (in base currency)</summary>
			public Unit<decimal> VolumeBase { get; set; }

			/// <summary>The volume (in quote currency)</summary>
			public Unit<decimal> VolumeQuote { get { return VolumeBase * Price; } }

			/// <summary>Orders are compared by price</summary>
			public int CompareTo(Offer rhs)
			{
				return Price.CompareTo(rhs.Price);
			}

			/// <summary>Check this order against the limits given in 'pair'</summary>
			public bool Validate(TradePair pair)
			{
				if (Price <= 0m)
					return false;

				if (!pair.VolumeRangeBase.Contains(VolumeBase))
					return false;

				if (!pair.VolumeRangeQuote.Contains(VolumeQuote))
					return false;

				return true;
			}
		}
	}
}

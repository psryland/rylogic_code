using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class OrderBook :IEnumerable<Offer>
	{
		// Notes:
		//   - This is one side of the MarketDepth (either buy or sell).
		//   - The available trades are ordered by price (Increasing for Q2B, Decreasing for B2Q).
		public OrderBook(Coin @base, Coin quote, ETradeType tt)
		{
			TradeType = tt;
			Offers = new List<Offer>();
			Base = @base;
			Quote = quote;
			RateUnits = Base.Symbol != Quote.Symbol ? $"{Quote}/{Base}" : string.Empty;
			RateUnitsInv = Base.Symbol != Quote.Symbol ? $"{Base}/{Quote}" : string.Empty;
		}
		public OrderBook(OrderBook rhs)
			:this(rhs.Base, rhs.Quote, rhs.TradeType)
		{
			Offers.Assign(rhs.Offers);
		}

		/// <summary>The trade direction of offer in this order book. E.g. B2Q means offers to convert Base to Quote</summary>
		public ETradeType TradeType { get; }

		/// <summary>Base currency</summary>
		public Coin Base { get; }

		/// <summary>Quote currency</summary>
		public Coin Quote { get; }

		/// <summary>The buy/sell offers</summary>
		public List<Offer> Offers { get; }

		/// <summary>If positive, then the first order is a minimum (i.e. Q2B). If negative, then the first order is a maximum (i.e. B2Q)</summary>
		public int Sign => TradeType.Sign();

		/// <summary>The number of orders</summary>
		public int Count => Offers.Count;

		/// <summary>Array access</summary>
		public Offer this[int index]
		{
			get => Offers[index];
			set => Offers[index] = value;
		}

		/// <summary>Remove all orders</summary>
		public void Clear()
		{
			Offers.Clear();
		}

		/// <summary>Add an offer to the depth of market</summary>
		public void Add(Offer offer, bool validate = true)
		{
			Debug.Assert(!validate || offer.AmountBase != 0.0._(Base));
			Debug.Assert(!validate || offer.AmountBase * offer.Price != 0.0._(Quote));
			Offers.Add(offer);
		}

		/// <summary>
		/// Consume orders up to 'price' or 'amount' (simulating them being filled).
		/// 'pair' is the trade pair that this OrderBook is associated with.
		/// Returns the orders that were consumed. 'amount_remaining' is what remains unfilled</summary>
		public IList<Offer> Consume(TradePair pair, EPlaceOrderType ot, Unit<double> price, Unit<double> amount, out Unit<double> amount_remaining)
		{
			amount_remaining = amount;

			var count = 0;
			foreach (var offer in Offers)
			{
				// Price is too high/low to fill 'offer', stop.
				if (ot != EPlaceOrderType.Market && Sign * price.CompareTo(offer.Price) < 0)
					break;

				// The volume remaining is less than the volume of 'offer', stop
				if (amount_remaining <= offer.AmountBase)
					break;

				// 'offer' is smaller than the remaining volume so it would be consumed. However, don't consume
				// 'offer' if doing so would leave 'amount_remaining' with an invalid trading amount.
				var rem = amount_remaining - offer.AmountBase;
				if (!pair.AmountRangeBase.Contains(rem) || !pair.AmountRangeQuote.Contains(rem * price))
					break;
				
				amount_remaining = rem;
				++count;
			}

			// Remove the orders that have been filled
			var consumed = Offers.Take(count).ToList();
			Offers.RemoveRange(0, count);

			// Remove any remaining amount from the top remaining order if doing so doesn't leave an invalid trading amount
			if (amount_remaining != 0 && Offers.Count != 0 && (ot == EPlaceOrderType.Market || Sign * price.CompareTo(Offers[0].Price) >= 0))
			{
				var rem = Offers[0].AmountBase - amount_remaining;
				if (pair.AmountRangeBase.Contains(rem) && pair.AmountRangeQuote.Contains(rem * Offers[0].Price))
				{
					consumed.Add(new Offer(Offers[0].Price, amount_remaining));
					Offers[0] = new Offer(Offers[0].Price, rem);
					amount_remaining = 0.0._(amount);
				}
			}

			return consumed;
		}

		/// <summary>Return the units for the conversion rate from Base to Quote (i.e. Quote/Base)</summary>
		public string RateUnits { get; }
		public string RateUnitsInv { get; }

		/// <summary>A string description of the order book</summary>
		public string Description => $"{Base}/{Quote} Orders={Count}";

		/// <summary>Enumerable Orders</summary>
		public IEnumerator<Offer> GetEnumerator()
		{
			return Offers.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}

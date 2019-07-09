using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
			Offers = new ObservableCollection<Offer>();
			Base = @base;
			Quote = quote;
		}
		public OrderBook(OrderBook rhs)
		{
			Base = rhs.Base;
			Quote = rhs.Quote;
			Offers = new ObservableCollection<Offer>(rhs.Offers);
			TradeType = rhs.TradeType;
		}

		/// <summary>The trade direction of offer in this order book. E.g. B2Q means offers to convert Base to Quote</summary>
		public ETradeType TradeType { get; }

		/// <summary>Base currency</summary>
		public Coin Base { get; }

		/// <summary>Quote currency</summary>
		public Coin Quote { get; }

		/// <summary>The buy/sell offers</summary>
		public ObservableCollection<Offer> Offers { get; }

		/// <summary>If positive, then the first order is a minimum (i.e. Q2B). If negative, then the first order is a maximum (i.e. B2Q)</summary>
		public int Sign => TradeType.Sign();

		/// <summary>The number of orders</summary>
		public int Count => Offers.Count;

		/// <summary>Array access</summary>
		public Offer this[int index] => Offers[index];

		/// <summary>Remove all orders</summary>
		public void Clear()
		{
			Offers.Clear();
		}

		/// <summary>Add an offer to the depth of market</summary>
		public void Add(Offer offer, bool validate = true)
		{
			Debug.Assert(!validate || offer.AmountBase != 0m._(Base));
			Debug.Assert(!validate || offer.AmountBase * offer.Price != 0m._(Quote));
			Offers.Add(offer);
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
			foreach (var order in Offers)
			{
				// Price is too high/low to fill 'order', stop.
				if (Sign * price.CompareTo(order.Price) < 0)
					break;

				// The volume remaining is less than the volume of 'order', stop
				if (volume_remaining <= order.AmountBase)
					break;

				// 'order' is smaller than the remaining volume so it would be consumed. However, don't consume
				// 'order' if doing so would leave 'volume_remaining' with an invalid trading volume.
				var rem = volume_remaining - order.AmountBase;
				if (!pair.AmountRangeBase.Contains(rem) || !pair.AmountRangeQuote.Contains(rem * price))
					break;
				
				volume_remaining = rem;
				++count;
			}

			// Remove the orders that have been filled
			var consumed = Offers.Take(count).ToList();
			Offers.RemoveRange(0, count);

			// Remove any remaining volume from the top remaining order if doing so doesn't leave an invalid trading volume
			if (volume_remaining != 0 && Offers.Count != 0 && Sign * price.CompareTo(Offers[0].Price) >= 0)
			{
				var rem = Offers[0].AmountBase - volume_remaining;
				if (pair.AmountRangeBase.Contains(rem) && pair.AmountRangeQuote.Contains(rem * Offers[0].Price))
				{
					consumed.Add(new Offer(Offers[0].Price, volume_remaining));
					Offers[0] = new Offer(Offers[0].Price, rem);
					volume_remaining = 0m._(volume);
				}
			}

			return consumed;
		}

		/// <summary>Return the units for the conversion rate from Base to Quote (i.e. Quote/Base)</summary>
		public string RateUnits    => Base.Symbol != Quote.Symbol ? $"{Quote}/{Base}" : string.Empty;
		public string RateUnitsInv => Base.Symbol != Quote.Symbol ? $"{Base}/{Quote}" : string.Empty;

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

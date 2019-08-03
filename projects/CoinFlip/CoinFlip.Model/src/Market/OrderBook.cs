using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
			Debug.Assert(!validate || offer.AmountBase * offer.PriceQ2B != 0.0._(Quote));
			Offers.Add(offer);
		}

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

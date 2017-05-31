using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoinFlip
{
	/// <summary>The Ask/Bid prices between two coins</summary>
	[DebuggerDisplay("{Base.Symbol}/{Quote.Symbol}")]
	public class TradePair :IDisposable
	{
		// Notes:
		// - Links two coins together by their Ask/Bid prices
		// - Currency pair: Base/Quote e.g. BTC/USDT = $2500
		//   "1 unit of base currency (BTC) == 2500 units of quote currency (USDT)"

		public TradePair(Coin base_, Coin quote)
		{
			Base  = base_;
			Quote = quote;

			Ask = new Orders(+1);
			Bid = new Orders(-1);
		}
		public virtual void Dispose()
		{
			Quote = null;
			Base = null;
		}

		/// <summary>An UID for the trade pair (Exchange dependent value)</summary>
		public int TradePairId
		{
			get;
			set;
		}

		/// <summary>The quote currency</summary>
		public Coin Quote
		{
			get { return m_quote; }
			private set
			{
				if (m_quote == value) return;
				if (m_quote != null)
				{
					m_quote.Pairs.Remove(this);
				}
				m_quote = value;
				if (m_quote != null)
				{
					m_quote.Pairs.Add(this);
				}
			}
		}
		private Coin m_quote;

		/// <summary>The base currency</summary>
		public Coin Base
		{
			get { return m_base; }
			private set
			{
				if (m_base == value) return;
				if (m_base != null)
				{
					m_base.Pairs.Remove(this);
				}
				m_base = value;
				if (m_base != null)
				{
					m_base.Pairs.Add(this);
				}
			}
		}
		private Coin m_base;

		/// <summary>Return the other coin involved in the trade pair</summary>
		public Coin OtherCoin(Coin coin)
		{
			if (coin == Quote) return Base;
			if (coin == Base) return Quote;
			throw new Exception("'coin' is not in this pair");
		}

		/// <summary>The Ask price offers</summary>
		public Orders Ask
		{
			get;
			private set;
		}

		/// <summary>The Ask price offers</summary>
		public Orders Bid
		{
			get;
			private set;
		}
	}

	/// <summary>Depth of market</summary>
	public class Orders
	{
		public Orders(int sign)
		{
			Sign = sign;
			Offers = new List<Offer>();
		}

		/// <summary>The direction of market depth. +1 = Ask (sell orders), -1 = Bid (buy orders)</summary>
		public int Sign
		{
			get;
			private set;
		}

		/// <summary>Return the price for the given volume (null if there are no offers)</summary>
		public decimal? Price(decimal volume = 0)
		{
			foreach (var o in Offers)
			{
				volume -= o.Volume;
				if (volume <= 0)
					return o.Price;
			}
			return null;
		}

		/// <summary>The available offers ordered by price. (Increasing for Ask, Decreasing for Bid)</summary>
		public List<Offer> Offers { get; private set; }

		[DebuggerDisplay("Price={Price} Vol={Volume}")]
		public struct Offer
		{
			public Offer(decimal price, decimal volume)
			{
				Price = price;
				Volume = volume;
			}

			/// <summary>The price (to buy or sell) (in quote currency)</summary>
			public decimal Price { get; set; }

			/// <summary>The volume offered (in base currency)</summary>
			public decimal Volume { get; set; }
		}
	}
}

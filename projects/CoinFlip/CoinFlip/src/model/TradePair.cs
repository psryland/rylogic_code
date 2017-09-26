using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using pr.common;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	/// <summary>The Ask/Bid prices between two coins</summary>
	[DebuggerDisplay("{NameWithExchange,nq}")]
	public class TradePair :IComparable<TradePair>, IComparable
	{
		// Notes:
		// - Links two coins together by their Ask/Bid prices
		// - Currency pair: Base/Quote e.g. BTC/USDT = $2500
		//   "1 unit of base currency (BTC) == 2500 units of quote currency (USDT)"
		//   Note, this means the units of the BTC/USDT rate are USDT-per-BTC, counter intuitively :-/
		//   since X(BTC) * price(USDT/BTC) = Y(USDT)

		public TradePair(Coin base_, Coin quote, Exchange exchange,
			int? trade_pair_id = null,
			RangeF<Unit<decimal>>? volume_range_base = null,
			RangeF<Unit<decimal>>? volume_range_quote = null,
			RangeF<Unit<decimal>>? price_range = null)
		{
			Base  = base_;
			Quote = quote;
			Exchange = exchange;

			TradePairId      = trade_pair_id;
			VolumeRangeBase  = volume_range_base  ?? new RangeF<Unit<decimal>>(0m._(Base), decimal.MaxValue._(Base));
			VolumeRangeQuote = volume_range_quote ?? new RangeF<Unit<decimal>>(0m._(Quote), decimal.MaxValue._(Quote));
			PriceRange       = price_range        ?? new RangeF<Unit<decimal>>(0m._(RateUnits), decimal.MaxValue._(RateUnits));

			Q2B = new OrderBook(base_, quote);
			B2Q = new OrderBook(base_, quote);
		}

		/// <summary>The name of this pair. Format Base/Quote</summary>
		public string Name
		{
			get { return $"{Base?.Symbol ?? "---"}/{Quote?.Symbol ?? "---"}"; }
		}
		public string NameWithExchange
		{
			get { return $"{Name} - {Exchange.Name}"; }
		}

		/// <summary>Return a unique key string for this pair</summary>
		public string UniqueKey
		{
			get { return MakeKey(this); }
		}

		/// <summary>An UID for the trade pair (Exchange dependent value)</summary>
		public int? TradePairId
		{
			get;
			set;
		}

		/// <summary>The quote currency</summary>
		public Coin Quote
		{
			[DebuggerStepThrough] get { return m_quote; }
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
			[DebuggerStepThrough] get { return m_base; }
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

		/// <summary>The exchange offering this trade</summary>
		public Exchange Exchange
		{
			[DebuggerStepThrough] get { return m_exchange; }
			private set
			{
				if (m_exchange == value) return;
				m_exchange = value;
			}
		}
		private Exchange m_exchange;

		/// <summary>Return the other coin involved in the trade pair</summary>
		public Coin OtherCoin(Coin coin)
		{
			if (coin == Quote) return Base;
			if (coin == Base) return Quote;
			throw new Exception("'coin' is not in this pair");
		}

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get; private set; }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get; private set; }

		/// <summary>The allowable range of volume for trading the base currency</summary>
		public RangeF<Unit<decimal>> VolumeRangeBase
		{
			get;
			private set;
		}

		/// <summary>The allowable range of volume for trading the quote currency</summary>
		public RangeF<Unit<decimal>> VolumeRangeQuote
		{
			get;
			private set;
		}

		/// <summary>The allowed price range when trading this pair</summary>
		public RangeF<Unit<decimal>> PriceRange
		{
			get;
			private set;
		}

		/// <summary>Return the Fee charged when trading this pair</summary>
		public decimal Fee
		{
			// When the pairs are on different exchanges there is no fee
			get { return Base.Exchange == Quote.Exchange ? Base.Exchange.Fee : 0; }
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

		/// <summary>Return the current best price for the given trade type</summary>
		public Unit<decimal> CurrentPrice(ETradeType tt)
		{
			switch (tt) {
			default: throw new Exception("Unknown trade type: {0}".Fmt(tt));
			case ETradeType.Q2B: return B2Q.Orders.Count != 0 ? B2Q.Orders[0].Price : 0m._(RateUnits);
			case ETradeType.B2Q: return Q2B.Orders.Count != 0 ? Q2B.Orders[0].Price : 0m._(RateUnits);
			}
		}

		/// <summary>
		/// Convert a volume of 'Base' currency to 'Quote' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.
		/// Use 'volume' = 0 to get the spot price</summary>
		public Trade BaseToQuote(Unit<decimal> volume)
		{
			if (volume < 0m._(Base))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in quote currency.
			var trade = new Trade(ETradeType.B2Q, this, 0m._(Base), 0m._(Quote), 0m._(Quote)/1m._(Base));
			foreach (var x in B2Q)
			{
				if (x.VolumeBase > volume)
				{
					trade.Price = x.Price;
					trade.VolumeIn += volume;
					trade.VolumeOut += x.Price * volume;
					break;
				}
				else
				{
					trade.Price = x.Price;
					trade.VolumeIn += x.VolumeBase;
					trade.VolumeOut += x.Price * x.VolumeBase;
					volume -= x.VolumeBase;
				}
			}
			return trade;
		}

		/// <summary>
		/// Convert a volume of 'Quote' currency to 'Base' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.
		/// Use 'volume' = 0 to get the spot price</summary>
		public Trade QuoteToBase(Unit<decimal> volume)
		{
			if (volume < 0m._(Quote))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in base currency
			// Note, the units are not the typical units for an order because
			// I'm just using 'Order' to pass back a price and volume pair.
			var trade = new Trade(ETradeType.Q2B, this, 0m._(Quote), 0m._(Base), 0m._(Base)/1m._(Quote));
			foreach (var x in Q2B)
			{
				if (x.Price * x.VolumeBase > volume)
				{
					trade.Price = 1m / x.Price;
					trade.VolumeIn += volume;
					trade.VolumeOut += volume / x.Price;
					break;
				}
				else
				{
					trade.Price = 1m / x.Price;
					trade.VolumeIn += x.VolumeQuote;
					trade.VolumeOut += x.VolumeBase;
					volume -= x.VolumeQuote;
				}
			}
			return trade;
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex(ETradeType tt, Unit<decimal> price)
		{
			// Check units
			if (price < 0m._(RateUnits))
				throw new Exception("Invalid price");

			return tt == ETradeType.B2Q
				? Q2B.Orders.BinarySearch(x => +x.Price.CompareTo(price), find_insert_position:true)
				: B2Q.Orders.BinarySearch(x => -x.Price.CompareTo(price), find_insert_position:true);
		}

		/// <summary>The volume of orders with a better price than 'price'</summary>
		public Unit<decimal> OrderBookDepth(ETradeType tt, Unit<decimal> price)
		{
			var index = OrderBookIndex(tt, price);
			var orders = tt == ETradeType.B2Q ? Q2B.Orders : B2Q.Orders;
			return orders.Take(index).Sum(x => x.VolumeBase)._(Base);
		}

		/// <summary>Update this pair using the contents of 'rhs'</summary>
		public void Update(TradePair rhs)
		{
			// Sanity check
			if (Base != rhs.Base || Quote != rhs.Quote || Exchange != rhs.Exchange)
				throw new Exception("Update for the wrong trading pair");

			// Update the update-able parts
			TradePairId      = rhs.TradePairId;
			VolumeRangeBase  = rhs.VolumeRangeBase;
			VolumeRangeQuote = rhs.VolumeRangeQuote;
			PriceRange       = rhs.PriceRange;
			UpdateOrderBook(rhs.B2Q.Orders, rhs.Q2B.Orders);
		}

		/// <summary>Update the list of buy/sell orders</summary>
		public void UpdateOrderBook(IEnumerable<Order> buys, IEnumerable<Order> sells)
		{
			using (B2Q.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				B2Q.Orders.Clear();
				B2Q.Orders.AddRange(buys);
			}
			using (Q2B.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				Q2B.Orders.Clear();
				Q2B.Orders.AddRange(sells);
			}
			Debug.Assert(AssertOrdersValid());
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Name;
		}

		#region Equal
		public bool Equals(TradePair rhs)
		{
			return
				rhs != null &&
				Base.Equals(rhs.Base) &&
				Quote.Equals(rhs.Quote) &&
				Exchange == rhs.Exchange;
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as TradePair);
		}
		public override int GetHashCode()
		{
			return new { Base, Quote, Exchange }.GetHashCode();
		}
		#endregion

		#region IComparable
		public int CompareTo(TradePair rhs)
		{
			return Name.CompareTo(rhs.Name);
		}
		int IComparable.CompareTo(object obj)
		{
			return CompareTo((TradePair)obj);
		}
		#endregion

		/// <summary>Check the orders are in the correct order</summary>
		public bool AssertOrdersValid()
		{
			// Asking price should increase
			for (int i = 1; i < Q2B.Count; ++i)
				if (Q2B[i-1].Price > Q2B[i].Price)
					return false;

			// Bid price should decrease
			for (int i = 1; i < B2Q.Count; ++i)
				if (B2Q[i-1].Price < B2Q[i].Price)
					return false;

			return true;
		}

		/// <summary>Convert two currency symbols into a unique key for this pair</summary>
		public static string MakeKey(string sym0, string sym1)
		{
			return "{0}/{1}".Fmt(sym0, sym1);
		}
		public static string MakeKey(string sym0, string sym1, Exchange exch0, Exchange exch1)
		{
			return MakeKey(sym0, sym1) + " {0}/{1}".Fmt(exch0.Name, exch1.Name);
		}
		public static string MakeKey(TradePair pair)
		{
			return pair.Base.Exchange != pair.Quote.Exchange
				? MakeKey(pair.Base.Symbol, pair.Quote.Symbol, pair.Base.Exchange, pair.Quote.Exchange)
				: MakeKey(pair.Base.Symbol, pair.Quote.Symbol);
		}

		/// <summary>Return the coin that is common between the given two pairs (or null)</summary>
		public static Coin CommonCoin(TradePair lhs, TradePair rhs)
		{
			return
				lhs.Base == rhs.Base ? lhs.Base :
				lhs.Base == rhs.Quote ? lhs.Base :
				lhs.Quote == rhs.Base ? lhs.Quote :
				lhs.Quote == rhs.Quote ? lhs.Quote :
				null;
		}

		/// <summary>Convert a string 'base/quote' into the PairNames helper object</summary>
		public static PairNames Parse(string pair_name)
		{
			return new PairNames(pair_name);
		}
	}

	/// <summary>Helper for handling 'base/quote' pair name strings</summary>
	public class PairNames
	{
		public PairNames(string base_, string quote)
		{
			Base = base_;
			Quote = quote;
		}
		public PairNames(string pair_name)
		{
			string[] coins;
			if (!pair_name.HasValue() || (coins = pair_name.Split('/')).Length != 2)
				throw new ArgumentException("Invalid pair name", nameof(pair_name));

			Base = coins[0];
			Quote = coins[1];
		}
		public PairNames(TradePair pair)
		{
			if (pair == null)
				throw new ArgumentNullException(nameof(pair));

			Base = pair.Base.Symbol;
			Quote = pair.Quote.Symbol;
		}

		/// <summary></summary>
		public string Base { get; private set; }

		/// <summary></summary>
		public string Quote { get; private set; }

		/// <summary>The name of the pair</summary>
		public string Name
		{
			get { return $"{Base}/{Quote}"; }
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Name;
		}
	}
}

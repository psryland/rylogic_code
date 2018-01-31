using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

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
			MarketDepth      = new MarketDepth(base_, quote);
		}

		/// <summary>The currency pair</summary>
		public CoinPair CurrencyPair
		{
			get { return new CoinPair(Base,Quote); }
		}

		/// <summary>The name of this pair. Format Base/Quote</summary>
		public string Name
		{
			get { return CurrencyPair.Name; }
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

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Exchange.Model; }
		}

		/// <summary>Return the other coin involved in the trade pair</summary>
		public Coin OtherCoin(Coin coin)
		{
			if (coin == Quote) return Base;
			if (coin == Base) return Quote;
			throw new Exception("'coin' is not in this pair");
		}

		/// <summary>Enumerate the coins of this pair</summary>
		public IEnumerable<Coin> Coins
		{
			get
			{
				yield return Base;
				yield return Quote;
			}
		}

		/// <summary>The order books for this pair</summary>
		public MarketDepth MarketDepth { [DebuggerStepThrough] get; private set; }

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q { [DebuggerStepThrough] get { return MarketDepth.B2Q; } }

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B { [DebuggerStepThrough] get { return MarketDepth.Q2B; } }

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

		/// <summary>The allowed price range (in Quote/Base) when trading this pair</summary>
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

		/// <summary>Returns the time frames for which candle data is available for this pair</summary>
		public IEnumerable<ETimeFrame> CandleDataAvailable
		{
			get { return Exchange.EnumAvailableCandleData(this).Select(x => x.TimeFrame); }
		}

		/// <summary>Return the spot price (Quote/Base) for the given trade type</summary>
		public Unit<decimal>? SpotPrice(ETradeType tt)
		{
			switch (tt) {
			default: throw new Exception($"Unknown trade type: {tt}");
			case ETradeType.Q2B: return B2Q.Orders.Count != 0 ? B2Q.Orders[0].Price : (Unit<decimal>?)null;
			case ETradeType.B2Q: return Q2B.Orders.Count != 0 ? Q2B.Orders[0].Price : (Unit<decimal>?)null;
			}
		}

		/// <summary>Return the current difference between buy/sell prices</summary>
		public Unit<decimal>? Spread
		{
			// Remember: Q2B spot price = B2Q[0].Price and visa versa
			// Spread is the difference between the buy and sell price,
			// which is always a loss (i.e. negative).
			get { return -(SpotPrice(ETradeType.Q2B) - SpotPrice(ETradeType.B2Q)); }
		}

		/// <summary>
		/// Convert a volume of 'Base' currency to 'Quote' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.
		/// Use 'volume' = 0 to get the spot price</summary>
		public Trade BaseToQuote(string fund_id, Unit<decimal> volume)
		{
			if (volume < 0m._(Base))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in quote currency.
			var trade = new Trade(fund_id, ETradeType.B2Q, this, 0m._(RateUnits), 0m._(Base));
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
		public Trade QuoteToBase(string fund_id, Unit<decimal> volume)
		{
			if (volume < 0m._(Quote))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in base currency
			// Note, the units are not the typical units for an order because
			// I'm just using 'Order' to pass back a price and volume pair.
			var trade = new Trade(fund_id, ETradeType.Q2B, this, 0m._(RateUnits), 0m._(Base));
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

		/// <summary>Convert a volume of currency using the available orders. e.g. Q2B => 'volume' in Quote, out in 'Base'</summary>
		public Trade MakeTrade(string fund_id, ETradeType tt, Unit<decimal> volume)
		{
			return
				tt == ETradeType.Q2B ? QuoteToBase(fund_id, volume) :
				tt == ETradeType.B2Q ? BaseToQuote(fund_id, volume) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex(ETradeType tt, Unit<decimal> price)
		{
			// Check units
			if (price < 0m._(RateUnits))
				throw new Exception("Invalid price");

			// If a trade cannot be filled by existing orders, it becomes an offer.
			// E.g.
			//  - Want to trade B2Q == Sell our 'B' to get 'Q'.
			//  - If there are no suitable B2Q.Orders (i.e. people wanting to buy 'B') then our trade becomes an offer in the Q2B order book.
			//    i.e. we want to buy 'Q' so our trade is a Q2B offer.
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
			MarketDepth.UpdateOrderBook(rhs.B2Q.Orders, rhs.Q2B.Orders);
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
			for (int i = 0; i != Q2B.Count; ++i)
			{
				if (Q2B[i].Price < 0m._(RateUnits))
					throw new Exception("Q2B order book price is invalid");
				if (Q2B[i].VolumeBase < 0m._(Base))
					throw new Exception("Q2B order book volume is invalid");
				if (i > 0 && Q2B[i-1].Price > Q2B[i].Price)
					throw new Exception("Q2B order book prices are out of order");
			}

			// Bid price should decrease
			for (int i = 0; i != B2Q.Count; ++i)
			{
				if (B2Q[i].Price < 0m._(RateUnits))
					throw new Exception("B2Q order book price is invalid");
				if (B2Q[i].VolumeBase < 0m._(Base))
					throw new Exception("B2Q order book volume is invalid");
				if (i > 0 && B2Q[i-1].Price < B2Q[i].Price)
					throw new Exception("B2Q order book prices are out of order");
			}

			// Check the spread
			if (Spread < 0m._(RateUnits))
				throw new Exception("Spread is negative");

			return true;
		}

		/// <summary>Convert two currency symbols into a unique key for this pair</summary>
		public static string MakeKey(string sym0, string sym1)
		{
			return $"{sym0}/{sym1}";
		}
		public static string MakeKey(string sym0, string sym1, Exchange exch0, Exchange exch1)
		{
			return MakeKey(sym0, sym1) + $" {exch0.Name}/{exch1.Name}";
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

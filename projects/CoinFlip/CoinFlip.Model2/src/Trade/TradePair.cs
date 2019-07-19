using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoinFlip.Settings;
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
			RangeF<Unit<double>>? amount_range_base = null,
			RangeF<Unit<double>>? amount_range_quote = null,
			RangeF<Unit<double>>? price_range = null)
		{
			Base  = base_;
			Quote = quote;
			Exchange = exchange;
			SpotPrice = new SpotPrices(base_, quote);
			RateUnits = Base.Symbol != Quote.Symbol ? $"{Quote}/{Base}" : string.Empty;
			RateUnitsInv = Base.Symbol != Quote.Symbol ? $"{Base}/{Quote}" : string.Empty;

			TradePairId = trade_pair_id;
			AmountRangeBase  = amount_range_base  ?? new RangeF<Unit<double>>(0.0._(Base), double.MaxValue._(Base));
			AmountRangeQuote = amount_range_quote ?? new RangeF<Unit<double>>(0.0._(Quote), double.MaxValue._(Quote));
			PriceRange       = price_range        ?? new RangeF<Unit<double>>(0.0._(RateUnits), double.MaxValue._(RateUnits));
			MarketDepth      = new MarketDepth(base_, quote);
		}

		/// <summary>The currency pair</summary>
		public CoinPair CurrencyPair => new CoinPair(Base, Quote);

		/// <summary>The name of this pair. Format Base/Quote</summary>
		public string Name => CurrencyPair.Name;
		public string NameWithExchange => $"{Name} - {Exchange.Name}";

		/// <summary>Return a unique key string for this pair</summary>
		public string UniqueKey => MakeKey(this);

		/// <summary>An UID for the trade pair (Exchange dependent value)</summary>
		public int? TradePairId { get; set; }

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
		public Exchange Exchange { get; }

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

		/// <summary>The current spot price for this pair</summary>
		public SpotPrices SpotPrice { get; }

		/// <summary>Return the current difference between the lowest Q2B offer and the highest B2Q offer (positive number)</summary>
		public Unit<double>? Spread => SpotPrice.Spread;

		/// <summary>The order books for this pair</summary>
		public MarketDepth MarketDepth
		{
			get { return m_market_depth; }
			private set
			{
				if (m_market_depth == value) return;
				if (m_market_depth != null)
				{
					m_market_depth.OrderBookChanged -= HandleOrderBookChanged;
				}
				m_market_depth = value;
				if (m_market_depth != null)
				{
					m_market_depth.OrderBookChanged += HandleOrderBookChanged;
				}

				// Handler
				void HandleOrderBookChanged(object sender, EventArgs e)
				{
					CoinData.NotifyLivePriceChanged(Base);
					CoinData.NotifyLivePriceChanged(Quote);
				}
			}
		}
		private MarketDepth m_market_depth;

		/// <summary>Prices for converting Base to Quote. First price is a maximum</summary>
		public OrderBook B2Q => MarketDepth.B2Q;

		/// <summary>Prices for converting Quote to Base. First price is a minimum</summary>
		public OrderBook Q2B => MarketDepth.Q2B;

		/// <summary>Return the order book that provides offers for a trade in this direction</summary>
		public OrderBook OrderBook(ETradeType tt)
		{
			return
				tt == ETradeType.B2Q ? B2Q :
				tt == ETradeType.Q2B ? Q2B :
				throw new Exception("Unknown trade type");
		}

		/// <summary>The allowable range of amounts for trading the base currency</summary>
		public RangeF<Unit<double>> AmountRangeBase { get; private set; }

		/// <summary>The allowable range of amounts for trading the quote currency</summary>
		public RangeF<Unit<double>> AmountRangeQuote { get; private set; }

		/// <summary>The allowed price range (in Quote/Base) when trading this pair</summary>
		public RangeF<Unit<double>> PriceRange { get; private set; }

		/// <summary>Return the Fee charged when trading this pair. When the pairs are on different exchanges there is no fee</summary>
		public double Fee => Base.Exchange == Quote.Exchange ? Base.Exchange.Fee : 0;

		/// <summary>Return the units for the conversion rate from Base to Quote (i.e. Quote/Base)</summary>
		public string RateUnits { get; }
		public string RateUnitsInv { get; }

		/// <summary>Returns the time frames for which candle data is available for this pair</summary>
		public IEnumerable<ETimeFrame> CandleDataAvailable => Exchange.EnumAvailableCandleData(this).Select(x => x.TimeFrame);

		/// <summary>Return the order type implied by the given trade type and price relative to the spot price</summary>
		public EPlaceOrderType? OrderType(ETradeType trade_type, Unit<double> price_q2b)
		{
			// If the spot price is unknown, then so is the impled order type
			var market_price_q2b = SpotPrice[trade_type];
			if (market_price_q2b == null)
				return null;

			if (trade_type == ETradeType.Q2B)
			{
				return
					price_q2b > market_price_q2b.Value * (1.0 + SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
					price_q2b < market_price_q2b.Value * (1.0 - SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
					EPlaceOrderType.Market;
			}
			if (trade_type == ETradeType.B2Q)
			{
				return
					price_q2b < market_price_q2b.Value * (1.0 + SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
					price_q2b > market_price_q2b.Value * (1.0 - SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
					EPlaceOrderType.Market;
			}
			throw new Exception("Unknown trade type");
		}

		/// <summary>
		/// Convert an amount of 'Base' currency to 'Quote' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.
		/// Use 'amount' = 0 to get the spot price</summary>
		public Trade BaseToQuote(string fund_id, Unit<double> amount)
		{
			if (amount < 0.0._(Base))
				throw new Exception("Invalid amount");

			// Determine the best price and amount in quote currency.
			var trade = new Trade(fund_id, ETradeType.B2Q, this, 0.0._(RateUnits), 0.0._(Base));
			foreach (var x in B2Q)
			{
				if (x.AmountBase > amount)
				{
					trade.Price = x.Price;
					trade.AmountIn += amount;
					trade.AmountOut += x.Price * amount;
					break;
				}
				else
				{
					trade.Price = x.Price;
					trade.AmountIn += x.AmountBase;
					trade.AmountOut += x.Price * x.AmountBase;
					amount -= x.AmountBase;
				}
			}
			return trade;
		}

		/// <summary>
		/// Convert an amount of 'Quote' currency to 'Base' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.
		/// Use 'amount' = 0 to get the spot price</summary>
		public Trade QuoteToBase(string fund_id, Unit<double> amount)
		{
			if (amount < 0.0._(Quote))
				throw new Exception("Invalid amount");

			// Determine the best price and amount in base currency.
			// Note, the units are not the typical units for an order because
			// I'm just using 'trade' to pass back a price and amount pair.
			var trade = new Trade(fund_id, ETradeType.Q2B, this, price_q2b:0.0._(RateUnits), amount_base:0.0._(Base));
			foreach (var x in Q2B)
			{
				if (x.Price * x.AmountBase > amount)
				{
					trade.Price = 1.0 / x.Price;
					trade.AmountIn += amount;
					trade.AmountOut += amount / x.Price;
					break;
				}
				else
				{
					trade.Price = 1.0 / x.Price;
					trade.AmountIn += x.AmountQuote;
					trade.AmountOut += x.AmountBase;
					amount -= x.AmountQuote;
				}
			}
			return trade;
		}

		/// <summary>Convert an amount of currency using the available orders. e.g. Q2B => 'amount' in Quote, out in 'Base'</summary>
		public Trade MakeTrade(string fund_id, ETradeType tt, Unit<double> amount)
		{
			return
				tt == ETradeType.Q2B ? QuoteToBase(fund_id, amount) :
				tt == ETradeType.B2Q ? BaseToQuote(fund_id, amount) :
				throw new Exception("Unknown trade type");
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex(ETradeType tt, Unit<double> price, out bool beyond_order_book)
		{
			// Check units
			if (price < 0.0._(RateUnits))
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
					idx = Q2B.Offers.BinarySearch(x => +x.Price.CompareTo(price), find_insert_position: true);
					beyond_order_book = idx == Q2B.Offers.Count;
					break;
				}
			case ETradeType.Q2B:
				{
					idx = B2Q.Offers.BinarySearch(x => -x.Price.CompareTo(price), find_insert_position: true);
					beyond_order_book = idx == B2Q.Offers.Count;
					break;
				}
			}
			return idx;
		}

		/// <summary>The total value of orders with a better price than 'price'</summary>
		public Unit<double> OrderBookDepth(ETradeType tt, Unit<double> price, out bool beyond_order_book)
		{
			var index = OrderBookIndex(tt, price, out beyond_order_book);
			var orders = tt == ETradeType.B2Q ? Q2B.Offers : B2Q.Offers;
			return orders.Take(index).Sum(x => x.AmountBase);
		}

		/// <summary>Update this pair using the contents of 'rhs'</summary>
		public void Update(TradePair rhs)
		{
			// Sanity check
			if (Base != rhs.Base || Quote != rhs.Quote || Exchange != rhs.Exchange)
				throw new Exception("Update for the wrong trading pair");

			// Update the update-able parts
			TradePairId      = rhs.TradePairId;
			AmountRangeBase  = rhs.AmountRangeBase;
			AmountRangeQuote = rhs.AmountRangeQuote;
			PriceRange       = rhs.PriceRange;
			MarketDepth.UpdateOrderBook(rhs.B2Q.Offers, rhs.Q2B.Offers);
		}

		/// <summary>Return the default amount to use for a trade on 'pair' (in CoinIn currency)</summary>
		public Unit<double> DefaultTradeAmount(ETradeType tt)
		{
			var coin = tt.CoinIn(this);
			return coin.DefaultTradeAmount;
		}

		/// <summary>Return the default amount to use for a trade on this pair (in Base currency)</summary>
		public Unit<double> DefaultTradeAmountBase(ETradeType tt, Unit<double> price_q2b)
		{
			var vol = DefaultTradeAmount(tt);
			return
				tt == ETradeType.B2Q ? vol :                // 'vol' is in base 
				tt == ETradeType.Q2B ? vol / price_q2b :    // 'vol' is in quote
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the amount range to validate against for the input currency</summary>
		public RangeF<Unit<double>> AmountRangeIn(ETradeType tt)
		{
			return
				tt == ETradeType.B2Q ? AmountRangeBase :
				tt == ETradeType.Q2B ? AmountRangeQuote :
				throw new Exception("Unknown trade type");
		}

		/// <summary>Return the amount range to validate against for the output currency</summary>
		public RangeF<Unit<double>> AmountRangeOut(ETradeType tt)
		{
			return
				tt == ETradeType.B2Q ? AmountRangeQuote :
				tt == ETradeType.Q2B ? AmountRangeBase :
				throw new Exception("Unknown trade type");
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
				if (Q2B[i].Price < 0)
					throw new Exception("Q2B order book price is invalid");
				if (Q2B[i].AmountBase < 0)
					throw new Exception("Q2B order book amount is invalid");
				if (i > 0 && Q2B[i-1].Price > Q2B[i].Price)
					throw new Exception("Q2B order book prices are out of order");
			}

			// Bid price should decrease
			for (int i = 0; i != B2Q.Count; ++i)
			{
				if (B2Q[i].Price < 0)
					throw new Exception("B2Q order book price is invalid");
				if (B2Q[i].AmountBase < 0)
					throw new Exception("B2Q order book amount is invalid");
				if (i > 0 && B2Q[i-1].Price < B2Q[i].Price)
					throw new Exception("B2Q order book prices are out of order");
			}

			// Check the spread
			if (Spread < 0.0._(RateUnits))
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

using System;
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

			TradePairId = trade_pair_id;
			VolumeRangeBase = volume_range_base;
			VolumeRangeQuote = volume_range_quote;
			PriceRange = price_range;

			Ask = new OrderBook(base_, quote);
			Bid = new OrderBook(base_, quote);
		}

		/// <summary>The name of this pair. Format Base/Quote</summary>
		public string Name
		{
			get { return "{0}/{1}".Fmt(Base?.Symbol ?? "---", Quote?.Symbol ?? "---"); }
		}
		public string NameWithExchange
		{
			get { return "{0} - {1}".Fmt(Name, Exchange.Name); }
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

		/// <summary>The Ask price offers (Prices for converting Quote to Base. First price is a minimum)</summary>
		public OrderBook Ask
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The Bid price offers (Prices for converting Base to Quote. First price is a maximum)</summary>
		public OrderBook Bid
		{
			[DebuggerStepThrough] get;
			private set;
		}

		/// <summary>The allowable range of volume for </summary>
		public RangeF<Unit<decimal>>? VolumeRangeBase
		{
			get;
			private set;
		}

		/// <summary>The allowable range of volume for </summary>
		public RangeF<Unit<decimal>>? VolumeRangeQuote
		{
			get;
			private set;
		}

		/// <summary>The allowed price range when trading this pair</summary>
		public RangeF<Unit<decimal>>? PriceRange
		{
			get;
			private set;
		}

		/// <summary>Return the Fee charged when trading this pair</summary>
		public decimal Fee
		{
			get
			{
				// When the pairs are on different exchanges there is no fee
				return Base.Exchange == Quote.Exchange
					? Base.Exchange.TransactionFee
					: 0;
			}
		}

		/// <summary>Return the units for the conversion rate from Base to Quote (i.e. Quote/Base)</summary>
		public string RateUnits
		{
			get { return "{0}/{1}".Fmt(Quote, Base); }
		}

		/// <summary>Invalidate the pair data so that the pair will not be traded until updated with the latest data</summary>
		public void Invalidate()
		{
			Ask.Clear();
			Bid.Clear();
		}

		/// <summary>Return the current best price for the given trade type</summary>
		public Unit<decimal> CurrentPrice(ETradeType tt)
		{
			switch (tt) {
			default: throw new Exception("Unknown trade type: {0}".Fmt(tt));
			case ETradeType.Buy:  return Bid.Orders.Count != 0 ? Bid.Orders[0].Price : 0m._(RateUnits);
			case ETradeType.Sell: return Ask.Orders.Count != 0 ? Ask.Orders[0].Price : 0m._(RateUnits);
			}
		}

		/// <summary>Differential update the list of buy/sell orders</summary>
		public void UpdateOrderBook(Order[] buys, Order[] sells)
		{
			using (Bid.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				Bid.Orders.Clear();
				Bid.Orders.AddRange(buys);
			}
			using (Ask.Orders.SuspendEvents(reset_bindings_on_resume: true))
			{
				Ask.Orders.Clear();
				Ask.Orders.AddRange(sells);
			}
			Debug.Assert(AssertOrdersValid());
		}

		/// <summary>
		/// Convert a volume of 'Base' currency to 'Quote' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.</summary>
		public Order BaseToQuote(Unit<decimal> volume)
		{
			if (volume < 0m._(Base))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in quote currency.
			// Note, the units are not the typical units for an order because
			// I'm just using 'Order' to pass back a price and volume pair.
			var order = new Order(0m._(Quote)/1m._(Base), 0m._(Quote));
			foreach (var x in Bid)
			{
				if (x.VolumeBase > volume)
				{
					order.Price = x.Price;
					order.VolumeBase += x.Price * volume;
					break;
				}
				else
				{
					order.VolumeBase += x.Price * x.VolumeBase;
					volume -= x.VolumeBase;
				}
			}
			return order;
		}

		/// <summary>
		/// Convert a volume of 'Quote' currency to 'Base' currency using the available orders.
		/// If there is insufficient liquidity, returns the amount traded from what was available.
		/// Also returns the price at which the conversion would happen.</summary>
		public Order QuoteToBase(Unit<decimal> volume)
		{
			if (volume < 0m._(Quote))
				throw new Exception("Invalid volume");

			// Determine the best price and volume in base currency
			// Note, the units are not the typical units for an order because
			// I'm just using 'Order' to pass back a price and volume pair.
			var order = new Order(0m._(Base)/1m._(Quote), 0m._(Base));
			foreach (var x in Ask)
			{
				if (x.Price * x.VolumeBase > volume)
				{
					order.Price = 1m / x.Price;
					order.VolumeBase += volume / x.Price;
					break;
				}
				else
				{
					order.VolumeBase += x.VolumeBase;
					volume -= x.Price * x.VolumeBase;
				}
			}
			return order;
		}

		/// <summary>Place an order to convert 'volume_base' (base currency) to quote currency at 'price' (Quote/Base)</summary>
		public async Task CreateB2QOrder(Unit<decimal> volume_base, Unit<decimal>? price = null)
		{
			await Exchange.CreateB2QOrder(this, volume_base, price);
		}

		/// <summary>Place an order to convert 'volume_quote' (quote currency) to base currency at 'price' (Base/Quote)</summary>
		public async Task CreateQ2BOrder(Unit<decimal> volume_quote, Unit<decimal>? price = null)
		{
			await Exchange.CreateQ2BOrder(this, volume_quote, price);
		}

		///// <summary>Place an order to buy or sell 'volume' (in base currency) on the exchange that offers this pair</summary>
		//public async Task CreateOrder(ETradeType tt, Unit<decimal> volume, Unit<decimal>? price = null)
		//{
		//	await Exchange.CreateOrder(this, tt, volume, price);
		//}

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
			for (int i = 1; i < Ask.Count; ++i)
				if (Ask[i-1].Price > Ask[i].Price)
					return false;

			// Bid price should decrease
			for (int i = 1; i < Bid.Count; ++i)
				if (Bid[i-1].Price < Bid[i].Price)
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
	}
}

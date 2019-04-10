using System;
using System.Diagnostics;
using System.Threading.Tasks;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Trade
	{
		// Notes:
		//  - A 'Trade' is a description of a trade that *could* be placed. It is different
		//    to an 'Order' which is live on an exchange, waiting to be filled.
		//  - A 'Trade' represents a single exchange of funds, so a completed Order consists
		//    of one or more completed trades.
		//  - AmountIn * Price does not have to equal AmountOut, because 'Trade' is used
		//    with the order book to calculate the best price for trading 'amount_in'.

		/// <summary>Create a trade on 'pair' at 'price_q2b' using 'amount_base' or the default for CoinIn if not given</summary>
		public Trade(string fund_id, ETradeType tt, TradePair pair, Unit<decimal> price_q2b, Unit<decimal>? amount_base = null)
		{
			// Check trade amounts and units
			var amt_base = amount_base ?? pair.DefaultTradeAmountBase(tt, price_q2b);
			if (amt_base < 0m._(pair.Base))
				throw new Exception("Invalid trade amount");
			if (price_q2b < 0m._(pair.RateUnits))
				throw new Exception("Invalid trade price");
			if (amt_base * price_q2b < 0m._(pair.Quote))
				throw new Exception("Invalid trade amount (quote)");

			FundId     = fund_id;
			Pair       = pair;
			TradeType  = tt;
			AmountBase = amt_base;
			PriceQ2B   = price_q2b;
		}

		/// <summary>Create a trade on 'pair' to convert 'amount_in' of 'coin_in' to 'amount_out'</summary>
		public Trade(string fund_id, TradePair pair, Coin coin_in, Unit<decimal> amount_in, Unit<decimal> amount_out)
		{
			FundId = fund_id;
			Pair = pair;
			TradeType =
				pair.Base == coin_in ? ETradeType.B2Q :
				pair.Quote == coin_in ? ETradeType.Q2B :
				throw new Exception($"Currency {coin_in} is not one of {pair.Name}");
			AmountBase = TradeType.AmountBase(PriceQ2B, amount_id:amount_in);
			PriceQ2B = TradeType.PriceQ2B(amount_out / amount_in);
		}

		/// <summary>Copy construct a trade, with the amount scaled by 'scale'</summary>
		public Trade(Trade rhs, decimal scale = 1m)
		{
			FundId     = rhs.FundId;
			Pair       = rhs.Pair;
			TradeType  = rhs.TradeType;
			AmountBase = rhs.AmountBase * scale;
			PriceQ2B   = rhs.PriceQ2B;
		}

		/// <summary>Create a trade based on an existing position</summary>
		public Trade(Order odr)
			:this(odr.FundId, odr.TradeType, odr.Pair, odr.PriceQ2B, odr.AmountBase)
		{}

		/// <summary>Copy constructor</summary>
		public Trade(Trade rhs)
			:this(rhs.FundId, rhs.TradeType, rhs.Pair, rhs.PriceQ2B, rhs.AmountBase)
		{}

		/// <summary>The fund associated with this trade</summary>
		public string FundId { get; }

		/// <summary>The pair being traded</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;
				m_pair = value;
				PriceQ2B = PriceQ2B._(value.RateUnits);
				AmountBase = AmountBase._(value.Base);
			}
		}
		private TradePair m_pair;

		/// <summary>The trade type</summary>
		public ETradeType TradeType { get; set; }

		/// <summary>Given the current spot price, return the order type</summary>
		public EPlaceOrderType OrderType
		{
			get
			{
				var market_price_q2b = Pair.MakeTrade(FundId, TradeType, AmountIn).PriceQ2B;
				if (TradeType == ETradeType.Q2B)
				{
					return
						PriceQ2B > market_price_q2b * (decimal)(1.0 + SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
						PriceQ2B < market_price_q2b * (decimal)(1.0 - SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
						EPlaceOrderType.Market;
				}
				if (TradeType == ETradeType.B2Q)
				{
					return
						PriceQ2B < market_price_q2b * (decimal)(1.0 + SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
						PriceQ2B > market_price_q2b * (decimal)(1.0 - SettingsData.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
						EPlaceOrderType.Market;
				}
				throw new Exception("Unknown trade type");
			}
		}

		/// <summary>The base amount to trade</summary>
		public Unit<decimal> AmountBase { get; set; }

		/// <summary>The amount being sold</summary>
		public Unit<decimal> AmountIn
		{
			get { return TradeType.AmountIn(AmountBase, PriceQ2B); }
			set { AmountBase = TradeType.AmountBase(PriceQ2B, amount_id:value); }
		}

		/// <summary>The amount being bought</summary>
		public Unit<decimal> AmountOut
		{
			get { return TradeType.AmountOut(AmountBase, PriceQ2B); }
			set { AmountBase = TradeType.AmountBase(PriceQ2B, amount_out:value); }
		}

		/// <summary>The amount being bought after commissions</summary>
		public Unit<decimal> AmountNett => AmountOut * (1 - Pair.Fee);

		/// <summary>The price to make the trade at (Quote/Base)</summary>
		public Unit<decimal> PriceQ2B { get; set; }

		/// <summary>The price to make the trade at (CoinOut/CoinIn)</summary>
		public Unit<decimal> Price
		{
			get { return TradeType.Price(PriceQ2B); }
			set { PriceQ2B = TradeType.PriceQ2B(value); }
		}

		/// <summary>The inverse of the price to make the trade at (in CoinIn/CoinOut)</summary>
		public Unit<decimal> PriceInv => Math_.Div(1m._(), Price, 0m / 1m._(Price));

		/// <summary>The effective price of this trade after fees (in Quote/Base)</summary>
		public Unit<decimal> PriceQ2BNett => TradeType.PriceQ2B(PriceNett);

		/// <summary>The effective price of this trade after fees (in CoinOut/CoinIn)</summary>
		public Unit<decimal> PriceNett => AmountNett / AmountIn;

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex => Pair.OrderBookIndex(TradeType, PriceQ2B, out var _);

		/// <summary>The depth of this position in the order book for the trade type</summary>
		public Unit<decimal> OrderBookDepth => Pair.OrderBookDepth(TradeType, PriceQ2B, out var _);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote;

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base;

		/// <summary>The allowable range on input trade amounts</summary>
		public RangeF<Unit<decimal>> PriceRange => Pair.PriceRange;

		/// <summary>The allowable range on input trade amounts</summary>
		public RangeF<Unit<decimal>> AmountRangeIn => Pair.AmountRangeIn(TradeType);

		/// <summary>The allowable range on output trade amounts</summary>
		public RangeF<Unit<decimal>> AmountRangeOut => Pair.AmountRangeOut(TradeType);

		/// <summary>String description of the trade</summary>
		public string Description => $"{AmountIn.ToString("G6", true)} → {AmountOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}";

		/// <summary>Check whether this trade is an allowed trade</summary>
		public EValidation Validate(Guid? reserved_balance_in = null, Unit<decimal>? additional_balance_in = null)
		{
			// Notes:
			// - 'reserved_balance' is the Id on a hold placed on a certain amount of balance
			//    usually when it will be needed to offset another trade. Providing this guid
			//    means include it in the available balance.
			// - 'additional_balance' is extra balance that is assumed to be available for the
			//    trade. Typically, this would be from a trade that will be cancelled freeing
			//    up some available amount (i.e. in modifying an existing order)

			var result = EValidation.Valid;

			// Check the trade amounts
			if (AmountIn <= 0)
				result |= EValidation.AmountInIsInvalid;
			else if (!Pair.AmountRangeIn(TradeType).Contains(AmountIn))
				result |= EValidation.AmountInOutOfRange;
			if (AmountOut <= 0)
				result |= EValidation.AmountOutIsInvalid;
			else if (!Pair.AmountRangeOut(TradeType).Contains(AmountOut))
				result |= EValidation.AmountOutOutOfRange;

			// Check the price limits.
			if (Price <= 0)
				result |= EValidation.PriceIsInvalid;
			else if (!Pair.PriceRange.Contains(PriceQ2B))
				result |= EValidation.PriceOutOfRange;

			var bal = TradeType.CoinIn(Pair).Balances[FundId];
			var available = bal.Available;
			if (reserved_balance_in != null) available += bal.Reserved(reserved_balance_in.Value);
			if (additional_balance_in != null) available += additional_balance_in.Value;

			// Check for sufficient balance
			if (AmountIn > available)
				result |= EValidation.InsufficientBalance;

			// Allowing for fees:
			// Poloniex: Fee reduces the currency being received.
			// Cryptopia: Fee is charged on the quote amount amount.

			// When trading Q2B (i.e. buying base currency) commissions reduce the amount of base currency received.
			// When trading B2Q (i.e. selling base currency) commissions are removed from

			//// Check the balances (allowing for fees)
			//var bal = TradeType == ETradeType.B2Q ? Pair.Base.Balance : Pair.Quote.Balance;
			//var available = bal.Available + (reserved_balance != null ? bal.Reserved(reserved_balance.Value) : 0m._(bal.Coin));
			//if (available < AmountIn * (1.0000001m + Pair.Fee))
			//	result |= EValidation.InsufficientBalance;

			return result;
		}

		/// <summary>Create this trade on the Exchange that owns 'Pair'</summary>
		public async Task<OrderResult> CreateOrder()
		{
			return await Pair.Exchange.CreateOrder(FundId, TradeType, Pair, AmountIn, Price);
		}

		#region Equals
		public bool Equals(Trade rhs)
		{
			return
				rhs != null &&
				TradeType == rhs.TradeType &&
				Pair == rhs.Pair &&
				Math_.Abs(PriceQ2B - rhs.PriceQ2B) < Misc.PriceEpsilon._(PriceQ2B) &&
				Math_.Abs(AmountBase - rhs.AmountBase) < Misc.AmountEpsilon._(AmountBase);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as Trade);
		}
		public override int GetHashCode()
		{
			return new { TradeType, Pair, PriceQ2B, AmountBase }.GetHashCode();
		}
		#endregion
	}
}

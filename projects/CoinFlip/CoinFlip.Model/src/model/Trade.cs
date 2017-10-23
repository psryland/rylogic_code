using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using pr.common;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	/// <summary>
	/// A 'Trade' is a description of a trade that could be placed. It is different
	/// to a 'Position' which is live on an exchange, waiting to be filled.</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class Trade
	{
		// Notes:
		//  - VolumeIn * Price does not have to equal VolumeOut, because 'Trade' is used
		//    with the order book to calculate the best price for trading 'volume_in'.

		/// <summary>Create a trade on 'pair' at 'price_q2b' using 'volume_base' or the default for CoinIn if not given</summary>
		public Trade(ETradeType tt, TradePair pair, Unit<decimal> price_q2b, Unit<decimal>? volume_base = null)
		{
			var vol_base = volume_base ?? tt.DefaultTradeVolumeBase(pair, price_q2b);
			var price = tt.Price(price_q2b);
			var volume_in = tt.VolumeIn(vol_base, price_q2b);
			var volume_out = tt.VolumeOut(vol_base, price_q2b);

			// Check trade volumes and units
			if (tt == ETradeType.B2Q)
			{
				if (volume_in < 0m._(pair.Base))
					throw new Exception("Invalid trade volume in");
				if (volume_out < 0m._(pair.Quote))
					throw new Exception("Invalid trade volume out");
				if (price < 0m._(pair.RateUnits))
					throw new Exception("Invalid trade price");
			}
			if (tt == ETradeType.Q2B)
			{
				if (volume_in < 0m._(pair.Quote))
					throw new Exception("Invalid trade volume in");
				if (volume_out < 0m._(pair.Base))
					throw new Exception("Invalid trade volume out");
				if (price < 0m._(pair.RateUnitsInv))
					throw new Exception("Invalid trade price");
			}

			TradeType = tt;
			Pair      = pair;
			VolumeIn  = volume_in;
			VolumeOut = volume_out;
			PriceQ2B  = price_q2b;
		}

		/// <summary>Copy construct a trade, with the volume scaled by 'scale'</summary>
		public Trade(Trade rhs, decimal scale = 1m)
		{
			TradeType = rhs.TradeType;
			Pair      = rhs.Pair;
			VolumeIn  = rhs.VolumeIn * scale;
			VolumeOut = rhs.VolumeOut * scale;
			PriceQ2B  = rhs.PriceQ2B;
		}

		/// <summary>Create a trade based on an existing position</summary>
		public Trade(Position pos)
			:this(pos.TradeType, pos.Pair, pos.PriceQ2B, pos.VolumeBase)
		{}

		/// <summary>Access the app model</summary>
		public Model Model
		{
			get { return Pair.Exchange.Model; }
		}

		/// <summary>The trade type</summary>
		public ETradeType TradeType
		{
			get { return m_trade_type; }
			set
			{
				if (m_trade_type == value) return;
				m_trade_type = value;

				// Swap
				var tmp = VolumeIn;
				VolumeIn = VolumeOut;
				VolumeOut = tmp;
			}
		}
		private ETradeType m_trade_type;

		/// <summary>The pair being traded</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;
				m_pair = value;
				PriceQ2B = PriceQ2B._(value.RateUnits);
				VolumeIn = VolumeIn._(TradeType.CoinIn(value));
				VolumeOut = VolumeOut._(TradeType.CoinOut(value));
			}
		}
		private TradePair m_pair;

		/// <summary>Give the current spot price, return the order type</summary>
		public EPlaceOrderType OrderType
		{
			get
			{
				var market_price_q2b = Pair.MakeTrade(TradeType, VolumeIn).PriceQ2B;
				if (TradeType == ETradeType.Q2B)
				{
					return
						PriceQ2B > market_price_q2b * (decimal)(1.0 + Model.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
						PriceQ2B < market_price_q2b * (decimal)(1.0 - Model.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
						EPlaceOrderType.Market;
				}
				if (TradeType == ETradeType.B2Q)
				{
					return
						PriceQ2B < market_price_q2b * (decimal)(1.0 + Model.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Stop :
						PriceQ2B > market_price_q2b * (decimal)(1.0 - Model.Settings.MarketOrderPriceToleranceFrac) ? EPlaceOrderType.Limit :
						EPlaceOrderType.Market;
				}
				throw new Exception("Unknown trade type");
			}
		}

		/// <summary>The volume being sold</summary>
		public Unit<decimal> VolumeIn { get; set; }

		/// <summary>The volume being bought</summary>
		public Unit<decimal> VolumeOut { get; set; }

		/// <summary>The volume being bought after commissions</summary>
		public Unit<decimal> VolumeNett
		{
			get { return VolumeOut * (1 - Pair.Fee); }
		}

		/// <summary>The price to make the trade at (Quote/Base)</summary>
		public Unit<decimal> PriceQ2B { get; set; }

		/// <summary>The price to make the trade at (CoinOut/CoinIn)</summary>
		public Unit<decimal> Price
		{
			get { return TradeType.Price(PriceQ2B); }
			set { PriceQ2B = TradeType.PriceQ2B(value); }
		}

		/// <summary>The inverse of the price to make the trade at (in CoinIn/CoinOut)</summary>
		public Unit<decimal> PriceInv
		{
			get { return Maths.Div(1m._(), Price, 0m / 1m._(Price)); }
		}

		/// <summary>The effective price of this trade after fees (in Quote/Base)</summary>
		public Unit<decimal> PriceQ2BNett
		{
			get { return TradeType.PriceQ2B(PriceNett); }
		}

		/// <summary>The effective price of this trade after fees (in CoinOut/CoinIn)</summary>
		public Unit<decimal> PriceNett
		{
			get { return VolumeNett / VolumeIn; }
		}

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex
		{
			get { return Pair.OrderBookIndex(TradeType, PriceQ2B); }
		}

		/// <summary>The depth of this position in the order book for the trade type</summary>
		public Unit<decimal> OrderBookDepth
		{
			get { return Pair.OrderBookDepth(TradeType, PriceQ2B); }
		}

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Base : Pair.Quote; }
		}

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut
		{
			get { return TradeType == ETradeType.B2Q ? Pair.Quote : Pair.Base; }
		}

		/// <summary>The allowable range on input trade volumes</summary>
		public RangeF<Unit<decimal>> PriceRange
		{
			get { return Pair.PriceRange; }
		}

		/// <summary>The allowable range on input trade volumes</summary>
		public RangeF<Unit<decimal>> VolumeRangeIn
		{
			get { return TradeType.VolumeRangeIn(Pair); }
		}

		/// <summary>The allowable range on output trade volumes</summary>
		public RangeF<Unit<decimal>> VolumeRangeOut
		{
			get { return TradeType.VolumeRangeOut(Pair); }
		}

		/// <summary>String description of the trade</summary>
		public string Description
		{
			get { return $"{VolumeIn.ToString("G6", true)} → {VolumeOut.ToString("G6", true)} @ {PriceQ2B.ToString("G6", true)}"; }
		}

		/// <summary>Check whether this trade is an allowed trade</summary>
		public EValidation Validate(Guid? reserved_balance = null)
		{
			var result = EValidation.Valid;

			// Check the trade volumes
			if (VolumeIn <= 0)
				result |= EValidation.VolumeInIsInvalid;
			else if (!TradeType.VolumeRangeIn(Pair).Contains(VolumeIn))
				result |= EValidation.VolumeInOutOfRange;
			if (VolumeOut <= 0)
				result |= EValidation.VolumeOutIsInvalid;
			else if (!TradeType.VolumeRangeOut(Pair).Contains(VolumeOut))
				result |= EValidation.VolumeOutOutOfRange;

			// Check the price limits.
			if (Price <= 0)
				result |= EValidation.PriceIsInvalid;
			else if (!Pair.PriceRange.Contains(PriceQ2B))
				result |= EValidation.PriceOutOfRange;

			// Check for sufficient balance
			var bal = TradeType.CoinIn(Pair).Balance;
			var available = bal.Available + (reserved_balance != null ? bal.Reserved(reserved_balance.Value) : 0m._(bal.Coin));
			if (VolumeIn > available)
				result |= EValidation.InsufficientBalance;

			// Allowing for fees:
			// Poloniex: Fee reduces the currency being received.
			// Cryptopia: Fee is charged on the quote volume amount.

			// When trading Q2B (i.e. buying base currency) commissions reduce the amount of base currency received.
			// When trading B2Q (i.e. selling base currency) commissions are removed from

			//// Check the balances (allowing for fees)
			//var bal = TradeType == ETradeType.B2Q ? Pair.Base.Balance : Pair.Quote.Balance;
			//var available = bal.Available + (reserved_balance != null ? bal.Reserved(reserved_balance.Value) : 0m._(bal.Coin));
			//if (available < VolumeIn * (1.0000001m + Pair.Fee))
			//	result |= EValidation.InsufficientBalance;

			return result;
		}

		/// <summary>Create this trade on the Exchange that owns 'Pair'</summary>
		public TradeResult CreateOrder()
		{
			return Pair.Exchange.CreateOrder(TradeType, Pair, VolumeIn, Price);
		}
	}

	/// <summary>The result of a submit trade request</summary>
	[DebuggerDisplay("{Description,nq}")]
	public class TradeResult
	{
		public TradeResult(TradePair pair)
			:this(pair, 0)
		{}
		public TradeResult(TradePair pair, ulong order_id)
			:this(pair, order_id, null)
		{}
		public TradeResult(TradePair pair, ulong order_id, IEnumerable<ulong> trade_ids)
		{
			Pair = pair;
			OrderId = order_id;
			TradeIds = trade_ids?.Cast<ulong>().ToList() ?? new List<ulong>();
		}

		/// <summary>The pair that was traded</summary>
		public TradePair Pair { get; private set; }

		/// <summary>The ID of an order that has been added to the order book of a pair</summary>
		public ulong OrderId { get; private set; }

		/// <summary>Filled orders as a result of a submitted trade</summary>
		public List<ulong> TradeIds { get; private set; }

		/// <summary>A string description of this trade result</summary>
		public string Description
		{
			get { return $"{Pair.Name} Id={OrderId} [{string.Join(",",TradeIds)}]"; }
		}
	}
}

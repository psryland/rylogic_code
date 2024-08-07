using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using CoinFlip.Settings;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Maths;
using Rylogic.Utility;

namespace CoinFlip
{
	[DebuggerDisplay("{Description,nq}")]
	public class Trade :INotifyPropertyChanged
	{
		// Notes:
		//  - A 'Trade' is a description of a trade that *could* be placed. It is different
		//    to an 'Order' which is live on an exchange, waiting to be filled.
		//  - AmountIn * Price does not have to equal AmountOut, because 'Trade' is used
		//    with the order book to calculate the best price for trading a given amount.
		//    The price will represent the best price that covers all of the amount, not
		//    the spot price.
		//  - Don't implicitly change amounts/prices based on order type for the same reason.
		//    Instead, allow any values to be set and use validate to check they're correct.
		//    The 'EditTradeUI' should be used to modify properties and ensure correct behaviour
		//    w.r.t to order type.
		// Rounding issues: 
		//  - Quantisation doesn't help, that just makes the discrepancies larger.
		//  - Instead, maintain separate values for amount in and amount out. These imply the
		//    price and allow control over which side of the trade gets rounded.

		/// <summary>Create a trade on 'pair' to convert 'amount_in' of 'coin_in' to 'amount_out'</summary>
		public Trade(Fund fund, TradePair pair, EOrderType order_type, ETradeType trade_type, Unit<decimal> amount_in, Unit<decimal> amount_out, Unit<decimal>? price_q2b = null, string? creator = null)
		{
			// Check trade amounts and units
			if (amount_in < 0m._(trade_type.CoinIn(pair)))
				throw new Exception("Invalid trade 'in' amount");
			if (amount_out < 0m._(trade_type.CoinOut(pair)))
				throw new Exception("Invalid trade 'out' amount");
			if (amount_out != 0 && amount_in != 0 && trade_type.PriceQ2B(amount_out / amount_in) < 0m._(pair.RateUnits))
				throw new Exception("Invalid trade price");

			CreatorName = creator ?? string.Empty;
			Fund = fund;
			Pair = pair;
			OrderType = order_type;
			TradeType = trade_type;
			AmountIn = amount_in;
			AmountOut = amount_out;
			PriceQ2B =
				price_q2b != null ? price_q2b.Value :
				amount_out != 0 && amount_in != 0 ? TradeType.PriceQ2B(amount_out / amount_in) :
				SpotPriceQ2B;
		}

		/// <summary>Copy construct a trade, with the amount scaled by 'scale'</summary>
		public Trade(Trade rhs, decimal scale = 1)
			: this(rhs.Fund, rhs.Pair, rhs.OrderType, rhs.TradeType, rhs.AmountIn * scale, rhs.AmountOut * scale, rhs.PriceQ2B, rhs.CreatorName)
		{ }

		/// <summary>Create a trade based on an existing position</summary>
		public Trade(Order rhs)
			: this(rhs.Fund, rhs.Pair, rhs.OrderType, rhs.TradeType, rhs.AmountIn, rhs.AmountOut, rhs.PriceQ2B, rhs.CreatorName)
		{ }

		/// <summary>A helper string for recording who created this trade</summary>
		public string CreatorName { get; set; }

		/// <summary>The fund associated with this trade</summary>
		public Fund Fund
		{
			get => m_fund;
			set
			{
				if (m_fund == value) return;
				m_fund = value;
				NotifyPropertyChanged(nameof(Fund));
			}
		}
		private Fund m_fund = null!;

		/// <summary>The exchange that this trade is/would be on</summary>
		public Exchange Exchange => Pair.Exchange;

		/// <summary>The pair being traded</summary>
		public TradePair Pair
		{
			get { return m_pair; }
			set
			{
				if (m_pair == value) return;

				// Since 'Trade' is not dispose able, we have to attach a weak handler to 'Needed'
				// on the market data. The handler contains 'this' so that while this object lives
				// the 'Needed' event will signal true.
				var HandleNeeded = WeakRef.MakeWeak<HandledEventArgs>((s, a) => a.Handled = this != null, h => m_pair.MarketDepth.Needed -= h);
				var HandleOBChanged = WeakRef.MakeWeak((s, a) => NotifyPropertyChanged(nameof(DistanceQ2BDesc)), h => m_pair.MarketDepth.OrderBookChanged -= h);

				if (m_pair != null)
				{
					m_pair.MarketDepth.OrderBookChanged -= HandleOBChanged;
					m_pair.MarketDepth.Needed -= HandleNeeded;

					m_amount_in = m_amount_in._();
					m_amount_out = m_amount_out._();
				}
				m_pair = value;
				if (m_pair != null)
				{
					m_amount_in = m_amount_in._(TradeType.CoinIn(m_pair));
					m_amount_out = m_amount_out._(TradeType.CoinOut(m_pair));

					m_pair.MarketDepth.Needed += HandleNeeded;
					m_pair.MarketDepth.OrderBookChanged += HandleOBChanged;
				}

				NotifyPropertyChanged(nameof(Pair));
			}
		}
		private TradePair m_pair = null!;

		/// <summary>Given the current spot price, return the order type</summary>
		public EOrderType OrderType
		{
			get => m_order_type;
			set
			{
				if (m_order_type == value) return;
				m_order_type = value;
				NotifyPropertyChanged(nameof(OrderType));

				// Note: Don't try to make this adjust other properties when changed,
				// that just makes the type unpredictable. Keep properties independent
				// and use validate to check for correctness
			}
		}
		private EOrderType m_order_type;

		/// <summary>The trade type</summary>
		public ETradeType TradeType
		{
			get => m_trade_type;
			set
			{
				if (m_trade_type == value) return;
				m_trade_type = value;
				Util.Swap(ref m_amount_in, ref m_amount_out);
				NotifyPropertyChanged(nameof(TradeType));
			}
		}
		private ETradeType m_trade_type;

		/// <summary>The input amount to trade (in base or quote, depending on 'TradeType')</summary>
		public Unit<decimal> AmountIn
		{
			get => m_amount_in;
			set
			{
				Debug.Assert(value >= 0m._(TradeType.CoinIn(Pair)));
				if (m_amount_in != 0 && m_amount_in == value) return;
				m_amount_in = value;
				NotifyPropertyChanged(nameof(AmountIn));

				// Note: Don't try to make this adjust other properties when changed,
				// that just makes the type unpredictable. Keep properties independent
				// and use validate to check for correctness
			}
		}
		private Unit<decimal> m_amount_in;

		/// <summary>The output amount of the trade excluding commissions (in quote or base, depending on 'TradeType')</summary>
		public Unit<decimal> AmountOut
		{
			get => m_amount_out;
			set
			{
				Debug.Assert(value >= 0m._(TradeType.CoinOut(Pair)));
				if (m_amount_out != 0 && m_amount_out == value) return;
				m_amount_out = value;
				NotifyPropertyChanged(nameof(AmountOut));

				// Note: Don't try to make this adjust other properties when changed,
				// that just makes the type unpredictable. Keep properties independent
				// and use validate to check for correctness
			}
		}
		private Unit<decimal> m_amount_out;

		/// <summary>The price to make the trade at (Quote/Base)</summary>
		public Unit<decimal> PriceQ2B
		{
			get => m_price_q2b;
			set
			{
				Debug.Assert(value >= 0m._(Pair.RateUnits));
				if (m_price_q2b != 0 && m_price_q2b == value) return;
				m_price_q2b = value;
				NotifyPropertyChanged(nameof(PriceQ2B));
				
				// Note: Don't try to make this adjust other properties when changed,
				// that just makes the type unpredictable. Keep properties independent
				// and use validate to check for correctness
			}
		}
		private Unit<decimal> m_price_q2b;

		/// <summary>The price (in CoinOut/CoinIn)</summary>
		public Unit<decimal> Price
		{
			get => PriceQ2B != 0 ? TradeType.Price(PriceQ2B) : 0m._(TradeType.RateUnits(Pair));
			set => PriceQ2B = value != 0 ? TradeType.PriceQ2B(value) : 0m._(Pair.RateUnits);
		}

		/// <summary>The base amount to trade</summary>
		public Unit<decimal> AmountBase => TradeType.AmountBase(PriceQ2B, AmountIn, AmountOut);

		/// <summary>The amount to trade (in quote currency) based on the trade price</summary>
		public Unit<decimal> AmountQuote => TradeType.AmountQuote(PriceQ2B, AmountIn, AmountOut);

		/// <summary>The commission that would be charged on this trade (in CommissionCoin)</summary>
		public Unit<decimal> Commission => Exchange.Fee * AmountOut;

		/// <summary>The commission that would be charged on this trade (in quote currency)</summary>
		public Coin CommissionCoin => CoinOut;

		/// <summary>Return the current spot price of the pair associated with this order</summary>
		public Unit<decimal> SpotPriceQ2B => Pair.SpotPrice[TradeType];// ?? 0m._(Pair.RateUnits);

		/// <summary>The position of this trade in the order book for the trade type</summary>
		public int OrderBookIndex => Pair.OrderBookIndex(TradeType, PriceQ2B, out var _);

		/// <summary>The depth of this position in the order book for the trade type</summary>
		public Unit<decimal> OrderBookDepth => Pair.OrderBookDepth(TradeType, PriceQ2B, out var _);

		/// <summary>The coin type being sold</summary>
		public Coin CoinIn => TradeType.CoinIn(Pair);

		/// <summary>The coin type being bought</summary>
		public Coin CoinOut => TradeType.CoinOut(Pair);

		/// <summary>The allowable range on input trade amounts</summary>
		public RangeF<Unit<decimal>> PriceRange => Pair.PriceRangeQ2B;

		/// <summary>The allowable range on input trade amounts</summary>
		public RangeF<Unit<decimal>> AmountRangeIn => Pair.AmountRangeIn(TradeType);

		/// <summary>The allowable range on output trade amounts</summary>
		public RangeF<Unit<decimal>> AmountRangeOut => Pair.AmountRangeOut(TradeType);

		/// <summary>The price distance between the order price and the current spot price</summary>
		public Unit<decimal>? DistanceQ2B
		{
			get
			{
				var spot_b2q = Pair.SpotPrice[ETradeType.B2Q];
				var spot_q2b = Pair.SpotPrice[ETradeType.Q2B];
				return
					TradeType == ETradeType.B2Q ? (PriceQ2B - spot_b2q) : //TradeType == ETradeType.B2Q && spot_b2q != null ? (PriceQ2B - spot_b2q.Value) :
					TradeType == ETradeType.Q2B ? (spot_q2b - PriceQ2B) : //TradeType == ETradeType.Q2B && spot_q2b != null ? (spot_q2b.Value - PriceQ2B) :
					(Unit<decimal>?)0m;
			}
		}

		/// <summary>UI Friendly distance description</summary>
		public string DistanceQ2BDesc
		{
			get
			{
				var dist = DistanceQ2B;
				var idx = Pair.OrderBookIndex(TradeType, PriceQ2B, out var beyond);
				var pls = beyond ? "+" : string.Empty;
				return dist != null ? $"{dist.Value.ToString(6, false)} ({idx}{pls})" : "---";
			}
		}

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

			// Check the price
			if (PriceQ2B <= 0)
				result |= EValidation.PriceIsInvalid;
			else if (!Pair.PriceRangeQ2B.Contains(PriceQ2B))
				result |= EValidation.PriceOutOfRange;
			else if (AmountIn != 0 && !Misc.EqlPrice(Price, AmountOut / AmountIn, Price * 0.0001m))
				result |= EValidation.Inconsistent;

			// Check the order type
			if (OrderType == EOrderType.Limit)
			{
				if ((TradeType == ETradeType.Q2B && PriceQ2B > Pair.SpotPrice[TradeType]) ||
					(TradeType == ETradeType.B2Q && PriceQ2B < Pair.SpotPrice[TradeType]))
					result |= EValidation.InvalidOrderType;
			}
			if (OrderType == EOrderType.Stop)
			{
				if ((TradeType == ETradeType.Q2B && PriceQ2B < Pair.SpotPrice[TradeType]) ||
					(TradeType == ETradeType.B2Q && PriceQ2B > Pair.SpotPrice[TradeType]))
					result |= EValidation.InvalidOrderType;
			}

			// Check for sufficient balance
			var bal = Fund[TradeType.CoinIn(Pair)];
			var available = bal.Available;
			if (reserved_balance_in != null) available += bal.Holds[reserved_balance_in.Value].Amount;
			if (additional_balance_in != null) available += additional_balance_in.Value;
			if (AmountIn > available)
				result |= EValidation.InsufficientBalance;

			return result;
		}

		/// <summary>Create this trade on the Exchange that owns 'Pair'</summary>
		public async Task<OrderResult> CreateOrder(CancellationToken cancel)
		{
			// Create the order on the exchange
			Model.Log.Write(ELogLevel.Info, $"Creating or modifying trade: {Description}");
			return await Exchange.CreateOrder(this, cancel);
		}

		/// <summary>Trade property changed</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			// Note, don't notify for all derived properties, there's too many combinations.
			// Observers should just handle the properties with setters. UI's will need to
			// wrap this type to handle property changed.
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>A string description of the trade type</summary>
		public string TradeTypeDesc =>
			TradeType == ETradeType.Q2B ? $"{Pair.Quote}→{Pair.Base} ({TradeType})" :
			TradeType == ETradeType.B2Q ? $"{Pair.Base}→{Pair.Quote} ({TradeType})" :
			"---";

		/// <summary>The basic colour to associate with this trade</summary>
		public Colour32 TradeColour =>
			TradeType == ETradeType.Q2B ? SettingsData.Settings.Chart.Q2BColour :
			TradeType == ETradeType.B2Q ? SettingsData.Settings.Chart.B2QColour :
			throw new Exception($"Unknown trade type: {TradeType}");

		/// <summary>String description of the trade</summary>
		public virtual string Description => $"{AmountIn.ToString(6, true)} → {AmountOut.ToString(6, true)} @ ~{PriceQ2B.ToString(4, true)}";

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
		public override bool Equals(object? obj)
		{
			return obj is Trade trade && Equals(trade);
		}
		public override int GetHashCode()
		{
			return new { TradeType, Pair, PriceQ2B, AmountBase }.GetHashCode();
		}
		#endregion
	}
}

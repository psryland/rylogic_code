using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	[DebuggerDisplay("{DebuggerDisplay}")]
	public class Trade :ITrade
	{
		// Notes:
		//  A 'Trade' is the bot's representation of a position on a symbol, or simulated position on a symbol.
		//  Trades can be used to track the profit/loss of a position without having to actually hold the position.
		//  Trades are different to 'Position', 'PendingOrder', or 'Order'
		//  SL/TP are nullable because they are not required, even though it's generally a good idea to have them

		/// <summary>Trade state</summary>
		public enum EResult
		{
			Unknown,
			StopEntryOrder,
			LimitOrder,

			// All below here are considered open
			Open,

			// All below here are considered closed
			Closed,
			HitSL,
			HitTP
		}

		/// <summary>Create a trade with explicit values</summary>
		public Trade(Instrument instr, TradeType tt, string label, QuoteCurrency ep, QuoteCurrency? sl, QuoteCurrency? tp, long volume, string comment = null, Idx? idx = null, EResult result = EResult.Unknown)
		{
			CAlgoId    = null;
			TradeIndex = m_trade_index++;
			TradeType  = tt;
			Instrument = instr;
			Result     = result;
			EntryIndex = (double)(idx ?? instr.IdxNow) - instr.IdxFirst;
			ExitIndex  = EntryIndex;
			Expiration = null;
			Label      = label ?? string.Empty;
			Comment    = comment ?? string.Empty;

			EP         = ep;
			SL         = sl;
			TP         = tp;
			Volume     = volume;

			MaxFavourableExcursion = 0.0;
			MaxAdverseExcursion    = 0.0;
			NetProfit              = 0.0;
			GrossProfit            = 0.0;
		}

		/// <summary>
		/// Create a trade with automatic SL and TP levels set.
		/// SL/TP levels are set based on the current account balance (even if 'idx' != 0)</summary>
		/// <param name="instr">The instrument to be traded</param>
		/// <param name="tt">Whether to buy or sell</param>
		/// <param name="label">Optional. An identifying name for the trade</param>
		/// <param name="ep">Optional. The price at which the trade was entered. (default is current ask/bid price)</param>
		/// <param name="sl">Optional. The stop loss (absolute) to use instead of automatically finding one</param>
		/// <param name="tp">Optional. The take profit (absolute) to use instead of automatically finding one</param>
		/// <param name="risk">Optional. Scaling factor for the amount to risk. (default is 1.0)</param>
		/// <param name="comment">Optional. A comment/tag associated with the trade</param>
		/// <param name="idx">Optional. The instrument index of when the trade was created. (default is the current time)</param>
		public Trade(Instrument instr, TradeType tt, string label = null, QuoteCurrency? ep = null, QuoteCurrency? sl = null, QuoteCurrency? tp = null, double? risk = null, string comment = null, Idx? idx = null, EResult result = EResult.Unknown)
			:this(instr, tt, label, 0, null, null, 0, comment:comment, idx:idx, result:result)
		{
			try
			{
				var bot = instr.Bot;
				var sign = tt.Sign();
				Idx index = idx ?? 0;
				bot.Debugging.Trace("Creating Trade (Index = {0})".Fmt(TradeIndex));

				// If the index == 0, add the fractional index amount
				if (index == 0)
				{
					var ticks_elapsed = bot.UtcNow.Ticks - instr.Latest.Timestamp;
					var ticks_per_candle = instr.TimeFrame.ToTicks();
					EntryIndex += Maths.Clamp((double)ticks_elapsed / ticks_per_candle, 0.0, 1.0);
					ExitIndex = EntryIndex;
				}

				// Set the trade entry price
				EP = ep ?? (index == 0
					? (QuoteCurrency)instr.CurrentPrice(sign) // Use the latest price
					: (QuoteCurrency)instr[index].Open + (sign > 0 ? instr.Symbol.Spread : 0));// Use the open price of the candle at 'index'

				// Choose a risk scaler
				risk = risk ?? 1.0;

				// Find the account currency value of the available risk
				var balance_to_risk = bot.Broker.BalanceToRisk * risk.Value;
				if (balance_to_risk == 0)
					throw new Exception("Insufficient available risk. Current Risk: {0}%, Maximum Risk: {1}%".Fmt(100*bot.Broker.TotalRiskFrac, 100.0*bot.Settings.MaxRiskFrac));

				// Require the SL to be at least 2 * the median candle size
				var volatility = instr.Symbol.QuoteToAcct(2 * instr.MCS * instr.Symbol.VolumeMin);
				if (balance_to_risk < volatility)
					throw new Exception("Insufficient available risk. Volatility: {0}, Balance To Risk: {1}".Fmt(volatility, balance_to_risk));

				// Get the instrument to recommend trade exit conditions
				var exit = instr.ChooseTradeExit(tt, EP, idx:index, risk:risk);
				TP     = tp != null ? tp.Value : exit.TP;
				SL     = sl != null ? sl.Value : exit.SL;
				Volume = sl != null ? instr.Bot.Broker.ChooseVolume(instr, sl.Value / risk.Value) : exit.Volume;
			}
			catch (Exception ex)
			{
				Error = ex;
			}
		}

		/// <summary>Construct a trade from an existing position</summary>
		public Trade(Instrument instr, Position pos, EResult result = EResult.Open)
			:this(instr, pos.TradeType, pos.Label, pos.EntryPrice, pos.StopLoss, pos.TakeProfit, pos.Volume, comment:pos.Comment)
		{
			CAlgoId = pos.Id;
			Result = result;

			EntryIndex = instr.IndexAt(pos.EntryTime   ) - instr.IdxFirst;
			ExitIndex  = instr.IndexAt(instr.Bot.UtcNow) - instr.IdxFirst;

			NetProfit   = pos.NetProfit;
			GrossProfit = pos.GrossProfit;

			// Buy at the bid price, then: loss = EP - ask, profit = ask - EP
			// Sell at the ask price, then: loss = bid - EP, profit = EP - bid
			MaxFavourableExcursion = 0.0;
			MaxAdverseExcursion    = 0.0;
			for (var i = (int)EntryIndex; i != (int)ExitIndex; ++i)
			{
				var hi = Instrument.Data.High[i];
				var lo = Instrument.Data.Low [i];
				if (Sign > 0)
				{
					MaxAdverseExcursion = Math.Max(MaxAdverseExcursion, EP - lo);
					MaxFavourableExcursion = Math.Max(MaxFavourableExcursion, hi - EP);
				}
				else
				{
					MaxAdverseExcursion = Math.Max(MaxAdverseExcursion, hi - EP);
					MaxFavourableExcursion = Math.Max(MaxFavourableExcursion, EP - lo);
				}
			}
		}

		/// <summary>Construct a trade from an existing pending order</summary>
		public Trade(Instrument instr, PendingOrder ord, EResult? result = null)
			:this(instr, ord.TradeType, ord.Label, ord.TargetPrice, ord.StopLoss, ord.TakeProfit, ord.Volume, comment:ord.Comment)
		{
			CAlgoId = ord.Id;
			Result = result ?? (ord.OrderType == PendingOrderType.Limit ? EResult.LimitOrder : EResult.StopEntryOrder);

			var now = instr.Bot.UtcNow;
			var fin = ord.ExpirationTime ?? (now + Instrument.TimeFrame.ToTimeSpan(num:10));
			EntryIndex = instr.IndexAt(now) - instr.IdxFirst;
			ExitIndex  = instr.IndexAt(fin) - instr.IdxFirst;
		}

		/// <summary>Incrementing trade index</summary>
		public int TradeIndex { get; private set; }
		private static int m_trade_index;

		/// <summary>A name for this trade</summary>
		public string Name
		{
			get { return "trade_{0}".Fmt(Id); }
		}

		/// <summary>The CAlgo Id for this trade</summary>
		public int Id
		{
			get { return CAlgoId ?? TradeIndex; }
		}

		/// <summary>Direction of trade</summary>
		public TradeType TradeType { get; private set; }

		/// <summary>The sign of this trade</summary>
		public int Sign
		{
			get { return TradeType.Sign(); }
		}

		/// <summary>The instrument traded</summary>
		public Instrument Instrument
		{
			get;
			private set;
		}

		/// <summary>The string identifier for the symbol</summary>
		public string SymbolCode
		{
			get { return Instrument.SymbolCode; }
		}

		/// <summary>The CAlgo Id for this trade (or null)</summary>
		public int? CAlgoId { get; set; }

		/// <summary>The entry point CAlgo index (can be a fractional index)</summary>
		public double EntryIndex { get; private set; }

		/// <summary>The exit point CAlgo index (can be a fractional index)</summary>
		public double ExitIndex { get; private set; }

		/// <summary>When the trade or pending order should/did expire. Null means not closed, or indefinite</summary>
		public DateTime? Expiration { get; set; }

		/// <summary>The entry price</summary>
		public QuoteCurrency EP { get; set; }

		/// <summary>The stop loss price (absolute, in quote currency)</summary>
		public QuoteCurrency? SL { get; set; }

		/// <summary>The take profit price (absolute, in quote currency)</summary>
		public QuoteCurrency? TP { get; set; }

		/// <summary>The stop loss in pips (relative, positive = on losing side of EP)</summary>
		public Pips? SL_pips
		{
			get { return SL != null ? Instrument.Symbol.QuoteToPips(TradeType.Sign() * (EP - SL.Value)) : (Pips?)null; }
			set { SL = value != null ? EP - TradeType.Sign() * Instrument.Symbol.PipsToQuote(value.Value) : (QuoteCurrency?)null; }
		}

		/// <summary>The take profit in pips (relative, positive = on wining side of EP)</summary>
		public Pips? TP_pips
		{
			get { return TP != null ? Instrument.Symbol.QuoteToPips(TradeType.Sign() * (TP.Value - EP)) : (Pips?)null; }
			set { TP = value != null ? EP + TradeType.Sign() * Instrument.Symbol.PipsToQuote(value.Value) : (QuoteCurrency?)null; }
		}

		/// <summary>The size of the trade</summary>
		public long Volume { get; set; }

		/// <summary>A string label for the trade</summary>
		public string Label { get; set; }

		/// <summary>A comment attached to the position</summary>
		public string Comment { get; set; }

		/// <summary>Null if this is a valid trade, otherwise the error that makes the trade invalid</summary>
		public Exception Error { get; private set; }

		/// <summary>The outcome of this trade so far</summary>
		public EResult Result { get; private set; }

		/// <summary>True if the trade has not been triggered yet</summary>
		public bool IsPending
		{
			get { return Result == EResult.LimitOrder || Result == EResult.StopEntryOrder; }
		}

		/// <summary>True if the trade has not closed</summary>
		public bool IsLive
		{
			get { return Result >= EResult.Open && Result < EResult.Closed; }
		}

		/// <summary>True if the trade has closed</summary>
		public bool IsClosed
		{
			get { return Result >= EResult.Closed; }
		}

		/// <summary>Reward to risk ratio of the SL/TP levels</summary>
		public double RtR
		{
			get
			{
				var sign = TradeType.Sign();
				if (SL == null) return 0.0;
				if (TP == null) return double.PositiveInfinity;
				return Maths.Div(sign * (TP.Value - EP), sign * (EP - SL.Value));
			}
		}

		/// <summary>Return the value of this trade at the current price level</summary>
		public QuoteCurrency ValueNow
		{
			get { return this.ValueAt(Instrument.LatestPrice); }
		}

		/// <summary>The current profit/loss of this trade including commissions</summary>
		public AcctCurrency NetProfit { get; private set; }

		/// <summary>The raw profit/loss of this trade based on price only</summary>
		public AcctCurrency GrossProfit { get; private set; }

		/// <summary>The best price seen over the duration of the trade (in quote currency, relative, positive = profit, not scaled by volume)</summary>
		public QuoteCurrency MaxFavourableExcursion { get; private set; }

		/// <summary>The worst price seen over the duration of the trade (in currency, relative, positive = loss, not scaled by volume)</summary>
		public QuoteCurrency MaxAdverseExcursion { get; private set; }

		/// <summary>The reward to risk seen over the duration of the trade</summary>
		public double PeakRtR
		{
			get
			{
				var mfe = Math.Max(0.0, MaxFavourableExcursion);
				var mae = Math.Max(0.0, MaxAdverseExcursion);
				return Maths.Div(mfe, mae);
			}
		}

		/// <summary>Find the normalised maximum adverse and favourable excursion of price from the entry point of this trade</summary>
		public RangeF[] MaxExcursionNormalised(int periods)
		{
			// The max adverse/favourable excursion of price
			// from the entry price for 'periods' candles after entry
			var excursion = new RangeF[periods];

			// Normalise by MCS at the time of entry
			var idx = Math.Max(0, EntryIndex-50) + Instrument.IdxFirst;
			var mcs = Instrument.MedianCandleSize(idx, idx + 50);

			var mae = 0.0; // maximum adverse excursion
			var mfe = 0.0; // maximum favourable excursion
			var start = (int)EntryIndex;
			for (int i = 0; i != periods; ++i)
			{
				if (start + i >= Instrument.Count) break;
				var hi = Instrument.Data.High[start + i];
				var lo = Instrument.Data.Low [start + i];
				if (Sign > 0)
				{
					mae = Math.Max(mae, EP - lo);
					mfe = Math.Max(mfe, hi - EP);
				}
				else
				{
					mae = Math.Max(mae, hi - EP);
					mfe = Math.Max(mfe, EP - lo);
				}
				excursion[i].Beg = mae / mcs;
				excursion[i].End = mfe / mcs;
			}

			return excursion;
		}

		///// <summary>Incorporate a candle into this trade</summary>
		//public void AddCandle(Candle candle)
		//{
		//	// Adding a candle "open"s an unknown trade
		//	if (Result == EResult.Unknown)
		//		Result = EResult.Open;

		//	// If the trade is pending, look for an entry trigger
		//	if (Result == EResult.LimitOrder)
		//	{

		//		if ((TradeType == TradeType.Buy  && candle.Close >= EP) ||
		//			(TradeType == TradeType.Sell && candle.Close <= EP))
		//		{
		//			Result = EResult.Open;
		//			EntryIndex = candle.Index;
		//		}
		//	}
		//	if (Result == EResult.StopEntryOrder)
		//	{
		//	}

		//	// Trade has closed out
		//	if (Result == EResult.HitSL || Result == EResult.HitTP)
		//		return;

		//	// Otherwise, if this is an open trade.
		//	if (Result == EResult.Open)
		//	{
		//		// Assume the worst for order of prices within a candle
		//		var prices = TradeType.Sign() > 0
		//			? new[] {candle.Open, candle.Low, candle.High, candle.Close }
		//			: new[] {candle.Open, candle.High, candle.Low, candle.Close };

		//		// Check each price value against the trade
		//		var sign = TradeType.Sign();
		//		foreach (var p in prices)
		//		{
		//			// If the trade is a buy, then it closes at the bid price.
		//			var price = sign > 0 ? (QuoteCurrency)p : p + Instrument.Spread;

		//			// SL hit
		//			if (SL != null && sign * (price - SL.Value) < 0)
		//			{
		//				NetProfit = sign * Instrument.Symbol.QuoteToAcct(SL.Value - EP) * Volume;
		//				MaxAdverseExcursion  = sign * (EP - SL.Value);
		//				Result = EResult.HitSL;
		//				Expiration = candle.TimestampUTC.DateTime; 
		//				break;
		//			}

		//			// TP hit
		//			if (TP != null && sign * (price - TP.Value) > 0)
		//			{
		//				NetProfit = sign * Instrument.Symbol.QuoteToAcct(TP.Value - EP) * Volume;
		//				MaxFavourableExcursion = sign * (TP.Value - EP);
		//				Result = EResult.HitTP;
		//				Expiration = candle.TimestampUTC.DateTime;
		//				break;
		//			}

		//			// Update the current profit
		//			NetProfit = sign * Instrument.Symbol.QuoteToAcct(price - EP) * Volume;

		//			// Record the peaks
		//			MaxFavourableExcursion = Math.Max(MaxFavourableExcursion, sign * (price - EP));
		//			MaxAdverseExcursion = Math.Max(MaxAdverseExcursion, sign * (EP - price));
		//		}

		//		ExitIndex = candle.Index;
		//	}
		//}

		/// <summary>Simulate the behaviour of this trade by adding a stream of price ticks</summary>
		public void Simulate(PriceTick price)
		{
			var sign = TradeType.Sign();

			// Adding a price tick 'open's an unknown trade
			if (Result == EResult.Unknown)
				Result = EResult.Open;

			// If the trade is pending, look for an entry trigger
			if ((Result == EResult.LimitOrder     && Math.Sign(EP - price.Price(sign)) == sign) ||
				(Result == EResult.StopEntryOrder && Math.Sign(price.Price(sign) - EP) == sign))
			{
				Result = EResult.Open;
				EntryIndex = price.Index;
			}

			// Trade has closed out
			if (Result == EResult.HitSL ||
				Result == EResult.HitTP)
				return;

			// Otherwise, if this is an open trade.
			if (Result == EResult.Open)
			{
				// SL hit
				if (SL != null && sign * (price.Price(-sign) - SL.Value) < 0)
				{
					NetProfit = sign * Instrument.Symbol.QuoteToAcct(SL.Value - EP) * Volume;
					MaxAdverseExcursion  = sign * (EP - SL.Value);
					Result = EResult.HitSL;
					Expiration = price.TimestampUTC.DateTime; 
				}

				// TP hit
				else if (TP != null && sign * (price.Price(-sign) - TP.Value) > 0)
				{
					NetProfit = sign * Instrument.Symbol.QuoteToAcct(TP.Value - EP) * Volume;
					MaxFavourableExcursion = sign * (TP.Value - EP);
					Result = EResult.HitTP;
					Expiration = price.TimestampUTC.DateTime;
				}

				// Update the current profit
				else
				{
					NetProfit = sign * Instrument.Symbol.QuoteToAcct(price.Price(-sign) - EP) * Volume;

					// Record the peaks
					MaxFavourableExcursion = Math.Max(MaxFavourableExcursion, sign * (price.Price(-sign) - EP));
					MaxAdverseExcursion    = Math.Max(MaxAdverseExcursion   , sign * (EP - price.Price(-sign)));
				}

				ExitIndex = price.Index;
			}
		}

		/// <summary>Simulate this trade being closed at the current price</summary>
		public void Close()
		{
			Result = EResult.Closed;
		}

		/// <summary>Debugger string</summary>
		private string DebuggerDisplay
		{
			get { return "idx=[{0:N3},{1:N3}] {2} Profit={3} RtR={4}".Fmt(EntryIndex, ExitIndex, TradeType, NetProfit, RtR); }
		}
	}
}

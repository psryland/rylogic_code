using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	[DebuggerDisplay("idx=[{EntryIndex},{ExitIndex}] {TradeType} RtR={RtR}")]
	public class Trade :ITrade
	{
		// Notes:
		//  A 'Trade' is the bot's representation of a position on a symbol, or simulated position on a symbol.
		//  Trades can be used to track the profit/loss of a position without having to actually hold the position.
		//  Trades are different to 'Position', 'PendingOrder', or 'Order'
		//  SL/TP are nullable because they are not required, even though it's generally a good idea to have them

		/// <summary>Create a trade with explicit values</summary>
		public Trade(Instrument instr, TradeType tt, string label, QuoteCurrency ep, QuoteCurrency? sl, QuoteCurrency? tp, long volume, NegIdx? neg_idx = null)
		{
			CAlgoId    = null;
			TradeIndex = m_trade_index++;
			TradeType  = tt;
			Instrument = instr;
			Result     = EResult.Unknown;
			EntryIndex = (double)((neg_idx ?? 0) - instr.IdxFirst);
			ExitIndex  = EntryIndex;
			Expiration = null;
			Label      = label ?? string.Empty;

			EP         = ep;
			SL         = sl;
			TP         = tp;
			Volume     = volume;

			PeakProfit = 0.0;
			PeakLoss   = 0.0;
		}

		/// <summary>
		/// Create a trade with automatic SL and TP levels set.
		/// SL/TP levels are set based on the current account balance (even if 'neg_idx' != 0)</summary>
		/// <param name="bot">The access to the CAlgo interface</param>
		/// <param name="instr">The instrument to be traded</param>
		/// <param name="tt">Whether to buy or sell</param>
		/// <param name="label">Optional. An identifying name for the trade</param>
		/// <param name="neg_idx">Optional. The instrument index of when the trade was created. (default is the current time)</param>
		/// <param name="ep">Optional. The price at which the trade was entered. (default is current ask/bid price)</param>
		/// <param name="sl">Optional. The stop loss (absolute) to use instead of automatically finding one</param>
		/// <param name="tp">Optional. The take profit (absolute) to use instead of automatically finding one</param>
		/// <param name="risk">Optional. Scaling factor for the amount to risk. (default is 1.0)</param>
		public Trade(Rylobot bot, Instrument instr, TradeType tt, string label = null, NegIdx? neg_idx = null, QuoteCurrency? ep = null, QuoteCurrency? sl = null, QuoteCurrency? tp = null, double? risk = null)
			:this(instr, tt, label, 0, 0, 0, 0, neg_idx)
		{
			try
			{
				var sign = tt.Sign();
				NegIdx index = neg_idx ?? 0;
				Debugging.Trace("Creating Trade (Index = {0})".Fmt(TradeIndex));

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
					? instr.CurrentPrice(sign) // Use the latest price
					: instr[index].Open + (sign > 0 ? instr.Symbol.Spread : 0));// Use the open price of the candle at 'index'

				// Choose a risk scaler
				risk = risk ?? 1.0;

				// Find the account currency value of the available risk
				var balance_to_risk = bot.Broker.BalanceToRisk * risk.Value;
				if (balance_to_risk == 0)
					throw new Exception("Insufficient available risk. Current Risk: {0}%, Maximum Risk: {1}%".Fmt(bot.Broker.TotalRiskPC, bot.Settings.MaxRiskPC));

				// Require the SL to be at least 2 * the median candle size
				var volatility = instr.Symbol.QuoteToAcct(2 * instr.MedianCS_50 * instr.Symbol.VolumeMin);
				if (balance_to_risk < volatility)
					throw new Exception("Insufficient available risk. Volatility: {0}, Balance To Risk: {1}".Fmt(volatility, balance_to_risk));

				// Choose an initial volume
				Volume = sl != null
					? instr.Bot.Broker.MaxVolume(instr, sl.Value / risk.Value)
					: instr.Symbol.VolumeMin;

				// Get the support and resistance levels for setting SL and TP
				var snr = new SnR(instr, index, EP);
				Debugging.Dump(snr);

				#region Set SL

				// Choose a stop loss value for the trade.
				// Look at recent history and set the stop loss beyond recent pecks/toughs
				if (sl == null)
				{
					QuoteCurrency sl_rel = 0.0;

					// Scan backwards looking for a peak in the stop loss direction.
					foreach (var candle in instr.CandleRange(index - bot.Settings.LookBackCount, index))
					{
						var limit = candle.WickLimit(-sign);
						var diff = EP - limit;
						if (Misc.Sign(diff) != sign) continue;
						if (Misc.Abs(diff) < sl_rel) continue;
						sl_rel = Misc.Abs(diff);
					}

					// In the case of strong trends this SL loss is too large, limit it to just past a strong SnR level
					var ema = bot.Indicators.ExponentialMovingAverage(instr.Data.Close, bot.Settings.SlowEMAPeriods);
					var trend = instr.MeasureTrend(ema.Result.FirstDerivative(index));
					if (Math.Abs(trend) > 0.5)
					{
						foreach (var lvl in snr.SnRLevels.Where(x => x.Strength > 0.5f))
						{
							var diff = (EP - lvl.Price) * 1.5f;
							if (Misc.Sign(diff) != sign) continue;
							if (Misc.Abs(diff) > sl_rel) continue;
							sl_rel = Misc.Abs(diff);
						}
					}

					// For short trades, add the spread to the SL
					sl_rel += (sign > 0 ? 0 : instr.Symbol.Spread);

					// Add on a bit as a safety buffer
					sl_rel *= 1.1f;

					// Adjust the volume so that the risk is within the acceptable range
					// If the risk is too high reduce the volume first, down to the VolumeMin
					// then reduce 'peak'. If the risk is low, increase volume to fit within 'risk'.

					// Find the account value risked at the current stop loss
					var sl_acct = instr.Symbol.QuoteToAcct(sl_rel);
					var optimal_volume = balance_to_risk / sl_acct;

					// If the risk is too high, reduce the stop loss
					if (optimal_volume < instr.Symbol.VolumeMin)
					{
						Volume = instr.Symbol.VolumeMin;
						sl_rel = instr.Symbol.AcctToQuote(balance_to_risk / Volume);
					}
					// Otherwise, round down to the nearest volume multiple
					else
					{
						Volume = instr.Symbol.NormalizeVolume(optimal_volume, RoundingMode.Down);
					}

					sl = EP - sign * sl_rel;
				}

				// Set the SL level
				SL = sl;

				#endregion

				#region Set TP

				// Determine a take profit value if none provided
				if (tp == null)
				{
					QuoteCurrency tp_rel = 0.0;

					// Look for a level on the profit side of the entry price
					var nearest = snr.Nearest(EP, sign);
					if (nearest != null)
					{
						tp_rel = Misc.Abs(nearest.Price - EP);
					}
					else
					{
						// If there are no levels, use a few typical candle sizes
						tp_rel = instr.MedianCS_50 * 3;
					}

					tp = EP + sign * tp_rel;
				}

				// Set the TP level
				TP = tp;

				#endregion
			}
			catch (Exception ex)
			{
				Error = ex;
			}
		}

		/// <summary>Construct a trade from an existing position</summary>
		public Trade(Instrument instr, Position pos)
			:this(instr, pos.TradeType, pos.Label, pos.EntryPrice, pos.StopLoss, pos.TakeProfit, pos.Volume)
		{
			CAlgoId = pos.Id;
			Result = EResult.Open;

			EntryIndex = instr.FractionalIndexAt(pos.EntryTime) - (double)instr.IdxFirst;
			ExitIndex  = instr.FractionalIndexAt(instr.Bot.UtcNow) - (double)instr.IdxFirst;

			// Buy at the bid price, then: loss = EP - ask, profit = ask - EP
			// Sell at the ask price, then: loss = bid - EP, profit = EP - bid
			PeakProfit = Math.Max(0.0, (double)(TradeType == TradeType.Buy ? (instr.LatestPrice.Ask - EP) : (EP - instr.LatestPrice.Bid)));
			PeakLoss   = Math.Min(0.0, (double)(TradeType == TradeType.Buy ? (EP - instr.LatestPrice.Ask) : (instr.LatestPrice.Bid - EP)));
		}

		/// <summary>Construct a trade from an existing pending order</summary>
		public Trade(Instrument instr, PendingOrder ord)
			:this(instr, ord.TradeType, ord.Label, ord.TargetPrice, ord.StopLoss, ord.TakeProfit, ord.Volume)
		{
			CAlgoId = ord.Id;
			Result = EResult.Pending;

			var now = instr.Bot.UtcNow;
			var fin = ord.ExpirationTime ?? (now + Instrument.TimeFrame.ToTimeSpan(num:10));
			EntryIndex = instr.FractionalIndexAt(now) - (double)instr.IdxFirst;
			ExitIndex  = instr.FractionalIndexAt(fin) - (double)instr.IdxFirst;
		}

		/// <summary>Incrementing trade index</summary>
		public int TradeIndex { get; private set; }
		private static int m_trade_index;

		/// <summary>The CAlgo Id for this trade</summary>
		public int Id { get { return CAlgoId ?? TradeIndex; } }

		/// <summary>Direction of trade</summary>
		public TradeType TradeType { get; private set; }

		/// <summary>The instrument traded</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>The string identifier for the symbol</summary>
		public string SymbolCode { get { return Instrument.SymbolCode; } }

		/// <summary>The CAlgo Id for this trade (or null)</summary>
		public int? CAlgoId { get; set; }

		/// <summary>The entry point CAlgo index (can be a fractional index)</summary>
		public double EntryIndex { get; private set; }

		/// <summary>The exit point CAlgo index (can be a fractional index)</summary>
		public double ExitIndex { get; private set; }

		/// <summary>When the trade or pending order should/did expire. Null means not closed, or indefinite</summary>
		public DateTime? Expiration { get; set; }

		/// <summary>The entry price</summary>
		public QuoteCurrency EP { get; private set; }

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

		/// <summary>Null if this is a valid trade, otherwise the error that makes the trade invalid</summary>
		public Exception Error { get; private set; }

		/// <summary>The outcome of this trade so far</summary>
		public EResult Result { get; private set; }
		public enum EResult { Unknown, Pending, Open, HitSL, HitTP };

		/// <summary>Reward to risk ratio of the SL/TP levels</summary>
		public double RtR
		{
			get
			{
				var sign = TradeType.Sign();
				if (SL == null) return 0.0;
				if (TP == null) return double.PositiveInfinity;
				return Misc.Div(sign * (TP.Value - EP), sign * (EP - SL.Value));
			}
		}

		/// <summary>Return the value of this trade if it was to be closed at the given price tick</summary>
		public QuoteCurrency ValueAt(PriceTick price, bool consider_sl = true, bool consider_tp = true)
		{
			// Closing a Buy means selling to the highest *bid*er
			var p = TradeType == TradeType.Buy ? price.Bid : price.Ask;
			return new Order(this, true).ValueAt(p, consider_sl, consider_tp);
		}

		/// <summary>Return the value of this trade at the current price level</summary>
		public QuoteCurrency ValueNow()
		{
			return ValueAt(Instrument.LatestPrice);
		}

		/// <summary>The highest profit seen over the duration of the trade (in quote currency, relative, positive = profit)</summary>
		public QuoteCurrency PeakProfit { get; private set; }

		/// <summary>The highest loss seen over the duration of the trade (in currency, relative, positive = loss)</summary>
		public QuoteCurrency PeakLoss { get; private set; }

		/// <summary>The highest reward to risk seen over the duration of the trade</summary>
		public double PeakRtR
		{
			get { return Misc.Div(Misc.Max(0.0, PeakProfit), Misc.Max(Instrument.PipSize, PeakLoss)); }
		}

		/// <summary>Incorporate a candle into this trade</summary>
		public void AddCandle(Candle candle, NegIdx index)
		{
			// Adding a candle "open"s an unknown candle
			if (Result == EResult.Unknown)
				Result = EResult.Open;

			// If the trade is pending, look of an entry trigger
			if (Result == EResult.Pending)
			{
				if ((TradeType == TradeType.Buy  && candle.Close >= EP) ||
					(TradeType == TradeType.Sell && candle.Close <= EP))
				{
					Result = EResult.Open;
					EntryIndex = (double)(index - Instrument.IdxFirst);
				}
			}

			// Trade has closed out
			if (Result == EResult.HitSL || Result == EResult.HitTP)
				return;

			// Otherwise, if this is an open trade.
			if (Result == EResult.Open)
			{
				// Assume the worst for order of prices within a candle
				var prices = TradeType.Sign() > 0
					? new[] {candle.Open, candle.Low, candle.High, candle.Close }
					: new[] {candle.Open, candle.High, candle.Low, candle.Close };

				var sign = TradeType.Sign();
				foreach (var p in prices)
				{
					// SL hit
					if (SL != null && sign * (SL.Value - p) > 0)
					{
						PeakLoss = sign * (EP - SL.Value);
						Result = EResult.HitSL;
						Expiration = candle.TimestampUTC.DateTime; 
						break;
					}

					// TP hit
					if (TP != null && sign * (p - TP.Value) > 0)
					{
						PeakProfit = sign * (TP.Value - EP);
						Result = EResult.HitTP;
						Expiration = candle.TimestampUTC.DateTime;
						break;
					}

					// Record the peaks
					PeakProfit = Misc.Max(PeakProfit, sign * (p - EP));
					PeakLoss   = Misc.Max(PeakLoss  , sign * (EP - p));
				}

				ExitIndex = (double)(index - Instrument.IdxFirst);
			}
		}
	}
}

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
	public class Trade
	{
		/// <summary>Create a trade with explicit values</summary>
		public Trade(Instrument instr, TradeType tt, string label, QuoteCurrency ep, QuoteCurrency sl, QuoteCurrency tp, long volume, NegIdx? neg_idx = null)
		{
			Id         = m_trade_id++;
			TradeType  = tt;
			Instrument = instr;
			Result     = EResult.Open;
			EntryIndex = (double)((neg_idx ?? 0) - instr.FirstIdx);
			ExitIndex  = EntryIndex;
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
		public Trade(Rylobot bot, Instrument instr, TradeType tt, string label = null, NegIdx? neg_idx = null, double? ep = null, double? risk = null, RangeF? rtr_range = null)
			:this(instr, tt, label, 0, 0, 0, 0, neg_idx)
		{
			var sign = tt.Sign();
			NegIdx index = neg_idx ?? 0;

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

			// Find the account currency value of the available risk
			var balance_to_risk = bot.Broker.BalanceToRisk * (risk ?? 1.0);
			if (balance_to_risk == 0)
				throw new Exception("Insufficient available risk. Current Risk: {0}%, Maximum Risk: {1}%".Fmt(bot.Broker.TotalRiskPC, bot.Settings.MaxRiskPC));

			#region Set SL

			QuoteCurrency sl = 0.0;

			// Scan backwards looking for a peak in the stop loss direction.
			foreach (var candle in instr.CandleRange(index - bot.Settings.LookBackCount, index))
			{
				var limit = candle.WickLimit(-sign);
				var diff = (double)(EP - limit);
				if (Maths.Sign(diff) != sign) continue;
				if (Math.Abs(diff) < sl) continue;
				sl = Math.Abs(diff);
			}

			// For short trades, add the spread to the SL
			sl += (sign > 0 ? 0 : instr.Symbol.Spread);

			// Add on a bit as a safety buffer
			sl *= 1.1f;

			// Adjust the volume so that the risk is within the acceptable range
			// If the risk is too high reduce the volume first, down to the VolumeMin
			// then reduce 'peak'. If the risk is low, increase volume to fit within 'risk'.

			// Find the account value risked at the current stop loss
			var sl_acct = instr.Symbol.QuoteToAcct(sl);
			var optimal_volume = balance_to_risk / sl_acct;

			// If the risk is too high, reduce the stop loss
			if (optimal_volume < instr.Symbol.VolumeMin)
			{
				Volume = instr.Symbol.VolumeMin;
				sl = instr.Symbol.AcctToQuote(balance_to_risk / Volume);
			}
			// Otherwise, round down to the nearest volume multiple
			else
			{
				Volume = instr.Symbol.NormalizeVolume(optimal_volume, RoundingMode.Down);
			}

			// Set the SL level
			SL = EP - sign * sl;

			#endregion

			#region Set TP

			QuoteCurrency tp = sl;
			var rtr = rtr_range ?? bot.Settings.RewardToRisk;

			// Get the support and resistance levels
			var snr = new SnR(instr, index - bot.Settings.LookBackCount, index);

			// Select SnR levels that are between the min and max reward to risk ratios.
			// Set the TP at the nearest SnR level above the minimum RtR ratio
			// Scale the TP to 95% to put it on the near-side of the SnR level
			var lvl_min = EP + sign * sl * rtr.Begin;
			var lvl_max = EP + sign * sl * rtr.End;
			var lvl = EP + sign * tp;
			var nearest = snr.Nearest(lvl, sign, new RangeF((double)lvl_min, (double)lvl_max));
			if (nearest != null)
				tp = sign * 0.90 * (nearest.Price - EP);

			// Set the TP level
			TP = EP + sign * tp;

			#endregion
		}

		/// <summary>Construct a trade from an existing position</summary>
		public Trade(Instrument instr, Position pos)
			:this(instr, pos.TradeType, pos.Label, pos.EntryPrice, pos.StopLoss ?? pos.EntryPrice, pos.TakeProfit ?? pos.EntryPrice, pos.Volume)
		{
			EntryIndex = instr.FractionalIndexAt(pos.EntryTime) - (double)instr.FirstIdx;
			ExitIndex  = instr.FractionalIndexAt(instr.Bot.UtcNow) - (double)instr.FirstIdx;

			PeakProfit = TradeType.Sign() * (TP - EP);
			PeakLoss   = TradeType.Sign() * (EP - SL);
		}

		/// <summary>Direction of trade</summary>
		public TradeType TradeType { get; private set; }

		/// <summary>The instrument traded</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>Incrementing trade index</summary>
		public int Id { get; private set; }
		private static int m_trade_id;

		/// <summary>The entry point CAlgo index (can be a fractional index)</summary>
		public double EntryIndex { get; private set; }

		/// <summary>The exit point CAlgo index (can be a fractional index)</summary>
		public double ExitIndex { get; private set; }

		/// <summary>The entry price</summary>
		public QuoteCurrency EP { get; private set; }

		/// <summary>The stop loss price (absolute, in quote currency)</summary>
		public QuoteCurrency SL { get; set; }

		/// <summary>The take profit price (absolute, in quote currency)</summary>
		public QuoteCurrency TP { get; set; }

		/// <summary>The stop loss in pips (relative, positive = on losing side of EP)</summary>
		public Pips SL_pips
		{
			get { return Instrument.Symbol.QuoteToPips(TradeType.Sign() * (EP - SL)); }
			set { SL = EP - TradeType.Sign() * Instrument.Symbol.PipsToQuote(value); }
		}

		/// <summary>The take profit in pips (relative, positive = on wining side of EP)</summary>
		public Pips TP_pips
		{
			get { return Instrument.Symbol.QuoteToPips(TradeType.Sign() * (TP - EP)); }
			set { TP = EP + TradeType.Sign() * Instrument.Symbol.PipsToQuote(value); }
		}

		/// <summary>The size of the trade</summary>
		public long Volume { get; set; }

		/// <summary>A string label for the trade</summary>
		public string Label { get; set; }

		/// <summary>The outcome of this trade so far</summary>
		public EResult Result { get; private set; }
		public enum EResult { Unknown, Open, HitSL, HitTP };

		/// <summary>Reward to risk ratio of the SL/TP levels</summary>
		public double RtR
		{
			get
			{
				var sign = TradeType.Sign();
				return Misc.Div(sign * (TP - EP), sign * (EP - SL));
			}
		}

		/// <summary>The highest profit seen over the duration of the trade (in currency, relative, positive = profit)</summary>
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
			// Trade has closed out
			if (Result != EResult.Open)
				return;

			// Assume the worst for order of prices within a candle
			var prices = TradeType.Sign() > 0
				? new[] {candle.Open, candle.Low, candle.High, candle.Close }
				: new[] {candle.Open, candle.High, candle.Low, candle.Close };

			var sign = TradeType.Sign();
			foreach (var p in prices)
			{
				// SL hit
				if (sign * (SL - p) > 0)
				{
					PeakLoss = sign * (EP - SL);
					Result = EResult.HitSL;
					break;
				}

				// TP hit
				if (sign * (p - TP) > 0)
				{
					PeakProfit = sign * (TP - EP);
					Result = EResult.HitTP;
					break;
				}

				// Record the peaks
				PeakProfit = Misc.Max(PeakProfit, sign * (p - EP));
				PeakLoss   = Misc.Max(PeakLoss  , sign * (EP - p));
			}

			ExitIndex = (double)(index - Instrument.FirstIdx);
		}
	}
}

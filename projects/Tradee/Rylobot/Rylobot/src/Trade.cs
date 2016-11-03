using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	[DebuggerDisplay("idx=[{EntryIndex},{ExitIndex}] {Type} RtR={RtR}")]
	public class Trade
	{
		/// <summary>
		/// Create a trade with automatic SL and TP levels set.
		/// SL/TP levels are set based on the current account balance (even if index != 0)</summary>
		public Trade(Rylobot bot, Instrument instr, TradeType tt, string label = null, NegIdx? neg_idx = null, double? min_rtr = null, double? price = null)
		{
			var sign = tt.Sign();
			NegIdx index = neg_idx ?? 0;

			Type       = tt;
			Bot        = bot;
			Instrument = instr;
			Id         = m_trade_id++;
			Result     = EResult.Open;
			EntryIndex = index - instr.FirstIdx;
			ExitIndex  = EntryIndex;
			Label      = label ?? string.Empty;

			// Set the trade entry price
			EP = price ?? (index == 0
				? instr.CurrentPrice(sign) // Use the latest price
				: instr[index].Open + (sign > 0 ? instr.Symbol.Spread : 0));// Use the open price of the candle at 'index'

			#region Set SL

			// Find the percentage of balance available to risk
			var max_risk_pc = bot.Settings.MaxRiskPC;
			var available_risk_pc = max_risk_pc - bot.Broker.TotalRiskPC;
			if (available_risk_pc <= 0)
				throw new Exception("Insufficient available risk. Current Risk: {0}%, Maximum Risk: {1}%".Fmt(bot.Broker.TotalRiskPC, max_risk_pc));

			// Find the account currency value of the available risk
			var balance_to_risk = bot.Broker.Balance * available_risk_pc * 0.01;

			// Scan backwards looking for a peak in the stop loss direction.
			var sl = 0.0;
			foreach (var candle in instr.CandleRange(index - bot.Settings.LookBackCount, index))
			{
				var limit = candle.WickLimit(-sign);
				var diff = EP - limit;
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
			var sl_acct = instr.Symbol.QuotePriceToAcct(sl);
			var optimal_volume = balance_to_risk / sl_acct;

			// If the risk is too high, reduce the stop loss
			if (optimal_volume < instr.Symbol.VolumeMin)
			{
				Volume = instr.Symbol.VolumeMin;
				sl = instr.Symbol.AcctToQuotePrice(balance_to_risk / Volume);
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

			// Get the support and resistance levels
			var snr = new SnR(instr, index - bot.Settings.LookBackCount, index);

			var tp = sl;

			// Select SnR levels that are between the min and max reward to risk ratios.
			// Set the TP at the nearest SnR level above the minimum RtR ratio
			var lvl_min = EP + sign * sl * (min_rtr ?? bot.Settings.MinRewardToRisk);
			var lvl_max = EP + sign * sl * (           bot.Settings.MaxRewardToRisk);
			var levels = snr.SnRLevels.Where(x => sign*(x.Price - lvl_min) > 0 && sign*(lvl_max - x.Price) > 0);
			if (levels.Any())
			{
				var lvl = EP + sign * tp;
				var nearest = levels.MinBy(x => Math.Abs(x.Price - lvl));
				tp = sign * (nearest.Price - EP);
			}

			// Set the TP level
			TP = EP + sign * tp;

			#endregion
		}

		/// <summary>Direction of trade</summary>
		public TradeType Type { get; private set; }

		/// <summary>App logic</summary>
		public Rylobot Bot { get; private set; }

		/// <summary>The instrument traded</summary>
		public Instrument Instrument { get; private set; }

		/// <summary>Incrementing trade index</summary>
		public int Id { get; private set; }
		private static int m_trade_id;

		/// <summary>The entry point CAlgo index</summary>
		public int EntryIndex { get; private set; }

		/// <summary>The exit point CAlgo index</summary>
		public int ExitIndex { get; private set; }

		/// <summary>The entry price</summary>
		public double EP { get; private set; }

		/// <summary>The stop loss price (absolute, in quote currency)</summary>
		public double SL { get; set; }

		/// <summary>The take profit price</summary>
		public double TP { get; set; }

		/// <summary>The stop loss in pips (relative, positive = on losing side of EP)</summary>
		public double SL_pips
		{
			get { return Instrument.Symbol.QuotePriceToPips(Type.Sign() * (EP - SL)); }
			set { SL = EP - Type.Sign() * Instrument.Symbol.PipsToQuotePrice(value); }
		}

		/// <summary>The take profit in pips (relative, positive = on wining side of EP)</summary>
		public double TP_pips
		{
			get { return Instrument.Symbol.QuotePriceToPips(Type.Sign() * (TP - EP)); }
			set { TP = EP + Type.Sign() * Instrument.Symbol.PipsToQuotePrice(value); }
		}

		/// <summary>The size of the trade</summary>
		public long Volume { get; set; }

		/// <summary>A string label for the trade</summary>
		public string Label { get; set; }

		/// <summary>The outcome of this trade so far</summary>
		public EResult Result { get; private set; }
		public enum EResult { Open, HitSL, HitTP };

		/// <summary>Reward to risk ratio of the SL/TP levels</summary>
		public double RtR
		{
			get
			{
				var sign = Type.Sign();
				return Maths.Div(sign * (TP - EP), sign * (EP - SL));
			}
		}

		/// <summary>The highest profit seen over the duration of the trade (in currency, positive = profit)</summary>
		public double PeakProfit { get; private set; }

		/// <summary>The highest loss seen over the duration of the trade (in currency, positive = loss)</summary>
		public double PeakLoss { get; private set; }

		/// <summary>The highest reward to risk seen over the duration of the trade</summary>
		public double PeakRtR
		{
			get { return Maths.Div(Math.Max(0, PeakProfit), Math.Max(Instrument.PipSize, PeakLoss)); }
		}

		/// <summary>Incorporate a candle into this trade</summary>
		public void AddCandle(Candle candle, NegIdx index)
		{
			// Trade has closed out
			if (Result != EResult.Open)
				return;

			// Assume the worst for order of prices within a candle
			var prices = Type.Sign() > 0
				? new[] {candle.Open, candle.Low, candle.High, candle.Close }
				: new[] {candle.Open, candle.High, candle.Low, candle.Close };

			var sign = Type.Sign();
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
				PeakProfit = Math.Max(PeakProfit, sign * (p - EP));
				PeakLoss   = Math.Max(PeakLoss  , sign * (EP - p));
			}

			ExitIndex = index - Instrument.FirstIdx;
		}
	}
}

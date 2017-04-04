using System;
using System.Diagnostics;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary>Position manager base class</summary>
	public abstract class PositionManager :IDisposable
	{
		protected PositionManager(Rylobot bot, Position pos)
		{
			Bot                = bot;
			Position           = pos;
			MinRtR             = 1.0;
			MinRtRCrossedIndex = null;
			Closed             = false;
		}
		public virtual void Dispose()
		{
			Position = null;
			Bot = null;
		}

		/// <summary>The strategy that created the position and position manager</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.PositionClosed -= HandlePositionClosed;
				} 
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.PositionClosed += HandlePositionClosed;
				}
			}
		}
		private Rylobot m_bot;

		/// <summary>The broker</summary>
		public Broker Broker
		{
			[DebuggerStepThrough] get { return Bot.Broker; }
		}

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return Bot.Instrument; }
		}

		/// <summary>The position to be managed</summary>
		public virtual Position Position
		{
			[DebuggerStepThrough] get { return m_position; }
			private set
			{
				if (m_position == value) return;
				if (m_position != null)
				{
					Profit                 = 0;
					LastProfit             = 0;
					PeakProfit             = 0;
					PeakProfitIndex        = 0;
					LastProfitableIndex    = 0;
					CandlesSincePeak       = 0;
					CandlesSinceProfitable = 0;
					TicksSincePeak         = 0;
					TicksSinceProfitable   = 0;
					Profitable             = true;
					NewPeak                = true;
					IsGain                 = false;
				}
				m_position = value;
				if (m_position != null)
				{

					EntryIndex             = Instrument.IndexAt(m_position.EntryTime) - Instrument.IdxFirst;
					Profit                 = m_position.NetProfit;
					LastProfit             = m_position.NetProfit;
					PeakProfit             = m_position.NetProfit;
					PeakProfitIndex        = EntryIndex;
					LastProfitableIndex    = EntryIndex;
					CandlesSincePeak       = 0;
					CandlesSinceProfitable = 0;
					TicksSincePeak         = 0;
					TicksSinceProfitable   = 0;
					Profitable             = true;
					NewPeak                = true;
					IsGain                 = false;
				}
			}
		}
		private Position m_position;

		/// <summary>The (CAlgo) index of when 'Position' was opened</summary>
		public double EntryIndex { get; private set; }

		/// <summary>The current profit value (basically NetProfit)</summary>
		public AcctCurrency Profit { get; private set; }

		/// <summary>The profit value last time step was called</summary>
		public AcctCurrency LastProfit { get; private set; }

		/// <summary>The maximum profit reached with the current trade</summary>
		public AcctCurrency PeakProfit { get; private set; }

		/// <summary>Where the peak profit was detected</summary>
		public double PeakProfitIndex { get; private set; }

		/// <summary>Where the position was last profitable</summary>
		public double LastProfitableIndex { get; private set; }

		/// <summary>The number of candles since a new peak was made</summary>
		public int CandlesSincePeak { get; private set; }

		/// <summary>The number of candles since the trade was profitable</summary>
		public int CandlesSinceProfitable { get; private set; }

		/// <summary>The number of price ticks since a new peak was made</summary>
		public int TicksSincePeak { get; private set; }

		/// <summary>The number of price ticks since the trade was profitable</summary>
		public int TicksSinceProfitable { get; private set; }

		/// <summary>True if the position is currently profitable</summary>
		public bool Profitable { get; private set; }

		/// <summary>True if the current step is a new profit peak</summary>
		public bool NewPeak { get; private set; }

		/// <summary>True if the current step is a gain in profit</summary>
		public bool IsGain { get; private set; }

		/// <summary>The minimum reward to risk ratio for the position (i.e. don't take less profit than this)</summary>
		public double MinRtR { get; set; }

		/// <summary>The (CAlgo) index that profit exceeded the MinRtR (or null)</summary>
		public int? MinRtRCrossedIndex { get; private set; }

		/// <summary>True once the position has closed</summary>
		public bool Closed { get; private set; }

		/// <summary>Output the current state</summary>
		public virtual void Dump()
		{
			if (Bot != null)
				Bot.Dump();
		}

		/// <summary>Manage an open trade. This should be called on each tick</summary>
		public virtual void Step()
		{
			if (Closed)
				return;

			LastProfit = Profit;
			Profit     = Position.NetProfit;
			IsGain     = Profit > LastProfit;

			// Track profitable
			Profitable = Position.NetProfit >= 0;
			LastProfitableIndex = Profitable ? Instrument.Count : LastProfitableIndex;

			// Profit peaks
			NewPeak         = Position.NetProfit >= PeakProfit;
			PeakProfit      = NewPeak ? Position.NetProfit : PeakProfit;
			PeakProfitIndex = NewPeak ? Instrument.Count : PeakProfitIndex;

			// How long since a new profit peak was made
			CandlesSincePeak = NewPeak ? 0 : Instrument.Count - (int)PeakProfitIndex;
			TicksSincePeak   = NewPeak ? 0 : TicksSincePeak + 1;

			// How long since the trade was profitable at all
			CandlesSinceProfitable = Profitable ? 0 : Instrument.Count - (int)LastProfitableIndex;
			TicksSinceProfitable   = Profitable ? 0 : TicksSinceProfitable + 1;

			// Check whether 'MinRtR' has been exceeded
			if (MinRtRCrossedIndex == null && MinRtRExceeded)
				MinRtRCrossedIndex = Instrument.Count - 1;

			StepCore();
		}
		protected abstract void StepCore();

		/// <summary>True if the current value of the position exceeds the MinRtR value</summary>
		protected bool MinRtRExceeded
		{
			get
			{
				// It's been exceeded in the past
				if (MinRtRCrossedIndex != null)
					return true;

				// Get the SL to determine the minimum TP
				var sl_rel = Position.StopLossRel();
				if (sl_rel <= 0)
					return true;

				// Compare the current value to the SL
				var rel = Position.ValueAt(Instrument.LatestPrice);
				var tp_rel = MinRtR * sl_rel;
				return rel >= tp_rel;
			}
		}

		/// <summary>Close half the position and move the SL to break even</summary>
		protected void CloseHalf()
		{
			var vol = Bot.Symbol.NormalizeVolume(Position.Volume / 2);
			var sl = Position.EntryPrice + Position.Sign() * Instrument.Spread * 2.0;

			// If the position was at the minimum volume, just close it
			if (Position.Volume == vol)
				Broker.ClosePosition(Position);
			else
				Broker.ModifyOrder(Instrument, Position, sl:sl, vol:vol);
		}

		/// <summary>Handle position closed</summary>
		private void HandlePositionClosed(object sender, PositionClosedEventArgs args)
		{
			if (args.Position.Id != Position.Id) return;
			Closed = true;
		}
	}

	/// <summary>A position manager that closes a position when it looks like it's done</summary>
	public class PositionManagerNervious :PositionManager
	{
		/// <summary>State machine states</summary>
		public enum EState
		{
			TradeLooksGood,
			Scalp,
			TrailingSL,
			NoRecentProfitPeaks,
			LookForExitAfterNextPeak,
			CloseAtNextNonPeak,
			CloseAtNextProfitDrop,
			BailIfProfitable,
			PositionClosed,
		}

		/// <summary>Manage an active position</summary>
		public PositionManagerNervious(Rylobot bot, Position pos)
			:base(bot, pos)
		{
			ColdFeetCount = 10;
			State = EState.TradeLooksGood;
		}

		/// <summary>How many candles without a new profit peak before we start getting nervous</summary>
		public int ColdFeetCount { get; set; }

		/// <summary>The current state of managing a open position</summary>
		public EState State
		{
			[DebuggerStepThrough] get { return m_state; }
			private set
			{
				if (m_state == value) return;
				m_state = value;
				StateChanged.Raise(this);
			}
		}
		private EState m_state;

		/// <summary>Raised when the state changes</summary>
		public event EventHandler StateChanged;

		/// <summary>The profit limit at which we want to take profits as soon as possible</summary>
		public AcctCurrency ScalpThreshold
		{
			get { return Broker.Balance * 0.01; }
		}

		/// <summary>The profit limit at which we move the SL to break even</summary>
		public AcctCurrency BreakEvenThreshold
		{
			get { return Broker.Balance * 0.005; }
		}

		/// <summary>Manage an open trade. This should be called on each tick</summary>
		protected override void StepCore()
		{
			// Move SL to break even when profit > BreakEvenThreshold (adjusted for Spread)
			var adjusted_profit = Position.NetProfit - Instrument.Symbol.QuoteToAcct(Instrument.Spread * Position.Volume);
			if (adjusted_profit > BreakEvenThreshold && Position.StopLossRel() > 0)
			{
				Bot.Debugging.Trace("Moving SL to break even - Profit ${0} > Threshold ${1}".Fmt(Position.NetProfit, BreakEvenThreshold));

				var trade = new Trade(Instrument, Position) { SL = Position.EntryPrice + Position.Sign() * 2.0 * Instrument.Spread };
				Broker.ModifyOrder(Position, trade);
			}

			// What about candle follow mode:
			// If the price is heading for the TP and the last tops/bottoms of the last few candles
			// have been steadily increasing/decreasing. Move the SL to just above/below the previous candle

			// A state machine for managing the position
			switch (State)
			{
			// In this state, the trade looks good. Keep it open.
			case EState.TradeLooksGood:
				#region
				{
					// Enter scalp mode when profit > scalp threshold
					if (Profit > ScalpThreshold)
					{
						Bot.Debugging.Trace("Entering scalp mode - Profit ${0} > Threshold ${1}".Fmt(Profit, ScalpThreshold));
						State = EState.Scalp;
					}
					else if (CandlesSincePeak >= ColdFeetCount)
					{
						// It's been a while since a profit peak was set...
						Bot.Debugging.Trace("Getting nervous... no peaks for a while");
						State = EState.LookForExitAfterNextPeak;
					}
					break;
				}
				#endregion
			case EState.Scalp:
				{
					// In this mode we've made a decent profit.
					var close = false;
					for (;;)
					{
						//// Close if the profit drops to fraction of the peak
						//const double ScalpFraction = 0.5;
						//if (Position.NetProfit < ScalpFraction * PeakProfit)
						//{
						//	close = true;
						//	Debugging.Trace("Position closed - profit dropped by {0} - ${1}".Fmt(ScalpFraction, Position.NetProfit));
						//}

						// Close if the closed candle is an indecision candle.
						if (Instrument.NewCandle &&
							Instrument[-1].Type(Instrument.MCS).IsIndecision())
						{
							Bot.Debugging.Trace("Closing position - indecision candle");
							close = true;
							break;
						}

						// Close if the peak of the candle just closed is not better than the candle before's peak i.e. Not higher lows, or lower highs
						if (Instrument.NewCandle &&
							Position.EntryTime < Instrument[-1].TimestampUTC &&
							Instrument.SequentialPeaks(Position.Sign(), 0) < 1)
						{
							Bot.Debugging.Trace("Closing position - peaks oppose trend direction");
							close = true;
							break;
						}

						// Close if the candle closes worse then the close of the -2 candle
						if (Instrument.NewCandle &&
							Position.EntryTime < Instrument[-1].TimestampUTC &&
							Math.Sign(Instrument[-1].Close - Instrument[-2].Close) != Position.Sign())
						{
							Bot.Debugging.Trace("Closing position - candle close was worse that close of -2 candle");
							close = true;
							break;
						}

						// Close if the profit is huge
						if (Position.NetProfit > 2.0 * ScalpThreshold && TicksSincePeak > 10)
						{
							Bot.Debugging.Trace("Closing position - huge spike detected!");
							close = true;
							break;
						}

						// Don't close.
						break;
					}
					if (close)
					{
						Broker.ClosePosition(Position);
						State = EState.PositionClosed;
					}
					break;
				}
			case EState.TrailingSL:
				{
					// In this mode, we move the SL up to just above/below a recently closed candle
					if (Instrument.NewCandle)
					{
						var sign = Position.TradeType.Sign();
						var limit = Instrument.CurrentPrice(-sign);
						foreach (var c in Instrument.CandleRange(-3,0))
							limit = sign > 0 ? Math.Min(limit, c.Low) : Math.Max(limit, c.High);

						// Move the SL if in the correct direction
						if (sign > 0 && limit > Position.StopLoss ||
							sign < 0 && limit < Position.StopLoss)
						{
							var trade = new Trade(Instrument, Position) { SL = limit };
							Broker.ModifyOrder(Position, trade);
						}
					}
					break;
				}
			case EState.LookForExitAfterNextPeak:
				{
					// In this state, the profit hit a peak a while ago,
					// and we're waiting for it to come back to that peak
					// level so we can bail out.
					if (NewPeak)
					{
						State = EState.CloseAtNextNonPeak;
						Bot.Debugging.Trace("Closing at next non peak");
					}
					else if (CandlesSincePeak >= 2 * ColdFeetCount)
					{
						State = EState.BailIfProfitable;
						Bot.Debugging.Trace("Closing when profitable");
					}
					break;
				}
			case EState.CloseAtNextNonPeak:
				{
					// In this state we're looking to exit the trade the
					// next time the profit peak is not set
					if (!NewPeak)
					{
						Broker.ClosePosition(Position);
						State = EState.PositionClosed;
						Bot.Debugging.Trace("Position closed - non-peak");
					}
					break;
				}
			case EState.CloseAtNextProfitDrop:
				{
					// In this state we just want out with the minimum of loss.
					// Bail as soon as the profit decreases
					if (!IsGain)
					{
						Broker.ClosePosition(Position);
						State = EState.PositionClosed;
						Bot.Debugging.Trace("Position closed - profit dropped");
					}
					break;
				}
			case EState.BailIfProfitable:
				{
					// In this state, it's been too long since the peak was last
					// hit, bail out if profitable
					if (NewPeak)
					{
						State = EState.CloseAtNextNonPeak;
					}
					else if (Position.NetProfit > 0)
					{
						Broker.ClosePosition(Position);
						State = EState.PositionClosed;
					}
					else if (CandlesSinceProfitable > 4 * ColdFeetCount)
					{
						State = EState.CloseAtNextProfitDrop;
						Bot.Debugging.Trace("Closing at next drop in profit");
					}
					break;
				}
			}
		}
	}

	/// <summary>Trailing stop loss that uses the worst of the last N candles</summary>
	public class PositionManagerCandleFollow :PositionManager
	{
		public PositionManagerCandleFollow(Rylobot bot, Position pos, int num_candles)
			:base(bot, pos)
		{
			NumCandles = num_candles;
		}

		/// <summary>The look-back length</summary>
		public int NumCandles { get; private set; }

		/// <summary></summary>
		protected override void StepCore()
		{
			if (!Instrument.NewCandle)
				return;

			// Don't adjust the SL until the profit is better than MinRtR
			if (!MinRtRExceeded)
				return;

			// Look at the last few candles to find the level for the SL
			var range = Instrument.PriceRange(-NumCandles, 1);

			// Don't set the SL above the current price
			var sign = Position.Sign();
			var min_sl = Instrument.CurrentPrice(-sign) - sign * Instrument.MCS;

			// Choose the worst over the range and adjust the SL
			if (Position.Sign() > 0)
			{
				var sl = range.Beg - 5 * Instrument.PipSize - Instrument.Spread;
				if (sl < min_sl && (sl > Position.StopLoss || Position.StopLoss == null))
					Broker.ModifyOrder(Instrument, Position, sl:sl);
			}
			else
			{
				var sl = range.End + 5 * Instrument.PipSize + Instrument.Spread;
				if (sl > min_sl && (sl < Position.StopLoss || Position.StopLoss == null))
					Broker.ModifyOrder(Instrument, Position, sl:sl);
			}
		}
	}

	/// <summary>Take profit when the price drops to a percentage of the peak</summary>
	public class PositionManagerPeakPC :PositionManager
	{
		public PositionManagerPeakPC(Rylobot bot, Position pos, double peak_frac = 0.7)
			:base(bot, pos)
		{
			PeakFrac = peak_frac;
		}

		/// <summary>The pull back from the peak</summary>
		public double PeakFrac
		{
			get;
			private set;
		}

		/// <summary>True if the price got close to the threshold</summary>
		private bool Nearly
		{
			get;
			set;
		}

		protected override void StepCore()
		{
			// Only apply this when in profit and the TP has been crossed
			if (Profit <= 0 || MinRtRCrossedIndex == null)
				return;

			// The fraction of the peak profit
			var ratio = Profit / PeakProfit;
			Bot.Debugging.Trace("PeakPC ratio: {0:N3}".Fmt(ratio));
			if (ratio <= 0)
				return;

			// Watch for the price dipping to just above the threshold
			if (ratio < PeakFrac + 0.1)
				Nearly = true;

			// Close if, we've passed the min RtR
			// and the ratio is below the threshold,
			// or we nearly closed and now the price is at a high again
			if (ratio < PeakFrac || (Nearly && ratio > 0.95))
				Bot.ClosePosition(Position);
		}
	}

	/// <summary>A position manager that tries to maximise profits</summary>
	public class PositionManagerLetHerRun :PositionManager
	{
		public PositionManagerLetHerRun(Rylobot bot, Position pos)
			:base(bot, pos)
		{
			PeakRatio = 0.0;
		}

		/// <summary>A scalp threshold. If price spikes higher than this, close out immediately</summary>
		public double PeakRatio { get; private set; }

		/// <summary></summary>
		protected override void StepCore()
		{
			// Determine the age of the trade (in fractional candles)
			var age = Instrument.IdxNow - (double)(EntryIndex + Instrument.IdxFirst);
			if (age < 0.1)
				return;

			// Get the SL to determine the minimum TP
			var sl_rel = Position.StopLossRel();
			if (sl_rel <= 0)
				return;

			// If the profit is less than the required RtR, do nothing until it's crossed
			// If the profit has cross the MinRtR and fallen back, close out
			var rel = Position.ValueAt(Instrument.LatestPrice);
			var tp_rel = MinRtR * sl_rel;
			if (rel < tp_rel)
			{
				if (MinRtRCrossedIndex != null && (MinRtRCrossedIndex.Value - EntryIndex) >= 1)
					Broker.ClosePosition(Position);

				return;
			}

			//
			var min_ratio = (double)tp_rel/ (Instrument.PipSize * age);
			var ratio     = (double)rel   / (Instrument.PipSize * age);

			PeakRatio = Math.Max(PeakRatio, ratio);

			Bot.Debugging.Trace("Ratio: {0}".Fmt(ratio));

			//// Spike! grab now
			//if (area > MaxProfitGradient)
			//{
			//	Debugging.Trace("Profit Spike!: {0:N3} / {1:N3}".Fmt(ratio, MaxProfitGradient));
			//	Broker.ClosePosition(Position);
			//}
			//else if (ratio < min_grad)
			//{
			//}

			//// Take profit when the aspect drops below the threshold
			//if (ratio < 2*min_grad)
			//	Broker.ClosePosition(Position);
		}
	}

	/// <summary>Close a trade after a fixed number of candles</summary>
	public class PositionManagerFixedTime :PositionManager
	{
		public PositionManagerFixedTime(Rylobot bot, Position pos, int num_candles)
			:base(bot, pos)
		{
			NumCandles = num_candles;
		}

		/// <summary>The number of candles to hold the position for</summary>
		public int NumCandles { get; set; }

		/// <summary></summary>
		protected override void StepCore()
		{
			if (!Instrument.NewCandle)
				return;

			if (Instrument.Count - EntryIndex >= NumCandles)
				Bot.ClosePosition(Position);
		}
	}
}

using System;
using System.Diagnostics;
using cAlgo.API;
using pr.extn;
using pr.maths;
using pr.util;

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
			Done             = false;
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

		/// <summary>True once the position manager has done it's job</summary>
		public bool Done { get; protected set; }

		/// <summary>Output the current state</summary>
		public virtual void Dump()
		{
			if (Bot != null)
				Bot.Dump();
		}

		/// <summary>Manage an open trade. This should be called on each tick</summary>
		public void Step()
		{
			if (Done)
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

			// Record that close half was used
			Bot.TradeStats.Event(Position, "CloseHalf");

			// If the position was at the minimum volume, just close it
			if (Position.Volume == vol)
				Broker.ClosePosition(Position, "CloseHalf - Min Volume");
			else
				Broker.ModifyOrder(Instrument, Position, sl:sl, vol:vol);
		}

		/// <summary>Handle position closed</summary>
		private void HandlePositionClosed(object sender, PositionClosedEventArgs args)
		{
			if (args.Position.Id != Position.Id) return;
			Done = true;
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

	/// <summary>Move the stop loss of a trade to break even at a given profit</summary>
	public class PositionManagerMoveToBreakEven :PositionManager
	{
		private QuoteCurrency m_rel_price;
		private QuoteCurrency? m_bias;

		/// <summary>
		/// 'rel_price' is the price in the profit direction that trips the break even. e.g. MCS would trip break even when the price is in profit by MCS.
		/// 'bias' is the offset from break even</summary>
		public PositionManagerMoveToBreakEven(Rylobot bot, Position position, QuoteCurrency rel_price, QuoteCurrency? bias = null)
			:base(bot, position)
		{
			m_rel_price = rel_price;
			m_bias = bias;
		}
		protected override void StepCore()
		{
			// Look for price exceeding the break even level
			var price = Instrument.LatestPrice.Price(-Position.Sign());
			var rel = Position.Sign() * (price - Position.EntryPrice);
			if (rel > m_rel_price)
				Broker.MoveToBreakEven(Instrument, Position, m_bias);
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
						Broker.ClosePosition(Position, "{0} - Scalp".Fmt(R<PositionManagerNervious>.NameOf));
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
						Broker.ClosePosition(Position, "{0} - Non-Peak".Fmt(R<PositionManagerNervious>.NameOf));
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
						Broker.ClosePosition(Position, "{0} - Next Profit Drop".Fmt(R<PositionManagerNervious>.NameOf));
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
						Broker.ClosePosition(Position, "{0} - Bail With Profit".Fmt(R<PositionManagerNervious>.NameOf));
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

	/// <summary>A position manager that tries to maximise profits</summary>
	public class PositionManagerLetHerRun :PositionManager
	{
		/// <summary>A scalp threshold. If price spikes higher than this, close out immediately</summary>
		private double m_peak_ratio;
		public PositionManagerLetHerRun(Rylobot bot, Position pos)
			:base(bot, pos)
		{
			m_peak_ratio = 0.0;
		}
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
					Broker.ClosePosition(Position, "{0} - Min RtR".Fmt(R<PositionManagerLetHerRun>.NameOf));

				return;
			}

			//
			var min_ratio = (double)tp_rel/ (Instrument.PipSize * age);
			var ratio     = (double)rel   / (Instrument.PipSize * age);

			m_peak_ratio = Math.Max(m_peak_ratio, ratio);

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

	/// <summary>Exit at a reversal candle pattern</summary>
	public class PositionManagerCandlePattern :PositionManager
	{
		public PositionManagerCandlePattern(Rylobot bot, Position position)
			:base(bot, position)
		{}
		protected override void StepCore()
		{
			// Look for a candle pattern that indicators trend in the other direction
			CandlePattern patn;
			var pattern = Instrument.IsCandlePattern(out patn);
			if (pattern != null && patn.TT != Position.TradeType)
				Broker.ClosePosition(Position, "{0} - {1}".Fmt(R<PositionManagerCandlePattern>.NameOf, pattern.ToString()));
		}
	}

	/// <summary>Close a trade when there is a breakout that opposes the trade direction</summary>
	public class PositionManagerBreakOut :PositionManager
	{
		private int m_periods;
		private bool m_only_if_in_profit;

		public PositionManagerBreakOut(Rylobot bot, Position position, int periods, bool only_if_in_profit)
			:base(bot, position)
		{
			m_periods = periods;
			m_only_if_in_profit = only_if_in_profit;
		}
		protected override void StepCore()
		{
			// If not in profit when required
			if (m_only_if_in_profit && Position.NetProfit < 0)
				return;

			// If not enough candles since open
			if (Instrument.IdxLast - Instrument.IndexAt(Position.EntryTime) < m_periods)
				return;

			{// Look for an exit signal
				var exit_tt = Instrument.IsBreakOut(m_periods);
				if (exit_tt != null && exit_tt.Value != Position.TradeType)
				{
					Broker.ClosePosition(Position, "{0}-{1} - BreakOut".Fmt(R<PositionManagerBreakOut>.NameOf, m_periods));
					Done = true;
					return;
				}
			}
		}
	}

	/// <summary>Close a position when it fails to make a new profit peak for a while</summary>
	public class PositionManagerTopDrop :PositionManager
	{
		private int m_max_peak_separation;
		private bool m_only_if_in_profit;

		public PositionManagerTopDrop(Rylobot bot, Position position, int max_peak_separation, bool only_if_in_profit)
			:base(bot, position)
		{
			m_max_peak_separation = max_peak_separation;
			m_only_if_in_profit = only_if_in_profit;
		}
		protected override void StepCore()
		{
			// If not in profit when required
			if (m_only_if_in_profit && Position.NetProfit < 0)
				return;

			// Check on new candles
			if (!Instrument.NewCandle)
				return;

			// If not enough candles since open
			if (Instrument.IdxLast - Instrument.IndexAt(Position.EntryTime) < m_max_peak_separation * 4 / 5)
				return;

			// Look for exit conditions
			if (CandlesSincePeak >= m_max_peak_separation * 4 / 5)
			{
				// As we get near the max peak separation, try to close at a peak
				var candle = Instrument[-1];
				if (!candle.Type(Instrument.MCS).IsIndecision() && candle.Sign != Position.Sign())
				{
					Broker.ClosePosition(Position, "{0}-{1} - Same Sign Candle".Fmt(R<PositionManagerTopDrop>.NameOf, m_max_peak_separation));
					Done = true;
					return;
				}

				// Max peak separation
				if (CandlesSincePeak >= m_max_peak_separation)
				{
					Broker.ClosePosition(Position, "{0}-{1} - Max Peak Separation".Fmt(R<PositionManagerTopDrop>.NameOf, m_max_peak_separation));
					Done = true;
					return;
				}
			}
		}
	}

	/// <summary>Close after a sequence of adverse candles (ignoring indecision candles)</summary>
	public class PositionManagerAdverseCandles :PositionManager
	{
		private int m_count;
		public PositionManagerAdverseCandles(Rylobot bot, Position position, int count)
			:base(bot, position)
		{
			m_count = count;
		}
		protected override void StepCore()
		{
			// Check on new candles
			if (!Instrument.NewCandle)
				return;

			// If not enough candles since open
			if (Instrument.IdxLast - Instrument.IndexAt(Position.EntryTime) < m_count)
				return;

			var sign = Position.Sign();
			var mcs = Instrument.MCS;

			// Look for exit conditions
			var count = 0;
			for (var i = Instrument.IdxLast; i-- != Instrument.IdxFirst;)
			{
				var candle = Instrument[i];
				if (candle.Sign == sign) break;
				if (candle.Type(mcs).IsIndecision()) continue;
				++count;
			}
			if (count >= m_count)
			{
				Broker.ClosePosition(Position, "{0}-{1} - Adverse Candles".Fmt(R<PositionManagerAdverseCandles>.NameOf, m_count));
				Done = true;
				return;
			}
		}
	}

	/// <summary>Close after the price crosses a moving average by more than 'offset * MCS'</summary>
	public class PositionManagerCloseAtMA :PositionManager
	{

		private Indicator m_ma;
		private double m_offset;
		public PositionManagerCloseAtMA(Rylobot bot, Position position, int ma_periods, double offset)
			:base(bot, position)
		{
			m_ma     = Indicator.EMA("ma", Instrument, ma_periods);
			m_offset = offset;
		}
		protected override void StepCore()
		{
			var mcs = Instrument.MCS;

			// Move the TP to the current MA value
			var sign = Position.Sign();
			var tp = m_ma[0] + sign * m_offset * mcs;
			if (Position.TakeProfit == null || Math.Abs(Position.TakeProfit.Value - tp) > Instrument.PipSize)
				Broker.ModifyOrder(Instrument, Position, tp:tp);
		}
	}
}

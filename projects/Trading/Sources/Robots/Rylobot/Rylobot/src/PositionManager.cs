using System;
using System.Diagnostics;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	public class PositionManager
	{
		/// <summary>Manage an active position</summary>
		public PositionManager(Instrument instr, Position pos, Idx? neg_idx = null)
		{
			var index = (int)((neg_idx ?? 0) - instr.IdxFirst);

			ColdFeetCount       = 10;
			Instrument          = instr;
			Position            = pos;
			PeakProfit          = double.NegativeInfinity;
			LastProfit          = double.NegativeInfinity;
			State               = EState.TradeLooksGood;
			PeakProfitIndex     = index;
			LastProfitableIndex = index;
		}

		/// <summary>The app logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return Instrument.Bot; }
		}

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return m_instr; }
			private set
			{
				if (m_instr == value) return;
				m_instr = value;
			}
		}
		private Instrument m_instr;

		/// <summary>The position to be managed</summary>
		public Position Position
		{
			[DebuggerStepThrough] get { return m_position; }
			private set
			{
				if (m_position == value) return;
				m_position = value;
			}
		}
		private Position m_position;

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
		public event EventHandler StateChanged;

		/// <summary>How many candles with a new profit peak before we start getting nervous</summary>
		public int ColdFeetCount { get; set; }

		/// <summary>The maximum profit reached with the current trade</summary>
		public AcctCurrency PeakProfit { get; private set; }

		/// <summary>The profit value last time step was called</summary>
		public AcctCurrency LastProfit { get; private set; }

		/// <summary>Where the peak profit was detected</summary>
		public int PeakProfitIndex { get; private set; }

		/// <summary>Where the position was last profitable</summary>
		public int LastProfitableIndex { get; private set; }

		/// <summary>The number of candles since a new peak was made</summary>
		public int CandlesSincePeak { get; private set; }

		/// <summary>The number of candles since the trade was profitable</summary>
		public int CandlesSinceProfitable { get; private set; }

		/// <summary>The number of price ticks since a new peak was made</summary>
		public int TicksSincePeak { get; private set; }

		/// <summary>The number of price ticks since the trade was profitable</summary>
		public int TicksSinceProfitable { get; private set; }

		/// <summary>The profit limit at which we want to take profits as soon as possible</summary>
		public AcctCurrency ScalpThreshold
		{
			get { return Bot.Broker.Balance * 0.01; }
		}

		/// <summary>The profit limit at which we move the SL to break even</summary>
		public AcctCurrency BreakEvenThreshold
		{
			get { return Bot.Broker.Balance * 0.005; }
		}

		/// <summary>Manage an open trade. This should be called on each tick</summary>
		public void Step()
		{
			if (Position == null || State == EState.PositionClosed)
				return;

			// Current profit state
			var profitable = Position.NetProfit > 0;
			var new_peak = Position.NetProfit >= PeakProfit;
			var better = Position.NetProfit > LastProfit;
			var profit = Instrument.Symbol.AcctToQuote(Position.NetProfit);
			if (new_peak)
			{
				PeakProfit = Position.NetProfit;
				PeakProfitIndex = Instrument.Count;
			}

			// How long since a new profit peak was made
			CandlesSincePeak = new_peak ? 0 : Instrument.Count - PeakProfitIndex;
			TicksSincePeak   = new_peak ? 0 : TicksSincePeak + 1;

			// How long since the trade was profitable at all
			CandlesSinceProfitable = profitable ? 0 : Instrument.Count - LastProfitableIndex;
			TicksSinceProfitable   = profitable ? 0 : TicksSinceProfitable + 1;

			// Enter scalp mode when profit > scalp threshold
			if (State == EState.TradeLooksGood && Position.NetProfit > ScalpThreshold)
			{
				Debugging.Trace("Entering scalp mode - Profit ${0} > Threshold ${1}".Fmt(Position.NetProfit, ScalpThreshold));
				State = EState.Scalp;
			}

			// Move SL to break even when profit > BreakEvenThreshold (adjusted for Spread)
			var adjusted_profit = Position.NetProfit - Instrument.Symbol.QuoteToAcct(Instrument.Spread * Position.Volume);
			if (adjusted_profit > BreakEvenThreshold && Position.StopLossRel() > 0)
			{
				Debugging.Trace("Moving SL to break even - Profit ${0} > Threshold ${1}".Fmt(Position.NetProfit, BreakEvenThreshold));

				var trade = new Trade(Instrument, Position) { SL = Position.EntryPrice + Position.Sign() * 2.0 * Instrument.Spread };
				Bot.Broker.ModifyOrder(Position, trade);
			}

			// What about candle follow mode:
			// If the price is heading for the TP and the last tops/bottoms of the last few candles
			// have been steadily increasing/decreasing. Move the SL to just above/below the previous candle

			// A state machine for managing the position
			switch (State)
			{
			case EState.TradeLooksGood:
				{
					// In this state, the trade looks good. Keep it open.
					if (CandlesSincePeak >= ColdFeetCount)
					{
						// It's been a while since a profit peak was set...
						State = EState.LookForExitAfterNextPeak;
						Debugging.Trace("Getting nervous... no peaks for a while");
					}
					break;
				}
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
							Debugging.Trace("Closing position - indecision candle");
							close = true;
							break;
						}

						// Close if the peak of the candle just closed is not better than the candle before's peak i.e. Not higher lows, or lower highs
						if (Instrument.NewCandle &&
							Position.EntryTime < Instrument[-1].TimestampUTC &&
							Instrument.SequentialPeaks(Position.Sign(), 0) < 1)
						{
							Debugging.Trace("Closing position - peaks oppose trend direction");
							close = true;
							break;
						}

						// Close if the candle closes worse then the close of the -2 candle
						if (Instrument.NewCandle &&
							Position.EntryTime < Instrument[-1].TimestampUTC &&
							Math.Sign(Instrument[-1].Close - Instrument[-2].Close) != Position.Sign())
						{
							Debugging.Trace("Closing position - candle close was worse that close of -2 candle");
							close = true;
							break;
						}

						// Close if the profit is huge
						if (Position.NetProfit > 2.0 * ScalpThreshold && TicksSincePeak > 10)
						{
							Debugging.Trace("Closing position - huge spike detected!");
							close = true;
							break;
						}

						// Don't close.
						break;
					}
					if (close)
					{
						Bot.Broker.ClosePosition(Position);
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
							Bot.Broker.ModifyOrder(Position, trade);
						}
					}
					break;
				}
			case EState.LookForExitAfterNextPeak:
				{
					// In this state, the profit hit a peak a while ago,
					// and we're waiting for it to come back to that peak
					// level so we can bail out.
					if (new_peak)
					{
						State = EState.CloseAtNextNonPeak;
						Debugging.Trace("Closing at next non peak");
					}
					else if (CandlesSincePeak >= 2 * ColdFeetCount)
					{
						State = EState.BailIfProfitable;
						Debugging.Trace("Closing when profitable");
					}
					break;
				}
			case EState.CloseAtNextNonPeak:
				{
					// In this state we're looking to exit the trade the
					// next time the profit peak is not set
					if (!new_peak)
					{
						Position = Bot.Broker.ClosePosition(Position);
						State = EState.PositionClosed;
						Debugging.Trace("Position closed - non-peak");
					}
					break;
				}
			case EState.CloseAtNextProfitDrop:
				{
					// In this state we just want out with the minimum of loss.
					// Bail as soon as the profit decreases
					if (!better)
					{
						Position = Bot.Broker.ClosePosition(Position);
						State = EState.PositionClosed;
						Debugging.Trace("Position closed - profit dropped");
					}
					break;
				}
			case EState.BailIfProfitable:
				{
					// In this state, it's been too long since the peak was last
					// hit, bail out if profitable
					if (new_peak)
					{
						State = EState.CloseAtNextNonPeak;
					}
					else if (Position.NetProfit > 0)
					{
						Position = Bot.Broker.ClosePosition(Position);
						State = EState.PositionClosed;
					}
					else if (CandlesSinceProfitable > 4 * ColdFeetCount)
					{
						State = EState.CloseAtNextProfitDrop;
						Debugging.Trace("Closing at next drop in profit");
					}
					break;
				}
			}

			// Track peak and profitable positions
			if (profitable)
			{
				LastProfitableIndex = Instrument.Count;
			}
			if (Position != null)
			{
				LastProfit = Position.NetProfit;
			}
		}
	}
}

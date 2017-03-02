using System;
using System.Diagnostics;
using cAlgo.API;
using pr.extn;

namespace Rylobot
{
	public class PositionManager
	{
		/// <summary>Manage an active position</summary>
		public PositionManager(Instrument instr, Position pos, NegIdx? neg_idx = null)
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
		public double PeakProfit { get; private set; }

		/// <summary>The profit value last time step was called</summary>
		public double LastProfit { get; private set; }

		/// <summary>Where the peak profit was detected</summary>
		public int PeakProfitIndex { get; private set; }

		/// <summary>Where the position was last profitable</summary>
		public int LastProfitableIndex { get; private set; }

		/// <summary>Manage an open trade. This should be called on each tick</summary>
		public void Step()
		{
			if (Position == null || State == EState.PositionClosed)
				return;

			// Current profit state
			var profitable = Position.NetProfit > 0;
			var new_peak = Position.NetProfit > PeakProfit;
			var better = Position.NetProfit > LastProfit;

			// How long since a new profit peak was made
			var time_since_peak = new_peak ? 0 : Instrument.Count - PeakProfitIndex;

			// How long since the trade was profitable at all
			var time_since_profitable = profitable ? 0 : Instrument.Count - LastProfitableIndex;

			// If we're lucky enough to get a huge spike in profit, go into scalp mode
			if (Position.NetProfit > Bot.Broker.AllowedRisk)
				State = EState.Scalp;

			// What about candle follow mode:
			// If the price is heading for the TP and the last tops/bottoms of the last few candles
			// have been steadily increasing/decreasing. Move the SL to just above/below the previous candle

			// A state machine for managing the position
			switch (State)
			{
			case EState.TradeLooksGood:
				{
					// In this state, the trade looks good. Keep it open.
					if (time_since_peak >= ColdFeetCount)
					{
						// It's been a while since a profit peak was set...
						State = EState.LookForExitAfterNextPeak;
						Debugging.Trace("Getting nervous... no peaks for a while");
					}
					break;
				}
			case EState.Scalp:
				{
					// In this mode we've made a decent profit. Close the Position
					// when the profit drops by 5% of the peak
					if (Position.NetProfit < 0.95 * PeakProfit)
					{
						Bot.Broker.ClosePosition(Position);
						State = EState.PositionClosed;
						Debugging.Trace("Position closed - scalping profit");
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
					else if (time_since_peak >= 2 * ColdFeetCount)
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
					else if (time_since_profitable > 4 * ColdFeetCount)
					{
						State = EState.CloseAtNextProfitDrop;
						Debugging.Trace("Closing at next drop in profit");
					}
					break;
				}
			}

			// Track peak and profitable positions
			if (new_peak)
			{
				PeakProfit = Position.NetProfit;
				PeakProfitIndex = Instrument.Count;
			}
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

using System;
using System.Diagnostics;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.maths;

namespace Rylobot
{
	public class StrategyTrend :Strategy
	{
		private ExponentialMovingAverage m_ema0;
		private ExponentialMovingAverage m_ema1;

		public StrategyTrend(Rylobot bot)
			:base(bot, "StrategyTrend")
		{
			m_slope = new ExpMovingAvr(10);
			m_ema0 = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Median, 100);
			m_ema1 = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Median, 55);
		}

		/// <summary>The position managed by this strategy</summary>
		public Position Position
		{
			[DebuggerStepThrough] get { return m_position; }
			private set
			{
				if (m_position == value) return;
				if (m_position != null)
				{
					PositionManager = null;
					State = EState.FindEntryTrigger;
				}
				m_position = value;
				if (m_position != null)
				{
					State = EState.ManagePosition;
					PositionManager = new PositionManager(Instrument, m_position);
					PositionManager.StateChanged += (s,a) => Dump();
				}
			}
		}
		private Position m_position;

		/// <summary>An optional manager of a position</summary>
		public PositionManager PositionManager
		{
			[DebuggerStepThrough] get { return m_pos_mgr; }
			set
			{
				if (m_pos_mgr == value) return;
				m_pos_mgr = value;
			}
		}
		private PositionManager m_pos_mgr;

		/// <summary>Return the current trend.  +0.5 and up = rising, -0.5 and below = falling</summary>
		public double Trend
		{
			get
			{
				for (; m_slope.Count < m_ema0.Result.Count; )
				{
					// This strategy is suitable for trending data.
					// Best when the slope is steadily increasing/decreasing.
					// Use the slope of the large EMA for suitability.
					const double SlopeThreshold = 0.025;
					var s = m_ema0.Result.FirstDerivative(m_slope.Count) / Instrument.MCS_50;
					var score = Maths.Sigmoid(s, SlopeThreshold);
					m_slope.Add(score);
				}
				return m_slope.Mean;
			}
		}
		private ExpMovingAvr m_slope;

		/// <summary>Return the suitability of this strategy</summary>
		public override double SuitabilityScore
		{
			get { return Maths.Abs(Trend); }
		}

		/// <summary>State machine state variable</summary>
		private EState State
		{
			get { return m_state; }
			set
			{
				if (m_state == value) return;
				m_state = value;
			}
		}
		private EState m_state;
		private enum EState
		{
			FindEntryTrigger,
			EnterOnPullBack,
			ManagePosition,
		}

		/// <summary>The target entry for a trade, use while in the 'BuyOnPullBack' state</summary>
		private TargetEntryData TargetEntry
		{
			get; set;
		}
		private struct TargetEntryData
		{
			public TargetEntryData(QuoteCurrency price, TradeType tt)
			{
				Price = price;
				TT = tt;
			}
			public QuoteCurrency Price;
			public TradeType TT;
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			// Find the position associated with this strategy
			Position = FindLivePosition(Position);

			switch (State)
			{
			case EState.FindEntryTrigger:
				#region
				{
					// In this state we're waiting for an entry trigger.
					Debug.Assert(Position == null);

					// Do nothing unless the conditions are right
					if (SuitabilityScore < 0.5)
						return;

					if (Instrument.NewCandle)
						Dump();

					// Always trade with the trend
					var sign = Math.Sign(Trend);
					var tt = sign > 0 ? TradeType.Buy : TradeType.Sell;

					// Look for a candle pattern that indicates entry
					int forecast_direction;
					QuoteCurrency target_entry;
					if (!Instrument.IsCandlePattern(0, out forecast_direction, out target_entry) ||
						forecast_direction != sign)
						return;

					// Switch to the 'EnterOnPullBack' state
					State = EState.EnterOnPullBack;
					TargetEntry = new TargetEntryData(target_entry, tt);
					Dump();
					break;
				}
				#endregion
			case EState.EnterOnPullBack:
				#region
				{
					if (Instrument.NewCandle)
						Dump();

					// In this state we're waiting for the price to pull back
					// to the target entry. Check this on every tick.
					var tt = TargetEntry.TT;
					var cp = Instrument.CurrentPrice(tt.Sign());
					if (Misc.Sign(cp - TargetEntry.Price) != tt.Sign())
					{
						var trade = new Trade(Bot, Instrument, tt, Label);
						Position = Bot.Broker.CreateOrder(trade);
						Dump();
					}
					break;
				}
				#endregion
			case EState.ManagePosition:
				#region
				{
					if (Instrument.NewCandle)
						Dump();

					// Manage a trade position
					PositionManager.Step();
					break;
				}
				#endregion
			}
		}

		/// <summary>Debugging Output</summary>
		private void Dump()
		{
			if (!Debugger.IsAttached) return;
			Debugging.Dump(Instrument, range_:new Range(-100,1), high_res:20, emas:new[] {200,55});
			Debugging.Dump(new SnR(Instrument, -100, 0));
			if (Position != null)
				Debugging.LogTrade(Position);
		}
	}
}

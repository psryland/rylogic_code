using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Indicators;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyTail :Strategy
	{
		public StrategyTail(Rylobot bot)
			:base(bot, "StrategyTail")
		{}

		/// <summary>The position managed by this strategy</summary>
		public Position Position
		{
			get { return m_position; }
			private set
			{
				if (m_position == value) return;
				if (m_position != null)
				{
					PositionManager = null;
				}
				m_position = value;
				if (m_position != null)
				{
					PositionManager = new PositionManager(Instrument, m_position);
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

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// Only on new candles
			Position = FindLivePosition(Position);
			if (!Instrument.NewCandle)
				return;

			if (Debugger.IsAttached)
			{
				Debugging.Dump(Instrument, high_res:20.0, emas:new[] { 100 });
				if (Instrument.Count == 230)
					Debug.WriteLine("Break");
			}

			var sym = Instrument.Symbol;
			var mcs = Instrument.MedianCandleSize(-10, 0);
			var C0 = Instrument[0];
			var C1 = Instrument[-1];
			var C2 = Instrument[-2];

			// One at a time
			if (Position == null)
			{
				TradeType tt;
				string msg;

				// Look for a reversal
				int reversal_direction;
				QuoteCurrency target_entry;
				if (!Instrument.IsCandlePattern(0, out reversal_direction, out target_entry))
					return;

				var preceeding_trend = Instrument.MeasureTrend(-10, 0);

				var tt_confirmation = C1.Bullish ? TradeType.Buy : TradeType.Sell;

				const double EmaSlopeToMCS = 20;
				var ema = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close,100);
				var ema_slope = ema.Result.FirstDerivative(ema.Result.Count-1) * EmaSlopeToMCS / mcs;

				// If the EMA slope is flat, look for ranging
				if (Math.Abs(ema_slope) < 1.0)
				{
					// Side of the EMA. Must be significantly to one side of the EMA
					var dist = C1.Close - ema.Result.LastValue;
					if (Math.Abs(dist) < mcs)
						return;

					tt = C1.Close < ema.Result.LastValue ? TradeType.Buy : TradeType.Sell;
					msg = "{0} - ranging EMA. Slope = {1}, Distance = {2}".Fmt(tt, ema_slope, dist);
				}
				// Otherwise look for indecision near the EMA
				else
				{
					var dist = C1.Close - ema.Result.LastValue;
					if (Maths.Abs(dist) > mcs)
						return;

					tt = ema_slope > 0.0 ? TradeType.Buy : TradeType.Sell;
					msg = "{0} - trending EMA. Slope = {1}, Dist = {2}".Fmt(tt, ema_slope, dist);
				}

				// Check the candle confirms the direction
				if (tt != tt_confirmation)
					return;

				var sign = tt.Sign();
				var rtr = new RangeF(1.0, 2.0);
				var trade = new Trade(Bot, Instrument, tt, Label, rtr_range:rtr);
				Bot.Print(msg);
				Position = Bot.Broker.CreateOrder(trade);
				PositionManager = new PositionManager(Instrument, Position);
			}
			else
			{
				PositionManager.Step();
			}
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.0; // not implemented 
		}
	}
}

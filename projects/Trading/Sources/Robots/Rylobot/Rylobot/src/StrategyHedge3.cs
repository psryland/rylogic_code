using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyHedge3 :Strategy
	{
		// Notes:
		//  - Open 1K positions
		//  - No SL/TP
		//  - Ensure the direction of profit for the net position is the same as the slow EMA
		//  - Set SL to break even + amount

		public StrategyHedge3(Rylobot bot)
			:base(bot, "StrategyHedge3")
		{
			// Initialise the median price average
			MedianPrice = new ExpMovingAvr(10);
			foreach (var c in Instrument.CandleRange())
				MedianPrice.Add(c.Median);

			LastEquity = Bot.Broker.Equity;
		}
		public override void Dispose()
		{
			base.Dispose();
		}

		/// <summary>The Equity at the last time a position was opened</summary>
		private AcctCurrency LastEquity { get; set; }

		/// <summary>The EMA of the median candle price</summary>
		private ExpMovingAvr MedianPrice
		{
			get;
			set;
		}

		/// <summary>The current signed net position volume</summary>
		private long NetVolume
		{
			get { return Bot.Broker.NetVolume(Label); }
		}

		/// <summary>The direction of increasing profit</summary>
		private int ProfitDirection
		{
			get { return Math.Sign(NetVolume); }
		}

		/// <summary>The sign of the trend direction</summary>
		private int TrendDirection
		{
			get
			{
				//
				const int count = 5;
				var t0 = Instrument.MeasureTrend(+0 - count, +0);
				var t1 = Instrument.MeasureTrend(+1 - count, +1);

				// The previous trend was really strong, use the current candle as the direction
				if (Math.Abs(t0) > 0.9)
					return Instrument.Latest.Sign;

				// The previous trend was weak, but with the latest candle is stronger
				if (Math.Abs(t0) < 0.3 && Math.Abs(t1) > 0.6)
					return Instrument.Latest.Sign;

			//	// Otherwise use the slow EMA gradient
			//	var ema = Bot.Indicators.ExponentialMovingAverage(Instrument.Data.Close, 200);
			//	return Math.Sign(ema.Result.FirstDerivative());

				// The last complete candle
				var candle = Instrument.LatestAge > 0.5 ? Instrument[0] : Instrument[-1];
				var ratio = Instrument.Compare(candle, MedianPrice.Mean, false);
				return Math.Sign(ratio);

				//var ema = Bot.Indicators.ExponentialMovingAverage(Instrument.Data.Median, 3);
				//var candle = Instrument[-1];
				//var ratio = Instrument.Compare(candle, ema);
				//return Math.Sign(ratio);

				//var slope = Instrument.EMASlope(0);
				//var slope = Bot.Indicators.ExponentialMovingAverage(Instrument.Data.Close, 14).Result.FirstDerivative();
				//return Math.Sign(slope);
			}
		}

		/// <summary>True if there are positions within 'MinPositionDist' of the current price</summary>
		private bool NearbyPositions
		{
			get
			{
				var min_trade_dist = Instrument.MCS * 0.1;
				var price = Instrument.LatestPrice.Mid;
				return Positions.Any(x => Misc.Abs(x.EntryPrice - price) < min_trade_dist);
			}
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			// Track the average median price
			if (Instrument.NewCandle)
				MedianPrice.Add(Instrument.Latest.Median);

			// Determine the profit gradient from the net position
			var profit_sign = ProfitDirection;

			// Get the trend direction
			var sign = TrendDirection;

			// Look for positions that cancel out
			#region Cancel-Outs
			{
				var winners = Positions.Where(x => x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToList();
				var losers  = Positions.Where(x => x.TradeType.Sign() != sign && x.NetProfit < 0).OrderBy(x => +x.NetProfit).ToList();
				for (;winners.Count != 0 && losers.Count != 0;)
				{
					// Start with out best winner, and cancel as many losers
					// as possible while still keeping a net profit.
					var good = winners.First();
					var bal = good.NetProfit;

					// Find losing trades to cancel
					var to_cancel = new List<Position>();
					foreach (var bad in losers)
					{
						var min_profit = Bot.Broker.Equity * 0.001;
						if (bal + bad.NetProfit < min_profit) continue;
						to_cancel.Add(bad);
						bal -= bad.NetProfit;
					}

					// No cancellations
					if (to_cancel.Count == 0)
						break;

					// Close the trades that cancel with a profit
					Bot.Broker.ClosePosition(good);
					foreach (var p in to_cancel) Bot.Broker.ClosePosition(p);
					winners.Remove(good);
					losers.RemoveAll(to_cancel);
				}
			}
			#endregion

			// Look for positions with the wrong direction but which happen to be in profit
			#region Lucky
			{
				foreach (var pos in Positions.Where(x => x.TradeType.Sign() != sign && x.NetProfit > 0).ToArray())
					Bot.Broker.ClosePosition(pos);
			}
			#endregion

			// Close all positions in the wrong direction with a loss greater than some amount.
			#region Close on Direction Change
			{
				var max_loss = Bot.Broker.Equity * 0.002;
				foreach (var pos in Positions.Where(x => x.TradeType.Sign() != sign && x.NetProfit < -max_loss).ToArray())
					Bot.Broker.ClosePosition(pos);
			}
			#endregion

			// Close on large equity jumps
			if (Bot.Broker.Equity > LastEquity * 1.01)
				Bot.Broker.CloseAllPositions(Label);

			// Check that profit direction agrees with the EMA slope
			#region Set Profit Direction

			// If the trend is against the profit direction, open another position
			// If there are positions within 'min_trade_dist' of the current price
			// don't open new positions until the price has moved
			profit_sign = ProfitDirection;
			if (sign != profit_sign && !NearbyPositions && Instrument.NewCandle)
			{
				Debugging.LogInstrument();

				var tt = sign != 0
					? CAlgo.SignToTradeType(+sign)
					: CAlgo.SignToTradeType(-profit_sign);

				var sl = Instrument.CurrentPrice(-sign) - sign * Instrument.MCS * 10;

				// Choose a volume that will make the profit direction == 'sign' (scale volume as the balance grows)
				var volume = Math.Abs(NetVolume);
				if (sign != 0) volume += Bot.Broker.MaxVolume(Instrument, sl);
				if (volume == 0) volume = Instrument.Symbol.VolumeMin;

				// Open a position so the profit direction matches the EMA slope
				var trade = new Trade(Instrument, tt, Label, Instrument.LatestPrice.Mid, null, null, volume);
				using (Bot.Broker.SuspendRiskCheck(true))
					Bot.Broker.CreateOrder(trade);

				// Record the equity at the time of the last trade
				LastEquity = Bot.Broker.Equity;
			}

			#endregion

			if (Instrument.NewCandle)
			{
				Debugging.LogInstrument();
				Debugging.BreakOnCandleOfInterest();
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get { return 1.0; }
		}
	}
}

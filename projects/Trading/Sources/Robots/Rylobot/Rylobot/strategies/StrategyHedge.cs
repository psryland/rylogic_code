using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	public class StrategyHedge :Strategy
	{
		private const int EmaPeriods = 55;

		public StrategyHedge(Rylobot bot, double risk)
			:base(bot, "StrategyHedge", risk)
		{
			EMA = Indicator.EMA(Instrument, EmaPeriods);
		}

		/// <summary>Return the suitability of this strategy</summary>
		public override double SuitabilityScore
		{
			get { return 1.0f; }
		}

		/// <summary>EMA for determining trend</summary>
		private Indicator EMA { get; set; }

		/// <summary>The equity level due to trades created by this strategy the last time a position was opened</summary>
		private AcctCurrency LastEquity { get; set; }

		/// <summary>The sign of the trend direction</summary>
		private int TrendSign
		{
			get
			{
				var slope = EMA.FirstDerivative(0);
				var trend = Instrument.MeasureTrendFromSlope(slope);
				return Math.Abs(trend) > 0.5 ? Math.Sign(trend) : 0;
			}
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.LogInstrument();
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			using (Broker.SuspendRiskCheck(ignore_risk:true))
			{
				const double TakeProfitFrac = 1.020;
				const double ReverseFrac    = 0.998;
				Dump();

				// The unit of volume to trade
				var mcs = Instrument.MCS;
				var ep = Instrument.LatestPrice.Mid;
				var vol = Broker.ChooseVolume(Instrument, 5.0 * mcs);

				// Open a trade in the direction of the trend
				if (ProfitSign == 0)
				{
					Dump();
					Debugging.Trace("Idx={0},Tick={1} - Setting profit sign ({2}) - Equity = ${3}".Fmt(Instrument.Count, Bot.TickNumber, TrendSign, Equity));

					var tt = CAlgo.SignToTradeType(TrendSign);
					var trade = new Trade(Instrument, tt, Label, ep, null, null, vol);
					Broker.CreateOrder(trade);
					LastEquity = Equity;
				}

				// While the profit direction is the same as the trend direction
				// look for good spots to add hedged pending orders
				if (ProfitSign == TrendSign)
				{
					// If the current price is at a SnR level, add pending orders on either side.
					// Hopefully price will reverse at the SnR level and only trigger one of the pending orders.
					// If price then reverses we can sell an existing profitable trade and be left with the
					// profit direction going in the right direction
					var snr = new SnR(Instrument, ep);
					var lvl_above = snr.Nearest(ep, +1, min_dist:0.5*mcs, min_strength:0.7);
					var lvl_below = snr.Nearest(ep, -1, min_dist:0.5*mcs, min_strength:0.7);

					// Choose price levels for the pending orders
					var above = lvl_above != null ? lvl_above.Price + 0.5*mcs : ep + mcs;
					var below = lvl_below != null ? lvl_below.Price - 0.5*mcs : ep - mcs;

					// Only recreate if necessary
					if (!PendingOrders.Any(x => x.TradeType == TradeType.Buy  && Maths.FEql(x.TargetPrice, above, 5*Instrument.PipSize)) ||
						!PendingOrders.Any(x => x.TradeType == TradeType.Sell && Maths.FEql(x.TargetPrice, below, 5*Instrument.PipSize)))
					{
						Dump();
						Debugging.Dump(snr);
						Debugging.Trace("Idx={0},Tick={1} - Adjusting pending orders - Equity = ${2}".Fmt(Instrument.Count, Bot.TickNumber, Equity));

						// Cancel any other pending orders further away than these too
						Broker.CancelAllPendingOrders(Label);

						var buy = new Trade(Instrument, TradeType.Buy, Label, above, null, null, vol);
						var sel = new Trade(Instrument, TradeType.Sell, Label, below, null, null, vol);
						Broker.CreatePendingOrder(buy);
						Broker.CreatePendingOrder(sel);
					}
				}

				// If the profit is against the Trend, do nothing until losing a fraction of last equity
				if (ProfitSign != TrendSign && Equity < ReverseFrac * LastEquity)
				{
					Dump();
					Debugging.Trace("Idx={0},Tick={1} - Changing profit sign to ({2}) - Equity = ${3}".Fmt(Instrument.Count, Bot.TickNumber, TrendSign, Equity));
					var sign = -ProfitSign;

					// Try to reverse the sign by closing profitable positions
					foreach (var pos in Positions.Where(x => x.Sign() != sign && x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToArray())
					{
						Broker.ClosePosition(pos);

						// Break out when the profit direction has been reversed
						if (ProfitSign == sign)
							break;
					}

					// Couldn't reverse the sign by closing positions, open a new one
					if (ProfitSign != sign)
					{
						var tt = CAlgo.SignToTradeType(sign);
						var reverse_vol = Math.Abs(NetVolume) + vol;
						var trade = new Trade(Instrument, tt, Label, ep, null, null, reverse_vol);
						Broker.CreateOrder(trade);
					}
					LastEquity = Equity;
				}

				// If equity is greater than a threshold, take profits
				if (Equity > TakeProfitFrac * LastEquity)
				{
					Dump();
					Debugging.Trace("Idx={0},Tick={1} - Profit taking - Equity = ${2}".Fmt(Instrument.Count, Bot.TickNumber, Equity));

					var sign = ProfitSign;
					foreach (var pos in Positions.Where(x => x.Sign() == sign && x.NetProfit > 0).OrderBy(x => -x.NetProfit).ToArray())
					{
						if (pos.Volume >= Math.Abs(NetVolume)) continue;
						Broker.ClosePosition(pos);
					}
					LastEquity = Equity;
				}
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

		///// <summary>Close all positions on shutdown</summary>
		//protected override void OnBotStopping()
		//{
		//	foreach (var pos in Positions)
		//		Broker.ClosePosition(pos);

		//	base.OnBotStopping();
		//}
	}
}

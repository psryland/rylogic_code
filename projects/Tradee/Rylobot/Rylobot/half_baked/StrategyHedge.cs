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
		// Notes:
		//  - At the 00 levels, open long and short positions
		//  - When price moves to a new 00 level, close profitable
		//    positions and open new positions so that longs == shorts
		//  - Relies on price variation
		//  - Don't need SL/TP because position is always hedged

		// RESULT:
		// Doesn't work... you end up with bad trades at the extreme prices that
		// never get cleared.

		private Pips BoundarySizePips = 50.0;
		private QuoteCurrency MinProfitThreshold = 1.0;
		private double PendingOrderOffset = 2; // multiples of mcs

		public StrategyHedge(Rylobot bot)
			:base(bot, "StrategyHedge")
		{}

		/// <summary>Return the suitability of this strategy</summary>
		public override double SuitabilityScore
		{
			get { return 1.0f; }
		}

		/// <summary>The last price boundary considered</summary>
		private QuoteCurrency LastBoundary { get; set; }

		/// <summary>The volume to use for hedged positions</summary>
		private long Volume
		{
			get { return Instrument.Symbol.VolumeMin; }
		}

		/// <summary>The Net profit</summary>
		private QuoteCurrency Profit { get; set; }

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (Instrument.HighRes.Count < 2)
				return;

			using (Bot.Broker.SuspendRiskCheck(ignore_risk:true))
			{
				// When price crosses a new price boundary
				var old = Instrument.HighRes.Back(1);
				var nue = Instrument.HighRes.Back(0);

				// Ensure one trade to kick things off
				if (!Positions.Any())
				{
					var trade = new Trade(Instrument, TradeType.Buy, Label, Instrument.LatestPrice.Bid, null, null, Volume);
					Bot.Broker.CreateOrder(trade);
				}

				// If the price is spanning a boundary, look for profit
				var boundary = PriceBoundary(old, nue);
				if (boundary != null && boundary != LastBoundary)
				{
					LastBoundary = boundary.Value;

					// Close any positions with a nett profit
					var volume_closed = 0L;
					foreach (var pos in Positions)
					{
						if (pos.NetProfit < MinProfitThreshold) continue;
						volume_closed += pos.TradeType.Sign() * pos.Volume;
						Bot.Broker.ClosePosition(pos);
						Profit += pos.NetProfit;
					}

					// Find the worse position and if we've made more in profit since it was created close it.
					if (Positions.Any())
					{
						var worst = Positions.MinBy(x => x.NetProfit);
						if (Profit > -worst.NetProfit)
						{
							volume_closed += worst.TradeType.Sign() * worst.Volume;
							Bot.Broker.ClosePosition(worst);
							Profit += worst.NetProfit;
						}
					}

					// Create new positions
					if (volume_closed != 0)
					{
						var sold_sign = Math.Sign(volume_closed);
						var sold_tt = CAlgo.SignToTradeType(sold_sign);

						// Create pending orders around the current price.
						// Hopefully, the pending orders won't be tripped and price will reverse.
						{
							var ep = nue.Mid + sold_sign * PendingOrderOffset * Instrument.MCS_50;
							var trade = new Trade(Instrument, sold_tt, Label, ep, null, null, Volume);
							Bot.Broker.CreatePendingOrder(trade);
						}
						{
							var ep = nue.Mid - sold_sign * PendingOrderOffset * Instrument.MCS_50;
							var trade = new Trade(Instrument, sold_tt.Opposite(), Label, ep, null, null, Volume);
							Bot.Broker.CreatePendingOrder(trade);
						}

						// Ensure there is only one pending order above and below the current price
						var order_above = (PendingOrder)null;
						var order_below = (PendingOrder)null;
						var price_above = +double.MaxValue;
						var price_below = -double.MaxValue;
						foreach (var pos in PendingOrders)
						{
							if (pos.TradeType == TradeType.Buy && pos.TargetPrice < price_above)
								order_above = pos;
							if (pos.TradeType == TradeType.Sell && pos.TargetPrice > price_below)
								order_below = pos;
						}
						foreach (var pos in PendingOrders)
						{
							if (pos == order_above) continue;
							if (pos == order_below) continue;
							Bot.Broker.CancelPendingOrder(pos);
						}
					}
				}

				// Ensure the positions are balanced
				var balance = 0L;
				foreach (var pos in Positions)
					balance += pos.TradeType.Sign() * pos.Volume;
				foreach (var pos in PendingOrders)
					balance += pos.TradeType.Sign() * pos.Volume;

				// If there is a net position, create positions to restore balance
				if (balance != 0)
				{
					// A positive balance means losses if the price falls.
					// Therefore, positive => sell, negative => buy.
					var tt = CAlgo.SignToTradeType(-Math.Sign(balance));

					// Create a trade to balance the position
					var trade = new Trade(Instrument, tt, Label, nue.Mid, null, null, Math.Abs(balance));
					Bot.Broker.CreateOrder(trade);
				}
			}
		}

		/// <summary>Return the price bounding spanned by 'price0' and 'price1'</summary>
		private QuoteCurrency? PriceBoundary(PriceTick old, PriceTick nue)
		{
			// Convert to pips
			var pips0 = old.Mid / Instrument.PipSize;
			var pips1 = nue.Mid / Instrument.PipSize;

			// Quantise to the boundary size
			var quantised0 = (int)(pips0 / BoundarySizePips);
			var quantised1 = (int)(pips1 / BoundarySizePips);

			// Rising price
			if (quantised0 < quantised1)
				return quantised1 * (double)BoundarySizePips * Instrument.PipSize;

			// Falling price
			if (quantised0 > quantised1)
				return quantised0 * (double)BoundarySizePips * Instrument.PipSize;

			// Not spanning a boundary
			return null;
		}

		/// <summary>When a position closes</summary>
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
		}

		/// <summary>Close all positions on shutdown</summary>
		protected override void OnBotStopping()
		{
			foreach (var pos in Positions)
				Bot.Broker.ClosePosition(pos);

			base.OnBotStopping();
		}
	}
}

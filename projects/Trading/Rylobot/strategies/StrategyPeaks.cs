using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyPeaks :Strategy
	{
		// Notes:
		//  - Use the peak detector to identify trends
		//  - fit curves to the peaks
		//  - open trades when price reaches the curve
		//  - close when trades reach the other curve

		public StrategyPeaks(Rylobot bot, double risk)
			:base(bot, "StrategyPeaks", risk)
		{
			Debugging.DumpInstrument += Dump;
		}
		public override void Dispose()
		{
			Debugging.DumpInstrument -= Dump;
			base.Dispose();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				// Potential price peaks
				var pp = new PricePeaks(Instrument, 0);
				if (pp.PeakCount < 4 || pp.PeakGap < 2.0 * Instrument.MCS)
					return 0.0;

				// Break-outs are top priority
				if (pp.IsBreakOut)
					return 1.0;

				// The peaks indicate a trend
				return pp.Strength;
			}
		}

		/// <summary>Debugging output</summary>
		public override void Dump()
		{
			Debugging.CurrentPrice(Instrument);
			Debugging.Dump(Instrument, range:new Range(-100, 1), high_res:20.0);
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			if (Instrument.NewCandle)
				Dump();

			// Don't open new positions while there are pending orders or existing positions
			if (Positions.Any() || PendingOrders.Any())
				return;

			// Look for peak patterns
			TradeType tt;
			QuoteCurrency ep;
			var pat = Instrument.IsPeakPattern(Instrument.IdxNow, out tt, out ep);
			if (pat != null)
			{
				var pattern = pat.Value;

				Dump();
				Debugging.Dump(new PricePeaks(Instrument, 0));
				Debugging.Dump(new SnR(Instrument));
				Debugging.Trace("Peak pattern: {0}".Fmt(pattern));

				var sign = tt.Sign();
				var mcs = Instrument.MCS;

				// Convert the patterns to trades
				switch (pattern)
				{
				case EPeakPattern.BreakOutHigh:
				case EPeakPattern.BreakOutLow:
					{
						// For break outs, enter immediately and use a candle
						// follow position manager because they tend to run.
						var price_range = Instrument.PriceRange(-10, 1);
						var sl = price_range.Mid - sign * price_range.Size * 0.6;
						var tp = (QuoteCurrency?)null;
						var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);
						var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol);
						var pos = Broker.CreateOrder(trade);
						if (pos != null)
							PositionManagers.Add(new PositionManagerCandleFollow(this, pos, 5));

						break;
					}
				case EPeakPattern.HighReversal:
				case EPeakPattern.LowReversal:
					{	
						// For reversals, enter when the price is near the trend line.
						// Use a fixed SL/TP
						var price_range = Instrument.PriceRange(-10, 1);
						var sl = price_range.Mid - sign * price_range.Size * 0.6;
						var tp = price_range.Mid + sign * price_range.Size * 0.4;
						var vol = Broker.ChooseVolume(Instrument, Math.Abs(ep - sl), risk:Risk);

						// If the current price is better than the entry price, enter immediately
						if (sign * (ep - Instrument.CurrentPrice(sign)) > 0)
						{
							var trade = new Trade(Instrument, tt, Label, ep, sl, tp, vol);
							Broker.CreateOrder(trade);
						}
						else
						{
							var order = new Trade(Instrument, tt, Label, ep, sl, tp, vol) { Expiration = Instrument.ExpirationTime(1) };
							Broker.CreatePendingOrder(order);
						}
						break;
					}
				}
			}
		}

		protected override void OnPositionOpened(Position position)
		{
		}
		protected override void OnPositionClosed(Position position)
		{
		}

		/// <summary>Watch for bot stopped</summary>
		protected override void OnBotStopping()
		{
			base.OnBotStopping();

			// Log the whole instrument
			Debugging.DebuggingEnabled = true;
			Debugging.ReportEdge(100);

			var ldr = new pr.ldr.LdrBuilder();
			foreach (var trade in Debugging.AllTrades.Values)
			{
				Debugging.Dump(trade, ldr_:ldr);
				Debugging.Dump(new PricePeaks(Instrument, trade.EntryIndex + Instrument.IdxFirst), ldr_:ldr);
			}
			ldr.ToFile(Debugging.FP("trades.ldr"));

			Debugging.Dump(Instrument);
			Debugging.DebuggingEnabled = false;
		}
	}
}

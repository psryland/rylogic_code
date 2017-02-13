using System;
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
		//

		public StrategyHedge(Rylobot bot)
			:base(bot, "StrategyHedge")
		{
			Buy = Bot.Positions.Find(BuyLabel);
			Sel = Bot.Positions.Find(SelLabel);
		}

		/// <summary>Hedged trades</summary>
		public Position Buy { get; private set; }
		public Position Sel { get; private set; }

		/// <summary>Labels for the buy/sell positions</summary>
		private string BuyLabel { get { return Label + "_buy"; } }
		private string SelLabel { get { return Label + "_sel"; } }

		/// <summary>Net winnings</summary>
		private AcctCurrency BuyNett { get; set; }
		private AcctCurrency SelNett { get; set; }

		/// <summary>Return the suitability of this strategy</summary>
		public override double SuitabilityScore
		{
			get
			{
				// Aspect ratio of the bounding area
				var avr = new AvrVarMinMax();
				foreach (var candle in Instrument.CandleRange(-100, 0))
					avr.Add(candle.Median);

				var mcs = Instrument.MedianCandleSize(-100, 0);
				var aspect = 100 / (avr.Range.Size / mcs);
				return base.SuitabilityScore;
			}
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();

			if (BuyNett > 0 && -Sel.NetProfit < 0.5 * BuyNett)
				Sel = Bot.Broker.ClosePosition(Sel);
			if (SelNett > 0 && -Buy.NetProfit < 0.5 * SelNett)
				Buy = Bot.Broker.ClosePosition(Buy);

			if (Buy == null || Sel == null)
				CreateHedge();
		}

		/// <summary>When a position closes</summary>
		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);
			Dump();

			if (Buy != null && Buy.Id == position.Id)
				BuyNett += position.NetProfit;
			if (Sel != null && Sel.Id == position.Id)
				SelNett += position.NetProfit;

			CreateHedge();
		}

		/// <summary>Create/maintain the hedged trades</summary>
		private void CreateHedge()
		{
			try
			{
				var ask = Instrument.CurrentPrice(+1);
				var bid = Instrument.CurrentPrice(-1);
				var mid = Instrument.CurrentPrice(0);
				var mcs = Instrument.MCS_50;
				var sym = Instrument.Symbol;
				var volume = sym.VolumeMin;
				if (Instrument.Spread > mcs/2)
					return;

				// Pick the hi and lo levels
				var hi = Misc.Max(mid + 2*mcs, Buy != null ? Buy.BreakEven(1.0) + mcs : mid);
				var lo = Misc.Min(mid - 2*mcs, Sel != null ? Sel.BreakEven(1.0) - mcs : mid);

				using (Bot.Broker.SuspendRiskCheck())
				{
					// Ensure the hedge trades exist
					Buy = FindLivePosition(Buy);
					if (Buy == null)
					{
						var trade = new Trade(Instrument, TradeType.Buy, BuyLabel, ask, lo, hi, volume);
						Buy = Bot.Broker.CreateOrder(trade);
					}
					Sel = FindLivePosition(Sel);
					if (Sel == null)
					{
						var trade = new Trade(Instrument, TradeType.Sell, SelLabel, bid, hi, lo, volume);
						Sel = Bot.Broker.CreateOrder(trade);
					}

					// Adjust the SL/TP levels
					{
						var trade = new Trade(Instrument, Buy);
						trade.SL = lo - mcs/2;
						trade.TP = hi - mcs/2;
						Bot.Broker.ModifyOrder(Buy, trade, rethrow:true);
					}
					{
						var trade = new Trade(Instrument, Sel);
						trade.SL = hi + mcs/2;
						trade.TP = lo + mcs/2;
						Bot.Broker.ModifyOrder(Sel, trade, rethrow:true);
					}
				}

				Dump();
			}
			catch
			{
				// Close out on failure
				CloseOut();
			}
		}

		/// <summary>Close the hedged trade</summary>
		private void CloseOut()
		{
			Buy = Bot.Broker.ClosePosition(Buy);
			Sel = Bot.Broker.ClosePosition(Sel);
		}

		/// <summary>Debugging output</summary>
		private void Dump()
		{
			//if (!Debugger.IsAttached) return;
			//Debugging.Dump(Instrument, high_res:20);
			//var ldr = new pr.ldr.LdrBuilder();
			//using (ldr.Group("active", Debugging.ScaleTxfm))
			//{
			//	if (Buy != null) Debugging.Dump(Buy, Instrument, ldr_:ldr);
			//	if (Sel != null) Debugging.Dump(Sel, Instrument, ldr_:ldr);
			//}
			//ldr.ToFile(Debugging.FP("hedge.ldr"));
		}
	}
}

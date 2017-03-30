using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.gfx;
using pr.maths;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategySpike :Strategy
	{
		// Notes:
		// - Wait for marubozu candles
		// - Run on 1min data

		public StrategySpike(Rylobot bot, double risk)
			:base(bot, "StrategySpike", risk)
		{
			Periods = 5;
			XShift = 1.5;
			HighResCandleWidth = 0.5;
			EMA0 = new ExpMovingAvr(Periods);
			Debugging.DumpInstrument += Dump;
		}
		public override void Dispose()
		{
			Debugging.DumpInstrument -= Dump;
			base.Dispose();
		}

		/// <summary>Price moving average</summary>
		private ExpMovingAvr EMA0 { get; set; }

		/// <summary>EMA periods</summary>
		private int Periods { get; set; }

		/// <summary>EMA X shift</summary>
		private double XShift { get; set; }

		/// <summary>The width of the high res candle</summary>
		private double HighResCandleWidth { get; set; }

		/// <summary>Output the current state</summary>
		public override void Dump()
		{
			Debugging.Dump(Instrument, range:new Range(-100,1), high_res:20.0);
			//Debugging.Dump(EMA0.Extrapolate().Curve, "ema0", Colour32.Green, new RangeF(-5.0, 5.0));
		}

		/// <summary>Update the EP/SL/TP in 'buy','sel' to span the current price</summary>
		private void MakeSpanningTrades(Trade buy, Trade sel)
		{
			var price = EMA0.Mean;
			var ep_buy = price + Instrument.Spread + 0.25*Instrument.MCS;
			var ep_sel = price - 0.25*Instrument.MCS;

			var tp_rel = 1.0*Instrument.MCS;
			var sl_rel = 1.0*Instrument.MCS + Instrument.Spread;
			var vol    = Broker.ChooseVolume(Instrument, sl_rel, risk:0.5);

			buy.EP = ep_buy;
			buy.SL = ep_buy - sl_rel;
			buy.TP = ep_buy + tp_rel;
			buy.Volume = vol;
			
			sel.EP = ep_sel;
			sel.SL = ep_sel + sl_rel;
			sel.TP = ep_sel - tp_rel;
			sel.Volume = vol;
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			base.Step();
			Dump();

			EMA0.Add(Instrument.LatestPrice.Mid);
			var position = Positions.FirstOrDefault();
			var buy = PendingOrders.FirstOrDefault(x => x.TradeType == TradeType.Buy);
			var sel = PendingOrders.FirstOrDefault(x => x.TradeType == TradeType.Sell);

			// If a pending order is triggered cancel the other one
			if (position != null)
			{
				if (buy != null) Broker.CancelPendingOrder(buy);
				if (sel != null) Broker.CancelPendingOrder(sel);
				buy = null;
				sel = null;

				if (position.NetProfit > Broker.Balance * 0.001)
					Broker.ModifyOrder(Instrument, position, sl:position.EntryPrice + position.Sign() * 2.0 * Instrument.Spread);
			}

			// Update the pending orders with each tick
			else if (buy != null && sel != null)
			{
				var buy_ = new Trade(Instrument, buy);
				var sel_ = new Trade(Instrument, sel);
				MakeSpanningTrades(buy_, sel_);
				Broker.ModifyPendingOrder(buy, buy_);
				Broker.ModifyPendingOrder(sel, sel_);
			}

			// If there are no positions or pending orders, create them
			else if (position == null && buy == null && sel == null)
			{
				var buy_ = new Trade(Instrument, TradeType.Buy,  Label, 0, null, null, 0);
				var sel_ = new Trade(Instrument, TradeType.Sell, Label, 0, null, null, 0);
				MakeSpanningTrades(buy_, sel_);
				Broker.CreatePendingOrder(buy_);
				Broker.CreatePendingOrder(sel_);
			}
		}

		/// <summary>Watch for pending order filled</summary>
		protected override void OnPositionOpened(Position position)
		{
			Dump();
		}

		/// <summary>Watch for position closed</summary>
		protected override void OnPositionClosed(Position position)
		{
			EMA0.Reset(EMA0.WindowSize);
			EMA0.Add(Instrument.LatestPrice.Mid);
			Dump();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public override double SuitabilityScore
		{
			get
			{
				return Instrument.MCS > 5.0 * Instrument.Spread ? 1.0 : 0.0;
			//	var candle = Instrument.HighResCandle(Instrument.IdxNow - HighResCandleWidth, Instrument.IdxNow);
			//	return candle.BodyLength > 1.0 * Instrument.MCS ? 1.0 : 0.0;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	public class StrategyRevenge :Strategy
	{
		// Note:
		//  - Make a trade
		//  - If it goes the wrong way, make another one in the opposite direction for twice the volume
		//  - repeat.
		// Only if price is moving by more than the mean candle size
		// Only if price is within one sigma of the mean

		private const int HistoryWindow = 100;

		public StrategyRevenge(Rylobot bot)
			:base(bot, "StrategyRevenge")
		{
			ReversesCount = 3;
			TakeProfitPC = 0.001;
			Positions = new List<Position>();
			PriceStats = new ExpMovingAvrMinMax();

			// Initialise the stats
			foreach (var candle in Instrument.CandleRange())
			{
				PriceStats.Add(candle.Open);
				PriceStats.Add(candle.High);
				PriceStats.Add(candle.Low);
				PriceStats.Add(candle.Close);
			}
		}

		/// <summary>The number of times to create a reverse trade before giving up</summary>
		private int ReversesCount
		{
			get;
			set;
		}

		/// <summary>The percentage of current balance to cash in at</summary>
		private double TakeProfitPC
		{
			get;
			set;
		}

		/// <summary>Trades created by this strategy</summary>
		private List<Position> Positions
		{
			get;
			set;
		}

		/// <summary>Statistics for the recent price history</summary>
		private ExpMovingAvrMinMax PriceStats
		{
			get;
			set;
		}

		/// <summary>The balance to risk for the current set of trades</summary>
		private AcctCurrency BalanceRisked
		{
			get;
			set;
		}

		/// <summary>The combined position of all positions created by this strategy</summary>
		private AcctCurrency NetPosition
		{
			get { return Positions.Sum(x => x.NetProfit); }
		}

		public void Step2()
		{
			// Look for an entry trigger (candle pattern, SnR levels, next price vs. prev using quadratic etc)

			// Create a trade.
			// Set the SL to be > 2*mcs and higher than any recent peaks
			// Set the TP to be between [-SL, -2*SL] or at any significant SnR level


		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			var current_price = Instrument.CurrentPrice(0);
			var sym = Instrument.Symbol;

			// Find the stats for the recent price history
			PriceStats.Add((double)current_price);

			// Synchronise the Positions with our list
			var live_positions = Bot.Positions.ToHashSet(x => x.Id);
			Positions.RemoveIf(x => !live_positions.Contains(x.Id));

			// Measure the current net position
			var net = NetPosition;

			// If the sum of all trades is greater than the threshold, cash in
			// If the sum of all trades exceeds 90% of the balance to risk, bail out
			var profit_threshold = Misc.Max(1.0, Bot.Broker.Balance * TakeProfitPC);
			if (net > profit_threshold)
			{
				CloseOut();
				return; // Return so that closed positions get output while debugging
			}
			if (net < -BalanceRisked)
			{
				CloseOut();
				return; // Return so that closed positions get output while debugging
			}

			// Attempt to compress the positions
			for (;Positions.Count > 2;)
			{
				var pos0 = Positions.Back(0);
				var pos1 = Positions.Back(1);
				if (pos0.NetProfit + pos1.NetProfit > 1.0)
				{
					Bot.Broker.ClosePosition(pos0);
					Bot.Broker.ClosePosition(pos1);
					continue;
				}
				break;
			}

			// If we have no position, make a trade
			if (Positions.Count == 0)
			{
				// Get the current price direction
				var q = Quadratic.FromPoints(
					new v2(-2f, (float)Instrument[-2].Median),
					new v2(-1f, (float)Instrument[-1].Median),
					new v2(-0f, (float)Instrument[-0].Median));
				var next_price =(QuoteCurrency)q.F(1.0);

				var tt = next_price > current_price ? TradeType.Buy : TradeType.Sell;
				var sign = tt.Sign();

				// Create the initial trade
				var volume = sym.VolumeMin;
				var risk = Bot.Broker.MaxSL(Instrument, volume);
				var ep = Instrument.CurrentPrice(sign);
				var tp = ep + sign * risk;
				var sl = ep - sign * risk;
				var trade = new Trade(Instrument, tt, Label, ep, sl, tp, volume);

				// Record how much we're risking on this set of trades
				BalanceRisked = sym.QuoteToAcct(risk * volume);

				// Open the position
				var pos = Bot.Broker.CreateOrder(trade);
				if (pos != null)
					Positions.Add(pos);
			}
			else
			{
				// Get the loss threshold given the current number of trades
				var loss_per_level = BalanceRisked / ReversesCount;
				var loss_threshold = Positions.Count * loss_per_level;

				// If the net profit has dropped below the threshold, open a reverse trade
				if (net < -loss_threshold)
				{
					// The reverse trade is created such that the total risk remains the same.
					var prev = Positions.Back();
					var volume = sym.NormalizeVolume(prev.Volume * (Positions.Count + 1) / Positions.Count);
					var tt = prev.TradeType.Opposite();
					var ep = Instrument.CurrentPrice(tt.Sign());
					var tp = prev.StopLossAbs();
					var sl = prev.TakeProfitAbs();
					var trade = new Trade(Instrument, tt, Label, ep, sl, tp, volume);

					// Open the reverse position
					var pos = Bot.Broker.CreateOrder(trade);
					if (pos != null)
						Positions.Add(pos);
				}
			}

			int break_point;
			if (Instrument.NewCandle)
				break_point = 1;
		}

		/// <summary>Close all positions</summary>
		private void CloseOut()
		{
			foreach (var pos in Positions)
				Bot.Broker.ClosePosition(pos);

			Positions.Clear();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.0; // not implemented 
		}
	}
}

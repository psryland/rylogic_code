using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using cAlgo.API.Indicators;

namespace Rylobot
{
	/// <summary></summary>
	public class StrategyTail :Strategy
	{
		private ExponentialMovingAverage m_ema;

		public StrategyTail(Rylobot bot)
			:base(bot, "StrategyTail")
		{
			m_ema = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, 100);
		}

		/// <summary>Called when new data is received</summary>
		public override void Step()
		{
			// One trade at a time
			if (Position != null)
				return;

			// Only on new candles
			if (!Instrument.NewCandle)
				return;

			var dp = m_ema.Result.FirstDerivative(m_ema.Result.Count - 2);
			var ddp = m_ema.Result.SecondDerivative(m_ema.Result.Count - 2);

			if (dp > Instrument.Symbol.PipSize * 0 && //ddp > 0 &&      // Positive slope and rising
				Instrument.CurrentPrice(+1) < m_ema.Result.LastValue) // Price below EMA
			{
				Debugging.Trading.Begin();
				Debugging.Trading.Comment("Price is below a rising EMA");
				Debugging.Trading.SnrLevels(new SnR(Instrument, -100, Instrument.LastIdx));

				var trade = new Trade(Bot, Instrument, TradeType.Buy, Label);
				Bot.Broker.CreateOrder(trade);
				return;
			}

			if (dp < -Instrument.Symbol.PipSize * 0 && //ddp < 0 &&     // Negative slope and falling
				Instrument.CurrentPrice(-1) > m_ema.Result.LastValue) // Price above EMA
			{
				Debugging.Trading.Begin();
				Debugging.Trading.Comment("Price is above a falling EMA");
				Debugging.Trading.SnrLevels(new SnR(Instrument, -100, Instrument.LastIdx));

				var trade = new Trade(Bot, Instrument, TradeType.Sell, Label);
				Bot.Broker.CreateOrder(trade);
				return;
			}
		}

		protected override void OnPositionClosed(Position position)
		{
			base.OnPositionClosed(position);

			Debugging.Trading.Position(position, Bot);
			Debugging.Trading.Instrument(Instrument, position, diagnostic:true);
			Debugging.Trading.End();
		}

		/// <summary>Return a score for how well suited this strategy is to the current conditions</summary>
		public static double FitnessScore(Rylobot bot)
		{
			return 0.0; // not implemented 
		}
	}
}

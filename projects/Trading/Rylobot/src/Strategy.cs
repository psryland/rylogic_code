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
using pr.util;

namespace Rylobot
{
	public abstract class Strategy :IDisposable
	{
		// Notes:
		// - Strategies should be resume-able, i.e on start they should look for
		//   existing positions associated with the strategy and continue using them.
		//   This is so startup/shutdown have very little effect on the running of the bot.

		public Strategy(Rylobot bot, string label, double risk)
		{
			Bot = bot;
			Label = label;
			Suitability = new List<Vec2d>();
			Correlator = new Correlator(label);
			Risk = risk;

			Debugging.DumpInstrument += Dump;
		}
		public virtual void Dispose()
		{
			Debugging.DumpInstrument -= Dump;

			Bot = null;
		}

		/// <summary>A label for positions created by this strategy</summary>
		public string Label
		{
			get;
			private set;
		}

		/// <summary>Application logic</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.Tick -= HandleTick;
					m_bot.Stopping -= HandleBotStopping;
					m_bot.Positions.Closed -= HandlePositionClosed;
					m_bot.Positions.Opened -= HandlePositionOpened;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.Positions.Opened += HandlePositionOpened;
					m_bot.Positions.Closed += HandlePositionClosed;
					m_bot.Stopping += HandleBotStopping;
					m_bot.Tick += HandleTick;
				}
			}
		}
		private Rylobot m_bot;

		/// <summary>The main instrument for this bot</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return Bot.Instrument; }
		}

		/// <summary>The account broker</summary>
		public Broker Broker
		{
			[DebuggerStepThrough] get { return Bot.Broker; }
		}

		/// <summary>An object for tracking correlation between trades and decision factors</summary>
		public Correlator Correlator
		{
			get;
			private set;
		}

		/// <summary>
		/// The fraction of the allowed account risk assigned to this strategy.
		/// The sum of Risk for all active strategies should add up to 1.0</summary>
		public double Risk
		{
			get;
			private set;
		}

		/// <summary>The net equity as a result of all trades created by this strategy</summary>
		public AcctCurrency Equity
		{
			get { return Broker.Balance + Broker.NetProfit(Label); }
		}


		#region Signals

		/// <summary>Returns the trade direction of the current trend</summary>
		protected TradeType? SignalSlope(int periods)
		{
			// Price
			var ema = Bot.Indicators.ExponentialMovingAverage(Bot.MarketSeries.Close, periods);

			// Measure the average slope over the window
			var slope = new ExpMovingAvr(periods);
			for (int i = 0; i != periods; ++i)
				slope.Add(ema.Result.FirstDerivative(-i) / Instrument.Symbol.PipSize);

			// Wait for significant slope
			var SlopeThreshold = 0.8; // pips / candle
			return
				slope.Mean > +SlopeThreshold ? TradeType.Buy :
				slope.Mean < -SlopeThreshold ? TradeType.Sell :
				(TradeType?)null;
		}

		/// <summary>Trade entry signal based on MACD</summary>
		protected TradeType? SignalMACD()
		{
			// Look for minima or maxima in the short period average
			var macd = Bot.Indicators.MacdCrossOver(26, 12, 9);

			// Fit a quadratic to the MACD line (blue one)
			var poly = Quadratic.FromPoints(
				-2.0, macd.MACD.Last(2),
				-1.0, macd.MACD.Last(1),
				-0.0, macd.MACD.Last(0));

			// If the quadratic predicts a peak it might be time to buy/sell.
			var peak_x = poly.StationaryPoints[0];
			if (peak_x < -1.0 || peak_x > 0.0)
				return null;

			var blue = macd.MACD.LastValue;
			var red  = macd.Signal.LastValue;
			for (;;)
			{
				var minima = poly.ddF(peak_x) > 0;
				if (minima) // buy?
				{
					// MACD must be below the signal line
					if (blue > red)
						break;

					return TradeType.Buy;
				}
				else // sell?
				{
					// MACD must be above the signal line
					if (blue < red)
						break;

					return TradeType.Sell;
				}
			}
			return null;
		}

		#endregion

		public static class CorrFactor
		{
			public const string SL = "SL";
			public const string TP = "TP";
			public const string RtR = "RtR";
			public const string Volume = "Volume";
		}
	}
}

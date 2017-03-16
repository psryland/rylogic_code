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

		public Strategy(Rylobot bot, string label)
		{
			Bot = bot;
			Label = label;
			Suitability = new List<Vec2d>();
			PositionManagers = new List<PositionManager>();
			Correlator = new Correlator(label);
		}
		public virtual void Dispose()
		{
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

		/// <summary>The current signed net position volume</summary>
		public long NetVolume
		{
			get { return Broker.NetVolume(Label); }
		}

		/// <summary>The direction of increasing profit</summary>
		public int ProfitSign
		{
			get { return Math.Sign(NetVolume); }
		}

		/// <summary>The net equity as a result of all trades created by this strategy</summary>
		public AcctCurrency Equity
		{
			get { return Broker.Balance + Broker.NetProfit(Label); }
		}

		/// <summary>How well this strategy applies to the data</summary>
		public virtual double SuitabilityScore
		{
			get { return 0.0; }
		}
		private List<Vec2d> Suitability;

		/// <summary>Step the strategy. Note: Instruments are signed up to Bot.Tick, they will be updated before Strategy.Step() is called.</summary>
		public virtual void Step()
		{
			if (Instrument.NewCandle)
			{
			//	// Track suitability
			//	var x = Instrument.IndexAt(Bot.UtcNow) - Instrument.IdxFirst;
			//	Suitability.Add(new Vec2d(x, SuitabilityScore));
			}

			// Step active position managers
			foreach (var pm in PositionManagers)
				pm.Step();
		}

		/// <summary>Called just before the bot stops</summary>
		protected virtual void OnBotStopping()
		{
			//{// Output instrument
			//	var ldr = new pr.ldr.LdrBuilder();
			//	using (ldr.Group(string.Empty, Debugging.ScaleTxfm))
			//		Debugging.Dump(Instrument, emas:new[] {100,55}, ldr_:ldr);
			//	ldr.ToFile(Debugging.FP("{0}_{1}.ldr".Fmt(Label, Instrument.SymbolCode)));
			//}
			//{// Output suitability
			//	var range = RangeF.Invalid;
			//	foreach (var c in Instrument.CandleRange())
			//	{
			//		range.Encompass(c.Low);
			//		range.Encompass(c.High);
			//	}

			//	var ldr = new pr.ldr.LdrBuilder();
			//	using (ldr.Group(string.Empty, Debugging.ScaleTxfm))
			//	{
			//		var suitability = Suitability.Select(x => new v4((float)x.x, (float)(x.y * range.Size + range.Begin), 0.05f, 1f));
			//		ldr.Line("suitability", 0xFF8000FF, 1, suitability);
			//		ldr.Line("suitability_0", 0xFF8000A0, new v4(0f, (float)range.Begin, 0.05f, 1f), new v4(Instrument.Count, (float)range.Begin, 0.05f, 1f));
			//		ldr.Line("suitability_v", 0xFF8020F0, new v4(0f, (float)range.Mid  , 0.05f, 1f), new v4(Instrument.Count, (float)range.Mid  , 0.05f, 1f));
			//		ldr.Line("suitability_1", 0xFF8000A0, new v4(0f, (float)range.End  , 0.05f, 1f), new v4(Instrument.Count, (float)range.End  , 0.05f, 1f));
			//	}
			//	ldr.ToFile(Debugging.FP("{0}_suitability.ldr".Fmt(Label)));
			//}
		}

		/// <summary>Active position managers</summary>
		public List<PositionManager> PositionManagers
		{
			get;
			private set;
		}

		/// <summary>Return the live positions created by this strategy</summary>
		public IEnumerable<Position> Positions
		{
			get { return Bot.Positions.Where(x => x.Label == Label); }
		}

		/// <summary>Return pending orders created by this strategy</summary>
		public IEnumerable<PendingOrder> PendingOrders
		{
			get { return Bot.PendingOrders.Where(x => x.Label == Label); }
		}

		/// <summary>Return the position associated with 'id' or null</summary>
		protected Position FindLivePosition(int id)
		{
			return Bot.Positions.FirstOrDefault(x => x.Id == id);
		}
		protected Position FindLivePosition(string label)
		{
			return Bot.Positions.FirstOrDefault(x => x.Label == label);
		}
		protected Position FindLivePosition(Position pos)
		{
			if (pos == null) return null;
			return FindLivePosition(pos.Id);
		}

		/// <summary>Called when a position is opened</summary>
		private void HandlePositionOpened(PositionOpenedEventArgs args)
		{
			var position = args.Position;

			Debugging.Trace("Idx={0},Tick={1} - Position Opened - {2} EP={3} Volume={4}".Fmt(Instrument.Count, Bot.TickNumber, position.TradeType, position.EntryPrice, position.Volume));

			Correlator.Track(position, CorrFactor.SL, position.StopLossRel());
			Correlator.Track(position, CorrFactor.TP, position.TakeProfitRel());
			Correlator.Track(position, CorrFactor.RtR, position.TakeProfitRel() / position.StopLossRel());
			Correlator.Track(position, CorrFactor.Volume, position.Volume);

			// Notify position closed
			OnPositionOpened(args.Position);
		}

		/// <summary>Called when a position is opened</summary>
		protected virtual void OnPositionOpened(Position position)
		{}

		/// <summary>Called when a position closes</summary>
		private void HandlePositionClosed(PositionClosedEventArgs args)
		{
			var position = args.Position;

			Debugging.Trace("Idx={0},Tick={1} - Position Closed - {2} EP={3} Volume={4} Profit=${5} Equity=${6}".Fmt(Instrument.Count, Bot.TickNumber, position.TradeType, position.EntryPrice, position.Volume, position.NetProfit, Equity));

			// Track the trade result
			Correlator.Result(position);

			// Close any position managers that are managing 'position'
			PositionManagers.RemoveIf(x => x.Position == position);

			// Notify position closed
			OnPositionClosed(position);
		}

		/// <summary>Called when the current position closes</summary>
		protected virtual void OnPositionClosed(Position position)
		{}

		/// <summary>Called just before the bot stops</summary>
		private void HandleBotStopping(object sender, EventArgs e)
		{
			foreach (var pos in Positions)
				Correlator.Result(pos);

			Debugging.Dump(Correlator);

			// Notify stopping
			OnBotStopping();
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

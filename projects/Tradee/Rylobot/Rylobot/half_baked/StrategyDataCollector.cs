using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylobot
{
	public class StrategyDataCollector :Strategy
	{
		// Notes:
		// This is not a trading strategy, but a helper for collecting trading data.
		// Given a range of trading data it generates a set of the ideal trades for use in 
		// training a neural net.

		/// <summary>Training data to Testing data size ratio</summary>
		private const int TrainToTestRatio = 10;

		/// <summary>Output streams for the training and testing data</summary>
		private StreamWriter m_training;
		private StreamWriter m_testing;

		public StrategyDataCollector(Rylobot bot)
			:base(bot, "StrategyDataCollector")
		{
			NNet = new PredictorNeuralNet(bot);

			WindowSize = 20;
			RtRThreshold = 2.0;

			// Generate the training and test data
			var outdir = @"P:\projects\Tradee\Rylobot\Rylobot\net\Data";
			m_training = new StreamWriter(Path_.CombinePath(outdir, "training.txt"));
			m_testing  = new StreamWriter(Path_.CombinePath(outdir, "testing.txt"));
		}

		/// <summary>A predictor used for entry point and direction signalling</summary>
		public PredictorNeuralNet NNet
		{
			[DebuggerStepThrough] get { return m_pred_nnet; }
			private set
			{
				if (m_pred_nnet == value) return;
				if (m_pred_nnet != null)
				{
					Util.Dispose(ref m_pred_nnet);
				}
				m_pred_nnet = value;
				if (m_pred_nnet != null)
				{
				}
			}
		}
		private PredictorNeuralNet m_pred_nnet;

		/// <summary>The maximum trade length in candles</summary>
		public int WindowSize { get; private set; }

		/// <summary>The minimum reward to risk ratio</summary>
		public double RtRThreshold { get; private set; }

		/// <summary></summary>
		public override void Step()
		{
			// Avoid boundary cases in the data
			if (Instrument.Count < PredictorNeuralNet.HistoryWindow + WindowSize)
				return;

			// Only do this per candle
			if (!Instrument.NewCandle)
				return;

			// At a candle close:
			//  Record as many indicator features as I can think of
			//  Imagine a trade of both directions was created at the open on this new candle
			//  See how the trades plays out over the next WindowSize candles
			//  Output 'buy' 'sell' or 'no trade' based on the trade result
			// This means the training data will contain the state of all indicators at this
			// candle close, and the ideal trade to have taken at that point.

			// Use a position in the past as the 'current' position so that we can
			// use the future candle data to see what actually happens.
			// 'index - 1' is the candle that just closed, 'index' is the new candle
			var index = Instrument.Count - WindowSize;
			NegIdx neg_idx = Instrument.FirstIdx + index;

			// Get features up to the last closed candle, since we don't know anything about 'index' yet
			var features = NNet.Features;

			// Create a buy and sell trade at the open of the new candle
			// Use the Broker's ChooseSL/TP so that the trades are realistic
			var entry = Instrument[neg_idx];
			var buy = new Trade(Bot, Instrument, TradeType.Buy , neg_idx:neg_idx);
			var sel = new Trade(Bot, Instrument, TradeType.Sell, neg_idx:neg_idx);

			// See how each trade plays out over the next WindowSize candles
			for (var i = neg_idx; i != Instrument.LastIdx; ++i)
			{
				var c = Instrument[i];
				buy.AddCandle(c, i);
				sel.AddCandle(c, i);
			}

			// Select the best trade as the desired output for the given features
			var is_buy = buy.PeakRtR > sel.PeakRtR && buy.Result != Trade.EResult.Open;
			var is_sel = sel.PeakRtR > buy.PeakRtR && sel.Result != Trade.EResult.Open;
			var labels = "|labels {0} {1} {2} ".Fmt(is_buy?1:0, is_sel?1:0, !(is_buy||is_sel)?1:0);

			// Select the file to write to
			var fs = (index % TrainToTestRatio) == 0 ? m_testing : m_training;

			// Output the labels
			fs.Write(labels);

			// Write the feature string (last so the training data is easier to read)
			fs.Write(features);

			// New line
			fs.WriteLine();

			// Write the results to a file
			if (cooldown == 0)
			{
				if (m_ldr == null)
				{
					m_ldr = new pr.ldr.LdrBuilder();
					m_grp = m_ldr.Group("Training");
				}
				if (buy.PeakRtR > sel.PeakRtR)
					Debugging.Dump(buy, ldr_:m_ldr);
				else
					Debugging.Dump(sel, ldr_:m_ldr);

				cooldown = 20;
			}
			else
			{
				--cooldown;
			}
		}
		private pr.ldr.LdrBuilder m_ldr;
		private Scope m_grp;
		private int cooldown;

		/// <summary></summary>
		protected override void OnBotStopping()
		{
			if (m_ldr != null)
			{
				Util.Dispose(ref m_grp);
				m_ldr.ToFile(Debugging.FP("training.ldr"));
			}
			Instrument.Dump();

			Util.Dispose(ref m_training);
			Util.Dispose(ref m_testing );

			base.OnBotStopping();
		}
	}
}

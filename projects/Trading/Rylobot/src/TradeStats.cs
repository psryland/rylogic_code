using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary>A class for correlating factors with trade success</summary>
	public class TradeStats :IDisposable
	{
		public TradeStats(Rylobot bot)
		{
			Bot = bot;
			Records = new RecordMap();
			Predictors = new Dictionary<string, Predictor>();
		}
		public virtual void Dispose()
		{
			Bot = null;
		}

		/// <summary>A name for the correlator</summary>
		public Rylobot Bot
		{
			[DebuggerStepThrough] get { return m_bot; }
			private set
			{
				if (m_bot == value) return;
				if (m_bot != null)
				{
					m_bot.PositionClosed -= HandlePositionClosed;
					m_bot.PositionOpened -= HandlePositionOpened;
					m_bot.Stopping -= HandleBotStopping;
				}
				m_bot = value;
				if (m_bot != null)
				{
					m_bot.PositionOpened += HandlePositionOpened;
					m_bot.PositionClosed += HandlePositionClosed;
					m_bot.Stopping += HandleBotStopping;
				}
			}
		}
		private Rylobot m_bot;

		/// <summary>The instrument</summary>
		public Instrument Instrument
		{
			[DebuggerStepThrough] get { return Bot.Instrument; }
		}

		/// <summary>Tracked trades and their associated factors</summary>
		public RecordMap Records { get; private set; }
		public class RecordMap :Dictionary<int, Record> // trade id : record
		{
			public new Record this[int id]
			{
				get { return this.GetOrAdd(id, i => new Record(i)); }
			}
		}

		/// <summary>Record an event in the life cycle of 'pos'</summary>
		public void Event(Position pos, string event_name)
		{
			var rec = Records[pos.Id];
			rec.Events.Add(event_name);
		}

		/// <summary>The factors tracked</summary>
		public Dictionary<string, Predictor> Predictors
		{
			get;
			private set;
		}
		public class Predictor
		{
			private readonly TradeStats m_stats;
			public Predictor(string name, TradeStats stats)
			{
				m_stats = stats;
				Name = name;
				Predictions = new Dictionary<int, Prediction>();
			}

			/// <summary>The name of the predictor</summary>
			public string Name { get; private set; }

			/// <summary>Trade id to prediction for it's sign</summary>
			public Dictionary<int, Prediction> Predictions { get; private set; }

			/// <summary>The distribution of prediction values and prediction accuracy</summary>
			public Distribution Distribution(double bucket_size = 0.001)
			{
				var distr = new Distribution(bucket_size, name:Name);

				// Compile the prediction results
				foreach (var pred in Predictions)
				{
					// Look for the Record associated with the trade id
					var rec = (Record)null;
					if (!m_stats.Records.TryGetValue(pred.Key, out rec))
						continue;

					// See how the prediction went
					var correct = Maths.SignI(pred.Value.Sign == rec.Sign) * Math.Sign((double)rec.NetProfit);

					// Add to the distribution
					distr.Add(pred.Value.Value, correct);
				}

				return distr;
			}
		}

		/// <summary>Track the use of 'value' to predict the sign that 'position' should have</summary>
		public void Track(Position pos, string predictor_name, double value, int predicted_sign)
		{
			var predictor = Predictors.GetOrAdd(predictor_name, n => new Predictor(n, this));
			predictor.Predictions[pos.Id] = new Prediction(value, predicted_sign);
		}

		/// <summary>Calculate profit stats on a collection of records</summary>
		private ResultStats CalcStats(IEnumerable<Record> records)
		{
			var wins = records.Where(x => x.NetProfit >= 0).ToArray();
			var loss = records.Where(x => x.NetProfit <  0).ToArray();
			
			var stats        = new ResultStats();
			stats.Wins       = wins.Length;
			stats.Losses     = loss.Length;
			stats.WinAmount  = wins.Sum(x => (double)x.NetProfit);
			stats.LossAmount = loss.Sum(x => (double)x.NetProfit);
			return stats;
		}

		/// <summary>Output the results of trade result correlated with factors</summary>
		public string Report
		{
			get
			{
				// Perform a correlation between each factor and the trade result
				var sb = new StringBuilder();
				var stats = CalcStats(Records.Values);

				// Add win/loss ratio
				sb.AppendLine("Nett:          {0:C}".Fmt(stats.NetProfit));
				sb.AppendLine("Net Profit:    {0:C}".Fmt(stats.WinAmount));
				sb.AppendLine("Net Loss:      {0:C}".Fmt(stats.LossAmount));
				sb.AppendLine();
				sb.AppendLine("Win Count:     {0}".Fmt(stats.Wins));
				sb.AppendLine("Loss Count:    {0}".Fmt(stats.Losses));
				sb.AppendLine("Win Ratio:     {0:N3}%".Fmt(stats.WinRatioPC));
				sb.AppendLine();
				sb.AppendLine("Avr $/Winner:  {0:C}".Fmt(stats.AvrAmountPerWin));
				sb.AppendLine("Avr $/Loser:   {0:C}".Fmt(stats.AvrAmountPerLoss));
				sb.AppendLine("Avr $/Trade:   {0:C}".Fmt(stats.AvrAmountPerTrade));
				sb.AppendLine();

				// All the unique events recorded
				var events = Records.Values.SelectMany(x => x.Events).Distinct().OrderBy(x => x).ToArray();

				// Position history stats
				sb.AppendLine("Position History Stats:");
				foreach (var evt in events)
				{
					// Get all the records that have this event
					stats = CalcStats(Records.Values.Where(x => x.Events.Contains(evt)));

					sb.AppendLine("{0,-50}: win/loss = {1,3}/{2,-3} ({3,7:C}/{4,-7:C}), net profit = {5,7:C}: ".Fmt(
						evt, stats.Wins, stats.Losses, stats.WinAmount, stats.LossAmount, stats.NetProfit));
				}
				sb.AppendLine();

				// Individual trade log
				sb.AppendLine("Trades:");
				foreach (var trade in Records.Values.OrderBy(x => x.TradeId))
				{
					sb.AppendLine("{0,5:N1} - Id={1,5} - {2,7:C} - {3}".Fmt((double)trade.Index, trade.TradeId, trade.NetProfit, string.Join("->", trade.Events)));
				}
				sb.AppendLine();

				return sb.ToString();
			}
		}

		/// <summary>Handle a position opened</summary>
		private void HandlePositionOpened(object sender, PositionOpenedEventArgs e)
		{
			var pos = e.Position;
			var rec = Records[pos.Id];

			rec.Sign = pos.Sign();
			rec.Index = Instrument.IndexAt(pos.EntryTime) - Instrument.IdxFirst;
			rec.Events.Add("Opened");
		}

		/// <summary>Handle a position closed</summary>
		private void HandlePositionClosed(object sender, PositionClosedEventArgs e)
		{
			var pos = e.Position;
			var rec = Records[pos.Id];
	
			// Record whether a trade was successful or not
			rec.NetProfit = pos.NetProfit;

			if (pos.StopLoss != null && Math.Sign(pos.StopLoss.Value - Instrument.LatestPrice.Price(-pos.Sign())) == pos.Sign())
				rec.Events.Add("Hit SL");
			else if (pos.TakeProfit != null && Math.Sign(Instrument.LatestPrice.Price(pos.Sign()) - pos.TakeProfit.Value) == pos.Sign())
				rec.Events.Add("Hit TP");
			else
				rec.Events.Add("Closed");
		}

		/// <summary>Handle the bot stopping</summary>
		private void HandleBotStopping(object sender, EventArgs e)
		{
		}

		/// <summary>A trade direction prediction</summary>
		public class Prediction
		{
			public Prediction(double value, int sign)
			{
				Value = value;
				Sign = sign;
			}

			/// <summary>The value used to make the prediction</summary>
			public double Value { get; private set; }

			/// <summary>The predicted sign that trade should've had</summary>
			public int Sign { get; private set; }
		}

		/// <summary>Data per trade</summary>
		public class Record
		{
			public Record(int trade_id)
			{
				TradeId = trade_id;
				Events  = new List<string>();
			}

			/// <summary>The trade that</summary>
			public int TradeId { get; private set; }

			/// <summary>The trade direction of the position</summary>
			public int Sign { get; set; }

			/// <summary>The CAlgo index of when the position was opened</summary>
			public Idx Index { get; set; }

			/// <summary>A history of things that were done to the trade</summary>
			public List<string> Events { get; private set; }

			/// <summary>The trade resulting profit</summary>
			public AcctCurrency NetProfit { get; set; }

			/// <summary>Trade success sign</summary>
			public int Success
			{
				get { return NetProfit > 0 ? +1 : NetProfit < 0 ? -1 : 0; }
			}
		}

		/// <summary>Trade result stats</summary>
		public class ResultStats
		{
			public int Wins;
			public int Losses;
			public double WinAmount;
			public double LossAmount;
			public double AvrAmountPerWin
			{
				get { return WinAmount / Wins; }
			}
			public double AvrAmountPerLoss
			{
				get { return LossAmount / Losses; }
			}
			public double AvrAmountPerTrade
			{
				get { return (WinAmount + LossAmount) / (Wins + Losses); }
			}
			public double NetProfit
			{
				get { return WinAmount + LossAmount; }
			}
			public double WinRatioPC
			{
				get { return 100.0 * Wins / (Wins + Losses); }
			}
		}
	}
}

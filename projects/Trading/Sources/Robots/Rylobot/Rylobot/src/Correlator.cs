using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cAlgo.API;
using pr.extn;
using pr.maths;

namespace Rylobot
{
	/// <summary>A class for correlating factors with trade success</summary>
	public class Correlator
	{
		public Correlator(string name)
		{
			Name = name;
			Factors = new HashSet<string>();
			Records = new Dictionary<int, Record>();
		}

		/// <summary>A name for the correlator</summary>
		public string Name { get; private set; }

		/// <summary>The factors tracked</summary>
		public HashSet<string> Factors
		{
			get;
			private set;
		}

		/// <summary>Tracked trades and their associated factors</summary>
		public Dictionary<int, Record> Records { get; private set; }

		/// <summary>Associate a factor with a trade</summary>
		public void Track(Position pos, string factor, double value)
		{
			// Record the factors seen
			Factors.Add(factor);

			// Get the record for 'pos'
			var record = Records.GetOrAdd(pos.Id, id => new Record(id));

			// Add the factor for this position
			record.Factor[factor] = value;
		}

		/// <summary>Record whether a trade was successful or not</summary>
		public void Result(Position pos)
		{
			var record = Records.GetOrAdd(pos.Id, id => new Record(id));
			record.Profit = pos.NetProfit;
		}

		/// <summary>Output the results of trade result correlated with factors</summary>
		public string Report
		{
			get
			{
				// Perform a correlation between each factor and the trade result
				var sb = new StringBuilder();

				var wins = Records.Values.Where(x => x.Profit >= 0).ToArray();
				var loss = Records.Values.Where(x => x.Profit <  0).ToArray();
				var win_total = wins.Sum(x => (double)x.Profit);
				var los_total = loss.Sum(x => (double)x.Profit);

				// Add win/loss ratio
				sb.AppendLine("Net Profit:   ${0:N2}".Fmt(win_total));
				sb.AppendLine("Net Loss:     ${0:N2}".Fmt(los_total));
				sb.AppendLine("Nett:         ${0:N2}".Fmt(win_total + los_total));
				sb.AppendLine("Profit Ratio:  {0:N3}".Fmt(win_total / los_total));
				sb.AppendLine();
				sb.AppendLine("Win Count:     {0}".Fmt(wins.Length));
				sb.AppendLine("Loss Count:    {0}".Fmt(loss.Length));
				sb.AppendLine("Win Ratio:     {0:N3}%".Fmt(100.0 * wins.Length / (wins.Length + loss.Length)));
				sb.AppendLine();

				sb.AppendLine("Correlations:");
				foreach (var factor in Factors)
				{
					var corr = new Correlation();
					foreach (var rec in Records.Values.Where(x => x.Factor.ContainsKey(factor)))
					{
						var value = rec.Factor[factor];
						corr.Add(rec.Success, value);
					}
					sb.AppendLine("{0,-10}: = {1:N3}".Fmt(factor, corr.CorrCoeff));
				}
				return sb.ToString();
			}
		}

		/// <summary></summary>
		public class Record
		{
			public Record(int trade_id)
			{
				TradeId = trade_id;
				Factor = new Dictionary<string, double>();
			}

			/// <summary>The trade that</summary>
			public int TradeId { get; private set; }

			/// <summary>Factors associated with this trade</summary>
			public Dictionary<string, double> Factor { get; private set; }

			/// <summary>The trade resulting profit</summary>
			public AcctCurrency Profit { get; set; }

			/// <summary>Trade was successful</summary>
			public int Success { get { return Profit > 0 ? +1 : Profit < 0 ? -1 : 0; } }
		}
	}
}

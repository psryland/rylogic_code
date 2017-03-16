using System;
using System.Diagnostics;
using cAlgo.API;
using pr.maths;

namespace Rylobot
{
	/// <summary>A wrapper for CAlgo indicators than maps Idx to the data</summary>
	public class Indicator
	{
		private readonly IndicatorDataSeries m_series;
		public Indicator(Instrument instr, IndicatorDataSeries series)
		{
			Instrument = instr;
			m_series = series;
		}

		/// <summary>The instrument this is an indicator on</summary>
		public Instrument Instrument
		{
			get;
			private set;
		}

		/// <summary>The number of elements in this indicator</summary>
		public int Count
		{
			get { return m_series.Count; }
		}

		/// <summary>Index range (-Count, 0]</summary>
		public Idx IdxFirst
		{
			get { return 1 - m_series.Count; }
		}
		public Idx IdxLast
		{
			get { return +1; }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public double this[Idx neg_idx]
		{
			get
			{
				Debug.Assert(neg_idx >= IdxFirst && neg_idx < IdxLast);

				var val0 = m_series[neg_idx - IdxFirst];
				var val1 = m_series[neg_idx - IdxFirst];
				return Maths.Lerp(val0, val1, (double)neg_idx - (int)neg_idx);
			}
		}

		/// <summary>Return the first derivative of the data series at 'index'</summary>
		public double FirstDerivative(Idx index)
		{
			return m_series.FirstDerivative(index);
		}
		public double FirstDerivative()
		{
			return m_series.FirstDerivative();
		}

		/// <summary>Return the second derivative of the data series at 'index'</summary>
		public double SecondDerivative(Idx index)
		{
			return m_series.SecondDerivative(index);
		}
		public double SecondDerivative()
		{
			return m_series.SecondDerivative();
		}

		/// <summary>Return a quadratic approximation of a EMA or null if no decent approximation could be made</summary>
		public Extrapolation Extrapolate(int history_count = 5)
		{
			// Require a number of periods
			if (Count < 10)
				return null;

			// Try combinations to get the best fit
			var combos = new[]
			{
				new[] {-0,-1,-2 },
				new[] {-0,-1,-3 },
				new[] {-0,-2,-4 },
				new[] {-0,-2,-5 },
				new[] {-0,-3,-6 },
				new[] {-0,-3,-7 },
				new[] {-0,-4,-6 },
				new[] {-0,-4,-8 },
			};

			// Test each quadratic, return the best fit
			var best = (Extrapolation)null;
			foreach (var combo in combos)
			{
				// Create a curve from the sample points
				var curve = Quadratic.FromPoints(
					combo[0], this[combo[0]],
					combo[1], this[combo[1]],
					combo[2], this[combo[2]]);

				// Measure the confidence of the curve as an approximation
				var err = new Avr();
				foreach (var c in Instrument.HighResRange(-history_count, 1))
				{
					var i = Instrument.IndexAt(c.TimestampUTC);
					err.Add(Maths.Sqr(this[i] - curve.F(i)));
				}
				var error = Math.Sqrt(err.Mean);

				// Map the error range to [0,+1], where > 0.5 is "good"
				var conf = 1.0 - Maths.Sigmoid(error, (double)Instrument.MCS * 0.2);
				if (best == null || best.Confidence < conf)
					best = new Extrapolation(curve, conf);
			}

			// Return the best guess
			return best;
		}
	}

	#region Extrapolation
	public class Extrapolation
	{
		public Extrapolation(Quadratic curve, double confidence)
		{
			Curve = curve;
			Confidence = confidence;
		}

		/// <summary>The predicted curve</summary>
		public Quadratic Curve { get; private set; }

		/// <summary>The confidence in the prediction</summary>
		public double Confidence { get; private set; }

		/// <summary>Get the predicted value</summary>
		public double this[Idx idx]
		{
			get { return Curve.F(idx); }
		}

		/// <summary>Get the first derivative at 'idx'</summary>
		public double dF(Idx idx)
		{
			return Curve.dF(idx);
		}

		/// <summary>Get the second derivative at 'idx'</summary>
		public double ddF(Idx idx)
		{
			return Curve.ddF(idx);
		}
	}
	#endregion
}

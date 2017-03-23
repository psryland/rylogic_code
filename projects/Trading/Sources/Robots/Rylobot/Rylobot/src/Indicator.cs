using System;
using System.Diagnostics;
using cAlgo.API;
using pr.maths;

namespace Rylobot
{
	/// <summary>A wrapper for CAlgo indicators than maps Idx to the data</summary>
	public class Indicator
	{
		public Indicator(Instrument instr, IndicatorDataSeries series, int periods, int? extrap_history = null)
		{
			Instrument    = instr;
			Source        = series;
			Periods       = periods;
			ExtrapHistory = extrap_history ?? periods;
		}

		/// <summary>SMA indicator</summary>
		public static Indicator SMA(Instrument instr, int periods, int? extrap_history = null)
		{
			return new Indicator(instr, instr.Bot.Indicators.SimpleMovingAverage(instr.Data.Close, periods).Result, periods, extrap_history);
		}

		/// <summary>EMA indicator</summary>
		public static Indicator EMA(Instrument instr, int periods, int? extrap_history = null)
		{
			return new Indicator(instr, instr.Bot.Indicators.ExponentialMovingAverage(instr.Data.Close, periods).Result, periods, extrap_history);
		}

		/// <summary>The indicator series providing the data</summary>
		public IndicatorDataSeries Source
		{
			get;
			private set;
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
			get { return Source.Count; }
		}

		/// <summary>The number of periods associated with the indicator</summary>
		public int Periods
		{
			get;
			private set;
		}

		/// <summary>The number of periods to fit extrapolated curves to</summary>
		public int ExtrapHistory
		{
			get;
			set;
		}

		/// <summary>Index range (-Count, 0]</summary>
		public Idx IdxFirst
		{
			get { return 1 - Source.Count; }
		}
		public Idx IdxLast
		{
			get { return +1; }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest</summary>
		public double this[Idx idx]
		{
			get
			{
				if (Count == 0) throw new IndexOutOfRangeException("");
				var i0 = Maths.Clamp(idx - 1, IdxFirst, IdxLast-1);
				var i1 = Maths.Clamp(idx    , IdxFirst, IdxLast-1);
				var val0 = Source[i0 - IdxFirst];
				var val1 = Source[i1 - IdxFirst];
				var val = Maths.Lerp(val0, val1, (double)idx - (int)idx);
				Debug.Assert(Maths.IsFinite(val));
				return val;
			}
		}

		/// <summary>Return the first derivative of the data series at 'index'</summary>
		public double FirstDerivative(Idx idx)
		{
			return Source.FirstDerivative(idx);
		}
		public double FirstDerivative()
		{
			return Source.FirstDerivative();
		}

		/// <summary>Return the second derivative of the data series at 'index'</summary>
		public double SecondDerivative(Idx idx)
		{
			return Source.SecondDerivative(idx);
		}
		public double SecondDerivative()
		{
			return Source.SecondDerivative();
		}

		/// <summary>Default extrapolation of the indicator</summary>
		public Extrapolation Future
		{
			get { return Extrapolate(2, ExtrapHistory); }
		}

		/// <summary>Return a polynomial approximation of a EMA or null if no decent approximation could be made</summary>
		public Extrapolation Extrapolate(int order, int history_count)
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
				var curve = (IPolynomial)null;
				switch (order)
				{
				default: throw new Exception("Unsupported polynomial order");
				case 2:
					curve = Quadratic.FromPoints(
					combo[0], this[combo[0]],
					combo[1], this[combo[1]],
					combo[2], this[combo[2]]);
					break;
				case 1:
					curve = Monic.FromPoints(
					combo[0], this[combo[0]],
					combo[2], this[combo[2]]);
					break;
				}

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
		public Extrapolation(IPolynomial curve, double confidence)
		{
			Curve = curve;
			Confidence = confidence;
		}

		/// <summary>The predicted curve</summary>
		public IPolynomial Curve { get; private set; }

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

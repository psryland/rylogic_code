using System;
using System.Diagnostics;
using System.Linq;
using cAlgo.API;
using pr.common;
using pr.maths;

namespace Rylobot
{
	/// <summary>A wrapper for CAlgo indicators than maps Idx to the data</summary>
	public class Indicator
	{
		public Indicator(string name, Instrument instr, IndicatorDataSeries[] series, int periods, int? extrap_history = null)
		{
			Name          = name;
			Instrument    = instr;
			Source        = series;
			Periods       = periods;
			ExtrapHistory = extrap_history ?? periods;
		}

		/// <summary>SMA indicator</summary>
		public static Indicator SMA(string name, Instrument instr, int periods, int? extrap_history = null)
		{
			return new Indicator(name, instr, new[] { instr.Indicators.SimpleMovingAverage(instr.Data.Close, periods).Result }, periods, extrap_history);
		}

		/// <summary>EMA indicator</summary>
		public static Indicator EMA(string name, Instrument instr, int periods, int? extrap_history = null)
		{
			return new Indicator(name, instr, new [] { instr.Indicators.ExponentialMovingAverage(instr.Data.Close, periods).Result } , periods, extrap_history);
		}

		/// <summary>MACD indicator</summary>
		public static Indicator MACD(string name, Instrument instr, int long_cycle, int short_cycle, int periods, int? extrap_history = null)
		{
			var indi = instr.Indicators.MacdCrossOver(long_cycle, short_cycle, periods);
			return new Indicator(name, instr, new [] { indi.MACD, indi.Signal }, periods, extrap_history);
		}

		/// <summary>A Donchian channel indicator</summary>
		public static Indicator Donchian(string name, Instrument instr, int periods, int? extrap_history = null)
		{
			var indi = instr.Indicators.DonchianChannel(periods);
			return new Indicator(name, instr, new [] { indi.Top, indi.Middle, indi.Bottom }, periods, extrap_history);
		}

		/// <summary>An identifying name for the indicator</summary>
		public string Name
		{
			get;
			private set;
		}

		/// <summary>The indicator series providing the data</summary>
		private IndicatorDataSeries[] Source
		{
			get;
			set;
		}

		/// <summary>The instrument this is an indicator on</summary>
		public Instrument Instrument
		{
			get;
			private set;
		}

		/// <summary>The number of data series in this indicator</summary>
		public int SourceCount
		{
			get { return Source.Length; }
		}

		/// <summary>The number of elements in this indicator</summary>
		public int Count
		{
			get { return Source[0].Count; }
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
			get { return 1 - Count; }
		}
		public Idx IdxLast
		{
			get { return +1; }
		}

		/// <summary>The raw data. Idx = -(Count+1) is the oldest, Idx = 0 is the latest.</summary>
		public double this[Idx idx, int series = 0]
		{
			get
			{
				if (Count == 0) throw new IndexOutOfRangeException("");
				var i0 = Maths.Clamp(idx    , IdxFirst, IdxLast-1);
				var i1 = Maths.Clamp(idx + 1, IdxFirst, IdxLast-1);
				var val0 = Source[series][(int)(i0 - IdxFirst)];
				var val1 = Source[series][(int)(i1 - IdxFirst)];
				var val = Maths.Lerp(val0, val1, (double)idx - (int)idx);
				return val;
			}
		}

		/// <summary>Return the first derivative of the data series at 'index'</summary>
		public double FirstDerivative(Idx idx, int series = 0)
		{
			return Source[series].FirstDerivative(idx);
		}

		/// <summary>Return the second derivative of the data series at 'index'</summary>
		public double SecondDerivative(Idx idx, int series = 0)
		{
			return Source[series].SecondDerivative(idx);
		}

		/// <summary>Default extrapolation of the indicator</summary>
		public Extrapolation Future
		{
			get { return Extrapolate(2, ExtrapHistory); }
		}

		/// <summary>Return a polynomial approximation of the indicator values or null if no decent approximation could be made. 'order' is polynomial order (i.e. 1 or 2)</summary>
		public Extrapolation Extrapolate(int order, int history_count, Idx? idx_ = null, int series = 0)
		{
			var idx = idx_ ?? IdxLast;

			// Get the points to fit too
			var range = Instrument.IndexRange(idx - history_count, idx);
			var points = range
				.Where(x => Maths.IsFinite(Source[series][(int)(x - IdxFirst)]))
				.Select(x => new v2((float)x, (float)Source[series][(int)(x - IdxFirst)]))
				.ToArray();

			// Require a minimum number of points
			if (points.Length <= order)
				return null;

			// Create a curve using linear regression
			var curve = (IPolynomial)null;
			switch (order) {
			default: throw new Exception("Unsupported polynomial order");
			case 2: curve = Quadratic.FromLinearRegression(points); break;
			case 1: curve = Monic.FromLinearRegression(points); break;
			}

			// Measure the confidence in the fit.
			// Map the error range to [0,+1], where > 0.5 is "good"
			var rms  = Math.Sqrt(points.Sum(x => Maths.Sqr(x.y - curve.F(x.x))));
			var conf = 1.0 - Maths.Sigmoid(rms, Instrument.MCS * 0.2);
			return new Extrapolation(curve, conf);
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

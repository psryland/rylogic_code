//*********************************************************************
// Stat
//  Copyright (c) Rylogic Ltd 2008
//*********************************************************************

using System;
using System.Diagnostics;
using pr.maths;

namespace pr.maths
{
	/// <summary>Running Average</summary>
	[DebuggerDisplay("{Mean} N={Count}")]
	public class Avr
	{
		//' let: D(k) = X(k) - avr(k-1)           => X(k) = D(k) + avr(k-1)
		//'  avr(k-1) = SUM{X(k-1)} / (k-1)       => SUM{X(k-1)} = (k-1)*avr(k-1)
		//'  avr(k)   = (SUM{X(k-1)} + X(k)) / k
		//'           = ((k-1)*avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) - avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) + D(k)) / k
		//'           = avr(k-1) + D(k) / k

		protected double m_mean;
		protected uint   m_count;
		
		public Avr()
		{
			Reset();
		}
		public Avr(double mean, uint count)
		{
			m_mean  = mean;
			m_count = count;
		}
		public Avr(Avr rhs)
		{
			m_mean = rhs.m_mean;
			m_count = rhs.m_count;
		}

		/// <summary>Number of items added</summary>
		public uint Count
		{
			get { return m_count; }
		}

		/// <summary>Mean value</summary>
		public double Mean
		{
			get { return m_mean; }
		}

		/// <summary>Sum of values</summary>
		public double Sum
		{
			get { return m_mean * m_count; }
		}

		/// <summary>Reset the stats</summary>
		public virtual void Reset()
		{
			m_count = 0;
			m_mean  = 0.0;
		}
		
		/// <summary>
		/// Accumulate statistics for 'value' in a single pass.
		/// Note, this method is more accurate than the sum of squares, square of sums approach.</summary>
		public virtual void Add(double value)
		{
			++m_count;
			var diff = value - m_mean;
			var inv_count = 1.0 / m_count; // don't remove this, conversion to double here is needed for accuracy
			m_mean += diff * inv_count;
		}

		/// <summary>
		/// Calculate a running average.
		/// 'value' is the new value to add to the mean.
		/// 'mean' is the current mean.
		/// 'count' is the number of data points that have contributed to 'mean'</summary>
		public static double Running(double value, double mean, uint count)
		{
			return mean + (value - mean) / (1 + count);
		}
	}

	/// <summary>Running Average with standard deviation and variance</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class AvrVar :Avr
	{
		//' let: D(k) = X(k) - avr(k-1)           => X(k) = D(k) + avr(k-1)
		//'  avr(k-1) = SUM{X(k-1)} / (k-1)       => SUM{X(k-1)} = (k-1)*avr(k-1)
		//'  avr(k)   = (SUM{X(k-1)} + X(k)) / k
		//'           = ((k-1)*avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) - avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) + D(k)) / k
		//'           = avr(k-1) + D(k) / k
		//' Running standard variance:
		//'  var(k)   = (1/(k-1)) * SUM{(X(k) - avr(k))²}
		//' (k-1)*var(k) = SUM{(X(k) - avr(k))²}
		//'              = SUM{(X(k)² - 2*avr(k)*X(k) + avr(k)²}
		//'              = SUM{X(k)²} - 2*avr(k)*SUM{X(k)} + k*avr(k)²
		//'              = SUM{X(k)²} - 2*avr(k)*k*avr(k) + k*avr(k)²
		//'              = SUM{X(k)²} - 2*k*avr(k)² + k*avr(k)²
		//'              = SUM{X(k)²} - k*avr(k)²
		//' so:
		//'  (k-2)*var(k-1) = SUM{X(k-1)²} - (k-1)*avr(k-1)²
		//' taking:
		//'  (k-1)*var(k) - (k-2)*var(k-1) = SUM{X(k)²} - k*avr(k)² - SUM{X(k-1)²} + (k-1)*avr(k-1)²
		//'                                = X(k)² - k*avr(k)² + (k-1)*avr(k-1)²
		//'                                = X(k)²                               - k*(avr(k-1) + D(k)/k)²                       + k*avr(k-1)² - avr(k-1)²
		//'                                = (D(k) + avr(k-1))²                  - k*(avr(k-1) + D(k)/k)²                       + k*avr(k-1)² - avr(k-1)²
		//'                                = D(k)² + 2*D(k)*avr(k-1) + avr(k-1)² - k*(avr(k-1)² + 2*D(k)*avr(k-1)/k + D(k)²/k²) + k*avr(k-1)² - avr(k-1)²
		//'                                = D(k)² + 2*D(k)*avr(k-1) + avr(k-1)² - k*avr(k-1)² - 2*D(k)*avr(k-1) - D(k)²/k      + k*avr(k-1)² - avr(k-1)²
		//'                                = D(k)² - D(k)²/k
		//'                                = ((k-1)/k) * D(k)²

		protected double m_var;
		
		public AvrVar()
			:base()
		{
			m_var = 0;
		}
		public AvrVar(double mean, double var, uint count)
			:base(mean, count)
		{
			m_var = var;
		}
		public AvrVar(AvrVar rhs)
			:base(rhs)
		{
			m_var = rhs.m_var;
		}

		/// <summary>Population standard deviation.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdDev
		{
			get { return Maths.Sqrt(PopStdVar); }
		}

		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdDev
		{
			get { return Maths.Sqrt(SamStdVar); }
		}

		/// <summary>Population variance.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdVar
		{
			get { return m_count > 0 ? m_var * (1.0 / (m_count - 0)) : 0.0; }
		}

		/// <summary>Sample variance.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdVar
		{
			get { return m_count > 1 ? m_var * (1.0 / (m_count - 1)) : 0.0; }
		}

		/// <summary>Reset the stats</summary>
		public override void Reset()
		{
			base.Reset();
			m_var = 0.0;
		}

		/// <summary>
		/// Accumulate statistics for 'value' in a single pass.
		/// Note, this method is more accurate than the sum of squares, square of sums approach.</summary>
		public override void Add(double value)
		{
			++m_count;
			var diff = value - m_mean;
			var inv_count = 1.0 / m_count; // don't remove this, conversion to double here is needed for accuracy
			m_mean += diff * inv_count;
			m_var += diff * diff * ((m_count - 1) * inv_count);
		}
	}

	/// <summary>Running average of two variables with standard deviation, variance, and correlation coefficient</summary>
	public class Correlation
	{
		protected uint m_count;
		protected double m_mean_x;
		protected double m_mean_y;
		protected double m_var_x;
		protected double m_var_y;
		protected double m_var_xy;

		public Correlation()
		{
			m_count  = 0;
			m_mean_x = 0;
			m_mean_y = 0;
			m_var_x  = 0;
			m_var_y  = 0;
			m_var_xy = 0;
		}
		public Correlation(Correlation rhs)
		{
			m_count  = rhs.m_count;
			m_mean_x = rhs.m_mean_x;
			m_mean_y = rhs.m_mean_y;
			m_var_x  = rhs.m_var_x;
			m_var_y  = rhs.m_var_y;
			m_var_xy = rhs.m_var_xy;
		}

		/// <summary>Number of items added</summary>
		public uint Count
		{
			get { return m_count; }
		}

		/// <summary>Mean value</summary>
		public double MeanX
		{
			get { return m_mean_x; }
		}
		public double MeanY
		{
			get { return m_mean_y; }
		}

		/// <summary>Sum of values</summary>
		public double SumX
		{
			get { return m_mean_x * m_count; }
		}
		public double SumY
		{
			get { return m_mean_y * m_count; }
		}

		/// <summary>Population standard deviation.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdDevX
		{
			get { return Maths.Sqrt(PopStdVarX); }
		}
		public double PopStdDevY
		{
			get { return Maths.Sqrt(PopStdVarY); }
		}

		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdDevX
		{
			get { return Maths.Sqrt(SamStdVarX); }
		}
		public double SamStdDevY
		{
			get { return Maths.Sqrt(SamStdVarY); }
		}

		/// <summary>Population variance.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdVarX
		{
			get { return m_count > 0 ? m_var_x * (1.0 / (m_count - 0)) : 0.0; }
		}
		public double PopStdVarY
		{
			get { return m_count > 0 ? m_var_y * (1.0 / (m_count - 0)) : 0.0; }
		}

		/// <summary>Sample variance.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdVarX
		{
			get { return m_count > 1 ? m_var_x * (1.0 / (m_count - 1)) : 0.0; }
		}
		public double SamStdVarY
		{
			get { return m_count > 1 ? m_var_y * (1.0 / (m_count - 1)) : 0.0; }
		}

		/// <summary>Population covariance</summary>
		public double PopCoVar
		{
			get { return m_count > 0 ? m_var_xy * (1.0 / (m_count - 0)) : 0.0; }
		}

		/// <summary>Sample covariance</summary>
		public double SamCoVar
		{
			get { return m_count > 1 ? m_var_xy * (1.0 / (m_count - 1)) : 0.0; }
		}

		/// <summary>The correlation coefficient between 'x' and 'y' (A value between [-1,+1])</summary>
		public double CorrCoeff
		{
			get { return m_count > 0 && !Maths.FEql(m_var_xy, 0, Maths.TinyD) ? m_var_xy / Maths.Sqrt(m_var_x * m_var_y) : 0.0; }
		}

		/// <summary>Reset the stats</summary>
		public void Reset()
		{
			m_count  = 0;
			m_mean_x = 0;
			m_mean_y = 0;
			m_var_x  = 0;
			m_var_y  = 0;
			m_var_xy = 0;
		}

		/// <summary>
		/// Accumulate the correlation between two variables 'x' and 'y' in a single pass.
		/// Note, this method is more accurate than the sum of squares, square of sums approach.</summary>
		public void Add(double x, double y)
		{
			++m_count;
			var diff_x = x - m_mean_x;
			var diff_y = y - m_mean_y;
			var inv_count = 1.0 / m_count; // don't remove this, conversion to double here is needed for accuracy

			m_mean_x += diff_x * inv_count;
			m_mean_y += diff_y * inv_count;

			m_var_x += diff_x * diff_x * ((m_count - 1) * inv_count);
			m_var_y += diff_y * diff_y * ((m_count - 1) * inv_count);

			m_var_xy += diff_x * diff_y * ((m_count - 1) * inv_count);
		}
	}

	/// <summary>Exponential Moving Average</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class ExpMovingAvr
	{
		//'  avr(k) = a * X(k) + (1 - a) * avr(k-1)
		//'         = a * X(k) + avr(k-1) - a * avr(k-1)
		//'         = avr(k-1) + a * X(k) - a * avr(k-1)
		//'         = avr(k-1) + a * (X(k) - avr(k-1))
		//'    'a' is the exponential smoothing factor between (0,1)
		//'    define: a = 2 / (N + 1), where 'N' is roughly the window size of an equivalent moving window average
		//'    The interval over which the weights decrease by a factor of two (half-life) is approximately N/2.8854
		//' Exponential moving variance:
		//' (k-1)var(k) = SUM{w(k) * U(k)²}, where: U(k) = X(k) - avr(k)
		//'             = w(1)*U(1)² + w(2)*U(2)² + ... + w(k)*U(k)², where: w(1)+w(2)+...+w(k) = k
		//' If we say:  w(k) = k * a, ('a' between (0,1) as above) then SUM{w(k-1)} = k * (1-a)
		//' so consider var(k-1):
		//'  (k-2)var(k-1) = w(1)*U(1)² + w(2)*U(2)² + ... + w(k-1)*U(k-1)², where: w(1)+w(2)+...+w(k-1) = k - 1
		//' when we add the next term:
		//'  (k-1)var(k)   = w(1)*U(1)² + w(2)*U(2)² + ... + w(k-1)*U(k-1)² + w(k)*U(k)² (note w(1)..w(k-1) will be different values to above)
		//' we need:
		//'   k = k*a + k*(1-a) = w(k) + b*SUM{w(k-1)}
		//' => k*(1-a) = b*SUM{w(k-1)}
		//'          b = (1-a)*k/SUM{w(k-1)}
		//'            = (1-a)*k/(k-1)
		//' so:
		//' (k-1)var(k) = a*k*U(k)² + b*(k-2)var(k-1)
		//'             = a*k*U(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'  let: D(k) = X(k) - avr(k-1)
		//'       U(k) = X(k) - avr(k-1) + avr(k-1) - avr(k)
		//'            = D(k) + avr(k-1) - avr(k)
		//'            = D(k) - (avr(k) - avr(k-1))
		//'            = D(k) - a * (X(k) - avr(k-1))
		//'            = D(k) - a * D(k)
		//'            = (1-a)*D(k)
		//' then:
		//' (k-1)var(k) = a*k*U(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'             = a*k*(1-a)²*D(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'             = a*k*b²*D(k)² + (b*k/(k-1)) * (k-2)var(k-1)         where: b = (1-a)
		//'             = (b*k/(k-1))*((a*b*(k-1)*D(k)² + (k-2)var(k-1))
	
		private double m_mean;
		private double m_var;
		private uint   m_size;
		private uint   m_count;

		public ExpMovingAvr()
			:this(0U)
		{}
		public ExpMovingAvr(uint window_size)
		{
			Reset(window_size);
		}
		public ExpMovingAvr(ExpMovingAvr rhs)
		{
			m_mean  = rhs.m_mean;
			m_var   = rhs.m_var;
			m_size  = rhs.m_size;
			m_count = rhs.m_count;
		}

		/// <summary>The size of an equivalent moving average window size</summary>
		public uint WindowSize
		{
			get { return m_size; }
		}

		/// <summary>The number of data points added to this average</summary>
		public uint Count
		{
			get { return m_count; }
		}

		/// <summary>The average</summary>
		public double Mean
		{
			get { return m_mean; }
		}

		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		// Note: for a moving variance the choice between population/sample SD is a bit arbitrary
		public double PopStdDev
		{
			get { return Math.Sqrt(PopStdVar); }
		}
		public double SamStdDev
		{
			get { return Math.Sqrt(SamStdVar); }
		}
		public double PopStdVar
		{
			get { return m_count > 0 ? m_var * (1.0 / (m_count - 0)) : 0.0; }
		}
		public double SamStdVar
		{
			get { return m_count > 1 ? m_var * (1.0 / (m_count - 1)) : 0.0; }
		}

		public virtual void Reset(uint window_size)
		{
			m_size  = window_size;
			m_count = 0;
			m_mean  = 0.0;
			m_var   = 0.0;
		}
		public virtual void Add(double value)
		{
			if (m_count >= m_size)
			{
				++m_count;
				double diff  = value - m_mean;
				double a = 2.0 / (m_size + 1.0);
				double b = 1 - a;
				m_mean += diff * a;
				m_var = (b*m_count/(m_count-1)) * (a*b*(m_count-1)*diff*diff + m_var);
			}
			else // use standard mean/var until 'm_size' is reached
			{
				++m_count;
				double diff  = value - m_mean;
				double inv_count = 1.0 / m_count;
				m_mean += diff * inv_count;
				m_var  += diff * diff * ((m_count - 1) * inv_count);
			}
		}
	}

	/// <summary>Moving Window Average</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class MovingAvr
	{
		// Let: D(k) = X(k) - X(k-N) => X(k-N) = X(k) - D(k)
		// Average:
		//   avr(k) = avr(k-1) + (X(k) - X(k-N)) / N
		//          = avr(k-1) + D(k) / N
		private double[] m_window;
		private double   m_mean;
		private uint     m_count;
		private int      m_i;

		public double Mean
		{
			get { return m_mean; }
		}
		private double Var
		{
			get
			{
				double var = 0.0;
				uint count = m_count;
				int end = m_window.Length;
				for (int i = m_i; i-- != 0   && count != 0; --count) { double diff  = m_window[i] - m_mean; var += diff * diff; }
				for (int i = end; i-- != m_i && count != 0; --count) { double diff  = m_window[i] - m_mean; var += diff * diff; }
				return var;
			}
		}

		// NOTE: no recursive variance because we would need to buffer the averages as well
		// so that we could remove (X(k-N) - avr(k-N))² at each iteration
		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		public double PopStdDev { get { return Math.Sqrt(PopStdVar); } }
		public double SamStdDev { get { return Math.Sqrt(SamStdVar); } }
		public double PopStdVar { get { return m_count > 0 ? Var * (1.0 / (m_count - 0)) : 0.0; } }
		public double SamStdVar { get { return m_count > 1 ? Var * (1.0 / (m_count - 1)) : 0.0; } }
		
		public MovingAvr(uint window_size)
		{
			Reset(window_size);
		}
		public void Reset(uint window_size)
		{
			m_window = new double[window_size];
			m_mean   = 0.0;
			m_count  = 0;
			m_i      = 0;
		}
		public void Add(double value)
		{
			if (m_count == m_window.Length)
			{
				if (m_i == m_window.Length) m_i = 0;
				double diff = value - m_window[m_i];
				double inv_count = 1.0 / m_window.Length;
				m_mean += diff * inv_count;
				m_window[m_i++] = value;
			}
			else
			{
				++m_count;
				double diff = value - m_mean;
				double inv_count = 1.0 / m_count;
				m_mean += diff * inv_count;
				m_window[m_i++] = value;
			}
		}
	};
}


#if PR_UNITTESTS
namespace pr.unittests
{
	[TestFixture] public class TestStat
	{
		[Test] public void Stat()
		{
			double[] num = new[]{2.0,4.0,7.0,3.0,2.0,-5.0,-4.0,1.0,-7.0,3.0,6.0,-8.0};
			
			var s = new AvrVar();
			for (int i = 0; i != num.Length; ++i)
				s.Add(num[i]);
			
			Assert.AreEqual((uint)num.Length, s.Count);
			Assert.AreEqual(4.0                               ,s.Sum       ,Maths.TinyD);
			Assert.AreEqual(1.0/3.0                           ,s.Mean      ,Maths.TinyD);
			Assert.AreEqual(4.83621                           ,s.PopStdDev ,0.00001);
			Assert.AreEqual(23.38889                          ,s.PopStdVar ,0.00001);
			Assert.AreEqual(5.0512524699475787686684767441111 ,s.SamStdDev ,Maths.TinyD);
			Assert.AreEqual(25.515151515151515151515151515152 ,s.SamStdVar ,Maths.TinyD);
		}
		[Test] public void MovingWindowAvr()
		{
			const int BufSz = 13;
			
			Random rng = new Random();
			MovingAvr s = new MovingAvr(BufSz);
			double[] buf = new double[BufSz];
			int idx = 0;
			int count = 0;
			for (int i = 0; i != BufSz * 2; ++i)
			{
				double v = rng.NextDouble();
				buf[idx] = v;
				if (count != BufSz) ++count;
				idx = (idx + 1) % BufSz;
				double sum = 0.0f; for (uint j = 0; j != count; ++j) sum += buf[j];
				double mean = sum / count;
				s.Add(v);
				Assert.AreEqual(mean ,s.Mean ,0.00001);
			}
		}
		[Test] public void ExpMovingAvr()
		{
			const int BufSz = 13;
			
			Random rng = new Random();
			ExpMovingAvr s = new ExpMovingAvr(BufSz);
			const double a = 2.0 / (BufSz + 1);
			double ema = 0;
			int count = 0;
			for (int i = 0; i != BufSz * 2; ++i)
			{
				double v = rng.NextDouble();
				if (count < BufSz) { ++count; ema += (v - ema) / count; }
				else               { ema = a * v + (1 - a) * ema; }
				s.Add(v);
				Assert.AreEqual(ema ,s.Mean ,0.00001);
			}
		}
		[Test] public void Correlation()
		{
			var arr0 = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
			var arr1 = new double[] { 2, 3, 4, 5, 6, 7, 8, 9,10 };
			var arr2 = new double[] { 9, 8, 7, 6, 5, 4, 3, 2, 1 };
			var arr3 = new double[] { 5, 5, 5, 5, 5, 5, 5, 5, 5 };

			{
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr0[i]);
				Assert.AreEqual(corr.CorrCoeff, 1.0, Maths.TinyD);
			} {
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr1[i]);
				Assert.AreEqual(corr.CorrCoeff, 1.0, Maths.TinyD);
			} {
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr2[i]);
				Assert.AreEqual(corr.CorrCoeff, -1.0, Maths.TinyD);
			}{
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr3[i]);
				Assert.AreEqual(corr.CorrCoeff, 0.0, Maths.TinyD);
			}
		}
	}
}
#endif

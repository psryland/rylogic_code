//*********************************************************************
// Stat
//  Copyright (c) Rylogic Ltd 2008
//*********************************************************************

using System;
using pr.maths;

namespace pr.maths
{
	// Running average:
	// let: D(k) = X(k) - avr(k-1)           => X(k) = D(k) + avr(k-1)
	//  avr(k-1) = SUM{X(k-1)} / (k-1)       => SUM{X(k-1)} = (k-1)*avr(k-1)
	//  avr(k)   = (SUM{X(k-1)} + X(k)) / k
	//           = ((k-1)*avr(k-1) + D(k) + avr(k-1)) / k
	//           = (k*avr(k-1) - avr(k-1) + D(k) + avr(k-1)) / k
	//           = (k*avr(k-1) + D(k)) / k
	//           = avr(k-1) + D(k) / k
	// Running standard variance:
	//  var(k)   = (1/(k-1)) * SUM{(X(k) - avr(k))²}
	// (k-1)*var(k) = SUM{(X(k) - avr(k))²}
	//              = SUM{(X(k)² - 2*avr(k)*X(k) + avr(k)²}
	//              = SUM{X(k)²} - 2*avr(k)*SUM{X(k)} + k*avr(k)²
	//              = SUM{X(k)²} - 2*avr(k)*k*avr(k) + k*avr(k)²
	//              = SUM{X(k)²} - 2*k*avr(k)² + k*avr(k)²
	//              = SUM{X(k)²} - k*avr(k)²
	// so:
	//  (k-2)*var(k-1) = SUM{X(k-1)²} - (k-1)*avr(k-1)²
	// taking:
	//  (k-1)*var(k) - (k-2)*var(k-1) = SUM{X(k)²} - k*avr(k)² - SUM{X(k-1)²} + (k-1)*avr(k-1)²
	//                                = X(k)² - k*avr(k)² + (k-1)*avr(k-1)²
	//                                = X(k)²                               - k*(avr(k-1) + D(k)/k)²                       + k*avr(k-1)² - avr(k-1)²
	//                                = (D(k) + avr(k-1))²                  - k*(avr(k-1) + D(k)/k)²                       + k*avr(k-1)² - avr(k-1)²
	//                                = D(k)² + 2*D(k)*avr(k-1) + avr(k-1)² - k*(avr(k-1)² + 2*D(k)*avr(k-1)/k + D(k)²/k²) + k*avr(k-1)² - avr(k-1)²
	//                                = D(k)² + 2*D(k)*avr(k-1) + avr(k-1)² - k*avr(k-1)² - 2*D(k)*avr(k-1) - D(k)²/k      + k*avr(k-1)² - avr(k-1)²
	//                                = D(k)² - D(k)²/k
	//                                = ((k-1)/k) * D(k)²
	public class Stat
	{
		private double m_mean;
		private double m_var;
		private double m_min;
		private double m_max;
		private uint   m_count;
		
		public uint   Count    { get { return m_count; } }
		public double Mean     { get { return m_mean; } }
		public double Sum      { get { return m_mean * m_count; } }
		public double Minimum  { get { return m_min; } }
		public double Maximum  { get { return m_max; } }
		
		/// <summary>Population standard deviation.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdDev { get { return Maths.Sqrt(PopStdVar); } }
		
		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdDev { get { return Maths.Sqrt(SamStdVar); } }
		
		/// <summary>Population variance.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdVar { get { return m_count > 0 ? m_var * (1.0 / (m_count - 0)) : 0.0; } }
		
		/// <summary>Sample variance.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdVar { get { return m_count > 1 ? m_var * (1.0 / (m_count - 1)) : 0.0; } }
		
		public Stat()
		{
			Reset();
		}
		
		// Reset the stats
		public void Reset()
		{
			m_count = 0;
			m_mean  = 0.0;
			m_var   = 0.0;
			m_min   =  double.MaxValue;
			m_max   = -double.MaxValue;
		}
		
		// Accumulate statistics for 'value' in a single pass.
		// Note, this method is more accurate than the sum of squares, square of sums approach.
		public void Add(double value)
		{
			++m_count;
			double diff  = value - m_mean;
			double inv_count = 1.0 / m_count;
			m_mean += diff * inv_count;
			m_var  += diff * diff * ((m_count - 1) * inv_count);
			m_min = Math.Min(value, m_min);
			m_max = Math.Max(value, m_max);
		}
	}
	
	// Exponental moving average:
	//   avr(k) = a * X(k) + (1 - a) * avr(k-1)
	//          = a * X(k) + avr(k-1) - a * avr(k-1)
	//          = avr(k-1) + a * X(k) - a * avr(k-1)
	//          = avr(k-1) + a * (X(k) - avr(k-1))
	//    'a' is the exponential smoothing factor between (0,1)
	//    define: a = 2 / (N + 1), where 'N' is roughly the window size of an equivalent moving window average
	//    The interval over which the weights decrease by a factor of two (half-life) is approximately N/2.8854
	// Exponential moving variance:
	// (k-1)var(k) = SUM{w(k) * U(k)²}, where: U(k) = X(k) - avr(k)
	//             = w(1)*U(1)² + w(2)*U(2)² + ... + w(k)*U(k)², where: w(1)+w(2)+...+w(k) = k
	// If we say:  w(k) = k * a, ('a' between (0,1) as above) then SUM{w(k-1)} = k * (1-a)
	// so consider var(k-1):
	//  (k-2)var(k-1) = w(1)*U(1)² + w(2)*U(2)² + ... + w(k-1)*U(k-1)², where: w(1)+w(2)+...+w(k-1) = k - 1
	// when we add the next term:
	//  (k-1)var(k)   = w(1)*U(1)² + w(2)*U(2)² + ... + w(k-1)*U(k-1)² + w(k)*U(k)² (note w(1)..w(k-1) will be different values to above)
	// we need:
	//   k = k*a + k*(1-a) = w(k) + b*SUM{w(k-1)}
	// => k*(1-a) = b*SUM{w(k-1)}
	//          b = (1-a)*k/SUM{w(k-1)}
	//            = (1-a)*k/(k-1)
	// so:
	// (k-1)var(k) = a*k*U(k)² + b*(k-2)var(k-1)
	//             = a*k*U(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
	//  let: D(k) = X(k) - avr(k-1)
	//       U(k) = X(k) - avr(k-1) + avr(k-1) - avr(k)
	//            = D(k) + avr(k-1) - avr(k)
	//            = D(k) - (avr(k) - avr(k-1))
	//            = D(k) - a * (X(k) - avr(k-1))
	//            = D(k) - a * D(k)
	//            = (1-a)*D(k)
	// then:
	// (k-1)var(k) = a*k*U(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
	//             = a*k*(1-a)²*D(k)² + (1-a)*(k/(k-1)) * (k-2)var(k-1)
	//             = a*k*b²*D(k)² + (b*k/(k-1)) * (k-2)var(k-1)         where: b = (1-a)
	//             = (b*k/(k-1))*((a*b*(k-1)*D(k)² + (k-2)var(k-1))
	public class ExpMovingAvr
	{
		private double m_mean;
		private double m_var;
		private uint   m_size;
		private uint   m_count;
		
		public double Mean { get { return m_mean; } }
		
		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		// Note: for a moving variance the choice between population/sample sd is a bit arbitrary
		public double PopStdDev { get { return Maths.Sqrt(PopStdVar); } }
		public double SamStdDev { get { return Maths.Sqrt(SamStdVar); } }
		public double PopStdVar { get { return m_count > 0 ? m_var * (1.0 / (m_count - 0)) : 0.0; } }
		public double SamStdVar { get { return m_count > 1 ? m_var * (1.0 / (m_count - 1)) : 0.0; } }
		
		public ExpMovingAvr(uint window_size)
		{
			Reset(window_size);
		}
		public void Reset(uint window_size)
		{
			m_size  = window_size;
			m_count = 0;
			m_mean  = 0.0;
			m_var   = 0.0;
		}
		public void Add(double value)
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
	
	// A moving window average
	// Let: D(k) = X(k) - X(k-N) => X(k-N) = X(k) - D(k)
	// Average:
	//   avr(k) = avr(k-1) + (X(k) - X(k-N)) / N
	//          = avr(k-1) + D(k) / N
	public class MovingAvr
	{
		
		private double[] m_window;
		private double   m_mean;
		private uint     m_count;
		private int      m_i;
		
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
		
		public double Mean { get { return m_mean; } }
		
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
namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		[Test] public static void TestStat()
		{
			double[] num = new[]{2.0,4.0,7.0,3.0,2.0,-5.0,-4.0,1.0,-7.0,3.0,6.0,-8.0};
			
			Stat s = new Stat();
			for (int i = 0; i != num.Length; ++i)
				s.Add(num[i]);
			
			Assert.AreEqual(num.Length, s.Count);
			Assert.AreEqual(4.0                               ,s.Sum       ,Maths.TinyD);
			Assert.AreEqual(-8.0                              ,s.Minimum   ,Maths.TinyD);
			Assert.AreEqual(7.0                               ,s.Maximum   ,Maths.TinyD);
			Assert.AreEqual(1.0/3.0                           ,s.Mean      ,Maths.TinyD);
			Assert.AreEqual(4.83621                           ,s.PopStdDev ,0.00001);
			Assert.AreEqual(23.38889                          ,s.PopStdVar ,0.00001);
			Assert.AreEqual(5.0512524699475787686684767441111 ,s.SamStdDev ,Maths.TinyD);
			Assert.AreEqual(25.515151515151515151515151515152 ,s.SamStdVar ,Maths.TinyD);
		}
		[Test] public static void TestMovingWindowAvr()
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
		[Test] public static void TestExpMovingAvr()
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
	}
}
#endif

//*********************************************************************
// Stat
//  Copyright (c) Rylogic Ltd 2008
//*********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Maths;

namespace Rylogic.Maths
{
	public interface IStatMean
	{
		/// <summary>The sum of the values added</summary>
		int Count { get; }

		/// <summary>The sum of values added</summary>
		double Sum { get; }

		/// <summary>The mean value</summary>
		double Mean { get; }
	}
	public interface IStatVariance
	{
		/// <summary>Population standard deviation.<para/>Use when all data values in a set have been considered.</summary>
		double PopStdDev { get; }

		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		double SamStdDev { get; }

		/// <summary>Population variance.<para/>Use when all data values in a set have been considered.</summary>
		double PopStdVar { get; }

		/// <summary>Sample variance.<para/>Use the when the data values used are only a sample of the total population</summary>
		double SamStdVar { get; }
	}
	public interface IStatMeanAndVariance :IStatMean, IStatVariance
	{
	}
	public interface IStatSingleVariable
	{
		/// <summary>Add a single value to the stat</summary>
		IStatSingleVariable Add(double value);
	}
	public interface IStatTwoVariables
	{
		/// <summary>Add values to the stat</summary>
		void Add(double x, double y);
	}

	/// <summary>Simple Running Average</summary>
	[DebuggerDisplay("{Mean} N={Count}")]
	public class Avr<T> :IStatMean ,IStatSingleVariable where T:Avr<T>
	{
		//' let: D(k) = X(k) - avr(k-1)           => X(k) = D(k) + avr(k-1)
		//'  avr(k-1) = SUM{X(k-1)} / (k-1)       => SUM{X(k-1)} = (k-1)*avr(k-1)
		//'  avr(k)   = (SUM{X(k-1)} + X(k)) / k
		//'           = ((k-1)*avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) - avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) + D(k)) / k
		//'           = avr(k-1) + D(k) / k

		protected double m_mean;
		protected int    m_count;
		
		public Avr()
		{
			Reset();
		}
		public Avr(double mean, int count)
		{
			m_mean  = mean;
			m_count = count;
		}
		public Avr(Avr<T> rhs)
		{
			m_mean = rhs.m_mean;
			m_count = rhs.m_count;
		}

		/// <summary>Number of items added</summary>
		public int Count
		{
			get { return m_count; }
		}

		/// <summary>Sum of values</summary>
		public double Sum
		{
			get { return m_mean * m_count; }
		}

		/// <summary>Mean value</summary>
		public double Mean
		{
			get { return m_mean; }
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
		public T Add(double value)
		{
			AddInternal(value);
			return (T)this;
		}
		IStatSingleVariable IStatSingleVariable.Add(double value)
		{
			return Add(value);
		}
		protected virtual void AddInternal(double value)
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
		public static double Running(double value, double mean, int count)
		{
			return mean + (value - mean) / (1 + count);
		}
	}
	public class Avr :Avr<Avr>
	{
		public Avr()
			:base()
		{}
		public Avr(double mean, int count)
			:base(mean, count)
		{}
		public Avr(Avr rhs)
			:base(rhs)
		{}
	}

	/// <summary>Running Average with standard deviation and variance</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class AvrVar<T> :Avr<T> ,IStatMeanAndVariance ,IStatSingleVariable where T:AvrVar<T>
	{
		//' let: D(k) = X(k) - avr(k-1)           => X(k) = D(k) + avr(k-1)
		//'  avr(k-1) = SUM{X(k-1)} / (k-1)       => SUM{X(k-1)} = (k-1)*avr(k-1)
		//'  avr(k)   = (SUM{X(k-1)} + X(k)) / k
		//'           = ((k-1)*avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) - avr(k-1) + D(k) + avr(k-1)) / k
		//'           = (k*avr(k-1) + D(k)) / k
		//'           = avr(k-1) + D(k) / k
		//' Running standard variance:
		//'  var(k)   = (1/(k-1)) * SUM{(X(k) - avr(k))�}
		//' (k-1)*var(k) = SUM{(X(k) - avr(k))�}
		//'              = SUM{(X(k)� - 2*avr(k)*X(k) + avr(k)�}
		//'              = SUM{X(k)�} - 2*avr(k)*SUM{X(k)} + k*avr(k)�
		//'              = SUM{X(k)�} - 2*avr(k)*k*avr(k) + k*avr(k)�
		//'              = SUM{X(k)�} - 2*k*avr(k)� + k*avr(k)�
		//'              = SUM{X(k)�} - k*avr(k)�
		//' so:
		//'  (k-2)*var(k-1) = SUM{X(k-1)�} - (k-1)*avr(k-1)�
		//' taking:
		//'  (k-1)*var(k) - (k-2)*var(k-1) = SUM{X(k)�} - k*avr(k)� - SUM{X(k-1)�} + (k-1)*avr(k-1)�
		//'                                = X(k)� - k*avr(k)� + (k-1)*avr(k-1)�
		//'                                = X(k)�                               - k*(avr(k-1) + D(k)/k)�                       + k*avr(k-1)� - avr(k-1)�
		//'                                = (D(k) + avr(k-1))�                  - k*(avr(k-1) + D(k)/k)�                       + k*avr(k-1)� - avr(k-1)�
		//'                                = D(k)� + 2*D(k)*avr(k-1) + avr(k-1)� - k*(avr(k-1)� + 2*D(k)*avr(k-1)/k + D(k)�/k�) + k*avr(k-1)� - avr(k-1)�
		//'                                = D(k)� + 2*D(k)*avr(k-1) + avr(k-1)� - k*avr(k-1)� - 2*D(k)*avr(k-1) - D(k)�/k      + k*avr(k-1)� - avr(k-1)�
		//'                                = D(k)� - D(k)�/k
		//'                                = ((k-1)/k) * D(k)�

		protected double m_var;
		
		public AvrVar()
			:base()
		{
			m_var = 0;
		}
		public AvrVar(double mean, double var, int count)
			:base(mean, count)
		{
			m_var = var;
		}
		public AvrVar(AvrVar<T> rhs)
			:base(rhs)
		{
			m_var = rhs.m_var;
		}

		/// <summary>Population standard deviation.<para/>Use when all data values in a set have been considered.</summary>
		public double PopStdDev
		{
			get { return Math_.Sqrt(PopStdVar); }
		}

		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdDev
		{
			get { return Math_.Sqrt(SamStdVar); }
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
		protected override void AddInternal(double value)
		{
			++m_count;
			var diff = value - m_mean;
			var inv_count = 1.0 / m_count; // don't remove this, conversion to double here is needed for accuracy
			m_mean += diff * inv_count;
			m_var += diff * diff * ((m_count - 1) * inv_count);
		}
	}
	public class AvrVar :AvrVar<AvrVar>
	{
		public AvrVar()
			:base()
		{}
		public AvrVar(double mean, double var, int count)
			:base(mean, var, count)
		{}
		public AvrVar(AvrVar rhs)
			:base(rhs)
		{}
	}

	/// <summary>Running average with standard deviation and min/max range</summary>
	[DebuggerDisplay("{Mean ({PopStdDev}) N={Count} R=[{Min},{Max}]")]
	public class AvrVarMinMax<T> :AvrVar<T> ,IStatMeanAndVariance ,IStatSingleVariable where T:AvrVarMinMax<T>
	{
		protected double m_min;
		protected double m_max;

		public AvrVarMinMax()
			:base()
		{
			m_min = double.MaxValue;
			m_max = double.MinValue;
		}
		public AvrVarMinMax(double mean, double var, int count, double min, double max)
			:base(mean, var, count)
		{
			m_min = min;
			m_max = max;
		}
		public AvrVarMinMax(AvrVarMinMax<T> rhs)
			:base(rhs)
		{
			m_min = rhs.m_min;
			m_max = rhs.m_max;
		}

		/// <summary>Return the minimum value seen</summary>
		public double Min
		{
			get
			{
				if (Count == 0) throw new Exception("No values added, minimum is undefined");
				return m_min;
			}
		}

		/// <summary>Return the maximum value seen</summary>
		public double Max
		{
			get
			{
				if (Count == 0) throw new Exception("No values added, maximum is undefined");
				return m_max;
			}
		}

		/// <summary>The centre of the range</summary>
		public double Mid
		{
			get { return (Min + Max) / 2; }
		}

		/// <summary>The value range</summary>
		public RangeF Range
		{
			get { return new RangeF(Min, Max); }
		}

		/// <summary>Reset the stats</summary>
		public override void Reset()
		{
			m_min = double.MaxValue;
			m_max = double.MinValue;
			base.Reset();
		}

		/// <summary>Add a single value to the stat</summary>
		protected override void AddInternal(double value)
		{
			if (value > m_max) m_max = value;
			if (value < m_min) m_min = value;
			base.AddInternal(value);
		}
	}
	public class AvrVarMinMax :AvrVarMinMax<AvrVarMinMax>
	{
		public AvrVarMinMax()
			:base()
		{}
		public AvrVarMinMax(double mean, double var, int count, double min, double max)
			:base(mean, var, count, min, max)
		{}
		public AvrVarMinMax(AvrVarMinMax rhs)
			:base(rhs)
		{}
	}

	/// <summary>Exponential Moving Average</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class ExpMovingAvr<T> :IStatMeanAndVariance ,IStatSingleVariable where T:ExpMovingAvr<T>
	{
		//'  avr(k) = a * X(k) + (1 - a) * avr(k-1)
		//'         = a * X(k) + avr(k-1) - a * avr(k-1)
		//'         = avr(k-1) + a * X(k) - a * avr(k-1)
		//'         = avr(k-1) + a * (X(k) - avr(k-1))
		//'    'a' is the exponential smoothing factor between (0,1)
		//'    define: a = 2 / (N + 1), where 'N' is roughly the window size of an equivalent moving window average
		//'    The interval over which the weights decrease by a factor of two (half-life) is approximately N/2.8854
		//' Exponential moving variance:
		//' (k-1)var(k) = SUM{w(k) * U(k)�}, where: U(k) = X(k) - avr(k)
		//'             = w(1)*U(1)� + w(2)*U(2)� + ... + w(k)*U(k)�, where: w(1)+w(2)+...+w(k) = k
		//' If we say:  w(k) = k * a, ('a' between (0,1) as above) then SUM{w(k-1)} = k * (1-a)
		//' so consider var(k-1):
		//'  (k-2)var(k-1) = w(1)*U(1)� + w(2)*U(2)� + ... + w(k-1)*U(k-1)�, where: w(1)+w(2)+...+w(k-1) = k - 1
		//' when we add the next term:
		//'  (k-1)var(k)   = w(1)*U(1)� + w(2)*U(2)� + ... + w(k-1)*U(k-1)� + w(k)*U(k)� (note w(1)..w(k-1) will be different values to above)
		//' we need:
		//'   k = k*a + k*(1-a) = w(k) + b*SUM{w(k-1)}
		//' => k*(1-a) = b*SUM{w(k-1)}
		//'          b = (1-a)*k/SUM{w(k-1)}
		//'            = (1-a)*k/(k-1)
		//' so:
		//' (k-1)var(k) = a*k*U(k)� + b*(k-2)var(k-1)
		//'             = a*k*U(k)� + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'  let: D(k) = X(k) - avr(k-1)
		//'       U(k) = X(k) - avr(k-1) + avr(k-1) - avr(k)
		//'            = D(k) + avr(k-1) - avr(k)
		//'            = D(k) - (avr(k) - avr(k-1))
		//'            = D(k) - a * (X(k) - avr(k-1))
		//'            = D(k) - a * D(k)
		//'            = (1-a)*D(k)
		//' then:
		//' (k-1)var(k) = a*k*U(k)� + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'             = a*k*(1-a)�*D(k)� + (1-a)*(k/(k-1)) * (k-2)var(k-1)
		//'             = a*k*b�*D(k)� + (b*k/(k-1)) * (k-2)var(k-1)         where: b = (1-a)
		//'             = (b*k/(k-1))*((a*b*(k-1)*D(k)� + (k-2)var(k-1))
	
		protected double m_mean;
		protected double m_var;
		protected int    m_size;
		protected int    m_count;

		public ExpMovingAvr()
			:this(int.MaxValue)
		{}
		public ExpMovingAvr(int window_size)
		{
			Reset(window_size);
		}
		public ExpMovingAvr(ExpMovingAvr<T> rhs)
		{
			m_mean  = rhs.m_mean;
			m_var   = rhs.m_var;
			m_size  = rhs.m_size;
			m_count = rhs.m_count;
		}

		/// <summary>
		/// The size of an equivalent moving average window size.
		/// Note: Changing the window size dynamically does not significantly
		/// change the mean (so long as 'Count' is larger than the old/new window size)
		/// but does produce different standard deviation values</summary>
		public int WindowSize
		{
			get { return m_size; }
			set { m_size = value; }
		}

		/// <summary>The number of data points added to this average</summary>
		public int Count
		{
			get { return m_count; }
		}

		/// <summary>Sum of values</summary>
		public double Sum
		{
			get { return m_mean * m_count; }
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

		/// <summary>Reset the stats</summary>
		public virtual void Reset(int window_size)
		{
			if (window_size < 0) throw new ArgumentOutOfRangeException(nameof(window_size), "Window size must be greater than zero");
			m_size  = window_size;
			m_count = 0;
			m_mean  = 0.0;
			m_var   = 0.0;
		}
		public void Reset()
		{
			Reset(WindowSize);
		}

		/// <summary>Add a value to the moving average</summary>
		public T Add(double value)
		{
			AddInternal(value);
			return (T)this;
		}
		IStatSingleVariable IStatSingleVariable.Add(double value)
		{
			return Add(value);
		}
		protected virtual void AddInternal(double value)
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
	public class ExpMovingAvr :ExpMovingAvr<ExpMovingAvr>
	{
		public ExpMovingAvr()
			:base()
		{}
		public ExpMovingAvr(int window_size)
			:base(window_size)
		{}
		public ExpMovingAvr(ExpMovingAvr rhs)
			:base(rhs)
		{}
	}

	/// <summary>Exponential Moving Average and min/max</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count} R=[{Min},{Max}]")]
	public class ExpMovingAvrMinMax<T> :ExpMovingAvr<T> where T:ExpMovingAvrMinMax<T>
	{
		protected double m_min;
		protected double m_max;

		public ExpMovingAvrMinMax()
			:this(0)
		{}
		public ExpMovingAvrMinMax(int window_size, double min = double.MaxValue, double max = double.MinValue)
			:base(window_size)
		{
			m_min = min;
			m_max = max;
		}
		public ExpMovingAvrMinMax(ExpMovingAvrMinMax<T> rhs)
			:base(rhs)
		{
			m_min = rhs.m_min;
			m_max = rhs.m_max;
		}

		/// <summary>Return the minimum value seen</summary>
		public double Min
		{
			get
			{
				if (Count == 0) throw new Exception("No values added, minimum is undefined");
				return m_min;
			}
		}

		/// <summary>Return the maximum value seen</summary>
		public double Max
		{
			get
			{
				if (Count == 0) throw new Exception("No values added, maximum is undefined");
				return m_max;
			}
		}

		/// <summary>The centre of the range</summary>
		public double Mid
		{
			get { return (Min + Max) / 2; }
		}

		/// <summary>The value range</summary>
		public RangeF Range
		{
			get { return new RangeF(Min, Max); }
		}

		/// <summary>Reset the stats</summary>
		public override void Reset(int window_size)
		{
			m_min = double.MaxValue;
			m_max = double.MinValue;
			base.Reset(window_size);
		}

		/// <summary>Add a single value to the stat</summary>
		protected override void AddInternal(double value)
		{
			if (value > m_max) m_max = value;
			if (value < m_min) m_min = value;
			base.AddInternal(value);
		}
	}
	public class ExpMovingAvrMinMax :ExpMovingAvrMinMax<ExpMovingAvrMinMax>
	{
		public ExpMovingAvrMinMax()
			:base()
		{}
		public ExpMovingAvrMinMax(int window_size, double min = double.MaxValue, double max = double.MinValue)
			:base(window_size, min, max)
		{}
		public ExpMovingAvrMinMax(ExpMovingAvrMinMax rhs)
			:base(rhs)
		{}
	}

	/// <summary>Running average of two variables with standard deviation, variance, and correlation coefficient</summary>
	public class Correlation :IStatTwoVariables
	{
		protected int m_count;
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
		public int Count
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
			get { return Math_.Sqrt(PopStdVarX); }
		}
		public double PopStdDevY
		{
			get { return Math_.Sqrt(PopStdVarY); }
		}

		/// <summary>Sample standard deviation.<para/>Use the when the data values used are only a sample of the total population</summary>
		public double SamStdDevX
		{
			get { return Math_.Sqrt(SamStdVarX); }
		}
		public double SamStdDevY
		{
			get { return Math_.Sqrt(SamStdVarY); }
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
			get { return m_count > 0 && !Math_.FEql(m_var_xy, 0) ? m_var_xy / Math_.Sqrt(m_var_x * m_var_y) : 0.0; }
		}

		/// <summary>Returns a "best-fit" linear polynomial for the data added</summary>
		public Monic LinearRegression
		{
			get
			{
				// For typical X vs. Y data, call 'Add(x,y)' then use 'LinearRegression' to return the best fit line
				var A = CorrCoeff * SamStdDevY / SamStdDevX;
				var B = MeanY - A * MeanX;
				return new Monic(A,B);
			}
		}

		/// <summary>This is 'r�', i.e. a measure of how well the data fit the Linear Regression line</summary>
		public double CoeffOfDetermination
		{
			get { return Math_.Sqr(CorrCoeff); }
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

	/// <summary>Moving Window Average</summary>
	[DebuggerDisplay("{Mean} ({PopStdDev}) N={Count}")]
	public class MovingAvr :IStatMeanAndVariance ,IStatSingleVariable
	{
		// Let: D(k) = X(k) - X(k-N) => X(k-N) = X(k) - D(k)
		// Average:
		//   avr(k) = avr(k-1) + (X(k) - X(k-N)) / N
		//          = avr(k-1) + D(k) / N
		private double[] m_window;
		private double   m_mean;
		private int      m_count;
		private int      m_i;

		public MovingAvr(int window_size)
		{
			Reset(window_size);
		}
		public MovingAvr(MovingAvr rhs)
			:this(rhs.m_window.Length)
		{
			Array.Copy(rhs.m_window, m_window, m_window.Length);
			m_mean  = rhs.m_mean;
			m_count = rhs.m_count;
			m_i     = rhs.m_i;
		}

		/// <summary>The size of the moving window</summary>
		public int WindowSize
		{
			get { return m_window.Length; }
		}

		/// <summary>The number of data points added to this average</summary>
		public int Count
		{
			get { return m_count; }
		}

		/// <summary>Sum of values</summary>
		public double Sum
		{
			get { return m_mean * m_count; }
		}

		/// <summary>The average</summary>
		public double Mean
		{
			get { return m_mean; }
		}

		/// <summary>Variance</summary>
		private double Var
		{
			get
			{
				double var = 0.0;
				int count = m_count;
				int end = m_window.Length;
				for (int i = m_i; i-- != 0   && count != 0; --count) { double diff  = m_window[i] - m_mean; var += diff * diff; }
				for (int i = end; i-- != m_i && count != 0; --count) { double diff  = m_window[i] - m_mean; var += diff * diff; }
				return var;
			}
		}

		// NOTE: no recursive variance because we would need to buffer the averages as well
		// so that we could remove (X(k-N) - avr(k-N))� at each iteration
		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
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
			get { return m_count > 0 ? Var * (1.0 / (m_count - 0)) : 0.0; }
		}
		public double SamStdVar
		{
			get { return m_count > 1 ? Var * (1.0 / (m_count - 1)) : 0.0; }
		}

		/// <summary>Reset the stats</summary>
		public void Reset(int window_size)
		{
			m_window = new double[window_size];
			m_mean   = 0.0;
			m_count  = 0;
			m_i      = 0;
		}

		/// <summary>Add a value to the moving average</summary>
		public MovingAvr Add(double value)
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
			return this;
		}
		IStatSingleVariable IStatSingleVariable.Add(double value)
		{
			return Add(value);
		}
	}

	/// <summary>A class for tracking the distribution of a variable</summary>
	public class Distribution
	{
		public Distribution(double bucket_size, string name = null)
		{
			Name = name;
			BucketSize = bucket_size;
			Buckets = new BucketCollection(bucket_size);
			Reset();
		}

		/// <summary>A name for the distribution</summary>
		public string Name
		{
			get;
			set;
		}

		/// <summary>Get/Set the bucket size. Setting the bucket size resets the bucket collection</summary>
		public double BucketSize
		{
			get;
			private set;
		}

		/// <summary>The span of values added</summary>
		public RangeF XRange
		{
			get { return Buckets.XRange; }
		}

		/// <summary>The range of counts</summary>
		public RangeF YRange
		{
			get { return new RangeF(Buckets.YRange.Beg * Normalisation, Buckets.YRange.End * Normalisation); }
		}

		/// <summary>The distribution data as an ordered list of buckets</summary>
		public BucketCollection Buckets
		{
			[DebuggerStepThrough] get;
			private set;
		}
		public class BucketCollection
		{
			// Notes:
			//  - Buckets span from -inf,+inf, but we only store buckets with values in
			//  - There is a bucket whose centre value is 0. (i.e. it spans from [-bucket_radius,+bucket_radius))

			private List<Bucket> m_buckets;
			private double m_bucket_size;

			public BucketCollection(double bucket_size)
			{
				m_buckets = new List<Bucket>();
				m_bucket_size = bucket_size;
			}

			/// <summary>Reset the collection</summary>
			internal void Clear()
			{
				m_buckets.Clear();
			}

			/// <summary>The number of actual buckets</summary>
			internal int Count
			{
				get { return m_buckets.Count; }
			}

			/// <summary>Access buckets directly</summary>
			internal Bucket this[int i]
			{
				get
				{
					var r = XRange;
					return
						i <                0 ? new Bucket(r.Beg - m_bucket_size) :
						i >= m_buckets.Count ? new Bucket(r.End + m_bucket_size) :
						m_buckets[i];
				}
			}

			/// <summary>Return the bucket that would contain 'value'. If 'insert' is true, the bucket will be added to the collection if not already there</summary>
			public Bucket this[double value]
			{
				get { return this[value, false]; }
			}
			internal Bucket this[double value, bool insert]
			{
				get
				{
					// Quantise 'value' to a bucket value
					var bval = BucketValue(value);

					// Look for an existing bucket
					var idx = m_buckets.BinarySearch(x => !Math_.FEql(x.Value, bval) ? x.Value.CompareTo(bval) : 0);
					return idx >= 0
						? m_buckets[idx]
						: (insert ? m_buckets.Insert2(idx = ~idx, new Bucket(bval)) : new Bucket(bval));
				}
			}

			/// <summary>The span of bucket central values</summary>
			internal RangeF XRange
			{
				get { return m_buckets.Count != 0 ? new RangeF(m_buckets.Front().Value - m_bucket_size, m_buckets.Back().Value + m_bucket_size) : RangeF.Zero; }
			}

			/// <summary>The span of bucket count values</summary>
			internal RangeF YRange
			{
				get { return m_buckets.Count != 0 ? new RangeF(Math.Max(0, m_buckets.Min(x => x.Count)), Math.Min(0, m_buckets.Max(x => x.Count))) : RangeF.Zero; }
			}

			/// <summary>Set 'Normalisation' so that the maximum count returns a value of 'len' in 'this[i]'</summary>
			internal double NormalisingFactor(double len)
			{
				var sum = m_buckets.Sum(x => x.Count);
				return Math_.Div(len, sum, 1.0);
			}

			/// <summary>Return the bucket central value nearest to 'value'</summary>
			private double BucketValue(double value)
			{
				return m_bucket_size * Math.Floor((value + 0.5*m_bucket_size) / m_bucket_size);
			}

			/// <summary>Enumerate the buckets</summary>
			internal IEnumerable<Bucket> BucketRange()
			{
				if (Count == 0)
					yield break;

				// Include a zero bucket on each side of a run of non-zero buckets
				for (int i = -1; i != Count+1; ++i)
				{
					// Current and Next bucket
					var b0 = this[i];
					var b1 = this[i+1];

					yield return b0;

					// The distance between b1 and b0
					var dist = b1.Value - b0.Value - Math_.TinyD;

					// If there is a gap of at least two, insert two zero buckets
					if (dist > 2*m_bucket_size)
					{
						yield return this[b0.Value + m_bucket_size];
						yield return this[b1.Value - m_bucket_size];
					}
					else if (dist > 1*m_bucket_size)
					{
						yield return this[b0.Value + m_bucket_size];
					}
				}
			}
		}

		/// <summary>The number of values added to the distribution</summary>
		public int Count
		{
			get;
			private set;
		}

		/// <summary>A normalisation factor that effects values returned from 'this[i]'</summary>
		public double Normalisation
		{
			get;
			set;
		}

		/// <summary>Access the data as a continuous probability distribution</summary>
		public double this[double value]
		{
			get
			{
				// Get the buckets around 'value'
				var b0 = Buckets[value];
				var sign = value >= b0.Value ? +1 : -1;
				var b1 = Buckets[value + sign * BucketSize];

				// Blend the values from these buckets
				// Don't use 'SmoothStep', it adds "steps"
				var t = Math_.Frac(b0.Value, value, b1.Value);
				return Math_.Lerp(b0.Count, b1.Count, t);
			}
		}

		/// <summary>Reset the distribution</summary>
		public void Reset()
		{
			Buckets.Clear();
			Count = 0;
			Normalisation = 1.0;
		}

		/// <summary>Add a value to the distribution</summary>
		public void Add(double value)
		{
			Add(value, 1.0);
		}
		public void Add(double value, double count)
		{
			Buckets[value, true].Count += count;
			++Count;
		}

		/// <summary>Remove a value from the distribution</summary>
		public void Remove(double value)
		{
			Remove(value, 1.0);
		}
		public void Remove(double value, double count)
		{
			Buckets[value, true].Count -= count;
			--Count;
		}

		/// <summary>Set 'Normalisation' so that the maximum count returns a value of 'len' in 'this[i]'</summary>
		public void Normalise(double len = 1)
		{
			Normalisation = Buckets.NormalisingFactor(len);
		}

		/// <summary>
		/// Values for the given probability levels.
		/// 'probabilities' is an array of probability values ([0,1]) sorted from 0->1
		/// Returns the value for each probability. E.g for a probability of 0.5, the returned value has half the distribution less than it.</summary>
		public double[] Values(double[] probabilities)
		{
			Debug.Assert(int_.Range(probabilities.Length).All(i => i == 0 || probabilities[i] >= probabilities[i-1]));
			var values = new double[probabilities.Length];
			if (probabilities.Length == 0 || Buckets.Count == 0)
				return values;

			// Index into 'probabilities' and 'values'
			var p = 0;

			// Half bucket size
			var rad = 0.5 * BucketSize;

			// The accumulated probability
			var probability = 0.0;

			// Scales probabilities to un-normalised values
			var n = BucketSize / Buckets.NormalisingFactor(1.0);

			// Iterator through the buckets
			foreach (var bucket in Buckets.BucketRange())
			{
				// The central value of 'bucket'
				var val = bucket.Value;

				// Get the virtual buckets around 'val'
				var b0 = Buckets[val - BucketSize];
				var b1 = Buckets[val];
				var b2 = Buckets[val + BucketSize];

				// Probability is the area under the distribution.
				// This is made trickier by linear interpolation between buckets and missing buckets.
				var l = 0.5 * (b0.Count + b1.Count); // height of the left side of the bucket
				var c = b1.Count;                    // height of the centre of the bucket
				var r = 0.5 * (b1.Count + b2.Count); // height of the right side of the bucket

				// The probability for the left and right side of the bucket
				var lprob = 0.5 * (l + c) * rad;
				var rprob = 0.5 * (c + r) * rad;

				// Find values in the left side of the bucket
				for (; p != probabilities.Length && n*probabilities[p] < probability + lprob; ++p)
					values[p] = Math_.Lerp(val-rad, val, Math_.Frac(probability, n*probabilities[p], probability + lprob));

				// Accumulate the left side probability
				probability += lprob;

				// Find values in the right side of the bucket
				for (; p != probabilities.Length && n*probabilities[p] < probability + rprob; ++p)
					values[p] = Math_.Lerp(val, val+rad, Math_.Frac(probability, n*probabilities[p], probability + rprob));

				// Accumulate probability
				probability += rprob;
			}

			// Fill the remainder with a value greater that the max value in the distribution
			for (;p != probabilities.Length; ++p)
				values[p] = Buckets[Buckets.Count].Value - BucketSize;

			return values;
		}

		/// <summary>
		/// Probabilities for the given values.
		/// 'values' is an array of values sorted from sorted from smallest to largest
		/// Returns the probability for each value ([0,1]). E.g. for a value == median, the returned probability is 0.5</summary>
		public double[] Probability(double[] values)
		{
			Debug.Assert(int_.Range(values.Length).All(i => i == 0 || values[i] >= values[i-1]));
			var probabilities = new double[values.Length];
			if (values.Length == 0 || Buckets.Count == 0)
				return probabilities;

			// Index into 'values' and 'probabilities'
			var v = 0;

			// Half bucket size
			var rad = 0.5 * BucketSize;

			// The accumulated un-normalised probability (i.e. area under the graph)
			var probability = 0.0;

			// Iterate through the buckets
			foreach (var bucket in Buckets.BucketRange())
			{
				// The central value of 'bucket'
				var val = bucket.Value;

				// Get the virtual buckets around 'val'
				var b0 = Buckets[val - BucketSize];
				var b1 = Buckets[val];
				var b2 = Buckets[val + BucketSize];

				// Probability is the area under the distribution.
				// This is made trickier by linear interpolation between buckets and missing buckets.
				var l = 0.5 * (b0.Count + b1.Count); // height of the left side of the bucket
				var c = b1.Count;                    // height of the centre of the bucket
				var r = 0.5 * (b1.Count + b2.Count); // height of the right side of the bucket

				// The probability for the left and right side of the bucket
				var lprob = 0.5 * (l + c) * rad;
				var rprob = 0.5 * (c + r) * rad;

				// Find probabilities in the left side of the bucket
				for (; v != values.Length && values[v] < val; ++v)
					probabilities[v] = probability + Math_.Frac(val-rad, values[v], val) * lprob;

				// Accumulate the left side probability
				probability += lprob;

				// Find probabilities in the right side of the bucket
				for (; v != values.Length && values[v] < val+rad; ++v)
					probabilities[v] = probability + Math_.Frac(val, values[v], val+rad) * rprob;

				// Accumulate probability
				probability += rprob;
			}

			// Fill the remainder with 1.0
			for (;v != values.Length; ++v)
				probabilities[v] = probability;

			// Normalise probabilities
			for (var i = 0; i != probabilities.Length; ++i)
				probabilities[i] /= probability;

			return probabilities;
		}

		/// <summary>Output the distribution as CSV data</summary>
		public string ToCSV()
		{
			var sb = new StringBuilder();

			sb.AppendLine("Value,Count");
			foreach (var b in Buckets.BucketRange())
				sb.AppendLine($"{b.Value},{b.Count}");

			return sb.ToString();
		}

		/// <summary>A range of values</summary>
		[DebuggerDisplay("{Value} N={Count}")]
		public class Bucket
		{
			public Bucket(double value)
			{
				Value = value;
			}

			/// <summary>The central value of the bucket</summary>
			public double Value { get; private set; }

			/// <summary>The number of values added to this bucket</summary>
			public double Count { get; internal set; }
		}
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using System.Text;

	[TestFixture] public class TestStat
	{
		[Test] public void Stat()
		{
			double[] num = new[]{2.0,4.0,7.0,3.0,2.0,-5.0,-4.0,1.0,-7.0,3.0,6.0,-8.0};
			
			var s = new AvrVar();
			for (int i = 0; i != num.Length; ++i)
				s.Add(num[i]);
			
			Assert.AreEqual(num.Length, s.Count);
			Assert.AreEqual(4.0                               ,s.Sum       ,Math_.TinyD);
			Assert.AreEqual(1.0/3.0                           ,s.Mean      ,Math_.TinyD);
			Assert.AreEqual(4.83621                           ,s.PopStdDev ,0.00001);
			Assert.AreEqual(23.38889                          ,s.PopStdVar ,0.00001);
			Assert.AreEqual(5.0512524699475787686684767441111 ,s.SamStdDev ,Math_.TinyD);
			Assert.AreEqual(25.515151515151515151515151515152 ,s.SamStdVar ,Math_.TinyD);
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
				double sum = 0.0f; for (int j = 0; j != count; ++j) sum += buf[j];
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
		[Test] public void ExpMovingAvrMinMax()
		{
			const int BufSz = 13;
			
			var rng = new Random();
			var s = new ExpMovingAvrMinMax(BufSz);
			const double a = 2.0 / (BufSz + 1);
			double ema = 0, min = double.MaxValue, max = double.MinValue;
			int count = 0;

			for (int i = 0; i != BufSz * 2; ++i)
			{
				double v = rng.NextDouble();
				if (count < BufSz) { ++count; ema += (v - ema) / count; }
				else               { ema = a * v + (1 - a) * ema; }
				min = Math.Min(min, v);
				max = Math.Max(max, v);
				s.Add(v);
				Assert.AreEqual(ema ,s.Mean ,0.00001);
				Assert.AreEqual(min ,s.Min  ,0.00001);
				Assert.AreEqual(max ,s.Max ,0.00001);
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
				Assert.AreEqual(corr.CorrCoeff, 1.0, Math_.TinyD);
			} {
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr1[i]);
				Assert.AreEqual(corr.CorrCoeff, 1.0, Math_.TinyD);
			} {
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr2[i]);
				Assert.AreEqual(corr.CorrCoeff, -1.0, Math_.TinyD);
			}{
				var corr = new Correlation();
				for (int i = 0; i != arr0.Length; ++i)
					corr.Add(arr0[i], arr3[i]);
				Assert.AreEqual(corr.CorrCoeff, 0.0, Math_.TinyD);
			}
		}
		[Test] public void Probability()
		{
			// Check we're independent of bucket size
			foreach (var bk_size in new [] {0.1, 0.5, 1.0, 2.0, 5.0 })
			{
				var pd = new Distribution(bk_size);
				pd.Add(1);
				pd.Add(2);
				pd.Add(2);
				pd.Add(3);
				pd.Add(3);
				pd.Add(3);
				pd.Add(5);
				pd.Add(8);
				pd.Add(8);

				var sb = new StringBuilder();
				foreach (var x in double_.Range(pd.XRange, 0.1))
					sb.AppendLine("{0},{1}".Fmt(x, pd[x]));
				sb.ToFile(@"P:\dump\test.csv");

				{
					var probabilities = new[] {0.0, 0.1, 0.5, 0.8, 1.0 };
					var vals = pd.Values(probabilities);
					var prob = pd.Probability(vals);

					Assert.True(prob.All(x => x.Within(0.0, 1.0)));
					for (int i = 0; i != prob.Length; ++i)
						Assert.True(Math_.FEql(prob[i], probabilities[i]));
				}
				{
					var values = new[] {-10.0, -1.0, 0.0, 0.5, 3.0, 4.0, 5.0, 10.0 };
					var prob = pd.Probability(values);
					var vals = pd.Values(prob);
					var prob2 = pd.Probability(vals);
					
					Assert.True(prob.All(x => x.Within(0.0, 1.0)));
					for (int i = 0; i != vals.Length; ++i)
					{
						// Values with 0 or 1 probably are outside the distribution
						// so we can't get the value back
						if (prob[i] == 0.0)
						{
							Assert.True(Math_.FEql(prob[i], prob2[i]));
							Assert.True(values[i] <= vals[i]);
							continue;
						}
						if (prob[i] == 1.0)
						{
							Assert.True(Math_.FEql(prob[i], prob2[i]));
							Assert.True(values[i] >= vals[i]);
							continue;
						}

						// Values within ranges of zero probability are ambiguous, so long as the
						// probability for the values is the same, it's ok.
						Assert.True(Math_.FEql(vals[i], values[i]) || Math_.FEql(prob[i], prob2[i]));
					}
				}
			}
		}
	}
}
#endif

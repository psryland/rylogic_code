//*********************************************************************
// Stat
//  Copyright (c) Rylogic Ltd 2008
//*********************************************************************
#pragma once

#ifndef NOMINMAX
#error "NOMINMAX must be defined before including maths library headers"
#endif

#include <cmath>
#include <cfloat>
#include <cassert>
#include <functional>
#include <utility>

namespace pr
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
	// 'real' is typically a floating point type, although this does
	// work for any type that defines the necessary operators.
	template <typename real = double> class Stat
	{
		typedef unsigned int uint;
		real  m_mean;
		real  m_var;
		real  m_min;
		real  m_max;
		uint  m_count;

	public:
		uint Count() const     { return m_count; }
		real Mean() const      { return m_mean; }
		real Sum() const       { return m_mean * m_count; }
		real Minimum() const   { return m_min; }
		real Maximum() const   { return m_max; }

		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		real PopStdDev() const { return static_cast<real>(sqrt(PopStdVar())); }
		real SamStdDev() const { return static_cast<real>(sqrt(SamStdVar())); }
		real PopStdVar() const { return static_cast<real>(m_var * (1.0 / (m_count + (m_count == 0)))); }
		real SamStdVar() const { return static_cast<real>(m_var * (1.0 / (m_count - (m_count != 1)))); }

		Stat()
		{
			Reset();
		}

		// Reset the stats
		void Reset()
		{
			m_count = 0;
			m_mean  = real();
			m_var   = real();
			m_min   = std::numerical_limits<real>::max();
			m_max   = std::numerical_limits<real>::lowest();
		}

		// Accumulate statistics for 'value' in a single pass.
		// Note, this method is more accurate than the sum of squares, square of sums approach.
		template <typename MinFunc, typename MaxFunc> void Add(real const& value, MinFunc min_of, MaxFunc max_of)
		{
			++m_count;
			auto diff = value - m_mean;
			auto inv_count = 1.0 / m_count;
			m_mean += diff * inv_count;
			m_var  += diff * diff * ((m_count - 1) * inv_count);
			m_min = min_of(value, m_min);
			m_max = max_of(value, m_max);
		}
		void Add(real const& value)
		{
			Add(value
				,[](real const& l, real const& r){ return std::min(l,r); }
				,[](real const& l, real const& r){ return std::max(l,r); });
		}
	};

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
	// 'real' is typically a floating point type, although this does
	// work for any type that defines the necessary operators.
	template <typename real = double> struct ExpMovingAvr
	{
		typedef unsigned int uint;
		real m_mean;
		real m_var;
		uint m_size;
		uint m_count;

	public:
		uint Count() const { return m_count; }
		real Mean() const { return m_mean; }

		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		// Note: for a moving variance the choice between population/sample sd is a bit arbitrary
		real PopStdDev() const { return static_cast<real>(sqrt(PopStdVar())); }
		real SamStdDev() const { return static_cast<real>(sqrt(SamStdVar())); }
		real PopStdVar() const { return m_var * (1.0 / (m_count + (m_count == 0))); }
		real SamStdVar() const { return m_var * (1.0 / (m_count - (m_count != 1))); }

		ExpMovingAvr(uint window_size)
		{
			Reset(window_size);
		}
		void Reset(uint window_size)
		{
			m_size  = window_size;
			m_count = 0;
			m_mean  = real();
			m_var   = real();
		}
		void Add(real const& value)
		{
			if (m_count >= m_size)
			{
				++m_count;
				auto diff = value - m_mean;
				auto a = 2.0 / (m_size + 1.0);
				auto b = 1 - a;
				m_mean += diff * a;
				m_var = (b*m_count/(m_count-1)) * (a*b*(m_count-1)*diff*diff + m_var);
			}
			else // use standard mean/var until 'm_size' is reached
			{
				++m_count;
				auto diff = value - m_mean;
				auto inv_count = 1.0f / m_count;
				m_mean += diff * inv_count;
				m_var  += diff * diff * ((m_count - 1) * inv_count);
			}
		}
	};

	// A moving window average
	// Let: D(k) = X(k) - X(k-N) => X(k-N) = X(k) - D(k)
	// Average:
	//   avr(k) = avr(k-1) + (X(k) - X(k-N)) / N
	//          = avr(k-1) + D(k) / N
	// 'real' is typically a floating point type, although this does
	// work for any type that defines the necessary operators.
	template <uint MaxWindowSize, typename real = double> class MovingAvr
	{
		typedef unsigned int uint;
		real m_window[MaxWindowSize], *m_in;
		real m_mean;
		uint m_count;
		uint m_size;

		real Var() const
		{
			auto var = real();
			auto count = m_count;
			auto const* end = &m_window[0] + m_size;
			for (auto i = m_in; i-- != m_window && count; --count) { auto diff = *i - m_mean; var += diff * diff; }
			for (auto i = end;  i-- != m_window && count; --count) { auto diff = *i - m_mean; var += diff * diff; }
			return var;
		}

	public:
		uint Count() const { return m_count; }
		real Mean() const { return m_mean; }

		// NOTE: no recursive variance because we would need to buffer the averages as well
		// so that we could remove (X(k-N) - avr(k-N))² at each iteration
		// Use the population standard deviation when all data values in a set have been considered.
		// Use the sample standard deviation when the data values used are only a sample of the total population
		real PopStdDev() const { return static_cast<real>(sqrt(PopStdVar())); }
		real SamStdDev() const { return static_cast<real>(sqrt(SamStdVar())); }
		real PopStdVar() const { return Var() * (1.0 / (m_count + (m_count == 0))); }
		real SamStdVar() const { return Var() * (1.0 / (m_count - (m_count != 1))); }

		MovingAvr(uint window_size = MaxWindowSize)
		{
			Reset(window_size);
		}
		void Reset(uint window_size = MaxWindowSize)
		{
			assert(window_size <= MaxWindowSize);
			m_in    = &m_window[0];
			m_size  = window_size;
			m_mean  = real();
			m_count = 0;
		}
		void Add(real const& value)
		{
			if (m_count == m_size)
			{
				if (m_in == m_window + m_size) m_in = &m_window[0];
				auto diff = value - *m_in;
				auto inv_count = 1.0 / m_size;
				m_mean += diff * inv_count;
				*m_in++ = value;
			}
			else
			{
				++m_count;
				auto diff = value - m_mean;
				auto inv_count = 1.0 / m_count;
				m_mean += diff * inv_count;
				*m_in++ = value;
			}
		}
	};
}

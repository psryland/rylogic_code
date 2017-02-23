//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once

#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"

// TODO:
//  Make use of 'ComplexArray' for performance
//  Fix DFTBluestein
//  Fix Convolve
//  Add unit tests for DFT/Convolve

namespace pr
{
	// A stratified array of complex numbers
	template <typename Real> struct ComplexArray
	{
		using complex = std::complex<Real>;
		using ContR = std::vector<Real>;

		ContR m_real;
		ContR m_imag;

		ComplexArray()
			:m_real()
			,m_imag()
		{}
		ComplexArray(Real const* real, int length)
			:m_real(real, real + length)
			,m_imag(length)
		{}
		ComplexArray(Real const* real, Real const* imag, int length)
			:m_real(real, real + length)
			,m_imag(imag, imag + length)
		{}
		ComplexArray(complex const* rhs, int length)
			:m_real(length)
			,m_imag(length)
		{
			for (int i = 0; i != length; ++i)
			{
				m_real[i] = rhs[i].real();
				m_imag[i] = rhs[i].imag();
			}
		}

		// The length of the array
		size_t size() const
		{
			return m_real.size();
		}

		// Resize the array
		void resize(int size)
		{
			m_real.resize(size);
			m_imag.resize(size);
		}

		// A proxy object for assignment by complex number
		struct NumProxy
		{
			Real* m_real;
			Real* m_imag;

			NumProxy(Real* real, Real* imag)
				:m_real(real)
				,m_imag(imag)
			{}
			NumProxy& operator =(std::complex<Real> c)
			{
				*m_real = c.real();
				*m_imag = c.imag();
			}
			operator std::complex<Real>() const
			{
				return std::complex(*m_real, *m_imag);
			}
		};
		NumProxy operator[](int i) const
		{
			return NumProxy(&m_real[i], &m_imag[i]);
		}
	};

	namespace dft
	{
		// Convert a frequency domain value to a fractional buffer index. Returns -1 for out of range frequencies.
		template <typename Real> inline Real FIndexAt(Real freq, Real sampling_frequency, int buffer_size)
		{
			// Convert the frequency into a buffer index
			auto fidx = Real(freq * buffer_size / sampling_frequency);
			return fidx >= 0 && fidx < Real(buffer_size / 2) ? fidx : Real(-1);
		}

		// Convert a fractional buffer index to a frequency domain value.
		// Returns -1 if 'fidx' is outside the range [0, buffer_size/2)
		template <typename Real> inline Real FreqAt(Real fidx, Real sampling_frequency, int buffer_size)
	{
		if (fidx < 0 || fidx >= buffer_size/2) return Real(-1);
		return Real(sampling_frequency * fidx / buffer_size);
	}
	}

	// Sliding window Discrete Fourier Transform (DFT)
	template <typename Real> struct SlidingDFT
	{
	protected:

		// This code efficiently computes the discrete Fourier transforms (DFT) from a
		// continuous sequence of input values. It updates the DFT when each new time-domain
		// measurement arrives, effectively applying a sliding window over the last *N* samples.
		// This implementation applies the 'Hanning' window in order to minimise spectral leakage.
		// The update step has computational complexity O(N). If a new DFT is required every M
		// samples, and 'M < log2(N)', then this approach is more efficient than recalculating
		// the DFT from scratch each time.
		using complex = std::complex<Real>;
		using Buffer = std::vector<complex>;

		// Input signal ring buffer
		Buffer m_in;

		// The frequency domain values (*without* windowing).
		Buffer m_out;

		// Forward ('-tau * e^iw') and inverse ('+tau * e^iw') coefficients normalized (divided by WindowSize)
		Buffer m_coeffs;

		// The frequency that the values provided to 'Add' are sampled at.
		// Required to determine the output frequency range.
		Real m_sampling_frequency;

		// A damping factor guarantee stability.
		Real m_damping_factor;

		// The number of time-domain samples added to 'm_in'.
		int m_count;

	public:

		// Create a sliding DFT instance
		// 'window_size' is the number of samples in the sliding window. 
		// 'inverse' is true if this is actually an inverse DFT
		SlidingDFT()
			:SlidingDFT(0, false)
		{}
		SlidingDFT(int window_size, Real sampling_frequency, bool inverse = false)
			:m_in()
			,m_out()
			,m_coeffs()
			,m_sampling_frequency()
			,m_damping_factor(std::nexttoward(Real(1), Real(0)))
			,m_count()
		{
			if (window_size != 0)
				Reset(window_size, sampling_frequency, inverse);
		}

		// The size of the moving window
		int WindowSize() const
		{
			return int(m_in.size());
		}

		// Return the frequency domain range
		Range<Real> FreqRange() const
		{
			return Range<Real>(Real(0), Real(m_sampling_frequency / 2));
		}

		// Reset the Sliding DFT and set a window size
		// 'window_size' is the number of samples in the sliding window. 
		// 'inverse' is true if this is actually an inverse DFT
		void Reset(int window_size, Real sampling_frequency, bool inverse = false)
		{
			// Erase previous data
			m_in    .resize(0); m_in    .resize(window_size);
			m_out   .resize(0); m_out   .resize(window_size);
			m_coeffs.resize(0); m_coeffs.resize(window_size);
			m_sampling_frequency = sampling_frequency;
			m_count = 0;

			// Initialize 'e-to-the-i-thetas' for theta = 0..tau in increments of 1/N
			auto k = (!inverse ? +maths::tau : -maths::tau) * complex(0,1) / double(window_size);
			for (int i = 0; i != window_size; ++i)
				m_coeffs[i] = std::exp(k * Real(i));
		}

		// Add the next sample. Samples should represent the input signal sampled at 'm_sampling_frequency'
		void Add(Real value)
		{
			Add(complex(value, 0));
		}
		void Add(complex value)
		{
			// The window size
			auto N = WindowSize();

			// The ring buffer index
			auto idx = m_count % N;

			// The incoming and outgoing values
			auto old = m_in[idx];
			auto nue = m_in[idx] = value;

			// Update the DFT. Apply damping for stability
			auto damping = pow(m_damping_factor, Real(N));
			for (auto k = 0; k != N; ++k)
				m_out[k] = m_coeffs[k] * (m_damping_factor*m_out[k] - damping*old + nue);

			++m_count;
		}

		// Return the spectral power for the given frequency.
		// Note: this must be less than 'm_sampling_frequency / 2.0' due to the Nyquist limit.
		// It should be in the range returned from 'FreqRange'.
		// 'freq' is quantised to the nearest buffer index.
		Real Power(Real freq) const
		{
			auto fidx = FIndexAt(freq);
			if (fidx < 0)
				return Real(0);

			// Return the spectral power for the buffer index
			auto idx = int(fidx);
			auto a = Length(ValueAt(idx+0));
			auto b = Length(ValueAt(idx+1));
			return Real(Lerp(a, b, fidx - idx));
		}

		// Return the frequency domain values
		std::vector<Real> Spectrum() const
		{
			std::vector<Real> buf(WindowSize() / 2);
			Spectrum(buf.data(), int(buf.size()));
			return std::move(buf);
		}

		// Return the frequency domain values in 'buffer'
		void Spectrum(Real* buffer, int length)
		{
			assert("Insufficient buffer space" && length >= WindowSize() / 2);
			auto N = WindowSize();

			// 'm_out' contains the spectrum twice, with the upper portion mirrored
			// Only need to copy range [0,N/2). Apply a 'Hanning window' to minimise spectral leakage.
			auto a = m_out[N-1];
			auto b = m_out[0];
			auto c = m_out[1];
			for (int i = 0; i != N/2; ++i, a = b, b = c, c = m_out[i+1])
				buffer[i] = Real(Length(0.5*b - 0.25*(a + c)));
		}

		// Convert a frequency domain value to a fractional buffer index. Returns -1 for out of range frequencies.
		Real FIndexAt(Real freq) const
		{
			return dft::FIndexAt(freq, m_sampling_frequency, WindowSize());
		}

		// Return the frequency domain value at buffer index 'index' with a 'Hanning window' applied.
		complex ValueAt(int idx) const
		{
			auto N = WindowSize();
			idx += N;
			return 0.5 * m_out[idx % N] - 0.25 * (m_out[(idx-1) % N] + m_out[(idx+1) % N]);
		}
	};

	// Naive DFT implementation, used for testing. O(N²)
	template <typename Real> void DFTNaive(Real const* in, std::complex<Real>* out, int N)
	{
		for (int i = 0; i != N; ++i)
		{
			out[i] = 0.0;
			for (int j = 0; j != N; ++j)
			{
				auto a = -maths::tau * i * j / double(N);
				out[i] += in[j] * complex<Real>(cos(a), sin(a));
			}
		}
	}

	// Computes the discrete Fourier transform (DFT) of the given complex vector in place.
	// The vector can have any length, although powers of 2 are faster to compute.
	template <typename Real> void DFT(std::complex<Real>* signal, int length)
	{
		if (length == 0)
			return;

		if (IsPowerOfTwo(length))
			DFTRadix2(signal, length);
		else
			DFTBluestein(signal, length);
	}

	// Computes the inverse discrete Fourier transform (IDFT) of the given complex vector in place.
	// The vector can have any length although powers of 2 are faster to compute.
	// This transform does not perform scaling, so the inverse is not a true inverse.
	template <typename Real> void iDFT(std::complex<Real>* signal, int length)
	{
		// Swap real and imaginary components
		for (int i = 0; i != length; ++i)
		{
			auto re = signal[i].real();
			signal[i].real(signal[i].imag());
			signal[i].imag(re);
		}

		// "inverse" DFT
		DFT(signal, length);
	}

	// Computes the DFT of the given complex vector in place.
	// The vector length must be a power of 2. Uses the 'Cooley-Tukey' decimation-in-time radix-2 algorithm.
	template <typename Real> void DFTRadix2(std::complex<Real>* signal, int pow2_length)
	{
		int length = pow2_length;
		if (!IsPowerOfTwo(length))
			throw std::exception("Length is not a power of 2");

		// Compute levels = floor(log2(n))
		int levels = 0;
		for (auto i = length; i > 1; i >>= 1)
			++levels;

		// Pre-compute trig values
		std::vector<std::complex<Real>> coeff(length/2);
		for (int i = 0; i != length / 2; ++i)
		{
			auto a = maths::tau * i / length;
			coeff[i] = std::complex<Real>(Real(cos(a)), -Real(sin(a)));
		}

		// Bit-reversed addressing permutation
		for (auto i = 0; i != length; ++i)
		{
			auto j = int(ReverseBits32(i, levels));
			if (j > i)
				std::swap(signal[i], signal[j]);
		}

		// 'Cooley-Tukey' decimation-in-time radix-2 FFT
		for (auto size = 2; size <= length; size *= 2)
		{
			auto halfsize = size / 2;
			auto tablestep = length / size;
			for (auto i = 0; i < length; i += size)
			{
				for (auto j = i, k = 0; j < i + halfsize; j++, k += tablestep)
				{
					auto tmp = signal[j + halfsize] * coeff[k];
					signal[j + halfsize] = signal[j] - tmp;
					signal[j] += tmp;
				}
			}
			if (size == length)  // Prevent overflow in 'size *= 2'
				break;
		}
	}
	template <typename Real> std::vector<std::complex<Real>> DFTRadix2(Real const* signal, int length)
	{
		std::vector<std::complex<Real>> buf(length);
		for (int i = 0; i != length; ++i) buf[i] = std::complex<Real>(signal[i], 0);
		DFTRadix2(buf.data(), length);
		return std::move(buf);
	}

	// Computes the discrete Fourier transform (DFT) of the given complex vector in place.
	// The vector can have any length. This requires the convolution function, which in turn requires the radix-2 FFT function.
	// Uses 'Bluestein's chirp z-transform algorithm.
	template <typename Real> void DFTBluestein(std::complex<Real>* signal, int length)
	{
		// https://www.nayuki.io/res/free-small-fft-in-multiple-languages/fft.cpp
		using complex = std::complex<Real>;
		using BufferC = std::vector<complex>;

		// Find a power-of-2 convolution length m such that m >= length * 2 + 1
		size_t m;
		{
			size_t target;
			size_t const n_max = 0xFFFFFFFFUL;
			if (length > (n_max - 1) / 2)
				throw std::exception("Vector too large");

			target = length * 2 + 1;
			for (m = 1; m < target; m *= 2)
				if (n_max / 2 < m)
					throw std::exception("Vector too large");
		}

		// Pre-compute trig values
		std::vector<Real> cos_table(length);
		std::vector<Real> sin_table(length);
		for (size_t i = 0; i < length; i++)
		{
			// An accurate version of: auto temp = M_PI * i * i / length;
			auto temp = maths::tau_by_2 * size_t((uint64(i) * i % (uint64(length) * 2)) / length);
			cos_table[i] = Real(cos(temp));
			sin_table[i] = Real(sin(temp));
		}

		// Temporary vectors and preprocessing
		BufferC a(m);
		for (auto i = 0; i != length; ++i)
			a[i] = signal[i] * complex(cos_table[i], -sin_table[i]);
		
		BufferC b(m);
		b[0] = complex(cos_table[0], sin_table[0]);
		for (auto i = 1; i != length; ++i)
			b[i] = b[m-i] = complex(cos_table[i], sin_table[i]);

		// Convolution
		std::vector<Real> creal(m);
		std::vector<Real> cimag(m);
		Convolve(areal, aimag, breal, bimag, creal, cimag);

		// Post-processing
		for (size_t i = 0; i < length; i++)
		{
			real[i] =  creal[i] * cos_table[i] + cimag[i] * sin_table[i];
			imag[i] = -creal[i] * sin_table[i] + cimag[i] * cos_table[i];
		}
	}

	// Computes the circular convolution of the given real vectors. Each vector's length must be the same (length).
	template <typename Real> void Convolve(Real const* x, Real const* y, Real* out, int length)
	{
		// Create buffers of complex numbers and set the real parts to the given parameters
		std::vector<std::complex<Real>> X(length), Y(length), Out(length);
		for (int i = 0; i != length; ++i)
		{
			X.real(x[i]);
			Y.real(y[i]);
		}
		
		Convolve(X.data(), Y.data(), Out.data(), length);
	}

	// Computes the circular convolution of the given complex vectors. Each vector's length must be the same.
	template <typename Real> void Convolve(std::complex<Real> const* x, std::complex<Real> const* y, std::complex<Real>* out, int length)
	{

		size_t n = xreal.size();
		std::vector<Real> xr(xreal);
		std::vector<Real> xi(ximag);
		std::vector<Real> yr(yreal);
		std::vector<Real> yi(yimag);

		DiscreteFourierTransform(xr, xi);
		DiscreteFourierTransform(yr, yi);

		for (size_t i = 0; i < n; i++)
		{
			auto temp = xr[i] * yr[i] - xi[i] * yi[i];
			xi[i] = xi[i] * yr[i] + xr[i] * yi[i];
			xr[i] = temp;
		}

		InverseDiscreteFourierTransform(xr, xi);

		// Scaling (because this FFT implementation omits it)
		for (size_t i = 0; i < n; i++)
		{
			outreal[i] = xr[i] / n;
			outimag[i] = xi[i] / n;
		}
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/filesys/file.h"

namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_dft)
		{
			using BufferR = std::vector<double>;
			using BufferC = std::vector<std::complex<double>>;

			double const freq0 =   2.0; // hz
			double const freq1 =  10.0; // hz
			double const freq2 =  37.0; // hz
			double const freq3 =  60.0; // hz
			double const freq4 = 200.0; // hz
			double const SampFreq = 1000.0;

			// Create a sinusoidal signal
			BufferR signal(8192);
			for (int i = 0, iend = int(signal.size()); i != iend; ++i)
			{
				signal[i] =
					sin(maths::tau * freq0 * i / SampFreq) +
					sin(maths::tau * freq1 * i / SampFreq) +
					sin(maths::tau * freq2 * i / SampFreq) +
					sin(maths::tau * freq3 * i / SampFreq) +
					sin(maths::tau * freq4 * i / SampFreq);
			}

			// Naive DFT
			#if 0 // disabled, cause slow
			{
				BufferC naive(signal.size());
				DFTNaive(signal.data(), naive.data(), int(signal.size()));

				{// Output the frequency response
					std::string s_out;
					for (int i = 0, iend = int(naive.size() / 2); i != iend; ++i)
					{
						auto x = dft::FreqAt(double(i), SampFreq, naive.size());
						auto y = Length(naive[i]);
						s_out.append(pr::FmtS("%f, %f\n", x, y));
					}
					pr::BufferToFile(s_out, "P:\\dump\\frequencies0.csv");
				}
			}
			#endif
			{// Standard DFT
				auto freq = DFTRadix2(signal.data(), int(signal.size()));

				{// Output the frequency response
					std::string s_out;
					for (int i = 0, iend = int(freq.size()/2); i != iend; ++i)
					{
						auto x = dft::FreqAt(double(i), SampFreq, freq.size());
						auto y = Length(freq[i]);
						s_out.append(pr::FmtS("%f, %f\n", x, y));
					}
					pr::BufferToFile(s_out, "P:\\dump\\frequencies1.csv");
				}
			}
			{// Sliding Window DFT

				// Add the signal to the DFT
				int const window_size = 512;
				SlidingDFT<double> dft(window_size, SampFreq);
				for (int i = 0, iend = window_size; i != iend; ++i)
					dft.Add(signal[i]);

				{// Output the frequency response
					std::string s_out;
					auto freq_range = dft.FreqRange();
					for (double x = freq_range.m_beg; x < freq_range.m_end; x += 0.1)
						s_out.append(pr::FmtS("%f, %f\n", x, dft.Power(x)));
					pr::BufferToFile(s_out, "P:\\dump\\frequencies2.csv");
				}
			}
		}

	}
}
#endif


#if 0


// Computes the discrete Fourier transform (DFT) of the given complex vector, storing the result back into the vector.
// The vector can have any length. This requires the convolution function, which in turn requires the radix-2 FFT function.
// Uses 'Bluestein's chirp z-transform algorithm.
template <typename TCont, typename Real = TCont::value_type> void DiscreteFourierTransformBluestein(TCont& real, TCont& imag)
{
	if (real.size() != imag.size())
		throw std::exception("Mismatched lengths");

	// Find a power-of-2 convolution length m such that m >= n * 2 + 1
	size_t n = real.size();
	size_t m;
	{
		size_t target;
		size_t const n_max = 0xFFFFFFFFUL;
		if (n > (n_max - 1) / 2)
			throw std::exception("Vector too large");

		target = n * 2 + 1;
		for (m = 1; m < target; m *= 2)
			if (n_max / 2 < m)
				throw std::exception("Vector too large");
	}

	// Pre-compute trig values
	std::vector<Real> cos_table(n);
	std::vector<Real> sin_table(n);
	for (size_t i = 0; i < n; i++)
	{
		// An accurate version of: auto temp = M_PI * i * i / n;
		auto temp = maths::tau_by_2 * size_t(((unsigned long long)i * i % ((unsigned long long)n * 2)) / n);
		cos_table[i] = Real(cos(temp));
		sin_table[i] = Real(sin(temp));
	}

	// Temporary vectors and preprocessing
	std::vector<Real> areal(m);
	std::vector<Real> aimag(m);
	for (size_t i = 0; i < n; i++)
	{
		areal[i] =  real[i] * cos_table[i] + imag[i] * sin_table[i];
		aimag[i] = -real[i] * sin_table[i] + imag[i] * cos_table[i];
	}
	std::vector<Real> breal(m);
	std::vector<Real> bimag(m);
	breal[0] = cos_table[0];
	bimag[0] = sin_table[0];
	for (size_t i = 1; i < n; i++)
	{
		breal[i] = breal[m - i] = cos_table[i];
		bimag[i] = bimag[m - i] = sin_table[i];
	}

	// Convolution
	std::vector<Real> creal(m);
	std::vector<Real> cimag(m);
	Convolve(areal, aimag, breal, bimag, creal, cimag);

	// Post-processing
	for (size_t i = 0; i < n; i++)
	{
		real[i] =  creal[i] * cos_table[i] + cimag[i] * sin_table[i];
		imag[i] = -creal[i] * sin_table[i] + cimag[i] * cos_table[i];
	}
}


#endif



#if 0
struct Tests
{
	using Cont = std::vector<double>;
	std::default_random_engine m_rng;
	double m_max_log_error;

	Tests()
		:m_rng()
		,m_max_log_error(-maths::float_inf)
	{
		// Test powers of 2
		for (int i = 0; i != 11; ++i)
			TestDFT(1U << i);

		// Test small non-powers of 2
		for (int i = 0; i != 30; ++i)
			TestDFT(i);
	
		// Test power-of-2 size convolutions
		for (int i = 0; i != 11; ++i)
			TestConvolution(1U << i);
	
		// Test small non-powers of 2 convolutions
		for (int i = 0; i != 30; ++i)
			TestConvolution(i);
	
		//cout << endl;
		//cout << "Max log err = " << std::setprecision(3) << m_max_log_error << endl;
		//cout << "Test " << (m_max_log_error < -10 ? "passed" : "failed") << endl;
		PR_CHECK(m_max_log_error < -10, true);
	}

	// Test run a DFT
	void TestDFT(size_t n)
	{
		Cont inputreal(n); Fill(inputreal);
		Cont inputimag(n); Fill(inputimag);

		Cont refoutreal(n);
		Cont refoutimag(n);
		NaiveDFT(inputreal, inputimag, refoutreal, refoutimag, false);
	
		Cont actualoutreal(inputreal);
		Cont actualoutimag(inputimag);
		DiscreteFourierTransform(actualoutreal, actualoutimag);

		auto log_error = Log10RmsErr(refoutreal, refoutimag, actualoutreal, actualoutimag);
		cout
			<< "fftsize=" << std::setw(4) << std::setfill(' ') << n << "  "
			<< "logerr=" << std::setw(5) << std::setprecision(3) << std::setiosflags(std::ios::showpoint) << log_error << endl;
		//PR_CHECK(log_error < -10, true);
	}

	// Test run a Convolution
	void TestConvolution(size_t n)
	{
		Cont input0real(n); Fill(input0real);
		Cont input0imag(n); Fill(input0imag);
		Cont input1real(n); Fill(input1real);
		Cont input1imag(n); Fill(input1imag);

		Cont refoutreal(n);
		Cont refoutimag(n);
		NaiveConvolve(input0real, input0imag, input1real, input1imag, refoutreal, refoutimag);
	
		Cont actualoutreal(n);
		Cont actualoutimag(n);
		Convolve(input0real, input0imag, input1real, input1imag, actualoutreal, actualoutimag);

		auto log_error = Log10RmsErr(refoutreal, refoutimag, actualoutreal, actualoutimag);
		cout
			<< "convsize=" << std::setw(4) << std::setfill(' ') << n << "  "
			<< "logerr=" << std::setw(5) << std::setprecision(3) << std::setiosflags(std::ios::showpoint) << log_error << endl;
		//PR_CHECK(log_error < -10, true);
	}

	// Fill a buffer with real's
	void Fill(Cont& vec)
	{
		std::uniform_real_distribution<double> dist(-1.0, 1.0);
		for (auto& v : vec)
			v = dist(m_rng);
	}

	// Return the Log10 of the RMS error between two arrays of complex values
	double Log10RmsErr(Cont const& xreal, Cont const& ximag, Cont const& yreal, Cont const& yimag)
	{
		int n = xreal.size();
		double err = 0;
		for (int i = 0; i < n; i++)
			err += (xreal[i] - yreal[i]) * (xreal[i] - yreal[i]) + (ximag[i] - yimag[i]) * (ximag[i] - yimag[i]);
	
		err /= n > 0 ? n : 1;
		err = sqrt(err);  // Now this is a root mean square (RMS) error
		err = err > 0 ? log10(err) : -99.0;
		m_max_log_error = std::max(err, m_max_log_error);
		return err;
	}

	// Naive reference implementations
	void NaiveDFT(Cont const& inreal, Cont const& inimag, Cont& outreal, Cont& outimag, bool inverse)
	{
		int n = inreal.size();
		double coef = (inverse ? 1 : -1) * maths::tau;
		for (int k = 0; k < n; k++) {  // For each output element
			double sumreal = 0;
			double sumimag = 0;
			for (int t = 0; t < n; t++) {  // For each input element
				double angle = coef * ((long long)t * k % n) / n;
				sumreal += inreal[t]*cos(angle) - inimag[t]*sin(angle);
				sumimag += inreal[t]*sin(angle) + inimag[t]*cos(angle);
			}
			outreal[k] = sumreal;
			outimag[k] = sumimag;
		}
	}
	void NaiveConvolve(Cont const& xreal, Cont const& ximag, Cont const& yreal, Cont const& yimag, Cont& outreal, Cont& outimag) 
	{
		int n = xreal.size();
		for (int i = 0; i < n; i++) {
			double sumreal = 0;
			double sumimag = 0;
			for (int j = 0; j < n; j++) {
				int k = (i - j + n) % n;
				sumreal += xreal[k] * yreal[j] - ximag[k] * yimag[j];
				sumimag += xreal[k] * yimag[j] + ximag[k] * yreal[j];
			}
			outreal[i] = sumreal;
			outimag[i] = sumimag;
		}
	}
};
Tests test;
#endif

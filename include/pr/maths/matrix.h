//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/maths/forward.h"
#include "pr/maths/constants.h"
#include "pr/maths/maths_core.h"
#include "pr/maths/matrix4x4.h"

namespace pr
{
	// Dynamic NxM matrix
	template <typename Real>
	struct Matrix
	{
		using value_type = Real;
		enum { LocalBufCount = 16 };

	protected:

		// Notes:
		//  - Matrix has reference semantics because it is potentially a large object.
		//  - Data is stored as contiguous vectors (like m4x4 does, i.e. row major)
		//    Visually, the matrix is displayed with the vectors as columns.
		//    e.g.
		//     [{x}  {y}  {z}]
		//    is:                memory order:
		//     [x.x  y.x  z.x]    [0  4   8]
		//     [x.y  y.y  z.y]    [1  5   9]
		//     [x.z  y.z  z.z]    [2  6  10]
		//     [x.w  y.w  z.w]    [3  7  11]
		//  - 'vec_count' is the number of vectors in the matrix
		//  - 'cmp_count' is the number of components in each vector
		//  - The Row/Column description is confusing as hell because the matrix is displayed
		//    with the vectors as columns, even though the data are stored in rows. So I'm not
		//    using Row/Column notation. 'Vector/Component' notation is less ambiguous.
		//  - Accessors use 'vec' first then 'cmp' so that from left-to-right you select
		//    the vector first then the component.
		//  - The 'transposed' state should not be visible outside of the matrix. The matrix
		//    should look like any other matrix from an interface point-of-view.
		//  - 'transposed' is not a constructor parameter because almost anything is convertible
		//    to bool which causes the wrong constructor to be called. To create a matrix initialised
		//    with transposed data, use the constructor with all parameters

		Real  m_buf[LocalBufCount]; // Local buffer for small matrices
		Real* m_data;               // linear buffer of matrix elements
		int   m_vecs;               // number of vectors (Y dimension, aka rows in row major matrix)
		int   m_cmps;               // number of components per vector (X dimension, aka columns)
		bool  m_transposed;         // interpret the matrix as transposed (don't make this public, callers shouldn't know a matrix is transposed)

	public:

		Matrix()
			:m_data(&m_buf[0])
			,m_vecs(0)
			,m_cmps(0)
			,m_transposed(false)
		{
			resize(0,0);
		}
		Matrix(int vecs, int cmps)
			:Matrix()
		{
			resize(vecs, cmps, false);
		}
		Matrix(int vecs, int cmps, Real const* data)
			:Matrix(vecs, cmps, std::initializer_list<Real>(data, data + vecs * cmps))
		{
		}
		Matrix(int vecs, int cmps, std::initializer_list<Real> data)
			:Matrix(vecs, cmps, data, false)
		{
		}
		Matrix(int vecs, int cmps, std::initializer_list<Real> data, bool transposed)
			:Matrix(!transposed ? vecs : cmps, !transposed ? cmps : vecs)
		{
			assert("Data length mismatch" && int(data.size()) == vecs*cmps);
			memcpy(m_data, data.begin(), sizeof(Real) * size_t(vecs * cmps));
			m_transposed = transposed;
		}
		Matrix(Matrix&& rhs) noexcept
			:Matrix()
		{
			if (rhs.local())
			{
				resize(rhs.m_vecs, rhs.m_cmps);
				memcpy(m_data, rhs.m_data, sizeof(Real) * size());
				m_transposed = rhs.m_transposed;
			}
			else
			{
				m_vecs = rhs.m_vecs;
				m_cmps = rhs.m_cmps;
				m_data = rhs.m_data;
				m_transposed = rhs.m_transposed;

				rhs.m_data = &rhs.m_buf[0];
				rhs.m_vecs = 0;
				rhs.m_cmps = 0;
				rhs.m_transposed = false;
			}
		}
		Matrix(Matrix const& rhs)
			:Matrix()
		{
			resize(rhs.m_vecs, rhs.m_cmps);
			memcpy(m_data, rhs.m_data, sizeof(Real) * size());
			m_transposed = rhs.m_transposed;
		}
		explicit Matrix(v4_cref v)
			:Matrix(1, 4)
		{
			auto data = m_data;
			*data++ = Real(v.x);
			*data++ = Real(v.y);
			*data++ = Real(v.z);
			*data++ = Real(v.w);
		}
		explicit Matrix(m4_cref m)
			:Matrix(4, 4)
		{
			auto data = m_data;
			*data++ = Real(m.x.x); *data++ = Real(m.x.y); *data++ = Real(m.x.z); *data++ = Real(m.x.w);
			*data++ = Real(m.y.x); *data++ = Real(m.y.y); *data++ = Real(m.y.z); *data++ = Real(m.y.w);
			*data++ = Real(m.z.x); *data++ = Real(m.z.y); *data++ = Real(m.z.z); *data++ = Real(m.z.w);
			*data++ = Real(m.w.x); *data++ = Real(m.w.y); *data++ = Real(m.w.z); *data++ = Real(m.w.w);
		}
		~Matrix()
		{
			resize(0, 0);
		}

		// Assignment
		Matrix& operator = (Matrix&& rhs) noexcept
		{
			if (this == &rhs) return *this;
			if (rhs.local())
			{
				resize(rhs.m_vecs, rhs.m_cmps);
				memcpy(m_data, rhs.m_data, sizeof(Real) * size());
				m_transposed = rhs.m_transposed;
			}
			else
			{
				std::swap(m_vecs, rhs.m_vecs);
				std::swap(m_cmps, rhs.m_cmps);
				std::swap(m_data, rhs.m_data);
				std::swap(m_transposed, rhs.m_transposed);
			}
			return *this;
		}
		Matrix& operator = (Matrix const& rhs)
		{
			if (this == &rhs) return *this;
			resize(rhs.m_vecs, rhs.m_cmps);
			memcpy(m_data, rhs.m_data, sizeof(Real) * size());
			return *this;
		}

		// Access this matrix as a 2D array
		Real operator()(int vec, int cmp) const
		{
			if (m_transposed) std::swap(vec, cmp);
			assert(vec >= 0 && vec < m_vecs);
			assert(cmp >= 0 && cmp < m_cmps);
			return m_data[vec * m_cmps + cmp];
		}
		Real& operator()(int vec, int cmp)
		{
			if (m_transposed) std::swap(vec, cmp);
			assert(vec >= 0 && vec < m_vecs);
			assert(cmp >= 0 && cmp < m_cmps);
			return m_data[vec * m_cmps + cmp];
		}

		// The number of vectors in the matrix (i.e. Y dimension, aka row count in row-major matrix)
		int vecs() const
		{
			return !m_transposed ? m_vecs : m_cmps;
		}

		// The number of components per vector in the matrix (i.e. X dimension, aka column count in row-major matrix)
		int cmps() const
		{
			return !m_transposed ? m_cmps : m_vecs;
		}

		// The total number of elements in the matrix
		int size() const
		{
			return m_vecs * m_cmps;
		}

		// Access to the linear underlying matrix data
		Real const* data() const
		{
			return m_data;
		}
		Real* data()
		{
			return m_data;
		}

		// Access this matrix by vector
		struct VecProxy
		{
			Matrix* m_mat;
			int     m_idx;

			VecProxy(Matrix& m, int idx)
				:m_mat(&m)
				,m_idx(idx)
			{
				assert(idx >= 0 && idx < m_mat->vecs());
			}
			operator Matrix() const
			{
				Matrix v(1, m_mat->cmps());
				for (int i = 0, iend = m_mat->cmps(); i != iend; ++i)
					v(0, i) = (*m_mat)(m_idx, i);

				return v;
			}
			VecProxy& operator = (Matrix const& rhs)
			{
				assert("'rhs' must be a vector" && rhs.vecs() == 1 && rhs.cmps() == m_mat->cmps());

				for (int i = 0, iend = m_mat->cmps(); i != iend; ++i)
					(*m_mat)(m_idx, i) = rhs(0, i);

				return *this;
			}
			Real operator[](int i) const
			{
				return (*m_mat)(m_idx, i);
			}
			Real& operator[](int i)
			{
				return (*m_mat)(m_idx, i);
			}
		};
		VecProxy vec(int i) const
		{
			return VecProxy(const_cast<Matrix&>(*this), i);
		}

		// Access this matrix by components (transposed vector)
		struct CmpProxy
		{
			Matrix* m_mat;
			int     m_idx;

			CmpProxy(Matrix& m, int idx)
				:m_mat(&m)
				,m_idx(idx)
			{
				assert(idx >= 0 && idx < m_mat->cmps());
			}
			operator Matrix() const
			{
				Matrix v(m_mat->vecs(), 1);
				for (int i = 0, iend = m_mat->vecs(); i != iend; ++i)
					v(i, 0) = (*m_mat)(i, m_idx);

				return v;
			}
			CmpProxy& operator = (Matrix const& rhs)
			{
				assert("'rhs' must be a transposed vector" && rhs.cmps() == 1 && rhs.vecs() == m_mat->vecs());

				for (int i = 0, iend = m_mat->vecs(); i != iend; ++i)
					(*m_mat)(i, m_idx) = rhs(i, 0);

				return *this;
			}
			Real operator [](int i) const
			{
				return (*m_mat)(i, m_idx);
			}
			Real& operator [](int i)
			{
				return (*m_mat)(i, m_idx);
			}
		};
		CmpProxy cmp(int i) const
		{
			return CmpProxy(const_cast<Matrix&>(*this), i);
		}

		// True if the matrix is square
		bool IsSquare() const
		{
			return m_vecs == m_cmps;
		}

		// Set this matrix to all zeros
		Matrix& zero()
		{
			memset(m_data, 0, sizeof(Real) * size());
			return *this;
		}

		// Set this matrix to an identity matrix
		Matrix& identity()
		{
			zero();
			for (int i = 0, istep = cmps() + 1, iend = size(); i < iend; i += istep) m_data[i] = Real(1);
			return *this;
		}

		// Transpose this matrix
		Matrix& transpose()
		{
			m_transposed = !m_transposed;
			return *this;
		}

		// True if the data of this matrix is locally buffered
		bool local() const
		{
			return m_data == &m_buf[0];
		}

		// Change the dimensions of the matrix.
		void resize(int vecs, int cmps, bool preserve_data = true)
		{
			if (m_transposed) std::swap(vecs, cmps);

			// Check if a resize is needed
			auto new_count = size_t(vecs * cmps);
			auto old_count = size_t(m_vecs * m_cmps);
			auto min_count = std::min(new_count, old_count);
			auto min_vecs = std::min(vecs, m_vecs);
			auto min_cmps = std::min(cmps, m_cmps);
			
			// Reallocate buffer if necessary.
			auto data =
				new_count == old_count ? m_data :
				new_count > LocalBufCount ? new Real[new_count] :
				&m_buf[0];

			// Copy/Initialise data
			if (!preserve_data)
			{
				// Initialise to zeros
				memset(data, 0, sizeof(Real) * new_count);
			}
			else if (cmps == m_cmps)
			{
				// Matrix elements are stored as contiguous vectors so adding/removing vectors does not invalidate existing data.
				if (data != m_data) memcpy(data, m_data, sizeof(Real) * min_count);
				memset(data + min_count, 0, sizeof(Real) * (new_count - min_count));
			}
			else
			{
				// Adding components requires copying per vector.
				memset(data, 0, sizeof(Real) * new_count);
				for (int i = 0; i != min_vecs; ++i)
					memcpy(data + i*cmps, m_data + i*m_cmps, sizeof(Real) * min_cmps);
			}

			// Update the members
			m_vecs = vecs;
			m_cmps = cmps;
			std::swap(m_data, data);

			// Deallocate the old buffer
			if (data != m_data && data != &m_buf[0])
				delete[] data;
		}

		// Change the number of vectors in the matrix.
		void resize(int size, bool preserve_data = true)
		{
			resize(size, cmps(), preserve_data);
		}

		// Return an identity matrix of the given dimensions
		static Matrix Identity(int vecs, int cmps)
		{
			Matrix m(vecs, cmps);
			for (int i = 0, iend = std::min(vecs, cmps); i != iend; ++i)
				m(i, i) = 1;

			return m;
		}

		// Generates the random matrix
		template <typename Rng = std::default_random_engine> static Matrix Random(Rng& rng, int vecs, int cmps, Real min_value, Real max_value)
		{
			std::uniform_real_distribution<Real> dist(min_value, max_value);

			Matrix<Real> m(vecs, cmps);
			for (int i = 0, iend = m.size(); i != iend; ++i)
				m.data()[i] = dist(rng);

			return m;
		}

		#pragma region Operators

		// Unary operators
		friend Matrix<Real> operator + (Matrix<Real> const& m)
		{
			return m;
		}
		friend Matrix<Real> operator - (Matrix<Real> const& m)
		{
			// If 'm' is transposed, return a transposed matrix for consistency
			Matrix<Real> res(m.m_vecs, m.m_cmps);
			res.m_transposed = m.m_transposed;
			for (int i = 0, iend = res.size(); i != iend; ++i)
				res.m_data[i] = -m.m_data[i];
		
			return res;
		}

		// Addition/Subtraction
		friend Matrix<Real> operator + (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
		{
			assert(lhs.vecs() == rhs.vecs());
			assert(lhs.cmps() == rhs.cmps());

			// If one of the matrices is transposed, we need to add element-by-element
			// If both are transposed (or not), we can vectorise element adding
			// Return a matrix consist with the common transposed state
			if (lhs.m_transposed == rhs.m_transposed)
			{
				// If both are not transposed, we can vectorise element adding
				Matrix<Real> res(lhs.m_vecs, lhs.m_cmps);
				res.m_transposed = lhs.m_transposed;
				for (int i = 0, iend = res.size(); i != iend; ++i)
					res.m_data[i] = lhs.m_data[i] + rhs.m_data[i];
				return res;
			}
			else
			{
				// If one of the matrices is transposed, we need to add element-by-element
				Matrix<Real> res(lhs.vecs(), lhs.cmps());
				for (int r = 0; r != res.vecs(); ++r)
					for (int c = 0; c != res.cmps(); ++c)
						res(r, c) = lhs(r, c) + rhs(r, c);
				return res;
			}
		}
		friend Matrix<Real> operator - (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
		{
			assert(lhs.vecs() == rhs.vecs());
			assert(lhs.cmps() == rhs.cmps());

			// If one of the matrices is transposed, we need to subtract element-by-element
			// If both are transposed (or not), we can vectorise element substracting
			// Return a matrix consist with the common transposed state
			if (lhs.m_transposed != rhs.m_transposed)
			{
				// If both are not transposed, we can vectorise element subtracting
				Matrix<Real> res(lhs.m_vecs, lhs.m_cmps);
				res.m_transposed = lhs.m_transposed;
				for (int i = 0, iend = res.size(); i != iend; ++i)
					res.m_data[i] = lhs.m_data[i] - rhs.m_data[i];
				return res;
			}
			else
			{
				// If one of the matrices is transposed, we need to subtract element-by-element
				Matrix<Real> res(lhs.vecs(), lhs.cmps());
				for (int r = 0; r != res.vecs(); ++r)
					for (int c = 0; c != res.cmps(); ++c)
						res(r, c) = lhs(r, c) - rhs(r, c);
				return res;
			}
		}

		// Multiplication
		friend Matrix<Real> operator * (Real s, Matrix<Real> const& mat)
		{
			// Preserve the transposed state in the returned matrix
			Matrix<Real> res(mat.m_vecs, mat.m_cmps);
			res.m_transposed = mat.m_transposed;
			for (int i = 0, iend = res.size(); i != iend; ++i)
				res.m_data[i] = mat.m_data[i] * s;

			return res;
		}
		friend Matrix<Real> operator * (Matrix<Real> const& mat, Real s)
		{
			return s * mat;
		}
		friend Matrix<Real> operator * (Matrix<Real> const& b2c, Matrix<Real> const& a2b)
		{
			// Note:
			//  - The multplication order is the same as for m4x4.
			//    The reason for this order is because matrices are applied from right to left
			//    e.g.
			//       auto Va =             V = vector in space 'a'
			//       auto Vb =       a2b * V = vector in space 'b'
			//       auto Vc = b2c * a2b * V = vector in space 'c'
			//  - The shape of the result is:
			//       [   ]       [       ]       [   ]
			//       [a2c]       [  b2c  ]       [a2b]
			//       [1x3]   =   [  2x3  ]   *   [1x2]
			//       [   ]       [       ]       [   ]
			assert("Wrong matrix dimensions" && a2b.cmps() == b2c.vecs());

			// Result
			Matrix res(a2b.vecs(), b2c.cmps());

			auto msize = std::max(std::max(a2b.vecs(), a2b.cmps()), std::max(b2c.vecs(), b2c.cmps()));
			if (msize < 32)
			{
				// Small matrix multiply
				for (int r = 0; r != res.vecs(); ++r)
					for (int c = 0; c != res.cmps(); ++c)
						for (int k = 0; k != a2b.cmps(); ++k)
							res(r, c) += a2b(r, k) * b2c(k, c);
				
				return res;
			}

			// 'Strassen Multiply'
			int n = 0;
			int size = 1;
			while (msize > size) { size *= 2; n++; };
			int h = size / 2;

			// Temporary smaller square matrices
			const int M = 9;
			std::vector<Matrix> buf(n * M);
			
			//  8x8, 8x8, 8x8, ...
			//  4x4, 4x4, 4x4, ...
			//  2x2, 2x2, 2x2, ...
			//  . . .
			auto field = &buf[0];
			for (int i = 0; i < n - 4; i++)
			{
				auto z = (int)pow(2, n - i - 1);
				for (int j = 0; j < M; j++)
					field[i*M+j] = Matrix(z, z);
			}

			#pragma region Sub Functions
			struct L
			{
				static void SafeAplusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
					{
						for (int c = 0; c < sz; ++c) // cols
						{
							C(r, c) = 0;
							if (xa + c < A.cmps() && ya + r < A.vecs()) C(r, c) += A(ya + r, xa + c);
							if (xb + c < B.cmps() && yb + r < B.vecs()) C(r, c) += B(yb + r, xb + c);
						}
					}
				}
				static void SafeAminusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
					{
						for (int c = 0; c < sz; ++c) // cols
						{
							C(r, c) = 0;
							if (xa + c < A.cmps() && ya + r < A.vecs()) C(r, c) += A(ya + r, xa + c);
							if (xb + c < B.cmps() && yb + r < B.vecs()) C(r, c) -= B(yb + r, xb + c);
						}
					}
				}
				static void SafeACopytoC(Matrix const& A, int xa, int ya, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
					{
						for (int c = 0; c < sz; ++c) // cols
						{
							C(r, c) = 0;
							if (xa + c < A.cmps() && ya + r < A.vecs()) C(r, c) += A(ya + r, xa + c);
						}
					}
				}
				static void AplusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(r, c) = A(ya + r, xa + c) + B(yb + r, xb + c);
				}
				static void AminusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(r, c) = A(ya + r, xa + c) - B(yb + r, xb + c);
				}
				static void ACopytoC(Matrix const& A, int xa, int ya, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(r, c) = A(ya + r, xa + c);
				}
				static void StrassenMultiplyRun(Matrix const& A, Matrix const& B, Matrix& C, int l, Matrix* f)
				{
					// A * B into C, level of recursion, matrix field
					// function for square matrix 2^N x 2^N
					auto sz = A.vecs();
					if (sz < 32)
					{
						for (int r = 0; r < C.vecs(); ++r)
						{
							for (int c = 0; c < C.cmps(); ++c)
							{
								C(r, c) = 0;
								for (int k = 0; k < A.cmps(); ++k)
									C(r, c) += A(r, k) * B(k, c);
							}
						}
					}
					else
					{
						auto hh = sz / 2;
						AplusBintoC(A, 0, 0, A, hh, hh, f[l*M + 0], hh);
						AplusBintoC(B, 0, 0, B, hh, hh, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 1], l + 1, f); // (A11 + A22) * (B11 + B22);

						AplusBintoC(A, 0, hh, A, hh, hh, f[l*M + 0], hh);
						ACopytoC(B, 0, 0, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 2], l + 1, f); // (A21 + A22) * B11;

						ACopytoC(A, 0, 0, f[l*M + 0], hh);
						AminusBintoC(B, hh, 0, B, hh, hh, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 3], l + 1, f); //A11 * (B12 - B22);

						ACopytoC(A, hh, hh, f[l*M + 0], hh);
						AminusBintoC(B, 0, hh, B, 0, 0, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 4], l + 1, f); //A22 * (B21 - B11);

						AplusBintoC(A, 0, 0, A, hh, 0, f[l*M + 0], hh);
						ACopytoC(B, hh, hh, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 5], l + 1, f); //(A11 + A12) * B22;

						AminusBintoC(A, 0, hh, A, 0, 0, f[l*M + 0], hh);
						AplusBintoC(B, 0, 0, B, hh, 0, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 6], l + 1, f); //(A21 - A11) * (B11 + B12);

						AminusBintoC(A, hh, 0, A, hh, hh, f[l*M + 0], hh);
						AplusBintoC(B, 0, hh, B, hh, hh, f[l*M + 1], hh);
						StrassenMultiplyRun(f[l*M + 0], f[l*M + 1], f[l*M + 1 + 7], l + 1, f); // (A12 - A22) * (B21 + B22);

						// C11
						for (int r = 0; r < hh; r++) // rows
							for (int c = 0; c < hh; c++) // cols
								C(r, c) = f[l*M + 1 + 1](r, c) + f[l*M + 1 + 4](r, c) - f[l*M + 1 + 5](r, c) + f[l*M + 1 + 7](r, c);

						// C12
						for (int r = 0; r < hh; r++) // rows
							for (int c = hh; c < sz; c++) // cols
								C(r, c) = f[l*M + 1 + 3](r, c - hh) + f[l*M + 1 + 5](r, c - hh);

						// C21
						for (int r = hh; r < sz; r++) // rows
							for (int c = 0; c < hh; c++) // cols
								C(r, c) = f[l*M + 1 + 2](r - hh, c) + f[l*M + 1 + 4](r - hh, c);

						// C22
						for (int r = hh; r < sz; r++) // rows
							for (int c = hh; c < sz; c++) // cols
								C(r, c) = f[l*M + 1 + 1](r - hh, c - hh) - f[l*M + 1 + 2](r - hh, c - hh) + f[l*M + 1 + 3](r - hh, c - hh) + f[l*M + 1 + 6](r - hh, c - hh);
					}
				}
			};
			#pragma endregion

			#pragma region Strassen Multiply

			L::SafeAplusBintoC(a2b, 0, 0, a2b, h, h, field[0*M + 0], h);
			L::SafeAplusBintoC(b2c, 0, 0, b2c, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 1], 1, field); // (A11 + A22) * (B11 + B22);

			L::SafeAplusBintoC(a2b, 0, h, a2b, h, h, field[0*M + 0], h);
			L::SafeACopytoC(b2c, 0, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 2], 1, field); // (A21 + A22) * B11;

			L::SafeACopytoC(a2b, 0, 0, field[0*M + 0], h);
			L::SafeAminusBintoC(b2c, h, 0, b2c, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 3], 1, field); //A11 * (B12 - B22);

			L::SafeACopytoC(a2b, h, h, field[0*M + 0], h);
			L::SafeAminusBintoC(b2c, 0, h, b2c, 0, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 4], 1, field); //A22 * (B21 - B11);

			L::SafeAplusBintoC(a2b, 0, 0, a2b, h, 0, field[0*M + 0], h);
			L::SafeACopytoC(b2c, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 5], 1, field); //(A11 + A12) * B22;

			L::SafeAminusBintoC(a2b, 0, h, a2b, 0, 0, field[0*M + 0], h);
			L::SafeAplusBintoC(b2c, 0, 0, b2c, h, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 6], 1, field); //(A21 - A11) * (B11 + B12);

			L::SafeAminusBintoC(a2b, h, 0, a2b, h, h, field[0*M + 0], h);
			L::SafeAplusBintoC(b2c, 0, h, b2c, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 7], 1, field); // (A12 - A22) * (B21 + B22);

			// C11
			for (int r = 0; r < std::min(h, res.vecs()); r++) // rows
				for (int c = 0; c < std::min(h, res.cmps()); c++) // cols
					res(r, c) = field[0*M + 1 + 1](r, c) + field[0*M + 1 + 4](r, c) - field[0*M + 1 + 5](r, c) + field[0*M + 1 + 7](r, c);

			// C12
			for (int r = 0; r < std::min(h, res.vecs()); r++) // rows
				for (int c = h; c < std::min(2 * h, res.cmps()); c++) // cols
					res(r, c) = field[0*M + 1 + 3](r, c - h) + field[0*M + 1 + 5](r, c - h);

			// C21
			for (int r = h; r < std::min(2 * h, res.vecs()); r++) // rows
				for (int c = 0; c < std::min(h, res.cmps()); c++) // cols
					res(r, c) = field[0*M + 1 + 2](r - h, c) + field[0*M + 1 + 4](r - h, c);

			// C22
			for (int r = h; r < std::min(2 * h, res.vecs()); r++) // rows
				for (int c = h; c < std::min(2 * h, res.cmps()); c++) // cols
					res(r, c) = field[0*M + 1 + 1](r - h, c - h) - field[0*M + 1 + 2](r - h, c - h) + field[0*M + 1 + 3](r - h, c - h) + field[0*M + 1 + 6](r - h, c - h);

			#pragma endregion 

			return res;
		}
		
		// Equals
		friend bool operator == (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
		{
			if (lhs.vecs() != rhs.vecs()) return false;
			if (lhs.cmps() != rhs.cmps()) return false;
			if (lhs.m_transposed == rhs.m_transposed)
				return memcmp(lhs.data(), rhs.data(), sizeof(Real) * lhs.size()) == 0;

			// Fall back to element-by-element comparisons
			for (int r = 0, rend = lhs.vecs(); r != rend; ++r)
				for (int c = 0, cend = lhs.cmps(); c != cend; ++c)
					if (lhs(r, c) != rhs(r, c))
						return false;

			return true;
		}
		friend bool operator != (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
		{
			return !(lhs == rhs);
		}

		// Value equality
		friend bool FEqlRelative(Matrix<Real> const& lhs, Matrix<Real> const& rhs, Real tol)
		{
			if (lhs.vecs() != rhs.vecs()) return false;
			if (lhs.cmps() != rhs.cmps()) return false;
			if (lhs.m_transposed == rhs.m_transposed)
			{
				// Vectorise compares
				for (int i = 0, iend = lhs.size(); i != iend; ++i)
					if (!FEqlRelative(lhs.m_data[i], rhs.m_data[i], tol))
						return false;
			}
			else
			{
				// Element-by-element compares
				for (int r = 0, rend = lhs.vecs(); r != rend; ++r)
					for (int c = 0, cend = lhs.cmps(); c != cend; ++c)
						if (!FEqlRelative(lhs(r, c), rhs(r, c), tol))
							return false;
			}

			return true;
		}
		friend bool FEql(Matrix<Real> const& lhs, Matrix<Real> const& rhs)
		{
			return FEqlRelative(lhs, rhs, maths::tiny<Real>);
		}
		friend bool FEqlRelative(Matrix<Real> const& lhs, m4_cref rhs, float tol)
		{
			if (lhs.vecs() != 4) return false;
			if (lhs.cmps() != 4) return false;
			return
				FEqlRelative(float(lhs(0, 0)), rhs.x.x, tol) && FEqlRelative(float(lhs(0, 1)), rhs.x.y, tol) && FEqlRelative(float(lhs(0, 2)), rhs.x.z, tol) && FEqlRelative(float(lhs(0, 3)), rhs.x.w, tol) &&
				FEqlRelative(float(lhs(1, 0)), rhs.y.x, tol) && FEqlRelative(float(lhs(1, 1)), rhs.y.y, tol) && FEqlRelative(float(lhs(1, 2)), rhs.y.z, tol) && FEqlRelative(float(lhs(1, 3)), rhs.y.w, tol) &&
				FEqlRelative(float(lhs(2, 0)), rhs.z.x, tol) && FEqlRelative(float(lhs(2, 1)), rhs.z.y, tol) && FEqlRelative(float(lhs(2, 2)), rhs.z.z, tol) && FEqlRelative(float(lhs(2, 3)), rhs.z.w, tol) &&
				FEqlRelative(float(lhs(3, 0)), rhs.w.x, tol) && FEqlRelative(float(lhs(3, 1)), rhs.w.y, tol) && FEqlRelative(float(lhs(3, 2)), rhs.w.z, tol) && FEqlRelative(float(lhs(3, 3)), rhs.w.w, tol);
		}
		friend bool FEql(Matrix<Real> const& lhs, m4_cref rhs)
		{
			return FEqlRelative(lhs, rhs, maths::tinyf);
		}
		friend bool FEqlRelative(Matrix<Real> const& lhs, v4_cref rhs, float tol)
		{
			if (lhs.vecs() != 1 && lhs.cmps() != 1) return false;
			if (lhs.size() != 4) return false;
			return
				FEqlRelative(float(lhs.m_data[0]), rhs.x, tol) &&
				FEqlRelative(float(lhs.m_data[1]), rhs.y, tol) &&
				FEqlRelative(float(lhs.m_data[2]), rhs.z, tol) &&
				FEqlRelative(float(lhs.m_data[3]), rhs.w, tol);
		}
		friend bool FEql(Matrix<Real> const& lhs, v4_cref rhs)
		{
			return FEqlRelative(lhs, rhs, maths::tinyf);
		}

		#pragma endregion
	};

	// The LU decomposition of a square matrix
	template <typename Real>
	struct MatrixLU
	{
		struct LProxy
		{
			Matrix<Real> const* m_mat;
			LProxy(Matrix<Real> const& m)
				:m_mat(&m)
			{}
			Real operator ()(int vec, int cmp) const
			{
				assert(vec >= 0 && vec < m_mat->vecs());
				assert(cmp >= 0 && cmp < m_mat->cmps());
				return cmp > vec ? (*m_mat)(vec, cmp) : cmp == vec ? 1 : 0;
			}
		};
		struct UProxy
		{
			Matrix<Real> const* m_mat;
			UProxy(Matrix<Real> const& m)
				:m_mat(&m)
			{}
			Real operator ()(int vec, int cmp) const
			{
				assert(vec >= 0 && vec < m_mat->vecs());
				assert(cmp >= 0 && cmp < m_mat->cmps());
				return cmp <= vec ? (*m_mat)(vec, cmp) : 0;
			}
		};
	
		// The L and U matrices are stored in one matrix.
		Matrix<Real> lu;

		// Access to the lower diagonal matrix
		LProxy L;

		// Access to the upper diagonal matrix
		UProxy U;

		// The permutation row indices (length == rows())
		std::unique_ptr<int[]> pi;

		// The determinant of the permutation matrix
		Real DetOfP;

		MatrixLU(int vecs, int cmps, std::initializer_list<Real> data, bool transposed = false)
			:MatrixLU(Matrix<Real>(vecs, cmps, data, transposed))
		{}
		MatrixLU(Matrix<Real> const& m)
			:lu(m)
			,L(lu)
			,U(lu)
			,pi(new int[m.vecs()])
			,DetOfP(1.0f)
		{
			auto const N = m.IsSquare() ? m.vecs() : throw std::exception("LU decomposition is only possible on square matrices");

			// We will store both the L and U matrices in 'base' since we know
			// L has the form: [1 0] and U has the form: [U U]
			//                 [L 1]                     [0 U]
			auto  LL = Matrix<Real>::Identity(m.vecs(), m.cmps());
			auto& UU = lu;

			// Initialise the permutation vector
			for (int i = 0; i != N; ++i)
				pi[i] = i;

			// Decompose 'm' into 'LL' and 'UU'
			for (int v = 0; v != N; ++v)
			{
				// Pivoting is used to avoid instability when the pivot is ~0
				// It will probably always be enabled, but this documents it.
				const bool UsePivot = true;
				if constexpr (UsePivot)
				{
					// Find the largest component in the vector 'v' to use as the pivot
					auto p = 0;
					Real max = 0;
					for (int i = v; i != N; ++i)
					{
						auto val = abs(UU(v, i));
						if (val <= max) continue;
						max = val;
						p = i;
					}
					if (max == 0)
						throw std::runtime_error("The matrix is singular");

					// Switch the components of all vectors
					if (p != v)
					{
						std::swap(pi[v], pi[p]);
						DetOfP = -DetOfP;
				
						// Switch the components in LL and UU
						for (int i = 0; i != v; ++i)
							std::swap(LL(i, v), LL(i, p));
						for (int i = 0; i != N; ++i)
							std::swap(UU(i, v), UU(i, p));
					}
				}

				// Gaussian eliminate the remaining components of vector 'v'
				for (int c = v + 1; c != N; ++c)
				{
					LL(v, c) = UU(v, c) / UU(v, v);
					for (int i = v; i != N; ++i)
						UU(i, c) -= LL(v, c) * UU(i, v);
				}
			}

			// Combine 'LL' and 'UU' into 'LU' (note UU *is* lu)
			for (int v = 0; v != N; ++v)
				for (int c = v+1; c != N; ++c)
					lu(v, c) = LL(v, c);
		}

		// The matrix dimension (square)
		int dim() const
		{
			return lu.vecs();
		}

		// Access to the linear underlying matrix data
		Real const* data() const
		{
			return lu.data();
		}
		
		// Access this matrix as a 2D array
		Real operator()(bool change, int vec, int cmp) const
		{
			return lu(vec, cmp);
		}
	};

	// Return the transpose of matrix 'm'
	template <typename Real> inline Matrix<Real> Transpose(Matrix<Real> const& m)
	{
		return Matrix<Real>(m).transpose();
	}

	// Return the determinant of a matrix
	template <typename Real> inline Real Determinant(MatrixLU<Real> const& m)
	{
		auto det = m.DetOfP;
		for (int i = 0; i != m.dim(); ++i)
			det *= m.U(i, i);

		return det;
	}
	template <typename Real> inline Real Determinant(Matrix<Real> const& m)
	{
		return Determinant(MatrixLU<Real>(m));
	}

	// Return the dot product of two vectors
	template <typename Real> inline Real Dot(Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		assert("Dot product is between column vectors" && lhs.vecs() == 1 && rhs.vecs() == 1);
		assert("Dot product must be between vectors of the same length" && lhs.cmps() == rhs.cmps());

		Real dp = 0;
		for (int i = 0, iend = lhs.cmps(); i != iend; ++i)
			dp += lhs(0, i) * rhs(0, i);
		return dp;
	}

	// True if 'm' has an inverse
	template <typename Real> inline bool IsInvertible(MatrixLU<Real> const& m)
	{
		return Determinant(m) != 0;
	}
	template <typename Real> inline bool IsInvertible(Matrix<Real> const& m)
	{
		return IsInvertible(MatrixLU<Real>(m));
	}

	// Solves for 'x' in 'Ax = v'
	template <typename Real> inline Matrix<Real> Solve(MatrixLU<Real> const& A, Matrix<Real> const& v)
	{
		// e.g. [4x4][1x4] = [1x4]
		assert("Solution vector 'v' has the wrong dimensions" && A.dim() == v.cmps() && v.vecs() == 1);

		// Switch items in 'v' due to permutation matrix
		Matrix<Real> a(1, A.dim());
		for (int i = 0; i != A.dim(); ++i)
			a(0, i) = v(0, A.pi[i]);

		// Solve for x in 'L.x = b' assuming 'L' is a lower triangular matrix
		Matrix<Real> b(1, A.dim());
		for (int i = 0; i != A.dim(); ++i)
		{
			b(0, i) = a(0, i);
			for (int j = 0; j != i; ++j)
				b(0, i) -= A.L(j, i) * b(0, j);
		}

		// Solve for x in 'Ux = b' assuming 'U' is an upper triangular matrix
		Matrix<Real> c(b);
		for (int i = A.dim(); i-- != 0;)
		{
			b(0, i) = c(0, i);
			for (int j = A.dim() - 1; j > i; --j)
				b(0, i) -= A.U(j, i) * b(0, j);

			b(0, i) = b(0, i) / A.U(i, i);
		}

		return b;
	}

	// Solves for 'x' in 'Ax = v'
	template <typename Real> inline Matrix<Real> Solve(Matrix<Real> const& A, Matrix<Real> const& v)
	{
		if (!A.IsSquare()) throw std::exception("The matrix is not square");
		return Solve(MatrixLU<Real>(A), v);
	}

	// Return the inverse of matrix 'm'
	template <typename Real> inline Matrix<Real> Invert(MatrixLU<Real> const& lu)
	{
		assert("Matrix has no inverse" && IsInvertible(lu));

		// Inverse of an NxM matrix is a MxN matrix (even though this only works for square matrices)
		Matrix<Real> inv(lu.dim(), lu.dim());
		Matrix<Real> elem(1, lu.dim());
		for (int i = 0; i != lu.dim(); ++i)
		{
			elem(0, i) = 1;
			inv.vec(i) = Solve(lu, elem);
			elem(0, i) = 0;
		}
		return inv;
	}
	template <typename Real> inline Matrix<Real> Invert(Matrix<Real> const& m)
	{
		return Invert(MatrixLU<Real>(m));
	}

	// Matrix to the power 'pow'
	template <typename Real> inline Matrix<Real> Power(Matrix<Real> const& m, int pow)
	{
		if (pow == +1) return m;
		if (pow ==  0) return Matrix<Real>::Identity(m.vecs(), m.cmps());
		if (pow == -1) return Invert(m);

		auto x = pow < 0 ? Invert(m) : m;
		pow = pow < 0 ? -pow : pow;

		auto ret = Matrix<Real>::Identity(m.vecs(), m.cmps());
		while (pow != 0)
		{
			if ((pow & 1) == 1) ret *= x;
			x *= x;
			pow >>= 1;
		}
		return ret;
	}

	#if 0
	/// <summary>Parse a matrix from a string</summary>
	public static Matrix Parse(string s)
	{
		// Normalise the matrix string
		{
			// Remove any multiple spaces
			while (s.IndexOf("  ") != -1)
				s = s.Replace("  ", " ");

			// Remove any spaces before or after newlines
			s = s.Replace(" \r\n", "\r\n");
			s = s.Replace("\r\n ", "\r\n");

			// If the data ends in a newline, remove the trailing newline.
			// Make it easier by first replacing \r\n's with |'s then
			// restore the |'s with \r\n's
			s = s.Replace("\r\n", "|");
			while (s.LastIndexOf("|") == (s.Length - 1))
				s = s.Substring(0, s.Length - 1);

			s = s.Replace("|", "\r\n");
			s = s.Trim();
		}

		var rows = Regex.Split(s, "\r\n");
		var nums = rows[0].Split(' ');
		var m = Matrix<double>(rows.Length, nums.Length);
		for (int i = 0; i < rows.Length; i++)
		{
			nums = rows[i].Split(' ');
			for (int j = 0; j < nums.Length; j++)
				m[i,j] = double.Parse(nums[j]);
		}
		return m;
	}


	/// <summary>ToString</summary>
	public override string ToString()
	{
		var s = new StringBuilder();
		for (int i = 0; i < rows; i++)
		{
			for (int j = 0; j < cols; j++)
				s.Append("{0,5:0.00} ".Fmt(this[i,j]));

			s.AppendLine();
		}
		return s.ToString();
	}

	/// <summary>Permutation matrix "P" due to permutation vector "pi"</summary>
	private Matrix PermutationMatrix
	{
		get
		{
			var m = new Matrix(rows, cols);
			for (int i = 0; i < rows; i++)
				m[cache.pi[i], i] = 1;

			return m;
		}
	}
	#endif
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTest(MatrixTests)
	{
		using namespace pr;
		std::default_random_engine rng(1);

		{// LU decomposition
			auto m = MatrixLU<double>(4, 4, 
			{
				  1.0, +2.0,  3.0, +1.0,
				  4.0, -5.0,  6.0, +5.0,
				  7.0, +8.0,  9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			});
			auto res = Matrix<double>(4, 4,
			{
				3.0, 0.66666666666667, 0.33333333333333, 0.33333333333333,
				6.0, -9.0, -0.33333333333333, -0.22222222222222,
				9.0, 2.0, -11.333333333333, -0.3921568627451,
				12.0, 3.0, -3.0, -14.509803921569,
			});
			PR_CHECK(FEql(m.lu, res), true);
		}
		{// Invert
			auto m = Matrix<double>(4, 4, { 1, 2, 3, 1, 4, -5, 6, 5, 7, 8, 9, -9, -10, 11, 12, 0 });
			auto inv = Invert(m);
			auto INV = Matrix<double>(4, 4,
			{
				+0.258783783783783810, -0.018918918918918920, +0.018243243243243241, -0.068918918918918923,
				+0.414864864864864790, -0.124324324324324320, -0.022972972972972971, -0.024324324324324322,
				-0.164639639639639650, +0.098198198198198194, +0.036261261261261266, +0.048198198198198199,
				+0.405405405405405430, -0.027027027027027029, -0.081081081081081086, -0.027027027027027025,
			});
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Invert
			auto M = m4x4(
				v4(1.0f, +2.0f, 3.0f, +1.0f),
				v4(4.0f, -5.0f, 6.0f, +5.0f),
				v4(7.0f, +8.0f, 9.0f, -9.0f),
				v4(-10.0f, 11.0f, 12.0f, +0.0f)
			);
			auto INV = Invert(M);
			auto m = Matrix<double>(4, 4,
			{
				1.0, +2.0, 3.0, +1.0,
				4.0, -5.0, 6.0, +5.0,
				7.0, +8.0, 9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			});
			auto inv = Invert(m);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Invert transposed
			auto M = Transpose4x4(m4x4(
				v4(1.0f, +2.0f, 3.0f, +1.0f),
				v4(4.0f, -5.0f, 6.0f, +5.0f),
				v4(7.0f, +8.0f, 9.0f, -9.0f),
				v4(-10.0f, 11.0f, 12.0f, +0.0f)
			));
			auto INV = Invert(M);
			auto m = Matrix<double>(4, 4,
			{
				1.0, +2.0, 3.0, +1.0,
				4.0, -5.0, 6.0, +5.0,
				7.0, +8.0, 9.0, -9.0,
				-10.0, 11.0, 12.0, +0.0,
			}, true);
			auto inv = Invert(m);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(inv, INV), true);
		}
		{// Compare with m4x4
			auto M = m4x4::Random(rng, v4::Origin());
			Matrix<float> m(M);

			PR_CHECK(FEql(m, M), true);
			PR_CHECK(FEql(m(0, 3), M.x.w), true);
			PR_CHECK(FEql(m(3, 0), M.w.x), true);
			PR_CHECK(FEql(m(2, 2), M.z.z), true);

			PR_CHECK(IsInvertible(m) == IsInvertible(M), true);

			auto m1 = Invert(m);
			auto M1 = Invert(M);
			PR_CHECK(FEql(m1, M1), true);

			auto m2 = Transpose(m);
			auto M2 = Transpose4x4(M);
			PR_CHECK(FEql(m2, M2), true);
		}
		{// Multiply
			double data0[] = {1, 2, 3, 4, 0.1, 0.2, 0.3, 0.4, -4, -3, -2, -1};
			double data1[] = {1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4};
			double rdata[] = {30, 30, 30, 30, 30, 3, 3, 3, 3, 3, -20, -20, -20, -20, -20};
			auto a2b = Matrix<double>(3, 4, data0);
			auto b2c = Matrix<double>(4, 5, data1);
			auto A2C = Matrix<double>(3, 5, rdata);
			auto a2c = b2c * a2b;
			PR_CHECK(FEql(a2c, A2C), true);
		}
		{// Multiply
			std::uniform_real_distribution<float> dist(-5.0f, +5.0f);
			
			auto V0 = v4::Random(rng, -5, +5);
			auto M0 = m4x4::Random(rng, -5, +5);
			auto M1 = m4x4::Random(rng, -5, +5);

			auto v0 = Matrix<float>(V0);
			auto m0 = Matrix<float>(M0);
			auto m1 = Matrix<float>(M1);

			PR_CHECK(FEql(v0, V0), true);
			PR_CHECK(FEql(m0, M0), true);
			PR_CHECK(FEql(m1, M1), true);

			auto V2 = M0 * V0;
			auto v2 = m0 * v0;
			PR_CHECK(FEql(v2, V2), true);

			auto M2 = M0 * M1;
			auto m2 = m0 * m1;
			PR_CHECK(FEql(m2, M2), true);
		}
		{// Multiply round trip
			std::uniform_real_distribution<float> dist(-5.0f, +5.0f);
			const int SZ = 100;
			Matrix<float> m(SZ, SZ);
			for (int k = 0; k != 10; ++k)
			{
				for (int r = 0; r != m.vecs(); ++r)
					for (int c = 0; c != m.cmps(); ++c)
						m(r, c) = dist(rng);

				if (IsInvertible(m))
				{
					auto m_inv = Invert(m);

					auto i0 = Matrix<float>::Identity(SZ, SZ);
					auto i1 = m * m_inv;
					auto i2 = m_inv * m;

					PR_CHECK(FEqlRelative(i0, i1, 0.0001f), true);
					PR_CHECK(FEqlRelative(i0, i2, 0.0001f), true);

					break;
				}
			}
		}
		{// Transpose
			const int vecs = 4, cmps = 3;
			auto m = Matrix<double>::Random(rng, vecs, cmps, -5.0, 5.0);
			auto t = Transpose(m);

			PR_CHECK(m.vecs(), vecs);
			PR_CHECK(m.cmps(), cmps);
			PR_CHECK(t.vecs(), cmps);
			PR_CHECK(t.cmps(), vecs);

			for (int r = 0; r != vecs; ++r)
				for (int c = 0; c != cmps; ++c)
					PR_CHECK(m(r, c) == t(c, r), true);
		}
		{// Resizing
			auto M = Matrix<double>::Random(rng, 4, 3, -5.0, 5.0);
			auto m = M;
			auto t = Transpose(M);

			// Resizing a normal matrix adds more vectors, and preserves data
			PR_CHECK(m.vecs(), 4);
			PR_CHECK(m.cmps(), 3);
			m.resize(5);
			PR_CHECK(m.vecs(), 5);
			PR_CHECK(m.cmps(), 3);
			for (int r = 0; r != m.vecs(); ++r)
			{
				for (int c = 0; c != m.cmps(); ++c)
				{
					if (r < 4 && c < 3)
						PR_CHECK(m(r, c) == M(r, c), true);
					else
						PR_CHECK(m(r, c) == 0, true);
				}
			}

			// Resizing a transposed matrix adds more transposed vectors, and preserves data 
			PR_CHECK(t.vecs(), 3);
			PR_CHECK(t.cmps(), 4);
			t.resize(5);
			PR_CHECK(t.vecs(), 5);
			PR_CHECK(t.cmps(), 4);
			for (int r = 0; r != t.vecs(); ++r)
			{
				for (int c = 0; c != t.cmps(); ++c)
				{
					if (r < 3 && c < 4)
						PR_CHECK(t(r, c) == M(c, r), true);
					else
						PR_CHECK(t(r, c) == 0, true);
				}
			}
		}
		{// Dot Product
			auto a = Matrix<float>(1, 3, {1.0, 2.0, 3.0});
			auto b = Matrix<float>(1, 3, {3.0, 2.0, 1.0});
			auto r = Dot(a, b);
			PR_CHECK(FEql(r, 10.0f), true);
		}
	}
}
#endif

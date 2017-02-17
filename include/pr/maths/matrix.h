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

	private:

		// Store data as contiguous column vectors (like m4x4 does)
		// e.g.
		//  [{x}  {y}  {z}]
		// is:                memory order:
		//  [x.x  y.x  z.x]    [0  4   8]
		//  [x.y  y.y  z.y]    [1  5   9]
		//  [x.z  y.z  z.z]    [2  6  10]
		//  [x.w  y.w  z.w]    [3  7  11]
		Real  m_buf[LocalBufCount]; // Local buffer for small matrices
		Real* m_data;               // linear buffer of matrix elements
		int   m_cols;               // number of columns (X dimension)
		int   m_rows;               // number of rows (Y dimension)
		bool  m_transposed;         // interpret the matrix as transposed

	public:

		Matrix()
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			resize(0,0);
		}
		Matrix(int cols, int rows, bool transposed = false)
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			resize(cols, rows);
			m_transposed = transposed;
		}
		Matrix(int cols, int rows, std::initializer_list<Real> data, bool transposed = false)
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			assert("Data length mismatch" && int(data.size()) == cols * rows);
			resize(cols, rows);
			memcpy(m_data, data.begin(), cols * rows * sizeof(Real));
			m_transposed = transposed;
		}
		Matrix(m4x4_cref m)
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			resize(4, 4);

			auto data = m_data;
			*data++ = Real(m.x.x); *data++ = Real(m.x.y); *data++ = Real(m.x.z); *data++ = Real(m.x.w);
			*data++ = Real(m.y.x); *data++ = Real(m.y.y); *data++ = Real(m.y.z); *data++ = Real(m.y.w);
			*data++ = Real(m.z.x); *data++ = Real(m.z.y); *data++ = Real(m.z.z); *data++ = Real(m.z.w);
			*data++ = Real(m.w.x); *data++ = Real(m.w.y); *data++ = Real(m.w.z); *data++ = Real(m.w.w);
		}
		Matrix(Matrix&& rhs)
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			if (rhs.local())
			{
				resize(rhs.m_cols, rhs.m_rows);
				memcpy(m_data, rhs.m_data, size() * sizeof(Real));
				m_transposed = rhs.m_transposed;
			}
			else
			{
				m_cols = rhs.m_cols;
				m_rows = rhs.m_rows;
				m_data = rhs.m_data;
				m_transposed = rhs.m_transposed;

				rhs.m_data = &rhs.m_buf[0];
				rhs.m_cols = 0;
				rhs.m_rows = 0;
				rhs.m_transposed = false;
			}
		}
		Matrix(Matrix const& rhs)
			:m_data(&m_buf[0])
			,m_cols(0)
			,m_rows(0)
			,m_transposed(false)
		{
			resize(rhs.m_cols, rhs.m_rows);
			memcpy(m_data, rhs.m_data, size() * sizeof(Real));
			m_transposed = rhs.m_transposed;
		}
		~Matrix()
		{
			resize(0,0);
		}

		// Assignment
		Matrix& operator = (Matrix&& rhs)
		{
			if (this == &rhs) return *this;
			if (rhs.local())
			{
				resize(rhs.m_cols, rhs.m_rows);
				memcpy(m_data, rhs.m_data, size() * sizeof(Real));
				m_transposed = rhs.m_transposed;
			}
			else
			{
				std::swap(m_cols, rhs.m_cols);
				std::swap(m_rows, rhs.m_rows);
				std::swap(m_data, rhs.m_data);
				std::swap(m_transposed, rhs.m_transposed);
			}
			return *this;
		}
		Matrix& operator = (Matrix const& rhs)
		{
			if (this == &rhs) return *this;
			resize(rhs.m_cols, rhs.m_rows);
			memcpy(m_data, rhs.m_data, size() * sizeof(Real));
			return *this;
		}

		// Access this matrix as a 2D array
		Real operator()(int col, int row) const
		{
			if (m_transposed) std::swap(col, row);
			assert(col >= 0 && col < m_cols);
			assert(row >= 0 && row < m_rows);
			return m_data[col * m_rows + row];
		}
		Real& operator()(int col, int row)
		{
			if (m_transposed) std::swap(col, row);
			assert(col >= 0 && col < m_cols);
			assert(row >= 0 && row < m_rows);
			return m_data[col * m_rows + row];
		}

		// The number of columns in the matrix (i.e. X dimension)
		int cols() const
		{
			return !m_transposed ? m_cols : m_rows;
		}

		// The number of rows in the matrix (i.e. Y dimension)
		int rows() const
		{
			return !m_transposed ? m_rows : m_cols;
		}

		// Get/Set whether this matrix is transposed (i.e. switch between column/row major)
		// Doesn't move memory, just changes the interpretation of row/col.
		bool transposed() const
		{
			return m_transposed;
		}
		Matrix& transposed(bool transpose)
		{
			m_transposed = transpose;
			return *this;
		}

		// The total number of elements in the matrix
		int size() const
		{
			return m_cols * m_rows;
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

		// Access this matrix by row
		struct RowProxy
		{
			Matrix* m_mat;
			int     m_row;

			RowProxy(Matrix& m, int row)
				:m_mat(&m)
				,m_row(row)
			{
				assert(row >= 0 && row < m_mat->rows());
			}
			operator Matrix() const
			{
				Matrix v(m_mat->cols(), 1);
				for (int i = 0, iend = m_mat->cols(); i != iend; ++i)
					v(i,0) = (*m_mat)(i, m_row);

				return v;
			}
			RowProxy& operator = (Matrix const& rhs)
			{
				assert("'rhs' must be a row vector" && rhs.rows() == 1 && rhs.cols() == m_mat->cols());

				for (int i = 0, iend = m_mat->cols(); i != iend; ++i)
					(*m_mat)(i,m_row) = rhs(i,0);

				return *this;
			}
			Real operator[](int i) const
			{
				return (*m_mat)(i,m_row);
			}
			Real& operator[](int i)
			{
				return (*m_mat)(i,m_row);
			}
		};
		RowProxy row(int i) const
		{
			return RowProxy(const_cast<Matrix&>(*this), i);
		}

		// Access this matrix by column
		struct ColProxy
		{
			Matrix* m_mat;
			int     m_col;

			ColProxy(Matrix& m, int col)
				:m_mat(&m)
				,m_col(col)
			{
				assert(col >= 0 && col < m_mat->cols());
			}
			operator Matrix() const
			{
				Matrix v(1, m_mat->rows());
				for (int i = 0, iend = m_mat->rows(); i != iend; ++i)
					v(0,i) = (*m_mat)(m_col,i);

				return v;
			}
			ColProxy& operator = (Matrix const& rhs)
			{
				assert("'rhs' must be a column vector" && rhs.cols() == 1 && rhs.rows() == m_mat->rows());

				for (int i = 0, iend = m_mat->rows(); i != iend; ++i)
					(*m_mat)(m_col,i) = rhs(0,i);

				return *this;
			}
			Real operator [](int i) const
			{
				return (*m_mat)(m_col,i);
			}
			Real& operator [](int i)
			{
				return (*m_mat)(m_col,i);
			}
		};
		ColProxy col(int i) const
		{
			return ColProxy(const_cast<Matrix&>(*this), i);
		}

		// True if the matrix is square
		bool IsSquare() const
		{
			return m_cols == m_rows;
		}

		// True if the data of this matrix is locally buffered
		bool local() const
		{
			return m_data == &m_buf[0];
		}

		// Change the dimensions of the matrix.
		// Note: data is only preserved if 'rows' == 'rows()'.
		// If you want to add rows to a matrix while preserving data, use a 'transposed()' matrix.
		void resize(int cols, int rows)
		{
			// Matrix elements are stored as contiguous column vectors
			// so adding/removing columns does not invalidate existing data.
			if (m_transposed) std::swap(cols, rows);

			// Check if a resize is needed
			auto new_count = cols * rows;
			auto old_count = m_cols * m_rows;
			auto min_count = std::min(new_count, old_count);
			
			// Reallocate buffer if necessary.
			auto data =
				new_count == old_count ? m_data :
				new_count > LocalBufCount ? new Real[new_count] :
				&m_buf[0];

			// Copy/Initialise data
			if (rows == m_rows)
			{
				if (data != m_data) memcpy(data, m_data, min_count * sizeof(Real));
				memset(data + min_count, 0, (new_count - min_count) * sizeof(Real));
			}
			else
			{
				memset(data, 0, new_count * sizeof(Real));
			}

			// Update the members
			m_cols = cols;
			m_rows = rows;
			std::swap(m_data, data);

			// Deallocate old buffer
			if (data != m_data && data != &m_buf[0])
				delete[] data;
		}

		// Change the number of column or row vectors in the matrix, preserving data.
		// If the matrix is 'transposed()' the number of rows is changed, otherwise columns.
		void resize(int vecs)
		{
			if (!m_transposed)
				resize(vecs, rows());
			else
				resize(cols(), vecs);
		}

		// Return an identity matrix of the given dimensions
		static Matrix Identity(int cols, int rows)
		{
			Matrix m(cols, rows);
			for (int i = 0, iend = std::min(cols,rows); i != iend; ++i)
				m(i,i) = 1;

			return m;
		}

		// Generates the random matrix
		template <typename Rng = std::default_random_engine> static Matrix Random(Rng& rng, int cols, int rows, Real min_value, Real max_value)
		{
			std::uniform_real_distribution<Real> dist(min_value, max_value);

			Matrix<Real> m(cols, rows);
			for (int i = 0, iend = m.size(); i != iend; ++i)
				m.data()[i] = dist(rng);

			return m;
		}
	};

	// The LU decomposition of a square matrix
	template <typename Real>
	struct MatrixLU :private Matrix<Real>
	{
		// The L and U matrices are stored in one matrix.
		// Note: the base matrix is *not* LU and *not* the original matrix.
		// It's just a compressed way of storing both L and U.

		struct LProxy
		{
			Matrix const* m_mat;
			LProxy(Matrix const& m)
				:m_mat(&m)
			{}
			Real operator ()(int col, int row) const
			{
				assert(col >= 0 && col < m_mat->cols());
				assert(row >= 0 && row < m_mat->rows());
				return col < row ? (*m_mat)(col,row) : col == row ? 1 : 0;
			}
		};
		struct UProxy
		{
			Matrix const* m_mat;
			UProxy(Matrix const& m)
				:m_mat(&m)
			{}
			Real operator ()(int col, int row) const
			{
				assert(col >= 0 && col < m_mat->cols());
				assert(row >= 0 && row < m_mat->rows());
				return col >= row ? (*m_mat)(col,row) : 0;
			}
		};

		// The dimensions of the decomposed matrix
		using Matrix<Real>::cols;
		using Matrix<Real>::rows;

		// The determinant of the permutation matrix
		Real DetOfP;

		// The permutation row indices (length == rows())
		std::unique_ptr<int[]> pi;

		// Access to the lower diagonal matrix
		LProxy L;

		// Access to the upper diagonal matrix
		UProxy U;

		MatrixLU(Matrix const& m)
			:Matrix<Real>(m) // Note: after construction base != m
			,DetOfP(1.0f)
			,pi(std::make_unique<int[]>(m.rows()))
			,L(*this)
			,U(*this)
		{
			if (!m.IsSquare())
				throw std::exception("LU decomposition is only possible on square matrices");

			// We will store both the L and U matrices in 'base' since we know
			// L has the form: [1 0] and U has the form: [U U]
			//                 [L 1]                     [0 U]
			auto  LL = Matrix<Real>::Identity(m.cols(), m.rows());
			auto& UU = static_cast<Matrix<Real>&>(*this);

			// Initialise the permutation vector
			for (int i = 0; i != m.rows(); ++i)
				pi[i] = i;

			// Decompose 'm' into 'LL' and 'UU'
			for (int k = 0, k0 = 0, kend = m.cols() - 1; k != kend; ++k)
			{
				// Find the row with the biggest pivot
				Real p = 0;
				for (int r = k; r != m.rows(); ++r)
				{
					if (abs(UU(k,r)) <= p) continue;
					p = abs(UU(k,r));
					k0 = r;
				}
				if (p == 0)
					throw std::exception("The matrix is singular");

				// Switch two rows in permutation matrix
				std::swap(pi[k], pi[k0]);
				for (int i = 0; i != k; ++i) std::swap(LL(i,k), LL(i,k0));
				if (k != k0) DetOfP *= -1;

				// Switch rows in 'UU'
				for (int c = 0; c != m.cols(); ++c)
					std::swap(UU(c,k), UU(c,k0));

				// Gaussian eliminate the remaining rows
				for (int r = k + 1; r < m.rows(); ++r)
				{
					LL(k,r) = UU(k,r) / UU(k,k);
					for (int c = k; c < m.cols(); ++c)
						UU(c,r) = UU(c,r) - LL(k,r) * UU(c,k);
				}
			}

			// Store 'LL' in the zero part of 'UU' (i.e. *this)
			for (int r = 1; r < m.rows(); ++r)
				for (int c = 0; c != r; ++c)
					UU(c,r) = LL(c,r);
		}

	private:

		// Protect the array access operator, since it doesn't make
		// sense for callers to write to the matrix elements
		using Matrix<Real>::operator();
	};

	#pragma region Operators

	// Unary operators
	template <typename Real> inline Matrix<Real> operator + (Matrix<Real> const& m)
	{
		return m;
	}
	template <typename Real> inline Matrix<Real> operator - (Matrix<Real> const& m)
	{
		Matrix<Real> res(m.cols(), m.rows(), m.transposed());
		for (int i = 0, iend = res.size(); i != iend; ++i)
			res.data()[i] = -m.data()[i];
		
		return res;
	}

	// Addition/Subtraction
	template <typename Real> inline Matrix<Real> operator + (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		assert(lhs.cols() == rhs.cols());
		assert(lhs.rows() == rhs.rows());

		Matrix<Real> res(lhs.cols(), lhs.rows());
		if (lhs.transposed() != rhs.transposed())
		{
			// If one of the matrices is transposed, we need to add element-by-element
			for (int r = 0; r != res.rows(); ++r)
				for (int c = 0; c != res.cols(); ++c)
					res(c,r) = lhs(c,r) + rhs(c,r);
		}
		else
		{
			// If both are not transposed, we can vectorise element adding
			for (int i = 0, iend = res.size(); i != iend; ++i)
				res.data()[i] = lhs.data()[i] + rhs.data()[i];
		}
		return res;
	}
	template <typename Real> inline Matrix<Real> operator - (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		assert(lhs.cols() == rhs.cols());
		assert(lhs.rows() == rhs.rows());

		Matrix<Real> res(lhs.cols(), lhs.rows());
		if (lhs.transposed() != rhs.transposed())
		{
			// If one of the matrices is transposed, we need to add element-by-element
			for (int r = 0; r != res.rows(); ++r)
				for (int c = 0; c != res.cols(); ++c)
					res(c,r) = lhs(c,r) - rhs(c,r);
		}
		else
		{
			// If both are not transposed, we can vectorise element subtracting
			for (int i = 0, iend = res.size(); i != iend; ++i)
				res.data()[i] = lhs.data()[i] - rhs.data()[i];
		}
		return res;
	}

	// Multiplication
	template <typename Real> inline Matrix<Real> operator * (Real s, Matrix<Real> const& mat)
	{
		Matrix<Real> res(mat.cols(), mat.rows());
		for (int i = 0, iend = res.size(); i != iend; ++i)
			res.data()[i] = mat.data()[i] * s;

		return res;
	}
	template <typename Real> Matrix<Real> operator * (Matrix<Real> const& mat, Real s)
	{
		return s * mat;
	}
	template <typename Real> Matrix<Real> operator * (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		assert("Wrong matrix dimensions" && lhs.cols() == rhs.rows());
		using Matrix = Matrix<Real>;

		auto msize = std::max(std::max(lhs.rows(), lhs.cols()), std::max(rhs.rows(), rhs.cols()));
		if (msize < 32)
		{
			// Small matrix multiply
			Matrix res(rhs.cols(), lhs.rows());
			for (int r = 0; r != res.rows(); ++r)
				for (int c = 0; c != res.cols(); ++c)
					for (int k = 0; k != lhs.cols(); ++k)
						res(c,r) += lhs(k,r) * rhs(c,k);
			return res;
		}
		else
		{
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
					field[i*M+j] = Matrix(z,z);
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
							C(c,r) = 0;
							if (xa + c < A.cols() && ya + r < A.rows()) C(c,r) += A(xa + c, ya + r);
							if (xb + c < B.cols() && yb + r < B.rows()) C(c,r) += B(xb + c, yb + r);
						}
					}
				}
				static void SafeAminusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
					{
						for (int c = 0; c < sz; ++c) // cols
						{
							C(c,r) = 0;
							if (xa + c < A.cols() && ya + r < A.rows()) C(c,r) += A(xa + c, ya + r);
							if (xb + c < B.cols() && yb + r < B.rows()) C(c,r) -= B(xb + c, yb + r);
						}
					}
				}
				static void SafeACopytoC(Matrix const& A, int xa, int ya, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
					{
						for (int c = 0; c < sz; ++c) // cols
						{
							C(c,r) = 0;
							if (xa + c < A.cols() && ya + r < A.rows()) C(c,r) += A(xa + c, ya + r);
						}
					}
				}
				static void AplusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(c,r) = A(xa + c, ya + r) + B(xb + c, yb + r);
				}
				static void AminusBintoC(Matrix const& A, int xa, int ya, Matrix const& B, int xb, int yb, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(c,r) = A(xa + c, ya + r) - B(xb + c, yb + r);
				}
				static void ACopytoC(Matrix const& A, int xa, int ya, Matrix& C, int sz)
				{
					for (int r = 0; r < sz; ++r) // rows
						for (int c = 0; c < sz; ++c)
							C(c,r) = A(xa + c, ya + r);
				}
				static void StrassenMultiplyRun(Matrix const& A, Matrix const& B, Matrix& C, int l, Matrix* f)
				{
					// A * B into C, level of recursion, matrix field
					// function for square matrix 2^N x 2^N
					auto sz = A.rows();
					if (sz < 32)
					{
						for (int r = 0; r < C.rows(); ++r)
						{
							for (int c = 0; c < C.cols(); ++c)
							{
								C(c,r) = 0;
								for (int k = 0; k < A.cols(); ++k)
									C(c,r) += A(k,r) * B(c,k);
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
								C(c,r) = f[l*M + 1 + 1](c,r) + f[l*M + 1 + 4](c,r) - f[l*M + 1 + 5](c,r) + f[l*M + 1 + 7](c,r);

						// C12
						for (int r = 0; r < hh; r++) // rows
							for (int c = hh; c < sz; c++) // cols
								C(c,r) = f[l*M + 1 + 3](c - hh,r) + f[l*M + 1 + 5](c - hh,r);

						// C21
						for (int r = hh; r < sz; r++) // rows
							for (int c = 0; c < hh; c++) // cols
								C(c,r) = f[l*M + 1 + 2](c,r - hh) + f[l*M + 1 + 4](c,r - hh);

						// C22
						for (int r = hh; r < sz; r++) // rows
							for (int c = hh; c < sz; c++) // cols
								C(c,r) = f[l*M + 1 + 1](c - hh,r - hh) - f[l*M + 1 + 2](c - hh,r - hh) + f[l*M + 1 + 3](c - hh,r - hh) + f[l*M + 1 + 6](c - hh,r - hh);
					}
				}
			};
			#pragma endregion

			L::SafeAplusBintoC(lhs, 0, 0, lhs, h, h, field[0*M + 0], h);
			L::SafeAplusBintoC(rhs, 0, 0, rhs, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 1], 1, field); // (A11 + A22) * (B11 + B22);

			L::SafeAplusBintoC(lhs, 0, h, lhs, h, h, field[0*M + 0], h);
			L::SafeACopytoC(rhs, 0, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 2], 1, field); // (A21 + A22) * B11;

			L::SafeACopytoC(lhs, 0, 0, field[0*M + 0], h);
			L::SafeAminusBintoC(rhs, h, 0, rhs, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 3], 1, field); //A11 * (B12 - B22);

			L::SafeACopytoC(lhs, h, h, field[0*M + 0], h);
			L::SafeAminusBintoC(rhs, 0, h, rhs, 0, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 4], 1, field); //A22 * (B21 - B11);

			L::SafeAplusBintoC(lhs, 0, 0, lhs, h, 0, field[0*M + 0], h);
			L::SafeACopytoC(rhs, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 5], 1, field); //(A11 + A12) * B22;

			L::SafeAminusBintoC(lhs, 0, h, lhs, 0, 0, field[0*M + 0], h);
			L::SafeAplusBintoC(rhs, 0, 0, rhs, h, 0, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 6], 1, field); //(A21 - A11) * (B11 + B12);

			L::SafeAminusBintoC(lhs, h, 0, lhs, h, h, field[0*M + 0], h);
			L::SafeAplusBintoC(rhs, 0, h, rhs, h, h, field[0*M + 1], h);
			L::StrassenMultiplyRun(field[0*M + 0], field[0*M + 1], field[0*M + 1 + 7], 1, field); // (A12 - A22) * (B21 + B22);

			// Result
			Matrix res(rhs.cols(), lhs.rows());

			// C11
			for (int r = 0; r < std::min(h, res.rows()); r++) // rows
				for (int c = 0; c < std::min(h, res.cols()); c++) // cols
					res(c,r) = field[0*M + 1 + 1](c,r) + field[0*M + 1 + 4](c,r) - field[0*M + 1 + 5](c,r) + field[0*M + 1 + 7](c,r);

			// C12
			for (int r = 0; r < std::min(h, res.rows()); r++) // rows
				for (int c = h; c < std::min(2 * h, res.cols()); c++) // cols
					res(c,r) = field[0*M + 1 + 3](c - h,r) + field[0*M + 1 + 5](c - h,r);

			// C21
			for (int r = h; r < std::min(2 * h, res.rows()); r++) // rows
				for (int c = 0; c < std::min(h, res.cols()); c++) // cols
					res(c,r) = field[0*M + 1 + 2](c,r - h) + field[0*M + 1 + 4](c,r - h);

			// C22
			for (int r = h; r < std::min(2 * h, res.rows()); r++) // rows
				for (int c = h; c < std::min(2 * h, res.cols()); c++) // cols
					res(c,r) = field[0*M + 1 + 1](c - h,r - h) - field[0*M + 1 + 2](c - h,r - h) + field[0*M + 1 + 3](c - h,r - h) + field[0*M + 1 + 6](c - h,r - h);
			
			return res;
		}
	}

	// Equals
	template <typename Real> inline bool operator == (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		if (lhs.cols() != rhs.cols()) return false;
		if (lhs.rows() != rhs.rows()) return false;
		if (lhs.transposed() == rhs.transposed())
			return memcmp(lhs.data(), rhs.data(), lhs.size() * sizeof(Real)) == 0;

		// Fall back to element-by-element comparisons
		for (int r = 0, rend = lhs.rows(); r != rend; ++r)
			for (int c = 0, cend = lhs.cols(); c != cend; ++c)
				if (lhs(c,r) != rhs(c,r))
					return false;

		return true;
	}
	template <typename Real> inline bool operator != (Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		return !(lhs == rhs);
	}

	#pragma endregion

	#pragma region Functions

	// Value equality
	template <typename Real> inline bool FEql(Matrix<Real> const& lhs, Matrix<Real> const& rhs, Real tol = maths::tiny)
	{
		if (lhs.cols() != rhs.cols()) return false;
		if (lhs.rows() != rhs.rows()) return false;
		if (lhs.transposed() == rhs.transposed())
		{
			// Vectorise compares
			for (int i = 0, iend = lhs.size(); i != iend; ++i)
				if (!FEql(lhs.data()[i], rhs.data()[i], tol))
					return false;
		}
		else
		{
			// Element-by-element compares
			for (int r = 0, rend = lhs.rows(); r != rend; ++r)
				for (int c = 0, cend = lhs.cols(); c != cend; ++c)
					if (!FEql(lhs(c,r), rhs(c,r), tol))
						return false;
		}

		return true;
	}
	template <typename Real> inline bool FEql(Matrix<Real> const& lhs, m4x4_cref rhs, Real tol = maths::tiny)
	{
		if (lhs.cols() != 4) return false;
		if (lhs.rows() != 4) return false;
		return
			FEql(float(lhs(0,0)), rhs.x.x, tol) && FEql(float(lhs(0,1)), rhs.x.y, tol) && FEql(float(lhs(0,2)), rhs.x.z, tol) && FEql(float(lhs(0,3)), rhs.x.w, tol) &&
			FEql(float(lhs(1,0)), rhs.y.x, tol) && FEql(float(lhs(1,1)), rhs.y.y, tol) && FEql(float(lhs(1,2)), rhs.y.z, tol) && FEql(float(lhs(1,3)), rhs.y.w, tol) &&
			FEql(float(lhs(2,0)), rhs.z.x, tol) && FEql(float(lhs(2,1)), rhs.z.y, tol) && FEql(float(lhs(2,2)), rhs.z.z, tol) && FEql(float(lhs(2,3)), rhs.z.w, tol) &&
			FEql(float(lhs(3,0)), rhs.w.x, tol) && FEql(float(lhs(3,1)), rhs.w.y, tol) && FEql(float(lhs(3,2)), rhs.w.z, tol) && FEql(float(lhs(3,3)), rhs.w.w, tol);
	}

	// Return the transpose of matrix 'm'
	template <typename Real> inline Matrix<Real> Transpose(Matrix<Real> const& m)
	{
		Matrix<Real> t(m);
		t.transposed(!m.transposed());
		return t;
	}

	// Return the determinant of a matrix
	template <typename Real> inline Real Determinant(MatrixLU<Real> const& m)
	{
		auto det = m.DetOfP;
		for (int i = 0; i != m.rows(); ++i)
			det *= m.U(i,i);

		return det;
	}
	template <typename Real> inline Real Determinant(Matrix<Real> const& m)
	{
		return Determinant(MatrixLU<Real>(m));
	}

	// Return the dot product of two vectors
	template <typename Real> inline Real Dot(Matrix<Real> const& lhs, Matrix<Real> const& rhs)
	{
		assert("Dot product is between column vectors" && lhs.cols() == 1 && lhs.cols() == 1);
		assert("Dot product must be between vectors of the same length" && lhs.rows() == rhs.rows());

		Real dp = 0;
		for (int i = 0, iend = lhs.rows(); i != iend; ++i)
			dp += lhs(0,i) * rhs(0,i);
		return dp;
	}

	// True if 'm' has an inverse
	template <typename Real> inline bool IsInvertable(MatrixLU<Real> const& m)
	{
		return !FEql(Determinant(m), 0);
	}
	template <typename Real> inline bool IsInvertable(Matrix<Real> const& m)
	{
		return IsInvertable(MatrixLU<Real>(m));
	}

	// Solves for 'x' in 'Ax = v'
	template <typename Real> inline Matrix<Real> Solve(MatrixLU<Real> const& A, Matrix<Real> const& v)
	{
		// e.g. [4x4][4x1] = [4x1]
		assert("Solution vector has the wrong dimensions" && A.rows() == v.rows());

		Matrix<Real> a(1, A.rows());
		Matrix<Real> b(1, A.rows());

		// Switch items in "v" due to permutation matrix
		for (int r = 0; r != A.rows(); ++r)
			a(0,r) = v(0,A.pi[r]);

		// Solve for x in 'Lx = b' assuming 'L' is a lower triangular matrix
		for (int r = 0; r != A.rows(); ++r)
		{
			b(0,r) = a(0,r);
			for (int k = 0; k != r; ++k)
				b(0,r) -= A.L(k,r) * b(0,k);
			b(0,r) = b(0,r) / A.L(r,r);
		}

		a = b;

		// Solve for x in 'Ux = b' assuming 'U' is an upper triangular matrix
		for (int r = A.rows(); r-- != 0;)
		{
			b(0,r) = a(0,r);
			for (int k = A.rows() - 1; k > r; k--)
				b(0,r) -= A.U(k,r) * b(0,k);
			b(0,r) = b(0,r) / A.U(r,r);
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
		assert("Matrix has no inverse" && IsInvertable(lu));

		// Inverse of an NxM matrix is a MxN matrix (even though this only works for square matrices)
		Matrix<Real> inv(lu.rows(), lu.cols());
		Matrix<Real> elem(1, lu.rows());
		for (int i = 0; i != elem.size(); ++i)
		{
			elem(0,i) = 1;
			inv.col(i) = Solve(lu, elem);
			elem(0,i) = 0;
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
		if (pow ==  0) return Matrix<Real>::Identity(m.cols(),m.rows());
		if (pow == -1) return Invert(m);

		auto x = pow < 0 ? Invert(m) : m;
		if (pow < 0) pow *= -1;

		auto ret = Matrix<Real>::Identity(m.cols(), m.rows());
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
			// Make it easier by first replacing \r\n’s with |’s then
			// restore the |’s with \r\n’s
			s = s.Replace("\r\n", "|");
			while (s.LastIndexOf("|") == (s.Length - 1))
				s = s.Substring(0, s.Length - 1);

			s = s.Replace("|", "\r\n");
			s = s.Trim();
		}

		var rows = Regex.Split(s, "\r\n");
		var nums = rows[0].Split(' ');
		var m = new Matrix(rows.Length, nums.Length);
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
#include "pr/maths/matrix4x4.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_maths_matrix)
		{
			using namespace pr;
			std::default_random_engine rng(1);

			{// Compare with m4x4
				auto M = Random4x4(rng, -5.0f, +5.0f, v4Origin);
				Matrix<float> m(M);

				PR_CHECK(FEql(m, M), true);
				PR_CHECK(FEql(m(0,3), M.x.w), true);
				PR_CHECK(FEql(m(3,0), M.w.x), true);
				PR_CHECK(FEql(m(2,2), M.z.z), true);

				PR_CHECK(IsInvertable(m) == IsInvertable(M), true);

				auto m1 = Invert(m);
				auto M1 = Invert(M);
				PR_CHECK(FEql(m1, M1), true);

				auto m2 = Transpose(m);
				auto M2 = Transpose4x4(M);
				PR_CHECK(FEql(m2, M2), true);
			}
			{// Multiply round trip
				std::uniform_real_distribution<float> dist(-5.0f, +5.0f);
				const int SZ = 100;
				Matrix<float> m(SZ,SZ);
				for (int k = 0; k != 10; ++k)
				{
					for (int r = 0; r != m.rows(); ++r)
						for (int c = 0; c != m.cols(); ++c)
							m(c,r) = dist(rng);

					if (IsInvertable(m))
					{
						auto m_inv = Invert(m);

						auto i0 = Matrix<float>::Identity(SZ,SZ);
						auto i1 = m * m_inv;
						auto i2 = m_inv * m;

						PR_CHECK(FEql(i0, i1, 0.0001f), true);
						PR_CHECK(FEql(i0, i2, 0.0001f), true);

						break;
					}
				}
			}
			{// Transpose
				const int cols = 3, rows = 4;
				auto m = Matrix<double>::Random(rng, cols, rows, -5.0, 5.0);
				auto t = Transpose(m);

				PR_CHECK(m.cols(), cols);
				PR_CHECK(m.rows(), rows);
				PR_CHECK(t.cols(), rows);
				PR_CHECK(t.rows(), cols);

				for (int r = 0; r != rows; ++r)
					for (int c = 0; c != cols; ++c)
						PR_CHECK(m(c,r) == t(r,c), true);
			}
			{// Resizing
				const int cols = 3, rows = 4, sz = 5;
				auto M = Matrix<double>::Random(rng, cols, rows, -5.0, 5.0);
				auto m = M;
				auto t = Transpose(M);

				// Resizing a normal matrix adds more columns, and preserves data
				m.resize(sz);
				PR_CHECK(m.cols(), sz);
				PR_CHECK(m.rows(), rows);
				for (int r = 0; r != m.rows(); ++r)
					for (int c = 0; c != m.cols(); ++c)
						if (c < cols)
							PR_CHECK(m(c,r) == M(c,r), true);
						else
							PR_CHECK(m(c,r) == 0, true);

				// Resizing a transposed matrix adds more rows, and preserves data 
				t.resize(sz);
				PR_CHECK(t.cols(), rows);
				PR_CHECK(t.rows(), sz);
				for (int r = 0; r != t.rows(); ++r)
					for (int c = 0; c != t.cols(); ++c)
						if (r < cols)
							PR_CHECK(t(c,r) == M(r,c), true);
						else
							PR_CHECK(t(c,r) == 0, true);
			}
			{// Dot Product
				auto a = Matrix<float>(1,3,{1.0, 2.0, 3.0});
				auto b = Matrix<float>(1,3,{3.0, 2.0, 1.0});
				auto r = Dot(a,b);
				PR_CHECK(FEql(r, 10.0f), true);
			}
		}
	}
}
#endif
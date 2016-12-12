//***************************************************
// Matrix
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using pr.extn;

namespace pr.maths
{
	/*
    Matrix class in C#
    Written by Ivan Kuckir (ivan.kuckir@gmail.com, http://blog.ivank.net)
    Faculty of Mathematics and Physics
    Charles University in Prague
    (C) 2010
    - updated on 1. 6.2014 - Trimming the string before parsing
    - updated on 14.6.2012 - parsing improved. Thanks to Andy!
    - updated on 3.10.2012 - there was a terrible bug in LU, SoLE and Inversion. Thanks to Danilo Neves Cruz for reporting that!
	- updated on 3.11.2016 - refactored

    This code is distributed under MIT licence.

		Permission is hereby granted, free of charge, to any person
		obtaining a copy of this software and associated documentation
		files (the "Software"), to deal in the Software without
		restriction, including without limitation the rights to use,
		copy, modify, merge, publish, distribute, sub license, and/or sell
		copies of the Software, and to permit persons to whom the
		Software is furnished to do so, subject to the following
		conditions:

		The above copyright notice and this permission notice shall be
		included in all copies or substantial portions of the Software.

		THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
		EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
		OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
		NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
		HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
		WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
		FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
		OTHER DEALINGS IN THE SOFTWARE.
	*/
	[Serializable]
	[DebuggerDisplay("m{rows}x{cols}) {mat}")]
	public struct Matrix
	{
		public Matrix(int row_count, int col_count)
		{
			rows = row_count;
			cols = col_count;
			mat = new double[rows * cols];
			m_cache = null;
		}

		/// <summary>Copy constructor</summary>
		public Matrix(Matrix rhs)
			:this(rhs.rows, rhs.cols)
		{
			for (int i = 0; i < rows; i++)
				for (int j = 0; j < cols; j++)
					this[i,j] = rhs[i,j];

			// Clone the cached data too if available
			if (rhs.m_cache != null)
				m_cache = new CachedLU(rhs.m_cache);
		}

		/// <summary>Dimensions</summary>
		public int rows;
		public int cols;

		/// <summary>The matrix data</summary>
		public double[] mat;

		/// <summary>Access this matrix as a 2D array</summary>
		public double this[int row, int col]
		{
			get { return mat[row * cols + col]; }
			set { mat[row * cols + col] = value; }
		}

		/// <summary>Access this matrix by row</summary>
		public RowProxy row { get { return new RowProxy(this); } }
		public class RowProxy
		{
			private Matrix mat;
			public RowProxy(Matrix m) { mat = m; }
			public Matrix this[int k]
			{
				get
				{
					var m = new Matrix(1, mat.cols);
					for (int i = 0; i < mat.cols; i++)
						m[0,i] = mat[k,i];

					return m;
				}
				set
				{
					for (int i = 0; i < mat.rows; i++)
						mat[k,i] = value[0,i];
				}
			}
		}

		/// <summary>Access this matrix by column</summary>
		public ColProxy col { get { return new ColProxy(this); } }
		public class ColProxy
		{
			private Matrix mat;
			public ColProxy(Matrix m) { mat = m; }
			public Matrix this[int k]
			{
				get
				{
					var m = new Matrix(mat.rows, 1);
					for (int i = 0; i < mat.rows; i++)
						m[i,0] = mat[i,k];

					return m;
				}
				set
				{
					for (int i = 0; i < mat.rows; i++)
						mat[i,k] = value[i,0];
				}
			}
		}

		/// <summary>Cache the results of LU decomposition</summary>
		private CachedLU cache { get { return m_cache ?? (m_cache = new CachedLU(this)); } }
		private CachedLU m_cache;
		private class CachedLU
		{
			public CachedLU(Matrix m)
			{
				DetOfP = 1;

				if (!m.IsSquare)
					throw new Exception("LU decomposition is only possible on square matrices");

				L = IdentityMatrix(m.rows, m.cols);
				U = new Matrix(m);

				// Initialise the permutation vector
				pi = new int[m.rows];
				for (int i = 0; i < m.rows; i++)
					pi[i] = i;

				int k0 = 0;
				for (int k = 0; k < m.cols - 1; k++)
				{
					var p = 0.0;

					// Find the row with the biggest pivot
					for (int i = k; i < m.rows; i++)
					{
						if (Math.Abs(U[i, k]) > p)
						{
							p = Math.Abs(U[i, k]);
							k0 = i;
						}
					}
					if (p == 0)
						throw new Exception("The matrix is singular");

					{// Switch two rows in permutation matrix
						var tmp = pi[k];
						pi[k] = pi[k0];
						pi[k0] = tmp;
					}
					for (int i = 0; i < k; i++)
					{
						var tmp = L[k, i];
						L[k, i] = L[k0, i];
						L[k0, i] = tmp;
					}

					if (k != k0)
						DetOfP *= -1;

					// Switch rows in U
					for (int i = 0; i < m.cols; i++)
					{
						var tmp = U[k, i];
						U[k, i] = U[k0, i];
						U[k0, i] = tmp;
					}

					for (int i = k + 1; i < m.rows; i++)
					{
						L[i, k] = U[i, k] / U[k, k];
						for (int j = k; j < m.cols; j++)
							U[i, j] = U[i, j] - L[i, k] * U[k, j];
					}
				}
			}
			public CachedLU(CachedLU rhs)
			{
				L = new Matrix(rhs.L);
				U = new Matrix(rhs.U);
				DetOfP = rhs.DetOfP;
			}

			/// <summary>The LU decomposition of the owner matrix</summary>
			public Matrix L;
			public Matrix U;

			/// <summary>The determinant of the permutation matrix</summary>
			public double DetOfP;

			/// <summary>The permutation row indices</summary>
			public int[] pi;
		}

		/// <summary>Return the lower part of the LU decomposition of this matrix</summary>
		public Matrix L
		{
			get { return cache.L; }
		}

		/// <summary>Return the upper part of the LU decomposition of this matrix</summary>
		public Matrix U
		{
			get { return cache.U; }
		}

		/// <summary>True if the matrix is square</summary>
		public bool IsSquare
		{
			get { return rows == cols; }
		}

		/// <summary>Return the determinant of this matrix</summary>
		public double Determinant
		{
			get
			{
				var det = cache.DetOfP;
				for (int i = 0; i < rows; i++)
					det *= U[i, i];
				return det;
			}
		}

		/// <summary>Special case equality operators</summary>
		public static bool FEql(Matrix lhs, Matrix rhs)
		{
			if (lhs.rows != rhs.rows) return false;
			if (lhs.cols != rhs.cols) return false;
			for (int i = 0; i != lhs.mat.Length; ++i)
				if (!Maths.FEql(lhs.mat[i], rhs.mat[i]))
					return false;

			return true;
		}
		public static bool FEql(Matrix lhs, m4x4 rhs)
		{
			if (lhs.rows != 4 && lhs.cols != 4) return false;
			return
				Maths.FEql((float)lhs.mat[ 0], rhs.x.x) &&
				Maths.FEql((float)lhs.mat[ 1], rhs.x.y) &&
				Maths.FEql((float)lhs.mat[ 2], rhs.x.z) &&
				Maths.FEql((float)lhs.mat[ 3], rhs.x.w) &&

				Maths.FEql((float)lhs.mat[ 4], rhs.y.x) &&
				Maths.FEql((float)lhs.mat[ 5], rhs.y.y) &&
				Maths.FEql((float)lhs.mat[ 6], rhs.y.z) &&
				Maths.FEql((float)lhs.mat[ 7], rhs.y.w) &&

				Maths.FEql((float)lhs.mat[ 8], rhs.z.x) &&
				Maths.FEql((float)lhs.mat[ 9], rhs.z.y) &&
				Maths.FEql((float)lhs.mat[10], rhs.z.z) &&
				Maths.FEql((float)lhs.mat[11], rhs.z.w) &&

				Maths.FEql((float)lhs.mat[12], rhs.w.x) &&
				Maths.FEql((float)lhs.mat[13], rhs.w.y) &&
				Maths.FEql((float)lhs.mat[14], rhs.w.z) &&
				Maths.FEql((float)lhs.mat[15], rhs.w.w);
		}

		/// <summary>Return an identity matrix of the given dimensions</summary>
		public static Matrix IdentityMatrix(int row_count, int col_count)
		{
			var matrix = new Matrix(row_count, col_count);
			for (int i = 0; i < Math.Min(row_count, col_count); i++)
				matrix[i, i] = 1;
			return matrix;
		}

		/// <summary>Find the transpose of matrix 'm'</summary>
		public static Matrix Transpose(Matrix m)
		{
			var t = new Matrix(m.cols, m.rows);
			for (int i = 0; i < m.rows; i++)
				for (int j = 0; j < m.cols; j++)
					t[j, i] = m[i, j];
			return t;
		}

		/// <summary>True if 'mat' has an inverse</summary>
		public static bool IsInvertable(Matrix m)
		{
			return !Maths.FEql(m.Determinant, 0);
		}

		/// <summary>Return the inverse of matrix 'm'</summary>
		public static Matrix Invert(Matrix m)
		{
			Debug.Assert(IsInvertable(m), "Matrix has no inverse");

			var inv = new Matrix(m.rows, m.cols);
			var elem = new Matrix(m.rows, 1);
			for (int i = 0; i != m.rows; ++i)
			{
				elem[i,0] = 1;
				inv.col[i] = Solve(m, elem);
				elem[i,0] = 0;
			}
			return inv;
		}

		/// <summary>Matrix to the power 'pow'</summary>
		public static Matrix Power(Matrix m, int pow)
		{
			if (pow == 0) return IdentityMatrix(m.rows, m.cols);
			if (pow == 1) return new Matrix(m);
			if (pow == -1) return Invert(m);

			var x = pow < 0 ? Invert(m) : new Matrix(m);
			if (pow < 0) pow *= -1;

			var ret = IdentityMatrix(m.rows, m.cols);
			while (pow != 0)
			{
				if ((pow & 1) == 1) ret *= x;
				x *= x;
				pow >>= 1;
			}
			return ret;
		}

		/// <summary>Solves for x in 'Ax = v'</summary>
		public static Matrix Solve(Matrix A, Matrix v)
		{
			if (A.rows != A.cols) throw new Exception("The matrix is not square");
			if (A.rows != v.rows) throw new Exception("Solution vector has the wrong dimensions");

			// Switch two items in "v" due to permutation matrix
			var b = new Matrix(A.rows, 1);
			for (int i = 0; i < A.rows; i++)
				b[i,0] = v[A.cache.pi[i], 0];

			var x0 = SolveL(A.L, b);
			var x1 = SolveU(A.U, x0);
			return x1;
		}

		/// <summary>Solves for x in 'Lx = b' assuming 'L' is a lower triangular matrix</summary>
		public static Matrix SolveL(Matrix L, Matrix b)
		{
			var x = new Matrix(L.rows, 1);
			for (int i = 0; i < L.rows; i++)
			{
				x[i,0] = b[i,0];
				for (int j = 0; j < i; j++)
					x[i,0] -= L[i,j] * x[j,0];

				x[i,0] = x[i,0] / L[i,i];
			}
			return x;
		}

		/// <summary>Solves for x in 'Ux = b' assuming 'U' is an upper triangular matrix</summary>
		public static Matrix SolveU(Matrix U, Matrix b)
		{
			var x = new Matrix(U.rows, 1);
			for (int i = U.rows; i-- != 0;)
			{
				x[i,0] = b[i,0];
				for (int j = U.rows - 1; j > i; j--)
					x[i,0] -= U[i,j] * x[j,0];

				x[i,0] = x[i,0] / U[i,i];
			}
			return x;
		}

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

		/// <summary>Generates the random matrix</summary>
		public static Matrix Random(int row_count, int col_count, double min, double max, Random r)
		{
			var m = new Matrix(row_count, col_count);
			for (int i = 0; i != row_count; ++i)
				for (int j = 0; j != col_count; ++j)
					m[i,j] = r.Double(min, max);

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

		#region Operators

		// Unary operators
		public static Matrix operator + (Matrix m)
		{
			return m;
		}
		public static Matrix operator - (Matrix m)
		{
			return -1 * m;
		}

		// Addition/Subtraction
		public static Matrix operator + (Matrix lhs, Matrix rhs)
		{
			if (lhs.rows != rhs.rows || lhs.cols != rhs.cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.rows, lhs.cols);
			for (int i = 0; i != r.rows; ++i)
				for (int j = 0; j != r.cols; ++j)
					r[i,j] = lhs[i,j] + rhs[i,j];

			return r;
		}
		public static Matrix operator - (Matrix lhs, Matrix rhs)
		{
			if (lhs.rows != rhs.rows || lhs.cols != rhs.cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.rows, lhs.cols);
			for (int i = 0; i != r.rows; ++i)
				for (int j = 0; j != r.cols; ++j)
					r[i,j] = lhs[i,j] - rhs[i,j];

			return r;
		}

		/// <summary>Matrix product</summary>
		public static Matrix operator * (Matrix lhs, Matrix rhs)
		{
			if (lhs.cols != rhs.rows)
				throw new Exception("Wrong dimension of matrix!");

			Matrix R;
			int msize = Math.Max(Math.Max(lhs.rows, lhs.cols), Math.Max(rhs.rows, rhs.cols));
			if (msize < 32)
			{
				// Small matrix multiply
				R = new Matrix(lhs.rows, rhs.cols);
				for (int i = 0; i < R.rows; i++)
					for (int j = 0; j < R.cols; j++)
						for (int k = 0; k < lhs.cols; k++)
							R[i, j] += lhs[i, k] * rhs[k, j];
			}
			else
			{
				// 'Strassen Multiply'
				int size = 1; int n = 0;
				while (msize > size) { size *= 2; n++; };
				int h = size / 2;

				//  8x8, 8x8, 8x8, ...
				//  4x4, 4x4, 4x4, ...
				//  2x2, 2x2, 2x2, ...
				//  . . .
				var field = new Matrix[n, 9];
				for (int i = 0; i < n - 4; i++)
				{
					var z = (int)Math.Pow(2, n - i - 1);
					for (int j = 0; j < 9; j++)
						field[i,j] = new Matrix(z, z);
				}

				#region Sub Functions
				Action<Matrix,int,int,Matrix,int,int,Matrix,int> SafeAplusBintoC = (Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.cols && yb + i < B.rows) C[i, j] += B[yb + i, xb + j];
						}
					}
				};
				Action<Matrix,int,int,Matrix,int,int,Matrix,int> SafeAminusBintoC = (Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.cols && yb + i < B.rows) C[i, j] -= B[yb + i, xb + j];
						}
					}
				};
				Action<Matrix,int,int,Matrix,int> SafeACopytoC = (Matrix A, int xa, int ya, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.cols && ya + i < A.rows) C[i, j] += A[ya + i, xa + j];
						}
					}
				};
				Action<Matrix,int,int,Matrix,int,int,Matrix,int> AplusBintoC = (Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j] + B[yb + i, xb + j];
				};
				Action<Matrix,int,int,Matrix,int,int,Matrix,int> AminusBintoC = (Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j] - B[yb + i, xb + j];
				};
				Action<Matrix,int,int,Matrix,int> ACopytoC = (Matrix A, int xa, int ya, Matrix C, int sz) =>
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j];
				};
				Action<Matrix,Matrix,Matrix,int,Matrix[,]> StrassenMultiplyRun = null;
				StrassenMultiplyRun = (Matrix A, Matrix B, Matrix C, int l, Matrix[,] f) =>
				{
					// A * B into C, level of recursion, matrix field
					// function for square matrix 2^N x 2^N
					var sz = A.rows;
					if (sz < 32)
					{
						for (int i = 0; i < C.rows; i++)
						{
							for (int j = 0; j < C.cols; j++)
							{
								C[i, j] = 0;
								for (int k = 0; k < A.cols; k++)
									C[i, j] += A[i, k] * B[k, j];
							}
						}
						return;
					}

					var hh = sz / 2;
					AplusBintoC(A, 0, 0, A, hh, hh, f[l, 0], hh);
					AplusBintoC(B, 0, 0, B, hh, hh, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 1], l + 1, f); // (A11 + A22) * (B11 + B22);

					AplusBintoC(A, 0, hh, A, hh, hh, f[l, 0], hh);
					ACopytoC(B, 0, 0, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 2], l + 1, f); // (A21 + A22) * B11;

					ACopytoC(A, 0, 0, f[l, 0], hh);
					AminusBintoC(B, hh, 0, B, hh, hh, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 3], l + 1, f); //A11 * (B12 - B22);

					ACopytoC(A, hh, hh, f[l, 0], hh);
					AminusBintoC(B, 0, hh, B, 0, 0, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 4], l + 1, f); //A22 * (B21 - B11);

					AplusBintoC(A, 0, 0, A, hh, 0, f[l, 0], hh);
					ACopytoC(B, hh, hh, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 5], l + 1, f); //(A11 + A12) * B22;

					AminusBintoC(A, 0, hh, A, 0, 0, f[l, 0], hh);
					AplusBintoC(B, 0, 0, B, hh, 0, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 6], l + 1, f); //(A21 - A11) * (B11 + B12);

					AminusBintoC(A, hh, 0, A, hh, hh, f[l, 0], hh);
					AplusBintoC(B, 0, hh, B, hh, hh, f[l, 1], hh);
					StrassenMultiplyRun(f[l, 0], f[l, 1], f[l, 1 + 7], l + 1, f); // (A12 - A22) * (B21 + B22);

					/// C11
					for (int i = 0; i < hh; i++) // rows
						for (int j = 0; j < hh; j++) // cols
							C[i, j] = f[l, 1 + 1][i, j] + f[l, 1 + 4][i, j] - f[l, 1 + 5][i, j] + f[l, 1 + 7][i, j];

					/// C12
					for (int i = 0; i < hh; i++) // rows
						for (int j = hh; j < sz; j++) // cols
							C[i, j] = f[l, 1 + 3][i, j - hh] + f[l, 1 + 5][i, j - hh];

					/// C21
					for (int i = hh; i < sz; i++) // rows
						for (int j = 0; j < hh; j++) // cols
							C[i, j] = f[l, 1 + 2][i - hh, j] + f[l, 1 + 4][i - hh, j];

					/// C22
					for (int i = hh; i < sz; i++) // rows
						for (int j = hh; j < sz; j++) // cols
							C[i, j] = f[l, 1 + 1][i - hh, j - hh] - f[l, 1 + 2][i - hh, j - hh] + f[l, 1 + 3][i - hh, j - hh] + f[l, 1 + 6][i - hh, j - hh];
				};
				#endregion

				SafeAplusBintoC(lhs, 0, 0, lhs, h, h, field[0, 0], h);
				SafeAplusBintoC(rhs, 0, 0, rhs, h, h, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 1], 1, field); // (A11 + A22) * (B11 + B22);

				SafeAplusBintoC(lhs, 0, h, lhs, h, h, field[0, 0], h);
				SafeACopytoC(rhs, 0, 0, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 2], 1, field); // (A21 + A22) * B11;

				SafeACopytoC(lhs, 0, 0, field[0, 0], h);
				SafeAminusBintoC(rhs, h, 0, rhs, h, h, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 3], 1, field); //A11 * (B12 - B22);

				SafeACopytoC(lhs, h, h, field[0, 0], h);
				SafeAminusBintoC(rhs, 0, h, rhs, 0, 0, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 4], 1, field); //A22 * (B21 - B11);

				SafeAplusBintoC(lhs, 0, 0, lhs, h, 0, field[0, 0], h);
				SafeACopytoC(rhs, h, h, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 5], 1, field); //(A11 + A12) * B22;

				SafeAminusBintoC(lhs, 0, h, lhs, 0, 0, field[0, 0], h);
				SafeAplusBintoC(rhs, 0, 0, rhs, h, 0, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 6], 1, field); //(A21 - A11) * (B11 + B12);

				SafeAminusBintoC(lhs, h, 0, lhs, h, h, field[0, 0], h);
				SafeAplusBintoC(rhs, 0, h, rhs, h, h, field[0, 1], h);
				StrassenMultiplyRun(field[0, 0], field[0, 1], field[0, 1 + 7], 1, field); // (A12 - A22) * (B21 + B22);

				R = new Matrix(lhs.rows, rhs.cols); // result

				/// C11
				for (int i = 0; i < Math.Min(h, R.rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i, j] + field[0, 1 + 4][i, j] - field[0, 1 + 5][i, j] + field[0, 1 + 7][i, j];

				/// C12
				for (int i = 0; i < Math.Min(h, R.rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.cols); j++) // cols
						R[i, j] = field[0, 1 + 3][i, j - h] + field[0, 1 + 5][i, j - h];

				/// C21
				for (int i = h; i < Math.Min(2 * h, R.rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.cols); j++) // cols
						R[i, j] = field[0, 1 + 2][i - h, j] + field[0, 1 + 4][i - h, j];

				/// C22
				for (int i = h; i < Math.Min(2 * h, R.rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i - h, j - h] - field[0, 1 + 2][i - h, j - h] + field[0, 1 + 3][i - h, j - h] + field[0, 1 + 6][i - h, j - h];
			}
			return R;
		}

		/// <summary>Matrix times scalar</summary>
		public static Matrix operator * (double s, Matrix mat)
		{
			var r = new Matrix(mat.rows, mat.cols);
			for (int i = 0; i < mat.rows; i++)
				for (int j = 0; j < mat.cols; j++)
					r[i, j] = mat[i, j] * s;
			return r;
		}
		public static Matrix operator * (Matrix mat, double s)
		{
			return s * mat;
		}

		#endregion

		#region Equals
		public static bool operator == (Matrix lhs, Matrix rhs)
		{
			return Array_.Equal(lhs.mat, rhs.mat);
		}
		public static bool operator != (Matrix lhs, Matrix rhs)
		{
			return !(lhs == rhs);
		}
		public override bool Equals(object o)
		{
			return o is Matrix && (Matrix)o == this;
		}
		public override int GetHashCode()
		{
			return new { rows, cols, mat }.GetHashCode();
		}
		#endregion
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class UnitTestMatrix
	{
		[Test] public void Basic()
		{
			var rng = new Random(1);

			// Compare with m4x4
			var m0 = new Matrix(4,4);
			var M0 = m4x4.Random4x4(-5, +5, rng);
			for (int i = 0; i != 4; ++i)
				for (int j = 0; j != 4; ++j)
					m0[i,j] = M0[i,j];

			Assert.True(Matrix.FEql(m0, M0));

			Assert.True(Maths.FEql(M0.w.x, m0[3,0]));
			Assert.True(Maths.FEql(M0.x.w, m0[0,3]));
			Assert.True(Maths.FEql(M0.z.z, m0[2,2]));

			Assert.AreEqual(Matrix.IsInvertable(m0), m4x4.IsInvertable(M0));

			var m1 = Matrix.Invert(m0);
			var M1 = m4x4.Invert(M0);
			Assert.True(Matrix.FEql(m1, M1));

			var m2 = Matrix.Transpose(m0);
			var M2 = m4x4.Transpose4x4(M0);
			Assert.True(Matrix.FEql(m2, M2));
		}
	}
}
#endif
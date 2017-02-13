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
	/// <summary>A dynamic NxM matrix</summary>
	[DebuggerDisplay("{m_rows}x{m_cols}) {m_data}")]
	public struct Matrix
	{
		public int m_rows;
		public int m_cols;
		public double[] m_data;

		public Matrix(int row_count, int col_count)
		{
			m_rows = row_count;
			m_cols = col_count;
			m_data = new double[m_rows * m_cols];
		}
		public Matrix(Matrix rhs)
			:this(rhs.m_rows, rhs.m_cols)
		{
			Array.Copy(rhs.m_data, m_data, rhs.m_data.Length);
		}

		/// <summary>Return an identity matrix of the given dimensions</summary>
		public static Matrix Identity(int row_count, int col_count)
		{
			var m = new Matrix(row_count, col_count);
			for (int i = 0, iend = Math.Min(row_count, col_count); i != iend; ++i)
				m[i, i] = 1;

			return m;
		}

		/// <summary>Access this matrix as a 2D array</summary>
		public double this[int row, int col]
		{
			get
			{
				Debug.Assert(row >= 0 && row < m_rows);
				Debug.Assert(col >= 0 && col < m_cols);
				return m_data[row * m_cols + col];
			}
			set
			{
				Debug.Assert(row >= 0 && row < m_rows);
				Debug.Assert(col >= 0 && col < m_cols);
				m_data[row * m_cols + col] = value;
			}
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
					var m = new Matrix(1, mat.m_cols);
					for (int i = 0; i < mat.m_cols; i++)
						m[0,i] = mat[k,i];

					return m;
				}
				set
				{
					for (int i = 0; i < mat.m_rows; i++)
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
					var m = new Matrix(mat.m_rows, 1);
					for (int i = 0; i < mat.m_rows; i++)
						m[i,0] = mat[i,k];

					return m;
				}
				set
				{
					for (int i = 0; i < mat.m_rows; i++)
						mat[i,k] = value[i,0];
				}
			}
		}

		#region Functions

		/// <summary>Special case equality operators</summary>
		public static bool FEql(Matrix lhs, Matrix rhs)
		{
			if (lhs.m_rows != rhs.m_rows) return false;
			if (lhs.m_cols != rhs.m_cols) return false;
			for (int i = 0; i != lhs.m_data.Length; ++i)
				if (!Maths.FEql(lhs.m_data[i], rhs.m_data[i]))
					return false;

			return true;
		}
		public static bool FEql(Matrix lhs, m4x4 rhs)
		{
			if (lhs.m_rows != 4 && lhs.m_cols != 4) return false;
			return
				Maths.FEql((float)lhs.m_data[ 0], rhs.x.x) &&
				Maths.FEql((float)lhs.m_data[ 1], rhs.x.y) &&
				Maths.FEql((float)lhs.m_data[ 2], rhs.x.z) &&
				Maths.FEql((float)lhs.m_data[ 3], rhs.x.w) &&

				Maths.FEql((float)lhs.m_data[ 4], rhs.y.x) &&
				Maths.FEql((float)lhs.m_data[ 5], rhs.y.y) &&
				Maths.FEql((float)lhs.m_data[ 6], rhs.y.z) &&
				Maths.FEql((float)lhs.m_data[ 7], rhs.y.w) &&

				Maths.FEql((float)lhs.m_data[ 8], rhs.z.x) &&
				Maths.FEql((float)lhs.m_data[ 9], rhs.z.y) &&
				Maths.FEql((float)lhs.m_data[10], rhs.z.z) &&
				Maths.FEql((float)lhs.m_data[11], rhs.z.w) &&

				Maths.FEql((float)lhs.m_data[12], rhs.w.x) &&
				Maths.FEql((float)lhs.m_data[13], rhs.w.y) &&
				Maths.FEql((float)lhs.m_data[14], rhs.w.z) &&
				Maths.FEql((float)lhs.m_data[15], rhs.w.w);
		}

		/// <summary>True if the matrix is square</summary>
		public bool IsSquare
		{
			get { return m_rows == m_cols; }
		}

		/// <summary>Find the transpose of matrix 'm'</summary>
		public static Matrix Transpose(Matrix m)
		{
			var t = new Matrix(m.m_cols, m.m_rows);
			for (int i = 0; i < m.m_rows; i++)
				for (int j = 0; j < m.m_cols; j++)
					t[j, i] = m[i, j];
			return t;
		}

		/// <summary>Return the determinant of this matrix</summary>
		public static double Determinant(Matrix m)
		{
			return MatrixLU.Determinant(new MatrixLU(m));
		}

		/// <summary>True if 'mat' has an inverse</summary>
		public static bool IsInvertable(Matrix m)
		{
			return MatrixLU.IsInvertable(new MatrixLU(m));
		}

		/// <summary>Solves for x in 'Ax = v'</summary>
		public static Matrix Solve(Matrix A, Matrix v)
		{
			return MatrixLU.Solve(new MatrixLU(A), v);
		}

		/// <summary>Return the inverse of matrix 'm'</summary>
		public static Matrix Invert(Matrix m)
		{
			return MatrixLU.Invert(new MatrixLU(m));
		}

		/// <summary>Matrix to the power 'pow'</summary>
		public static Matrix Power(Matrix m, int pow)
		{
			if (pow == +1) return m;
			if (pow ==  0) return Identity(m.m_rows, m.m_cols);
			if (pow == -1) return Invert(m);

			var x = pow < 0 ? Invert(m) : m;
			if (pow < 0) pow *= -1;

			var ret = Identity(m.m_rows, m.m_cols);
			while (pow != 0)
			{
				if ((pow & 1) == 1) ret *= x;
				x *= x;
				pow >>= 1;
			}
			return ret;
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
			for (int i = 0; i < m_rows; i++)
			{
				for (int j = 0; j < m_cols; j++)
					s.Append("{0,5:0.00} ".Fmt(this[i,j]));

				s.AppendLine();
			}
			return s.ToString();
		}

		#endregion

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
			if (lhs.m_rows != rhs.m_rows || lhs.m_cols != rhs.m_cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.m_rows, lhs.m_cols);
			for (int i = 0; i != r.m_rows; ++i)
				for (int j = 0; j != r.m_cols; ++j)
					r[i,j] = lhs[i,j] + rhs[i,j];

			return r;
		}
		public static Matrix operator - (Matrix lhs, Matrix rhs)
		{
			if (lhs.m_rows != rhs.m_rows || lhs.m_cols != rhs.m_cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.m_rows, lhs.m_cols);
			for (int i = 0; i != r.m_rows; ++i)
				for (int j = 0; j != r.m_cols; ++j)
					r[i,j] = lhs[i,j] - rhs[i,j];

			return r;
		}

		/// <summary>Matrix product</summary>
		public static Matrix operator * (Matrix lhs, Matrix rhs)
		{
			if (lhs.m_cols != rhs.m_rows)
				throw new Exception("Wrong dimension of matrix!");

			Matrix R;
			int msize = Math.Max(Math.Max(lhs.m_rows, lhs.m_cols), Math.Max(rhs.m_rows, rhs.m_cols));
			if (msize < 32)
			{
				// Small matrix multiply
				R = new Matrix(lhs.m_rows, rhs.m_cols);
				for (int i = 0; i < R.m_rows; i++)
					for (int j = 0; j < R.m_cols; j++)
						for (int k = 0; k < lhs.m_cols; k++)
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
							if (xa + j < A.m_cols && ya + i < A.m_rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.m_cols && yb + i < B.m_rows) C[i, j] += B[yb + i, xb + j];
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
							if (xa + j < A.m_cols && ya + i < A.m_rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.m_cols && yb + i < B.m_rows) C[i, j] -= B[yb + i, xb + j];
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
							if (xa + j < A.m_cols && ya + i < A.m_rows) C[i, j] += A[ya + i, xa + j];
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
					var sz = A.m_rows;
					if (sz < 32)
					{
						for (int i = 0; i < C.m_rows; i++)
						{
							for (int j = 0; j < C.m_cols; j++)
							{
								C[i, j] = 0;
								for (int k = 0; k < A.m_cols; k++)
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

					// C11
					for (int i = 0; i < hh; i++) // rows
						for (int j = 0; j < hh; j++) // cols
							C[i, j] = f[l, 1 + 1][i, j] + f[l, 1 + 4][i, j] - f[l, 1 + 5][i, j] + f[l, 1 + 7][i, j];

					// C12
					for (int i = 0; i < hh; i++) // rows
						for (int j = hh; j < sz; j++) // cols
							C[i, j] = f[l, 1 + 3][i, j - hh] + f[l, 1 + 5][i, j - hh];

					// C21
					for (int i = hh; i < sz; i++) // rows
						for (int j = 0; j < hh; j++) // cols
							C[i, j] = f[l, 1 + 2][i - hh, j] + f[l, 1 + 4][i - hh, j];

					// C22
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

				R = new Matrix(lhs.m_rows, rhs.m_cols); // result

				// C11
				for (int i = 0; i < Math.Min(h, R.m_rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.m_cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i, j] + field[0, 1 + 4][i, j] - field[0, 1 + 5][i, j] + field[0, 1 + 7][i, j];

				// C12
				for (int i = 0; i < Math.Min(h, R.m_rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.m_cols); j++) // cols
						R[i, j] = field[0, 1 + 3][i, j - h] + field[0, 1 + 5][i, j - h];

				// C21
				for (int i = h; i < Math.Min(2 * h, R.m_rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.m_cols); j++) // cols
						R[i, j] = field[0, 1 + 2][i - h, j] + field[0, 1 + 4][i - h, j];

				// C22
				for (int i = h; i < Math.Min(2 * h, R.m_rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.m_cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i - h, j - h] - field[0, 1 + 2][i - h, j - h] + field[0, 1 + 3][i - h, j - h] + field[0, 1 + 6][i - h, j - h];
			}
			return R;
		}

		/// <summary>Matrix times scalar</summary>
		public static Matrix operator * (double s, Matrix mat)
		{
			var r = new Matrix(mat.m_rows, mat.m_cols);
			for (int i = 0; i < mat.m_rows; i++)
				for (int j = 0; j < mat.m_cols; j++)
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
			return Array_.Equal(lhs.m_data, rhs.m_data);
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
			return new { m_rows, m_cols, m_data }.GetHashCode();
		}
		#endregion
	}

	/// <summary>The LU decomposition of a square matrix</summary>
	[DebuggerDisplay("m{rows}x{cols}) {m_data}")]
	public struct MatrixLU
	{
		private Matrix[] m_mat;
		public MatrixLU(Matrix m)
		{
			m_mat = new Matrix[1];
			L = new LProxy(m_mat);
			U = new UProxy(m_mat);
			pi = new int[m.m_rows];
			DetOfP = 1;

			if (!m.IsSquare)
				throw new Exception("LU decomposition is only possible on square matrices");

			// We will store both the L and U matrices in 'mat' since we know
			// L has the form: [1 0] and U has the form: [U U]
			//                 [L 1]                     [0 U]
			var LL = Matrix.Identity(m.m_rows, m.m_cols);
			var UU = new Matrix(m);

			// Initialise the permutation vector
			for (int i = 0; i < m.m_rows; i++)
				pi[i] = i;

			// Decompose 'm' into 'LL' and 'UU'
			for (int k = 0, k0 = 0, kend = m.m_cols - 1; k != kend; k++)
			{
				// Find the row with the biggest pivot
				var p = 0.0;
				for (int i = k; i != m.m_rows; ++i)
				{
					if (Math.Abs(UU[i, k]) <= p) continue;
					p = Math.Abs(UU[i, k]);
					k0 = i;
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
					var tmp = LL[k, i];
					LL[k, i] = LL[k0, i];
					LL[k0, i] = tmp;
				}
				if (k != k0)
					DetOfP *= -1;

				// Switch rows in U
				for (int i = 0; i != m.m_cols; ++i)
				{
					var tmp = UU[k, i];
					UU[k, i] = UU[k0, i];
					UU[k0, i] = tmp;
				}

				// Gaussian eliminate the remaining rows
				for (int i = k + 1; i < m.m_rows; ++i)
				{
					LL[i,k] = UU[i,k] / UU[k,k];
					for (int j = k; j < m.m_cols; ++j)
						UU[i,j] = UU[i,j] - LL[i,k] * UU[k,j];
				}
			}

			// Store 'LL' in the zero part of 'UU'
			for (int r = 1; r < m.m_rows; ++r)
				for (int c = 0; c != r; ++c)
					UU[r,c] = LL[r,c];

			// Store in m_mat
			m_mat[0] = UU;
		}

		/// <summary>Row count</summary>
		public int m_rows
		{
			get { return m_mat[0].m_rows; }
		}

		/// <summary>Column count</summary>
		public int m_cols
		{
			get { return m_mat[0].m_cols; }
		}

		/// <summary>Accesser for the lower diagonal matrix</summary>
		public LProxy L;
		public struct LProxy
		{
			private Matrix[] m_mat;
			internal  LProxy(Matrix[] m)
			{
				m_mat = m;
			}
			public double this[int row, int col]
			{
				get
				{
					Debug.Assert(row >= 0 && row < m_mat[0].m_rows);
					Debug.Assert(col >= 0 && col < m_mat[0].m_cols);
					return row > col ? m_mat[0][row, col] : row == col ? 1 : 0;
				}
			}
		};

		/// <summary>Accesser for the upper diagonal matrix</summary>
		public UProxy U;
		public struct UProxy
		{
			private Matrix[] m_mat;
			internal UProxy(Matrix[] m)
			{
				m_mat = m;
			}
			public double this[int row, int col]
			{
				get
				{
					Debug.Assert(row >= 0 && row < m_mat[0].m_rows);
					Debug.Assert(col >= 0 && col < m_mat[0].m_cols);
					return row <= col ? m_mat[0][row, col] : 0;
				}
			}
		};

		/// <summary>The determinant of the permutation matrix</summary>
		public double DetOfP;

		/// <summary>The permutation row indices</summary>
		public int[] pi;

		#region Functions

		/// <summary>Permutation matrix "P" due to permutation vector "pi"</summary>
		private Matrix PermutationMatrix
		{
			get
			{
				var m = new Matrix(m_rows, m_cols);
				for (int i = 0; i < m_rows; i++)
					m[pi[i], i] = 1;

				return m;
			}
		}

		/// <summary>Return the determinant of this matrix</summary>
		public static double Determinant(MatrixLU lu)
		{
			var det = lu.DetOfP;
			for (int i = 0; i != lu.m_rows; ++i)
				det *= lu.U[i,i];

			return det;
		}

		/// <summary>True if 'mat' has an inverse</summary>
		public static bool IsInvertable(MatrixLU lu)
		{
			return !Maths.FEql(Determinant(lu), 0);
		}

		/// <summary>Solves for x in 'Ax = v'</summary>
		public static Matrix Solve(MatrixLU A, Matrix v)
		{
			if (A.m_rows != v.m_rows)
				throw new Exception("Solution vector has the wrong dimensions");

			var a = new Matrix(A.m_rows, 1);
			var b = new Matrix(A.m_rows, 1);

			// Switch items in "v" due to permutation matrix
			for (int i = 0; i != A.m_rows; ++i)
				a[i,0] = v[A.pi[i], 0];

			// Solve for x in 'Lx = b' assuming 'L' is a lower triangular matrix
			{
				for (int i = 0; i != A.m_rows; ++i)
				{
					b[i,0] = a[i,0];
					for (int j = 0; j != i; ++j)
						b[i,0] -= A.L[i,j] * b[j,0];

					b[i,0] = b[i,0] / A.L[i,i];
				}
			}

			a = b;

			// Solve for x in 'Ux = b' assuming 'U' is an upper triangular matrix
			{
				for (int i = A.m_rows; i-- != 0;)
				{
					b[i,0] = a[i,0];
					for (int j = A.m_rows - 1; j > i; j--)
						b[i,0] -= A.U[i,j] * b[j,0];

					b[i,0] = b[i,0] / A.U[i,i];
				}
			}

			return b;
		}

		/// <summary>Return the inverse of matrix 'm'</summary>
		public static Matrix Invert(MatrixLU lu)
		{
			Debug.Assert(IsInvertable(lu), "Matrix has no inverse");

			var inv = new Matrix(lu.m_rows, lu.m_cols);
			var elem = new Matrix(lu.m_rows, 1);
			for (int i = 0; i != lu.m_rows; ++i)
			{
				elem[i,0] = 1;
				inv.col[i] = Solve(lu, elem);
				elem[i,0] = 0;
			}
			return inv;
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
			var m = new Matrix(4,4);
			var M = m4x4.Random4x4(-5, +5, rng);
			for (int i = 0; i != 4; ++i)
				for (int j = 0; j != 4; ++j)
					m[i,j] = M[i,j];

			Assert.True(Matrix.FEql(m, M));

			Assert.True(Maths.FEql(M.w.x, m[3,0]));
			Assert.True(Maths.FEql(M.x.w, m[0,3]));
			Assert.True(Maths.FEql(M.z.z, m[2,2]));

			Assert.AreEqual(Matrix.IsInvertable(m), m4x4.IsInvertable(M));

			var m1 = Matrix.Invert(m);
			var M1 = m4x4.Invert(M);
			Assert.True(Matrix.FEql(m1, M1));

			var m2 = Matrix.Transpose(m);
			var M2 = m4x4.Transpose4x4(M);
			Assert.True(Matrix.FEql(m2, M2));
		}
		[Test] public void MultiplyRoundTrip()
		{
			var rng = new Random(1);

			const int SZ = 100;
			var m = new Matrix(SZ,SZ);
			for (int k = 0; k != 10; ++k)
			{
				for (int i = 0; i != m.m_rows; ++i)
					for (int j = 0; j != m.m_cols; ++j)
						m[i,j] = rng.Double(-5.0, +5.0);

				if (Matrix.IsInvertable(m))
				{
					var m_inv = Matrix.Invert(m);

					var i0 = Matrix.Identity(SZ,SZ);
					var i1 = m * m_inv;
					var i2 = m_inv * m;

					Assert.True(Matrix.FEql(i0, i1));
					Assert.True(Matrix.FEql(i0, i2));
					break;
				}
			}
		}
	}
}
#endif
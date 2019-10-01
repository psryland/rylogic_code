//***************************************************
// Matrix
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Maths
{
	/// <summary>A dynamic NxM matrix</summary>
	[DebuggerDisplay("{Description,nq}")]
	public struct Matrix
	{
		// Data stored as contiguous columns,
		// e.g. [xx xy xz xw][yx yy yz yw][...]
		private double[] m_data;

		public Matrix(int row_count, int col_count)
		{
			Rows = row_count;
			Cols = col_count;
			m_data = new double[Rows * Cols];
		}
		public Matrix(int row_count, int col_count, IEnumerable<double> data)
			:this(row_count, col_count)
		{
			var k = 0;
			foreach (var d in data)
			{
				if (k == m_data.Length)
					throw new Exception("Excess data when initialising Matrix");

				m_data[k++] = d;
			}
			if (k != m_data.Length)
				throw new Exception("Insufficient data when initialising Matrix");
		}
		public Matrix(int row_count, int col_count, IEnumerable<float> data)
			:this(row_count, col_count, data.Select(x => (double)x))
		{}
		public Matrix(bool row, IList<double> data)
			:this(row ? 1 : data.Count, row ? data.Count : 1)
		{
			data.CopyTo(m_data, 0);
		}
		//public Matrix(IList<IList<double>> data)
		//	:this(data.Count, data.Max(x => x.Count))
		//{
		//	for (var j = 0; j != Rows; ++j)
		//		data[j].CopyTo(m_data, j * Cols);
		//}
		public Matrix(Matrix rhs)
			:this(rhs.Rows, rhs.Cols)
		{
			Array.Copy(rhs.m_data, m_data, rhs.m_data.Length);
		}

		/// <summary>The number of rows in this matrix</summary>
		public int Rows { get; private set; }

		/// <summary>The number of columns in this matrix</summary>
		public int Cols { get; private set; }

		/// <summary>Access this matrix as a 2D array</summary>
		public double this[int row, int col]
		{
			get
			{
				Util.Assert(row >= 0 && row < Rows);
				Util.Assert(col >= 0 && col < Cols);
				return m_data[col * Rows + row];
			}
			set
			{
				Util.Assert(row >= 0 && row < Rows);
				Util.Assert(col >= 0 && col < Cols);
				m_data[col * Rows + row] = value;
			}
		}

		/// <summary>Access this matrix by row</summary>
		public RowProxy Row => new RowProxy(this);
		public class RowProxy
		{
			private Matrix mat;
			public RowProxy(Matrix m) { mat = m; }
			public Matrix this[int k]
			{
				get
				{
					var m = new Matrix(1, mat.Cols);
					for (int c = 0; c != mat.Cols; ++c)
						m[0, c] = mat[k, c];

					return m;
				}
				set
				{
					if (value.Cols != mat.Cols) throw new Exception("Incorrect number of columns");
					for (int c = 0; c != mat.Cols; ++c)
						mat[k, c] = value[0, c];
				}
			}
		}

		/// <summary>Access this matrix by column</summary>
		public ColProxy Col => new ColProxy(this);
		public class ColProxy
		{
			private Matrix mat;
			public ColProxy(Matrix m) { mat = m; }
			public Matrix this[int k]
			{
				get
				{
					var m = new Matrix(mat.Rows, 1);
					for (int r = 0; r != mat.Rows; ++r)
						m[r, 0] = mat[r, k];

					return m;
				}
				set
				{
					if (value.Rows != mat.Rows) throw new Exception("Incorrect number of rows");
					for (int r = 0; r != mat.Rows; ++r)
						mat[r, k] = value[r, 0];
				}
			}
		}

		/// <summary>A pretty string description of the matrix</summary>
		public string Description
		{
			get
			{
				var s = new StringBuilder();
				s.AppendLine($"[{Rows}x{Cols}]");
				for (int r = 0; r != Rows; ++r)
				{
					for (int c = 0; c != Cols; ++c)
						s.Append($"{this[r,c],5:0.00} ");

					s.AppendLine();
				}
				return s.ToString();
			}
		}

		/// <summary>ToString</summary>
		public override string ToString()
		{
			return Description;
		}

		/// <summary>Return an identity matrix of the given dimensions</summary>
		public static Matrix Identity(int row_count, int col_count)
		{
			var m = new Matrix(row_count, col_count);
			for (int i = 0, iend = Math.Min(row_count, col_count); i != iend; ++i)
				m[i, i] = 1;

			return m;
		}

		/// <summary>Return a row vector from a list of values</summary>
		public static Matrix AsRow(params double[] values)
		{
			return new Matrix(true, values);
		}

		/// <summary>Return a column vector from a list of values</summary>
		public static Matrix AsCol(params double[] values)
		{
			return new Matrix(false, values);
		}

		#region Functions

		/// <summary>Special case equality operators</summary>
		public static bool FEql(Matrix lhs, Matrix rhs)
		{
			if (lhs.Rows != rhs.Rows) return false;
			if (lhs.Cols != rhs.Cols) return false;
			for (int i = 0; i != lhs.m_data.Length; ++i)
				if (!Math_.FEql(lhs.m_data[i], rhs.m_data[i]))
					return false;

			return true;
		}
		public static bool FEql(Matrix lhs, m4x4 rhs)
		{
			if (lhs.Rows != 4 && lhs.Cols != 4) return false;
			return
				Math_.FEql((float)lhs.m_data[ 0], rhs.x.x) &&
				Math_.FEql((float)lhs.m_data[ 1], rhs.x.y) &&
				Math_.FEql((float)lhs.m_data[ 2], rhs.x.z) &&
				Math_.FEql((float)lhs.m_data[ 3], rhs.x.w) &&

				Math_.FEql((float)lhs.m_data[ 4], rhs.y.x) &&
				Math_.FEql((float)lhs.m_data[ 5], rhs.y.y) &&
				Math_.FEql((float)lhs.m_data[ 6], rhs.y.z) &&
				Math_.FEql((float)lhs.m_data[ 7], rhs.y.w) &&

				Math_.FEql((float)lhs.m_data[ 8], rhs.z.x) &&
				Math_.FEql((float)lhs.m_data[ 9], rhs.z.y) &&
				Math_.FEql((float)lhs.m_data[10], rhs.z.z) &&
				Math_.FEql((float)lhs.m_data[11], rhs.z.w) &&

				Math_.FEql((float)lhs.m_data[12], rhs.w.x) &&
				Math_.FEql((float)lhs.m_data[13], rhs.w.y) &&
				Math_.FEql((float)lhs.m_data[14], rhs.w.z) &&
				Math_.FEql((float)lhs.m_data[15], rhs.w.w);
		}

		/// <summary>True if the matrix is square</summary>
		public bool IsSquare
		{
			get { return Rows == Cols; }
		}

		/// <summary>Find the transpose of matrix 'm'</summary>
		public static Matrix Transpose(Matrix m)
		{
			var t = new Matrix(m.Cols, m.Rows);
			for (int r = 0; r < m.Rows; r++)
				for (int c = 0; c < m.Cols; c++)
					t[c, r] = m[r, c];
			return t;
		}

		/// <summary>Return the determinant of this matrix</summary>
		public static double Determinant(Matrix m)
		{
			return MatrixLU.Determinant(new MatrixLU(m));
		}

		/// <summary>True if 'mat' has an inverse</summary>
		public static bool IsInvertible(Matrix m)
		{
			return MatrixLU.IsInvertible(new MatrixLU(m));
		}

		/// <summary>Solves for x in 'A.x = v'</summary>
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
			if (pow ==  0) return Identity(m.Rows, m.Cols);
			if (pow == -1) return Invert(m);

			var x = pow < 0 ? Invert(m) : m;
			if (pow < 0) pow *= -1;

			var ret = Identity(m.Rows, m.Cols);
			while (pow != 0)
			{
				if ((pow & 1) == 1) ret *= x;
				x *= x;
				pow >>= 1;
			}
			return ret;
		}

		/// <summary>Generates the random matrix</summary>
		public static Matrix Random(int row_count, int col_count, double min, double max, Random rng)
		{
			var m = new Matrix(row_count, col_count);
			for (int r = 0; r != row_count; ++r)
				for (int c = 0; c != col_count; ++c)
					m[r,c] = rng.Double(min, max);

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
			for (int r = 0; r < rows.Length; r++)
			{
				nums = rows[r].Split(' ');
				for (int c = 0; c < nums.Length; c++)
					m[r,c] = double.Parse(nums[c]);
			}
			return m;
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
			if (lhs.Rows != rhs.Rows || lhs.Cols != rhs.Cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.Rows, lhs.Cols);
			for (int i = 0; i != r.Rows; ++i)
				for (int j = 0; j != r.Cols; ++j)
					r[i,j] = lhs[i,j] + rhs[i,j];

			return r;
		}
		public static Matrix operator - (Matrix lhs, Matrix rhs)
		{
			if (lhs.Rows != rhs.Rows || lhs.Cols != rhs.Cols)
				throw new Exception("Matrices must have the same dimensions!");

			var r = new Matrix(lhs.Rows, lhs.Cols);
			for (int i = 0; i != r.Rows; ++i)
				for (int j = 0; j != r.Cols; ++j)
					r[i,j] = lhs[i,j] - rhs[i,j];

			return r;
		}

		/// <summary>Matrix product</summary>
		public static Matrix operator * (Matrix lhs, Matrix rhs)
		{
			if (lhs.Cols != rhs.Rows)
				throw new Exception("Wrong dimension of matrix!");

			Matrix R;
			int msize = Math.Max(Math.Max(lhs.Rows, lhs.Cols), Math.Max(rhs.Rows, rhs.Cols));
			if (msize < 32)
			{
				// Small matrix multiply
				R = new Matrix(lhs.Rows, rhs.Cols);
				for (int i = 0; i < R.Rows; i++)
					for (int j = 0; j < R.Cols; j++)
						for (int k = 0; k < lhs.Cols; k++)
							R[i, j] += lhs[i, k] * rhs[k, j];
			}
			else
			{
				// "Strassen Multiply"
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
				static void SafeAplusBintoC(Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.Cols && ya + i < A.Rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.Cols && yb + i < B.Rows) C[i, j] += B[yb + i, xb + j];
						}
					}
				}
				static void SafeAminusBintoC(Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.Cols && ya + i < A.Rows) C[i, j] += A[ya + i, xa + j];
							if (xb + j < B.Cols && yb + i < B.Rows) C[i, j] -= B[yb + i, xb + j];
						}
					}
				}
				static void SafeACopytoC(Matrix A, int xa, int ya, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
					{
						for (int j = 0; j < sz; j++) // cols
						{
							C[i, j] = 0;
							if (xa + j < A.Cols && ya + i < A.Rows) C[i, j] += A[ya + i, xa + j];
						}
					}
				}
				static void AplusBintoC(Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j] + B[yb + i, xb + j];
				}
				static void AminusBintoC(Matrix A, int xa, int ya, Matrix B, int xb, int yb, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j] - B[yb + i, xb + j];
				}
				static void ACopytoC(Matrix A, int xa, int ya, Matrix C, int sz)
				{
					for (int i = 0; i < sz; i++) // rows
						for (int j = 0; j < sz; j++)
							C[i, j] = A[ya + i, xa + j];
				}
				static void StrassenMultiplyRun(Matrix A, Matrix B, Matrix C, int l, Matrix[,] f)
				{
					// A * B into C, level of recursion, matrix field
					// function for square matrix 2^N x 2^N
					var sz = A.Rows;
					if (sz < 32)
					{
						for (int i = 0; i < C.Rows; i++)
						{
							for (int j = 0; j < C.Cols; j++)
							{
								C[i, j] = 0;
								for (int k = 0; k < A.Cols; k++)
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
				}
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

				R = new Matrix(lhs.Rows, rhs.Cols); // result

				// C11
				for (int i = 0; i < Math.Min(h, R.Rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.Cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i, j] + field[0, 1 + 4][i, j] - field[0, 1 + 5][i, j] + field[0, 1 + 7][i, j];

				// C12
				for (int i = 0; i < Math.Min(h, R.Rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.Cols); j++) // cols
						R[i, j] = field[0, 1 + 3][i, j - h] + field[0, 1 + 5][i, j - h];

				// C21
				for (int i = h; i < Math.Min(2 * h, R.Rows); i++) // rows
					for (int j = 0; j < Math.Min(h, R.Cols); j++) // cols
						R[i, j] = field[0, 1 + 2][i - h, j] + field[0, 1 + 4][i - h, j];

				// C22
				for (int i = h; i < Math.Min(2 * h, R.Rows); i++) // rows
					for (int j = h; j < Math.Min(2 * h, R.Cols); j++) // cols
						R[i, j] = field[0, 1 + 1][i - h, j - h] - field[0, 1 + 2][i - h, j - h] + field[0, 1 + 3][i - h, j - h] + field[0, 1 + 6][i - h, j - h];
			}
			return R;
		}

		/// <summary>Matrix times scalar</summary>
		public static Matrix operator * (double s, Matrix mat)
		{
			var r = new Matrix(mat.Rows, mat.Cols);
			for (int i = 0; i < mat.Rows; i++)
				for (int j = 0; j < mat.Cols; j++)
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
			return new { Rows, Cols, m_data }.GetHashCode();
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
			pi = new int[m.Rows];
			DetOfP = 1;

			if (!m.IsSquare)
				throw new Exception("LU decomposition is only possible on square matrices");

			// We will store both the L and U matrices in 'mat' since we know
			// L has the form: [1 0] and U has the form: [U U]
			//                 [L 1]                     [0 U]
			var LL = Matrix.Identity(m.Rows, m.Cols);
			var UU = new Matrix(m);

			// Initialise the permutation vector
			for (int i = 0; i < m.Rows; i++)
				pi[i] = i;

			// Decompose 'm' into 'LL' and 'UU'
			for (int k = 0, k0 = 0, kend = m.Cols - 1; k != kend; k++)
			{
				// Find the row with the biggest pivot
				var p = 0.0;
				for (int i = k; i != m.Rows; ++i)
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
				for (int i = 0; i != m.Cols; ++i)
				{
					var tmp = UU[k, i];
					UU[k, i] = UU[k0, i];
					UU[k0, i] = tmp;
				}

				// Gaussian eliminate the remaining rows
				for (int i = k + 1; i < m.Rows; ++i)
				{
					LL[i,k] = UU[i,k] / UU[k,k];
					for (int j = k; j < m.Cols; ++j)
						UU[i,j] = UU[i,j] - LL[i,k] * UU[k,j];
				}
			}

			// Store 'LL' in the zero part of 'UU'
			for (int r = 1; r < m.Rows; ++r)
				for (int c = 0; c != r; ++c)
					UU[r,c] = LL[r,c];

			// Store in m_mat
			m_mat[0] = UU;
		}

		/// <summary>Row count</summary>
		public int m_rows
		{
			get { return m_mat[0].Rows; }
		}

		/// <summary>Column count</summary>
		public int m_cols
		{
			get { return m_mat[0].Cols; }
		}

		/// <summary>Accessor for the lower diagonal matrix</summary>
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
					Util.Assert(row >= 0 && row < m_mat[0].Rows);
					Util.Assert(col >= 0 && col < m_mat[0].Cols);
					return row > col ? m_mat[0][row, col] : row == col ? 1 : 0;
				}
			}
		};

		/// <summary>Accessor for the upper diagonal matrix</summary>
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
					Util.Assert(row >= 0 && row < m_mat[0].Rows);
					Util.Assert(col >= 0 && col < m_mat[0].Cols);
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
		public static bool IsInvertible(MatrixLU lu)
		{
			return !Math_.FEql(Determinant(lu), 0);
		}

		/// <summary>Solves for x in 'A.x = v'</summary>
		public static Matrix Solve(MatrixLU A, Matrix v)
		{
			if (A.m_rows != v.Rows)
				throw new Exception("Solution vector has the wrong dimensions");

			var a = new Matrix(A.m_rows, 1);
			var b = new Matrix(A.m_rows, 1);

			// Switch items in "v" due to permutation matrix
			for (int i = 0; i != A.m_rows; ++i)
				a[i,0] = v[A.pi[i], 0];

			// Solve for x in 'L.x = b' assuming 'L' is a lower triangular matrix
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

			// Solve for x in 'U.x = b' assuming 'U' is an upper triangular matrix
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
			Util.Assert(IsInvertible(lu), "Matrix has no inverse");

			var inv = new Matrix(lu.m_rows, lu.m_cols);
			var elem = new Matrix(lu.m_rows, 1);
			for (int i = 0; i != lu.m_rows; ++i)
			{
				elem[i,0] = 1;
				inv.Col[i] = Solve(lu, elem);
				elem[i,0] = 0;
			}
			return inv;
		}

		#endregion
	}
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Maths;

	[TestFixture] public class UnitTestMatrix
	{
		[Test] public void Basic()
		{
			var rng = new Random(1);

			// Compare with m4x4
			var m = new Matrix(4,4);
			var M = m4x4.Random4x4(-5, +5, rng);
			for (int c = 0; c != 4; ++c)
				for (int r = 0; r != 4; ++r)
					m[r,c] = M[c][r];

			Assert.True(Matrix.FEql(m, M));

			Assert.True(Math_.FEql(M.w.x, m[0,3]));
			Assert.True(Math_.FEql(M.x.w, m[3,0]));
			Assert.True(Math_.FEql(M.z.z, m[2,2]));

			Assert.Equal(Matrix.IsInvertible(m), Math_.IsInvertible(M));

			var m1 = Matrix.Invert(m);
			var M1 = Math_.Invert(M);
			Assert.True(Matrix.FEql(m1, M1));

			var m2 = Matrix.Transpose(m);
			var M2 = Math_.Transpose(M);
			Assert.True(Matrix.FEql(m2, M2));
		}
		[Test] public void MultiplyRoundTrip()
		{
			var rng = new Random(1);

			const int SZ = 100;
			var m = new Matrix(SZ,SZ);
			for (int k = 0; k != 10; ++k)
			{
				for (int i = 0; i != m.Rows; ++i)
					for (int j = 0; j != m.Cols; ++j)
						m[i,j] = rng.Double(-5.0, +5.0);

				if (Matrix.IsInvertible(m))
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
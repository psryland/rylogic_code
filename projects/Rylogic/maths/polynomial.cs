using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pr.maths
{
	/// <summary>Function with first and second derivative</summary>
	public interface IPolynomial
	{
		/// <summary>Evaluate F(x) at 'x'</summary>
		double F(double x);

		/// <summary>Evaluate dF(x)/dx at 'x'</summary>
		double dF(double x);

		/// <summary>Evaluate d²F(x)/dx at 'x'</summary>
		double ddF(double x);

		/// <summary>Return the real roots of the polynomial</summary>
		double[] Roots { get; }

		/// <summary>Return the X values of the maxima, minima, or inflection points</summary>
		double[] StationaryPoints { get; }
	}

	/// <summary>'F(x) = Ax + B. a.k.a. Linear</summary>
	public class Monic :IPolynomial
	{
		public double A;
		public double B;

		public Monic(double a, double b)
		{
			A = a;
			B = b;
		}

		/// <summary>Evaluate F(x) at 'x'</summary>
		public double F(double x)
		{
			return A*x + B;
		}

		/// <summary>Evaluate dF(x)/dx at 'x'</summary>
		public double dF(double x)
		{
			return A;
		}

		/// <summary>Evaluate d²F(x)/dx at 'x'</summary>
		public double ddF(double x)
		{
			return 0;
		}

		/// <summary>Calculate the real roots of this polynomial</summary>
		public double[] Roots
		{
			get
			{
				if (A == 0) return new double[0];
				return new double[1] { -B / A };
			}
		}

		/// <summary>Return the X values of the maxima, minima, or inflection points</summary>
		public double[] StationaryPoints
		{
			get { return new double[0] {}; }
		}

		/// <summary>Returns a linear approximation of a curve defined by evaluating F(x), dF(x)/dx at 'x'</summary>
		public static Monic FromDerivatives(double x, double y, double dy)
		{
			//' y  = Ax + B
			//' y' = A

			// A = dy
			var A = dy;

			// Ax + B = y
			var B = y - A*x;

			return new Monic(A,B);
		}

		/// <summary>Create a Monic from 2 points</summary>
		public static Monic FromPoints(v2 a, v2 b)
		{
			var A = (b.y - a.y) / (b.x - a.x);
			var B = a.y - A * a.x;
			return new Monic(A, B);
		}
		public static Monic FromPoints(double x0, double y0, double x1, double y1)
		{
			return FromPoints(
				new v2((float)x0, (float)y0),
				new v2((float)x1, (float)y1));
		}
	}

	/// <summary>'F(x) = Ax² + Bx + C'</summary>
	public class Quadratic :IPolynomial
	{
		public double A;
		public double B;
		public double C;

		public Quadratic(double a, double b, double c)
		{
			A = a;
			B = b;
			C = c;
		}

		/// <summary>Evaluate F(x) at 'x'</summary>
		public double F(double x)
		{
			return (A*x + B)*x + C;
		}

		/// <summary>Evaluate dF(x)/dx at 'x'</summary>
		public double dF(double x)
		{
			return 2*A*x + B;
		}

		/// <summary>Evaluate d²F(x)/dx at 'x'</summary>
		public double ddF(double x)
		{
			return 2*A;
		}

		/// <summary>Calculate the real roots of this polynomial</summary>
		public double[] Roots
		{
			get
			{
				// This method is numerically more stable than (-b +/- sqrt(b^2-4ac)) / 2a
				// (see numerical recipes, p184)
				var discriminant = B * B - 4.0 * A * C;
				if (discriminant < 0)
					return new double[0]; // No real roots

				discriminant = Math.Sqrt(discriminant);
				discriminant = B < 0
					? -0.5 * (B - discriminant)
					: -0.5 * (B + discriminant);

				return new double[2]
				{
					discriminant / A,
					C / discriminant
				};
			}
		}

		/// <summary>Return the X values of the maxima, minima, or inflection points</summary>
		public double[] StationaryPoints
		{
			get { return new double[1] { -B / (2*A) }; }
		}

		/// <summary>Returns a quadratic approximation of a curve defined by evaluating F(x), dF(x)/dx, and d²F(x)/dx at 'x'</summary>
		public static Quadratic FromDerivatives(double x, double y, double dy, double ddy)
		{
			//' y  = Ax² + Bx + C
			//' y' = 2Ax + B
			//' y" = 2A

			// 2A = ddy
			var A = ddy/2;

			// 2Ax + B = dy
			var B = dy - 2*A*x;

			// Ax² + Bx + C = y
			var C = y - (A*x - B)*x;

			return new Quadratic(A,B,C);
		}

		/// <summary>Create a quadratic from 3 points</summary>
		public static Quadratic FromPoints(v2 a, v2 b, v2 c)
		{
			//' Aa.x2 + Ba.x + C = a.y
			//' Ab.x2 + Bb.x + C = a.y
			//' Ac.x2 + Bc.x + C = a.y
			//' => Ax = y
			//' A = |a.x² a.x 1| x = |A| y = |a.y|
			//'     |b.x² b.x 1|     |B|     |b.y|
			//'     |c.x² c.x 1|     |C|     |c.y|
			var M = m3x4.Transpose(new m3x4(
				new v4(a.x*a.x, a.x, 1, 0),
				new v4(b.x*b.x, b.x, 1, 0),
				new v4(c.x*c.x, c.x, 1, 0)));

			var y = new v4(a.y, b.y, c.y, 0);
			var x = m3x4.Invert(M) * y;

			return new Quadratic(x.x, x.y, x.z);
		}
		public static Quadratic FromPoints(double x0, double y0, double x1, double y1, double x2, double y2)
		{
			return FromPoints(
				new v2((float)x0, (float)y0),
				new v2((float)x1, (float)y1),
				new v2((float)x2, (float)y2));
		}
	}

	/// <summary>'F(x) = Ax³ + Bx² + Cx + D'</summary>
	public class Cubic :IPolynomial
	{
		public double A;
		public double B;
		public double C;
		public double D;

		public Cubic(double a, double b, double c, double d)
		{
			A = a;
			B = b;
			C = c;
			D = d;
		}

		/// <summary>Evaluate F(x) at 'x'</summary>
		public double F(double x)
		{
			return ((A*x + B)*x + C)*x + D;
		}

		/// <summary>Evaluate dF(x)/dx at 'x'</summary>
		public double dF(double x)
		{
			return (3*A*x + 2*B)*x + C;
		}

		/// <summary>Evaluate d²F(x)/dx at 'x'</summary>
		public double ddF(double x)
		{
			return 6*A*x + 2*B;
		}

		/// <summary>Calculate the real roots of this polynomial</summary>
		public double[] Roots
		{
			get
			{
				// See http://www2.hawaii.edu/suremath/jrootsCubic.html for method
				var a0 = D / A;
				var a1 = C / A;
				var a2 = B / A;

				var q = (a1 / 3) - (a2 * a2 / 9);
				var r = ((a1 * a2 - 3 * a0) / 6) - (a2 * a2 * a2 / 27);
				var temp = (q * q * q) + (r * r);

				if (temp >= 0)
				{
					temp = Math.Sqrt(temp);
					var real_s1 = r + temp;
					var real_s2 = r - temp;

					if (Math.Abs(real_s1) > 0)
					{
						real_s1 = Maths.CubeRoot(Math.Abs(real_s1));
						if (real_s1 < 0)
							real_s1 = -real_s1;
					}
					if (Math.Abs(real_s2) > 0)
					{
						real_s2 = Maths.CubeRoot(Math.Abs(real_s2));
						if (real_s2 < 0)
							real_s2 = -real_s2;
					}
					return new double[1] { real_s1 + real_s2 - a2 / 3 };
				}
				else
				{
					temp = Math.Abs(temp);
					var imaginary_s1 = Math.Sqrt(temp);
					var imaginary_s2 = -imaginary_s1;
					var real_s1 = r;
					var real_s2 = r;

					// cube root of s1 and s2
					// note: magnitude of s1 and s2 are equal
					var magnitude = Maths.CubeRoot(Math.Sqrt(imaginary_s1 * imaginary_s1 + real_s1 * real_s2));

					var theta = Math.Atan2(imaginary_s1, real_s1) / 3;
					real_s1 = magnitude * Math.Cos(theta);
					imaginary_s1 = magnitude * Math.Sin(theta);

					theta = Math.Atan2(imaginary_s2, real_s2) / 3;
					real_s2 = magnitude * Math.Cos(theta);
					imaginary_s2 = magnitude * Math.Sin(theta);

					const double Root3By2 = 0.86602540378443864676;
					return new double[3]
					{
						real_s1 + real_s2 - a2 / 3.0f,
						((real_s1 + real_s2)/-2 - a2/3 + (imaginary_s2 - imaginary_s1) * Root3By2),
						((real_s1 + real_s2)/-2 - a2/3 - (imaginary_s2 - imaginary_s1) * Root3By2)
					};
				}
			}
		}

		/// <summary>Return the X values of the maxima, minima, or inflection points</summary>
		public double[] StationaryPoints
		{
			get
			{
				// dF(x) = 3Ax² + 2Bx + C
				// dF(x) == 0 at the roots of dF(x)
				return new Quadratic(3*A, 2*B, C).Roots;
			}
		}

		// Create a cubic from 4 points
		public static Cubic FromPoints(v2 a, v2 b, v2 c, v2 d)
		{
			//' Aa.x³ + Ba.x² + Ca.x + D = a.y
			//' Ab.x³ + Bb.x² + Cb.x + D = a.y
			//' Ac.x³ + Bc.x² + Cc.x + D = a.y
			//' Ad.x³ + Bd.x² + Cd.x + D = a.y
			//' => Ax = y
			//' A = |a.x² a.x 1| x = |A| y = |a.y|
			//'     |b.x² b.x 1|     |B|     |b.y|
			//'     |c.x² c.x 1|     |C|     |c.y|
			var M = m4x4.Transpose4x4(new m4x4(
				new v4(a.x*a.x*a.x, a.x*a.x, a.x, 1),
				new v4(b.x*b.x*b.x, b.x*b.x, b.x, 1),
				new v4(c.x*c.x*c.x, c.x*c.x, c.x, 1),
				new v4(d.x*d.x*d.x, d.x*d.x, d.x, 1)));

			var y = new v4(a.y, b.y, c.y, d.y);
			var x = m4x4.Invert(M) * y;

			return new Cubic(x.x, x.y, x.z, x.w);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using maths;

	[TestFixture] public class TestPolynomial
	{
		[Test] public void FromPoints()
		{
			var a = new v2(0.5f, 0.3f);
			var b = new v2(0.7f, -0.2f);
			var c = new v2(1.0f, 0.6f);

			var q = Quadratic.FromPoints(a,b,c);
			Assert.AreEqual(q.F(a.x), a.y, Maths.TinyF);
			Assert.AreEqual(q.F(b.x), b.y, Maths.TinyF);
			Assert.AreEqual(q.F(c.x), c.y, Maths.TinyF);
		}
	}
}
#endif
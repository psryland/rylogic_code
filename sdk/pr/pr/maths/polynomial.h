//*****************************************************************************
// Maths library
//  Copyright © Rylogic Ltd 2002
//*****************************************************************************

#pragma once
#ifndef PR_MATHS_POLYNOMIAL_H
#define PR_MATHS_POLYNOMIAL_H

#include "pr/maths/forward.h"
#include "pr/maths/scalar.h"

namespace pr
{
	namespace polynomial
	{
		enum { MaxRoots = 4 };
		struct Roots
		{
			uint  m_num_roots;
			float m_root[MaxRoots];
		};

		struct Quadratic
		{
			float m_a;
			float m_b;
			float m_c;
		};

		struct Cubic
		{
			float m_a;
			float m_b;
			float m_c;
			float m_d;
		};

		struct Quartic
		{
			float m_a;
			float m_b;
			float m_c;
			float m_d;
			float m_e;
		};

		//*****
		// Evaluate a polynomial
		inline float Eval_F(const Quadratic& quadratic, float x)	{ return ((quadratic.m_a * x) + quadratic.m_b) * x + quadratic.m_c; }
		inline float Eval_F(const Cubic& cubic, float x)			{ return (((cubic.m_a * x) + cubic.m_b) * x + cubic.m_c) * x + cubic.m_d; }
		inline float Eval_F(const Quartic& quartic, float x)		{ return ((((quartic.m_a * x) + quartic.m_b) * x + quartic.m_c) * x + quartic.m_d) * x + quartic.m_e; }

		//*****
		// Evaluate the derivative of a polynomial
		inline float Eval_dF(const Quadratic& quadratic, float x)	{ return (2.0f * quadratic.m_a * x) + quadratic.m_b; }
		inline float Eval_dF(const Cubic& cubic, float x)			{ return ((3.0f * cubic.m_a * x) + 2.0f * cubic.m_b) * x + cubic.m_c; }
		inline float Eval_dF(const Quartic& quartic, float x)		{ return (((4.0f * quartic.m_a * x) + 3.0f * quartic.m_b) * x + 2.0f * quartic.m_c) * x + quartic.m_d; }

		namespace impl
		{
			//*****
			// Calculate the real roots of a polynomial
			// This method is numerically more stable than (-b +/- sqrt(b^2-4ac)) / 2a
			// see numerical recipes, p184
			template <typename T> Roots FindRoots(const Quadratic& quadratic)
			{
				float discriminant = quadratic.m_b * quadratic.m_b - 4.0f * quadratic.m_a * quadratic.m_c;
				if( discriminant < 0.0f ) { Roots r = {0}; return r; }

				discriminant = Sqrt(discriminant);
				if( quadratic.m_b < 0.0f )	{ discriminant = -0.5f * (quadratic.m_b - discriminant); }
				else						{ discriminant = -0.5f * (quadratic.m_b + discriminant); }
				
				Roots r = {2, discriminant / quadratic.m_a, quadratic.m_c / discriminant};
				return r;
			}

			// See http://www2.hawaii.edu/suremath/jrootsCubic.html for method
			template <typename T> Roots FindRoots(const Cubic& cubic)
			{
				float a0 = cubic.m_d / cubic.m_a;
				float a1 = cubic.m_c / cubic.m_a;
				float a2 = cubic.m_b / cubic.m_a;

				float q = (a1 / 3.0f) - (a2 * a2 / 9.0f);
				float r = ((a1 * a2 - 3.0f * a0) / 6.0f) - (a2 * a2 * a2 / 27.0f);
				float temp = (q * q * q) + (r * r);

				if( temp >= 0.0f )
				{
					temp = Sqrt(temp);
					float real_s1 = r + temp;
					float real_s2 = r - temp;

					if( Abs(real_s1) > 0.0f )
					{
						real_s1 = Cubert(Abs(real_s1));
						if( real_s1 < 0.0f ) real_s1 = -real_s1;
					}
					if( Abs(real_s2) > 0.0f )
					{
						real_s2 = Cubert(Abs(real_s2));
						if( real_s2 < 0.0f ) real_s2 = -real_s2;
					}
					Roots r = {1, real_s1 + real_s2 - a2 / 3.0f};
					return r;
				}

				temp = Abs(temp);
				float imaginary_s1 = Sqrt(temp);
				float imaginary_s2 = -imaginary_s1;
				float real_s1 = r;
				float real_s2 = r;

				// cube root of s1 and s2

				//n.b. magnitude of s1 and s2 are equal
				float magnitude = Cubert(Sqrt(imaginary_s1 * imaginary_s1 + real_s1 * real_s2));

				float theta;
				theta		 = ATan2(imaginary_s1, real_s1) / 3.0f;
				real_s1		 = magnitude * Cos(theta);
				imaginary_s1 = magnitude * Sin(theta);

				theta		 = ATan2(imaginary_s2, real_s2) / 3.0f;
				real_s2		 = magnitude * Cos(theta);
				imaginary_s2 = magnitude * Sin(theta);

				const float root3_ovr_2 = 0.866025f;
				Roots roots =
				{
					3,
					real_s1 + real_s2 - a2 / 3.0f,
					((real_s1 + real_s2) / -2.0f - a2 / 3.0f + (imaginary_s2 - imaginary_s1) * root3_ovr_2),
					((real_s1 + real_s2) / -2.0f - a2 / 3.0f - (imaginary_s2 - imaginary_s1) * root3_ovr_2)
				};
				return roots;
			}

			// See http://forum.swarthmore.edu/dr.math/problems/cowan2.5.27.98.html
			template <typename T> Roots FindRoots(const Quartic& quartic)
			{
				// Calculate depressed equation (x^4 coefft. = 1, x^3 coefft. = 0)
				// by substituting x = y - b / 4a
				// See http://www.sosmath.com/algebra/factor/fac12/fac12.html
				Quartic depressed_eqn = 
				{
					1.0f,
					0.0f,
					(quartic.m_c -(quartic.m_b * quartic.m_b * 3.0f / (8.0f * quartic.m_a))) / quartic.m_a,
					((quartic.m_d + (quartic.m_b * quartic.m_b * quartic.m_b / (8.0f * quartic.m_a * quartic.m_a))) - (quartic.m_b * quartic.m_c / (2.0f * quartic.m_a))) / quartic.m_a,
					(((quartic.m_e - (quartic.m_b * quartic.m_b * quartic.m_b * quartic.m_b * 3.0f / (256.0f * quartic.m_a * quartic.m_a * quartic.m_a))) + (quartic.m_b * quartic.m_b * quartic.m_c / (16.0f * quartic.m_a * quartic.m_a))) - (quartic.m_b * quartic.m_d / (4.0f * quartic.m_a))) / quartic.m_a
				};

				//calculate coeffs. of resolvent cubic eqn.
				Cubic res_cubic =
				{
                    1.0f,
					2.0f * depressed_eqn.m_c,
					depressed_eqn.m_c * depressed_eqn.m_c - 4.0f * depressed_eqn.m_e,
					-depressed_eqn.m_d * depressed_eqn.m_d
				};

				Roots res_cubic_roots = FindRoots<T>(res_cubic);
				if( res_cubic_roots.m_num_roots == 0 )		{ Roots roots = {0}; return roots; }

				// Find a positive root
				int n = res_cubic_roots.m_num_roots;
				while( res_cubic_roots.m_root[--n] < 0.0f )
				{
					if( n == 0 )							{ Roots roots = {0}; return roots; }
				}
				float h = Sqrt(res_cubic_roots.m_root[n]);
				float j = (depressed_eqn.m_c + res_cubic_roots.m_root[n] - depressed_eqn.m_d / h) / 2.0f;

				Roots roots = {0};
				if( h * h - 4.0f * j >= 0.0f )
				{
					Quadratic quad = {1.0f, h, j};
					Roots quad_roots = FindRoots<T>(quad);
					roots.m_num_roots += quad_roots.m_num_roots;
					roots.m_root[roots.m_num_roots - 1] = quad_roots.m_root[0] - (quartic.m_b / (quartic.m_a * 4.0f));
					roots.m_root[roots.m_num_roots - 2] = quad_roots.m_root[1] - (quartic.m_b / (quartic.m_a * 4.0f));
				}

				h = -h;
				j = depressed_eqn.m_e / j;
				if( h * h - 4.0f * j >= 0.0f )
				{
					Quadratic quad = {1.0f, h, j};
					Roots quad_roots = FindRoots<T>(quad);
					roots.m_num_roots += quad_roots.m_num_roots;
					roots.m_root[roots.m_num_roots - 1] = quad_roots.m_root[0] - (quartic.m_b / (quartic.m_a * 4.0f));
					roots.m_root[roots.m_num_roots - 2] = quad_roots.m_root[1] - (quartic.m_b / (quartic.m_a * 4.0f));
				}
				return roots;
			}
		}//namespace impl

		inline Roots FindRoots(const Quadratic& quadratic)		{ return impl::FindRoots<void>(quadratic); }
		inline Roots FindRoots(const Cubic& cubic)				{ return impl::FindRoots<void>(cubic); }
		inline Roots FindRoots(const Quartic& quartic)			{ return impl::FindRoots<void>(quartic); }
	}//namespace polynomial
}//namespace pr

#endif//PR_MATHS_POLYNOMIAL_H





	
////   bezout
////   This code is based on MgcIntr2DElpElp.cpp written by David Eberly.  His
////  code along with many other excellent examples are avaiable at his site:
////   http://www.magic-software.com
//Intersection.bezout
// = function(e1, e2) {
//    var AB    = e1[0]*e2[1] - e2[0]*e1[1];
//    var AC    = e1[0]*e2[2] - e2[0]*e1[2];
//    var AD    = e1[0]*e2[3] - e2[0]*e1[3];
//    var AE    = e1[0]*e2[4] - e2[0]*e1[4];
//    var AF    = e1[0]*e2[5] - e2[0]*e1[5];
//    var BC    = e1[1]*e2[2] - e2[1]*e1[2];
//    var BE    = e1[1]*e2[4] - e2[1]*e1[4];
//    var BF    = e1[1]*e2[5] - e2[1]*e1[5];
//    var CD    = e1[2]*e2[3] - e2[2]*e1[3];
//    var DE    = e1[3]*e2[4] - e2[3]*e1[4];
//    var DF    = e1[3]*e2[5] - e2[3]*e1[5];
//    var BFpDE = BF + DE;
//    var BEmCD = BE - CD;
//
//    return new Polynomial(
//        AB*BC - AC*AC,
//        AB*BEmCD + AD*BC - 2*AC*AE,
//        AB*BFpDE + AD*BEmCD - AE*AE - 2*AC*AF,
//        AB*DF + AD*BFpDE - 2*AE*AF,
//        AD*DF - AF*AF
//    );
//};


//   intersectEllipseEllipse
//   This code is based on MgcIntr2DElpElp.cpp written by David Eberly.  His
//   code along with many other excellent examples are avaiable at his site:
//   http://www.magic-software.com
//   NOTE: Rotation will need to be added to this function
//Intersection.intersectEllipseEllipse = function(c1, rx1, ry1, c2, rx2, ry2)
//{
//    var a =
//	[
//        ry1*ry1, 0, rx1*rx1, -2*ry1*ry1*c1.x, -2*rx1*rx1*c1.y,
//        ry1*ry1*c1.x*c1.x + rx1*rx1*c1.y*c1.y - rx1*rx1*ry1*ry1
//    ];
//    var b =
//	[
//        ry2*ry2, 0, rx2*rx2, -2*ry2*ry2*c2.x, -2*rx2*rx2*c2.y,
//        ry2*ry2*c2.x*c2.x + rx2*rx2*c2.y*c2.y - rx2*rx2*ry2*ry2
//    ];
//
//    var yPoly   = Intersection.bezout(a, b);
//    var yRoots  = yPoly.getRoots();
//    var epsilon = 1e-3;
//    var norm0   = ( a[0]*a[0] + 2*a[1]*a[1] + a[2]*a[2] ) * epsilon;
//    var norm1   = ( b[0]*b[0] + 2*b[1]*b[1] + b[2]*b[2] ) * epsilon;
//    var result  = new Intersection("No Intersection");
//
//    for ( var y = 0; y < yRoots.length; y++ )
//	{
//        var xPoly = new Polynomial(
//            a[0],
//            a[3] + yRoots[y] * a[1],
//            a[5] + yRoots[y] * (a[4] + yRoots[y]*a[2])
//        );
//        var xRoots = xPoly.getRoots();
//
//        for ( var x = 0; x < xRoots.length; x++ ) {
//            var test =
//                ( a[0]*xRoots[x] + a[1]*yRoots[y] + a[3] ) * xRoots[x] + 
//                ( a[2]*yRoots[y] + a[4] ) * yRoots[y] + a[5];
//            if ( Math.abs(test) < norm0 ) {
//                test =
//                    ( b[0]*xRoots[x] + b[1]*yRoots[y] + b[3] ) * xRoots[x] +
//                    ( b[2]*yRoots[y] + b[4] ) * yRoots[y] + b[5];
//                if ( Math.abs(test) < norm1 ) {
//                    result.appendPoint( new Point2D( xRoots[x], yRoots[y] ) );
//                }
//            }
//        }
//    }
//
//    if ( result.points.length > 0 ) result.status = "Intersection";
//
//    return result;
//}



	//// A polynomial of maximum order 'N'
	//// c0 + c1.x + c2.x^2 + c3.x^3 + ... + cN.x^N
	//template <uint N>
	//struct Polynomial
	//{
	//	uint	m_degree;		// The number of coefficients in use is m_degree + 1
	//	float	m_coefs[N+1];

	//	//.TOLERANCE=1e-6;
	//	//.ACCURACY=6;
	//	Polynomial()
	//	:m_degree(0)
	//	,m_coefs()
	//	{}
	//	Polynomial(float const* coefs, uint num_coefs)
	//	{
	//		Set(coefs, num_coefs);
	//	}
	//	void Set(float const* coefs, uint num_coefs)
	//	{
	//		PR_ASSERT(PR_DBG_MATHS, num_coefs <= N, "This object cannot represent a polynomial of this order");
	//		m_degree = num_coefs - 1;
	//		float* in = m_coefs + m_degree;
	//		float const* out = coefs;
	//		while( num_coefs-- ) { *in-- = *out++; }
	//		//for( uint i = m_degree; i >= 0; --i ) this.coefs.push(coefs[i]);
	//	}
	//	float Eval(float x) const
	//	{
	//		float result = 0.0f;
	//		for( uint i = N-1; i >= 0; --i )
	//			result = result*x + m_coefs[i];
	//		return result;
	//	}
	//	Polynomial<N> Multiply(Polynomial<N> const& that)
	//	{
	//		var result=new Polynomial();
	//		for(var i=0;i<=this.getDegree()+that.getDegree();i++)
	//			result.coefs.push(0);

	//		for(var i=0;i<=this.getDegree();i++)
	//			for(var j=0;j<=that.getDegree();j++)
	//				result.coefs[i+j]+=m_coefs[i]*that.coefs[j];
	//		return result;
	//	}
	//	void DivideScalar(float scalar)
	//	{
	//		for( uint i = 0; i != N; ++i )
	//			m_coefs[i] /= scalar;
	//	}
	//	void Simplify()
	//	{
	//		for( var i=this.getDegree(); i>=0; i--)
	//		{
	//			if(Math.abs(m_coefs[i])<=Polynomial.TOLERANCE)
	//				m_coefs.pop();
	//			else
	//				break;
	//		}
	//	};

	//	bisection(min,max)
	//	{
	//		var minValue=this.eval(min);
	//		var maxValue=this.eval(max);
	//		var result;
	//		if(Math.abs(minValue)<=Polynomial.TOLERANCE)
	//			result=min;
	//		else if(Math.abs(maxValue)<=Polynomial.TOLERANCE)
	//			result=max;
	//		else if(minValue*maxValue<=0)
	//		{
	//			var tmp1=Math.log(max-min);
	//			var tmp2=Math.log(10)*Polynomial.ACCURACY;
	//			var iters=Math.ceil((tmp1+tmp2)/Math.log(2));
	//			for(var i=0; i<iters; i++)
	//			{
	//				result=0.5*(min+max);
	//				var value=this.eval(result);
	//				if(Math.abs(value)<=Polynomial.TOLERANCE)
	//				{
	//					break;
	//				}
	//				if(value*minValue<0)
	//				{
	//					max=result;
	//					maxValue=value;
	//				}
	//				else
	//				{
	//					min=result;
	//					minValue=value;
	//				}
	//			}
	//		}
	//		return result;
	//	}

	//	void toString()
	//	{
	//		var coefs=new Array();
	//		var signs=new Array();
	//		for(var i=N-1; i>=0; i--)
	//		{
	//			var value=m_coefs[i];
	//			if(value!=0)
	//			{
	//				var sign=(value<0)?" - ":" + ";
	//				value=Math.abs(value);
	//				if(i>0)
	//					if(value==1)
	//						value="x";
	//					else
	//						value+="x";
	//				if(i>1)
	//					value+="^"+i;
	//				signs.push(sign);
	//				coefs.push(value);
	//			}
	//		}
	//		signs[0]=(signs[0]==" + ")?"":"-";
	//		var result="";
	//		for(var i=0; i<coefs.length; i++)
	//			result+=signs[i]+coefs[i];
	//		return result;
	//	}

	//	uint Degree() const
	//	{
	//		return N-1;
	//	}
	//	Polynomial<N-1> Derivative() const
	//	{
	//		var derivative=new Polynomial();
	//		for(var i=1; i<N; i++)
	//		{
	//			derivative.coefs.push(i*m_coefs[i]);
	//		}
	//		return derivative;
	//	}
	//	Roots()
	//	{
	//		var result;
	//		this.simplify();
	//		switch(this.getDegree())
	//		{
	//		case 0:
	//			result=new Array();
	//			break;
	//		case 1:
	//			result=this.getLinearRoot();
	//			break;
	//		case 2:
	//			result=this.getQuadraticRoots();
	//			break;
	//		case 3:
	//			result=this.getCubicRoots();
	//			break;
	//		case 4:
	//			result=this.getQuarticRoots();
	//			break;
	//		default:
	//			result=new Array();
	//		}
	//		return result;
	//	}
	//	RootsInInterval(min,max)
	//	{
	//		var roots=new Array();
	//		var root;
	//		if(this.getDegree()==1)
	//		{
	//			root=this.bisection(min,max);
	//			if(root!=null)
	//				roots.push(root);
	//		}
	//		else
	//		{
	//			var deriv=this.getDerivative();
	//			var droots=deriv.getRootsInInterval(min,max);if(droots.length>0){root=this.bisection(min,droots[0]);if(root!=null)roots.push(root);for(i=0;i<=droots.length-2;i++){root=this.bisection(droots[i],droots[i+1]);if(root!=null)roots.push(root);}root=this.bisection(droots[droots.length-1],max);if(root!=null)roots.push(root);}else{root=this.bisection(min,max);if(root!=null)roots.push(root);}}return roots;};
	//	float	LinearRoot() const
	//	{
	//		float a = m_coefs[1];
	//		if( a != 0.0f ) result.push(-m_coefs[0]/a);
	//		return result;
	//	}
	//	float*	QuadraticRoots() const
	//	{
	//		var results=new Array();
	//		if(this.getDegree()==2)
	//		{
	//			var a=m_coefs[2];
	//			var b=m_coefs[1]/a;
	//			var c=m_coefs[0]/a;
	//			var d=b*b-4*c;
	//			if(d>0)
	//			{
	//				var e=Math.sqrt(d);
	//				results.push(0.5*(-b+e));
	//				results.push(0.5*(-b-e));
	//			}
	//			else if(d==0)
	//			{
	//				results.push(0.5*-b);
	//			}
	//		}
	//		return results;
	//	}
	//	float* CubicRoots() const
	//	{
	//		var results=new Array();
	//		if(this.getDegree()==3)
	//		{
	//			var c3=m_coefs[3];
	//			var c2=m_coefs[2]/c3;
	//			var c1=m_coefs[1]/c3;
	//			var c0=m_coefs[0]/c3;
	//			var a=(3*c1-c2*c2)/3;
	//			var b=(2*c2*c2*c2-9*c1*c2+27*c0)/27;
	//			var offset=c2/3;
	//			var discrim=b*b/4 + a*a*a/27;
	//			var halfB=b/2;
	//			if(Math.abs(discrim)<=Polynomial.TOLERANCE)disrim=0;
	//			if(discrim>0)
	//			{
	//				var e=Math.sqrt(discrim);
	//				var tmp;
	//				var root;
	//				tmp=-halfB+e;
	//				if(tmp>=0)root=Math.pow(tmp,1/3);
	//				else root=-Math.pow(-tmp,1/3);
	//				tmp=-halfB-e;
	//				if(tmp>=0)root+=Math.pow(tmp,1/3);
	//				else root-=Math.pow(-tmp,1/3);
	//				results.push(root-offset);
	//			}
	//			else if(discrim<0)
	//			{
	//				var distance=Math.sqrt(-a/3);
	//				var angle=Math.atan2(Math.sqrt(-discrim),-halfB)/3;
	//				var cos=Math.cos(angle);
	//				var sin=Math.sin(angle);
	//				var sqrt3=Math.sqrt(3);
	//				results.push(2*distance*cos-offset);
	//				results.push(-distance*(cos+sqrt3*sin)-offset);
	//				results.push(-distance*(cos-sqrt3*sin)-offset);
	//			}
	//			else
	//			{
	//				var tmp;
	//				if(halfB>=0)
	//					tmp=-Math.pow(halfB,1/3);
	//				else
	//					tmp=Math.pow(-halfB,1/3);
	//				results.push(2*tmp-offset);
	//				results.push(-tmp-offset);
	//			}
	//		}
	//		return results;
	//	}
	//	
	//	float* QuarticRoots() const
	//	{
	//		var results=new Array();
	//		if(this.getDegree()==4)
	//		{
	//			var c4=m_coefs[4];
	//			var c3=m_coefs[3]/c4;
	//			var c2=m_coefs[2]/c4;
	//			var c1=m_coefs[1]/c4;
	//			var c0=m_coefs[0]/c4;
	//			var resolveRoots=new Polynomial(1,-c2,c3*c1-4*c0,-c3*c3*c0+4*c2*c0-c1*c1).getCubicRoots();
	//			var y=resolveRoots[0];
	//			var discrim=c3*c3/4-c2+y;
	//			if(Math.abs(discrim)<=Polynomial.TOLERANCE)
	//				discrim=0;
	//			if(discrim>0)
	//			{
	//				var e=Math.sqrt(discrim);
	//				var t1=3*c3*c3/4-e*e-2*c2;
	//				var t2=(4*c3*c2-8*c1-c3*c3*c3)/(4*e);
	//				var plus=t1+t2;
	//				var minus=t1-t2;
	//				if(Math.abs(plus)<=Polynomial.TOLERANCE)plus=0;
	//				if(Math.abs(minus)<=Polynomial.TOLERANCE)minus=0;
	//				if(plus>=0)
	//				{
	//					var f=Math.sqrt(plus);
	//					results.push(-c3/4 + (e+f)/2);
	//					results.push(-c3/4 + (e-f)/2);
	//				}
	//				if(minus>=0)
	//				{
	//					var f=Math.sqrt(minus);
	//					results.push(-c3/4 + (f-e)/2);
	//					results.push(-c3/4 - (f+e)/2);
	//				}
	//			}
	//			else if(discrim<0)
	//			{}
	//			else
	//			{
	//				var t2=y*y-4*c0;
	//				if(t2>=-Polynomial.TOLERANCE)
	//				{
	//					if(t2<0)t2=0;
	//					t2=2*Math.sqrt(t2);
	//					t1=3*c3*c3/4-2*c2;
	//					if(t1+t2>=Polynomial.TOLERANCE)
	//					{
	//						var d=Math.sqrt(t1+t2);
	//						results.push(-c3/4 + d/2);
	//						results.push(-c3/4 - d/2);
	//					}
	//					if(t1-t2>=Polynomial.TOLERANCE)
	//					{
	//						var d=Math.sqrt(t1-t2);
	//						results.push(-c3/4 + d/2);
	//						results.push(-c3/4 - d/2);
	//					}
	//				}
	//			}
	//		}
	//		return results;
	//	}
	//};

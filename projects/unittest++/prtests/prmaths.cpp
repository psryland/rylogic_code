//*************************************************************
// Unit Test for Maths
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"
#include "pr/maths/maths.h"
#include "pr/maths/stat.h"
#include "pr/macros/link.h"

using namespace pr;

SUITE(PRMathsTests)
{
	TEST(AbsMinMaxClamp)
	{
		pr::Rnd rng;
		uint8  b  , b0  = rng.u8()                    , b1  = rng.u8();
		uint   u  , u0  = rng.u32()                   , u1  = rng.u32();
		int    i  , i0  = rng.i32()                   , i1  = rng.i32();
		long   l  , l0  = rng.i32()                   , l1  = rng.i32();
		float  f  , f0  = rng.f32()                   , f1  = rng.f32();
		double d  , d0  = rng.d32()                   , d1  = rng.d32();
		v2     V2 , V20 = Random2(rng, v2Zero, 10.0f) , V21 = Random2(rng, v2Zero, 10.0f);
		v3     V3 , V30 = Random3(rng, v3Zero, 10.0f) , V31 = Random3(rng, v3Zero, 10.0f);
		v4     V4 , V40 = Random4(rng, v4Zero, 10.0f) , V41 = Random4(rng, v4Zero, 10.0f);
		
		CHECK_EQUAL(b0 < b1 ? b0 : b1, Min(b0 ,b1 ));
		CHECK_EQUAL(u0 < u1 ? u0 : u1, Min(u0 ,u1 ));
		CHECK_EQUAL(i0 < i1 ? i0 : i1, Min(i0 ,i1 ));
		CHECK_EQUAL(l0 < l1 ? l0 : l1, Min(l0 ,l1 ));
		CHECK_EQUAL(f0 < f1 ? f0 : f1, Min(f0 ,f1 ));
		CHECK_EQUAL(d0 < d1 ? d0 : d1, Min(d0 ,d1 ));
		
		V2 = Min(V20,V21);
		V3 = Min(V30,V31);
		V4 = Min(V40,V41);
		for (int i = 0; i != 2; ++i) CHECK(V2[i] <= V20[i] && V2[i] <= V21[i]);
		for (int i = 0; i != 3; ++i) CHECK(V3[i] <= V30[i] && V3[i] <= V31[i]);
		for (int i = 0; i != 4; ++i) CHECK(V4[i] <= V40[i] && V4[i] <= V41[i]);
		
		CHECK_EQUAL(b0 < b1 ? b1 : b0, Max(b0 , b1));
		CHECK_EQUAL(u0 < u1 ? u1 : u0, Max(u0 , u1));
		CHECK_EQUAL(i0 < i1 ? i1 : i0, Max(i0 , i1));
		CHECK_EQUAL(l0 < l1 ? l1 : l0, Max(l0 , l1));
		CHECK_EQUAL(f0 < f1 ? f1 : f0, Max(f0 , f1));
		CHECK_EQUAL(d0 < d1 ? d1 : d0, Max(d0 , d1));
		
		V2 = Max(V20,V21);
		V3 = Max(V30,V31);
		V4 = Max(V40,V41);
		for (int i = 0; i != 2; ++i) CHECK(V2[i] >= V20[i] && V2[i] >= V21[i]);
		for (int i = 0; i != 3; ++i) CHECK(V3[i] >= V30[i] && V3[i] >= V31[i]);
		for (int i = 0; i != 4; ++i) CHECK(V4[i] >= V40[i] && V4[i] >= V41[i]);
		
		b  = Clamp(rng.u8()                    ,Min(b0 , b1) ,Max(b0 , b1));
		u  = Clamp(uint(rng.u32())             ,Min(u0 , u1) ,Max(u0 , u1));
		i  = Clamp(int(rng.i32())              ,Min(i0 , i1) ,Max(i0 , i1));
		l  = Clamp(rng.i32()                   ,Min(l0 , l1) ,Max(l0 , l1));
		f  = Clamp(rng.f32()                   ,Min(f0 , f1) ,Max(f0 , f1));
		d  = Clamp(rng.d32()                   ,Min(d0 , d1) ,Max(d0 , d1));
		V2 = Clamp(Random2(rng, v2Zero, 10.0f) ,Min(V20,V21) ,Max(V20,V21));
		V3 = Clamp(Random3(rng, v3Zero, 10.0f) ,Min(V30,V31) ,Max(V30,V31));
		V4 = Clamp(Random4(rng, v4Zero, 10.0f) ,Min(V40,V41) ,Max(V40,V41));
		
		CHECK(Min(b0 , b1) <= b && b <= Max(b0 , b1));
		CHECK(Min(u0 , u1) <= u && u <= Max(u0 , u1));
		CHECK(Min(i0 , i1) <= i && i <= Max(i0 , i1));
		CHECK(Min(l0 , l1) <= l && l <= Max(l0 , l1));
		CHECK(Min(f0 , f1) <= f && f <= Max(f0 , f1));
		CHECK(Min(d0 , d1) <= d && d <= Max(d0 , d1));
		for (int i = 0; i != 2; ++i) CHECK(Min(V20[i],V21[i]) <= V2[i] && V2[i] <= Max(V20[i],V21[i]));
		for (int i = 0; i != 3; ++i) CHECK(Min(V30[i],V31[i]) <= V3[i] && V3[i] <= Max(V30[i],V31[i]));
		for (int i = 0; i != 4; ++i) CHECK(Min(V40[i],V41[i]) <= V4[i] && V4[i] <= Max(V40[i],V41[i]));
	}
	TEST(v4)
	{
		v4 V1 = {1,2,3,4};
		V1 = pr::v4Zero;
		CHECK(IsZero3(V1));
		CHECK(IsZero4(V1));
		CHECK(FEqlZero3(V1));
		CHECK(FEqlZero4(V1));
		
		V1.set(4,2,-5,1);
		CHECK(Length3(V1) != Length4(V1));
		CHECK(!IsNormal3(V1));
		CHECK(!IsNormal4(V1));
		
		V1.w = 0.0f;
		v4 V2 = V1;
		Normalise3(V2);
		CHECK(FEql3(GetNormal3(V1), V2));
		
		V1.w = 1.0f;
		v4 V3 = V1;
		Normalise4(V3);
		CHECK(FEql4(GetNormal4(V1), V3));
		
		V1.set(-2,  4,  2,  6);
		V2.set(3, -5,  2, -4);
		m4x4 a2b = CrossProductMatrix4x4(V1);
		v4 V4 = a2b * V2; V4;
		V3 = Cross3(V1, V2);
		CHECK(FEql3(V4, V3));
	}
	TEST(m4x4Translation)
	{
		m4x4 m2;
		m4x4 m1 = m4x4::make(v4XAxis, v4YAxis, v4ZAxis, v4::make(1.0f, 2.0f, 3.0f, 1.0f));
		Translation(m2, v3::make(1.0f, 2.0f, 3.0f));
		CHECK(FEql(m1, m2));
		Translation(m2, v4::make(1.0f, 2.0f, 3.0f, 1.0f));
		CHECK(FEql(m1, m2));
	}
	TEST(m4x4CreateFrom)
	{
		v4 V1 = Random3(0.0f, 10.0f, 1.0f);
		m4x4 a2b; a2b.set(Random3N(0.0f), rand::f32(-maths::tau_by_2, maths::tau_by_2), Random3(0.0f, 10.0f, 1.0f));
		m4x4 b2c; b2c.set(Random3N(0.0f), rand::f32(-maths::tau_by_2, maths::tau_by_2), Random3(0.0f, 10.0f, 1.0f));
		CHECK(IsOrthonormal(a2b));
		CHECK(IsOrthonormal(b2c));
		v4 V2 = a2b * V1;
		v4 V3 = b2c * V2; V3;
		m4x4 a2c = b2c * a2b;
		v4 V4 = a2c * V1; V4;
		CHECK(FEql4(V3, V4));
	}
	TEST(m4x4CreateFrom2)
	{
		m4x4 m1; Rotation4x4(m1, 1.0f, 0.5f, 0.7f, v4Origin);
		m4x4 m2; m2.set(Quat::make(1.0f, 0.5f, 0.7f), v4Origin);
		CHECK(IsOrthonormal(m1));
		CHECK(IsOrthonormal(m2));
		CHECK(FEql(m1, m2));
		
		float ang = rand::f32(-1.0f, 1.0f);
		v4 axis = Random3N(0.0f);
		m1; Rotation4x4(m1, axis, ang, v4Origin);
		m2; m2.set(Quat::make(axis, ang), v4Origin);
		CHECK(IsOrthonormal(m1));
		CHECK(IsOrthonormal(m2));
		CHECK(FEql(m1, m2));
	}
	TEST(m4x4CreateFrom3)
	{
		m4x4 a2b; a2b.set(Random3N(0.0f), rand::f32(-1.0f, 1.0f), Random3(0.0f, 10.0f, 1.0f));
		a2b = m4x4::make(
				  v4::make(0.58738488f,  0.60045743f,  0.54261398f, 0.0f),
				  v4::make(-0.47383153f,  0.79869330f, -0.37090793f, 0.0f),
				  v4::make(-0.65609658f, -0.03924191f,  0.75365603f, 0.0f),
				  v4::make(0.09264841f,  6.84435890f,  3.09618950f, 1.0f));
				  
		m4x4 b2a;           b2a = GetInverse(a2b);
		m4x4 b2a_2 = a2b;   Inverse(b2a_2);
		CHECK(FEql(b2a, b2a_2));
		
		m4x4 a2a = b2a * a2b; a2a;
		CHECK(FEql(m4x4Identity, a2a));
		
		m4x4 b2a_fast = GetInverseFast(a2b); b2a_fast;
		m4x4 b2a_fast_2 = a2b; InverseFast(b2a_fast_2);
		
		CHECK(FEql(b2a_fast, b2a));
		CHECK(FEql(b2a_fast, b2a_fast_2));
	}
	TEST(m4x4Orthonormalise)
	{
		m4x4 a2b;
		a2b.x.set(-2.0f, 3.0f, 1.0f, 0.0f);
		a2b.y.set(4.0f,-1.0f, 2.0f, 0.0f);
		a2b.z.set(1.0f,-2.0f, 4.0f, 0.0f);
		a2b.w.set(1.0f, 2.0f, 3.0f, 1.0f);
		CHECK(IsOrthonormal(Orthonormalise(a2b)));
	}
	TEST(m4x4GetAxisAngle)
	{
		CHECK(true);
		//float ang = FRand(-1.0f, 1.0f);
		//v4 axis = v4RandomNormal3(0.0f);
		//m4x4 a2b; Rotation(a2b, axis, ang);
		
		//v4 X = v4XAxis;
		//v4 Xprim = a2b * X;
		
		//v4 Y = v4YAxis;
		//v4 Yprim = a2b * Y;
		
		//v4 Z = v4ZAxis;
		//v4 Zprim = a2b * Z;
		
		//v4 XcXp = Cross3(X, Xprim).Normalise3();
		//v4 YcYp = Cross3(Y, Yprim).Normalise3();
		//v4 ZcZp = Cross3(Z, Zprim).Normalise3();
		
		//v4 axis_out = Cross3(X_Xp, Y_Yp);
		//axis_out.Normalise3();
		
		//axis_out = axis_out;
		
		
		//float det4 = a2b.Determinant4(); det4;
		//float det3 = a2b.Determinant3(); det3;
		//CHECK(FEql(det3, det4));
		
		//float angle_out;
		//v4 axis_out;
		//a2b.GetAxisAngle(axis_out, angle_out);
		
		//bool correct = (FEql(ang,  0.0f,      0.001f) && FEql (angle_out, 0.0f, 0.001f)) ||
		//             (FEql(ang,  angle_out, 0.001f) && FEql3(axis,  axis_out, 0.01f)) ||
		//             (FEql(ang, -angle_out, 0.001f) && FEql3(axis, -axis_out, 0.01f));
		//CHECK(correct);
	}
	TEST(QuatConvert)
	{
		for (int i = 0; i != 100; ++i)
		{
			m4x4 a2b = Random4x4(Random3N(0.0f), -pr::maths::tau, pr::maths::tau, pr::v4Origin);
			Quat q   = Quat::make(a2b);
			m4x4 A2B = m4x4::make(q, pr::v4Origin);
			CHECK(FEql(a2b, A2B));
		}
	}
	TEST(QuatRotate)
	{
		Quat a2b = Quat::make(-0.57315874f, -0.57733983f, 0.39024505f, 0.43113413f);
		Quat b2c = Quat::make(-0.28671566f, 0.72167641f, -0.59547395f, 0.20588370f);
		Quat a2c = b2c * a2b;
		CHECK(IsNormal4(a2b));
		CHECK(IsNormal4(b2c));
		CHECK(IsNormal4(a2c));
		v4 V1 = v4::make(-7.8858266f, -0.29560062f, 6.0255852f, 1.0f);
		v4 V2 = Rotate(a2b, V1);
		v4 V3 = Rotate(b2c, V2); V3;
		v4 V4 = Rotate(a2c, V1); V4;
		CHECK(FEql4(V3, V4));
	}
	TEST(QuatMultiply)
	{
		float ang = rand::f32(-1.0f, 1.0f);
		v4 axis = Random3N(0.0f);
		Quat q_a2b; q_a2b.set(axis, ang);
		m4x4 m_a2b; m_a2b.set(axis, ang, v4Origin);
		m4x4 a2b; a2b.set(q_a2b, v4Origin);
		CHECK(FEql(a2b, m_a2b));
		
		v4 Va = Random3(0.1f, 10.0f, 1.0f);
		v4 mVb = m_a2b * Va; mVb;
		v4 qVb = Rotate(q_a2b, Va); qVb;
		CHECK(FEql3(qVb, mVb));
		
		v4 axis1 = Random3N(0.0f), axis2 = Random3N(0.0f);
		float ang1 = rand::f32(-1.0f, 1.0f), ang2 = rand::f32(-1.0f, 1.0f);
		
		m_a2b = Rotation4x4(axis1, ang1, v4Origin);
		m4x4 m_b2c = Rotation4x4(axis2, ang2, v4Origin);
		m4x4 m_a2c = m_b2c * m_a2b;
		
		q_a2b = Quat::make(axis1, ang1);
		Quat q_b2c = Quat::make(axis2, ang2);
		Quat q_a2c = q_b2c * q_a2b;
		
		v4 pos = Random3(0.5f, 10.0f, 1.0f);
		v4 m_pos = m_a2c * pos; m_pos;
		v4 q_pos = Rotate(q_a2c, pos); q_pos;
		CHECK(FEql4(m_pos, q_pos));
	}
	TEST(QuatGetConjugate)
	{
		v4 Va = Random3N(0.0f);
		v4 Vb = Random3N(0.0f);
		Quat q_a2b; q_a2b.set(Va, Vb);
		CHECK(FEql4(GetConjugate(GetConjugate(q_a2b)), q_a2b));
		v4 qVb = Rotate(q_a2b, Va); qVb;
		CHECK(FEql4(qVb, Vb));
	}
	TEST(QuatAxisAngle)
	{
		float ang = rand::f32(-1.0f, 1.0f);
		v4 axis = Random3N(0.0f);
		Quat q; q.set(axis, ang);
		float q_ang;
		v4 q_axis;
		AxisAngle(q, q_axis, q_ang);
		CHECK((FEql4(q_axis, axis, 0.001f) && FEql(q_ang, ang, 0.001f)) ||
			  (FEql4(-q_axis, axis, 0.001f) && FEql(-q_ang, ang, 0.001f)));
	}
	TEST(Stat)
	{
		{
			const double num[] = {2.0,4.0,7.0,3.0,2.0,-5.0,-4.0,1.0,-7.0,3.0,6.0,-8.0};
			const pr::uint count = sizeof(num)/sizeof(num[0]);
			Stat<> s;
			for (pr::uint i = 0; i != count; ++i)
				s.Add(num[i]);
			
			CHECK_EQUAL(count, s.Count());
			CHECK_CLOSE(4.0                               ,s.Sum()       ,pr::maths::dbl_tiny);
			CHECK_CLOSE(-8.0                              ,s.Minimum()   ,pr::maths::dbl_tiny);
			CHECK_CLOSE(7.0                               ,s.Maximum()   ,pr::maths::dbl_tiny);
			CHECK_CLOSE(1.0/3.0                           ,s.Mean()      ,pr::maths::dbl_tiny);
			CHECK_CLOSE(4.83621                           ,s.PopStdDev() ,0.00001);
			CHECK_CLOSE(23.38889                          ,s.PopStdVar() ,0.00001);
			CHECK_CLOSE(5.0512524699475787686684767441111 ,s.SamStdDev() ,pr::maths::dbl_tiny);
			CHECK_CLOSE(25.515151515151515151515151515152 ,s.SamStdVar() ,pr::maths::dbl_tiny);
		}
		{
			struct MinFunc { double operator()(double lhs, double rhs) { return lhs < rhs ? lhs : rhs; } };
			struct MaxFunc { double operator()(double lhs, double rhs) { return lhs < rhs ? rhs : lhs; } };
			const double num[] = {-0.50, 0.06, -0.31, 0.31, 0.09, -0.02, -0.15, 0.40, 0.32, 0.25, -0.33, 0.36, 0.21, 0.01, -0.20, -0.49, -0.41, -0.14, -0.35, -0.33};
			const pr::uint count = sizeof(num)/sizeof(num[0]);
			Stat<> s;
			for (pr::uint i = 0; i != count; ++i)
				s.Add(num[i], MinFunc(), MaxFunc());
				
			CHECK_EQUAL(count, s.Count());
			CHECK_CLOSE(-1.22   ,s.Sum()       ,pr::maths::dbl_tiny);
			CHECK_CLOSE(-0.5    ,s.Minimum()   ,pr::maths::dbl_tiny);
			CHECK_CLOSE(0.4     ,s.Maximum()   ,pr::maths::dbl_tiny);
			CHECK_CLOSE(-0.0610 ,s.Mean()      ,0.00001);
			CHECK_CLOSE(0.29233 ,s.PopStdDev() ,0.00001);
			CHECK_CLOSE(0.08546 ,s.PopStdVar() ,0.00001);
			CHECK_CLOSE(0.29993 ,s.SamStdDev() ,0.00001);
			CHECK_CLOSE(0.08996 ,s.SamStdVar() ,0.00001);
		}
		{
			const pr::v4 num[] = {pr::v4XAxis, pr::v4ZAxis, pr::v4ZAxis, pr::v4Origin};
			const pr::uint count = sizeof(num)/sizeof(num[0]);
			
			Stat<pr::v4> s;
			for (pr::uint i = 0; i != count; ++i)
				s.Add(num[i], pr::Min<v4>, pr::Max<v4>);
				
			//CHECK_EQUAL(count, s.Count());
			//CHECK(FEql(s.Sum()       ,-1.22      ,pr::maths::dbl_tiny));
			//CHECK(FEql(s.Minimum()   ,-0.5       ,pr::maths::dbl_tiny));
			//CHECK(FEql(s.Maximum()   ,0.4        ,pr::maths::dbl_tiny));
			//CHECK(FEql(s.Mean()      ,-0.0610    ,0.00001));
			//CHECK(FEql(s.PopStdDev() ,0.29233    ,0.00001));
			//CHECK(FEql(s.PopStdVar() ,0.08546    ,0.00001));
			//CHECK(FEql(s.SamStdDev() ,0.29993    ,0.00001));
			//CHECK(FEql(s.SamStdVar() ,0.08996    ,0.00001));
		}
	}
	TEST(MovingWindowAvr)
	{
		{// Testing moving window average
			enum { BufSz = 13 };
			
			pr::Rnd rng;
			MovingAvr<BufSz> s;
			double buf[BufSz]; uint in = 0, count = 0;
			for (int i = 0; i != BufSz * 2; ++i)
			{
				double v = rng.d32();
				buf[in] = v;
				count += (count != BufSz);
				in = (in + 1) % BufSz;
				double sum = 0.0f; for (uint j = 0; j != count; ++j) sum += buf[j];
				double mean = sum / count;
				s.Add(v);
				CHECK_CLOSE(mean    ,s.Mean()      ,0.00001);
			}
		}
	}
	TEST(ExpMovingAvr)
	{
		{// Testing exponential moving average
			enum { BufSz = 13 };
			pr::Rnd rng;
			ExpMovingAvr<> s(BufSz);
			double a = 2.0 / (BufSz + 1), ema = 0; int count = 0;
			for (int i = 0; i != BufSz * 2; ++i)
			{
				double v = rng.d32();
				if (count < BufSz) { ++count; ema += (v - ema) / count; }
				else               { ema = a * v + (1 - a) * ema; }
				s.Add(v);
				CHECK_CLOSE(ema ,s.Mean() ,0.00001);
			}
		}
	}
	TEST(Frustum)
	{
		float aspect = 1.4f;
		float fovY = pr::maths::tau / 6.0f;
		pr::Frustum f = pr::Frustum::makeFA(fovY, aspect, 0.0f);
		
		CHECK(FEql(f.Width(), 0.0f));
		CHECK(FEql(f.Height(), 0.0f));
		CHECK(FEql(f.FovY(), fovY));
		CHECK(FEql(f.Aspect(), aspect));
		
		f.ZDist(1.0f);
		CHECK(FEql(f.Width() , 2.0f * pr::Tan(0.5f * fovY) * aspect));
		CHECK(FEql(f.Height(), 2.0f * pr::Tan(0.5f * fovY)));
	}
	TEST(Geometry)
	{
		{
			// Intersect_LineToTriangle
			pr::v4 a = v4::make(-1.0f, -1.0f,  0.0f, 1.0f);
			pr::v4 b = v4::make(1.0f, -1.0f,  0.0f, 1.0f);
			pr::v4 c = v4::make(0.0f,  1.0f,  0.0f, 1.0f);
			pr::v4 s = v4::make(0.0f,  0.0f,  1.0f, 1.0f);
			pr::v4 e = v4::make(0.0f,  0.0f, -1.0f, 1.0f);
			pr::v4 e2= v4::make(0.0f,  1.0f,  1.0f, 1.0f);
			
			float t = 0, f2b = 0; pr::v4 bary = pr::v4Zero;
			CHECK(pr::Intersect_LineToTriangle(s, e, a, b, c, &t, &bary, &f2b));
			CHECK(pr::FEql3(bary, pr::v4::make(0.25f, 0.25f, 0.5f, 0.0f)));
			CHECK(pr::FEql(t, 0.5f));
			CHECK(f2b == 1.0f);
			
			CHECK(pr::Intersect_LineToTriangle(e, s, a, b, c, &t, &bary, &f2b));
			CHECK(pr::FEql3(bary, pr::v4::make(0.25f, 0.25f, 0.5f, 0.0f)));
			CHECK(pr::FEql(t, 0.5f));
			CHECK(f2b == -1.0f);
			
			CHECK(!pr::Intersect_LineToTriangle(s, e, a, b, c, 0, 0, 0, 0.7f, 1.0f));
			CHECK(!pr::Intersect_LineToTriangle(s, e, a, b, c, 0, 0, 0, 0.0f, 0.3f));
			CHECK(!pr::Intersect_LineToTriangle(s, e2, a, b, c));
			
			// near parallel case
			s = v4::make(-1.896277f, 0.602204f, 0.124205f, 1.0f);
			e = v4::make(-1.910564f, -0.397666f, 0.131691f, 1.0f);
			a = v4::make(-2.500000f, 1.000000f, 0.120000f, 1.0f);
			b = v4::make(-1.500000f, 1.250000f, 0.120000f, 1.0f);
			c = v4::make(-2.500000f, 1.250000f, 0.120000f, 1.0f);
			CHECK(!pr::Intersect_LineToTriangle(s, e, a, b, c, 0, 0, 0, -0.1f, 0.1f));
		}
	}
	TEST(Conv)
	{
		{
			std::string s = pr::To<std::string>(6.28f);
			float f = pr::To<float>(s.c_str());
			CHECK_CLOSE(6.28f, f, pr::maths::tiny);
		}
		{
			RECT      r = {1,2,3,4};
			pr::IRect R = pr::To<pr::IRect>(r);
			CHECK_EQUAL(r.left  , R.X());
			CHECK_EQUAL(r.top   , R.Y());
			CHECK_EQUAL(r.right , R.X() + R.SizeX());
			CHECK_EQUAL(r.bottom, R.Y() + R.SizeY());
		}
	}
}

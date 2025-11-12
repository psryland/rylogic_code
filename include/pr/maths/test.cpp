
#include <stdio.h>
#include <conio.h>
#include "pr/common/min_max_fix.h"
#include "pr/common/fmt.h"
#include "pr/macros/link.h"
#include "pr/filesys/fileex.h"
#include "pr/filesys/autofile.h"
#include "pr/maths/maths.h"
#include "pr/maths/primes.h"
#include "pr/maths/largeint.h"
#include "pr/maths/convexhull.h"
#include "pr/linedrawer/ldr_helper.h"
//#include "pr/maths/lcpsolver.h"
#include "pr/maths/stat.h"

using namespace pr;

// Unit test functions
void MathsUnitTest();
void UnitTest_Rand();
void UnitTest_Compression();

inline void Print(const v4& point)
{
	printf("[%6.6f %6.6f %6.6f %6.6f]\n", point.x, point.y, point.z, point.w);
}
inline void Print(const v3& point)
{
	printf("[%6.6f %6.6f %6.6f]\n", point.x, point.y, point.z);
}
inline void Print(const v2& point)
{
	printf("[%6.6f %6.6f]\n", point.x, point.y);
}
inline void PrintBits(unsigned int i)
{
	std::string str; str.reserve(32);
	for( int j = 31; j >= 0; --j )
		str.push_back(((i>>j)&1) ? '1' : '0');
	printf("%s\n", str.c_str());
}

struct DistSqPred
{
	v4 m_centre;
	DistSqPred(const v4& centre) : m_centre(centre) {}
	bool operator() (const v4& lhs, const v4& rhs) const
	{
		return Length3Sq(lhs - m_centre) < Length3Sq(rhs - m_centre);
	}
};

//void ComputeConstraintForces()
//{
//	// Initialise all of the constraint forces to zero
//
//	// Set the constraint accelerations to what they are if no constraint forces are applied
//	// Note: negative accelerations imply a constraint being violated
//	
//	// Initialise two empty sets, one for all the constraints that are not satisfied, and the other for those that are
//
//	// Find a negative constraint acceleration, d
//	{
//		// Adjust the constraint forces so that the negative constraint acceleration becomes zero
//		//DriveToZero(d);
//		do 
//		{
//			
//		}
//		while();
//	}
//}

void Print(float const* A, float const* x, float const* b, int N)
{
	printf("State *****************************\n");
	printf("A = \n");
	for( int i = 0; i != N; ++i )
	{
		for( int j = 0; j != N; ++j )
			printf(" %f", A[N*j + i]);
		printf("\n");
	}
	printf("x = \n");
	for( int j = 0; j != N; ++j )
		printf(" %f\n", x[j]);
	printf("b = \n");
	for( int i = 0; i != N; ++i )
		printf(" %f\n", b[i]);

	printf("\nAx = ");
	for( int i = 0; i != N; ++i )
	{
		float value = 0.0f;
		for( int j = 0; j != N; ++j )
		{
			value += A[N*j+i] * x[j];
		}
		printf("\n%f\t(error: %f)", value, Abs(b[i] - value));
	}
	printf("\n");
}

// Solves for 'x' when 'A' is a symmetric positive semi-definite matrix
void Solve(m4x4 const& A, v4& x, v4 const& b, m4x4& preconditioner, int i_max, float eps)
{
	m4x4 inv_pre = GetInverse(preconditioner);

	v4 residual = b - A * x;
	v4 direction = inv_pre * residual;
	float d_new = Dot4(residual, direction);
	float d0 = d_new;
	for( int i = 0; i < i_max && d_new > Sqr(eps) * d0; ++i )
	{
		v4 q = A * direction;
		float alp = d_new / Dot4(direction, q);
		x = x + alp * direction;

		if( (i % 50) == 49 )	residual = b - A * x;
		else					residual = residual - alp * q;

		v4 s = inv_pre * residual;
		float d_old = d_new;
		d_new = Dot4(residual, s);
		float beta = d_new / d_old;
		direction = s + beta * direction;

		Print(A.x.ToArray(), x.ToArray(), b.ToArray(), 4);
		eps = eps;
	}
}

//// Returns 
//// 'pt0' is the closest point on the line
//// 'pt1' is the closest point on the circle
//void ClosestPoint_LineToCircle(Line3 const& line, v4 const& cir_centre, v4 const& cir_norm, float radius, v4& pt0, v4& pt1)
//{
//	// Renderer the setup
//	StartFile("C:/deleteme/linecircle_scene.pr_script");
//	ldr::Line("Line", "FFFF0000", line.Start(), line.End());
//	m4x4 o2w = m4x4::make(cir_norm, 0.0f, cir_centre);
//	ldr::CylinderHR("Circle", "FF0000FF", o2w, radius, 0.02f);
//	EndFile();
//
//	//// Start at the circle centre.
//	//float radius_sq = Sqr(radius);
//	//pt1 = cir_centre;
//	//for( int i = 0; i != 4; ++i )
//	//{
//	//	float t;
//	//	ClosestPoint_PointToLineSegment(point, line, t);
//	//	pt0 = line.Point(t);
//
//	//	// Move as far as we can toward the closest point on the line
//	//	v4 diff = pt0 - cir_centre;
//	//	v4 step = diff - Dot3(diff, cir_norm) * cir_norm;
//	//	if( step.Length3Sq() < radius_sq )
//	//	pt1 = cir_centre + pt0
//	//}
//}
//
//void TestLineToCircle()
//{
//	Line3 line = Line3::make(v4Random3(1.0f, 3.0f, 1.0f), v4Random3(1.0f, 3.0f, 0.0f));
//	v4 cir_centre = v4Random3(1.0f, 3.0f, 1.0f);
//	v4 cir_norm   = v4RandomNormal3(0.0f);
//	float radius = FRand(0.5f, 2.0f);
//	v4 pt0;
//	v4 pt1;
//	ClosestPoint_LineToCircle(line, cir_centre, cir_norm, radius, pt0, pt1);
//}

template <typename Type, int Size>	Type*	begin	(Type (&arr)[Size])	{ return arr; }
template <typename Type, int Size>	Type*	end		(Type (&arr)[Size])	{ return arr + Size; }
template <typename Type, int Size>	size_t	CountOf	(Type (&)[Size])	{ return Size; }

#include "pr/hardware/cpuinfo.h"
//#include <nmmintrin.h>
//
//static RES CRC_HardwareUnrolled(const BYTE * buf, SIZE_T len)
//{
//	RES crc = CRCINIT;
//
//	// Align to DWORD boundary
//	SIZE_T align = (sizeof(DWORD) - (INT_PTR)buf) & (sizeof(DWORD) - 1);
//	align = Min(align, len);
//	len -= align;
//	for (; align; align--)
//		crc = g_crc_slicing[0][(crc ^ *buf++) & 0xFF] ^ (crc >> 8);
//
//	SIZE_T ndqwords = len / (sizeof(DWORD) * 4);
//	for (; ndqwords; ndqwords--)
//	{
//		crc = _mm_crc32_u32(crc, *(DWORD*)buf);
//		crc = _mm_crc32_u32(crc, *(DWORD*)(buf + sizeof(DWORD) ));
//		crc = _mm_crc32_u32(crc, *(DWORD*)(buf + sizeof(DWORD) * 2 ));
//		crc = _mm_crc32_u32(crc, *(DWORD*)(buf + sizeof(DWORD) * 3 ));
//		buf += sizeof(DWORD) * 4;
//	}
//
//	len &= sizeof(DWORD) * 4 - 1;
//	for (; len; len--)
//		crc = _mm_crc32_u8(crc, *buf++);
//	return ~crc;
//}

int main(void)
{
	pr::CpuInfo info;
	std::string s = info.Report();
	printf("%s", s.c_str());

	//char arr[25] = {0};
	//for (size_t i = 0; i != CountOf(arr); ++i)
	//	arr[i] = (char)i;
	//uint n = pr::Bits("01010110");
	//uint a = pr::LowBit(n);
	//uint b = pr::HighBit(n);
	//uint c = pr::HighBitIndex(n);

	//const F32vec4 Zero(0.0f, 0.0f, 0.0f, 0.0f);
	//const F32vec4 One(1.0f, 1.0f, 1.0f, 1.0f);

	//F32vec4 a(1.0f, 2.0f, 3.0f, 4.0f);
	//F32vec4 b(4.0f, 3.0f, 2.0f, 1.0f);
	//F32vec4 c = a / b;
	//F32vec4 d = select_neq(b, Zero, c, Zero);
	//F32vec4 e = _mm_shuffle_ps(a, a, 1);
	//F32vec4 f = _mm_shuffle_ps(a, a, 2);
	//F32vec4 g = _mm_shuffle_ps(a, a, _MM_SHUFFLE(2,3,0,1));

	//F32vec4 mask  = cmpgt(a, b);
	//F32vec4 total = _mm_add_ps(a, _mm_shuffle_ps(a, a, _MM_SHUFFLE(2,3,0,1)));
	//// total = a.x+a.y, a.x+a.y, a.z+a.w, a.z+a.w

	//float a0 = a[0];
	//F32vec4 m_count(0.0f);
	//F32vec4 m_mean(0.0f);
	//F32vec4 m_var(0.0f);
	//F32vec4 m_min( maths::float_max,  maths::float_max,  maths::float_max,  maths::float_max);
	//F32vec4 m_max(-maths::float_max, -maths::float_max, -maths::float_max, -maths::float_max);

	//FRandom frand;
	//for (int i = 0; i != 10; ++i)
	//{
	//	F32vec4 value(FRand(-1.0f, 1.0f), FRand(-1.0f, 1.0f), FRand(-1.0f, 1.0f), FRand(-1.0f, 1.0f));

	//	m_count = m_count + One;
	//	F32vec4 diff  = value - m_mean;
	//	m_var  = m_var  + (diff * diff * (m_count - One) / m_count);
	//	m_mean = m_mean + (diff / m_count);
	//	m_min = select_lt(m_min, value, m_min, value);
	//	m_max = select_gt(m_max, value, m_max, value);
	//}
	return 0;
}
//int main(void)
//{
//	for( int i = 0; i != 100; ++i )
//	{
//		std::string str;
//		v4 p0 = v4::make(0,0,0,1.0f);//v4Random3(1.0f, 5.0f, 1.0f); //p0.z = 0.0f;
//		v4 p1 = v4::make(0,1,0,1.0f);//v4Random3(1.0f, 5.0f, 1.0f); //p1.z = 0.0f;
//		v4 p2 = v4::make(1,1,0,1.0f);//v4Random3(1.0f, 5.0f, 1.0f); //p2.z = 0.0f;
//		v4 p3 = v4::make(1,0,0,1.0f);//v4Random3(1.0f, 5.0f, 1.0f); //p3.z = 0.0f;
//		ldr::Box("P0", "FFFF0000", p0, 0.1f, str);
//		ldr::Box("P1", "FFFF0000", p1, 0.1f, str);
//		ldr::Box("P2", "FF0000FF", p2, 0.1f, str);
//		ldr::Box("P3", "FF0000FF", p3, 0.1f, str);
//		ldr::Line("Start", "FFFF0000", p0, p1, str);
//		ldr::Line("End", "FF0000FF", p2, p3, str);
//
//		str += "*LineList spline FF00FF00 {\n";
//		Spline spline = Spline::make(p0, p1, p2, p3);
//		for( float t = -0.5f; t <= 1.5f; t += 0.01f )
//		{
//			v4 pos = spline.GetPosition(t);
//			ldr::Vec3(pos, str);
//			str += "\n";
//		}
//		str += "}\n";
//
//		v4 pt = v4Random3(1.0f, 5.0f, 1.0f);
//		
//		float t = spline.ClosestPoint(pt);
//		v4 closest = spline.GetPosition(t);
//		ldr::Line("Closest", "FFFFFF00", pt, closest, str);
//
//		StringToFile(str, "D:/deleteme/spline_test.pr_script");
//	}
//}

//void main(void)
//{
//	//{
//	//	for( int i = 0; i != 100000; ++i )
//	//	{
//	//		BoundingBox a, b;
//	//		a.Reset();
//	//		b.Reset();
//	//		Encompase(a, v4Random3(0.0f, 10.0f, 0.0f));
//	//		Encompase(a, v4Random3(0.0f, 10.0f, 0.0f));
//	//		Encompase(b, v4Random3(0.0f, 10.0f, 0.0f));
//	//		Encompase(b, v4Random3(0.0f, 10.0f, 0.0f));
//	//		v4 al = a.Lower(), au = a.Upper();
//	//		v4 bl = b.Lower(), bu = b.Upper();
//
//	//		bool intersect1 = IsIntersection(a, b);
//
//	//		bool intersect2 = !( al.x > bu.x || bl.x > au.x ||
//	//							 al.y > bu.y || bl.y > au.y ||
//	//							 al.z > bu.z || bl.z > au.z);
//	//		v4 m = al - bu;
//	//		v4 n = bl - au;
//	//		v4 g = Maximum(Maximum(m, n), v4Zero);
//	//		bool intersect3 = Dot3(g,g) == 0.0f;
//	//		//bool intersect3 =	!(m[m.LargestElement3()] > 0.0f ||
//	//		//					  n[n.LargestElement3()] > 0.0f);
//	//		
//	//		pr_assert(intersect1 == intersect2 && intersect1 == intersect3 && "");
//	//	}
//	//	//return !(box1[0] - box2[1] > 0 || box2[0] - box1[1] > 0);
//	//	return;
//	//}
//
//
//
//	v4 vec = v4RandomNormal3(0.0f);
//	ReduceDimension(vec, 1);
//	//TestClosestPointToEllipse();
//	//return;
//
//	// must be symmetric positive definite
//	m4x4 A = m4x4Random(0.0f, 10.0f);
//	A = Sqrt(A * A.GetTranspose());
//	PR_ASSERT(1, A.Determinant4() > 0.0f);
//	v4	 b = v4Random4(0.0f, 10.0f);
//	v4	 x = v4Random4(1.0f, 100.0f);
//	m4x4 pre = m4x4Identity;
//	Print(A.FloatArray(), x.FloatArray(), b.FloatArray(), 4);
//	Solve(A, x, b, pre, 5, 0.001f);
//	Print(A.FloatArray(), x.FloatArray(), b.FloatArray(), 4);
//
//	//// init random values
//	//for( int i = 0; i != N; ++i )
//	//	for( int j = 0; j != N; ++j )
//	//		A[i][j] = FRand(0.0f, 10.0f);
//	//for( int j = 0; j != N; ++j )
//	//	x[j] = 0.0f;
//	//for( int i = 0; i != N; ++i )
//	//	b[i] = FRand(-10.0f, 10.0f);
//	////A[0][0] = 3; A[0][1] = 1; A[0][2] = 2;
//	////A[1][0] = 2; A[1][1] = 5; A[1][2] = 2;
//	////A[2][0] = 1; A[2][1] = 1; A[2][2] = 7;
//	////x[0] = 0;
//	////x[1] = 0;
//	////x[2] = 0;
//	////b[0] = 1;
//	////b[1] = 1;
//	////b[2] = 1;
//
//	//Print();
//
//	//// solve Gauss-Seidel
//	//for( int iter = 0; iter != 100; ++iter )
//	//{
//	//	for( int i = 0; i != N; ++i )
//	//	{
//	//		float sum = 0.0f;
//	//		for( int j = 0; j != i; ++j )
//	//		{
//	//			sum += A[i][j] * x[j];
//	//		}
//	//		for( int j = i + 1; j != N; ++j )
//	//		{
//	//			sum += A[i][j] * x[j];
//	//		}
//
//	//		x[i] = (b[i] - sum) / A[i][i];
//	//	}
//	//	Print();
//	//}
//}

//{
//	StartFile("C:/Deleteme/deleteme.pr_script");EndFile();
//	v4 vec;
//	float delta = maths::tiny;
//	for( float dz = -delta; dz <= delta; dz += delta )
//	for( float dy = -delta; dy <= delta; dy += delta )
//	for( float dx = -delta; dx <= delta; dx += delta )
//	for( float z = -1.0f; z <= 1.0f; z += 1.0f )
//	for( float y = -1.0f; y <= 1.0f; y += 1.0f )
//	for( float x = -1.0f; x <= 1.0f; x += 1.0f )
//	{
//		vec.Set(x + dx, y + dy, z + dz, 0.0f);
//		if( FEqlZero3(vec) ) continue;
//
//		AppendFile("C:/Deleteme/deleteme.pr_script");
//		ldr::g_output->Print("*Line blah FFFF0000 {\n");
//		// Measure the length of the cross product with it's non-parallel
//		float len = Cross3(vec, NotParallel(vec)).Length3();
//		bool worser_found;
//		do
//		{
//			worser_found = false;
//			PR_ASSERT(1, len > maths::tiny);
//					
//			// Try a bunch of deviations from 'vec'
//			v4    worser_v   = vec;
//			float worser_len = len;
//			for( int i = 0; i != 10000; ++i )
//			{
//				m3x3 rot = m3x3Random(v4Random3(1.0f, 1.0f, 0.0f), 0.0f, 0.01f);
//				v4 v = rot * vec;
//				float l = Cross3(v, NotParallel(v)).Length3();
//				if( l < worser_len )
//				{
//					worser_v = v;
//					worser_len = l;
//					worser_found = true;
//				}
//			}
//			vec = worser_v;
//			len = worser_len;
//			ldr::g_output->Print(FmtS("0 0 0  %f %f %f\n", vec.x, vec.y, vec.z));
//		}
//		while( worser_found );
//		ldr::g_output->Print("}\n");
//		EndFile();
//	}
//}

//// Test diagonalise
//void main(void)
//{
//	m3x3 R = m3x3Random(v4RandomNormal3(1.0f), 0.0f, maths::two_pi);
//	m3x3 S = Scale3x3(FRand(-10.0f, 10.0f), FRand(-10.0f, 10.0f), FRand(-10.0f, 10.0f));
//	m3x3 A = R * S;
//	
//	m3x3 Evec;
//	v4 Eval;
//	m3x3 diag = Diagonalise3x3(A, Evec, Eval);
//
//	PHm3 copyA; copyA = A;
//	PHm3 Evec2;
//	PHv4 Eval2;
//	phDiagonalise(&copyA, &Eval2, &Evec2);
//
//	A;
//}

//// Test Primes
//void main(void)
//{
//	for( i = 2; i < 100000; i = PrimeGtrEq(i+1) )
//	{
//		printf("%d ", i);
//		PR_ASSERT(1, IsPrime(i));
//	}
//	for( ; i > 2; i = PrimeLessEq(i-1) )
//	{
//		printf("%d ", i);
//		PR_ASSERT(1, IsPrime(i));
//	}
//}

//void main()
//{
//	{// Test special cases
//		v4 s0    = v4::make(0,0,0,1.0f);
//		v4 line0 = v4::make(1.0f,0,0,0);
//		v4 s1    = v4::make(0,0,0,1.0f);
//		v4 line1 = v4::make(0,0,0,0);
//		
//		float t0, t1;
//		ClosestPoint_InfiniteLineToInfiniteLine(s0, line0, s1, line1, t0, t1);
//	}
//	v4 mn = v4::make(-10.0f, -10.0f, -10.0f, 0.0f);
//	v4 mx = v4::make( 10.0f,  10.0f,  10.0f, 0.0f);
//	for( int i = 0; i != 100; ++i )
//	{
//		v4 s0    = v4Random3(mn, mx, 1.0f);
//		v4 line0 = v4Random3(mn, mx, 1.0f) - s0;
//		v4 s1    = v4Random3(mn, mx, 1.0f);
//		v4 line1 = v4Random3(mn, mx, 1.0f) - s1;
//		StartFile("C:/Deleteme/closestPt_LineLine.pr_script");
//		ldr::LineD("A", "FFFF0000", s0, line0);
//		ldr::LineD("B", "FF0000FF", s1, line1);
//
//		float t0, t1;
//		ClosestPoint_InfiniteLineToInfiniteLine(s0, line0, s1, line1, t0, t1);
//		v4 S = s0 + t0 * line0;
//		v4 E = s1 + t1 * line1;
//
//		ldr::Line("R", "FFFFFF00", S, E);
//		EndFile();
//	}
//}

//void main()
//{
//	for( int i = 0; i != 100; ++i )
//	{
//		srand(i);
//		float near_ = -0.3f;
//		float far_  =  0.4f;
//		v4 dir = v4RandomNormal3(0.0f);
//		// both directions
//		// span both, span one, span the other, span none, left side, right side, random direction
//		v4 e = v4::make(-1.0f, -1.0f, -1.0f, 1.0f);
//		v4 s = v4::make( 1.0f,  1.0f,  1.0f, 1.0f);
//
//		StartFile("C:/DeleteMe/slabtest.pr_script");
//		ldr::Vector("Direction", "FFFFFF00", v4Origin, dir, 0.05f);
//		ldr::Quad("near", "FF00FF00", 2.0f, 2.0f, v4Origin + dir*near_, dir);
//		ldr::Quad("far" , "FF00FF00", 2.0f, 2.0f, v4Origin + dir*far_, dir);
//		ldr::Line("Before", "FFFF0000", s, e);
//		EndFile();
//		
//		ClipToSlab(dir, near_, far_, s, e);
//
//		AppendFile("C:/DeleteMe/slabtest.pr_script");
//		ldr::Line("After" , "FF0000FF", s, e);
//		EndFile();
// 		Sleep(2000);
//	}
//}

//void main()
//{
//	Quat q;
//	for( int i = 0 ; i != 100000; ++i )
//	{
//		q.Random();
//
//		v4 axis;
//		float angle;
//		q.AxisAngle(axis, angle);
//
//		v4 axis2;
//		float angle2;
//		q.AxisAngle2(axis2, angle2);
//
//		v4 axis3;
//		axis3.Set(q.x, q.y, q.z, 0.0f).Normalise3();
//		axis3 = axis3;
//		float angle3 = 2.0f * ACos(q.w);
//
//		PR_ASSERT(1, FEql(angle3, angle, 0.001f));
//		PR_ASSERT(1, FEql(angle3, angle2, 0.001f));
//
//		PR_ASSERT(1, Dot3(axis3, axis) > 0.5f);
//		PR_ASSERT(1, Dot3(axis3, axis2) > 0.5f);
//	}
//}

//void main()
//{
//	AutoFile f("C:/DeleteMe/linetest.pr_script");
//	for( int i = 0; i != 1000; ++i )
//	{
//		v4 norm = v4RandomNormal3(0.0f);
//		uint idx = CompressNormal(norm);
//		if( idx > 13 )
//		{
//			norm = -norm;
//			idx = 26 - idx;
//		}
//		v4 de_norm = DecompressNormal(idx);
//		PR_ASSERT(1, CompressNormal(de_norm) == idx);
//		
//		int x = (idx % 3);
//		int y = (idx % 9) / 3;
//		int z = (idx / 9);
//		uint colour = 0xFF000000 + ((x * 0x7F) << 16);// + ((y * 0x7F) << 8) + (z * 0x7F);
//		ldr::Line("l", Fmt("%X", colour).c_str(), v4Origin, de_norm);
//	}
//}

//BoundingBox XForm(const m4x4& m, const BoundingBox& bb)
//{
//	BoundingBox bbox = BoundingBox::make(m.pos, v4Zero);
//	m4x4 mat = m.GetTranspose3x3();
//	for( int i = 0; i != 3; ++i )
//	{
//		bbox.m_centre[i] += Dot4(    mat[i] , bb.m_centre);
//		bbox.m_radius[i] += Dot4(Abs(mat[i]), bb.m_radius);
//	}
//	return bbox;
//}	
//
//void main(void)
//{
//	BoundingBox bbox1, bbox2;
//	int i = 0;
//	for( i = 0; i != 1000; ++i )
//	{
//		srand(i);
//		bbox1.Set(v4Random3(v4Origin, 5.0f, 1.0f), Abs(v4Random3(0.5f, 3.0f, 0.0f)));
//		//bbox1.Set(v4Origin, Abs(v4Random3(0.5f, 3.0f, 0.0f)));
//		m4x4 mat = m4x4Random(v4Origin, 2.0f);
//		bbox2 = XForm(mat, bbox1);
//
//		StartFile("C:/Deleteme/bbox_test.pr_script");
//		ldr::BoundingBox("bbox1", "FFFF0000", bbox1);
//		ldr::GroupStart("box1_Trans");
//		ldr::Transform(mat);
//		ldr::BoundingBox("box1_Trans", "FFFFFF00", bbox1);
//		ldr::GroupEnd();
//		ldr::BoundingBox("bbox2", "FF0000FF", bbox2);
//		EndFile();
//
//		for( int j = 0; j != 8; ++j )
//		{
//			v4 c = bbox1.GetCorner(j);
//			c = mat * c;
//			AppendFile("C:/Deleteme/bbox_test.pr_script");
//			ldr::Box("pt", "FF00FF00", c, 0.05f);
//			EndFile();
//			
//			PR_ASSERT(1, IsWithin(bbox2, c, maths::tiny));
//		}
//	}
//}

//int main(void)
//{
//	OrientedBox obox1, obox2;
//
//	int i = 0;
//	for( i = 0; i != 1000; ++i )
//	{
//		srand(i);
//		obox1.CreateFrom(v4::make(0.0f, 0.0f, 0.0f, 1.0f), v4::make(1.0f, 1.0f, 1.0f, 0.0f), m3x3Identity);
//		obox2.CreateFrom(v4::make(0.0f, 0.0f, 0.0f, 1.0f), v4::make(1.0f, 1.0f, 1.0f, 0.0f), m3x3Identity);
//		m4x4 a2w = m4x4Random(v4::make(-1.3f, 0.0f, 0.0f, 1.0f), 1.0f);//m4x4Identity;//
//		m4x4 b2w = m4x4Random(v4::make( 1.3f, 0.0f, 0.0f, 1.0f), 1.0f);
//		obox1 = a2w * obox1;
//		obox2 = b2w * obox2;
//
//		v4 axis;
//		float depth;
//		StartFile("C:/DeleteMe/oob.pr_script");
//		ldr::OrientedBox("obox1", "FFFF0000", obox1);
//		ldr::OrientedBox("obox2", "FF0000FF", obox2);
//		ldr::Axis("obox1_origin", a2w);
//		ldr::Axis("obox2_origin", b2w);
//		EndFile();
//		if( IsIntersection(obox1, obox2, axis, depth) )
//		{
//			printf("Collideded\n");
//			int t1, t2;
//			v4 f1[4], f2[4], p1, p2;
//			obox1.SupportFeature( axis, f1, t1);
//			obox2.SupportFeature(-axis, f2, t2);
//	
//			AppendFile("C:/DeleteMe/oob.pr_script");
//			for( int i = 0; i != t1; ++i ) ldr::Sphere("FeatureA", "FFFF0000", f1[i], 0.03f);
//			for( int i = 0; i != t2; ++i ) ldr::Sphere("FeatureB", "FF0000FF", f2[i], 0.03f);
//			switch( t1 << OrientedBox::Bits | t2 )
//			{
//			case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Point:
//			case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Edge:
//			case OrientedBox::Point << OrientedBox::Bits | OrientedBox::Face:
//				ldr::LineD("Axis", "FFFFFF00", f1[0], axis * depth);
//				break;
//			case OrientedBox::Edge << OrientedBox::Bits | OrientedBox::Point:
//			case OrientedBox::Face << OrientedBox::Bits | OrientedBox::Point:
//				ldr::LineD("Axis", "FFFFFF00", f2[0], axis * -depth);
//				break;
//			case OrientedBox::Edge << OrientedBox::Bits | OrientedBox::Edge:
//				ClosestPoint_LineSegmentToLineSegment(f1[0], f1[1], f2[0], f2[1], p1, p2);
//				ldr::LineD("Axis", "FFFFFF00", p1, axis * depth);
//				break;
//			default:
//				PR_ASSERT(1, false);
//			}
//			EndFile();
//		}
//		else
//		{
//			printf("Not Collideded\n");
//		}
//		//Sleep(1000);
//	}
//	getch();
//	return 0;
//}

//int main(void)
//{
//	unsigned int a[] = {0x01234567, 0x89ABCDEF};
//	unsigned int b[] = {0x00000001, 0x23456789};
//	//unsigned int a[] = {0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
//	//					0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
//	//					0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF,
//	//					0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF};
//	//unsigned int a[] = {0x40000000, 0x00000000};
//
//	LargeInt L1, L2;
//	L1.set(a, sizeof(a)/sizeof(a[0]));
//	L2.set(b, sizeof(b)/sizeof(b[0]));
//	L1 %= 0x55555555;
//	return 0;	
//}

void MathsUnitTest()
{
	UnitTest_Rand();
	for( uint i = 0; i < 10000; ++i )
	{
	}
	UnitTest_Compression();
}

void UnitTest_Rand()
{}
//{
//	{
//		float sum = 0.0f;
//		FRandom rand;
//		for( uint i = 0; i < 1000; ++i )
//		{
//			rand.next();
//			sum += rand.value();
//		}
//		float avr = sum / 1000.0f; avr;
//		pr_assert(FEql(avr, 0.5f, 0.01f));
//	}
//	{
//		int sum = 0;
//		IRandom rand;
//		for( uint i = 0; i < 1000; ++i )
//		{
//			rand.next();
//			sum += rand.value();
//		}
//		float avr = sum / 1000.0f; avr;
//		//pr_assert(FEql(avr, 0.5f, 0.01f));
//	}
//}




//*****
// Test the compression functions
void UnitTest_Compression()
{
	#define Bits 8
	#define Type uint8

	{// Q
		v4 pos = v4::make(1.0f, -1.0f, 0.0f, 1.0f);
		v4 dir = Random3N(0.0f);

		Type pack_pos = PackNormV4<Bits, Type>(pos);
		Type pack_dir = PackNormV4<Bits, Type>(dir);

		v4 unpack_pos = UnpackNormV4<Bits>(pack_pos);
		v4 unpack_dir = UnpackNormV4<Bits>(pack_dir);

		v4 pos_diff = Abs(pos - unpack_pos);
		v4 dir_diff = Abs(dir - unpack_dir);

		Print(pos_diff);
		Print(dir_diff);
	}
	{// v4
		v4 pos = v4::make(1.0f, -1.0f, 0.0f, 1.0f);
		v4 dir = Random3N(0.0f);

		Type pack_pos = PackNormV4<Bits, Type>(pos);
		Type pack_dir = PackNormV4<Bits, Type>(dir);

		v4 unpack_pos = UnpackNormV4<Bits>(pack_pos);
		v4 unpack_dir = UnpackNormV4<Bits>(pack_dir);

		v4 pos_diff = Abs(pos - unpack_pos);
		v4 dir_diff = Abs(dir - unpack_dir);

		Print(pos_diff);
		Print(dir_diff);
	}
	{// v2
		v2 pos = v2::make(1.0f, -1.0f);
		v2 dir = Random2N();

		Type pack_pos = PackNormV2<Bits, Type>(pos);
		Type pack_dir = PackNormV2<Bits, Type>(dir);

		v2 unpack_pos = UnpackNormV2<Bits>(pack_pos);
		v2 unpack_dir = UnpackNormV2<Bits>(pack_dir);

		v2 pos_diff = Abs(pos - unpack_pos);
		v2 dir_diff = Abs(dir - unpack_dir);

		Print(pos_diff);
		Print(dir_diff);
	}
}




















//{
//		enum
//		{
//			MaxMaterialBits			= 6,
//			MaxFlagsBits			= 8 - MaxMaterialBits,
//			MaxMaterials			= 1 << MaxMaterialBits,
//			MaxFlags				= 1 << MaxFlagsBits,
//			MaterialsMask			= MaxMaterials - 1,
//			FlagsMask				= MaxFlags - 1,
//			NormalComponentBits		= 19,
//			NormalComponentMask		= (1 << NormalComponentBits) - 1,
//			NormalComponentSignMask	= 1 << NormalComponentBits,
//			DistanceComponentBits	= 16
//		};
//
//		#define Terrain_NormalComponentScale		(float)(NormalComponentMask)
//		#define Terrain_DistanceComponentScale		(float)((1 << DistanceComponentBits) / (2 * 768))
//
//		ri::uint32_t	GetMaterialReference() const				{ return m_mat_and_flags & MaterialsMask; }
//		void			SetMaterialReference(ri::uint32_t mat_ref)	{ RI_ASSERT((mat_ref & MaterialsMask) == mat_ref);	m_mat_and_flags = static_cast<ri::uint8_t>((m_mat_and_flags & ~MaterialsMask) | (mat_ref & MaterialsMask)); }
//		ri::uint32_t	GetSurfaceFlags() const						{ return (m_mat_and_flags & FlagsMask) >> MaxMaterialBits; }
//		void			SetSurfaceFlags(ri::uint32_t flags)			{ RI_ASSERT((flags & FlagsMask) == flags);			m_mat_and_flags = static_cast<ri::uint8_t>((m_mat_and_flags &  MaterialsMask) | ((flags & FlagsMask) << MaxMaterialBits)); }
//
//		// Return the height at x,z on this plane.
//		// 'x' and 'z' must be in region-relative co-ordinates.
//		MAreal GetHeightAt(MAreal x, MAreal z) const
//		{
//			MAv4 plane_equation = GetPlaneEquation();
//			return (plane_equation[3] - plane_equation[0]*x - plane_equation[2]*z) / plane_equation[1];
//		}
//
//		MAv4 GetNormal() const
//		{
//			MAv4 plane_equation = GetPlaneEquation(); plane_equation[3] = 0.0f;
//			return plane_equation;
//		}
//
//		MAv4 GetPlaneEquation() const
//		{
//			MAv4 plane_equation;
//			ri::uint32_t irootx = (((m_plane_rootzx & 0x07)     ) << 16) | m_plane_rootx;
//			ri::uint32_t irootz = (((m_plane_rootzx & 0x70) >> 4) << 16) | m_plane_rootz;
//			float rootx = (irootx & NormalComponentMask) / Terrain_NormalComponentScale;
//			float rootz = (irootz & NormalComponentMask) / Terrain_NormalComponentScale;
//			plane_equation[0] = rootx*rootx;
//			plane_equation[2] = rootz*rootz;
//			plane_equation[1] = maSqrt(1.0f - plane_equation[0]*plane_equation[0] - plane_equation[2]*plane_equation[2]);
//			plane_equation[3] = m_plane_w / Terrain_DistanceComponentScale;				
//			if( m_plane_rootzx & 0x08 ) plane_equation[0] = -plane_equation[0];
//			if( m_plane_rootzx & 0x80 ) plane_equation[2] = -plane_equation[2];
//			return plane_equation;
//		}
//
//		void SetPlaneEquation(MAv4 plane_equation)
//		{
//			m_plane_w				= static_cast<ri::int16_t> (plane_equation[3] * Terrain_DistanceComponentScale);
//			ri::uint32_t rootx		= static_cast<ri::uint32_t>(maSqrt(maAbs(plane_equation[0])) * Terrain_NormalComponentScale);
//			ri::uint32_t rootz		= static_cast<ri::uint32_t>(maSqrt(maAbs(plane_equation[2])) * Terrain_NormalComponentScale);
//			m_plane_rootx			= static_cast<ri::uint16_t>(rootx & 0xFFFF);
//			m_plane_rootz			= static_cast<ri::uint16_t>(rootz & 0xFFFF);
//			m_plane_rootzx			= static_cast<ri::uint8_t> ( ((rootz >> 16) << 4) | (rootx >> 16) );
//			if( plane_equation[0] < 0.0f ) m_plane_rootzx |= 0x08;
//			if( plane_equation[2] < 0.0f ) m_plane_rootzx |= 0x80;
//		}
//
//		// 20bits or x/z
//		ri::uint16_t	m_plane_rootx;
//		ri::uint16_t	m_plane_rootz;
//		ri::int16_t		m_plane_w;
//		ri::uint8_t		m_plane_rootzx;
//		ri::uint8_t		m_mat_and_flags;
//	}
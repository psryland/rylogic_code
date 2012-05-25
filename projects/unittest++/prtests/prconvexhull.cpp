//*************************************************************
// Unit Test for Maths
//*************************************************************
#include "unittest++/1.3/src/unittest++.h"

#define PR_MATHS_USE_PR_ASSERT 0
#define PR_DBG_CONVEX_HULL 0
#include "pr/maths/convexhull.h"

using namespace pr;
using namespace UnitTest;

SUITE(PRConvexHullTests)
{
	bool CheckHull(UnitTest::TestResults& testResults_, TestDetails const& m_details, v4 const* verts, std::size_t num_verts, uint const* index, uint const* faces, std::size_t num_faces)
	{
		// Generate normals for the faces
		v4* plane = PR_ALLOCA_POD(v4, num_faces);
		std::size_t f_idx = 0;
		for( uint const* f = faces, *f_end = f + 3*num_faces; f != f_end; f += 3, ++f_idx )
		{
			CHECK(*(f+0) < num_verts);
			CHECK(*(f+1) < num_verts);
			CHECK(*(f+2) < num_verts);
			CHECK(index[*(f+0)] < num_verts);
			CHECK(index[*(f+1)] < num_verts);
			CHECK(index[*(f+2)] < num_verts);
			v4 const& a = verts[index[*(f+0)]];
			v4 const& b = verts[index[*(f+1)]];
			v4 const& c = verts[index[*(f+2)]];

			v4 e0 = b - a;
			v4 e1 = c - a;
			plane[f_idx] = Cross3(e0, e1);
			CHECK(Length3(plane[f_idx]) > maths::tiny);
			plane[f_idx].w = -Dot3(plane[f_idx], a);
		}

		// Check each vertex is on, or behind all faces
		for( v4 const* v = verts, *v_end = v + num_verts; v != v_end; ++v )
		{
			for( v4 const* n = plane, *n_end = n + num_faces; n != n_end; ++n )
			{
				CHECK(Dot4(*v, *n) < maths::tiny);
			}
		}
		return true;
	}

	TEST(4Verts_AllDegenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 0.0f, 1.0f}			
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);
		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;
		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(4Verts_3Degenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f}			
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);
		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;
		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(4Verts_YDegenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.5f, 1.0f, 0.0f, 1.0f},
			{1.0f, 1.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f}			
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);
		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;
		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CHECK_EQUAL(std::size_t(4), vert_count);
		CHECK_EQUAL(std::size_t(4), face_count);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(4Verts_XYDegenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f},
			{-1.0f, -1.0f, 0.0f, 1.0f},
			{ 1.0f,  1.0f, 0.0f, 1.0f}
			
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);
		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;
		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(4Verts_XDegenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{1.0f, 0.0f, 0.0f, 1.0f},
			{1.0f, 1.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f}			
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);
		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;
		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CHECK_EQUAL(std::size_t(4), vert_count);
		CHECK_EQUAL(std::size_t(4), face_count);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(5Verts_XDegenerate)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f},
			{0.0f, 1.0f, 0.0f, 1.0f},
			{0.0f, 1.0f, 1.0f, 1.0f},
			{0.0f,-1.0f, 0.0f, 1.0f}
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(PointCloud)
	{
		uint const num_verts = 200;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i] = Random3(0.0f, 1.0f, 1.0f);
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(PointCloudWithExtremes)
	{
		uint const num_verts = 200;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i] = Random3(0.0f, 1.0f, 1.0f);
		}
		vert[num_verts - 2].set(-1.0f, -1.0f,  1.5f, 1.0f);
		vert[num_verts - 1].set(-1.0f, -1.0f, -1.5f, 1.0f);

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(Cube)
	{
		v4 vert[] = 
		{
			{0.0f, 0.0f, 0.0f, 1.0f},
			{1.0f, 0.0f, 0.0f, 1.0f},
			{0.0f, 1.0f, 0.0f, 1.0f},
			{1.0f, 1.0f, 0.0f, 1.0f},
			{0.0f, 0.0f, 1.0f, 1.0f},
			{1.0f, 0.0f, 1.0f, 1.0f},
			{0.0f, 1.0f, 1.0f, 1.0f},
			{1.0f, 1.0f, 1.0f, 1.0f}
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CHECK_EQUAL(std::size_t(8), vert_count);
		CHECK_EQUAL(std::size_t(12), face_count);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(Sphere)
	{
		uint const ydiv = 10;
		uint const xdiv = 10;
		uint const size = xdiv * (ydiv + 1);
		uint const size_faces = 2 * (size - 2);
		v4 vert[size];
		uint num_verts = 0;
		for( uint j = 0; j != ydiv + 1; ++j )
		{
			float r = Sin(j * maths::tau_by_2 / ydiv);
			float y = Cos(j * maths::tau_by_2 / ydiv);
			for( uint i = 0; i != xdiv; ++i )
			{
				float x = Cos(i * 2.0f * maths::tau_by_2 / xdiv) * r;
				float z = Sin(i * 2.0f * maths::tau_by_2 / xdiv) * r;
				vert[num_verts++].set(x * 2.0f, y, z * 0.6f, 1.0f);
				if( j == 0 || j == ydiv ) break;
			}
		}
		uint num_faces = 2 * (num_verts - 2);

		uint index[size];
		uint faces[3*size_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CHECK_EQUAL(std::size_t(92), vert_count);
		CHECK_EQUAL(std::size_t(180), face_count);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(LongShape)
	{
		v4 vert[] =
		{
			{-0.493541f, -2.294634f, 0.211264f, 1.000000f},
			{0.127335f, -5.477110f, 0.457231f, 1.000000f},
			{-0.456415f, 1.067915f, -0.491809f, 1.000000f},
			{0.476197f, 1.321660f, -0.113740f, 1.000000f},
			{-0.069687f, 4.504634f, 0.472885f, 1.000000f},
			{-0.438117f, -4.612503f, -0.133301f, 1.000000f},
			{0.223667f, 5.475550f, -0.490718f, 1.000000f},
			{0.297275f, -0.533585f, -0.473785f, 1.000000f},
			{-0.450738f, 4.839915f, -0.227746f, 1.000000f},
			{0.107293f, -4.806756f, 0.020900f, 1.000000f},
			{-0.493311f, 2.285343f, -0.209402f, 1.000000f},
			{0.282754f, 5.375336f, 0.326577f, 1.000000f},
			{-0.071634f, -2.778773f, -0.436316f, 1.000000f},
			{-0.450421f, 3.956512f, -0.420015f, 1.000000f},
			{0.443233f, 4.955112f, -0.236311f, 1.000000f},
			{-0.380446f, -1.554579f, -0.422242f, 1.000000f},
			{0.436832f, -2.741045f, -0.028393f, 1.000000f},
			{0.453205f, 0.586007f, -0.406524f, 1.000000f},
			{0.423260f, 5.022879f, 0.335766f, 1.000000f},
			{-0.389318f, -4.167378f, -0.214997f, 1.000000f},
			{0.469766f, 3.552652f, -0.305288f, 1.000000f},
			{0.406944f, -0.411167f, 0.449277f, 1.000000f},
			{-0.012819f, -1.606200f, 0.490088f, 1.000000f},
			{0.386338f, -3.094181f, 0.263505f, 1.000000f},
			{-0.447474f, 4.638098f, 0.444664f, 1.000000f},
			{-0.475273f, 4.891696f, 0.147518f, 1.000000f},
			{-0.377672f, -0.596598f, 0.464163f, 1.000000f},
			{-0.434584f, -4.868664f, 0.272354f, 1.000000f},
			{-0.382270f, -5.095175f, 0.226526f, 1.000000f},
			{-0.345977f, 3.564693f, -0.491722f, 1.000000f},
			{0.010861f, 3.385645f, 0.474700f, 1.000000f},
			{-0.156494f, 5.310896f, -0.370127f, 1.000000f},
			{-0.197054f, -4.943936f, 0.416591f, 1.000000f},
			{-0.447616f, 4.309384f, -0.352710f, 1.000000f},
			{-0.302173f, 5.210850f, -0.121394f, 1.000000f},
			{0.005122f, -0.192572f, -0.291109f, 1.000000f},
			{0.389094f, -2.432400f, -0.026930f, 1.000000f},
			{-0.320587f, -1.953142f, 0.301855f, 1.000000f},
			{0.338208f, 1.600369f, 0.096773f, 1.000000f},
			{0.240570f, 3.432305f, -0.421339f, 1.000000f},
			{-0.367506f, -1.464310f, -0.312721f, 1.000000f},
			{-0.386147f, 0.041389f, -0.443803f, 1.000000f},
			{0.212310f, -4.155481f, 0.058995f, 1.000000f},
			{-0.204579f, 5.202238f, -0.036123f, 1.000000f},
			{-0.376547f, 2.118421f, 0.126899f, 1.000000f},
			{-0.442893f, 4.362907f, -0.342261f, 1.000000f},
			{-0.430160f, 2.338058f, 0.062934f, 1.000000f},
			{0.119173f, 5.209123f, -0.465110f, 1.000000f},
			{-0.072112f, 5.306923f, -0.302250f, 1.000000f},
			{0.290649f, 0.887509f, 0.343167f, 1.000000f},
			{-0.322267f, 3.809903f, -0.475606f, 1.000000f},
			{-0.058321f, -4.298362f, 0.418672f, 1.000000f},
			{-0.071515f, -0.770706f, 0.440612f, 1.000000f},
			{0.421502f, 2.573503f, 0.236041f, 1.000000f},
			{0.054340f, -3.586976f, 0.172957f, 1.000000f},
			{-0.311438f, 0.786050f, 0.347521f, 1.000000f},
			{-0.097709f, 2.777553f, 0.456882f, 1.000000f},
			{0.131375f, 0.384183f, 0.447243f, 1.000000f},
			{0.401978f, -1.478414f, -0.082045f, 1.000000f},
			{0.436654f, 4.107828f, 0.093460f, 1.000000f},
			{0.337046f, 4.790114f, -0.070580f, 1.000000f},
			{0.351358f, 4.047699f, 0.096967f, 1.000000f},
			{0.044780f, -4.317312f, -0.016260f, 1.000000f},
			{-0.153452f, 4.222322f, -0.028753f, 1.000000f},
			{-0.339149f, 2.358130f, -0.242787f, 1.000000f},
			{-0.356153f, 3.271121f, -0.351402f, 1.000000f},
			{-0.135874f, 3.434116f, -0.445232f, 1.000000f},
			{0.122553f, -2.624215f, 0.026344f, 1.000000f},
			{-0.096256f, -3.140398f, -0.072270f, 1.000000f},
			{0.032491f, -2.981343f, -0.100330f, 1.000000f},
			{0.108166f, -1.288189f, -0.313272f, 1.000000f},
			{0.143102f, -2.837065f, 0.050306f, 1.000000f},
			{0.075624f, -0.419913f, -0.329157f, 1.000000f},
			{-0.221829f, 1.391353f, -0.372655f, 1.000000f},
			{0.032030f, 4.164355f, 0.265240f, 1.000000f},
			{0.266246f, 2.624946f, 0.043393f, 1.000000f},
			{-0.004968f, 3.458048f, 0.158350f, 1.000000f},
			{0.166290f, 4.163680f, -0.121559f, 1.000000f},
			{0.248056f, 3.112865f, -0.197773f, 1.000000f},
			{-0.430025f, -3.140008f, -0.048213f, 1.000000f},
			{-0.323131f, -1.150595f, -0.280679f, 1.000000f},
			{0.021959f, -4.556680f, 0.320031f, 1.000000f},
			{-0.479080f, -1.687412f, 0.078467f, 1.000000f},
			{-0.050032f, -4.929212f, 0.268486f, 1.000000f},
			{-0.268286f, -2.429099f, 0.291980f, 1.000000f},
			{-0.031944f, 3.906383f, 0.392041f, 1.000000f},
			{-0.155346f, 0.879561f, 0.009320f, 1.000000f},
			{-0.173062f, -2.593422f, 0.268893f, 1.000000f},
			{0.005934f, 3.110236f, 0.233234f, 1.000000f},
			{-0.056339f, 3.223024f, 0.250705f, 1.000000f},
			{-0.032761f, -1.260472f, 0.113718f, 1.000000f},
			{0.041210f, -1.585149f, 0.153634f, 1.000000f},
			{0.154975f, -0.833955f, 0.208787f, 1.000000f},
			{0.034065f, 1.070397f, 0.049187f, 1.000000f},
			{-0.158818f, 0.176298f, 0.166561f, 1.000000f},
			{0.067897f, -1.023180f, 0.215515f, 1.000000f},
			{-0.400348f, -0.600877f, -0.125627f, 1.000000f},
			{-0.220028f, -1.691183f, 0.075236f, 1.000000f},
			{0.104214f, -0.065671f, -0.071476f, 1.000000f},
			{-0.201052f, 0.270828f, -0.269817f, 1.000000f},
		};
		uint const num_verts = sizeof(vert)/sizeof(vert[0]);
		uint const num_faces = 2 * (num_verts - 2);

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CHECK_EQUAL(std::size_t(35), vert_count);
		CHECK_EQUAL(std::size_t(66), face_count);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(DegeneratePoint)
	{
		uint const num_verts = 20;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		vert[0] = Random3(0.0f, 1.0f, 1.0f);
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i] = vert[0];
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(DegenerateLine)
	{
		uint const num_verts = 20;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i].set(float(i) / num_verts, float(i) / num_verts, float(i) / num_verts, 1.0f);
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(DegeneratePlane)
	{
		uint const num_verts = 20;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		v4 dir = Random3N(0.0f);
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i] = Random3(0.0f, 1.0f, 1.0f);
			vert[i] -= Dot3(vert[i], dir) * dir;
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(!hull_made);
		CHECK_EQUAL(std::size_t(0), vert_count);
		CHECK_EQUAL(std::size_t(0), face_count);
	}
	TEST(DegeneratePointCloud)
	{
		uint const num_verts = 200;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		for( int i = 0; i != num_verts/2; ++i )
		{
			vert[i]             = Random3(0.0f, 1.0f, 1.0f);
			vert[i+num_verts/2] = Random3(0.0f, 1.0f, 1.0f);
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count, face_count;
		bool hull_made = ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);
		CHECK(hull_made);
		CheckHull(testResults_, m_details, vert, num_verts, index, faces, face_count);
	}
	TEST(VertSortingHull)
	{
		uint const num_verts = 20;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert0[num_verts];
		v4 vert1[num_verts];
		for( int i = 0; i != num_verts; ++i )
		{
			vert0[i] = Random3(0.0f, 1.0f, 1.0f);
			vert1[i] = vert0[i];
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		std::size_t vert_count0, face_count0;
		bool hull_made0 = ConvexHull(vert0, index, index+num_verts, faces, faces + 3*num_faces, vert_count0, face_count0);
		CHECK(hull_made0);

		std::size_t vert_count1, face_count1;
		bool hull_made1 = ConvexHull(vert1, num_verts, faces, faces + 3*num_faces, vert_count1, face_count1);
		CHECK(hull_made1);
		CHECK_EQUAL(vert_count0, vert_count1);
		CHECK_EQUAL(face_count0, face_count1);
		for( int i = 0; i != num_verts; ++i )
		{
			CHECK(FEql4(vert0[index[i]], vert1[i]));
		}
	}
	TEST(TimeTest)
	{
		uint const num_verts = 200;
		uint const num_faces = 2 * (num_verts - 2);
		v4 vert[num_verts];
		for( int i = 0; i != num_verts; ++i )
		{
			vert[i] = Random3(0.0f, 1.0f, 1.0f);
		}

		uint index[num_verts];
		uint faces[3*num_faces];
		for( uint i = 0; i != num_verts; ++i ) index[i] = i;

		RecordingReporter reporter;
		TestResults result(&reporter);
		{
			TimeConstraint t(10, result, TestDetails("ConvexHull Test", "", "", 0));
			for( int i = 0; i != 100; ++i )
			{
				std::size_t vert_count, face_count;
				ConvexHull(vert, index, index+num_verts, faces, faces + 3*num_faces, vert_count, face_count);				
			}			
		}
		testResults_;
	}
	//*/
}//SUITE(PRConvexHullTests)

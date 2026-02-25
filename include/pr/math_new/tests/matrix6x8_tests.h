//*****************************************************************************
// Maths library
//  Copyright (c) Rylogic Ltd 2002
//*****************************************************************************
#pragma once
#include "pr/math_new/math.h"

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/matrix.h"
namespace pr::math
{
	#if 0 // todo
	PRUnitTest(Matrix6x8Tests, float)
	{
		using S = T;
		using mat6_t = Mat6x8<S, void, void>;
		using mat3_t = Mat3x4<S, void, void>;
		using vec8_t = Vec8<S, void>;
		using vec4_t = Vec4<S, void>;
		using vec3_t = Vec3<S, void>;

		{// Memory order tests
			auto m1 = Matrix<S>{6, 8,  // = [{1} {2} {3} {4} {5} {6}]
				{
					1, 1, 1, 1, 1, 1, 1, 1,
					2, 2, 2, 2, 2, 2, 2, 2,
					3, 3, 3, 3, 3, 3, 3, 3,
					4, 4, 4, 4, 4, 4, 4, 4,
					5, 5, 5, 5, 5, 5, 5, 5,
					6, 6, 6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<S>{6, 8,
				{
					1, 1, 1, 1, 2, 2, 2, 2, // = [[1] [3]]
					1, 1, 1, 1, 2, 2, 2, 2, //   [[2] [4]]
					1, 1, 1, 1, 2, 2, 2, 2,
					3, 3, 3, 3, 4, 4, 4, 4,
					3, 3, 3, 3, 4, 4, 4, 4,
					3, 3, 3, 3, 4, 4, 4, 4,
			}};
			auto M1 = mat6_t // = [{1} {2} {3} {4} {5} {6}]
			{
				mat3_t{vec4_t(1), vec4_t(2), vec4_t(3)}, mat3_t{vec4_t(4), vec4_t(5), vec4_t(6)},
				mat3_t{vec4_t(1), vec4_t(2), vec4_t(3)}, mat3_t{vec4_t(4), vec4_t(5), vec4_t(6)}
			};
			auto M2 = mat6_t
			{
				mat3_t(1), mat3_t(3), // = [[1] [3]]
				mat3_t(2), mat3_t(4)  //   [[2] [4]]
			};

			for (int r = 0; r != 6; ++r)
				for (int c = 0; c != 8; ++c)
				{
					PR_EXPECT(FEql(m1(r, c), M1[r][c]));
					PR_EXPECT(FEql(m2(r, c), M2[r][c]));
				}

			auto M3 = mat6_t // = [{1} {2} {3} {4} {5} {6}]
			{
				vec8_t{1, 1, 1, 1, 1, 1, 1, 1},
				vec8_t{2, 2, 2, 2, 2, 2, 2, 2},
				vec8_t{3, 3, 3, 3, 3, 3, 3, 3},
				vec8_t{4, 4, 4, 4, 4, 4, 4, 4},
				vec8_t{5, 5, 5, 5, 5, 5, 5, 5},
				vec8_t{6, 6, 6, 6, 6, 6, 6, 6},
			};
			PR_EXPECT(FEql(M3, M1));
		}
		{// Array access
			auto m1 = mat6_t
			{
				mat3_t{vec4_t(1), vec4_t(2), vec4_t(3)}, mat3_t{vec4_t(4), vec4_t(5), vec4_t(6)}, // = [{1} {2} {3} {4} {5} {6}]
				mat3_t{vec4_t(1), vec4_t(2), vec4_t(3)}, mat3_t{vec4_t(4), vec4_t(5), vec4_t(6)}
			};
			PR_EXPECT(FEql(m1[0], vec8_t{1, 1, 1, 1, 1, 1, 1, 1}));
			PR_EXPECT(FEql(m1[1], vec8_t{2, 2, 2, 2, 2, 2, 2, 2}));
			PR_EXPECT(FEql(m1[2], vec8_t{3, 3, 3, 3, 3, 3, 3, 3}));
			PR_EXPECT(FEql(m1[3], vec8_t{4, 4, 4, 4, 4, 4, 4, 4}));
			PR_EXPECT(FEql(m1[4], vec8_t{5, 5, 5, 5, 5, 5, 5, 5}));
			PR_EXPECT(FEql(m1[5], vec8_t{6, 6, 6, 6, 6, 6, 6, 6}));

			auto tmp = m1.col(0);
			m1.col(0, m1[5]);
			m1.col(5, tmp);
			PR_EXPECT(FEql(m1[0], vec8_t{6, 6, 6, 6, 6, 6, 6, 6}));
			PR_EXPECT(FEql(m1[5], vec8_t{1, 1, 1, 1, 1, 1, 1, 1}));
		}
		{// Multiply vector
			auto m = Matrix<float>{6, 6,
				{
					1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
					2, 2, 2, 2, 2, 2,
					3, 3, 3, 3, 3, 3,
					4, 4, 4, 4, 4, 4,
					5, 5, 5, 5, 5, 5,
					6, 6, 6, 6, 6, 6,
			}};
			auto v = Matrix<float>{1, 6,
			{
				1, 2, 3, 4, 5, 6,
			}};
			auto e = Matrix<float>{1, 6,
			{
				91, 91, 91, 91, 91, 91,
			}};
			auto r = m * v;
			PR_EXPECT(FEql(r, e));

			auto M = mat6_t
			{
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)},
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto V = vec8_t
			{
				vec4_t{1, 2, 3, 0},
				vec4_t{4, 5, 6, 0},
			};
			auto E = vec8_t
			{
				vec4_t{91, 91, 91, 0},
				vec4_t{91, 91, 91, 0},
			};
			auto R = M * V;
			PR_EXPECT(FEql(R, E));
		}
		{// Multiply matrix
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto m2 = Matrix<float>{6, 6,
			{
				1, 1, 1, 2, 2, 2, // = [[1] [3]]
				1, 1, 1, 2, 2, 2, //   [[2] [4]]
				1, 1, 1, 2, 2, 2,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
				3, 3, 3, 4, 4, 4,
			}};
			auto m3 = Matrix<float>{6, 6,
			{
				36, 36, 36, 36, 36, 36,
				36, 36, 36, 36, 36, 36,
				36, 36, 36, 36, 36, 36,
				78, 78, 78, 78, 78, 78,
				78, 78, 78, 78, 78, 78,
				78, 78, 78, 78, 78, 78,
			}};
			auto m4 = m1 * m2;
			PR_EXPECT(FEql(m3, m4));

			auto M1 = mat6_t
			{
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)},
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)}, // = [{1} {2} {3} {4} {5} {6}]
			};
			auto M2 = mat6_t
			{
				mat3_t{vec3_t(1), vec3_t(1), vec3_t(1)}, mat3_t{vec3_t(3), vec3_t(3), vec3_t(3)}, // = [[1] [3]]
				mat3_t{vec3_t(2), vec3_t(2), vec3_t(2)}, mat3_t{vec3_t(4), vec3_t(4), vec3_t(4)}  //   [[2] [4]]
			};
			auto M3 = mat6_t
			{
				mat3_t{vec3_t(36), vec3_t(36), vec3_t(36)}, mat3_t{vec3_t(78), vec3_t(78), vec3_t(78)},
				mat3_t{vec3_t(36), vec3_t(36), vec3_t(36)}, mat3_t{vec3_t(78), vec3_t(78), vec3_t(78)}
			};
			auto M4 = M1 * M2;
			PR_EXPECT(FEql(M3, M4));
		}
		{// Transpose
			auto m1 = Matrix<float>{6, 6,
			{
				1, 1, 1, 1, 1, 1, // = [{1} {2} {3} {4} {5} {6}]
				2, 2, 2, 2, 2, 2,
				3, 3, 3, 3, 3, 3,
				4, 4, 4, 4, 4, 4,
				5, 5, 5, 5, 5, 5,
				6, 6, 6, 6, 6, 6,
			}};
			auto M1 = mat6_t
			{
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)}, // = [{1} {2} {3} {4} {5} {6}]
				mat3_t{vec3_t(1), vec3_t(2), vec3_t(3)}, mat3_t{vec3_t(4), vec3_t(5), vec3_t(6)}
			};
			auto m2 = Transpose(m1);
			auto M2 = Transpose(M1);

			for (int i = 0; i != 6; ++i)
			{
				PR_EXPECT(FEql(M2[i], vec8_t{vec3_t{1, 2, 3}, vec3_t{4, 5, 6}}));

				PR_EXPECT(FEql(m2(i, 0), M2[i].ang.x));
				PR_EXPECT(FEql(m2(i, 1), M2[i].ang.y));
				PR_EXPECT(FEql(m2(i, 2), M2[i].ang.z));
				PR_EXPECT(FEql(m2(i, 3), M2[i].lin.x));
				PR_EXPECT(FEql(m2(i, 4), M2[i].lin.y));
				PR_EXPECT(FEql(m2(i, 5), M2[i].lin.z));
			}
		}
		{// Inverse
			auto M = mat6_t
			{
				vec8_t{+1, +1, +2, -1, +6, +2},
				vec8_t{-2, +2, +4, -3, +5, -4},
				vec8_t{+1, +3, -2, -5, +4, +6},
				vec8_t{+1, +4, +3, -7, +3, -5},
				vec8_t{+1, +2, +3, -2, +2, +3},
				vec8_t{+1, -1, -2, -3, +6, -1}
			};
			auto M_ = mat6_t
			{
				vec8_t{+227.0f / 794.0f, -135.0f / 397.0f, -101.0f / 794.0f, +84.0f / 397.0f, -16.0f / 397.0f, -4.0f / 397.0f},
				vec8_t{+219.0f / 397.0f, -75.0f / 794.0f, +382.0f / 1985.f, +179.0f / 794.0f, -2647.f / 3970.f, -976.0f / 1985.f},
				vec8_t{-129.0f / 794.0f, +26.0f / 397.0f, -107.0f / 794.0f, -25.0f / 397.0f, +156.0f / 397.0f, +39.0f / 397.0f},
				vec8_t{+367.0f / 794.0f, -71.0f / 794.0f, +51.0f / 3970.f, +53.0f / 794.0f, -1733.f / 3970.f, -564.0f / 1985.f},
				vec8_t{+159.0f / 794.0f, +19.0f / 794.0f, +87.0f / 3970.f, -3.0f / 794.0f, -621.0f / 3970.f, -28.0f / 1985.f},
				vec8_t{-50.0f / 397.0f, +14.0f / 397.0f, +17.0f / 397.0f, -44.0f / 397.0f, +84.0f / 397.0f, +21.0f / 397.0f}
			};

			auto m_ = Invert(M);
			PR_EXPECT(FEql(m_, M_));

			auto I = M * M_;
			PR_EXPECT(FEql(I, mat6_t::Identity()));

			auto i = M * m_;
			PR_EXPECT(FEql(i, mat6_t::Identity()));
		}
	}
	#endif
}
#endif

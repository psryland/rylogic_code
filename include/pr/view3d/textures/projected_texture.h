//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"

namespace pr
{
	namespace rdr
	{
		struct ProjectedTexture
		{
			Texture2DPtr m_tex;
			m4x4 m_o2w;

			// Create a transform for projecting a world space point to normalised texture space
			static m4x4 MakeTransform(v4 const& eye, v4 const& at, v4 const& up, float aspect, float fovY, float Znear, float Zfar, bool orthographic)
			{
				// world to projection origin
				auto w2pt = InvertFast(m4x4::LookAt(eye, at, up));

				// Projection transform
				float height = 2.0f * Tan(fovY * 0.5f); // Ortho transform => height is calculated at '1' from the 'eye'
				auto proj = orthographic
					? m4x4::ProjectionOrthographic(height * aspect, height, Znear, Zfar, true)
					: m4x4::ProjectionPerspectiveFOV(fovY, aspect, Znear, Zfar, true);

				// Translate and scale to normalised texture coords
				auto scale = m4x4::Scale(0.5f, -0.5f, 1.0f, v4(0.5f, 0.5f, 0.0f, 1.0f));
				return scale * proj * w2pt;
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::rdr
{
	PRUnitTest(ProjectedTextureTests)
	{
		{// Projection
			m4x4 proj = ProjectedTexture::MakeTransform(v4(0,0,10,1), v4Origin, v4YAxis, 1.0f, float(maths::tau_by_4), 0.01f, 100.0f, false);
			v4 vin[] =
			{
				v4Origin,
				v4(-10,-10, 0, 1),
				v4(-10,  0, 0, 1),
				v4(-10, 10, 0, 1),
				v4(  0, 10, 0, 1),
				v4( 10, 10, 0, 1),
				v4( 10,  0, 0, 1),
				v4( 10,-10, 0, 1),
				v4(  0,-10, 0, 1),
			};
			v2 vout[] =
			{
				v2( 0.5f, 0.5f),
				v2( 0.0f, 1.0f),
				v2( 0.0f, 0.5f),
				v2( 0.0f, 0.0f),
				v2( 0.5f, 0.0f),
				v2( 1.0f, 0.0f),
				v2( 1.0f, 0.5f),
				v2( 1.0f, 1.0f),
				v2( 0.5f, 1.0f),
			};
			for (int i = 0; i != PR_COUNTOF(vin); ++i)
			{
				v4 const& vi = vin[i];
				v2 const& vo = vout[i];

				auto v0 = proj * vi;
				auto v1 = v0.xy / v0.w;

				PR_CHECK(FEql2(v1, vo), true);
			}
		}
		{// Orthographic
			m4x4 proj = ProjectedTexture::MakeTransform(v4(0,0,10,1), v4Origin, v4YAxis, 1.0f, float(maths::tau_by_4), 0.01f, 100.0f, true);
			v4 vin[] =
			{
				v4Origin,
				v4(-1, -1, 0, 1),
				v4(-1,  0, 0, 1),
				v4(-1,  1, 0, 1),
				v4( 0,  1, 0, 1),
				v4( 1,  1, 0, 1),
				v4( 1,  0, 0, 1),
				v4( 1, -1, 0, 1),
				v4( 0, -1, 0, 1),
			};
			v2 vout[] =
			{
				v2( 0.5f, 0.5f),
				v2( 0.0f, 1.0f),
				v2( 0.0f, 0.5f),
				v2( 0.0f, 0.0f),
				v2( 0.5f, 0.0f),
				v2( 1.0f, 0.0f),
				v2( 1.0f, 0.5f),
				v2( 1.0f, 1.0f),
				v2( 0.5f, 1.0f),
			};
			for (int i = 0; i != PR_COUNTOF(vin); ++i)
			{
				v4 const& vi = vin[i];
				v2 const& vo = vout[i];

				auto v0 = proj * vi;
				auto v1 = v0.xy / v0.w;

				PR_CHECK(FEql2(v1, vo), true);
			}
		}
	}
}
#endif
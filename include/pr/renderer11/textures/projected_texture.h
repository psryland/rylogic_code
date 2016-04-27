//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		struct ProjectedTexture
		{
			Texture2DPtr m_tex;
			pr::m4x4 m_o2w;

			// Create a transform for projecting a world space point to normalised texture space
			static pr::m4x4 MakeTransform(pr::v4 const& eye, pr::v4 const& at, pr::v4 const& up, float aspect, float fovY, float Znear, float Zfar, bool orthographic)
			{
				// world to projection origin
				auto w2pt = pr::InvertFast(m4x4::LookAt(eye, at, up));

				// Projection transform
				float height = 2.0f * pr::Tan(fovY * 0.5f); // Ortho transform => height is calculated at '1' from the 'eye'
				auto proj = orthographic
					? m4x4::ProjectionOrthographic(height * aspect, height, Znear, Zfar, true)
					: m4x4::ProjectionPerspectiveFOV(fovY, aspect, Znear, Zfar, true);

				// Translate and scale to normalised texture coords
				auto scale = m4x4::Scale(0.5f, -0.5f, 1.0f, pr::v4(0.5f, 0.5f, 0.0f, 1.0f));
				return scale * proj * w2pt;
			}
		};
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_renderer11_textures_projected_texture)
		{
			using namespace pr::rdr;

			{// Projection
				pr::m4x4 proj = ProjectedTexture::MakeTransform(pr::v4(0,0,10,1), pr::v4Origin, pr::v4YAxis, 1.0f, pr::maths::tau_by_4, 0.01f, 100.0f, false);
				pr::v4 vin[] =
				{
					pr::v4Origin,
					pr::v4(-10,-10, 0, 1),
					pr::v4(-10,  0, 0, 1),
					pr::v4(-10, 10, 0, 1),
					pr::v4(  0, 10, 0, 1),
					pr::v4( 10, 10, 0, 1),
					pr::v4( 10,  0, 0, 1),
					pr::v4( 10,-10, 0, 1),
					pr::v4(  0,-10, 0, 1),
				};
				pr::v2 vout[] =
				{
					pr::v2( 0.5f, 0.5f),
					pr::v2( 0.0f, 1.0f),
					pr::v2( 0.0f, 0.5f),
					pr::v2( 0.0f, 0.0f),
					pr::v2( 0.5f, 0.0f),
					pr::v2( 1.0f, 0.0f),
					pr::v2( 1.0f, 0.5f),
					pr::v2( 1.0f, 1.0f),
					pr::v2( 0.5f, 1.0f),
				};
				for (int i = 0; i != PR_COUNTOF(vin); ++i)
				{
					pr::v4 const& vi = vin[i];
					pr::v2 const& vo = vout[i];

					auto v0 = proj * vi;
					auto v1 = v0.xy / v0.w;

					PR_CHECK(FEql2(v1, vo), true);
				}
			}
			{// Orthographic
				pr::m4x4 proj = ProjectedTexture::MakeTransform(pr::v4(0,0,10,1), pr::v4Origin, pr::v4YAxis, 1.0f, pr::maths::tau_by_4, 0.01f, 100.0f, true);
				pr::v4 vin[] =
				{
					pr::v4Origin,
					pr::v4(-1, -1, 0, 1),
					pr::v4(-1,  0, 0, 1),
					pr::v4(-1,  1, 0, 1),
					pr::v4( 0,  1, 0, 1),
					pr::v4( 1,  1, 0, 1),
					pr::v4( 1,  0, 0, 1),
					pr::v4( 1, -1, 0, 1),
					pr::v4( 0, -1, 0, 1),
				};
				pr::v2 vout[] =
				{
					pr::v2( 0.5f, 0.5f),
					pr::v2( 0.0f, 1.0f),
					pr::v2( 0.0f, 0.5f),
					pr::v2( 0.0f, 0.0f),
					pr::v2( 0.5f, 0.0f),
					pr::v2( 1.0f, 0.0f),
					pr::v2( 1.0f, 0.5f),
					pr::v2( 1.0f, 1.0f),
					pr::v2( 0.5f, 1.0f),
				};
				for (int i = 0; i != PR_COUNTOF(vin); ++i)
				{
					pr::v4 const& vi = vin[i];
					pr::v2 const& vo = vout[i];

					auto v0 = proj * vi;
					auto v1 = v0.xy / v0.w;

					PR_CHECK(FEql2(v1, vo), true);
				}
			}
		}
	}
}
#endif
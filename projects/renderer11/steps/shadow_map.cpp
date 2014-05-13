//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/steps/shadow_map.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		namespace smap
		{
			// Shadow map cbuffer definitions
			#include "renderer11/shaders/hlsl/shadow/shadow_map_cbuf.hlsli"
		}

		// Algorithm:
		// - Create projection transforms from the light onto planes that are parallel the sides
		//   of the view frustum. Note, these projects are skewed so that there is more effective
		//   resolution near the camera
		// - Render the scene writing distance-to-frustum data. (this solves the problem of directional
		//   lights being at infinity)
		// - In the lighting pass, project ray from ws_pos to frustum, measure distance and compare to
		//   texture to detect shadow
		//
		// Notes:
		//  Although there's 5 projection matrices per light, one shadow ray can only hit one
		//  surface of the view frustum, so it should be possible to render all 5 frustum sides
		//  in a single pass

		ShadowMap::ShadowMap(Scene& scene)
			:RenderStep(scene)
			,m_shader(scene.m_rdr->m_shdr_mgr.FindShader(EStockShader::GBufferVS))
		{
			PR_ASSERT(PR_DBG_RDR, m_shader != nullptr, "GBuffer shader missing");
		}




		// Create a projection transform that will take points in world space and project them 
		// onto a surface parallel to the frustum plane for the given face (based on light type).
		bool CreateProjection(int face, pr::Frustum const& frust, pr::m4x4 const& c2w, Light const& light, pr::m4x4& w2s)
		{
			#define DBG_PROJ 0
			#if DBG_PROJ
			struct Dump {
				Dump(v4 const& tl_, v4 const& tr_, v4 const& bl_, v4 const& br_) {
					v4 tl = tl_ / tl_.w;
					v4 tr = tr_ / tr_.w;
					v4 bl = bl_ / bl_.w;
					v4 br = br_ / br_.w;
					std::string str;
					pr::ldr::Box("smap", 0xFFFFFFFF, pr::v4::make(0,0,0.5f,1), pr::v4::make(2,2,1,0), str);
					pr::ldr::Box("tl", 0xFFFF0000, tl, 0.2f, str);
					pr::ldr::Box("tr", 0xFF00FF00, tr, 0.2f, str);
					pr::ldr::Box("bl", 0xFF0000FF, bl, 0.2f, str);
					pr::ldr::Box("br", 0xFFFFFF00, br, 0.2f, str);
					pr::ldr::Write(str, "d:/deleteme/smap_proj_screen.ldr");
				}
			};
			#endif

			// Get the frustum normal for 'face'
			pr::v4 ws_norm = c2w * frust.Normal(face);
	
			float sign_z[4] =
			{
				pr::Sign<float>(face==1||face==3),
				pr::Sign<float>(face==0||face==3),
				pr::Sign<float>(face==1||face==2),
				pr::Sign<float>(face==0||face==2)
			};

			// Get the corners of the plane we want to project onto in world space.
			pr::v4 fdim = frust.Dim();
			pr::v4 tl, TL = c2w * pr::v4::make(-fdim.x,  fdim.y, sign_z[0]*fdim.z, 1.0f);
			pr::v4 tr, TR = c2w * pr::v4::make( fdim.x,  fdim.y, sign_z[1]*fdim.z, 1.0f);
			pr::v4 bl, BL = c2w * pr::v4::make(-fdim.x, -fdim.y, sign_z[2]*fdim.z, 1.0f);
			pr::v4 br, BR = c2w * pr::v4::make( fdim.x, -fdim.y, sign_z[3]*fdim.z, 1.0f);

			// Construct the projection transform based on the light type
			switch (light.m_type)
			{
			case ELight::Directional:
				{
					// The surface must face the light source
					if (pr::Dot3(light.m_direction, ws_norm) >= 0)
						return false;
			
					// Create a light to world transform
					// Position the light camera at the centre of the plane we're projecting onto looking in the light direction
					pr::v4 pos = (TL + TR + BL + BR) * 0.25f;
					pr::m4x4 lt2w = pr::LookAt(pos, pos + light.m_direction, pr::Parallel(light.m_direction,c2w.y) ? c2w.z : c2w.y);
					w2s = pr::InvertFast(lt2w);

					// Create an orthographic projection
					pr::m4x4 lt2s = pr::ProjectionOrthographic(1.0f, 1.0f, -100.0f, 100.0f, true);
					w2s = lt2s * w2s;

					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;

					// Rotate so that TL is above BL and TR is above BR (i.e. the left and right edges are vertical)
					pr::v2 ledge = Normalise2((tl - bl).xy());
					pr::m4x4 R = pr::m4x4Identity;
					R.x.set( ledge.y,  ledge.x, 0, 0);
					R.y.set(-ledge.x,  ledge.y, 0, 0);
					w2s = R * w2s;
			
					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;

					// Scale the face of the frustum into the viewport
					pr::m4x4 S = pr::Scale4x4(2.0f/(tr.x - tl.x), 2.0f/(tr.y - br.y), 1.0f, pr::v4Origin);
					w2s = S * w2s;
			
					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;

					// Shear to make the projected plane square
					pr::m4x4 H = pr::Shear4x4(-(tr.y - tl.y)/(tr.x - tl.x), 0, 0, 0, 0, 0, pr::v4Origin);
					w2s = H * w2s;

					#if DBG_PROJ
					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					Dump(tl,tr,bl,br);
					#endif
					return true;
				}
			case ELight::Spot:
			case ELight::Point:
				{
					// The surface must face the light source
					float dist_to_light = pr::Dot3(light.m_position - c2w.pos, ws_norm) + (face == 4)*frust.ZDist();
					if (dist_to_light <= 0) return false;

					// Create a light to world transform
					// Position the light camera at the light position looking in the -frustum plane normal direction
					pr::m4x4 lt2w = pr::LookAt(light.m_position, light.m_position - ws_norm, pr::Parallel(ws_norm,c2w.y) ? c2w.z : c2w.y);
					w2s = pr::Invert(lt2w);
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;

					// Create a perspective projection
					float zr = 0.001f, zf = dist_to_light, zn = zf*zr;
					pr::m4x4 lt2s = pr::ProjectionPerspective(tl.x*zr, tr.x*zr, tl.y*zr, bl.y*zr, zn, zf, true);
					w2s = lt2s * w2s;

					#if DBG_PROJ
					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					Dump(tl,tr,bl,br);
					#endif
					return true;
				}
			}
			return false;
			#undef DBG_PROJ
		}
	}
}
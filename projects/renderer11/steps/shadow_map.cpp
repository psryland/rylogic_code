//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/steps/shadow_map.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/render/state_stack.h"
#include "renderer11/shaders/common.h"

#define SMAP_TEST 1
#if SMAP_TEST
#include "pr/renderer11/instance.h"
#include "pr/renderer11/models/model_generator.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/shaders/input_layout.h"
#endif

namespace pr
{
	namespace rdr
	{
		// Algorithm:
		// - Create 5 projection transforms from the light onto each of the planes of the shadow frustum (= view frustum with nearer far plane).
		// - Vertex shader: transform verts to world space
		// - Geometry Shader: replicates the geometry 5 times, transforming each primitive by the corresponding projection transform
		// - Pixel Shader: use the id to write the depth into the appropriate place on the render target
		// - Use no depth buffer with blend mode min to avoid depth buffer issues
		// - Render the scene writing distance-to-frustum data. (this solves the problem of directional
		//   lights being at infinity)
		// - In the lighting pass, project ray from ws_pos to frustum, measure distance and compare to
		//   texture to detect shadow

		ShadowMap::ShadowMap(Scene& scene, Light& light, pr::iv2 size)
			:RenderStep(scene)
			,m_light(light)
			,m_tex()
			,m_rtv()
			,m_srv()
			,m_main_rtv()
			,m_main_dsv()
			,m_cbuf_frame (m_shdr_mgr->GetCBuf<hlsl::smap::CBufFrame >("smap::CBufFrame"))
			,m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::smap::CBufNugget>("smap::CBufNugget"))
			,m_smap_size(size)
			,m_vs(m_shdr_mgr->FindShader(EStockShader::ShadowMapVS))
			,m_ps(m_shdr_mgr->FindShader(EStockShader::ShadowMapPS))
			,m_gs_face(m_shdr_mgr->FindShader(EStockShader::ShadowMapFaceGS))
			,m_gs_line(m_shdr_mgr->FindShader(EStockShader::ShadowMapLineGS))
		{
			InitRT(size);
			
			m_dsb.Set(EDS::DepthEnable, FALSE);
			m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
			m_bsb.Set(EBS::BlendOp, D3D11_BLEND_OP_MIN, 1);
		}

		// Create the render target for the smap
		void ShadowMap::InitRT(pr::iv2 size)
		{
			// Release any existing RTs
			m_tex = nullptr;
			m_rtv = nullptr;
			m_srv = nullptr;

			auto device = m_scene->m_rdr->Device();
			auto dc = m_scene->m_rdr->ImmediateDC();

			// Create the smap texture
			TextureDesc tdesc;
			tdesc.Width          = size.x;
			tdesc.Height         = size.y;
			tdesc.Format         = DXGI_FORMAT_R16G16_UNORM;//DXGI_FORMAT_R8G8B8A8_UNORM; // R = z depth, G = -z depth
			tdesc.MipLevels      = 1;
			tdesc.ArraySize      = 1;
			tdesc.SampleDesc     = MultiSamp(1,0);
			tdesc.Usage          = D3D11_USAGE_DEFAULT;
			tdesc.BindFlags      = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
			tdesc.CPUAccessFlags = 0;
			tdesc.MiscFlags      = 0;

			// Create the resource
			pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_tex.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_tex, "smap tex"));

			// Get the render target view
			RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
			rtvdesc.Texture2D.MipSlice = 0;
			pr::Throw(device->CreateRenderTargetView(m_tex.m_ptr, &rtvdesc, &m_rtv.m_ptr));

			// Get the shader res view
			ShaderResViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
			srvdesc.Texture2D.MostDetailedMip = 0;
			srvdesc.Texture2D.MipLevels = 1;
			pr::Throw(device->CreateShaderResourceView(m_tex.m_ptr, &srvdesc, &m_srv.m_ptr));
		}

		// Bind the smap RT to the output merger
		void ShadowMap::BindRT(bool bind)
		{
			auto dc = m_scene->m_rdr->ImmediateDC();
			if (bind)
			{
				// Save a reference to the main render target/depth buffer
				dc->OMGetRenderTargets(1, &m_main_rtv.m_ptr, &m_main_dsv.m_ptr);

				// Bind the smap RT to the OM
				dc->OMSetRenderTargets(1, &m_rtv.m_ptr, nullptr);
			}
			else
			{
				// Restore the main RT and depth buffer
				dc->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);

				// Release our reference to the main rtv/dsv
				m_main_rtv = nullptr;
				m_main_dsv = nullptr;
			}
		}

		// Add model nuggets to the draw list for this render step
		void ShadowMap::AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets)
		{
			// Add a drawlist element for each nugget in the instance's model
			m_drawlist.reserve(m_drawlist.size() + nuggets.size());
			for (auto& nug : nuggets)
			{
				// Ensure the nugget contains the required vs/gs/ps shaders
				nug.m_smap[Id].m_vs = m_vs;
				nug.m_smap[Id].m_gs = (nug.m_topo == EPrim::LineList || nug.m_topo == EPrim::LineStrip) ? m_gs_line : m_gs_face;
				nug.m_smap[Id].m_ps = m_ps;

				// Add a dle for this nugget
				DrawListElement dle;
				dle.m_instance = &inst;
				dle.m_nugget   = &nug;
				dle.m_sort_key = 0;
				m_drawlist.push_back_fast(dle);
			}

			m_sort_needed = true;
		}

		// Perform the render step
		void ShadowMap::ExecuteInternal(StateStack& ss)
		{
			auto& dc = ss.m_dc;

			// Sort the draw list if needed
			SortIfNeeded();

			// Bind the g-buffer to the OM
			auto bind_gbuffer = pr::CreateScope(
				[this]{ BindRT(true); },
				[this]{ BindRT(false); });

			// Clear the smap depth buffer.
			// The depth data is the ws_pos-to-frustum surface, we only care about points
			// in front of the frustum faces => reset depths to zero.
			dc->ClearRenderTargetView(m_rtv.m_ptr, pr::ColourOne);

			// Viewport = the whole smap
			Viewport vp(UINT(m_smap_size.x), UINT(m_smap_size.y));
			dc->RSSetViewports(1, &vp);

			{// Set the frame constants
				auto& c2w = m_scene->m_view.m_c2w;
				auto shadow_frustum = m_scene->m_view.ShadowFrustum();
				
				hlsl::smap::CBufFrame cb = {};
				CreateProjection(shadow_frustum, 0, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[0]);
				CreateProjection(shadow_frustum, 1, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[1]);
				CreateProjection(shadow_frustum, 2, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[2]);
				CreateProjection(shadow_frustum, 3, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[3]);
				CreateProjection(shadow_frustum, 4, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[4]);

				cb.m_frust_dim = shadow_frustum.Dim();
				cb.m_frust_dim.w = m_scene->m_view.m_shadow_max_caster_dist;
				WriteConstants(dc, m_cbuf_frame, cb, EShaderType::VS|EShaderType::GS|EShaderType::PS);
			}
			
			// Loop over the elements in the draw list
			for (auto& dle : m_drawlist)
			{
				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Set the per-nugget constants
				hlsl::smap::CBufNugget cb = {};
				SetTxfm(*dle.m_instance, m_scene->m_view, cb);
				WriteConstants(dc, m_cbuf_nugget, cb, EShaderType::VS);

				Nugget const& nugget = *dle.m_nugget;
				dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}

#if SMAP_TEST
			{
				auto clip = [](float x) { return x < 0.0f; };
				auto step = [](float b, float a) { return a >= b ? 1.0f : 0.0f; };
				auto step4 = [](v4 const& b, v4 const& a)
				{
					return v4::make(
						b.x >= a.x ? 1.0f : 0.0f,
						b.y >= a.y ? 1.0f : 0.0f,
						b.z >= a.z ? 1.0f : 0.0f,
						b.w >= a.w ? 1.0f : 0.0f);
				};

				Vert verts[4] =
				{
					// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
					{pr::v4::make(-1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(0.0f,0.999f)},
					{pr::v4::make( 1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(0.999f,0.999f)},
					{pr::v4::make( 1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(0.999f,0.0f)},
					{pr::v4::make(-1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(0.0f,0.0f)},
				};
				struct
				{
					v4 ss_vert;
					v4 ws_vert;
				} vout[4];

				auto& c2w = m_scene->m_view.m_c2w;
				auto shadow_frustum = m_scene->m_view.ShadowFrustum();
				m4x4 proj;
				static int face = 0;
				CreateProjection(shadow_frustum, face, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, proj);
				
				for (int i = 0; i != 4; ++i)
				{
					vout[i].ss_vert = proj * verts[i].m_vert;
					vout[i].ss_vert.w += (vout[i].ss_vert.w == 0.0f) * pr::maths::tiny;
					vout[i].ws_vert = vout[i].ss_vert / vout[i].ss_vert.w;
					vout[i].ws_vert.w = float(face);
				}

				struct
				{
					v4 ss_vert;
					v2 depth;
					bool clipped;
				} rt[4] = {};

				for (int i = 0; i != 4; ++i)
				{
					v4 px = vout[i].ss_vert / vout[i].ss_vert.w;
					rt[i].ss_vert = px;

					// Clip to the wedge of the fwd texture we're rendering to (or no clip for the back texture)
					const float face_sign0[] = {-1.0f,  1.0f, -1.0f,  1.0f, 0.0f};
					const float face_sign1[] = { 1.0f, -1.0f, -1.0f,  1.0f, 0.0f};
					rt[i].clipped = false;
					rt[i].clipped |= clip(face_sign0[face] * (px.y - px.x));
					rt[i].clipped |= clip(face_sign1[face] * (px.y + px.x));

					rt[i].depth = v2::make((face != 4) * px.z, (face == 4) * px.z);
				}

				//auto IntersectFrustum = [&](Frustum const& frust, v4 const& s, v4 const& e)
				//{
				//	v4 const T = v4::make(1e38f);
				//	v4 d0 = frust.m_Tnorms * s;
				//	v4 d1 = frust.m_Tnorms * e;
				//	v4 t0 = step4(d1,d0)      * Min(T, -d0/(d1 - d0));        // Clip to the frustum sides
				//	float  t1 = step(e.z,s.z) * Min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

				//	float t = T.x;
				//	if (t0.x != 0) t = Min(t,t0.x);
				//	if (t0.y != 0) t = Min(t,t0.y);
				//	if (t0.z != 0) t = Min(t,t0.z);
				//	if (t0.w != 0) t = Min(t,t0.w);
				//	if (t1   != 0) t = Min(t,t1);
				//	return t;
				//};

				//auto t = IntersectFrustum(shadow_frustum, c2w.pos - c2w.z, c2w.pos - 2*c2w.z);
			}
#endif
		}

		// Create a projection transform that will take points in world space and project them
		// onto a surface parallel to the frustum plane for the given face (based on light type).
		// 'shadow_frustum' - the volume in which objects receive shadows. It should be aligned with
		//  the camera frustum but with a nearer far plane.
		// 'face' - the face index of the shadow frustum (see pr::Frustum::EPlane)
		// 'light' - the light source that we're creating the projection transform for
		// 'c2w' - the camera to world (and => shadow_frustum to world) transform
		// 'max_range' - is the maximum distance of any shadow casting object from the shadow frustum
		// plane. Effectively the projection near plane for directional lights or for point lights further
		// than this distance. Objects further than this distance don't result in pixels in the smap.
		// This should be the distance that depth information is normalised into the range [0,1) by.
		bool ShadowMap::CreateProjection(pr::Frustum const& shadow_frustum, int face, Light const& light, pr::m4x4 const& c2w, float max_range, pr::m4x4& w2s)
		{
			#define DBG_PROJ 1
			#if DBG_PROJ
			auto Dump = [&](v4 const& tl_, v4 const& tr_, v4 const& bl_, v4 const& br_, v4 const& light_)
			{
				v4 tl = tl_ / tl_.w;
				v4 tr = tr_ / tr_.w;
				v4 bl = bl_ / bl_.w;
				v4 br = br_ / br_.w;
				std::string str;
				// This is the screen space view volume for a light-camera looking at 'face' of the frustum
				pr::ldr::LineBox("view_volume", 0xFFFFFFFF, pr::v4::make(0,0,0.5f,1), pr::v4::make(2,2,1,0), str);
				pr::ldr::Box("tl", 0xFFFF0000, tl, 0.04f, str);
				pr::ldr::Box("tr", 0xFF00FF00, tr, 0.04f, str);
				pr::ldr::Box("bl", 0xFF0000FF, bl, 0.04f, str);
				pr::ldr::Box("br", 0xFFFFFF00, br, 0.04f, str);
				pr::ldr::LineD("light", 0xFFFFFF00, pr::v4Origin, light_ * max_range, str);
				pr::ldr::Write(str, "d:/dump/smap_proj_screen.ldr");
			};
			auto Frust = [&](v4 const& tl_, v4 const& tr_, v4 const& bl_, v4 const& br_, v4 const& light_)
			{
				std::string str;
				pr::ldr::Frustum("shadow_frustum", 0xffffffff, shadow_frustum, c2w * Translation4x4(0,0,-shadow_frustum.ZDist()), str);
				pr::ldr::Rect("obj", 0xFFFF00FF, 3, 2, 2, true, pr::m4x4Identity, str);
				pr::ldr::Box("tl", 0xFFFF0000, tl_, 0.04f, str);
				pr::ldr::Box("tr", 0xFF00FF00, tr_, 0.04f, str);
				pr::ldr::Box("bl", 0xFF0000FF, bl_, 0.04f, str);
				pr::ldr::Box("br", 0xFFFFFF00, br_, 0.04f, str);
				pr::ldr::Line("l", 0xFFA0A0E0, bl_, tr_, str);
				pr::ldr::Line("l", 0xFFA0A0E0, br_, tl_, str);
				pr::ldr::LineD("light", 0xFFFFFF00, pr::v4Origin, light_ * max_range, str);
				pr::ldr::Write(str, "d:/dump/smap_proj_frust.ldr");
			};
			#endif

			// TL,TR,BL,BR below refer to the corners of a quad that, for the first four faces,
			// has the camera position in the centre with two points in front of the camera and
			// two behind. (Note: in front of the camera means down the -z axis). We only care
			// about the wedge on the quad that goes from the camera to the two corners positioned
			// in front of the camera.
			float sign_z[4] =
			{
				pr::Sign<float>(face==1||face==3),
				pr::Sign<float>(face==0||face==3),
				pr::Sign<float>(face==1||face==2),
				pr::Sign<float>(face==0||face==2),
			};

			// Get the corners of the plane that will be the far clip plane (in world space).
			pr::v4 fdim = shadow_frustum.Dim();
			pr::v4 tl, TL, tr, TR, bl, BL, br, BR;
			TL = c2w * pr::v4::make(-fdim.x,  fdim.y, sign_z[0]*fdim.z, 1.0f);
			TR = c2w * pr::v4::make( fdim.x,  fdim.y, sign_z[1]*fdim.z, 1.0f);
			BL = c2w * pr::v4::make(-fdim.x, -fdim.y, sign_z[2]*fdim.z, 1.0f);
			BR = c2w * pr::v4::make( fdim.x, -fdim.y, sign_z[3]*fdim.z, 1.0f);
			if (face==0 || face == 1) { std::swap(TL,TR); std::swap(BL,BR); }
			if (face==2 || face == 3) { std::swap(TL,BL); std::swap(TR,BR); }
			PR_EXPAND(DBG_PROJ, Frust(TL,TR,BL,BR,light.m_direction));

			w2s = m4x4Zero;

			// Get the frustum normal for 'face'
			pr::v4 ws_norm = c2w * shadow_frustum.Normal(face);

			// Construct the projection transform based on the light type
			switch (light.m_type)
			{
			case ELight::Directional:
				{
					// The surface must face the light source
					if (pr::Dot3(light.m_direction, ws_norm) >= 0)
						return false;

					// This is a parallel projection, so it doesn't matter where we "position"
					// the light-camera. On the projection plane with the far plane at 0.0f and
					// the near plane at -max_range is as good as anywhere.

					// Create a light to world transform.
					pr::v4 centre = (TL + TR + BL + BR) * 0.25f;
					pr::m4x4 lt2w = pr::LookAt(centre, centre + light.m_direction, pr::Parallel(light.m_direction,c2w.y) ? c2w.z : c2w.y);
					w2s = pr::InvertFast(lt2w);

					// Create an orthographic projection
					pr::m4x4 lt2s = pr::ProjectionOrthographic(1.0f, 1.0f, -max_range, 0.0f, true);
					w2s = lt2s * w2s;

					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

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
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

					// Scale the face of the frustum into the viewport
					pr::m4x4 S = pr::Scale4x4(2.0f/(tr.x - tl.x), 2.0f/(tr.y - br.y), 1.0f, pr::v4Origin);
					w2s = S * w2s;

					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

					// Shear to make the projected plane square
					pr::m4x4 H1 = pr::Shear4x4((tl.y - tr.y)/(tr.x - tl.x), 0, 0, 0, 0, 0, pr::v4Origin);
					w2s = H1 * w2s;

					// Shear to make the projection plane perpendicular to the light direction
					pr::m4x4 H2 = pr::Shear4x4(0, 0.5f*(tl.z + bl.z) - 1.0f, 0, 0.5f*(bl.z + br.z) - 1.0f, 0, 0, pr::v4Origin);
					w2s = H2 * w2s;

					#if DBG_PROJ
					// Project the four corners of the plane
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));
					#endif
					return true;
				}
			case ELight::Spot:
			case ELight::Point:
				{
					// The surface must face the light source
					float dist_to_light = pr::Dot3(light.m_position - c2w.pos, ws_norm) + (face == 4)*shadow_frustum.ZDist();
					if (dist_to_light <= 0)
						return false;

					// Create a light to world transform
					// Position the light camera at the light position looking in the -frustum plane normal direction
					pr::m4x4 lt2w = pr::LookAt(light.m_position, light.m_position - ws_norm, pr::Parallel(ws_norm,c2w.y) ? c2w.z : c2w.y);
					w2s = pr::Invert(lt2w);
					tl = w2s * TL;
					tr = w2s * TR;
					bl = w2s * BL;
					br = w2s * BR;
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

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
					PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));
					#endif
					return true;
				}
			}
			return false;
			#undef DBG_PROJ
		}
	}
}

#if SMAP_TEST
#include "pr/renderer11/instance.h"
#include "pr/renderer11/models/model_generator.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/shaders/input_layout.h"

namespace pr
{
	namespace rdr
	{
		#define PR_RDR_INST(x)\
			x(pr::m4x4 ,m_i2w   ,EInstComp::I2WTransform)\
			x(ModelPtr ,m_model ,EInstComp::ModelPtr)
		PR_RDR_DEFINE_INSTANCE(SmapTestInstance, PR_RDR_INST);
		#undef PR_RDR_INST

		struct SmapTest
			:SmapTestInstance
			,pr::events::IRecv<Evt_UpdateScene>
			,pr::events::IRecv<Evt_SceneDestroy>
		{
			void OnEvent(pr::rdr::Evt_UpdateScene const& e)
			{
				auto rstep = e.m_scene.FindRStep<ShadowMap>();
				if (rstep == nullptr)
					return;

				if (m_model == nullptr)
				{
					{// Unit quad in Z = 0 plane
						float const t0 = 0.000f, t1 = 0.9999f;
						Vert verts[4] =
						{
							// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
							{pr::v4::make(-1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(t0,t1)},
							{pr::v4::make( 1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(t1,t1)},
							{pr::v4::make( 1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(t1,t0)},
							{pr::v4::make(-1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2::make(t0,t0)},
						};
						pr::uint16 idxs[] =
						{
							0, 1, 2, 0, 2, 3
						};
						auto bbox = pr::BBox::make(pr::v4Origin, pr::v4::make(1,1,0,0));

						MdlSettings s(verts, idxs, bbox, "smap quad");
						m_model = e.m_scene.m_rdr->m_mdl_mgr.CreateModel(s);

						NuggetProps ddata(EPrim::TriList, EGeom::Vert|EGeom::Tex0);
						ddata.m_tex_diffuse = e.m_scene.m_rdr->m_tex_mgr.CreateTexture2D(AutoId, rstep->m_tex, rstep->m_srv, SamplerDesc::PointClamp(), "smap_tex");
						m_model->CreateNugget(ddata);
						m_model->m_name = "smap test";
					}
				}

				auto& view = e.m_scene.m_view;
				float dz = -view.m_near*1.0001f;
				auto area = view.ViewArea(-dz);
				float dx = -area.x/4.0f;
				float dy = -area.y/4.0f;
				
				m_i2w = view.m_c2w * Translation4x4(dx,dy,dz) * Scale4x4(area.x/4.0f, area.y/4.0f, 1.0f, pr::v4Origin);
				e.m_scene.RStep<ForwardRender>().AddInstance(*this);
			}
			void OnEvent(pr::rdr::Evt_SceneDestroy const&)
			{
				m_model = nullptr;
			}
		} g_smap_test;
	}
}
#endif
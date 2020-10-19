//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/steps/shadow_map.h"
#include "pr/view3d/lights/light.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/render/state_stack.h"
#include "view3d/shaders/common.h"

#define SMAP_TEST 0//PR_DBG_RDR

namespace pr::rdr
{
	// Algorithm:
	// - Create 5 projection transforms from the light onto each of the planes of the shadow frustum (= view frustum with nearer far plane).
	// - Vertex shader: transform verts to world space
	// - Geometry Shader: replicates the geometry 5 times, transforming each primitive by the corresponding projection transform
	// - Pixel Shader: use the id to write the depth into the appropriate place on the render target
	// - Use no depth buffer with blend mode min to avoid depth buffer issues
	// - Render the scene writing distance-to-frustum data. (this solves the problem of directional
	//   lights being at infinity)
	// - In the lighting pass, project ray from 'ws_pos' to frustum, measure distance and compare to
	//   texture to detect shadow

	ShadowMap::ShadowMap(Scene& scene, Light& light, pr::iv2 size)
		:RenderStep(scene)
		,m_light(light)
		,m_tex()
		,m_rtv()
		,m_srv()
		,m_samp()
		,m_main_rtv()
		,m_main_dsv()
		,m_cbuf_frame (m_shdr_mgr->GetCBuf<hlsl::smap::CBufFrame >("smap::CBufFrame"))
		,m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::smap::CBufNugget>("smap::CBufNugget"))
		,m_smap_size(size)
		,m_vs(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapVS)))
		,m_ps(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapPS)))
		,m_gs_face(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapFaceGS)))
		,m_gs_line(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapLineGS)))
	{
		InitRT(size);
			
		m_dsb.Set(EDS::DepthEnable, FALSE);
		m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
		m_bsb.Set(EBS::BlendEnable, TRUE, 0);
		m_bsb.Set(EBS::BlendOp, D3D11_BLEND_OP_MAX, 0);
		m_bsb.Set(EBS::DestBlend, D3D11_BLEND_DEST_COLOR, 0);
		m_bsb.Set(EBS::SrcBlend, D3D11_BLEND_SRC_COLOR, 0);
	}

	// Create the render target for the smap
	void ShadowMap::InitRT(pr::iv2 size)
	{
		// Release any existing RTs
		m_tex = nullptr;
		m_rtv = nullptr;
		m_srv = nullptr;

		Renderer::Lock lock(m_scene->rdr());
		auto device = lock.D3DDevice();

		// Create the smap texture
		Texture2DDesc tdesc;
		tdesc.Width          = size.x;
		tdesc.Height         = size.y;
		tdesc.Format         = DXGI_FORMAT_R16G16_FLOAT; // R = z depth, G = -z depth
		tdesc.MipLevels      = 1;
		tdesc.ArraySize      = 1;
		tdesc.SampleDesc     = MultiSamp(1,0);
		tdesc.Usage          = D3D11_USAGE_DEFAULT;
		tdesc.BindFlags      = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
		tdesc.CPUAccessFlags = 0;
		tdesc.MiscFlags      = 0;
		pr::Throw(device->CreateTexture2D(&tdesc, 0, &m_tex.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(m_tex.get(), "smap tex"));

		// Get the render target view
		RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
		rtvdesc.Texture2D.MipSlice = 0;
		pr::Throw(device->CreateRenderTargetView(m_tex.get(), &rtvdesc, &m_rtv.m_ptr));

		// Get the shader res view
		ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
		srvdesc.Texture2D.MostDetailedMip = 0;
		srvdesc.Texture2D.MipLevels = 1;
		pr::Throw(device->CreateShaderResourceView(m_tex.get(), &srvdesc, &m_srv.m_ptr));

		// Create a sampler for sampling the shadow map
		auto sdesc = SamplerDesc::LinearClamp();
		pr::Throw(device->CreateSamplerState(&sdesc, &m_samp.m_ptr));
	}

	// Bind the smap RT to the output merger
	void ShadowMap::BindRT(bool bind)
	{
		Renderer::Lock lock(m_scene->rdr());
		auto dc = lock.ImmediateDC();
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
	void ShadowMap::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
	{
		Lock lock(*this);
		auto& drawlist = lock.drawlist();

		// Add a drawlist element for each nugget in the instance's model
		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
		{
			if (AllSet(nug.m_flags, ENuggetFlag::ShadowCastExclude)) continue;
			nug.AddToDrawlist(drawlist, inst, nullptr, Id);
		}

		m_sort_needed = true;
	}

	// Update the provided shader set appropriate for this render step
	void ShadowMap::ConfigShaders(ShaderSet1& ss, ETopo topo) const
	{
		ss.m_vs = m_vs.get();
		ss.m_ps = m_ps.get();
		switch (topo)
		{
		case ETopo::PointList:
			break; // Ignore point lists.. can a point cast a shadow anyway?
		case ETopo::LineList:
		case ETopo::LineStrip:
			ss.m_gs = m_gs_line.get();
			break;
		case ETopo::TriList:
		case ETopo::TriStrip:
			ss.m_gs = m_gs_face.get();
			break;
		case ETopo::LineListAdj:
		case ETopo::LineStripAdj:
		case ETopo::TriListAdj:
			//todo
			break;
		default:
			throw std::runtime_error("Unsupported primitive type");
		}
	}

	// Perform the render step
	void ShadowMap::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Sort the draw list if needed
		SortIfNeeded();

		// Bind the render target to the OM
		auto bind_smap = pr::CreateScope(
			[this]{ BindRT(true); },
			[this]{ BindRT(false); });

		// Clear the render target/depth buffer.
		// The depth data is the fractional distance between the frustum plane (0) and the light (1).
		// We only care about points in front of the frustum faces => reset depths to zero.
		dc->ClearRenderTargetView(m_rtv.m_ptr, pr::ColourZero.arr);

		// Viewport = the whole smap
		Viewport vp(UINT(m_smap_size.x), UINT(m_smap_size.y));
		dc->RSSetViewports(1, &vp);

		{// Set the frame constants
			auto& c2w = m_scene->m_view.m_c2w;
			auto shadow_frustum = m_scene->m_view.ShadowFrustum();
			auto zfar = shadow_frustum.zfar();
			auto wh = shadow_frustum.area(zfar);

			hlsl::smap::CBufFrame cb = {};
			CreateProjection(shadow_frustum, Frustum::EPlane::XPos, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[0]);
			CreateProjection(shadow_frustum, Frustum::EPlane::XNeg, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[1]);
			CreateProjection(shadow_frustum, Frustum::EPlane::YPos, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[2]);
			CreateProjection(shadow_frustum, Frustum::EPlane::YNeg, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[3]);
			CreateProjection(shadow_frustum, Frustum::EPlane::ZFar, m_light, c2w, m_scene->m_view.m_shadow_max_caster_dist, cb.m_proj[4]);

			cb.m_frust_dim = v4(0.5f * wh.x, 0.5f * wh.y, zfar, m_scene->m_view.m_shadow_max_caster_dist);
			WriteConstants(dc, m_cbuf_frame.get(), cb, EShaderType::VS|EShaderType::GS|EShaderType::PS);
		}

		// Draw each element in the draw list
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			StateStack::DleFrame frame(ss, dle);
			ss.Commit();

			auto const& nugget = *dle.m_nugget;

			// Set the per-nugget constants
			hlsl::smap::CBufNugget cb = {};
			SetTxfm(*dle.m_instance, m_scene->m_view, cb);
			WriteConstants(dc, m_cbuf_nugget.get(), cb, EShaderType::VS);

			// Draw the nugget
			dc->DrawIndexed(
				UINT(nugget.m_irange.size()),
				UINT(nugget.m_irange.m_beg),
				0);
		}

		PR_EXPAND(SMAP_TEST, Debugging(ss));
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
	bool ShadowMap::CreateProjection(pr::Frustum const& shadow_frustum, Frustum::EPlane face, Light const& light, pr::m4x4 const& c2w, float max_range, pr::m4x4& w2s)
	{
		#define DBG_PROJ 0 //PR_DBG_RDR
		#if DBG_PROJ
		auto Dump = [&](v4 const& tl_, v4 const& tr_, v4 const& bl_, v4 const& br_, v4 const& light_)
		{
			v4 tl = tl_ / tl_.w;
			v4 tr = tr_ / tr_.w;
			v4 bl = bl_ / bl_.w;
			v4 br = br_ / br_.w;
			std::string str;
			// This is the screen space view volume for a light-camera looking at 'face' of the frustum
			pr::ldr::LineBox("view_volume", 0xFFFFFFFF, pr::v4(0,0,0.5f,1), pr::v4(2,2,1,0), str);
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
			pr::SignF(face == Frustum::EPlane::XNeg || face == Frustum::EPlane::YNeg),
			pr::SignF(face == Frustum::EPlane::XPos || face == Frustum::EPlane::YNeg),
			pr::SignF(face == Frustum::EPlane::XNeg || face == Frustum::EPlane::YPos),
			pr::SignF(face == Frustum::EPlane::XPos || face == Frustum::EPlane::YPos),
		};

		// Get the corners of the plane that will be the far clip plane (in world space).
		// Note, the non-far plane faces of the frustum will actually result in a left
		// handed projection because I'm just reflecting the verts through the z-plane.
		// To compensate for this, the geometry shader reverses the winding order of the
		// faces. The reason for doing this is to simplify texture lookup, pixels on the
		// left of the screen will be on the left of the texture, rather than on the right.
		auto zfar = shadow_frustum.zfar();
		auto wh = shadow_frustum.area(zfar);
		auto fdim = v4(0.5f * wh.x, 0.5f * wh.y, zfar, 0);
		v4 tl, TL, tr, TR, bl, BL, br, BR;
		TL = c2w * v4(-fdim.x,  fdim.y, sign_z[0]*fdim.z, 1.0f);
		TR = c2w * v4( fdim.x,  fdim.y, sign_z[1]*fdim.z, 1.0f);
		BL = c2w * v4(-fdim.x, -fdim.y, sign_z[2]*fdim.z, 1.0f);
		BR = c2w * v4( fdim.x, -fdim.y, sign_z[3]*fdim.z, 1.0f);
		PR_EXPAND(DBG_PROJ, Frust(TL,TR,BL,BR,light.m_direction));

		w2s = m4x4Zero;

		// Get the frustum normal for 'face'
		v4 ws_norm = c2w * shadow_frustum.face_normal(face);

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
				auto centre = (TL + TR + BL + BR) * 0.25f;
				auto lt2w = m4x4::LookAt(centre, centre + light.m_direction, Parallel(light.m_direction,c2w.y) ? c2w.z : c2w.y);
				w2s = InvertFast(lt2w);

				// Create an orthographic projection
				auto lt2s = m4x4::ProjectionOrthographic(1.0f, 1.0f, -max_range, 0.0f, true);
				w2s = lt2s * w2s;

				// Project the four corners of the plane
				tl = w2s * TL;
				tr = w2s * TR;
				bl = w2s * BL;
				br = w2s * BR;
				PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

				// Rotate so that TL is above BL and TR is above BR (i.e. the left and right edges are vertical)
				auto ledge = Normalise((tl - bl).xy);
				auto R = m4x4Identity;
				R.x = v4( ledge.y,  ledge.x, 0, 0);
				R.y = v4(-ledge.x,  ledge.y, 0, 0);
				w2s = R * w2s;

				// Project the four corners of the plane
				tl = w2s * TL;
				tr = w2s * TR;
				bl = w2s * BL;
				br = w2s * BR;
				PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

				// Scale the face of the frustum into the viewport
				auto S = m4x4::Scale(2.0f/(tr.x - tl.x), 2.0f/(tr.y - br.y), 1.0f, v4Origin);
				w2s = S * w2s;

				// Project the four corners of the plane
				tl = w2s * TL;
				tr = w2s * TR;
				bl = w2s * BL;
				br = w2s * BR;
				PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

				// Shear to make the projected plane square
				auto H1 = m4x4::Shear((tl.y - tr.y)/(tr.x - tl.x), 0, 0, 0, 0, 0, v4Origin);
				w2s = H1 * w2s;

				// Shear to make the projection plane perpendicular to the light direction
				auto H2 = m4x4::Shear(0, 0.5f*(tl.z + bl.z) - 1.0f, 0, 0.5f*(bl.z + br.z) - 1.0f, 0, 0, v4Origin);
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
				float dist_to_light = Dot3(light.m_position - c2w.pos, ws_norm) + (face == Frustum::EPlane::ZFar)*shadow_frustum.zfar();
				if (dist_to_light <= 0)
					return false;

				// Create a light to world transform
				// Position the light camera at the light position looking in the -frustum plane normal direction
				auto lt2w = m4x4::LookAt(light.m_position, light.m_position - ws_norm, Parallel(ws_norm,c2w.y) ? c2w.z : c2w.y);
				w2s = Invert(lt2w);
				tl = w2s * TL;
				tr = w2s * TR;
				bl = w2s * BL;
				br = w2s * BR;
				PR_EXPAND(DBG_PROJ, Dump(tl,tr,bl,br,w2s*light.m_direction));

				// Create a perspective projection
				float zr = 0.001f, zf = dist_to_light, zn = zf*zr;
				auto lt2s = m4x4::ProjectionPerspective(tl.x*zr, tr.x*zr, tl.y*zr, bl.y*zr, zn, zf, true);
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

#if SMAP_TEST
#include "pr/view3d/instance.h"
#include "pr/view3d/models/model_generator.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/steps/forward_render.h"
#include "pr/view3d/shaders/input_layout.h"

namespace pr
{
	namespace rdr
	{
		// A model instance that draws a quad on the lower left of the view
		// containing the shadow map texture
		#define PR_RDR_INST(x)\
			x(pr::m4x4 ,m_i2w   ,EInstComp::I2WTransform)\
			x(ModelPtr ,m_model ,EInstComp::ModelPtr)
		PR_RDR_DEFINE_INSTANCE(SmapTestInstance, PR_RDR_INST);
		#undef PR_RDR_INST
		struct SmapTest
			:SmapTestInstance
			,pr::events::IRecv<Evt_UpdateScene>
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
							{pr::v4(-1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(t0,t1)},
							{pr::v4( 1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(t1,t1)},
							{pr::v4( 1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(t1,t0)},
							{pr::v4(-1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(t0,t0)},
						};
						pr::uint16 idxs[] =
						{
							0, 1, 2, 0, 2, 3
						};
						auto bbox = pr::BBox(pr::v4Origin, pr::v4(1,1,0,0));

						MdlSettings s(verts, idxs, bbox, "smap quad");
						m_model = e.m_scene.m_rdr->m_mdl_mgr.CreateModel(s);

						NuggetProps ddata(ETopo::TriList, EGeom::Vert|EGeom::Tex0);
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
		} g_smap_test;

		typedef pr::iv4 int4;
		typedef pr::v4 float4;
		typedef pr::v2 float2;
		typedef pr::m4x4 float4x4;
		#define uniform
		#define TINY 0.0001f

		struct SLight
		{
			// x = light type = 0 - ambient, 1 - directional, 2 - point, 3 - spot
			int4   m_info;         // Encoded info for global lighting
			float4 m_ws_direction; // The direction of the global light source
			float4 m_ws_position;  // The position of the global light source
			float4 m_ambient;      // The colour of the ambient light
			float4 m_colour;       // The colour of the directional light
			float4 m_specular;     // The colour of the specular light. alpha channel is specular power
			float4 m_spot;         // x = inner cos angle, y = outer cos angle, z = range, w = falloff
		};

		struct SShadow
		{
			int4 m_info;                // x = count of smaps
			float4 m_frust_dim;         // x = width at far plane, y = height at far plane, z = distance to far plane, w = smap max range (for normalising distances)
			row_major float4x4 m_frust; // Inward pointing frustum plane normals, transposed.
		};
		struct SSampler
		{
		} m_smap_sampler[1];
		struct STexture
		{
			float2 Sample(SSampler const& s, float2 const& uv)
			{
				return float2(0.6f,0.4f);
			}
		} m_smap_texture[1];

		// Shader intrinic functions
		bool clip(float x)
		{
			return x < 0.0f;
		};
		float step(float lo, float hi)
		{
			return hi >= lo ? 1.0f : 0.0f;
		};
		v4 step(v4 const& lo, v4 const& hi)
		{
			return v4(
				hi.x >= lo.x ? 1.0f : 0.0f,
				hi.y >= lo.y ? 1.0f : 0.0f,
				hi.z >= lo.z ? 1.0f : 0.0f,
				hi.w >= lo.w ? 1.0f : 0.0f);
		}
		v4 lerp(v4 const& a, v4 const& b, float t)
		{
			return (1-t)*a + t*b;
		}
		float saturate(float x)
		{
			return Clamp(x,0.0f,1.0f);
		}
		float length(v4 const& v)
		{
			return Length4(v);
		}
		v4 mul(v4 const& v, m4x4 const& m)
		{
			return m * v;
		}

		// Returns a direction vector of the shadow cast from point 'ws_vert' 
		float4 ShadowRayWS(uniform SLight const& light, float4 const& ws_pos)
		{
			return DirectionalLight(light) ? light.m_ws_direction : (ws_pos - light.m_ws_position);
		}

		// Returns the parametric value 't' of the intersection between the line
		// passing through 's' and 'e' with 'frust'.
		// Assumes 's' is within the frustum to start with
		float IntersectFrustum(uniform SShadow const& shadow, float4 const& s, float4 const& e)
		{
			const float4 T = float4(1e10f);
	
			// Find the distance from each frustum face for 's' and 'e'
			float4 d0 = mul(s, shadow.m_frust);
			float4 d1 = mul(e, shadow.m_frust);

			// Clip the edge 's-e' to each of the frustum sides (Actually, find the parametric
			// value of the intercept)(min(T,..) protects against divide by zero)
			float4 t0 = step(d1,d0)   * Min(T, -d0/(d1 - d0));        // Clip to the frustum sides
			float  t1 = step(e.z,s.z) * Min(T.x, -s.z / (e.z - s.z)); // Clip to the far plane

			// Set all components that are <= 0.0 to BIG
			t0 += step(t0, v4Zero) * T;
			t1 += step(t1, 0.0f) * T.x;

			// Find the smallest positive parametric value
			// => the closest intercept with a frustum plane
			float t = T.x;
			t = Min(t, t0.x);
			t = Min(t, t0.y);
			t = Min(t, t0.z);
			t = Min(t, t0.w);
			t = Min(t, t1);
			return t;
		}

		// Returns a value between [0,1] where 0 means fully in shadow, 1 means not in shadow
		float LightVisibility(uniform SShadow const& shadow, uniform int smap_index, uniform SLight const& light, uniform row_major float4x4 const& w2c, float4 const& ws_pos)
		{
			// Find the shadow ray in frustum space and its intersection with the frustum
			float4 ws_ray = ShadowRayWS(light, ws_pos);
			float4 fs_pos0 = mul(ws_pos         , w2c); fs_pos0.z += shadow.m_frust_dim.z;
			float4 fs_pos1 = mul(ws_pos + ws_ray, w2c); fs_pos1.z += shadow.m_frust_dim.z;
			float t = IntersectFrustum(shadow, fs_pos0, fs_pos1);
			float4 intercept = lerp(fs_pos0, fs_pos1, t);

			// Convert the intersection to texture space
			float2 uv = float2(0.5f + 0.5f*intercept.x/shadow.m_frust_dim.x, 0.5f - 0.5f*intercept.y/shadow.m_frust_dim.y);

			// Find the distance from the frustum to 'ws_pos'
			float dist = saturate(t * length(ws_ray) / shadow.m_frust_dim.w) + TINY;

			// Sample the smap
			float2 depth = m_smap_texture[smap_index].Sample(m_smap_sampler[smap_index], uv);

			// R channel is near frustum faces, G channel is the far frustum plane.
			// If intercept.z is 0 then the intercept is on the far plane.
			return step(TINY, intercept.z) * depth.x + step(intercept.z, TINY) * depth.y;
		}

		void ShadowMap::Debugging(StateStack& ss)
		{
			auto shadow_frustum = m_scene->m_view.ShadowFrustum();
			SShadow shadow = {};
			shadow.m_info = pr::iv4(1,0,0,0);
			shadow.m_frust = shadow_frustum.m_Tnorms;
			shadow.m_frust_dim = shadow_frustum.Dim();
			shadow.m_frust_dim.w = m_scene->m_view.m_shadow_max_caster_dist;

			SLight light = {};
			light.m_info = pr::iv4(1,0,0,0);
			light.m_ws_direction = m_scene->m_global_light.m_direction;
			light.m_ws_position  = m_scene->m_global_light.m_position;

			LightVisibility(shadow, 0, light, InvertFast(m_scene->m_view.m_c2w), pr::v4(0,0,0,1));

			/*
			Vert verts[4] =
			{
				// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
				{pr::v4(-1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(0.0f,0.999f)},
				{pr::v4( 1.0f, -1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(0.999f,0.999f)},
				{pr::v4( 1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(0.999f,0.0f)},
				{pr::v4(-1.0f,  1.0f, 0, 1), pr::ColourWhite, pr::v4Zero, pr::v2(0.0f,0.0f)},
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
				vout[i].ss_vert.w += (vout[i].ss_vert.w == 0.0f) * pr::maths::tinyf;
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

				rt[i].depth = v2((face != 4) * px.z, (face == 4) * px.z);
			}
			*/
		}
	}
}

#endif
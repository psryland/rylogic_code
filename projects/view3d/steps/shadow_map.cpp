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

#define PR_DBG_SMAP 0 //PR_DBG
#if PR_DBG_SMAP
#pragma message(PR_LINK "WARNING: ************************************************** Shadow Map debugging enabled")
#include "pr/ldraw/ldr_helper.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/models/model_generator.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/steps/forward_render.h"
#include "pr/view3d/shaders/input_layout.h"
namespace pr::rdr
{
	#define PR_RDR_INST(x)\
		x(m4x4     ,m_i2w   ,EInstComp::I2WTransform)\
		x(m4x4     ,m_c2s   ,EInstComp::C2STransform)\
		x(ModelPtr ,m_model ,EInstComp::ModelPtr)
	PR_RDR_DEFINE_INSTANCE(SmapTestInstance, PR_RDR_INST);
	#undef PR_RDR_INST
	
	// A model instance that draws a quad on the lower left
	// of the view containing the shadow map texture.
	struct SmapQuad :SmapTestInstance
	{
		ModelPtr Create(Renderer& rdr, ShadowMap::ShadowCaster const& caster)
		{
			// Unit quad in Z = 0 plane
			Vert const verts[4] =
			{
				{v4( 0.0f, 0.0f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.9999f)},
				{v4( 1.0f, 0.0f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.9999f)},
				{v4( 1.0f, 1.0f, 0, 1), ColourWhite, v4ZAxis, v2(0.9999f,0.0000f)},
				{v4( 0.0f, 1.0f, 0, 1), ColourWhite, v4ZAxis, v2(0.0000f,0.0000f)},
			};
			uint16_t const idxs[] =
			{
				0, 1, 2, 0, 2, 3
			};
			BBox const bbox(v4Origin, v4(1,1,0,0));

			MdlSettings s(verts, idxs, bbox, "smap quad");
			auto model = rdr.m_mdl_mgr.CreateModel(s);

			NuggetProps n(ETopo::TriList, EGeom::Vert|EGeom::Tex0);
			n.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, caster.m_tex.get(), caster.m_srv.get(), SamplerDesc::PointClamp(), false, "smap_tex");
			n.m_flags = SetBits(n.m_flags, ENuggetFlag::ShadowCastExclude, true);
			model->CreateNugget(n);
			return model;
		}
		void Update(Scene& scene, ShadowMap::ShadowCaster const& caster)
		{
			// Lazy create the model
			m_model = m_model ? m_model : Create(scene.rdr(), caster);

			// Screen space is [-0.5,+0.5]
			m_c2s = m4x4::ProjectionOrthographic(1.0f, 1.0f, -0.01f, 1000.0f, true);

			// Scale the unit quad and position in the lower left
			m_i2w = scene.m_view.CameraToWorld() * m4x4::Scale(0.2f, v4{-0.495f, -0.495f, 0, 1});
			scene.RStep<ForwardRender>().AddInstance(*this);
		}
	} g_smap_quad;
}
#endif

namespace pr::rdr
{
	ShadowMap::ShadowCaster::ShadowCaster(ID3D11Device* device, Light const& light, iv2 size, DXGI_FORMAT format)
		: m_params()
		, m_light(&light)
		, m_tex()
		, m_rtv()
		, m_srv()
	{
		// Release any existing RTs
		m_tex = nullptr;
		m_rtv = nullptr;
		m_srv = nullptr;

		// Create the smap texture
		Texture2DDesc tdesc;
		tdesc.Width = size.x;
		tdesc.Height = size.y;
		tdesc.Format = format;
		tdesc.MipLevels = 1;
		tdesc.ArraySize = 1;
		tdesc.SampleDesc = MultiSamp(1, 0);
		tdesc.Usage = D3D11_USAGE_DEFAULT;
		tdesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
		tdesc.CPUAccessFlags = 0;
		tdesc.MiscFlags = 0;//D3D11_RESOURCE_MISC_TEXTURECUBE;
		Throw(device->CreateTexture2D(&tdesc, 0, &m_tex.m_ptr));
		PR_EXPAND(PR_DBG_RDR, NameResource(m_tex.get(), "smap"));

		// Get the render target view
		RenderTargetViewDesc rtvdesc(tdesc.Format, D3D11_RTV_DIMENSION_TEXTURE2D);
		Throw(device->CreateRenderTargetView(m_tex.get(), &rtvdesc, &m_rtv.m_ptr));

		// Get the shader res view
		ShaderResourceViewDesc srvdesc(tdesc.Format, D3D11_SRV_DIMENSION_TEXTURE2D);
		srvdesc.Texture2D.MipLevels = tdesc.MipLevels;
		Throw(device->CreateShaderResourceView(m_tex.get(), &srvdesc, &m_srv.m_ptr));
	}

	// Update the projection parameters for the given scene
	void ShadowMap::ShadowCaster::UpdateParams(Scene const& scene, BBox_cref ws_bounds)
	{
		auto& c2w = scene.m_view.m_c2w;
		auto l2w = m_light->LightToWorld(ws_bounds.Centre(), 0.5f * ws_bounds.Diametre(), c2w);

		// Get the scene bounds in light space
		auto ls_bounds = InvertFast(l2w) * ws_bounds;

		// Inflate the bounds slightly so that the edge of the smap is avoided
		ls_bounds.m_radius *= 1.01f;

		// Create a projection that encloses the scene bounds. This is basically "c2s"
		auto zn = -ls_bounds.Centre().z - ls_bounds.Radius().z;
		auto zf = -ls_bounds.Centre().z + ls_bounds.Radius().z;
		auto l2s = m_light->Projection(zn, zf, ls_bounds.SizeX(), ls_bounds.SizeY(), Length(ls_bounds.Centre() - l2w.pos));

		// Save the projection values for later render steps
		m_params.m_bounds = ls_bounds;
		m_params.m_w2l = InvertFast(l2w);
		m_params.m_l2s = l2s;
	}

	// **************************

	ShadowMap::ShadowMap(Scene& scene, Light const& light, iv2 size, DXGI_FORMAT format)
		: RenderStep(scene)
		, m_caster()
		, m_samp()
		, m_main_rtv()
		, m_main_dsv()
		, m_cbuf_frame(m_shdr_mgr->GetCBuf<hlsl::smap::CBufFrame >("smap::CBufFrame"))
		, m_cbuf_nugget(m_shdr_mgr->GetCBuf<hlsl::smap::CBufNugget>("smap::CBufNugget"))
		, m_smap_format(format)
		, m_smap_size(size)
		, m_bbox_scene(BBoxReset)
		, m_vs(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapVS)))
		, m_ps(m_shdr_mgr->FindShader(RdrId(EStockShader::ShadowMapPS)))
	{
		m_dsb.Set(EDS::DepthEnable, FALSE);
		m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
		m_bsb.Set(EBS::BlendEnable, TRUE, 0);
		m_bsb.Set(EBS::BlendOp, D3D11_BLEND_OP_MAX, 0);
		m_bsb.Set(EBS::DestBlend, D3D11_BLEND_DEST_COLOR, 0);
		m_bsb.Set(EBS::SrcBlend, D3D11_BLEND_SRC_COLOR, 0);

		// Create a sampler for sampling the shadow map
		{
			Renderer::Lock lock(m_scene->rdr());
			auto sdesc = SamplerDesc::LinearClamp();
			Throw(lock.D3DDevice()->CreateSamplerState(&sdesc, &m_samp.m_ptr));
		}

		AddLight(light);
	}

	// Add a shadow casting light source
	void ShadowMap::AddLight(Light const& light)
	{
		Renderer::Lock lock(m_scene->rdr());
		m_caster.emplace_back(lock.D3DDevice(), light, m_smap_size, m_smap_format);
	}

	// Bind the smap RT to the output merger
	void ShadowMap::BindRT(ShadowCaster const* caster)
	{
		Renderer::Lock lock(m_scene->rdr());
		auto dc = lock.ImmediateDC();
		if (caster != nullptr)
		{
			// Save a reference to the main render target/depth buffer
			dc->OMGetRenderTargets(1, &m_main_rtv.m_ptr, &m_main_dsv.m_ptr);

			// Bind the smap RT to the OM
			dc->OMSetRenderTargets(1, &caster->m_rtv.m_ptr, nullptr);
		}
		else
		{
			// Restore the main RT and depth buffer
			dc->OMSetRenderTargets(1, &m_main_rtv.m_ptr, m_main_dsv.m_ptr);

			// Release our reference to the main rtv/dsv
			m_main_rtv = nullptr;
			m_main_dsv = nullptr;

			#if PR_DBG_SMAP
			// Save the shadow map texture to disk
			//DirectX::SaveToWICFile();
			#endif
		}
	}

	// Update the provided shader set to the shaders required by this render step
	void ShadowMap::ConfigShaders(ShaderSet1& ss, ETopo) const
	{
		ss.m_vs = m_vs.get();
		ss.m_ps = m_ps.get();
	}

	// Reset the drawlist
	void ShadowMap::ClearDrawlist()
	{
		RenderStep::ClearDrawlist();
		m_bbox_scene = BBoxReset;
	}

	// Add model nuggets to the draw list for this render step
	void ShadowMap::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets)
	{
		Lock lock(*this);
		auto& drawlist = lock.drawlist();
		drawlist.reserve(drawlist.size() + nuggets.size());

		// Add a drawlist element for each nugget in the instance's model
		Model const* model = nullptr;
		for (auto& nug : nuggets)
		{
			if (AllSet(nug.m_flags, ENuggetFlag::ShadowCastExclude)) continue;
			nug.AddToDrawlist(drawlist, inst, nullptr, Id);
			model = nug.m_owner;
		}

		// Grow the scene bounds if nuggets were added
		if (model != nullptr && model->m_bbox != BBoxReset)
		{
			auto i2w = GetO2W(inst);
			auto bbox = i2w * model->m_bbox;
			assert(bbox.valid() && "Model bounding box is invalid");
			Grow(m_bbox_scene, bbox);
		}

		m_sort_needed = true;
	}

	// Perform the render step
	void ShadowMap::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Determine the bounds for the shadow map.
		// Trim away objects that cannot cast shadows into the view frustum. (todo)
		// Nothing to render if there are no objects
		if (!m_bbox_scene.valid())
			return;

		// Sort the draw list if needed
		SortIfNeeded();

		// Render the shadow map for each shadow caster
		for (auto& caster : m_caster)
		{
			// Bind the smap as the render target
			auto bind_smap = CreateScope(
				[=] { BindRT(&caster); },
				[=] { BindRT(nullptr); });

			// Clear the render target
			constexpr float reset[] = {0,0,0,0};
			dc->ClearRenderTargetView(caster.m_rtv.m_ptr, reset);

			// Viewport = the whole smap
			Viewport vp(UINT(m_smap_size.x), UINT(m_smap_size.y));
			dc->RSSetViewports(1, &vp);

			// Set the frame constants
			caster.UpdateParams(*m_scene, m_bbox_scene);

			// Set up the smap shader
			{
				hlsl::smap::CBufFrame cb = {};
				cb.m_w2l = caster.m_params.m_w2l;
				cb.m_l2s = caster.m_params.m_l2s;
				WriteConstants(dc, m_cbuf_frame.get(), cb, EShaderType::VS | EShaderType::PS);
			}

			// Output the camera, light position, scene bounds, and smap projection.
			#if PR_DBG_SMAP
			{
				auto& c2w = m_scene->m_view.m_c2w;
				auto cam_zn = m_scene->m_view.Near(false);
				auto cam_zf = m_scene->m_view.FocusDist() * 2;
				auto l2w = InvertFast(caster.m_params.m_w2l);
				auto bounds = caster.m_params.m_bounds;
				auto& l2s = caster.m_params.m_l2s;
				auto s = cam_zf * 0.05f; // scale the light gfx based on frustum size

				ldr::Builder b;
				b.Box("scene_bounds", 0xFF0000FF).bbox(m_bbox_scene).wireframe();
				b.Frustum("camera_view", 0xFF00FFFF).nf(cam_zn, cam_zf).fov(m_scene->m_view.FovY(), m_scene->m_view.Aspect()).o2w(c2w).wireframe().axis(AxisId::NegZ);
				auto& blight =
					(caster.m_light->m_type == ELight::Directional) ? b.Cylinder("light", 0xFFFFFF00).hr(s*1.6f, s*0.4f, s*0.1f).o2w(l2w) :
					(caster.m_light->m_type == ELight::Spot       ) ? b.Cylinder("light", 0xFFFFFF00).hr(s*1.6f, s*0.4f, s*0.1f).o2w(l2w) :
					(caster.m_light->m_type == ELight::Point      ) ? (ldr::fluent::LdrObj&)b.Sphere("light", 0xFFFFFF00).r(s*0.3f).o2w(l2w) :
					throw std::runtime_error("Unsupported light type");
				blight.Box("light_bounds", 0xFFFFFF00).bbox(bounds).wireframe();
				blight.Frustum("light_proj", 0xFFFF00FF).proj(l2s).wireframe();
				b.Write("P:\\dump\\smap_view.ldr");
			}
			#endif

			// Draw each element in the draw list
			Lock lock(*this);
			for (auto& dle : lock.drawlist())
			{
				StateStack::DleFrame frame(ss, dle);
				auto const& nugget = *dle.m_nugget;

				// Set the per-nugget constants
				hlsl::smap::CBufNugget cb = {};
				SetTxfm(*dle.m_instance, m_scene->m_view, cb);
				WriteConstants(dc, m_cbuf_nugget.get(), cb, EShaderType::VS);

				// Draw the nugget
				DrawNugget(dc, nugget, ss);
			}

			// Debugging
			PR_EXPAND(PR_DBG_SMAP, g_smap_quad.Update(*m_scene, caster));
		}
	}

	// Call draw for a nugget
	void ShadowMap::DrawNugget(ID3D11DeviceContext* dc, Nugget const& nugget, StateStack& ss)
	{
		ss.Commit();
		if (nugget.m_irange.empty())
		{
			dc->Draw(
				UINT(nugget.m_vrange.size()),
				UINT(nugget.m_vrange.m_beg));
		}
		else
		{
			dc->DrawIndexed(
				UINT(nugget.m_irange.size()),
				UINT(nugget.m_irange.m_beg),
				0);
		}
	}
}

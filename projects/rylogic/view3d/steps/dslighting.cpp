//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "pr/view3d/forward.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/steps/dslighting.h"
#include "pr/view3d/steps/gbuffer.h"
#include "pr/view3d/util/stock_resources.h"
#include "view3d/shaders/common.h"
#include "view3d/render/state_stack.h"

namespace pr::rdr
{
	DSLighting::DSLighting(Scene& scene)
		:RenderStep(scene)
		,m_unit_quad()
		,m_gbuffer(scene.RStep<GBuffer>())
		,m_cbuf_camera  (m_shdr_mgr->GetCBuf<hlsl::ds::CBufCamera  >("ds::CBufCamera"))
		,m_cbuf_lighting(m_shdr_mgr->GetCBuf<hlsl::ds::CBufLighting>("ds::CBufLighting"))
		,m_vs(m_shdr_mgr->FindShader(RdrId(EStockShader::DSLightingVS)))
		,m_ps(m_shdr_mgr->FindShader(RdrId(EStockShader::DSLightingPS)))
	{
		{// Unit quad in Z = 0 plane
			float const t0 = 0.000f, t1 = 0.9999f;
			Vert verts[4] =
			{
				// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
				{v4(0.01f, 0, 0, 0), ColourWhite, v4Zero, v2(t0,t1)},
				{v4(1.01f, 0, 0, 0), ColourWhite, v4Zero, v2(t1,t1)},
				{v4(2.01f, 0, 0, 0), ColourWhite, v4Zero, v2(t1,t0)},
				{v4(3.01f, 0, 0, 0), ColourWhite, v4Zero, v2(t0,t0)},
			};
			uint16_t idxs[] =
			{
				0, 1, 2, 0, 2, 3
			};
			auto bbox = BBox(v4Origin, v4(1,1,0,0));

			MdlSettings s(verts, idxs, bbox, "unit quad");
			m_unit_quad.m_model = scene.m_wnd->mdl_mgr().CreateModel(s);

			NuggetProps ddata(ETopo::TriList, EGeom::Vert);
			ddata.m_smap[Id].m_vs = m_vs;
			ddata.m_smap[Id].m_ps = m_ps;
			m_unit_quad.m_model->CreateNugget(ddata);
		}

		// Disable Z-buffer
		m_dsb.Set(EDS::DepthEnable, false);
		m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
	}

	// Set the position of the four corners of the view frustum in camera space
	void SetFrustumCorners(SceneView const& view, hlsl::ds::CBufCamera& cb)
	{
		auto corner = Corners(view.ViewFrustum(), 1.0f);
		cb.m_frustum[0] = corner.x;
		cb.m_frustum[1] = corner.y;
		cb.m_frustum[2] = corner.z;
		cb.m_frustum[3] = corner.w;
	}

	// Perform the render step
	void DSLighting::ExecuteInternal(StateStack& ss)
	{
		auto dc = ss.m_dc;

		// Sort the draw list if needed
		SortIfNeeded();

		// Set the viewport
		dc->RSSetViewports(1, &m_scene->m_viewport);

		{// Set camera constants
			hlsl::ds::CBufCamera cb = {};
			SetViewConstants(m_scene->m_view, cb.m_cam);
			SetFrustumCorners(m_scene->m_view, cb);
			WriteConstants(dc, m_cbuf_camera.get(), cb, EShaderType::VS|EShaderType::PS);
		}
		{// Set lighting constants
			hlsl::ds::CBufLighting cb = {};
			SetLightingConstants(m_scene->m_global_light, m_scene->m_view, cb.m_light);
			WriteConstants(dc, m_cbuf_lighting.get(), cb, EShaderType::PS);
		}

		// Draw the full screen quad
		{
			// Bind the shader to the device
			DrawListElement dle;
			dle.m_nugget   = &m_unit_quad.m_model->m_nuggets.front();
			dle.m_instance = &m_unit_quad.m_base;
			dle.m_sort_key = SortKey();

			StateStack::DleFrame frame(ss, dle);
			ss.Commit();

			// Add the nugget to the device context
			dc->DrawIndexed(
				UINT(dle.m_nugget->m_irange.size()),
				UINT(dle.m_nugget->m_irange.m_beg),
				0);
		}
	}
}

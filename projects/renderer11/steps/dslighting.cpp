//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/steps/dslighting.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/util/stock_resources.h"
#include "renderer11/shaders/common.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		DSLighting::DSLighting(Scene& scene)
			:RenderStep(scene)
			,m_gbuffer(scene.RStep<GBuffer>())
			,m_cbuf_camera(m_shdr_mgr->GetCBuf<ds::CBufCamera>("ds::CBufCamera"))
			,m_cbuf_lighting(m_shdr_mgr->GetCBuf<ds::CBufLighting>("ds::CBufLighting"))
			,m_unit_quad()
			,m_vs(m_shdr_mgr->FindShader(EStockShader::DSLightingVS))
			,m_ps(m_shdr_mgr->FindShader(EStockShader::DSLightingPS))
		{
			{// Unit quad in Z = 0 plane
				float const t0 = 0.000f, t1 = 0.9999f;
				Vert verts[4] =
				{
					// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
					{pr::v4::make(0.01f, 0, 0, 0), pr::ColourWhite, pr::v4Zero, pr::v2::make(t0,t1)},
					{pr::v4::make(1.01f, 0, 0, 0), pr::ColourWhite, pr::v4Zero, pr::v2::make(t1,t1)},
					{pr::v4::make(2.01f, 0, 0, 0), pr::ColourWhite, pr::v4Zero, pr::v2::make(t1,t0)},
					{pr::v4::make(3.01f, 0, 0, 0), pr::ColourWhite, pr::v4Zero, pr::v2::make(t0,t0)},
				};
				pr::uint16 idxs[] =
				{
					0, 1, 2, 0, 2, 3
				};
				auto bbox = pr::BBox::make(pr::v4Origin, pr::v4::make(1,1,0,0));

				MdlSettings s(verts, idxs, bbox, "unit quad");
				m_unit_quad.m_model = scene.m_rdr->m_mdl_mgr.CreateModel(s);

				NuggetProps ddata(EPrim::TriList, EGeom::Vert);
				ddata.m_smap[Id].m_vs = m_vs;
				ddata.m_smap[Id].m_ps = m_ps;
				m_unit_quad.m_model->CreateNugget(ddata);
			}

			// Disable Z-buffer
			m_dsb.Set(EDS::DepthEnable, false);
			m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
		}

		// Set the position of the four corners of the view frustum in camera space
		void SetFrustumCorners(SceneView const& view, ds::CBufCamera& cb)
		{
			pr::GetCorners(view.Frustum(), cb.m_frustum, 1.0f);
		}

		// Perform the render step
		void DSLighting::ExecuteInternal(StateStack& ss)
		{
			auto& dc = ss.m_dc;

			// Sort the draw list if needed
			SortIfNeeded();

			// Set the viewport
			dc->RSSetViewports(1, &m_scene->m_viewport);

			{// Set camera constants
				ds::CBufCamera cb = {};
				SetViewConstants(m_scene->m_view, cb);
				SetFrustumCorners(m_scene->m_view, cb);
				WriteConstants(dc, m_cbuf_camera, cb, EShaderType::VS|EShaderType::PS);
			}
			{// Set lighting constants
				ds::CBufLighting cb = {};
				SetLightingConstants(m_scene->m_global_light, cb);
				WriteConstants(dc, m_cbuf_lighting, cb, EShaderType::PS);
			}

			// Draw the full screen quad
			{
				// Bind the shader to the device
				DrawListElement dle;
				dle.m_nugget   = &m_unit_quad.m_model->m_nuggets.front();
				dle.m_instance = &m_unit_quad.m_base;
				dle.m_sort_key = 0;

				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Add the nugget to the device context
				dc->DrawIndexed(
					UINT(dle.m_nugget->m_irange.size()),
					UINT(dle.m_nugget->m_irange.m_begin),
					0);
			}
		}
	}
}

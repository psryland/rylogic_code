//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/steps/deferred/dslighting.h"
#include "pr/renderer11/steps/deferred/gbuffer.h"
#include "renderer11/steps/deferred/ds_shader.h"
#include "renderer11/steps/common.h"
#include "renderer11/util/internal_resources.h"
#include "renderer11/render/state_stack.h"

namespace pr
{
	namespace rdr
	{
		DSLighting::DSLighting(Scene& scene)
			:RenderStep(scene)
			,m_gbuffer(scene.RStep<GBuffer>())
			,m_cbuf_camera()
			,m_cbuf_lighting()
			,m_unit_quad()
			,m_shader(scene.m_rdr->m_shdr_mgr.FindShader(ERdrShader::DSLighting))
		{
			{// Unit quad in Z = 0 plane
				float const t0 = 0.000f, t1 = 0.9999f;
				VertPCNT verts[4] =
				{
					// Encode the view frustum corner index in 'pos.x', biased for the float to int cast
					{pr::v3::make(0.01f, 0, 0), pr::ColourWhite, pr::v3ZAxis, pr::v2::make(t0,t1)},
					{pr::v3::make(1.01f, 0, 0), pr::ColourWhite, pr::v3ZAxis, pr::v2::make(t1,t1)},
					{pr::v3::make(2.01f, 0, 0), pr::ColourWhite, pr::v3ZAxis, pr::v2::make(t1,t0)},
					{pr::v3::make(3.01f, 0, 0), pr::ColourWhite, pr::v3ZAxis, pr::v2::make(t0,t0)},
				};
				pr::uint16 idxs[] =
				{
					0, 1, 2, 0, 2, 3
				};
				auto bbox = pr::BBox::make(pr::v4Origin, pr::v4::make(1,1,0,0));

				MdlSettings s(verts, idxs, bbox, "unit quad");
				m_unit_quad.m_model = scene.m_rdr->m_mdl_mgr.CreateModel(s);

				NuggetProps ddata(EPrim::TriList, VertPCNT::GeomMask);
				m_unit_quad.m_model->CreateNugget(ddata);
			}

			{// Create a constants buffer for camera properties
				CBufferDesc cbdesc(sizeof(DSShader::CBufCamera));
				pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_camera.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_camera, "dslighting CBufCamera"));
			}
			{// Create a constants buffer for lighting properties
				CBufferDesc cbdesc(sizeof(DSShader::CBufLighting));
				pr::Throw(scene.m_rdr->Device()->CreateBuffer(&cbdesc, nullptr, &m_cbuf_lighting.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_lighting, "dslighting CBufLighting"));
			}

			// Disable Z-buffer
			m_dsb.Set(EDS::DepthEnable, false);
			m_dsb.Set(EDS::DepthWriteMask, D3D11_DEPTH_WRITE_MASK_ZERO);
		}

		// Set the position of the four corners of the view frustum in camera space
		void SetFrustumCorners(SceneView const& view, DSShader::CBufCamera& cb)
		{
			pr::GetCorners(view.Frustum(), cb.m_frustum, 1.0f);
		}

		// Perform the render step
		void DSLighting::ExecuteInternal(StateStack& ss)
		{
			// Sort the draw list if needed
			SortIfNeeded();

			// Set the viewport
			ss.m_dc->RSSetViewports(1, &m_scene->m_viewport);

			{// Set camera constants
				DSShader::CBufCamera cb = {};
				SetViewConstants(m_scene->m_view, cb);
				SetFrustumCorners(m_scene->m_view, cb);
				WriteConstants(ss.m_dc, m_cbuf_camera, cb);
			}
			{// Set lighting constants
				DSShader::CBufLighting cb = {};
				SetLightingConstants(m_scene->m_global_light, cb);
				WriteConstants(ss.m_dc, m_cbuf_lighting, cb);
			}

			// Draw the full screen quad
			{
				Nugget const& nugget = m_unit_quad.m_model->m_nuggets.front();

				// Bind the shader to the device
				DrawListElement dle;
				dle.m_shader   = m_shader.m_ptr;
				dle.m_nugget   = &nugget;
				dle.m_instance = &m_unit_quad.m_base;
				dle.m_sort_key = 0;

				StateStack::DleFrame frame(ss, dle);
				ss.Commit();

				// Add the nugget to the device context
				ss.m_dc->DrawIndexed(
					UINT(nugget.m_irange.size()),
					UINT(nugget.m_irange.m_begin),
					0);
			}
		}
	}
}

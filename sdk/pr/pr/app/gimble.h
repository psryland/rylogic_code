//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_APP_GIMBLE_H
#define PR_APP_GIMBLE_H

#include "pr/app/forward.h"

namespace pr
{
	namespace app
	{
		// A gimble model
		struct Gimble :pr::events::IRecv<pr::rdr::Evt_SceneRender>
		{
			// A renderer instance type for the body
			#define PR_RDR_INST(x)\
				x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
				x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr    )
			PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
			#undef PR_RDR_INST

			Instance m_inst;    // The gimble instance
			pr::v4   m_ofs_pos; // The offset position from the camera focus point
			float    m_scale;   // A model size scaler.

			// Constructs a gimble model and instance.
			Gimble(pr::Renderer& rdr)
			:m_inst()
			,m_ofs_pos(pr::v4Zero)
			,m_scale(1.0f)
			{
				InitModel(rdr);
			}

			// Add the gimble to a viewport
			void OnEvent(pr::rdr::Evt_SceneRender const& e)
			{
				pr::rdr::SceneView const& view = e.m_scene.m_view;
				m_inst.m_i2w = pr::Scale4x4(m_scale, view.FocusPoint() + view.m_c2w * m_ofs_pos);
				e.m_scene.AddInstance(m_inst);
			}

		private:

			// Create a model for a 'gimble'
			void InitModel(pr::Renderer& rdr)
			{
				pr::rdr::VertPC const verts[] =
				{
					{{-0.1f,  0.0f,  0.0f}, {1.0f, 0.0f, 0.0f, 1.0f}},
					{{ 1.0f,  0.0f,  0.0f}, {1.0f, 0.0f, 0.0f, 1.0f}},
					{{ 0.0f, -0.1f,  0.0f}, {0.0f, 1.0f, 0.0f, 1.0f}},
					{{ 0.0f,  1.0f,  0.0f}, {0.0f, 1.0f, 0.0f, 1.0f}},
					{{ 0.0f,  0.0f, -0.1f}, {0.0f, 0.0f, 1.0f, 1.0f}},
					{{ 0.0f,  0.0f,  1.0f}, {0.0f, 0.0f, 1.0f, 1.0f}},
				};
				pr::uint16 const indices[] =
				{
					0, 1, 2, 3, 4, 5
				};

				// Create the gimble model
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(pr::rdr::MdlSettings(verts, indices, "gimble"));

				pr::rdr::DrawMethod method;

				// Get a suitable shader
				method.m_shader = rdr.m_shdr_mgr.FindShaderFor<pr::rdr::VertPC>();

				// Create a render nugget
				m_inst.m_model->CreateNugget(pr::rdr::ERenderStep::ForwardRender, method, pr::rdr::EPrim::LineList);
			}
		};
	}
}

#endif

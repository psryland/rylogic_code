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
		struct Gimble :pr::events::IRecv<pr::rdr::Evt_UpdateScene>
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
			void OnEvent(pr::rdr::Evt_UpdateScene const& e) override
			{
				pr::rdr::SceneView const& view = e.m_scene.m_view;
				m_inst.m_i2w = pr::Scale4x4(m_scale, view.FocusPoint() + view.m_c2w * m_ofs_pos);
				e.m_scene.AddInstance(m_inst);
			}

		private:

			// Create a model for a 'gimble'
			void InitModel(pr::Renderer& rdr)
			{
				using namespace pr::rdr;

				Vert const verts[] =
				{
					{{-0.1f,  0.0f,  0.0f, 1.0f}, {1.0f, 0.0f, 0.0f, 1.0f}, v4Zero, v2Zero},
					{{ 1.0f,  0.0f,  0.0f, 1.0f}, {1.0f, 0.0f, 0.0f, 1.0f}, v4Zero, v2Zero},
					{{ 0.0f, -0.1f,  0.0f, 1.0f}, {0.0f, 1.0f, 0.0f, 1.0f}, v4Zero, v2Zero},
					{{ 0.0f,  1.0f,  0.0f, 1.0f}, {0.0f, 1.0f, 0.0f, 1.0f}, v4Zero, v2Zero},
					{{ 0.0f,  0.0f, -0.1f, 1.0f}, {0.0f, 0.0f, 1.0f, 1.0f}, v4Zero, v2Zero},
					{{ 0.0f,  0.0f,  1.0f, 1.0f}, {0.0f, 0.0f, 1.0f, 1.0f}, v4Zero, v2Zero},
				};
				pr::uint16 const indices[] =
				{
					0, 1, 2, 3, 4, 5
				};

				// Create the gimble model
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(MdlSettings(verts, indices, pr::BBoxMake(verts), "gimble"));

				NuggetProps mat; // Get a suitable shader
				mat.m_topo = EPrim::LineList;
				mat.m_geom = EGeom::Vert | EGeom::Colr;

				// Create a render nugget
				m_inst.m_model->CreateNugget(mat);
			}
		};
	}
}

#endif

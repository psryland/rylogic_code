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
		struct Gimble :pr::events::IRecv<pr::app::Evt_AddToViewport>
		{
			// A renderer instance type for the gimble
			PR_RDR_DECLARE_INSTANCE_TYPE2
			(
				Instance
				,pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr
				,pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
			);
			
			Instance m_inst;
			pr::v4   m_ofs_pos; // The offset position from the camera focus point
			float    m_scale;   // A model size scaler.
			
			// Constructs a gimble model and instance.
			Gimble(pr::Renderer& rdr)
			:m_inst()
			,m_ofs_pos()
			,m_scale(1.0f)
			{}
			
			// Add the gimble to a viewport
			void OnEvent(pr::app::Evt_AddToViewport const& e)
			{
				m_inst.m_i2w = pr::Scale4x4(100.0f, e.m_cam->CameraToWorld().pos);
				e.m_vp->AddInstance(m_inst);
			}
		
		private:
			
			// Create a model for a 5-sided cubic dome
			void InitModel(pr::Renderer& rdr, string const& texpath)
			{
				pr::v4 const verts[] =
				{
					pr::v4::make(-0.1f,  0.0f,  0.0f, 1.0f), pr::v4::make(1.0f,  0.0f,  0.0f, 1.0f),
					pr::v4::make( 0.0f, -0.1f,  0.0f, 1.0f), pr::v4::make(0.0f,  1.0f,  0.0f, 1.0f),
					pr::v4::make( 0.0f,  0.0f, -0.1f, 1.0f), pr::v4::make(0.0f,  0.0f,  1.0f, 1.0f),
				};
				pr::uint16 lines[] = { 0, 1, 2, 3, 4, 5 };
				pr::Colour32 coloursFF[] = { 0xFFFF0000, 0xFFFF0000, 0xFF00FF00, 0xFF00FF00, 0xFF0000FF, 0xFF0000FF };
		
				m_inst.m_model = pr::rdr::model::Mesh(rdr, pr::rdr::model::EPrimitive::LineList, pr::geom::EVC, PR_COUNTOF(lines), PR_COUNTOF(verts), lines, verts, 0, coloursFF, 0, pr::m4x4Identity);
				m_inst.m_i2w   = pr::m4x4Identity;
			}
		};
	}
}

#endif

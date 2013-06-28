//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_OBJECTS_ASTRONOMICAL_BODY_H
#define PR_SOL_OBJECTS_ASTRONOMICAL_BODY_H

#include "sol/main/forward.h"

namespace sol
{
	struct AstronomicalBody
		:pr::events::IRecv<pr::rdr::Evt_SceneRender>
	{
		// A renderer instance type for the body
		#define PR_RDR_INST(x)\
			x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
			x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr    )
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
		#undef PR_RDR_INST

		pr::v4   m_position; // The position relative to the local coordinate system
		float    m_radius;   // The radius of the body
		float    m_mass;     // The mass of the body
		Instance m_inst;     // The renderer instance

		AstronomicalBody(pr::v4 const& position, float radius, float mass, pr::Renderer& rdr, wchar_t const* texture);
		void OnEvent(pr::rdr::Evt_SceneRender const& e);
	};
}

#endif

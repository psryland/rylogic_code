//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_OBJECTS_STAR_H
#define PR_SOL_OBJECTS_STAR_H

#include "sol/main/forward.h"
#include "sol/objects/astronomical_body.h"

namespace sol
{
	struct Star
		:AstronomicalBody
		,pr::events::IRecv<pr::rdr::Evt_SceneRender>
	{
		// A renderer instance type for the body
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			Instance
			,pr::m4x4            ,m_i2w   ,pr::rdr::EInstComp::I2WTransform
			,pr::rdr::ModelPtr   ,m_model ,pr::rdr::EInstComp::ModelPtr
		);

		Star(pr::v4 const& position, float radius, float mass, pr::Renderer& rdr, wchar_t const* texture);
		void OnEvent(pr::rdr::Evt_SceneRender const& e);
	};
}

#endif

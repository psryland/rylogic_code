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
	{
		Star(pr::v4 const& position, float radius, float mass, pr::Renderer& rdr, wchar_t const* texture);
		//void OnEvent(pr::rdr::Evt_SceneRender const& e);
	};
}

#endif

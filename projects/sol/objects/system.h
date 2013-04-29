//*****************************************************************************************
// Sol
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_SOL_OBJECTS_SYSTEM_H
#define PR_SOL_OBJECTS_SYSTEM_H

#include "sol/main/forward.h"
#include "sol/objects/astronomical_body.h"

namespace sol
{
	// A container for stars, planets, moons, etc
	struct System
	{
		pr::Array<AstronomicalBody> m_bodies;
		System(int seed);
	
	private:
		void GenerateStars(int seed);
	};
}

#endif

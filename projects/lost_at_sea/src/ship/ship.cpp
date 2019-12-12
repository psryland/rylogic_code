//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/ship/ship.h"

namespace las
{
	// placeholder for the player's ship
	Ship::Ship(Renderer& rdr)
		:m_inst()
	{
		m_inst.m_model = ModelGenerator<>::Box(rdr, v4(2, 1, 4, 0), m4x4Identity, Colour32Green);
	}

	// Render the ship
	void Ship::AddToScene(rdr::Scene& scene)
	{
		m_inst.m_i2w = m4x4Identity;
		scene.AddInstance(m_inst);
	}
}
//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#include "stdafx.h"
#include "ship/ship.h"
#include "event.h"

// placeholder for the player's ship
las::Ship::Ship(pr::Renderer& rdr)
:m_inst()
{
	m_inst.m_model = pr::rdr::model::Box(rdr, pr::v4::make(2.0f, 1.0f, 4.0f, 0.0f), pr::m4x4Identity, pr::Colour32Green);
}

// Render the ship
void las::Ship::OnEvent(las::Evt_AddToViewport const& e)
{
	m_inst.m_i2w = pr::m4x4Identity;
	e.m_vp->AddInstance(m_inst);
}

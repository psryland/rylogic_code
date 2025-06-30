//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2015
//************************************
#include "src/forward.h"
#include "src/ship/ship.h"

namespace las
{
	// placeholder for the player's ship
	Ship::Ship(ResourceFactory& factory)
		:m_inst()
	{
		auto opts = ModelGenerator::CreateOptions().colours({ &Colour32Green, 1 });
		m_inst.m_model = ModelGenerator::Box(factory, v4(2, 1, 4, 0), &opts);
	}

	// Render the ship
	void Ship::AddToScene(Scene& scene)
	{
		m_inst.m_i2w = m4x4::Identity();
		scene.AddInstance(m_inst);
	}
}
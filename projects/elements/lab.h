#pragma once

#include "elements/forward.h"
#include "elements/material.h"
#include "elements/game_constants.h"

namespace ele
{
	// Used to create materials
	struct Lab
	{
		GameConstants const& m_consts;

		explicit Lab(GameConstants const& consts);

		// React two materials
		void React(Material m1, Material m2, Material& out1, Material& out2) const;
	
	private:
		PR_NO_COPY(Lab);
	};
}

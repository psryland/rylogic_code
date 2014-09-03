//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#ifndef LAS_SHIP_H
#define LAS_SHIP_H

#include "forward.h"
#include "event.h"

namespace las
{
	// placeholder for the player's ship
	struct Ship
		:pr::events::IRecv<las::Evt_AddToViewport>
	{
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			Instance
			,pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr
			,pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
		);
		Instance m_inst;           // The ship instance
		
		Ship(pr::Renderer& rdr);
		void OnEvent(las::Evt_AddToViewport const& e);
	};
}

#endif

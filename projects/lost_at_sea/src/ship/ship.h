//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************

#pragma once

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/event.h"

namespace las
{
	struct Ship
	{
		#define PR_RDR_INST(x)\
			x(rdr::ModelPtr ,m_model ,rdr::EInstComp::ModelPtr)\
			x(m4x4          ,m_i2w   ,rdr::EInstComp::I2WTransform)
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
		#undef PR_RDR_INST

		// The ship instance
		Instance m_inst;

		Ship(Renderer& rdr);
		void AddToScene(Scene& scene);
	};
}

//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#pragma once

#include "lost_at_sea/src/forward.h"
#include "lost_at_sea/src/event.h"

namespace las
{
	struct Skybox
		:pr::events::IRecv<Evt_AddToViewport>
	{
		#define PR_RDR_INST(x)\
			x(pr::rdr::ModelPtr ,m_model ,EInstComp::ModelPtr)\
			x(pr::m4x4          ,m_i2w   ,EInstComp::I2WTransform)
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
		#undef PR_RDR_INST

		Instance m_inst;             // The skybox instance
		pr::rdr::Texture2DPtr m_tex; // A texture for the skybox
		
		Skybox(pr::Renderer& rdr, string const& texpath);
		void OnEvent(las::Evt_AddToViewport const& e);
	};
}

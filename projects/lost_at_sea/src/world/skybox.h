//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2011
//************************************
#ifndef LAS_SKYBOX_H
#define LAS_SKYBOX_H
#pragma once

#include "forward.h"
#include "event.h"

namespace las
{
	struct Skybox
		:pr::events::IRecv<las::Evt_AddToViewport>
	{
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			Instance
			,pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr
			,pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
		);
		Instance m_inst;           // The skybox instance
		pr::rdr::TexturePtr m_tex; // A texture for the skybox
		
		Skybox(pr::Renderer& rdr, string const& texpath);
		void OnEvent(las::Evt_AddToViewport const& e);
	};
}

#endif

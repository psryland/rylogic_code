//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "renderer11/steps/forward/fwd_shader.h"

namespace pr
{
	namespace rdr
	{
		FwdShader::FwdShader(ShaderManager* mgr)
			:BaseShader(mgr)
		{
			// Create a per-model constants buffer
			CBufferDesc cbdesc(sizeof(CBufModel));
			pr::Throw(mgr->m_device->CreateBuffer(&cbdesc, 0, &m_cbuf_model.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(m_cbuf_model, "FwdShader::CBufModel"));
		}
	}
}

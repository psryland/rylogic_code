//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_set.h"
#include "pr/renderer11/shaders/shader_manager.h"

namespace pr
{
	namespace rdr
	{
		// Get or add a shader with id 'shdr_id'
		ShaderPtr ShaderSet::get(RdrId shdr_id) const
		{
			for (auto& s : *this)
				if (s->m_id == shdr_id)
					return s;
			return nullptr;
		}
		ShaderPtr ShaderSet::get(RdrId shdr_id, ShaderManager* mgr)
		{
			auto s = get(shdr_id);
			if (s != nullptr) return s;
			push_back(s = mgr->FindShader(shdr_id));
			return s;
		}

		// Return a shader by shader type
		ShaderPtr ShaderSet::get(EShaderType type) const
		{
			auto shdr = find(type);
			if (shdr == nullptr) throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Shader of type %s not in set", type.ToString()));
			return shdr;
		}
		ShaderPtr ShaderSet::find(EShaderType type) const
		{
			for (auto& s : *this)
				if (s->m_shdr_type == type)
					return s;
			return nullptr;
		}
	}
}
//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"

namespace pr
{
	namespace rdr
	{
		// A collection of shader instances
		struct ShaderSet :private pr::Array<ShaderPtr, 8, true>
		{
			typedef pr::Array<ShaderPtr, 8, true> Cont;

			// Allow shaders to be added manually
			using Cont::push_back;
			using Cont::clear;

			// Iterator access
			using Cont::begin;
			using Cont::end;

			// Get or add a shader with id 'shdr_id'
			ShaderPtr get(RdrId shdr_id, ShaderManager* mgr);
			ShaderPtr get(RdrId shdr_id) const;

			// Return a shader by shader type
			ShaderPtr get(EShaderType type) const;
			ShaderPtr find(EShaderType type) const;
			
			// Return a dx shader pointer for the given type
			template <EShaderType::Enum_ ST> D3DPtr<typename DxShaderType<ST>::type> find_dx() const
			{
				auto shdr = find(ST).m_ptr;
				return shdr != nullptr ? shdr->m_shdr : nullptr;
			}

			bool operator != (ShaderSet const& rhs) const { return !(static_cast<Cont const&>(*this) == static_cast<Cont const&>(rhs)); }
		};
	}
}
//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_SHADERS_SHADER_MANAGER_H
#define PR_RDR_SHADERS_SHADER_MANAGER_H

#include <hash_map>
#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/shaders/shader.h"

namespace pr
{
	namespace rdr
	{
		class ShaderManager
		{
			typedef Lookup<RdrId, Shader*> ShaderLookup;

			pr::rdr::Allocator<Shader>    m_alex_shader;
			D3DPtr<ID3D11Device>          m_device;
			ShaderLookup                  m_lookup_shader;  // A map from shader id to existing shader instances
			
			ShaderManager(ShaderManager const&); // no copying
			ShaderManager& operator = (ShaderManager const&);
			
			friend struct pr::rdr::Shader;
			void Delete(pr::rdr::Shader const* shdr);
			
			// Create the built-in shaders
			void CreateStockShaders();
			
		public:
			ShaderManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~ShaderManager();
			
			// Create a basic shader.
			// Clients should call this method to create the basic shader object and register it
			// with the shader manager. Afterwards they can use the shader pointer returned to setup
			// the specific properties of the shader. Pass nulls for unneeded shader descriptions
			pr::rdr::ShaderPtr CreateShader(RdrId id, BindShaderFunc bind_func, VShaderDesc const* vsdesc, PShaderDesc const* psdesc);
			
			// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
			pr::rdr::ShaderPtr FindShaderFor(EGeom::Type geom_mask) const;
			template <class Vert> pr::rdr::ShaderPtr FindShaderFor() const { return FindShaderFor(Vert::GeomMask); }
		};
	}
}

#endif

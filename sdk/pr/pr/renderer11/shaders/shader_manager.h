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
			typedef Lookup<RdrId, BaseShader*> ShaderLookup;
			typedef std::function<ShaderPtr(pr::rdr::ShaderManager*)> ShaderAlex;

			pr::rdr::MemFuncs    m_mem; // Not using an allocator here, because the Shader type isn't known until 'CreateShader' is called
			D3DPtr<ID3D11Device> m_device;
			ShaderLookup         m_lookup_shader;  // A map from shader id to existing shader instances

			ShaderManager(ShaderManager const&); // no copying
			ShaderManager& operator = (ShaderManager const&);

			friend struct BaseShader;
			void Delete(pr::rdr::BaseShader const* shdr);

			// Create the built-in shaders
			void CreateStockShaders();

			// Builds the basic parts of a shader.
			pr::rdr::ShaderPtr InitShader(ShaderAlex create, RdrId id, ShaderSetupFunc setup, VShaderDesc const* vsdesc, PShaderDesc const* psdesc, CBufferDesc const* cbdesc, char const* name);

		public:
			ShaderManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~ShaderManager();

			// Create a custom shader object.
			// Pass nulls for 'vsdesc', 'psdesc' if they're not needed
			template <class ShaderType> RefPtr<ShaderType> CreateShader(RdrId id, ShaderSetupFunc setup, VShaderDesc const* vsdesc, PShaderDesc const* psdesc, CBufferDesc const* cbdesc, char const* name)
			{
				// Create a lambda for allocating the shader
				ShaderAlex create = [&](pr::rdr::ShaderManager* mgr){ return pr::rdr::Allocator<ShaderType>(m_mem).New(mgr); };
				return InitShader(create, id, setup, vsdesc, psdesc, cbdesc, name);
			}

			// Return the shader corresponding to 'id' or null if not found
			pr::rdr::ShaderPtr FindShader(RdrId id) const;

			// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
			pr::rdr::ShaderPtr FindShaderFor(EGeom geom_mask) const;
			template <class Vert> pr::rdr::ShaderPtr FindShaderFor() const { return FindShaderFor(Vert::GeomMask); }
		};
	}
}

#endif

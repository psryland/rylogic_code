//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include <unordered_map>
#include "pr/renderer11/forward.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/lookup.h"
#include "pr/renderer11/util/stock_resources.h"

namespace pr
{
	namespace rdr
	{
		class ShaderManager
		{
			typedef Lookup<RdrId, D3DPtr<ID3D11InputLayout>>    IPLookup;
			typedef Lookup<RdrId, D3DPtr<ID3D11VertexShader>>   VSLookup;
			typedef Lookup<RdrId, D3DPtr<ID3D11PixelShader>>    PSLookup;
			typedef Lookup<RdrId, D3DPtr<ID3D11GeometryShader>> GSLookup;
			typedef Lookup<RdrId, D3DPtr<ID3D11Buffer>>         CBufLookup;
			typedef Lookup<RdrId, ShaderBase*>                  ShaderLookup;

			typedef std::function<ShaderPtr(ShaderManager*)> ShaderAlexFunc;
			typedef std::function<void(ShaderBase*)>         ShaderDeleteFunc;

			MemFuncs     m_mem;           // Not using an allocator here, because the Shader type isn't known until 'CreateShader' is called
			IPLookup     m_lookup_ip;     // Map from id to D3D input layout
			VSLookup     m_lookup_vs;     // Map from id to D3D vertex shader
			PSLookup     m_lookup_ps;     // Map from id to D3D pixel shader
			GSLookup     m_lookup_gs;     // Map from id to D3D geometry shader
			ShaderLookup m_lookup_shader; // Map from id to ShaderBase instances
			CBufLookup   m_lookup_cbuf;   // Shared cbuffer objects

			friend struct ShaderBase;
			ShaderManager(ShaderManager const&); // no copying
			ShaderManager& operator = (ShaderManager const&);

			// Create the built-in shaders
			void CreateStockShaders();

			// Used for stock shaders to specialise
			template <typename TShader> void CreateShader();

			// Adds/Removes a shader from the lookup map
			ShaderPtr InitShader(ShaderBase* shdr);
			void      DestShader(ShaderBase* shdr);

			// Called when a shader's ref count hits zero to delete 'shdr' via the correct allocator
			template <typename ShaderType> void DeleteShader(ShaderType* shdr)
			{
				DestShader(shdr);
				Allocator<ShaderType>(m_mem).Delete(shdr);
			}

		public:

			// dx device
			D3DPtr<ID3D11Device> m_device;

			ShaderManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device);
			~ShaderManager();

			// Get or Create a dx shader.
			// If 'id' does not already exist, 'desc' must not be null.
			D3DPtr<ID3D11InputLayout>    GetIP(RdrId id, VShaderDesc const* desc = nullptr);
			D3DPtr<ID3D11VertexShader>   GetVS(RdrId id, VShaderDesc const* desc = nullptr);
			D3DPtr<ID3D11PixelShader>    GetPS(RdrId id, PShaderDesc const* desc = nullptr);
			D3DPtr<ID3D11GeometryShader> GetGS(RdrId id, GShaderDesc const* desc = nullptr);

			// Create a custom shader object derived from ShaderBase.
			template <typename ShaderType, typename D3DShaderType>
			typename std::enable_if<std::is_base_of<ShaderBase, ShaderType>::value, typename RefPtr<ShaderType>>::type
			CreateShader(RdrId id, typename D3DPtr<D3DShaderType> d3d_shdr, char const* name)
			{
				ShaderPtr shdr = Allocator<ShaderType>(m_mem).New(this, id, name, d3d_shdr);
				return InitShader(shdr.m_ptr);
			}

			// Return the shader corresponding to 'id' or null if not found
			ShaderPtr FindShader(RdrId id) const;

			// Create a copy of an existing shader.
			ShaderPtr CloneShader(RdrId id, RdrId new_id, char const* new_name);

			// Get or create a cbuffer object for given type 'TCBuf'
			template <typename TCBuf> D3DPtr<ID3D11Buffer> GetCBuf(char const* name = nullptr)
			{
				// For each instantiation of this method, create a new id
				static RdrId id = MonotonicId();

				// See if a cbuffer already exists for this type
				auto iter = m_lookup_cbuf.find(id);
				if (iter != end(m_lookup_cbuf))
					return iter->second;

				// Create the cbuffer, add it to the lookup, and return it
				D3DPtr<ID3D11Buffer> cbuf;
				CBufferDesc cbdesc(sizeof(TCBuf));
				pr::Throw(m_device->CreateBuffer(&cbdesc, 0, &cbuf.m_ptr));
				PR_EXPAND(PR_DBG_RDR, NameResource(cbuf, name)); (void)name;
				m_lookup_cbuf[id] = cbuf;
				return cbuf;
			}
		};
	}
}

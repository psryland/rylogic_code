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
			using IPLookup     = Lookup<RdrId, D3DPtr<ID3D11InputLayout>>    ;
			using VSLookup     = Lookup<RdrId, D3DPtr<ID3D11VertexShader>>   ;
			using PSLookup     = Lookup<RdrId, D3DPtr<ID3D11PixelShader>>    ;
			using GSLookup     = Lookup<RdrId, D3DPtr<ID3D11GeometryShader>> ;
			using CBufLookup   = Lookup<RdrId, D3DPtr<ID3D11Buffer>>         ;
			using ShaderLookup = Lookup<RdrId, ShaderBase*>                  ;

			using ShaderAlexFunc = std::function<ShaderPtr(ShaderManager*)>;
			using ShaderDeleteFunc = std::function<void(ShaderBase*)>;
			using AllocationsTracker = AllocationsTracker<ShaderBase>;

			MemFuncs             m_mem;           // Not using an allocator here, because the Shader type isn't known until 'CreateShader' is called
			AllocationsTracker   m_dbg_mem;       // Allocation tracker
			Renderer&            m_rdr;           // The owner renderer instance
			IPLookup             m_lookup_ip;     // Map from id to D3D input layout
			VSLookup             m_lookup_vs;     // Map from id to D3D vertex shader
			PSLookup             m_lookup_ps;     // Map from id to D3D pixel shader
			GSLookup             m_lookup_gs;     // Map from id to D3D geometry shader
			ShaderLookup         m_lookup_shader; // Map from id to ShaderBase instances
			CBufLookup           m_lookup_cbuf;   // Shared 'cbuffer' objects
			std::recursive_mutex m_mutex;

			friend struct ShaderBase;

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
				std::lock_guard<std::recursive_mutex> lock(m_mutex);
				DestShader(shdr);
				assert(m_dbg_mem.remove(shdr));
				Allocator<ShaderType>(m_mem).Delete(shdr);
			}

			// Get or create a 'cbuffer' object for 'id' of size 'sz'
			D3DPtr<ID3D11Buffer> GetCBuf(char const* name, RdrId id, size_t sz);

		public:

			ShaderManager(MemFuncs& mem, Renderer& rdr);
			ShaderManager(ShaderManager const&) = delete;
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
				std::lock_guard<std::recursive_mutex> lock(m_mutex);

				ShaderPtr shdr(Allocator<ShaderType>(m_mem).New(this, id, name, d3d_shdr));
				assert(m_dbg_mem.add(shdr.m_ptr));
				return InitShader(shdr.m_ptr);
			}

			// Return the shader corresponding to 'id' or null if not found
			ShaderPtr FindShader(RdrId id);

			// Create a copy of an existing shader.
			ShaderPtr CloneShader(RdrId id, RdrId new_id, char const* new_name);

			// Return the shader corresponding to 'id' or null if not found
			template <typename ShaderType> pr::RefPtr<ShaderType> FindShader(RdrId id)
			{
				return FindShader(id);
			}

			// Look for a shader with id 'id'. If not found, find the shader with id 'base_id' and clone it with name 'name'
			template <typename ShaderType> pr::RefPtr<ShaderType> FindShader(RdrId id, RdrId base_id, char const* name)
			{
				auto shdr = FindShader(id);
				return shdr ? shdr : CloneShader(base_id, id, name);
			}

			// Look for a shader named 'name'. If not found, find the shader with id 'base_id' and clone it with name 'name'
			template <typename ShaderType> pr::RefPtr<ShaderType> FindShader(char const* name, RdrId base_id)
			{
				auto id = MakeId(name);
				return FindShader<ShaderType>(id, base_id, name);
			}

			// Get or create a 'cbuffer' object for given type 'TCBuf'
			template <typename TCBuf> D3DPtr<ID3D11Buffer> GetCBuf(char const* name = nullptr)
			{
				std::lock_guard<std::recursive_mutex> lock(m_mutex);

				// For each instantiation of this method, create a new id
				static RdrId id = MonotonicId();

				// Get the buffer for 'id'
				return GetCBuf(name, id, sizeof(TCBuf));
			}
		};
	}
}

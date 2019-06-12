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

namespace pr::rdr
{
	class ShaderManager
	{
		// Notes:
		//  - The shader manager is a store of D3D shaders.
		//  - The shader manager allows for application specific shaders.
		//  - The 'Shader' derived objects are reference counted instances of D3D11 shaders.
		//  - A 'Shader' derived object is created for each configuration of its shader constants.
		//    This might be as many as one per nugget.
		//  - The 'ShaderLookup' container is a collection of weak references to Shader instances.
		//    An application can cache Shader instances that use the same constants. This is not
		//    necessary though, creating a Shader instance per nugget is ok.

		using IPLookup     = Lookup<RdrId, D3DPtr<ID3D11InputLayout>>;
		using VSLookup     = Lookup<RdrId, D3DPtr<ID3D11VertexShader>>;
		using PSLookup     = Lookup<RdrId, D3DPtr<ID3D11PixelShader>>;
		using GSLookup     = Lookup<RdrId, D3DPtr<ID3D11GeometryShader>>;
		using CSLookup     = Lookup<RdrId, D3DPtr<ID3D11ComputeShader>>;
		using CBufLookup   = Lookup<RdrId, D3DPtr<ID3D11Buffer>>;
		using ShaderLookup = Lookup<RdrId, Shader*>;

		using ShaderAlexFunc = std::function<ShaderPtr(ShaderManager*)>;
		using ShaderDeleteFunc = std::function<void(Shader*)>;
		using AllocationsTracker = AllocationsTracker<Shader>;
		friend struct Shader;

		MemFuncs              m_mem;           // Not using an allocator here, because the Shader type isn't known until 'CreateShader' is called
		AllocationsTracker    m_dbg_mem;       // Allocation tracker
		Renderer&             m_rdr;           // The owner renderer instance
		IPLookup              m_lookup_ip;     // Map from id to D3D input layout
		VSLookup              m_lookup_vs;     // Map from id to D3D vertex shader
		PSLookup              m_lookup_ps;     // Map from id to D3D pixel shader
		GSLookup              m_lookup_gs;     // Map from id to D3D geometry shader
		CSLookup              m_lookup_cs;     // Map from id to D3D compute shader
		ShaderLookup          m_lookup_shader; // Map from id to Shader instances
		CBufLookup            m_lookup_cbuf;   // Shared 'cbuffer' objects
		pr::vector<ShaderPtr> m_stock_shaders; // A collection of references to the stock shaders
		std::recursive_mutex  m_mutex;

		// Create the built-in shaders
		void CreateStockShaders();

		// Used for stock shaders to specialise
		template <typename TShader> void CreateShader();

		// Called when a shader's ref count hits zero
		template <typename ShaderType> void DeleteShader(ShaderType* shdr)
		{
			std::lock_guard<std::recursive_mutex> lock(m_mutex);
				
			// Remove from the cache
			m_lookup_shader.erase(shdr->m_id);

			// Release memory
			assert(m_dbg_mem.remove(shdr));
			Allocator<ShaderType>(m_mem).Delete(shdr);
		}

		// Get/Create a 'cbuffer' object for 'id' of size 'sz'
		D3DPtr<ID3D11Buffer> GetCBuf(char const* name, RdrId id, size_t sz);

	public:

		ShaderManager(MemFuncs& mem, Renderer& rdr);
		ShaderManager(ShaderManager const&) = delete;
		~ShaderManager();

		// Get/Create a dx shader. If 'id' does not already exist, 'desc' must not be null.
		D3DPtr<ID3D11InputLayout>    GetIP(RdrId id, VShaderDesc const* desc = nullptr);
		D3DPtr<ID3D11VertexShader>   GetVS(RdrId id, VShaderDesc const* desc = nullptr);
		D3DPtr<ID3D11PixelShader>    GetPS(RdrId id, PShaderDesc const* desc = nullptr);
		D3DPtr<ID3D11GeometryShader> GetGS(RdrId id, GShaderDesc const* desc = nullptr);
		D3DPtr<ID3D11GeometryShader> GetGS(RdrId id, GShaderDesc const* desc, StreamOutDesc const& so_desc);
		D3DPtr<ID3D11ComputeShader>  GetCS(RdrId id, CShaderDesc const* desc = nullptr);

		// Create an instance of a shader object derived from Shader.
		template <typename ShaderType, typename DxShaderType, typename = std::enable_if_t<std::is_base_of_v<Shader, ShaderType>>>
		pr::RefPtr<ShaderType> CreateShader(RdrId id, typename D3DPtr<DxShaderType> const& d3d_shdr, char const* name)
		{
			std::lock_guard<std::recursive_mutex> lock(m_mutex);
			PR_ASSERT(PR_DBG_RDR, id == AutoId || FindShader(id) == nullptr, "A shader with this Id already exists");

			// Set up a sort id for the shader that groups them by D3D shader
			auto sort_id = SortKeyId(ptrdiff_t(d3d_shdr.get() - 0) % SortKey::MaxShaderId);

			// Allocate the shader instance
			pr::RefPtr<ShaderType> shdr(Allocator<ShaderType>(m_mem).New(this, id, sort_id, name, d3d_shdr), true);
			assert(m_dbg_mem.add(shdr.m_ptr));

			// Store a weak reference to the instance
			AddLookup(m_lookup_shader, shdr->m_id, shdr.get());

			// Return the shader instance
			return std::move(shdr);
		}

		// Return a cached Shader instance corresponding to 'id' or null if not found
		template <typename ShaderType, typename = std::enable_if_t<std::is_base_of_v<Shader, ShaderType>>>
		pr::RefPtr<ShaderType> FindShader(RdrId id)
		{
			// AutoId means make a new shader, so it'll never exist already
			if (id == AutoId)
				return nullptr;

			std::lock_guard<std::recursive_mutex> lock(m_mutex);

			// Look for 'id' in the cache.
			auto shdr = GetOrDefault(m_lookup_shader, id, (Shader*)nullptr);
			return pr::RefPtr<ShaderType>(static_cast<ShaderType*>(shdr), true);
		}
		ShaderPtr FindShader(RdrId id)
		{
			// This allows shaders to be found without having to include the definition of the shader
			return FindShader<Shader>(id);
		}

		// Get/Create a Shader instance corresponding to 'id'.
		// Use 'id' = AutoId to ignore the shader instance cache and just create a new instance of 'base_id'
		template <typename ShaderType, typename = std::enable_if_t<std::is_base_of_v<Shader, ShaderType>>>
		pr::RefPtr<ShaderType> GetShader(RdrId id, RdrId base_id, char const* name = nullptr)
		{
			// Look in the cache for an instance with id 'id'
			auto shdr = FindShader<ShaderType>(id);
			if (shdr == nullptr)
			{
				// Create a shader based on 'base_id'
				auto existing = FindShader<ShaderType>(base_id);
				if (!existing)
					throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Existing shader with id %d not found", base_id));

				// Create a copy of 'existing'
				shdr = CreateShader<ShaderType>(id, existing->dx_shader(), name ? name : existing->m_name.c_str());
				shdr->m_bsb     = existing->m_bsb;
				shdr->m_rsb     = existing->m_rsb;
				shdr->m_dsb     = existing->m_dsb;
				shdr->m_orig_id = existing->m_orig_id;
			}
			return std::move(shdr);
		}

		// Get/Create a Shader instance corresponding to 'id'.
		// 'id' should be a string that uniquely identifies the shader and it's constants.
		// This allows the caching of shader instances with the same constants to work.
		// Don't worry if it's too complex though, creating new shader instances is relatively cheap.
		template <typename ShaderType, typename = std::enable_if_t<std::is_base_of_v<Shader, ShaderType>>>
		pr::RefPtr<ShaderType> GetShader(char const* id, RdrId base_id, char const* name = nullptr)
		{
			auto id_ = id ? MakeId(id) : AutoId;
			return std::move(GetShader<ShaderType>(id_, base_id, name));
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

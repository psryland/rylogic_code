//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/util/event_types.h"
#include "renderer11/util/forward_private.h"

namespace pr
{
	namespace rdr
	{
		ShaderManager::ShaderManager(MemFuncs& mem, Renderer& rdr)
			:m_mem(mem)
			,m_dbg_mem()
			,m_rdr(rdr)
			,m_lookup_ip(mem)
			,m_lookup_vs(mem)
			,m_lookup_ps(mem)
			,m_lookup_gs(mem)
			,m_lookup_shader(mem)
			,m_lookup_cbuf(mem)
			,m_mutex()
		{
			CreateStockShaders();
		}
		ShaderManager::~ShaderManager()
		{
			Renderer::Lock lock(m_rdr);
			auto dc = lock.ImmediateDC();

			// Clear all shaders
			dc->VSSetShader(0, 0, 0);
			dc->PSSetShader(0, 0, 0);
			dc->GSSetShader(0, 0, 0);
			dc->HSSetShader(0, 0, 0);
			dc->DSSetShader(0, 0, 0);
		}

		// Create the built-in shaders
		void ShaderManager::CreateStockShaders()
		{
			// Forward shaders
			CreateShader<FwdShaderVS>();
			CreateShader<FwdShaderPS>();

			// GBuffer shaders
			CreateShader<GBufferShaderVS>();
			CreateShader<GBufferShaderPS>();
			CreateShader<DSLightingShaderVS>();
			CreateShader<DSLightingShaderPS>();

			// Shadow map shaders
			CreateShader<ShadowMapVS>();
			CreateShader<ShadowMapFaceGS>();
			CreateShader<ShadowMapLineGS>();
			CreateShader<ShadowMapPS>();

			// Other shaders
			CreateShader<ThickLineListShaderGS>();
			CreateShader<ArrowHeadShaderGS>();
		}

		// Get (or create) a d3d resource of type 'TRes'.
		template <typename TRes, typename TCreate, typename TLookup = Lookup<RdrId, D3DPtr<TRes>>>
		D3DPtr<TRes> Get(RdrId id, TLookup& lookup, TCreate create)
		{
			// If 'id' is AutoId, the caller wants a new instance.
			if (id != AutoId)
			{
				// Check the lookup table as it may already exist
				auto iter = lookup.find(id);
				if (iter != std::end(lookup))
					return iter->second;
			}

			// Doesn't already exist, or the caller wants a new instance
			auto res = create();

			// Add it to the lookup
			id = id == AutoId ? MakeId(res.m_ptr) : id;
			AddLookup(lookup, id, res);
			return std::move(res);
		}

		// Get (or create) an input layout
		D3DPtr<ID3D11InputLayout> ShaderManager::GetIP(RdrId id, VShaderDesc const* desc)
		{
			std::lock_guard<std::recursive_mutex> lock0(m_mutex);

			// Note: we need an input layout per vertex shader because the CreateInputLayout
			// validates the input layout for the vertex shader and can make changes if there
			// is a difference.
			return Get<ID3D11InputLayout>(id, m_lookup_ip, [&]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Input layout description not provided");
				
				Renderer::Lock lock1(m_rdr);
				D3DPtr<ID3D11InputLayout> ip;
				pr::Throw(lock1.D3DDevice()->CreateInputLayout(desc->m_iplayout, UINT(desc->m_iplayout_count), desc->m_data, desc->m_size, &ip.m_ptr));
				return std::move(ip);
			});
		}

		// Get (or create) a vertex shader.
		D3DPtr<ID3D11VertexShader> ShaderManager::GetVS(RdrId id, VShaderDesc const* desc)
		{
			std::lock_guard<std::recursive_mutex> lock0(m_mutex);

			return Get<ID3D11VertexShader>(id, m_lookup_vs, [&]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Vertex shader description not provided");

				// Ensure the associated input layout exists
				GetIP(id, desc);
				
				// Attach the input layout as private data to the vertex shader
				Renderer::Lock lock1(m_rdr);
				D3DPtr<ID3D11VertexShader> vs;
				pr::Throw(lock1.D3DDevice()->CreateVertexShader(desc->m_data, desc->m_size, 0, &vs.m_ptr));
				return std::move(vs);
			});
		}

		// Get (or create) a pixel shader.
		D3DPtr<ID3D11PixelShader> ShaderManager::GetPS(RdrId id, PShaderDesc const* desc)
		{
			std::lock_guard<std::recursive_mutex> lock0(m_mutex);

			return Get<ID3D11PixelShader>(id, m_lookup_ps, [&]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Pixel shader description not provided");

				// Create the pixel shader
				Renderer::Lock lock1(m_rdr);
				D3DPtr<ID3D11PixelShader> ps;
				pr::Throw(lock1.D3DDevice()->CreatePixelShader(desc->m_data, desc->m_size, 0, &ps.m_ptr));
				return std::move(ps);
			});
		}

		// Get (or create) a geometry shader.
		D3DPtr<ID3D11GeometryShader> ShaderManager::GetGS(RdrId id, GShaderDesc const* desc)
		{
			std::lock_guard<std::recursive_mutex> lock0(m_mutex);

			return Get<ID3D11GeometryShader>(id, m_lookup_gs, [&]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Geometry shader description not provided");

				// Create the pixel shader
				Renderer::Lock lock1(m_rdr);
				D3DPtr<ID3D11GeometryShader> gs;
				pr::Throw(lock1.D3DDevice()->CreateGeometryShader(desc->m_data, desc->m_size, 0, &gs.m_ptr));
				return std::move(gs);
			});
		}

		// Add a shader instance to our map
		void ShaderManager::AddShader(ShaderPtr const& shader)
		{
			// Should already be lock_guarded
			PR_ASSERT(PR_DBG_RDR, FindShader(shader->m_id) == nullptr, "A shader with this Id already exists");

			// Set up a sort id for the shader
			shader->m_sort_id = m_lookup_shader.size() % SortKey::MaxShaderId;

			// Add the shader instance to the lookup map
			AddLookup(m_lookup_shader, shader->m_id, shader);
		}

		// Find a shader by id
		ShaderPtr ShaderManager::FindShader(RdrId id)
		{
			// AutoId means make a new shader, so it'll never exist already
			if (id == AutoId)
				return nullptr;

			std::lock_guard<std::recursive_mutex> lock(m_mutex);

			// See if 'id' already exists, if not, then we'll carry on and create a new shader.
			auto iter = m_lookup_shader.find(id);
			if (iter == std::end(m_lookup_shader))
				return nullptr;

			return iter->second;
		}

		// Create a copy of an existing shader.
		ShaderPtr ShaderManager::CloneShader(RdrId id, RdrId new_id, char const* new_name)
		{
			auto existing = FindShader(id);
			if (!existing)
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Existing shader with id %d not found", id));

			return existing->Clone(new_id, new_name);
		}

		// Get or create a 'cbuffer' object for given type 'TCBuf'
		D3DPtr<ID3D11Buffer> ShaderManager::GetCBuf(char const* name, RdrId id, size_t sz)
		{
			auto iter = m_lookup_cbuf.find(id);
			if (iter != end(m_lookup_cbuf))
				return iter->second;

			Renderer::Lock lock(m_rdr);

			// Create the 'cbuffer', add it to the lookup, and return it
			D3DPtr<ID3D11Buffer> cbuf;
			CBufferDesc cbdesc(sz);
			pr::Throw(lock.D3DDevice()->CreateBuffer(&cbdesc, 0, &cbuf.m_ptr));
			PR_EXPAND(PR_DBG_RDR, NameResource(cbuf.get(), name)); (void)name;
			m_lookup_cbuf[id] = cbuf;
			return std::move(cbuf);
		}

		//// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
		//ShaderSet ShaderManager::BuildShaderFor(EGeom geom) const
		//{
		//	//Todo
		//	//ShaderBase const* cheapest = 0; // This is the shader that matches all bits in 'geom' with the fewest extra bits
		//	//ShaderBase const* closest = 0;  // This is the shader that matches the most bits in 'geom_mask'
		//	//pr::uint cheapest_bit_count = pr::maths::uint_max;
		//	//pr::uint closest_bit_count  = 0;
		//	//for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
		//	//{
		//	//	ShaderBase const& shdr = *i->second;

		//	//	// Quick out on an exact match
		//	//	if (shdr.m_geom == geom)
		//	//		return const_cast<ShaderBase*>(&shdr); // const_cast because pr::RefPtr only handles non-const pointers

		//	//	// If the shader supports all the bits in 'geom_mask' with extras, find the cheapest
		//	//	pr::uint count = pr::CountBits<pr::uint>(shdr.m_geom);
		//	//	if (pr::AllSet(shdr.m_geom, geom))
		//	//	{
		//	//		if (cheapest_bit_count > count)
		//	//		{
		//	//			cheapest = &shdr;
		//	//			cheapest_bit_count = count;
		//	//		}
		//	//	}
		//	//	// Otherwise, find the shader that supports the most bits of 'geom_mask'
		//	//	else
		//	//	{
		//	//		if (closest_bit_count < count)
		//	//		{
		//	//			closest = &shdr;
		//	//			closest_bit_count = count;
		//	//		}
		//	//	}
		//	//}

		//	//// Choose the cheapest over the closest
		//	//if (cheapest != 0)
		//	//	return const_cast<ShaderBase*>(cheapest);
		//	//if (closest != 0)
		//	//	return const_cast<ShaderBase*>(closest);

		//	// Throw if nothing suitable is found
		//	std::string msg = pr::Fmt("No suitable shader found that supports geometry mask: %X\nAvailable Shaders:\n" ,geom);
		//	for (auto& sh : m_lookup_shader)
		//		msg += pr::Fmt("   %s - geometry mask: %X\n" ,sh.second->m_name.c_str() ,sh.second->m_geom);

		//	PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		//	throw pr::Exception<HRESULT>(ERROR_NOT_SUPPORTED, msg);
		//}
	}
}
//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/event_types.h"
#include "renderer11/util/forward_private.h"

namespace pr
{
	namespace rdr
	{
		ShaderManager::ShaderManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device)
			:m_mem(mem)
			,m_lookup_ip(mem)
			,m_lookup_vs(mem)
			,m_lookup_ps(mem)
			,m_lookup_gs(mem)
			,m_lookup_shader(mem)
			,m_lookup_cbuf(mem)
			,m_device(device)
		{
			CreateStockShaders();
		}
		ShaderManager::~ShaderManager()
		{
			auto dc = ImmediateDC(m_device);

			// Clear all shaders
			dc->VSSetShader(0, 0, 0);
			dc->PSSetShader(0, 0, 0);
			dc->GSSetShader(0, 0, 0);
			dc->HSSetShader(0, 0, 0);
			dc->DSSetShader(0, 0, 0);

			// Release the ref added in CreateShader()
			while (!m_lookup_shader.empty())
			{
				auto iter = begin(m_lookup_shader);
				PR_INFO_IF(PR_DBG_RDR, pr::PtrRefCount(iter->second) != 1, pr::FmtS("External references to shader %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
				iter->second->Release();
			}
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

		// Get (or create) a shader of type 'TShdr'.
		template <typename TShdr, typename TCreate> D3DPtr<TShdr> Get(RdrId id, typename Lookup<RdrId, D3DPtr<TShdr>>& lookup, TCreate create)
		{
			// If 'id' is AutoId, the caller wants a new instance.
			if (id != AutoId)
			{
				// Check the lookup table as it may already exist
				auto iter = lookup.find(id);
				if (iter != end(lookup))
					return iter->second;
			}

			// Doesn't already exist, or the caller wants a new instance
			// Create the shader
			D3DPtr<TShdr> shdr = create();

			// Add it to the lookup
			id = id == AutoId ? MakeId(shdr.m_ptr) : id;
			AddLookup(lookup, id, shdr.m_ptr);

			// The ShaderManager acts as a container of custom shaders.
			// We need to prevent the shader from immediately being destroyed
			// even if the caller holds no references to the shader.
			shdr->AddRef();
			return shdr;
		}

		// Get (or create) an input layout
		D3DPtr<ID3D11InputLayout> ShaderManager::GetIP(RdrId id, VShaderDesc const* desc)
		{
			// Note: we need an input layout per vertex shader because the CreateInputLayout
			// validates the input layout for the vertex shader and can make changes if there
			// is a difference.
			return Get(id, m_lookup_ip, [=]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Input layout description not provided");

				D3DPtr<ID3D11InputLayout> ip;
				pr::Throw(m_device->CreateInputLayout(desc->m_iplayout, UINT(desc->m_iplayout_count), desc->m_data, desc->m_size, &ip.m_ptr));
				return ip;
			});
		}

		// Get (or create) a vertex shader.
		D3DPtr<ID3D11VertexShader> ShaderManager::GetVS(RdrId id, VShaderDesc const* desc)
		{
			return Get(id, m_lookup_vs, [=]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Vertex shader description not provided");

				// Ensure the associated input layout exists
				GetIP(id, desc);
				
				// Attach the input layout as private data to the vertex shader
				D3DPtr<ID3D11VertexShader> vs;
				pr::Throw(m_device->CreateVertexShader(desc->m_data, desc->m_size, 0, &vs.m_ptr));
				return vs;
			});
		}

		// Get (or create) a pixel shader.
		D3DPtr<ID3D11PixelShader> ShaderManager::GetPS(RdrId id, PShaderDesc const* desc)
		{
			return Get(id, m_lookup_ps, [=]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Pixel shader description not provided");

				// Create the pixel shader
				D3DPtr<ID3D11PixelShader> ps;
				pr::Throw(m_device->CreatePixelShader(desc->m_data, desc->m_size, 0, &ps.m_ptr));
				return ps;
			});
		}

		// Get (or create) a geometry shader.
		D3DPtr<ID3D11GeometryShader> ShaderManager::GetGS(RdrId id, GShaderDesc const* desc)
		{
			return Get(id, m_lookup_gs, [=]
			{
				if (desc == nullptr)
					throw pr::Exception<HRESULT>(E_FAIL, "Geometry shader description not provided");

				// Create the pixel shader
				D3DPtr<ID3D11GeometryShader> gs;
				pr::Throw(m_device->CreateGeometryShader(desc->m_data, desc->m_size, 0, &gs.m_ptr));
				return gs;
			});
		}

		// Add a shader instance to our map
		ShaderPtr ShaderManager::InitShader(ShaderBase* shdr)
		{
			PR_ASSERT(PR_DBG_RDR, FindShader(shdr->m_id) == 0, "A shader with this Id already exists");

			// Setup a sort id for the shader
			shdr->m_sort_id = m_lookup_shader.size() % sortkey::MaxShaderId;

			// Add the shader instance to the lookup map
			AddLookup(m_lookup_shader, shdr->m_id, shdr);

			// The ShaderManager acts as a container of custom shaders.
			// We need to prevent the shader from immediately being destroyed
			// if the caller holds no references to the shader.
			shdr->AddRef();
			return shdr;
		}

		// Delete a shader instance from our map
		void ShaderManager::DestShader(ShaderBase* shdr)
		{
			if (shdr == nullptr)
				return;

			// Find 'shdr' in the map of RdrIds to shader instances
			auto iter = m_lookup_shader.find(shdr->m_id);
			PR_ASSERT(PR_DBG_RDR, iter != std::end(m_lookup_shader), "Shader not found");

			// Remove the entry from the RdrId lookup map
			m_lookup_shader.erase(iter);
		}

		// Find a shader by id
		ShaderPtr ShaderManager::FindShader(RdrId id) const
		{
			// AutoId means make a new shader, so it'll never exist already
			if (id == AutoId)
				return nullptr;

			// See if 'id' already exists, if not, then we'll carry on and create a new shader.
			auto iter = m_lookup_shader.find(id);
			if (iter == std::end(m_lookup_shader))
				return nullptr;

			ShaderBase& existing = *(iter->second);
			return &existing;
		}

		// Create a copy of an existing shader.
		ShaderPtr ShaderManager::CloneShader(RdrId id, RdrId new_id, char const* new_name)
		{
			auto existing = FindShader(id);
			if (!existing)
				throw pr::Exception<HRESULT>(E_FAIL, pr::FmtS("Existing shader with id %d not found", id));

			return existing->Clone(new_id, new_name);
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
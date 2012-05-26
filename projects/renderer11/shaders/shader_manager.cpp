//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/dds_texture_loader.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"

using namespace pr::rdr;

GUID const TexInfoGUID = {0x506e436e, 0x5a4f, 0x4190, 0x98, 0x43, 0x99, 0x7a, 0x19, 0xa8, 0xd8, 0x69}; // {506E436E-5A4F-4190-9843-997A19A8D869}

pr::rdr::ShaderManager::ShaderManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_alex_shader(Allocator<Shader>(mem))
,m_device(device)
,m_lookup_shader(mem)
{
	CreateStockShaders(*this);
}
pr::rdr::ShaderManager::~ShaderManager()
{
	// Release the ref added in CreateShader()
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
		i->second->Release();
}

// Create a basic shader.
// Clients should call this method to create the basic shader object and register it
// with the shader manager. Afterwards they can use the shader pointer returned to setup
// the specific properties of the shader. Pass nulls for unneeded shader descriptions
pr::rdr::ShaderPtr pr::rdr::ShaderManager::CreateShader(RdrId id, BindShaderFunc bind_func, VShaderDesc const* vsdesc, PShaderDesc const* psdesc)
{
	// If the user has provided a specific id for the shader, look for an existing
	// shader instance with the same name and return it.
	if (id != AutoId)
	{
		// See if 'id' already exists, if not, then we'll carry on and create a new shader.
		ShaderLookup::const_iterator iter = m_lookup_shader.find(id);
		if (iter != m_lookup_shader.end())
		{
			PR_ASSERT(PR_DBG_RDR, vsdesc == 0 && psdesc == 0, "data provided for an existing shader");
			Shader& existing = *(iter->second);
			return &existing;
		}
	}
	
	// Allocate the shader instance
	pr::rdr::ShaderPtr inst = m_alex_shader.New();
	
	// If 'id' doesn't exist (or is Auto), allocate a new shader
	if (vsdesc != 0)
	{
		// Create the shader
		pr::Throw(m_device->CreateVertexShader(vsdesc->m_data, vsdesc->m_size, 0, &inst->m_vs.m_ptr));
		
		// Create the input layout
		pr::Throw(m_device->CreateInputLayout(vsdesc->m_iplayout, UINT(vsdesc->m_iplayout_count), vsdesc->m_data, vsdesc->m_size, &inst->m_iplayout.m_ptr));
		
		// Set the minimum vertex format mask
		inst->m_geom_mask = vsdesc->m_geom_mask;
	}
	if (psdesc != 0)
	{
		// Create the pixel shader
		pr::Throw(m_device->CreatePixelShader(psdesc->m_data, psdesc->m_size, 0, &inst->m_ps.m_ptr));
	}
	
	// Populate the remaining shader instance variables
	inst->m_id   = id == AutoId ? MakeId(inst.m_ptr) : id;
	inst->m_mgr  = this;
	inst->Setup  = bind_func;
	inst->m_name = L"";
	inst->m_sort_id = m_lookup_shader.size() % pr::rdr::sortkey::MaxShaderId;
	PR_ASSERT(PR_DBG_RDR, !m_lookup_shader.count(inst->m_id), "overwriting an existing shader id");
	m_lookup_shader[inst->m_id] = inst.m_ptr;
	
	// We need to prevent the shader from immediately being destroyed
	// This ref is removed in the destructor
	inst->AddRef();
	return inst;
}

// Delete a shader instance
void pr::rdr::ShaderManager::Delete(pr::rdr::Shader const* shdr)
{
	if (shdr == 0) return;
	
	// Find 'shdr' in the map of RdrIds to shader instances
	// We'll remove this, but first use it as a non-const reference
	ShaderLookup::iterator iter = m_lookup_shader.find(shdr->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_lookup_shader.end(), "Shader not found");
	
	// Delete the shader and remove the entry from the RdrId lookup map
	m_alex_shader.Delete(iter->second);
	m_lookup_shader.erase(iter);
}

// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
pr::rdr::ShaderPtr pr::rdr::ShaderManager::FindShaderFor(EGeom::Type geom_mask) const
{
	pr::rdr::Shader const* closest = 0;
	pr::uint matching_bit_count = 0;
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
	{
		Shader const& shdr = *i->second;
		
		// Doesn't meet minimum requirements?
		if (!pr::AllSet(geom_mask, shdr.m_geom_mask))
			continue;
		
		// Quick out on an exact match
		if (shdr.m_geom_mask == geom_mask)
			closest = &shdr;
		
		// Otherwise find the shader that uses the most fields of geom_mask
		// 'geom_mask' has all of the 'shdr.m_geom_mask' bits set, plus some extra bits
		// so just finding the 'shdr.m_geom_mask' with the most bits will find the best match
		pr::uint match_count = pr::CountBits((pr::uint)shdr.m_geom_mask);
		if (match_count >= matching_bit_count)
		{
			// Typically, more complex shaders have higher valued geom masks, when the number
			// of matching bits is equal choose the highest mask value to (hopefully) get the better shader
			if (match_count == matching_bit_count && closest != 0 && shdr.m_geom_mask < closest->m_geom_mask)
				continue;
			
			matching_bit_count = match_count;
			closest = &shdr;
		}
	}
	
	// Throw if nothing suitable is found
	if (matching_bit_count == 0)
	{
		std::string msg = pr::Fmt("No suitable shader found that supports geometry mask: %X\nAvailable Shaders:\n" ,geom_mask);
		for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
			msg += pr::Fmt("   %s - geometry mask: %X\n" ,i->second->m_name.c_str() ,i->second->m_geom_mask);
		
		PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
		throw pr::Exception<HRESULT>(E_FAIL, msg);
	}
	return const_cast<Shader*>(closest);
}

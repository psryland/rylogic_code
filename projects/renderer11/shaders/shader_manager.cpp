//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/render/sortkey.h"

using namespace pr::rdr;

pr::rdr::ShaderManager::ShaderManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_mem(mem)
,m_device(device)
,m_lookup_shader(mem)
{
	CreateStockShaders();
}
pr::rdr::ShaderManager::~ShaderManager()
{
	// Clear all shaders
	auto dc = ImmediateDC(m_device);
	dc->VSSetShader(0, 0, 0);
	dc->PSSetShader(0, 0, 0);
	dc->GSSetShader(0, 0, 0);
	dc->HSSetShader(0, 0, 0);
	dc->DSSetShader(0, 0, 0);
	
	// Release the ref added in CreateShader()
	while (!m_lookup_shader.empty())
	{
		auto iter = begin(m_lookup_shader);
		PR_INFO_EXP(PR_DBG_RDR, pr::PtrRefCount(iter->second) == 1, pr::FmtS("External references to shader %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
}

// Builds the basic parts of a shader.
pr::rdr::ShaderPtr pr::rdr::ShaderManager::InitShader(ShaderAlex create, RdrId id, ShaderSetupFunc setup, VShaderDesc const* vsdesc, PShaderDesc const* psdesc)
{
	D3DPtr<ID3D11InputLayout>     iplayout;
	D3DPtr<ID3D11VertexShader>    vs;
	D3DPtr<ID3D11PixelShader>     ps;
	EGeom::Type geom_mask = 0;
	
	// If 'id' doesn't exist (or is Auto), allocate a new shader
	if (vsdesc != 0)
	{
		// Create the shader
		pr::Throw(m_device->CreateVertexShader(vsdesc->m_data, vsdesc->m_size, 0, &vs.m_ptr));
		
		// Create the input layout
		pr::Throw(m_device->CreateInputLayout(vsdesc->m_iplayout, UINT(vsdesc->m_iplayout_count), vsdesc->m_data, vsdesc->m_size, &iplayout.m_ptr));
		
		// Set the minimum vertex format mask
		geom_mask = vsdesc->m_geom_mask;
	}
	if (psdesc != 0)
	{
		// Create the pixel shader
		pr::Throw(m_device->CreatePixelShader(psdesc->m_data, psdesc->m_size, 0, &ps.m_ptr));
	}
	
	// Allocate the new shader instance
	ShaderPtr shdr = create(this);
	shdr->m_iplayout  = iplayout;
	shdr->m_vs        = vs;
	shdr->m_ps        = ps;
	shdr->m_id        = id == AutoId ? MakeId(shdr.m_ptr) : id;
	shdr->Setup       = setup;
	shdr->m_name      = EShader::ToString(shdr->m_id);
	shdr->m_geom_mask = geom_mask;
	shdr->m_sort_id   = m_lookup_shader.size() % pr::rdr::sortkey::MaxShaderId;
	PR_ASSERT(PR_DBG_RDR, FindShader(shdr->m_id) == 0, "A shader with this Id already exists");
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(shdr->m_vs, pr::FmtS("vshdr <RdrId:%d>", shdr->m_id)));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(shdr->m_ps, pr::FmtS("pshdr <RdrId:%d>", shdr->m_id)));
	PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(shdr->m_iplayout, pr::FmtS("iplayout <RdrId:%d>", shdr->m_id)));
	AddLookup(m_lookup_shader, shdr->m_id, shdr.m_ptr);
	
	// The ShaderManager acts as a container of custom shaders. We need to
	// prevent the shader from immediately being destroyed even if the caller
	// holds no references to the shader
	shdr->AddRef();
	return shdr;
}

// Delete a shader instance
void pr::rdr::ShaderManager::Delete(pr::rdr::BaseShader const* shdr)
{
	if (shdr == 0) return;
	
	// Find 'shdr' in the map of RdrIds to shader instances
	// We'll remove this, but first use it as a non-const reference
	ShaderLookup::iterator iter = m_lookup_shader.find(shdr->m_id);
	PR_ASSERT(PR_DBG_RDR, iter != m_lookup_shader.end(), "Shader not found");
	
	// Delete the shader and remove the entry from the RdrId lookup map
	Allocator<BaseShader>(m_mem).Delete(iter->second);
	m_lookup_shader.erase(iter);
}

// Find a shader by id
pr::rdr::ShaderPtr pr::rdr::ShaderManager::FindShader(RdrId id) const
{
	// AutoId means make a new shader, so it'll never exist already
	if (id == AutoId)
		return 0;
	
	// See if 'id' already exists, if not, then we'll carry on and create a new shader.
	ShaderLookup::const_iterator iter = m_lookup_shader.find(id);
	if (iter == m_lookup_shader.end())
		return 0;
	
	BaseShader& existing = *(iter->second);
	return &existing;
}

// Return a pointer to a shader that is best suited for rendering geometry with the vertex structure described by 'geom_mask'
pr::rdr::ShaderPtr pr::rdr::ShaderManager::FindShaderFor(EGeom::Type geom_mask) const
{
	pr::rdr::BaseShader const* cheapest = 0; // This is the shader that matches all bits in 'geom_mask' with the fewest extra bits
	pr::rdr::BaseShader const* closest = 0;  // This is the shader that matches the most bits in 'geom_mask'
	pr::uint cheapest_bit_count = pr::maths::uint_max;
	pr::uint closest_bit_count  = 0;
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
	{
		BaseShader const& shdr = *i->second;
		
		// Quick out on an exact match
		if (shdr.m_geom_mask == geom_mask)
			return const_cast<BaseShader*>(&shdr); // const_cast because pr::RefPtr only handles non-const pointers
		
		// If the shader supports all the bits in 'geom_mask' with extras, find the cheapest
		pr::uint count = pr::CountBits(shdr.m_geom_mask);
		if (pr::AllSet(shdr.m_geom_mask, geom_mask))
		{
			if (cheapest_bit_count > count)
			{
				cheapest = &shdr;
				cheapest_bit_count = count;
			}
		}
		// Otherwise, find the shader that supports the most bits of 'geom_mask'
		else
		{
			if (closest_bit_count < count)
			{
				closest = &shdr;
				closest_bit_count = count;
			}
		}
	}
	
	// Choose the cheapest over the closest
	if (cheapest != 0)
		return const_cast<BaseShader*>(cheapest);
	if (closest != 0)
		return const_cast<BaseShader*>(closest);
	
	// Throw if nothing suitable is found
	std::string msg = pr::Fmt("No suitable shader found that supports geometry mask: %X\nAvailable Shaders:\n" ,geom_mask);
	for (auto i = begin(m_lookup_shader), iend = end(m_lookup_shader); i != iend; ++i)
		msg += pr::Fmt("   %s - geometry mask: %X\n" ,i->second->m_name.c_str() ,i->second->m_geom_mask);
	
	PR_ASSERT(PR_DBG_RDR, false, msg.c_str());
	throw pr::Exception<HRESULT>(ERROR_NOT_SUPPORTED, msg);
}

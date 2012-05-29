//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/dds_texture_loader.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/util.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/render/drawlist_element.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/render/sortkey.h"
#include "pr/renderer11/util/lock.h"
#include "renderer11/shaders/cbuffer.h"

using namespace pr::rdr;

namespace pr
{
	namespace rdr
	{
		// include generated header files
		#include "renderer11/shaders/shaders/vs_txfm_tint.h"
		#include "renderer11/shaders/shaders/ps_txfm_tint.h"
	}
}

pr::rdr::ShaderManager::ShaderManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_alex_shader(Allocator<Shader>(mem))
,m_device(device)
,m_lookup_shader(mem)
{
	CreateStockShaders();
}
pr::rdr::ShaderManager::~ShaderManager()
{
	// Release the ref added in CreateShader()
	while (!m_lookup_shader.empty())
	{
		auto iter = begin(m_lookup_shader);
		PR_INFO_EXP(PR_DBG_RDR, pr::PtrRefCount(iter->second) == 1, pr::FmtS("External references to shader %d - %s still exist", iter->second->m_id, iter->second->m_name.c_str()));
		iter->second->Release();
	}
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
	id = id == AutoId ? MakeId(inst.m_ptr) : id;
	
	// If 'id' doesn't exist (or is Auto), allocate a new shader
	if (vsdesc != 0)
	{
		// Create the shader
		pr::Throw(m_device->CreateVertexShader(vsdesc->m_data, vsdesc->m_size, 0, &inst->m_vs.m_ptr));
		PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(inst->m_vs, pr::FmtS("vshdr <RdrId:%d>", id)));
		
		// Create the input layout
		pr::Throw(m_device->CreateInputLayout(vsdesc->m_iplayout, UINT(vsdesc->m_iplayout_count), vsdesc->m_data, vsdesc->m_size, &inst->m_iplayout.m_ptr));
		PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(inst->m_iplayout, pr::FmtS("iplayout <RdrId:%d>", id)));
		
		// Set the minimum vertex format mask
		inst->m_geom_mask = vsdesc->m_geom_mask;
	}
	if (psdesc != 0)
	{
		// Create the pixel shader
		pr::Throw(m_device->CreatePixelShader(psdesc->m_data, psdesc->m_size, 0, &inst->m_ps.m_ptr));
		PR_EXPAND(PR_DBG_RDR, pr::rdr::NameResource(inst->m_ps, pr::FmtS("pshdr <RdrId:%d>", id)));
	}
	
	// Populate the remaining shader instance variables
	inst->m_id   = id;
	inst->m_mgr  = this;
	inst->Setup  = bind_func;
	inst->m_name = "";
	inst->m_sort_id = m_lookup_shader.size() % pr::rdr::sortkey::MaxShaderId;
	AddLookup(m_lookup_shader, inst->m_id, inst.m_ptr);
	
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

// Create the built-in shaders
void pr::rdr::ShaderManager::CreateStockShaders()
{
	// Helper function for setting up the two standard constant buffers for a shader
	auto CreateCBufModel = [this](pr::rdr::ShaderPtr& shdr)
	{
		shdr->m_cbuf.resize(1);
		CBufferDesc cbdesc(sizeof(CBufModel));
		pr::Throw(m_device->CreateBuffer(&cbdesc, 0, &shdr->m_cbuf[0].m_ptr));
	};

	{//TxTint
		BindShaderFunc map = [](D3DPtr<ID3D11DeviceContext>& dc, pr::rdr::DrawMethod const& meth, Nugget const&, BaseInstance const& inst, SceneView const& view)
		{
			pr::m4x4 o2s = pr::GetInverse(view.m_c2w) * GetO2W(inst);
			pr::Colour const* col = inst.find<pr::Colour>(EInstComp::TintColour32);
			
			CBufModel cb = {};
			cb.m_o2s = o2s;
			cb.m_tint = col ? *col : pr::ColourWhite;
			*Lock(dc, meth.m_shader->m_cbuf[0], 0, D3D11_MAP_WRITE_DISCARD, 0).ptr<CBufModel>() = cb;
		};
		
		// Create the basic shader
		VShaderDesc vsdesc(VertP(), vs_txfm_tint, sizeof(vs_txfm_tint));
		PShaderDesc psdesc(ps_txfm_tint, sizeof(ps_txfm_tint));
		pr::rdr::ShaderPtr shdr = CreateShader(shader::TxTint, map, &vsdesc, &psdesc);
		CreateCBufModel(shdr);
	}


//// Setup this shader for rendering
//void pr::rdr::Shader::Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, SceneView const& view)
//{
//	(void)view;
//	(void)dle; //todo textures
//	
//	// Configure the constants buffer for this shader
////todo	m_map(dc, m_constants, dle, view);
//	
//	// Bind the constant buffer to the device
////todo	dc->VSSetConstantBuffers(0, 1, &m_constants.m_ptr);
//	
//	// Apply the blend state if present
//	if (m_blend_state)
//		dc->OMSetBlendState(m_blend_state.m_ptr, 0, 0xffffffff);
//	
//	// Apply the rasterizer state if present
//	if (m_rast_state)
//		dc->RSSetState(m_rast_state.m_ptr);
//	
//	// Apply the depth buffer state if present
//	if (m_depth_state)
//		dc->OMSetDepthStencilState(m_depth_state.m_ptr, 0);
//}

}

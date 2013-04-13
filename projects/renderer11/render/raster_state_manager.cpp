//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/render/raster_state_manager.h"
#include "pr/renderer11/util/wrappers.h"

using namespace pr::rdr;

// Create the commonly used raster states
void CreateStockRasterStates(pr::rdr::RasterStateManager& rsm)
{
	rsm.RasterState(ERasterState::SolidCullNone  ,RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_NONE));
	rsm.RasterState(ERasterState::SolidCullBack  ,RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_BACK));
	rsm.RasterState(ERasterState::SolidCullFront ,RasterizerDesc(D3D11_FILL_SOLID, D3D11_CULL_FRONT));
	rsm.RasterState(ERasterState::WireCullNone   ,RasterizerDesc(D3D11_FILL_WIREFRAME, D3D11_CULL_NONE));
}

pr::rdr::RasterStateManager::RasterStateManager(pr::rdr::MemFuncs& mem, D3DPtr<ID3D11Device>& device)
:m_device(device)
,m_lookup_rs(mem)
{
	CreateStockRasterStates(*this);
}
pr::rdr::RasterStateManager::~RasterStateManager()
{
	// Release the ref added in RasterState()
	while (!m_lookup_rs.empty())
	{
		auto iter = begin(m_lookup_rs);
		#if PR_DBG_RDR
		long ref_count = pr::PtrRefCount(iter->second);
		PR_INFO_EXP(PR_DBG_RDR, ref_count == 1, pr::FmtS("%d external references to raster state %d still exist" ,ref_count ,iter->first));
		#endif
		iter->second->Release();
		m_lookup_rs.erase(iter);
	}
}

// Return a stock rasterizer state 
D3DPtr<ID3D11RasterizerState> pr::rdr::RasterStateManager::RasterState(RdrId id, RasterizerDesc const& desc, RdrId* out_id)
{
	// Look for an existing raster state object
	auto rs = id != AutoId ? RasterState(id) : 0;
	if (!rs)
	{
		pr::Throw(m_device->CreateRasterizerState(&desc, &rs.m_ptr));
		id = id == AutoId ? MakeId(rs.m_ptr) : id;
		AddLookup(m_lookup_rs, id, rs.m_ptr);
		rs->AddRef(); // Add a ref since it's in our lookup map
	}
	if (out_id) *out_id = id;
	return rs;
}

// Get a pre-existing raster state by it's id
D3DPtr<ID3D11RasterizerState> pr::rdr::RasterStateManager::RasterState(RdrId id)
{
	auto i = m_lookup_rs.find(id);
	return i != m_lookup_rs.end() ? i->second : 0;
}

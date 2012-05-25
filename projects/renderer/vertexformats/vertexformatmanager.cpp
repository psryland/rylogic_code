//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/VertexFormats/VertexFormatManager.h"
#include "pr/renderer/Renderer/renderer.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::vf;

namespace pr
{
	namespace rdr
	{
		namespace vf
		{
			// PosNormTex
			D3DVERTEXELEMENT9 g_vd_PosNormDiffTex[] = 
			{
				{0, offsetof(PosNormDiffTex, m_vertex), D3DDECLTYPE_FLOAT3,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0},
				{0, offsetof(PosNormDiffTex, m_normal), D3DDECLTYPE_FLOAT3,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_NORMAL,   0},
				{0, offsetof(PosNormDiffTex, m_colour), D3DDECLTYPE_D3DCOLOR, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_COLOR,    0},
				{0, offsetof(PosNormDiffTex, m_tex   ), D3DDECLTYPE_FLOAT2,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 0},
				D3DDECL_END()
			};
			// PosNormDiffTexFuture
			D3DVERTEXELEMENT9 g_vd_PosNormDiffTexFuture[] = 
			{
				{0, offsetof(PosNormDiffTexFuture, m_vertex), D3DDECLTYPE_FLOAT3,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_POSITION, 0},
				{0, offsetof(PosNormDiffTexFuture, m_normal), D3DDECLTYPE_FLOAT3,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_NORMAL,   0},
				{0, offsetof(PosNormDiffTexFuture, m_colour), D3DDECLTYPE_D3DCOLOR, D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_COLOR,    0},
				{0, offsetof(PosNormDiffTexFuture, m_tex   ), D3DDECLTYPE_FLOAT2,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 0},
				{0, offsetof(PosNormDiffTexFuture, m_future), D3DDECLTYPE_FLOAT4,   D3DDECLMETHOD_DEFAULT, D3DDECLUSAGE_TEXCOORD, 1},
				D3DDECL_END()
			};

			// Array of all vertex declarations
			D3DVERTEXELEMENT9* g_vd_pointers[EVertType::NumberOf] =
			{
				g_vd_PosNormDiffTex,
				g_vd_PosNormDiffTexFuture
			};
		}
	}
}
	
// Constructor
VertexFormatManager::VertexFormatManager(D3DPtr<IDirect3DDevice9> d3d_device)
:pr::events::IRecv<pr::rdr::Evt_DeviceLost>(EDeviceResetPriority::VertexFormatManager)
,pr::events::IRecv<pr::rdr::Evt_DeviceRestored>(EDeviceResetPriority::VertexFormatManager)
,m_d3d_device(d3d_device)
{
	OnEvent(pr::rdr::Evt_DeviceRestored(d3d_device));
}
VertexFormatManager::~VertexFormatManager()
{
	// Make sure we're able to release all of the vertex declarations
	if (m_d3d_device) m_d3d_device->SetVertexDeclaration(0);
}
	
// Release the vertex declarations
void pr::rdr::VertexFormatManager::OnEvent(pr::rdr::Evt_DeviceLost const&)
{
	// Make sure we're able to release all of the vertex declarations
	m_d3d_device->SetVertexDeclaration(0);
	
	for (uint i = 0; i < EVertType::NumberOf; ++i) { m_vd[i] = 0; }
	m_d3d_device = 0;
}
	
// Create the vertex declarations
void pr::rdr::VertexFormatManager::OnEvent(pr::rdr::Evt_DeviceRestored const& e)
{
	m_d3d_device = e.m_d3d_device;
	for (uint i = 0; i < EVertType::NumberOf; ++i)
		Throw(m_d3d_device->CreateVertexDeclaration(vf::g_vd_pointers[i], &m_vd[i].m_ptr));
}
	


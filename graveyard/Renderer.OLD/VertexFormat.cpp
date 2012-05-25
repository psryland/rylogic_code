//****************************************************************************
//
//	Vertex Format
//
//****************************************************************************
#include "Stdafx.h"
#include "PR/Renderer/VertexFormat.h"
#include "PR/Renderer/Renderer.h"
#include "PR/Geometry/PRGeometry.h"

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
				{0,	offsetof(PosNormDiffTex, m_vertex	),	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
				{0,	offsetof(PosNormDiffTex, m_normal	),	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
				{0,	offsetof(PosNormDiffTex, m_colour	),	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
				{0,	offsetof(PosNormDiffTex, m_tex		),	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
				D3DDECL_END()
			};
			// PosNormDiffTexFuture
			D3DVERTEXELEMENT9 g_vd_PosNormDiffTexFuture[] = 
			{
				{0,	offsetof(PosNormDiffTexFuture, m_vertex	),	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
				{0,	offsetof(PosNormDiffTexFuture, m_normal	),	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
				{0,	offsetof(PosNormDiffTexFuture, m_colour	),	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
				{0,	offsetof(PosNormDiffTexFuture, m_tex	),	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
				{0,	offsetof(PosNormDiffTexFuture, m_future	),	D3DDECLTYPE_FLOAT4,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	1},
				D3DDECL_END()
			};

			// Array of all vertex declarations
			D3DVERTEXELEMENT9* g_vd_pointers[EType_NumberOf] =
			{
				g_vd_PosNormDiffTex,
				g_vd_PosNormDiffTexFuture
			};
		}//namespace vf
	}//namespace rdr
}//namespace pr

//****************************************************************************
// vf::Manager methods
Manager::Manager(D3DPtr<IDirect3DDevice9> d3d_device)
:m_d3d_device(d3d_device)
{
	CreateDeviceDependentObjects(d3d_device);
}
Manager::~Manager()
{
	// Make sure we're able to release all of the vertex declarations
	if( m_d3d_device ) m_d3d_device->SetVertexDeclaration(0);
}

// Create the vertex declarations
void Manager::CreateDeviceDependentObjects(D3DPtr<IDirect3DDevice9>	d3d_device)
{
	m_d3d_device = d3d_device;
	for( uint i = 0; i < EType_NumberOf; ++i )
	{
		Verify(m_d3d_device->CreateVertexDeclaration(g_vd_pointers[i], &m_vd[i].m_ptr));
	}
}

// Release the vertex declarations
void Manager::ReleaseDeviceDependentObjects()
{
	// Make sure we're able to release all of the vertex declarations
	m_d3d_device->SetVertexDeclaration(0);
	
	for( uint i = 0; i < EType_NumberOf; ++i )	{ m_vd[i] = 0; }
	m_d3d_device = 0;
}

//****************************************************************************
// vf::Iter methods
namespace pr
{
	namespace rdr
	{
		namespace vf
		{
			struct MemberOffsets
			{
				int	m_vertex;
				int m_normal;
				int	m_colour;
				int m_tex;
			};

			static const MemberOffsets g_MemberOffsets[] = 
			{
				// PosNormTex
				{ offsetof(PosNormDiffTex, m_vertex), offsetof(PosNormDiffTex, m_normal), offsetof(PosNormDiffTex, m_colour), offsetof(PosNormDiffTex, m_tex) },	
				// PosNormDiffTexFuture
				{ offsetof(PosNormDiffTexFuture, m_vertex), offsetof(PosNormDiffTexFuture, m_normal), offsetof(PosNormDiffTexFuture, m_colour), offsetof(PosNormDiffTexFuture, m_tex) },	
				// Invalid
				{ -1, -1, -1, -1 }
			};
		}//namespace vf
	}//namespace rdr
}//namespace pr

// Variables to use if the vertex does not contain the corresponding component
v3			g_vertex_dummy;
v3			g_norm_dummy;
Colour32	g_colour_dummy;
v2			g_tex_dummy;

//*****
// Construction
Iter::Iter()
:m_iter		(0)
,m_vf		(EType_Invalid)
,m_format	(0)
,m_size		(0)
,m_ofs		(0)
{
	PR_ERROR(PR_DBG_RDR);
}
Iter::Iter(void* vertex_buffer, Type type)
:m_iter		(static_cast<uint8*>(vertex_buffer))
,m_vf		(type)
,m_format	(GetFormat(type))
,m_size		(GetSize(type))
,m_ofs		(&g_MemberOffsets[type])
{	PR_ASSERT_STR(PR_DBG_RDR, type < EType_NumberOf, "Unknown vertex format type"); }

//*****
// Member access
v3& Iter::Vertex()
{
	if( m_format & EFormat_Pos )	return *reinterpret_cast<v3*>(m_iter + m_ofs->m_vertex);
	return g_vertex_dummy;
}
v3& Iter::Normal()
{
	if( m_format & EFormat_Norm )	return *reinterpret_cast<v3*>(m_iter + m_ofs->m_normal);
	return g_norm_dummy;
}
Colour32& Iter::Colour()
{
	if( m_format & EFormat_Diff )	return *reinterpret_cast<Colour32*>(m_iter + m_ofs->m_colour);
	return g_colour_dummy;
}
v2& Iter::Tex()
{ 
	if( m_format & EFormat_Tex )	return *reinterpret_cast<v2*>(m_iter + m_ofs->m_tex);
	return g_tex_dummy;	
}

//*****
// Set a vertex based on a Vertex
void Iter::Set(const pr::Vertex& vertex)
{
	if( m_format & EFormat_Pos	)	Vertex() = v3::construct(vertex.m_vertex);
	if( m_format & EFormat_Norm	)	Normal() = v3::construct(vertex.m_normal);
	if( m_format & EFormat_Diff	)	Colour() = vertex.m_colour;
	if( m_format & EFormat_Tex	)	Tex()    = vertex.m_tex_vertex;
}

//*****
// Set a vertex explicitly
void Iter::Set(const v4& pos, const v4& norm, Colour32 col, const v2& uv)
{
	if( m_format & EFormat_Pos	)	Vertex() = v3::construct(pos);
	if( m_format & EFormat_Norm	)	Normal() = v3::construct(norm);
	if( m_format & EFormat_Diff	)	Colour() = col;
	if( m_format & EFormat_Tex	)	Tex()	 = uv;
}


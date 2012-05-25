//***************************************************************************
//
//	FVFVertexIter 
//
//***************************************************************************

#include "Stdafx.h"
#include "FVFStructs.h"
#include "Renderer/Renderer.h"

struct FVFMemberOffsets
{
	int	m_xyz;
	int m_normal;
	int	m_diffuse;
	int m_tex;
};

static const FVFMemberOffsets g_FVFMemberOffsets[] = 
{
//  xyz , normal    , diffuse                   , tex
	{// FVF_XYZ
		offsetof(XYZ, m_vertex),
		-1,
		-1,
		-1
	},	
	{// FVF_XYZ_NORMAL
		offsetof(XYZ_NORMAL, m_vertex),
		offsetof(XYZ_NORMAL, m_normal),
		-1,
		-1
	},	
	{// FVF_XYZ_DIFFUSE
		offsetof(XYZ_DIFFUSE, m_vertex),
		-1,
		offsetof(XYZ_DIFFUSE, m_colour),
		-1
	},
	{// FVF_XYZ_NORMAL_DIFFUSE
		offsetof(XYZ_NORMAL_DIFFUSE, m_vertex),
		offsetof(XYZ_NORMAL_DIFFUSE, m_normal),
		offsetof(XYZ_NORMAL_DIFFUSE, m_colour),
		-1
	},
	{// FVF_XYZ_TEX1
		offsetof(XYZ_TEX1, m_vertex),
		-1,
		-1,
		offsetof(XYZ_TEX1, m_tex),
	},
	{// FVF_XYZ_NORMAL_TEX1
		offsetof(XYZ_NORMAL_TEX1, m_vertex),
		offsetof(XYZ_NORMAL_TEX1, m_normal),
		-1,
		offsetof(XYZ_NORMAL_TEX1, m_tex),
	},
	{// FVF_XYZ_DIFFUSE_TEX1
		offsetof(XYZ_DIFFUSE_TEX1, m_vertex),
		-1,
		offsetof(XYZ_DIFFUSE_TEX1, m_colour),
		offsetof(XYZ_DIFFUSE_TEX1, m_tex),
	},
	{// FVF_XYZ_NORMAL_DIFFUSE_TEX1
		offsetof(XYZ_NORMAL_DIFFUSE_TEX1, m_vertex),
		offsetof(XYZ_NORMAL_DIFFUSE_TEX1, m_normal),
		offsetof(XYZ_NORMAL_DIFFUSE_TEX1, m_colour),
		offsetof(XYZ_NORMAL_DIFFUSE_TEX1, m_tex),
	},
	{// INVALID_FVF
		-1,
		-1,
		-1,
		-1
	}
};

//*****
// Variables to use if the fvf vertex does not contain the corresponding component
v3			g_vertex_dummy;
v3			g_norm_dummy;
D3DCOLOR	g_colour_dummy;
v2			g_tex_dummy;

FVFVertexIter::FVFVertexIter()
:m_iter		(0)
,m_fvf		(INVALID_FVF)
,m_size		(0)
,m_format	(0)
,m_ofs		(&g_FVFMemberOffsets[INVALID_FVF])
{
	PR_ERROR();
}

FVFVertexIter::FVFVertexIter(void* vertex_buffer, FVF fvf)
:m_iter		(static_cast<uint8*>(vertex_buffer))
,m_fvf		(fvf)
,m_size		(FVFSize(fvf))
,m_format	(FVFFormat(fvf))
,m_ofs		(&g_FVFMemberOffsets[fvf])
{
	PR_ASSERT_STR(fvf < NUMBER_OF_FVFS, "Unknown FVF Type");
}

//*****
// Member access
v3& FVFVertexIter::Vertex()
{
	if( m_format & D3DFVF_XYZ )		return *reinterpret_cast<v3*>(m_iter + m_ofs->m_xyz);
	return g_vertex_dummy;
}
v3& FVFVertexIter::Normal()
{
	if( m_format & D3DFVF_NORMAL )	return *reinterpret_cast<v3*>(m_iter + m_ofs->m_normal);
	return g_norm_dummy;
}
D3DCOLOR& FVFVertexIter::Colour()
{
	if( m_format & D3DFVF_DIFFUSE )	return *reinterpret_cast<D3DCOLOR*>(m_iter + m_ofs->m_diffuse);
	return g_colour_dummy;
}
v2& FVFVertexIter::Tex()
{ 
	if( m_format & D3DFVF_TEX1 )	return *reinterpret_cast<v2*>(m_iter + m_ofs->m_tex);
	return g_tex_dummy;	
}

//*****
// Set a vertex based on a PR::Vertex
void FVFVertexIter::Set(const PR::Vertex& vertex)
{
	if( m_format & D3DFVF_XYZ		)	Vertex().Set(vertex.m_vertex);
	if( m_format & D3DFVF_NORMAL	)	Normal().Set(vertex.m_normal);
	if( m_format & D3DFVF_DIFFUSE	)	Colour() = vertex.m_colour;
	if( m_format & D3DFVF_TEX1		)	Tex()	.Set(vertex.m_tex_vertex);
}

//*****
// Set a vertex explicitly
void FVFVertexIter::Set(const v4& pos, const v4& norm, D3DCOLOR col, const v2& uv)
{
	if( m_format & D3DFVF_XYZ		)	Vertex().Set(pos);
	if( m_format & D3DFVF_NORMAL	)	Normal().Set(norm);
	if( m_format & D3DFVF_DIFFUSE	)	Colour() = col;
	if( m_format & D3DFVF_TEX1		)	Tex()	.Set(uv);
}

//****************************************
// Vertex Declarations

// XYZ
D3DVERTEXELEMENT9 g_VDecl_XYZ[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
    D3DDECL_END()
};

// XYZ_NORMAL
D3DVERTEXELEMENT9 g_VDecl_XYZ_NORMAL[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
    D3DDECL_END()
};

// XYZ_DIFFUSE
D3DVERTEXELEMENT9 g_VDecl_XYZ_DIFFUSE[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
    D3DDECL_END()
};

// XYZ_NORMAL_DIFFUSE
D3DVERTEXELEMENT9 g_VDecl_XYZ_NORMAL_DIFFUSE[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
	{0,	24,	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
    D3DDECL_END()
};

// XYZ_TEX1
D3DVERTEXELEMENT9 g_VDecl_XYZ_TEX1[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
    D3DDECL_END()
};

// XYZ_NORMAL_TEX1
D3DVERTEXELEMENT9 g_VDecl_XYZ_NORMAL_TEX1[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
	{0,	24,	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
    D3DDECL_END()
};

// XYZ_DIFFUSE_TEX1
D3DVERTEXELEMENT9 g_VDecl_XYZ_DIFFUSE_TEX1[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
	{0,	16,	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
    D3DDECL_END()
};

// XYZ_NORMAL_DIFFUSE_TEX1
D3DVERTEXELEMENT9 g_VDecl_XYZ_NORMAL_DIFFUSE_TEX1[] = 
{
    {0,	0,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_POSITION,	0},
	{0,	12,	D3DDECLTYPE_FLOAT3,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_NORMAL,	0},
	{0,	24,	D3DDECLTYPE_D3DCOLOR,	D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_COLOR,		0},
	{0,	28,	D3DDECLTYPE_FLOAT2,		D3DDECLMETHOD_DEFAULT,	D3DDECLUSAGE_TEXCOORD,	0},
    D3DDECL_END()
};

IDirect3DVertexDeclaration9* g_fvf_vertex_declarations[NUMBER_OF_FVFS];

bool VertexTypeManager::ReCreate()
{
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ,						&g_fvf_vertex_declarations[FVF_XYZ]						)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_NORMAL,				&g_fvf_vertex_declarations[FVF_XYZ_NORMAL]				)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_DIFFUSE,				&g_fvf_vertex_declarations[FVF_XYZ_DIFFUSE]				)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_NORMAL_DIFFUSE,		&g_fvf_vertex_declarations[FVF_XYZ_NORMAL_DIFFUSE]		)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_TEX1,				&g_fvf_vertex_declarations[FVF_XYZ_TEX1]				)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_NORMAL_TEX1,			&g_fvf_vertex_declarations[FVF_XYZ_NORMAL_TEX1]			)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_DIFFUSE_TEX1,		&g_fvf_vertex_declarations[FVF_XYZ_DIFFUSE_TEX1]		)) ) return false;
	if( Failed(m_renderer->GetD3DDevice()->CreateVertexDeclaration(g_VDecl_XYZ_NORMAL_DIFFUSE_TEX1, &g_fvf_vertex_declarations[FVF_XYZ_NORMAL_DIFFUSE_TEX1]	)) ) return false;
	return true;
}
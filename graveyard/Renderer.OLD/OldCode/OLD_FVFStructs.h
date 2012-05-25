//**********************************************************************************
//
// Vertex format structures
//
//**********************************************************************************
#ifndef FVFSTRUCTS_H
#define FVFSTRUCTS_H

#include <DXSDK/Include/d3d9.h>
#include "Maths/Maths.h"
#include "Geometry/PRGeometry.h"

// The FVF types
enum FVFType
{
	FVF_XYZ,
	FVF_XYZ_NORMAL,
	FVF_XYZ_DIFFUSE,
	FVF_XYZ_NORMAL_DIFFUSE,
	FVF_XYZ_TEX1,
	FVF_XYZ_NORMAL_TEX1,
	FVF_XYZ_DIFFUSE_TEX1,
	FVF_XYZ_NORMAL_DIFFUSE_TEX1,
	NUMBER_OF_FVFS,
	INVALID_FVF		= NUMBER_OF_FVFS
};

typedef uint16 FVF;
typedef uint32 VertexFormat;

// The FVF structures
struct XYZ						{ v3 m_vertex;                                           };
struct XYZ_NORMAL				{ v3 m_vertex; v3 m_normal;                              };
struct XYZ_DIFFUSE				{ v3 m_vertex;              D3DCOLOR m_colour;           };
struct XYZ_NORMAL_DIFFUSE		{ v3 m_vertex; v3 m_normal; D3DCOLOR m_colour;           };
struct XYZ_TEX1					{ v3 m_vertex;                                 v2 m_tex; };
struct XYZ_NORMAL_TEX1			{ v3 m_vertex; v3 m_normal;                    v2 m_tex; };
struct XYZ_DIFFUSE_TEX1			{ v3 m_vertex;              D3DCOLOR m_colour; v2 m_tex; };
struct XYZ_NORMAL_DIFFUSE_TEX1	{ v3 m_vertex; v3 m_normal; D3DCOLOR m_colour; v2 m_tex; };


//LPDIRECT3DVERTEXDECLARATION9 m_pVertexDeclaration;
//g_d3dDevice->CreateVertexDeclaration(dwDecl3, &m_pVertexDeclaration);
//
//m_pd3dDevice->SetVertexDeclaration(m_pVertexDeclaration);
//

//*****
// Returns the size of an vertex_fvf type
inline uint FVFSize(FVF fvf)
{
	switch( fvf )
	{
	case FVF_XYZ:						return sizeof(XYZ);
	case FVF_XYZ_NORMAL:				return sizeof(XYZ_NORMAL);
	case FVF_XYZ_DIFFUSE:				return sizeof(XYZ_DIFFUSE);
	case FVF_XYZ_NORMAL_DIFFUSE:		return sizeof(XYZ_NORMAL_DIFFUSE);
	case FVF_XYZ_TEX1:					return sizeof(XYZ_TEX1);
	case FVF_XYZ_NORMAL_TEX1:			return sizeof(XYZ_NORMAL_TEX1);
	case FVF_XYZ_DIFFUSE_TEX1:			return sizeof(XYZ_DIFFUSE_TEX1);
	case FVF_XYZ_NORMAL_DIFFUSE_TEX1:	return sizeof(XYZ_NORMAL_DIFFUSE_TEX1);
	default:							PR_ERROR_STR("Unknown FVF type"); return 0;
	}
}

//*****
// Return the vertex_format for an fvf type
inline VertexFormat FVFFormat(FVF fvf)
{
	switch( fvf )
	{
	case FVF_XYZ:						return (D3DFVF_XYZ);
	case FVF_XYZ_NORMAL:				return (D3DFVF_XYZ|D3DFVF_NORMAL);
	case FVF_XYZ_DIFFUSE:				return (D3DFVF_XYZ|D3DFVF_DIFFUSE);
	case FVF_XYZ_NORMAL_DIFFUSE:		return (D3DFVF_XYZ|D3DFVF_NORMAL|D3DFVF_DIFFUSE);
	case FVF_XYZ_TEX1:					return (D3DFVF_XYZ|D3DFVF_TEX1);
	case FVF_XYZ_NORMAL_TEX1:			return (D3DFVF_XYZ|D3DFVF_NORMAL|D3DFVF_TEX1);
	case FVF_XYZ_DIFFUSE_TEX1:			return (D3DFVF_XYZ|D3DFVF_DIFFUSE|D3DFVF_TEX1);
	case FVF_XYZ_NORMAL_DIFFUSE_TEX1:	return (D3DFVF_XYZ|D3DFVF_NORMAL|D3DFVF_DIFFUSE|D3DFVF_TEX1);	
	default:							PR_ERROR_STR("Unknown FVF type"); return 0;
	}
}

//*****
// Return the vertex declaration for an fvf type
extern IDirect3DVertexDeclaration9* g_fvf_vertex_declarations[NUMBER_OF_FVFS];
inline IDirect3DVertexDeclaration9* FVFVertexDeclaration(FVF fvf)
{
	PR_ASSERT_STR(fvf < NUMBER_OF_FVFS, "Unknown FVF type");
	return g_fvf_vertex_declarations[fvf];
}

//*****
// Return the fvf for a geometry type
inline FVF FVFFromGeometryType(PR::GeomType type)
{
	using namespace PR;
	switch( type )
	{
	case GeometryType::Vertex:																			return FVF_XYZ;
	case GeometryType::Vertex | GeometryType::Normal:													return FVF_XYZ_NORMAL;
	case GeometryType::Vertex                        | GeometryType::Colour:							return FVF_XYZ_DIFFUSE;
	case GeometryType::Vertex | GeometryType::Normal | GeometryType::Colour:							return FVF_XYZ_NORMAL_DIFFUSE;
	case GeometryType::Vertex                                               | GeometryType::Texture:	return FVF_XYZ_TEX1;
	case GeometryType::Vertex | GeometryType::Normal                        | GeometryType::Texture:	return FVF_XYZ_NORMAL_TEX1;
	case GeometryType::Vertex                        | GeometryType::Colour | GeometryType::Texture:	return FVF_XYZ_DIFFUSE_TEX1;
	case GeometryType::Vertex | GeometryType::Normal | GeometryType::Colour | GeometryType::Texture:	return FVF_XYZ_NORMAL_DIFFUSE_TEX1;
	default:	PR_ERROR_STR("Unknown combination of geometry types"); return 0;
	}
}

//*****
// An iterator to an FVF vertex. Use this class as if it was a pointer to the vertex of the FVF type given
struct FVFMemberOffsets;
class FVFVertexIter
{
public:
	FVFVertexIter();
	FVFVertexIter(void* vertex_buffer, FVF fvf);

	void				 Set(const PR::Vertex& vertex);
	void				 Set(const v4& pos = v4Origin, const v4& norm = v4Zero, D3DCOLOR col = 0, const v2& uv = v2Zero);
	v3&				 	 Vertex();
	v3&					 Normal();
	D3DCOLOR&			 Colour();
	v2&					 Tex();

private:
	struct bool_tester { int x; };
	typedef int bool_tester::* bool_type;

public:
						 operator bool_type() const		{ return m_iter != 0 ? &bool_tester::x : static_cast<bool_type>(0); }
	FVFVertexIter&		 operator ++()					{ m_iter += m_size; return *this; }
	FVFVertexIter		 operator[](uint ofs) const		{ return FVFVertexIter(m_iter + ofs * m_size, m_fvf); }

private:
	uint8*					m_iter;
	FVF						m_fvf;
	uint					m_size;
	VertexFormat			m_format;
	const FVFMemberOffsets*	m_ofs;
};

class Renderer;
class VertexTypeManager
{
public:
	void Initialise(Renderer* renderer)	{ m_renderer = renderer; ReCreate(); }
	bool ReCreate();

private:
	Renderer*	m_renderer;
};

#endif//FVFSTRUCTS_H

//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"

namespace pr::rdr
{
	// Only support 1 vertex format because extra formats multiply the number of shader
	// permutations. If in the future more data is needed in the vertex format, just add it
	// and update the shaders to handle the case when the data is missing/invalid.
	// To do this, prefer to do degenerate calculations than 'if' statements in the shaders.
	// Also, using full fat v4s to allow for encoding extra info into unused members.
	//
	// Although there is only one format, code the rest of the renderer assuming 'Vert'
	// is a template parameter. Specialised shaders may wish to create specific vertex
	// formats (e.g. texture transforming shader, say)

	// *The* vertex format
	struct Vert
	{
		// This allows code templated on vertex type to ask what geometry components are supported
		// In many cases, an model will have nuggets with a subset of these geom flags.
		static EGeom const GeomMask = EGeom::Vert | EGeom::Colr | EGeom::Norm | EGeom::Tex0;

		v4     m_vert;
		Colour m_diff;
		v4     m_norm;
		v2     m_tex0;
		v2     pad;

		// The vertex layout description
		static D3D11_INPUT_ELEMENT_DESC const (&Layout())[4]
		{
			static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
			{
				{"POSITION" , 0 , DXGI_FORMAT_R32G32B32A32_FLOAT , 0 , offsetof(Vert , m_vert) , D3D11_INPUT_PER_VERTEX_DATA , 0},
				{"COLOR"    , 0 , DXGI_FORMAT_R32G32B32A32_FLOAT , 0 , offsetof(Vert , m_diff) , D3D11_INPUT_PER_VERTEX_DATA , 0},
				{"NORMAL"   , 0 , DXGI_FORMAT_R32G32B32A32_FLOAT , 0 , offsetof(Vert , m_norm) , D3D11_INPUT_PER_VERTEX_DATA , 0},
				{"TEXCOORD" , 0 , DXGI_FORMAT_R32G32_FLOAT       , 0 , offsetof(Vert , m_tex0) , D3D11_INPUT_PER_VERTEX_DATA , 0},
			};
			return s_desc;
		}
	};

	inline v4 const& GetP(Vert const& vert)
	{
		return vert.m_vert;
	}
	inline Colour const& GetC(Vert const& vert)
	{
		return vert.m_diff;
	}
	inline v4 const& GetN(Vert const& vert)
	{
		return vert.m_norm;
	}
	inline v2 const& GetT(Vert const& vert)
	{
		return vert.m_tex0;
	}

	// Don't set values that aren't given, allows these functions to be composed
	inline void SetP(Vert& vert, v4 const& pos)
	{
		vert.m_vert = pos;
	}
	inline void SetC(Vert& vert, Colour const& col)
	{
		vert.m_diff = col;
	}
	inline void SetN(Vert& vert, v4 const& norm)
	{
		vert.m_norm = norm;
	}
	inline void SetT(Vert& vert, v2 const& uv)
	{
		vert.m_tex0 = uv;
	}
	inline void SetPC(Vert& vert, v4 const& pos, Colour const& col)
	{
		vert.m_vert = pos;
		vert.m_diff = col;
	}
	inline void SetPT(Vert& vert, v4 const& pos, v2 const& uv)
	{
		vert.m_vert = pos;
		vert.m_tex0 = uv;
	}
	inline void SetPCN(Vert& vert, v4 const& pos, Colour const& col, v4 const& norm)
	{
		vert.m_vert = pos;
		vert.m_diff = col;
		vert.m_norm = norm;
	}
	inline void SetPCNT(Vert& vert, v4 const& pos, Colour const& col, v4 const& norm, v2 const& uv)
	{
		vert.m_vert = pos;
		vert.m_diff = col;
		vert.m_norm = norm;
		vert.m_tex0 = uv;
	}

	inline void Encompass(BBox& bbox, Vert const& vert)
	{
		Encompass(bbox, vert.m_vert);
	}

}

//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MODELS_INPUT_LAYOUT_H
#define PR_RDR_MODELS_INPUT_LAYOUT_H

#include "pr/renderer11/forward.h"

namespace pr
{
	namespace rdr
	{
		// Position only vertex
		struct VertP
		{
			static EGeom::Enum_ const GeomMask = static_cast<EGeom::Enum_>(EGeom::Vert);

			v3 m_pos;

			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const(&Layout())[1]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT ,0 ,offsetof(VertP,m_pos) ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};

		// Don't set values that aren't given
		inline void SetPC  (VertP& vert, v4 const& pos, Colour32)                       { vert.m_pos = pos.xyz(); }
		inline void SetPCNT(VertP& vert, v4 const& pos, Colour32, v4 const&, v2 const&) { vert.m_pos = pos.xyz(); }

		// Position and colour
		struct VertPC
		{
			static EGeom::Enum_ const GeomMask = static_cast<EGeom::Enum_>(EGeom::Vert | EGeom::Colr);

			v3     m_pos;
			Colour m_col;

			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const(&Layout())[2]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,offsetof(VertPC,m_pos) ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"COLOR"    ,0 ,DXGI_FORMAT_R32G32B32A32_FLOAT ,0 ,offsetof(VertPC,m_col) ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};

		// Don't set values that aren't given
		inline void SetPC  (VertPC& vert, v4 const& pos, Colour32 col)                       { vert.m_pos = pos.xyz(); vert.m_col = col; }
		inline void SetPCNT(VertPC& vert, v4 const& pos, Colour32 col, v4 const&, v2 const&) { vert.m_pos = pos.xyz(); vert.m_col = col; }

		// Position, Diffuse Texture
		struct VertPT
		{
			static EGeom::Enum_ const GeomMask = static_cast<EGeom::Enum_>(EGeom::Vert | EGeom::Tex0);

			v3     m_pos;
			v2     m_uv;

			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const(&Layout())[2]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,offsetof(VertPT,m_pos) ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"TEXCOORD" ,0 ,DXGI_FORMAT_R32G32_FLOAT       ,0 ,offsetof(VertPT,m_uv)  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};

		// Don't set values that aren't given
		inline void SetPC  (VertPT& vert, v4 const& pos, Colour32)                          { vert.m_pos = pos.xyz(); }
		inline void SetPCNT(VertPT& vert, v4 const& pos, Colour32, v4 const&, v2 const& uv) { vert.m_pos = pos.xyz(); vert.m_uv = uv; }

		// Position, Colour, Normal, Diffuse Texture
		struct VertPCNT
		{
			static EGeom::Enum_ const GeomMask = static_cast<EGeom::Enum_>(EGeom::Vert | EGeom::Colr | EGeom::Norm | EGeom::Tex0);

			v3     m_pos;
			Colour m_col;
			v3     m_norm;
			v2     m_uv;

			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const(&Layout())[4]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,offsetof(VertPCNT,m_pos)  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"COLOR"    ,0 ,DXGI_FORMAT_R32G32B32A32_FLOAT ,0 ,offsetof(VertPCNT,m_col)  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"NORMAL"   ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,offsetof(VertPCNT,m_norm) ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"TEXCOORD" ,0 ,DXGI_FORMAT_R32G32_FLOAT       ,0 ,offsetof(VertPCNT,m_uv)   ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};

		// Don't set values that aren't given
		inline void SetPC  (VertPCNT& vert, v4 const& pos, Colour32 col)                               { vert.m_pos = pos.xyz(); vert.m_col = col; }
		inline void SetPCNT(VertPCNT& vert, v4 const& pos, Colour32 col, v4 const& norm, v2 const& uv) { vert.m_pos = pos.xyz(); vert.m_col = col; vert.m_norm = norm.xyz(); vert.m_uv = uv; }
	}
}

#endif

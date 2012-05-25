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
			enum { GeomMask = EGeom::Pos };
			
			pr::v3 m_pos;
			
			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const (&Layout())[1]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT ,0 ,0 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};
		
		// Position and colour
		struct VertPC
		{
			enum { GeomMask = EGeom::Pos | EGeom::Diff };
			
			pr::v3     m_pos;
			pr::Colour m_col;
			
			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const (&Layout())[2]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,0  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"COLOR"    ,0 ,DXGI_FORMAT_R32G32B32A32_FLOAT ,0 ,12 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};
		
		// Position, Diffuse Texture
		struct VertPT
		{
			enum { GeomMask = EGeom::Pos | EGeom::Tex0 };
			
			pr::v3     m_pos;
			pr::v2     m_tex;
			
			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const (&Layout())[2]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,0  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"TEXCOORD" ,0 ,DXGI_FORMAT_R32G32_FLOAT       ,0 ,12 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};

		// Position, Normal, Diffuse Texture, Colour
		struct VertPNTC
		{
			enum { GeomMask = EGeom::Pos | EGeom::Diff | EGeom::Norm | EGeom::Tex0 };
			
			pr::v3     m_pos;
			pr::Colour m_col;
			pr::v3     m_norm;
			pr::v2     m_tex;
			
			// The vertex layout description
			static D3D11_INPUT_ELEMENT_DESC const (&Layout())[4]
			{
				static D3D11_INPUT_ELEMENT_DESC const s_desc[] =
				{
					{"POSITION" ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,0  ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"COLOR"    ,0 ,DXGI_FORMAT_R32G32B32A32_FLOAT ,0 ,32 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"NORMAL"   ,0 ,DXGI_FORMAT_R32G32B32_FLOAT    ,0 ,12 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
					{"TEXCOORD" ,0 ,DXGI_FORMAT_R32G32_FLOAT       ,0 ,24 ,D3D11_INPUT_PER_VERTEX_DATA ,0},
				};
				return s_desc;
			}
		};
	}
}

#endif


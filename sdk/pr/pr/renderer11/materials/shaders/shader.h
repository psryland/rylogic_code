//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once
#ifndef PR_RDR_MATERIALS_SHADER_H
#define PR_RDR_MATERIALS_SHADER_H

#include "pr/renderer11/forward.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
		namespace shader
		{
			enum Type
			{
				Tx,
			};
			
			// Callback function for configuring the constants for a shader
			typedef std::function<void(D3DPtr<ID3D11DeviceContext>& dc, D3DPtr<ID3D11Buffer>& cbuf, DrawListElement const& dle, SceneView const& view)> MapConstants;
		}
		
		// Initialisation data for a shader
		struct ShaderDesc
		{
			void const* m_data; // The compiled shader data
			size_t      m_size; // The compiled shader data size
			
			ShaderDesc(void const* data, size_t size)
			:m_data(data)
			,m_size(size)
			{}
		};
		
		// Vertex shader flavour
		struct VShaderDesc :ShaderDesc
		{
			D3D11_INPUT_ELEMENT_DESC const* m_iplayout; // The input layout description
			size_t               m_iplayout_count;      // The number of elements in the input layout
			pr::rdr::EGeom::Type m_geom_mask;           // The minimum requirements of the vertex format
			pr::rdr::CBufferDesc m_cbuf_desc;           // A description of the constants buffer to create
			
			// Initialise the shader description.
			template <class Vert> VShaderDesc(Vert const&, void const* data, size_t size, size_t consts_size)
			:ShaderDesc(data, size)
			,m_iplayout(Vert::Layout())
			,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
			,m_geom_mask(Vert::GeomMask)
			,m_cbuf_desc(consts_size)
			{}
		};
		
		// Pixel shader flavour
		struct PShaderDesc :ShaderDesc
		{
			PShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
			{}
		};
		
		// A collection of shaders (kinda like an effect)
		struct Shader :pr::RefCount<Shader>
		{
			D3DPtr<ID3D11InputLayout>       m_iplayout;        // The input layout compatible with this shader
			D3DPtr<ID3D11Buffer>            m_cbuf_frame;      // The constants that change per frame
			D3DPtr<ID3D11Buffer>            m_cbuf_object;     // The constants that change per object
			D3DPtr<ID3D11VertexShader>      m_vs;              // The vertex shader (null if not used)
			D3DPtr<ID3D11PixelShader>       m_ps;              // The pixel shader (null if not used)
			D3DPtr<ID3D11GeometryShader>    m_gs;              // The geometry shader (null if not used)
			D3DPtr<ID3D11HullShader>        m_hs;              // The hull shader (null if not used)
			D3DPtr<ID3D11DomainShader>      m_ds;              // The domain shader (null if not used)
			D3DPtr<ID3D11BlendState>        m_blend_state;     // Blend states (null if not used)
			D3DPtr<ID3D11DepthStencilState> m_depth_state;     // Depth buffer states (null if not used)
			D3DPtr<ID3D11RasterizerState>   m_rast_state;      // Rasterizer states (null if not used)
			RdrId                           m_id;              // Id for this shader instance
			EGeom::Type                     m_geom_mask;       // The geometry type supported by this shader
			MaterialManager*                m_mat_mgr;         // The material manager that created this texture
			shader::MapConstants            m_map;             // A callback for setting up the constants for the shader
			string32                        m_name;            // Human readable id for the texture
			SortKeyId                       m_sort_id;
			
			Shader();
			
			// Setup this shader for rendering
			void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, SceneView const& view);
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Shader>* doomed);
		};
		
		// Create the built in shaders
		void CreateStockShaders(pr::rdr::MaterialManager& mat_mgr);
	}
}

#endif

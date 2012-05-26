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
				TxTint,
			};
		}
		
		// User provided callback function for binding a shader to the device context
		typedef std::function<void(D3DPtr<ID3D11DeviceContext>& dc, pr::rdr::DrawMethod const& meth, Nugget const& nugget, BaseInstance const& inst, SceneView const& view)> BindShaderFunc;
		
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
			
			// Initialise the shader description.
			template <class Vert> VShaderDesc(Vert const&, void const* data, size_t size)
			:ShaderDesc(data, size)
			,m_iplayout(Vert::Layout())
			,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
			,m_geom_mask(Vert::GeomMask)
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
			typedef pr::Array< D3DPtr<ID3D11Buffer>, 16, true > CBufCont;
			
			D3DPtr<ID3D11InputLayout>       m_iplayout;        // The input layout compatible with this shader
			CBufCont                        m_cbuf;            // Constant buffers used by the shader
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
			ShaderManager*                  m_mgr;             // The shader manager that created this shader
			string32                        m_name;            // Human readable id for the texture
			SortKeyId                       m_sort_id;
			
			Shader();
			
			// User provided callback for binding this shader to the device context
			BindShaderFunc Setup;
			
			// Set the shaders of the dc for the non-null shader pointers
			void Bind(D3DPtr<ID3D11DeviceContext>& dc) const;
			
			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Shader>* doomed);
		};
		
		// Create the built in shaders
		void CreateStockShaders(pr::rdr::ShaderManager& mgr);
	}
}

#endif

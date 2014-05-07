//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/util/wrappers.h"

namespace pr
{
	namespace rdr
	{
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
			size_t m_iplayout_count;    // The number of elements in the input layout
			pr::rdr::EGeom m_geom_mask; // The minimum requirements of the vertex format

			// Initialise the shader description.
			// 'Vert' should be a vertex type containing the minimum required fields for the VS
			template <class Vert> VShaderDesc(void const* data, size_t size, Vert const&, pr::rdr::EGeom geom_mask = Vert::GeomMask)
				:ShaderDesc(data, size)
				,m_iplayout(Vert::Layout())
				,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
				,m_geom_mask(geom_mask)
			{}
			template <class Vert, size_t Sz> VShaderDesc(byte const (&data)[Sz], Vert const&, pr::rdr::EGeom geom_mask = Vert::GeomMask)
				:ShaderDesc(data, Sz)
				,m_iplayout(Vert::Layout())
				,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
				,m_geom_mask(geom_mask)
			{}
		};

		// Pixel shader flavour
		struct PShaderDesc :ShaderDesc
		{
			PShaderDesc(void const* data, size_t size)
				:ShaderDesc(data, size)
			{}
			template <size_t Sz> PShaderDesc(byte const (&data)[Sz])
				:ShaderDesc(data, Sz)
			{}
		};

		// The base class of a custom shader.
		// All shaders must inherit this class.
		// This is kind of like an old school effect
		struct BaseShader :pr::RefCount<BaseShader>
		{
			D3DPtr<ID3D11InputLayout>    m_iplayout;      // The input layout compatible with this shader
			D3DPtr<ID3D11VertexShader>   m_vs;            // The vertex shader (null if not used)
			D3DPtr<ID3D11PixelShader>    m_ps;            // The pixel shader (null if not used)
			D3DPtr<ID3D11GeometryShader> m_gs;            // The geometry shader (null if not used)
			D3DPtr<ID3D11HullShader>     m_hs;            // The hull shader (null if not used)
			D3DPtr<ID3D11DomainShader>   m_ds;            // The domain shader (null if not used)
			RdrId                        m_id;            // Id for this shader instance
			EGeom                        m_geom_mask;     // The geometry type supported by this shader
			ShaderManager*               m_mgr;           // The shader manager that created this shader
			SortKeyId                    m_sort_id;       // A key used to order shaders next to each other in the drawlist
			BSBlock                      m_bsb;           // The blend state for the shader
			RSBlock                      m_rsb;           // The rasterizer state for the shader
			DSBlock                      m_dsb;           // The depth buffering state for the shader
			string32                     m_name;          // Human readable id for the texture

			explicit BaseShader(ShaderManager* mgr);
			virtual ~BaseShader() {}

			// Setup the shader ready to be used on 'dle'
			// Note, shaders are set/cleared by the state stack.
			// Only per-model constants, textures, and samplers need to be set here.
			virtual void Setup(D3DPtr<ID3D11DeviceContext>& dc, DrawListElement const& dle, RenderStep const& rstep);

			// Undo any changes made by this shader on the dc
			// Note, shaders are set/cleared by the state stack.
			// This method is only needed to clear texture/sampler slots
			virtual void Cleanup(D3DPtr<ID3D11DeviceContext>& dc);

			// Ref counting cleanup function
			static void RefCountZero(pr::RefCount<BaseShader>* doomed);

		protected:

			// Helper for binding 'tex' to a texture slot, along with its sampler
			void BindTextureAndSampler(D3DPtr<ID3D11DeviceContext>& dc, Texture2DPtr tex, UINT slot = 0);

			// Use the shader manager 'CreateShader'
			// factory method to create new shaders
			friend struct Allocator<BaseShader>;
			BaseShader();
		};
	}
}

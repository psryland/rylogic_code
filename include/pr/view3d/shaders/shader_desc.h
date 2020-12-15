//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once
#include "pr/view3d/forward.h"

namespace pr::rdr
{
	// Initialisation data for a shader
	struct ShaderDesc
	{
		void const* m_data; // The compiled shader data
		size_t      m_size; // The compiled shader data size

		ShaderDesc(void const* data, size_t size)
			:m_data(data)
			, m_size(size)
		{
		}
	};

	// Vertex shader flavour
	struct VShaderDesc :ShaderDesc
	{
		D3D11_INPUT_ELEMENT_DESC const* m_iplayout; // The input layout description
		size_t m_iplayout_count;                    // The number of elements in the input layout

		// Initialise the shader description.
		// 'Vert' should be a vertex type containing the minimum required fields for the VS
		template <class Vert> VShaderDesc(void const* data, size_t size, Vert const&)
			:ShaderDesc(data, size)
			, m_iplayout(Vert::Layout())
			, m_iplayout_count(sizeof(Vert::Layout()) / sizeof(D3D11_INPUT_ELEMENT_DESC))
		{
		}
		template <class Vert, size_t Sz> VShaderDesc(byte const (&data)[Sz], Vert const&)
			: ShaderDesc(data, Sz)
			, m_iplayout(Vert::Layout())
			, m_iplayout_count(sizeof(Vert::Layout()) / sizeof(D3D11_INPUT_ELEMENT_DESC))
		{
		}
	};

	// Pixel shader flavour
	struct PShaderDesc :ShaderDesc
	{
		PShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
		{
		}
		template <size_t Sz> PShaderDesc(byte const (&data)[Sz])
			: ShaderDesc(data, Sz)
		{
		}
	};

	// Geometry shader flavour
	struct GShaderDesc :ShaderDesc
	{
		GShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
		{
		}
		template <size_t Sz> GShaderDesc(byte const (&data)[Sz])
			: ShaderDesc(data, Sz)
		{
		}
	};

	// Compute shader flavour
	struct CShaderDesc :ShaderDesc
	{
		CShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
		{
		}
		template <size_t Sz> CShaderDesc(byte const (&data)[Sz])
			: ShaderDesc(data, Sz)
		{
		}
	};

	// Stream output stage description
	struct StreamOutDesc
	{
		pr::vector<D3D11_SO_DECLARATION_ENTRY> m_decl;
		pr::vector<UINT> m_strides;
		UINT m_raster_stream;

		StreamOutDesc(std::initializer_list<D3D11_SO_DECLARATION_ENTRY> decl, UINT raster_stream = D3D11_SO_NO_RASTERIZED_STREAM)
			:m_decl(std::begin(decl), std::end(decl))
			, m_strides(D3D11_SO_BUFFER_SLOT_COUNT)
			, m_raster_stream(raster_stream)
		{
			for (auto entry : m_decl)
				m_strides[entry.Stream] += entry.ComponentCount * sizeof(float);
			for (; !m_strides.empty() && m_strides.back() == 0;)
				m_strides.pop_back();
		}
		D3D11_SO_DECLARATION_ENTRY const* decl() const { return m_decl.data(); }
		UINT const* strides() const { return m_strides.data(); }
		UINT num_entries() const { return UINT(m_decl.size()); }
		UINT num_strides() const { return UINT(m_strides.size()); }
		UINT raster_stream() const { return m_raster_stream; }
		ID3D11ClassLinkage* class_linkage() const { return nullptr; }
	};
}
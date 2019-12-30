//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
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
		size_t m_iplayout_count;                    // The number of elements in the input layout

		// Initialise the shader description.
		// 'Vert' should be a vertex type containing the minimum required fields for the VS
		template <class Vert> VShaderDesc(void const* data, size_t size, Vert const&)
			:ShaderDesc(data, size)
			,m_iplayout(Vert::Layout())
			,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
		{}
		template <class Vert, size_t Sz> VShaderDesc(byte const (&data)[Sz], Vert const&)
			:ShaderDesc(data, Sz)
			,m_iplayout(Vert::Layout())
			,m_iplayout_count(sizeof(Vert::Layout())/sizeof(D3D11_INPUT_ELEMENT_DESC))
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

	// Geometry shader flavour
	struct GShaderDesc :ShaderDesc
	{
		GShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
		{}
		template <size_t Sz> GShaderDesc(byte const (&data)[Sz])
			:ShaderDesc(data, Sz)
		{}
	};

	// Compute shader flavour
	struct CShaderDesc :ShaderDesc
	{
		CShaderDesc(void const* data, size_t size)
			:ShaderDesc(data, size)
		{}
		template <size_t Sz> CShaderDesc(byte const (&data)[Sz])
			:ShaderDesc(data, Sz)
		{}
	};

	// Stream output stage description
	struct StreamOutDesc
	{
		pr::vector<D3D11_SO_DECLARATION_ENTRY> m_decl;
		pr::vector<UINT> m_strides;
		UINT m_raster_stream;

		StreamOutDesc(std::initializer_list<D3D11_SO_DECLARATION_ENTRY> decl, UINT raster_stream = D3D11_SO_NO_RASTERIZED_STREAM)
			:m_decl(std::begin(decl), std::end(decl))
			,m_strides(D3D11_SO_BUFFER_SLOT_COUNT)
			,m_raster_stream(raster_stream)
		{
			for (auto entry : m_decl)
				m_strides[entry.Stream] += entry.ComponentCount * sizeof(float);
			for (;!m_strides.empty() && m_strides.back() == 0;)
				m_strides.pop_back();
		}
		D3D11_SO_DECLARATION_ENTRY const* decl() const { return m_decl.data(); }
		UINT const* strides() const                    { return m_strides.data(); }
		UINT num_entries() const                       { return UINT(m_decl.size()); }
		UINT num_strides() const                       { return UINT(m_strides.size()); }
		UINT raster_stream() const                     { return m_raster_stream; }
		ID3D11ClassLinkage* class_linkage() const      { return nullptr; }
	};

	// The base class for a shader.
	struct Shader :pr::RefCount<Shader>
	{
		// Notes:
		// - This object wraps a single VS, or PS, or GS, etc
		// - Shader objects are intended to be lightweight instances of D3D shaders.
		// - Shader objects group a D3D shader with it's per-nugget constants.
		// - Shader objects can be created for each nugget that needs them.

		D3DPtr<ID3D11DeviceChild> m_dx_shdr;   // Pointer to the dx shader
		EShaderType               m_shdr_type; // The type of shader this is
		ShaderManager*            m_mgr;       // The shader manager that created this shader
		Renderer*                 m_rdr;       // The renderer
		RdrId                     m_id;        // Id for this shader
		SortKeyId                 m_sort_id;   // A key used to order shaders next to each other in the drawlist
		BSBlock                   m_bsb;       // The blend state for the shader
		RSBlock                   m_rsb;       // The rasterizer state for the shader
		DSBlock                   m_dsb;       // The depth buffering state for the shader
		string32                  m_name;      // Human readable id for the shader
		RdrId                     m_orig_id;   // Id of the shader this is a clone of (used for debugging)

		// Set up the shader ready to be used on 'dle'
		// This needs to take the state stack and set things via that, to prevent unnecessary state changes
		virtual void Setup(ID3D11DeviceContext* dc, DeviceState& state);

		// Undo any changes made by this shader
		virtual void Cleanup(ID3D11DeviceContext*) {}

		// Return the input layout associated with this shader. Note: returns null for all shaders except vertex shaders
		D3DPtr<ID3D11InputLayout> IpLayout() const;

	protected:

		// Use the shader manager 'CreateShader' factory method to create new shaders
		template <typename DxShaderType>
		Shader(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType> const& dx_shdr)
			:pr::RefCount<Shader>()
			,m_dx_shdr(dx_shdr.get(), true)
			,m_shdr_type(ShaderTypeId<DxShaderType>::value)
			,m_mgr(mgr)
			,m_rdr(&mgr->m_rdr)
			,m_id(id == AutoId ? MakeId(this) : id)
			,m_sort_id(sort_id)
			,m_bsb()
			,m_rsb()
			,m_dsb()
			,m_name(name ? name : "")
			,m_orig_id(m_id)
		{}
		virtual ~Shader()
		{}

		// Ref counting clean up
		friend struct pr::RefCount<Shader>;
		static void RefCountZero(pr::RefCount<Shader>* doomed)
		{
			static_cast<Shader*>(doomed)->OnRefCountZero();
		}
		virtual void OnRefCountZero() = 0;

		// Forwarding methods are needed because only Shader is a friend of the ShaderManager
		template <typename ShaderType> void Delete(ShaderType* shdr)
		{
			return m_mgr->DeleteShader(shdr);
		}
	};

	// ********************************************************************

	// Base class for each dx shader type
	template <typename DxShaderType, typename Derived>
	struct ShaderT :Shader
	{
		// Return the D3D shader interface down-cast to 'DxShaderType'
		D3DPtr<DxShaderType> dx_shader() const
		{
			return static_cast<D3DPtr<DxShaderType>>(m_dx_shdr);
		}

	protected:

		ShaderT(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType> const& dx_shdr)
			:Shader(mgr, id, sort_id, name, dx_shdr)
		{}

		// Ref count clean up
		void OnRefCountZero() override
		{
			Shader::Delete<Derived>(static_cast<Derived*>(this));
		}
	};
}

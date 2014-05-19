//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
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

		// The base class for a shader. All shaders must inherit this class.
		// This object wraps a single VS, or PS, or GS, etc
		// A Shader is an instance of a dx shader, it contains shader-specific per-nugget data.
		struct ShaderBase :pr::RefCount<ShaderBase>
		{
			D3DPtr<ID3D11DeviceChild> m_shdr;      // Pointer to the dx shader
			EShaderType               m_shdr_type; // The type of shader this is
			ShaderManager*            m_mgr;       // The shader manager that created this shader
			RdrId                     m_id;        // Id for this shader
			EGeom                     m_geom;      // Required geometry format for this shader
			SortKeyId                 m_sort_id;   // A key used to order shaders next to each other in the drawlist
			BSBlock                   m_bsb;       // The blend state for the shader
			RSBlock                   m_rsb;       // The rasterizer state for the shader
			DSBlock                   m_dsb;       // The depth buffering state for the shader
			string32                  m_name;      // Human readable id for the shader

			virtual ~ShaderBase() {}

			// Setup the shader ready to be used on 'dle'
			// This needs to take the state stack and set things via that, to prevent unnecessary state changes
			virtual void Setup(D3DPtr<ID3D11DeviceContext>& dc, DeviceState& state);

			// Undo any changes made by this shader
			virtual void Cleanup(D3DPtr<ID3D11DeviceContext>&) {}

			// Create a clone of this shader
			ShaderPtr Clone(RdrId new_id, char const* new_name)
			{
				return MakeClone(new_id, new_name);
			}
			template <typename ShaderType> pr::RefPtr<ShaderType> Clone(RdrId new_id, char const* new_name)
			{
				return MakeClone(new_id, new_name);
			}

			// Return the input layout associated with this shader.
			// Note, returns null for all shaders except vertex shaders
			D3DPtr<ID3D11InputLayout> IpLayout() const;

			// Ref counting cleanup
			static void RefCountZero(pr::RefCount<ShaderBase>* doomed);
			protected: virtual void Delete() = 0;

		protected:

			// Use the shader manager 'CreateShader' factory method to create new shaders
			friend struct Allocator<ShaderBase>;
			template <typename DxShaderType> ShaderBase(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<DxShaderType> dx_shdr)
				:pr::RefCount<ShaderBase>()
				,m_shdr(dx_shdr)
				,m_shdr_type(ShaderTypeId<DxShaderType>::value)
				,m_mgr(mgr)
				,m_id(id == AutoId ? MakeId(this) : id)
				,m_geom()
				,m_sort_id()
				,m_bsb()
				,m_rsb()
				,m_dsb()
				,m_name(name)
			{}

			// Create a new shader that is a copy of this shader
			virtual ShaderPtr MakeClone(RdrId new_id, char const* new_name) = 0;

			// Forwarding methods are needed because only ShaderBase is a friend of the ShaderManager
			template <typename ShaderType> void Delete(ShaderType* shdr)
			{
				return m_mgr->DeleteShader(shdr);
			}
		};

		// ********************************************************************

		// Base class for each dx shader type
		template <typename DxShaderType, typename Derived>
		struct Shader :ShaderBase
		{
		protected:

			Shader(ShaderManager* mgr, RdrId id, char const* name, D3DPtr<DxShaderType> dx_shdr)
				:ShaderBase(mgr, id, name, dx_shdr)
			{}

			// Default implementation. Derived can override if needed
			ShaderPtr MakeClone(RdrId new_id, char const* new_name) override
			{
				auto shdr = m_mgr->CreateShader<Derived>(new_id, m_shdr, new_name);

				shdr->m_geom     = m_geom;
				shdr->m_bsb      = m_bsb;
				shdr->m_rsb      = m_rsb;
				shdr->m_dsb      = m_dsb;
				return shdr;
			}

			// Ref count cleanup
			void Delete() override
			{
				ShaderBase::Delete<Derived>(static_cast<Derived*>(this));
			}
		};
	}
}

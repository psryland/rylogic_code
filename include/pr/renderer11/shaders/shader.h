//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/state_block.h"
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

		// The base class for a shader.
		struct ShaderBase :pr::RefCount<ShaderBase>
		{
			// Notes:
			// - This object wraps a single VS, or PS, or GS, etc
			// - ShaderBase objects are intended to be lightweight instances of D3D shaders.
			// - ShaderBase objects group a D3D shader with it's per-nugget constants.
			// - ShaderBase objects can be created for each nugget that needs them.

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

			friend struct Allocator<ShaderBase>;

			// Use the shader manager 'CreateShader' factory method to create new shaders
			template <typename DxShaderType>
			ShaderBase(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType> const& dx_shdr)
				:pr::RefCount<ShaderBase>()
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
			virtual ~ShaderBase()
			{}

			// Ref counting clean up
			friend struct pr::RefCount<ShaderBase>;
			static void RefCountZero(pr::RefCount<ShaderBase>* doomed)
			{
				static_cast<ShaderBase*>(doomed)->OnRefCountZero();
			}
			virtual void OnRefCountZero() = 0;

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
			// Return the D3D shader interface down-cast to 'DxShaderType'
			D3DPtr<DxShaderType> dx_shader() const
			{
				return static_cast<D3DPtr<DxShaderType>>(m_dx_shdr);
			}

		protected:

			Shader(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType> const& dx_shdr)
				:ShaderBase(mgr, id, sort_id, name, dx_shdr)
			{}

			// Ref count clean up
			void OnRefCountZero() override
			{
				ShaderBase::Delete<Derived>(static_cast<Derived*>(this));
			}
		};
	}
}

//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	// The base class for a shader.
	struct Shader :pr::RefCount<Shader>
	{
		// Notes:
		// - This object wraps a single VS, or PS, or GS, etc
		// - Shader objects are intended to be lightweight instances of D3D shaders.
		// - Shader objects group a D3D shader with it's per-nugget constants.
		// - Shader objects can be created for each nugget that needs them.

		D3DPtr<ID3D11DeviceChild> m_dx_shdr;   // Pointer to the DX shader
		EShaderType               m_shdr_type; // The type of shader this is
		ShaderManager*            m_mgr;       // The shader manager that created this shader
		RdrId                     m_id;        // Id for this shader
		SortKeyId                 m_sort_id;   // A key used to order shaders next to each other in the draw-list
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
		Shader(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType>& dx_shdr)
			:pr::RefCount<Shader>()
			,m_dx_shdr(dx_shdr.get(), true)
			,m_shdr_type(ShaderTypeId<DxShaderType>::value)
			,m_mgr(mgr)
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

		// The renderer
		Renderer& rdr() { return m_mgr->m_rdr; }

		// Ref counting clean up.
		// This is needed because 'Shader' doesn't know the actual type of 'doomed'.
		// Calling the virtual function allows the derived shader to call Delete with a known type.
		static void RefCountZero(RefCount<Shader>* doomed)
		{
			static_cast<Shader*>(doomed)->OnRefCountZero();
		}
		virtual void OnRefCountZero() = 0;
		friend struct RefCount<Shader>;

		// Forwarding methods are needed because only Shader is a friend of the ShaderManager
		template <typename ShaderType> void Delete(ShaderType* shdr)
		{
			return m_mgr->DeleteShader(shdr);
		}
	};

	// CRTP base class for a shader.
	template <typename DxShaderType, typename Derived>
	struct ShaderT :Shader
	{
		// Return the D3D shader interface down-cast to 'DxShaderType'
		D3DPtr<DxShaderType> dx_shader()
		{
			return static_cast<D3DPtr<DxShaderType>>(m_dx_shdr);
		}

	protected:

		ShaderT(ShaderManager* mgr, RdrId id, SortKeyId sort_id, char const* name, D3DPtr<DxShaderType>& dx_shdr)
			:Shader(mgr, id, sort_id, name, dx_shdr)
		{}

		// Ref count clean up
		void OnRefCountZero() override
		{
			Shader::Delete<Derived>(static_cast<Derived*>(this));
		}
	};
}

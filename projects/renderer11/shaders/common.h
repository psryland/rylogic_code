//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/util/util.h"

#ifdef NDEBUG
#define PR_RDR_SHADER_COMPILED_DIR(file) PR_STRINGISE(renderer11/shaders/hlsl/compiled/release/##file)
#else
#define PR_RDR_SHADER_COMPILED_DIR(file) PR_STRINGISE(renderer11/shaders/hlsl/compiled/debug/##file)
#endif

namespace pr
{
	namespace rdr
	{
		#if PR_RDR_RUNTIME_SHADERS
		void RegisterRuntimeShader(RdrId id, char const* cso_filepath);
		#endif

		#include "renderer11/shaders/hlsl/cbuf.hlsli"

		// The constant buffer definitions
		namespace fwd
		{
			#include "renderer11/shaders/hlsl/forward/forward_cbuf.hlsli"
		}
		namespace ds
		{
			#include "renderer11/shaders/hlsl/deferred/gbuffer_cbuf.hlsli"
		}
		namespace screenspace
		{
			#include "renderer11/shaders/hlsl/screenspace/thick_line_cbuf.hlsli"
		}
		namespace smap
		{
			#include "renderer11/shaders/hlsl/shadow/shadow_map_cbuf.hlsli"
		}

		// Set the geometry type
		template <typename TCBuf> void SetGeomType(NuggetProps const& ddata, TCBuf& cb)
		{
			// Convert a geom into an iv4 for flags passed to a shader
			cb.m_geom = pr::iv4::make(
				pr::AllSet(ddata.m_geom, EGeom::Norm),
				pr::AllSet(ddata.m_geom, EGeom::Tex0),
				0,
				0);
		}

		// Set the transform properties of a constants buffer
		template <typename TCBuf> void SetTxfm(BaseInstance const& inst, SceneView const& view, TCBuf& cb)
		{
			pr::m4x4 o2w = GetO2W(inst);
			pr::m4x4 w2c = pr::InvertFast(view.m_c2w);
			pr::m4x4 c2s; if (!FindC2S(inst, c2s)) c2s = view.m_c2s;
			cb.m_o2s = c2s * w2c * o2w;
			cb.m_o2w = o2w;
			cb.m_n2w = pr::Orthonorm(cb.m_o2w);
		}

		// Set the tint properties of a constants buffer
		template <typename TCBuf> void SetTint(BaseInstance const& inst, TCBuf& cb)
		{
			pr::Colour32 const* col = inst.find<pr::Colour32>(EInstComp::TintColour32);
			pr::Colour c = col ? *col : pr::ColourWhite;
			cb.m_tint = c;
		}

		// Set the texture properties of a constants buffer
		template <typename TCBuf> void SetTexDiffuse(NuggetProps const& ddata, TCBuf& cb)
		{
			cb.m_tex2surf0 = ddata.m_tex_diffuse != nullptr
				? ddata.m_tex_diffuse->m_t2s
				: pr::m4x4Identity;
		}

		// Helper for setting scene view constants
		template <typename TCBuf> void SetViewConstants(SceneView const& view, TCBuf& cb)
		{
			cb.m_c2w = view.m_c2w;
			cb.m_c2s = view.m_c2s;
			cb.m_w2c = pr::InvertFast(cb.m_c2w);
			cb.m_w2s = cb.m_c2s * cb.m_w2c;
		}

		// Helper for setting lighting constants
		template <typename TCBuf> void SetLightingConstants(Light const& light, TCBuf& cb)
		{
			cb.m_light_info         = pr::v4::make(static_cast<float>(light.m_type),0.0f,0.0f,0.0f);
			cb.m_ws_light_direction = light.m_direction;
			cb.m_ws_light_position  = light.m_position;
			cb.m_light_ambient      = static_cast<pr::Colour>(light.m_ambient);
			cb.m_light_colour       = static_cast<pr::Colour>(light.m_diffuse);
			cb.m_light_specular     = pr::Colour::make(light.m_specular, light.m_specular_power);
			cb.m_spot               = pr::v4::make(light.m_inner_cos_angle, light.m_outer_cos_angle, light.m_range, light.m_falloff);
		}

		// Lock and write 'cb' into 'cbuf'. The set 'cbuf' as the constants for the shaders
		template <typename TCBuf> void WriteConstants(D3DPtr<ID3D11DeviceContext>& dc, D3DPtr<ID3D11Buffer>& cbuf, TCBuf const& cb, EShaderType shdr_types)
		{
			{// Copy the buffer to the dx buffer
				LockT<TCBuf> lock(dc, cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}

			// Bind the constants to the shaders
			if (shdr_types & EShaderType::VS) dc->VSSetConstantBuffers(TCBuf::slot, 1, &cbuf.m_ptr);
			if (shdr_types & EShaderType::PS) dc->PSSetConstantBuffers(TCBuf::slot, 1, &cbuf.m_ptr);
			if (shdr_types & EShaderType::GS) dc->GSSetConstantBuffers(TCBuf::slot, 1, &cbuf.m_ptr);
			if (shdr_types & EShaderType::HS) dc->HSSetConstantBuffers(TCBuf::slot, 1, &cbuf.m_ptr);
			if (shdr_types & EShaderType::DS) dc->DSSetConstantBuffers(TCBuf::slot, 1, &cbuf.m_ptr);
		}

		// Helper for binding 'tex' to a texture slot, along with its sampler
		inline void BindTextureAndSampler(D3DPtr<ID3D11DeviceContext>& dc, UINT slot, Texture2DPtr tex, D3DPtr<ID3D11SamplerState> default_samp_state)
		{
			if (tex != nullptr)
			{
				// Set the shader resource view of the texture and the texture sampler
				dc->PSSetShaderResources(slot, 1, &tex->m_srv.m_ptr);
				dc->PSSetSamplers(slot, 1, &tex->m_samp.m_ptr);
			}
			else
			{
				ID3D11ShaderResourceView* null_srv[1] = {};
				dc->PSSetShaderResources(slot, 1, null_srv);

				ID3D11SamplerState* null_samp[1] = {default_samp_state.m_ptr};
				dc->PSSetSamplers(slot, 1, null_samp);
			}
		}
	}
}

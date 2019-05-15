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
		// How To Make A New Shader:
		// - Add an HLSL file:  e.g. '/renderer11/shaders/hlsl/<whatever>/your_file.hlsl'
		//   The HLSL file should contain the VS,GS,PS,etc shader definition (see existing examples)
		//   Change the Item Type to 'Custom Build Tool'. The default python script should already
		//   be set from the property sheets.
		// - Add a separate HLSLI file: e.g. 'your_file_cbuf.hlsli' (copy from an existing one)
		//   Set the Item Type to 'Does not participate in the build'
		// - Add a 'shdr_your_file.cpp' file (see existing).
		// - Shaders that get referenced externally to the renderer (i.e. most from now on), need
		//   a public header file as well 'shdr_your_file.h'. This will contain the ShaderT<> derived
		//   types, with the implementation in 'shdr_your_file.cpp' (e.g. shdr_screen_space).
		//   Shaders only used by the renderer don't need a header file (e.g. shdr_fwd.cpp)
		// - The 'Setup' function in your ShaderT<> derived object should follow the 'SetXYZConstants'
		//   pattern. You should be able to #include the 'your_file_cbuf.hlsli' file in the 'shdr_your_file.cpp'
		//   where the 'Setup' method is implemented.

		#if PR_RDR_RUNTIME_SHADERS
		void RegisterRuntimeShader(RdrId id, char const* cso_filepath);
		#endif

		namespace hlsl
		{
			#include "renderer11/shaders/hlsl/cbuf.hlsli"
			#include "renderer11/shaders/hlsl/types.hlsli"

			// The constant buffer definitions
			namespace fwd
			{
				#include "renderer11/shaders/hlsl/forward/forward_cbuf.hlsli"
			}
			namespace ds
			{
				#include "renderer11/shaders/hlsl/deferred/gbuffer_cbuf.hlsli"
			}
			namespace ss
			{
				#include "renderer11/shaders/hlsl/screenspace/screenspace_cbuf.hlsli"
			}
			namespace smap
			{
				#include "renderer11/shaders/hlsl/shadow/shadow_map_cbuf.hlsli"
			}
		}

		// Set the CBuffer model constants flags
		template <typename TCBuf> void SetModelFlags(NuggetData const& ddata, int inst_id, TCBuf& cb)
		{
			// Convert a geom into an iv4 for flags passed to a shader
			cb.m_flags = iv4(
				pr::AllSet(ddata.m_geom, EGeom::Norm),
				pr::AllSet(ddata.m_geom, EGeom::Tex0) && ddata.m_tex_diffuse != nullptr,
				ddata.m_sort_key.Group() > ESortGroup::PreAlpha ? 1 : 0,
				inst_id);
		}

		// Set the transform properties of a constants buffer
		template <typename TCBuf> void SetTxfm(BaseInstance const& inst, SceneView const& view, TCBuf& cb)
		{
			pr::m4x4 o2w = GetO2W(inst);
			pr::m4x4 w2c = pr::InvertFast(view.CameraToWorld());
			pr::m4x4 c2s = FindC2S(inst, c2s) ? c2s : view.CameraToScreen();

			cb.m_o2s = c2s * w2c * o2w;
			cb.m_o2w = o2w;

			// Orthonormalise the rotation part of the normal to world transform (allowing for scale matrices)
			cb.m_n2w = cb.m_o2w;
			cb.m_n2w.x = Normalise3(cb.m_n2w.x, v4Zero);
			cb.m_n2w.y = Normalise3(Cross3(cb.m_n2w.z, cb.m_n2w.x), v4Zero);
			cb.m_n2w.z = Cross3(cb.m_n2w.x, cb.m_n2w.y);
		}

		// Set the tint properties of a constants buffer
		template <typename TCBuf> void SetTint(BaseInstance const& inst, TCBuf& cb)
		{
			pr::Colour32 const* col = inst.find<pr::Colour32>(EInstComp::TintColour32);
			pr::Colour c = col ? *col : pr::ColourWhite;
			cb.m_tint = c.rgba;
		}

		// Set the texture properties of a constants buffer
		template <typename TCBuf> void SetTexDiffuse(NuggetData const& ddata, TCBuf& cb)
		{
			cb.m_tex2surf0 = ddata.m_tex_diffuse != nullptr
				? ddata.m_tex_diffuse->m_t2s
				: pr::m4x4Identity;
		}

		// Helper for setting scene view constants
		inline void SetViewConstants(SceneView const& view, hlsl::Camera& cb)
		{
			cb.m_c2w = view.CameraToWorld();
			cb.m_c2s = view.CameraToScreen();
			cb.m_w2c = pr::InvertFast(cb.m_c2w);
			cb.m_w2s = cb.m_c2s * cb.m_w2c;
		}

		// Helper for setting lighting constants
		inline void SetLightingConstants(Light const& light, hlsl::Light& cb)
		{
			cb.m_info         = iv4(int(light.m_type),0,0,0);
			cb.m_ws_direction = light.m_direction;
			cb.m_ws_position  = light.m_position;
			cb.m_ambient      = Colour(light.m_ambient).rgba;
			cb.m_colour       = Colour(light.m_diffuse).rgba;
			cb.m_specular     = Colour(light.m_specular, light.m_specular_power).rgba;
			cb.m_spot         = v4(light.m_inner_cos_angle, light.m_outer_cos_angle, light.m_range, light.m_falloff);
		}

		// Helper for setting shadow map constants
		inline void SetShadowMapConstants(SceneView const& view, int smap_count, hlsl::Shadow& cb)
		{
			auto shadow_frustum = view.ShadowFrustum();
			auto max_range = view.m_shadow_max_caster_dist;

			cb.m_info        = iv4(smap_count, 0, 0, 0);
			cb.m_frust_dim   = shadow_frustum.Dim();
			cb.m_frust_dim.w = max_range;
			cb.m_frust       = shadow_frustum.m_Tnorms;
		}

		// Lock and write 'cb' into 'cbuf'. The set 'cbuf' as the constants for the shaders
		template <typename TCBuf> void WriteConstants(ID3D11DeviceContext* dc, ID3D11Buffer* cbuf, TCBuf const& cb, EShaderType shdr_types)
		{
			// Copy the buffer to the dx buffer
			if (cbuf != nullptr)
			{
				LockT<TCBuf> lock(dc, cbuf, 0, D3D11_MAP_WRITE_DISCARD, 0);
				*lock.ptr() = cb;
			}

			// Bind the constants to the shaders
			ID3D11Buffer* buffers[] = {cbuf};
			if (AllSet(shdr_types, EShaderType::VS)) dc->VSSetConstantBuffers(TCBuf::slot, 1, buffers);
			if (AllSet(shdr_types, EShaderType::PS)) dc->PSSetConstantBuffers(TCBuf::slot, 1, buffers);
			if (AllSet(shdr_types, EShaderType::GS)) dc->GSSetConstantBuffers(TCBuf::slot, 1, buffers);
			if (AllSet(shdr_types, EShaderType::CS)) dc->CSSetConstantBuffers(TCBuf::slot, 1, buffers);
			if (AllSet(shdr_types, EShaderType::HS)) dc->HSSetConstantBuffers(TCBuf::slot, 1, buffers);
			if (AllSet(shdr_types, EShaderType::DS)) dc->DSSetConstantBuffers(TCBuf::slot, 1, buffers);
		}
	}
}

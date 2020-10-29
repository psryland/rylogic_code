//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/render/scene_view.h"
#include "pr/view3d/steps/shadow_map.h"
#include "pr/view3d/lights/light.h"
#include "pr/view3d/instances/instance.h"
#include "pr/view3d/util/util.h"

#ifdef NDEBUG
#define PR_RDR_SHADER_COMPILED_DIR(file) PR_STRINGISE(view3d/shaders/hlsl/compiled/release/##file)
#else
#define PR_RDR_SHADER_COMPILED_DIR(file) PR_STRINGISE(view3d/shaders/hlsl/compiled/debug/##file)
#endif

namespace pr::rdr
{
	// How To Make A New Shader:
	// - Add an HLSL file:  e.g. '/view3d/shaders/hlsl/<whatever>/your_file.hlsl'
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
	// - If your shader is a stock resource,
	//      - add it to the enum in "stock_resources.h", 
	//      - forward declare the shader struct in "shader_forward.h"

	#if PR_RDR_RUNTIME_SHADERS
	void RegisterRuntimeShader(RdrId id, char const* cso_filepath);
	#endif

	namespace hlsl
	{
		#include "view3d/shaders/hlsl/cbuf.hlsli"
		#include "view3d/shaders/hlsl/types.hlsli"

		// The constant buffer definitions
		namespace fwd
		{
			#include "view3d/shaders/hlsl/forward/forward_cbuf.hlsli"
			static_assert((sizeof(CBufFrame) % 16) == 0);
			static_assert((sizeof(CBufNugget) % 16) == 0);
			static_assert((sizeof(CBufFade) % 16) == 0);
		}
		namespace ds
		{
			#include "view3d/shaders/hlsl/deferred/gbuffer_cbuf.hlsli"
			static_assert((sizeof(CBufCamera) % 16) == 0);
			static_assert((sizeof(CBufLighting) % 16) == 0);
			static_assert((sizeof(CBufNugget) % 16) == 0);
		}
		namespace ss
		{
			#include "view3d/shaders/hlsl/screenspace/screen_space_cbuf.hlsli"
			static_assert((sizeof(CBufFrame) % 16) == 0);
		}
		namespace smap
		{
			#include "view3d/shaders/hlsl/shadow/shadow_map_cbuf.hlsli"
			static_assert((sizeof(CBufFrame) % 16) == 0);
			static_assert((sizeof(CBufNugget) % 16) == 0);
		}
		namespace diag
		{
			#include "view3d/shaders/hlsl/utility/diagnostic_cbuf.hlsli"
			static_assert((sizeof(CBufFrame) % 16) == 0);
		}
	}

	// Set the CBuffer model constants flags
	template <typename TCBuf> void SetModelFlags(BaseInstance const& inst, NuggetData const& nug, Scene const& scene, TCBuf& cb)
	{
		auto model_flags = 0;
		{
			// Has normals
			if (pr::AllSet(nug.m_geom, EGeom::Norm))
				model_flags |= hlsl::ModelFlags_HasNormals;
		}

		auto texture_flags = 0;
		{
			// Has diffuse texture
			if (pr::AllSet(nug.m_geom, EGeom::Tex0) && nug.m_tex_diffuse != nullptr)
			{
				texture_flags |= hlsl::TextureFlags_HasDiffuse;

				// Texture by projection from the environment map
				if (nug.m_tex_diffuse->m_src_id == RdrId(EStockTexture::EnvMapProjection))
					texture_flags |= hlsl::TextureFlags_ProjectFromEnvMap;
			}

			// Is reflective
			auto reflec = inst.find<float>(EInstComp::EnvMapReflectivity);
			if (reflec != nullptr &&                       // The instance has a reflectivity value
				scene.m_global_envmap != nullptr &&        // There is an env map
				pr::AllSet(nug.m_geom, EGeom::Norm) &&     // The model contains normals
				*reflec * nug.m_relative_reflectivity != 0)// and the reflectivity isn't zero
				texture_flags |= hlsl::TextureFlags_IsReflective;
		}

		auto alpha_flags = 0;
		{
			// Has alpha pixels
			if (nug.m_sort_key.Group() > ESortGroup::PreAlpha)
				alpha_flags |= hlsl::AlphaFlags_HasAlpha;
		}

		auto inst_id = 0;
		{
			// Unique id for this instance
			inst_id = UniqueId(inst);
		}

		cb.m_flags = iv4{ model_flags, texture_flags, alpha_flags, inst_id };
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
		cb.m_n2w.x = Normalise(cb.m_n2w.x, v4Zero);
		cb.m_n2w.y = Normalise(Cross3(cb.m_n2w.z, cb.m_n2w.x), v4Zero);
		cb.m_n2w.z = Cross3(cb.m_n2w.x, cb.m_n2w.y);
	}

	// Set the tint properties of a constants buffer
	template <typename TCBuf> void SetTint(BaseInstance const& inst, NuggetData const& nug, TCBuf& cb)
	{
		auto col = inst.find<Colour32>(EInstComp::TintColour32);
		auto c = Colour((col ? *col : pr::Colour32White) * nug.m_tint);
		cb.m_tint = c.rgba;
	}

	// Set the texture properties of a constants buffer
	template <typename TCBuf> void SetTexDiffuse(NuggetData const& nug, TCBuf& cb)
	{
		cb.m_tex2surf0 = nug.m_tex_diffuse != nullptr
			? nug.m_tex_diffuse->m_t2s
			: pr::m4x4Identity;
	}

	// Set the environment map properties of a constants buffer
	template <typename TCBuf> void SetEnvMap(BaseInstance const& inst, NuggetData const& nug, TCBuf& cb)
	{
		auto reflectivity = inst.find<float>(EInstComp::EnvMapReflectivity);
		cb.m_env_reflectivity = reflectivity != nullptr
			? *reflectivity * nug.m_relative_reflectivity
			: 0.0f;
	}

	// Set the scene view constants
	inline void SetViewConstants(SceneView const& view, hlsl::Camera& cb)
	{
		cb.m_c2w = view.CameraToWorld();
		cb.m_c2s = view.CameraToScreen();
		cb.m_w2c = pr::InvertFast(cb.m_c2w);
		cb.m_w2s = cb.m_c2s * cb.m_w2c;
	}

	// Set the lighting constants
	inline void SetLightingConstants(Light const& light, SceneView const& view, hlsl::Light& cb)
	{
		// If the global light is camera relative, adjust the position and direction appropriately
		auto pos = light.m_cam_relative ? view.CameraToWorld() * light.m_position : light.m_position;
		auto dir = light.m_cam_relative ? view.CameraToWorld() * light.m_direction : light.m_direction;

		cb.m_info         = iv4(int(light.m_type),0,0,0);
		cb.m_ws_direction = dir;
		cb.m_ws_position  = pos;
		cb.m_ambient      = Colour(light.m_ambient).rgba;
		cb.m_colour       = Colour(light.m_diffuse).rgba;
		cb.m_specular     = Colour(light.m_specular, light.m_specular_power).rgba;
		cb.m_spot         = v4(light.m_inner_angle, light.m_outer_angle, light.m_range, light.m_falloff);
	}

	// Set the shadow map constants
	inline void SetShadowMapConstants(ShadowMap const* smap_step, hlsl::Shadow& cb)
	{
		// Ignore if there is no shadow map step
		if (smap_step == nullptr) return;

		// Add the shadow maps to the shader params
		int i = 0;
		for (auto& caster : smap_step->m_caster)
		{
			if (i == hlsl::MaxShadowMaps) break;
			cb.m_info.x = i + 1;
			cb.m_info.y = smap_step->m_smap_size;
			cb.m_w2l[i] = caster.m_params.m_w2ls;
			cb.m_l2s[i] = caster.m_params.m_ls2s;
			++i;
		}
	}

	// Set the env-map to world orientation
	inline void SetEnvMapConstants(TextureCube* env_map, hlsl::EnvMap& cb)
	{
		if (env_map == nullptr) return;
		cb.m_w2env = InvertFast(env_map->m_cube2w);
	}

	// Lock and write 'cb' into 'cbuf'. The set 'cbuf' as the constants for the shaders
	template <typename TCBuf> void WriteConstants(ID3D11DeviceContext* dc, ID3D11Buffer* cbuf, TCBuf const& cb, EShaderType shdr_types)
	{
		// Copy the buffer to the dx buffer
		if (cbuf != nullptr)
		{
			Lock lock(dc, cbuf, 0, sizeof(TCBuf), EMap::WriteDiscard, EMapFlags::None);
			*lock.ptr<TCBuf>() = cb;
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

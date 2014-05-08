//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/render/scene_view.h"
#include "pr/renderer11/lights/light.h"
#include "pr/renderer11/instances/instance.h"

#ifdef NDEBUG
#define PR_RDR_COMPILED_SHADER_DIR(file) PR_STRINGISE(renderer11/shaders/hlsl/compiled/release/##file)
#else
#define PR_RDR_COMPILED_SHADER_DIR(file) PR_STRINGISE(renderer11/shaders/hlsl/compiled/debug/##file)
#endif

namespace pr
{
	namespace rdr
	{
		// Set the geometry type
		template <typename TCBuf> void Geom(NuggetProps const& ddata, TCBuf& cb)
		{
			// Convert a geom into an iv4 for flags passed to a shader
			cb.m_geom = pr::iv4::make(
				pr::AllSet(ddata.m_geom, EGeom::Norm),
				pr::AllSet(ddata.m_geom, EGeom::Tex0),
				0,
				0);
		}

		// Set the transform properties of a constants buffer
		template <typename TCBuf> void Txfm(BaseInstance const& inst, SceneView const& view, TCBuf& cb)
		{
			pr::m4x4 o2w = GetO2W(inst);
			pr::m4x4 w2c = pr::GetInverseFast(view.m_c2w);
			pr::m4x4 c2s; if (!FindC2S(inst, c2s)) c2s = view.m_c2s;
			cb.m_o2s = c2s * w2c * o2w;
			cb.m_o2w = o2w;
			cb.m_n2w = pr::Orthonormalise(cb.m_o2w);

			pr::Transpose4x4(cb.m_o2s);
			pr::Transpose4x4(cb.m_o2w);
			pr::Transpose4x4(cb.m_n2w);
		}

		// Set the tint properties of a constants buffer
		template <typename TCBuf> void Tint(BaseInstance const& inst, TCBuf& cb)
		{
			pr::Colour32 const* col = inst.find<pr::Colour32>(EInstComp::TintColour32);
			pr::Colour c = col ? *col : pr::ColourWhite;
			cb.m_tint = c;
		}

		// Set the texture properties of a constants buffer
		template <typename TCBuf> void Tex0(NuggetProps const& ddata, TCBuf& cb)
		{
			cb.m_tex2surf0 = ddata.m_tex_diffuse != nullptr
				? ddata.m_tex_diffuse->m_t2s
				: pr::m4x4Identity;

			pr::Transpose4x4(cb.m_tex2surf0);
		}

		// Helper for setting scene view constants
		template <typename TCBuf> void SetViewConstants(SceneView const& view, TCBuf& cb)
		{
			cb.m_c2w = view.m_c2w;
			cb.m_c2s = view.m_c2s;
			cb.m_w2c = pr::GetInverseFast(cb.m_c2w);
			cb.m_w2s = cb.m_c2s * cb.m_w2c;

			pr::Transpose4x4(cb.m_c2w);
			pr::Transpose4x4(cb.m_c2s);
			pr::Transpose4x4(cb.m_w2c);
			pr::Transpose4x4(cb.m_w2s);
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
	}
}

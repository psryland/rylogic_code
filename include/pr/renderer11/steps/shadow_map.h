//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/steps/render_step.h"

namespace pr
{
	namespace rdr
	{
		// Constructs a shadow map
		struct ShadowMap :RenderStep
		{
			static const ERenderStep::Enum_ Id = ERenderStep::ShadowMap;

			Light&                           m_light;           // The shadow casting light
			D3DPtr<ID3D11Texture2D>          m_tex;
			D3DPtr<ID3D11RenderTargetView>   m_rtv;
			D3DPtr<ID3D11ShaderResourceView> m_srv;
			D3DPtr<ID3D11SamplerState>       m_samp;
			D3DPtr<ID3D11RenderTargetView>   m_main_rtv;        // The main RT for restoring after the 'rstep'
			D3DPtr<ID3D11DepthStencilView>   m_main_dsv;        // The main DB for restoring after the 'rstep'
			D3DPtr<ID3D11Buffer>             m_cbuf_frame;      // Per-frame constant buffer
			D3DPtr<ID3D11Buffer>             m_cbuf_nugget;     // Per-nugget constant buffer
			pr::iv2                          m_smap_size;       // Dimensions of the 'smap' texture
			ShaderPtr                        m_vs;
			ShaderPtr                        m_ps;
			ShaderPtr                        m_gs_face;
			ShaderPtr                        m_gs_line;

			explicit ShadowMap(Scene& scene, Light& light, pr::iv2 size);

		private:

			ShadowMap(ShadowMap const&);
			ShadowMap& operator = (ShadowMap const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Create render targets for the GBuffer based on the current render target size
			void InitRT(pr::iv2 size);

			// Set the g-buffer as the render target
			void BindRT(bool bind);

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;

			// Create a projection transform that will take points in world space and project them
			// onto a surface parallel to the frustum plane for the given face (based on light type).
			// 'shadow_frustum' - the volume in which objects receive shadows. It should be aligned with
			//  the camera frustum but with a nearer far plane.
			// 'face' - the face index of the shadow frustum (see pr::Frustum::EPlane)
			// 'light' - the light source that we're creating the projection transform for
			// 'c2w' - the camera to world (and => shadow_frustum to world) transform
			// 'max_range' - is the maximum distance of any shadow casting object from the shadow frustum
			// plane. Effectively the projection near plane for directional lights or for point lights further
			// than this distance. Objects further than this distance don't result in pixels in the 'smap'.
			// This should be the distance that depth information is normalised into the range [0,1) by.
			bool CreateProjection(pr::Frustum const& shadow_frustum, int face, Light const& light, pr::m4x4 const& c2w, float max_range, pr::m4x4& w2s);
		
			// Method used to debug 'smaps'
			void Debugging(StateStack& ss);
		};
	}
}

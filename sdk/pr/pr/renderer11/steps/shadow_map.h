//*********************************************
// Renderer
//  Copyright Â(c) Rylogic Ltd 2012
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
			D3DPtr<ID3D11RenderTargetView>   m_main_rtv;        // The main RT for restoring after the rstep
			D3DPtr<ID3D11DepthStencilView>   m_main_dsv;        // The main DB for restoring after the rstep
			D3DPtr<ID3D11Buffer>             m_cbuf_frame;      // Per-frame constant buffer
			D3DPtr<ID3D11Buffer>             m_cbuf_light;      // Per-light constant buffer
			D3DPtr<ID3D11Buffer>             m_cbuf_nugget;     // Per-nugget constant buffer
			pr::iv2                          m_smap_size;       // Dimensions of the smap texture
			float                            m_shadow_distance; // The distance in front of the camera to consider shadows

			explicit ShadowMap(Scene& scene, Light& light, pr::iv2 size);

		private:

			ShadowMap(ShadowMap const&);
			ShadowMap& operator = (ShadowMap const&);

			// The type of render step this is
			ERenderStep::Enum_ GetId() const override { return Id; }

			// Create render targets for the gbuffer based on the current render target size
			void InitRT(pr::iv2 size);

			// Set the g-buffer as the render target
			void BindRT(bool bind);

			// Add model nuggets to the draw list for this render step
			void AddNuggets(BaseInstance const& inst, TNuggetChain& nuggets) override;

			// Perform the render step
			void ExecuteInternal(StateStack& ss) override;

			// Create a projection transform that will take points in world space and project them
			// onto a surface parallel to the frustum plane for the given face (based on light type).
			bool CreateProjection(int face, pr::Frustum const& frust, pr::m4x4 const& c2w, Light const& light, pr::m4x4& w2s);
		};
	}
}
